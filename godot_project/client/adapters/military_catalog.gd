class_name MilitaryCatalog
extends RefCounted

## Display-only military metadata — plain English for spectator/inspector (no sim rules).

const BUILDINGS_PATH := "res://sim/data/config/military_buildings.json"
const UNITS_PATH := "res://sim/data/config/military_units.json"

const DISPLAY_ALIASES: Dictionary = {
	"vehicle_plant": "fabricator",
	"defence_turret": "turret",
	"collector": "harvester",
	"resource_processor": "supply_depot",
}

const ROLE_TEXT: Dictionary = {
	"command_hub": "Your main base. Lose it and the battle is over.",
	"power_generator": "Supplies power so other buildings can operate.",
	"supply_depot": "Processes harvested minerals into credits.",
	"barracks": "Trains infantry squads such as Rifle Squads.",
	"fabricator": "Builds vehicles and harvesters for your army.",
	"turret": "Automated defence — fires at enemies in range.",
	"rifle_squad": "Basic infantry — good against other infantry.",
	"harvester": "Collects minerals from fields and returns them to base.",
}

const STATUS_REASONS: Dictionary = {
	"underpowered": "Waiting for more power",
	"insufficient_credits": "Not enough credits",
	"blocked": "Blocked",
	"no_field": "No mineral field in range",
}


static var _buildings: Dictionary = {}
static var _units: Dictionary = {}
static var _loaded: bool = false


static func load_catalog() -> void:
	if _loaded:
		return
	_buildings = _load_json(BUILDINGS_PATH).get("buildings", {})
	_units = _load_json(UNITS_PATH).get("units", {})
	_loaded = true


static func catalog_key(sim_type: String) -> String:
	return str(DISPLAY_ALIASES.get(sim_type, sim_type))


static func display_name(sim_type: String) -> String:
	load_catalog()
	var key: String = catalog_key(sim_type)
	if _buildings.has(sim_type):
		return str(_buildings[sim_type].get("display_name", _titleize(sim_type)))
	if _units.has(sim_type):
		return str(_units[sim_type].get("display_name", _titleize(sim_type)))
	match key:
		"fabricator":
			return "Fabricator"
		"turret":
			return "Turret"
		"harvester":
			return "Harvester"
		"supply_depot":
			return "Supply Depot"
	return _titleize(key)


static func role_text(sim_type: String) -> String:
	var key: String = catalog_key(sim_type)
	return str(ROLE_TEXT.get(key, ROLE_TEXT.get(sim_type, "Military asset.")))


static func human_reason(reason: String) -> String:
	var lower: String = reason.to_lower()
	for key in STATUS_REASONS:
		if key in lower:
			return str(STATUS_REASONS[key])
	return reason.replace("_", " ").capitalize()


static func format_building_inspector(data: Dictionary, snapshot: Dictionary) -> String:
	var btype: String = str(data.get("building_type", "?"))
	var name: String = display_name(btype)
	var q: int = int(data.get("anchor_q", data.get("q", -1)))
	var r: int = int(data.get("anchor_r", data.get("r", -1)))
	var hp: int = int(data.get("hp", 0))
	var max_hp: int = int(data.get("max_hp", hp))
	var lines: PackedStringArray = PackedStringArray([
		name,
		role_text(btype),
		"",
		"Health: %d / %d" % [hp, maxi(max_hp, 1)],
		"Status: %s" % _building_status(data),
		"Position: (%d, %d)" % [q, r],
	])
	var progress: int = int(data.get("production_progress", 0))
	if progress > 0:
		lines.append("Production progress: %d ticks" % progress)
	var queue_line: String = _queue_line_for_building(int(data.get("id", 0)), snapshot)
	if queue_line != "":
		lines.append(queue_line)
	var paused: String = str(data.get("paused_reason", ""))
	if paused != "":
		lines.append("Waiting: %s" % human_reason(paused))
	return "\n".join(lines)


static func format_unit_inspector(data: Dictionary, snapshot: Dictionary) -> String:
	var utype: String = str(data.get("unit_type", "?"))
	var name: String = display_name(utype)
	var q: int = int(data.get("q", -1))
	var r: int = int(data.get("r", -1))
	var hp: int = int(data.get("hp", 0))
	var max_hp: int = int(data.get("max_hp", hp))
	var lines: PackedStringArray = PackedStringArray([
		name,
		role_text(utype),
		"",
		"Health: %d / %d" % [hp, maxi(max_hp, 1)],
		"Status: %s" % str(data.get("state", "idle")).capitalize(),
		"Position: (%d, %d)" % [q, r],
	])
	var order: String = str(data.get("order", ""))
	if order != "":
		lines.append("Orders: %s" % order.replace("_", " "))
	var target_id: int = int(data.get("attack_target_id", 0))
	if target_id > 0:
		lines.append("Attacking target #%d" % target_id)
	var cargo: int = int(data.get("cargo", 0))
	if cargo > 0:
		lines.append("Cargo: %d minerals" % cargo)
	var field_id: int = int(data.get("harvest_field_id", 0))
	if field_id > 0:
		lines.append("Harvest field: #%d" % field_id)
	return "\n".join(lines)


static func format_construction_site(data: Dictionary) -> String:
	var btype: String = str(data.get("building_type", "?"))
	var progress: int = LayerRenderer.site_progress_percent(data)
	return (
		"Construction Site: %s\n%s\n\nProgress: %d%%\nPosition: (%d, %d)"
		% [
			display_name(btype),
			role_text(btype),
			progress,
			int(data.get("anchor_q", data.get("q", -1))),
			int(data.get("anchor_r", data.get("r", -1))),
		]
	)


static func format_selection(selection: Dictionary, snapshot: Dictionary) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {})
	match kind:
		"building":
			return format_building_inspector(data, snapshot)
		"unit":
			return format_unit_inspector(data, snapshot)
		"construction_site":
			return format_construction_site(data)
		"empty":
			var q: int = int(data.get("q", -1))
			var r: int = int(data.get("r", -1))
			if q >= 0:
				return "Empty hex (%d, %d)\nClick a unit or building to inspect it." % [q, r]
			return "Click the map to inspect units, buildings, or construction sites."
		_:
			return "Selection: %s" % kind


static func _building_status(data: Dictionary) -> String:
	var state: String = str(data.get("state", "idle")).to_lower()
	var paused: String = str(data.get("paused_reason", ""))
	if paused != "":
		return "Waiting — %s" % human_reason(paused)
	match state:
		"working", "active":
			return "Operational"
		"underpowered":
			return "Underpowered"
		_:
			return state.capitalize()


static func _queue_line_for_building(building_id: int, snapshot: Dictionary) -> String:
	for queue in snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		if int(queue.get("building_id", 0)) != building_id:
			continue
		var items: Array = queue.get("items", [])
		if items.is_empty():
			return "Production queue: empty"
		var labels: PackedStringArray = PackedStringArray()
		for item in items:
			if typeof(item) != TYPE_DICTIONARY:
				continue
			var utype: String = str(item.get("unit_type", "?"))
			var prog: int = int(item.get("progress", 0))
			var req: int = int(item.get("required_ticks", 0))
			labels.append("%s (%d/%d)" % [display_name(utype), prog, req])
		return "Production queue: " + ", ".join(labels)
	return ""


static func _load_json(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {}
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	return parsed if typeof(parsed) == TYPE_DICTIONARY else {}


static func _titleize(text: String) -> String:
	return text.replace("_", " ").capitalize()
