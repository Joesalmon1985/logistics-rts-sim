class_name SelectionFormatter
extends RefCounted

## Formats snapshot selection data for human-readable inspector display.


static func format(selection: Dictionary, snapshot: Dictionary = {}) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {})
	var q: int = int(data.get("q", data.get("anchor_q", -1)))
	var r: int = int(data.get("r", data.get("anchor_r", -1)))
	if q < 0 and data.has("anchor_q"):
		q = int(data.get("anchor_q", -1))
		r = int(data.get("anchor_r", -1))

	match kind:
		"building":
			return _format_building(data, q, r)
		"unit":
			return _format_unit(data, q, r, snapshot)
		"construction_site":
			return _format_construction_site(data, q, r)
		"tile":
			return _format_tile(data, q, r, snapshot)
		_:
			return "Empty hex (%d, %d)\nClick a coloured hex to inspect terrain, buildings, or units." % [q, r]


static func _format_building(data: Dictionary, q: int, r: int) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"BUILDING",
		"Type: %s" % str(data.get("building_type", "?")),
		"Player: %s" % str(data.get("player_id", "?")),
		"Position: (%d, %d)" % [q, r],
		"State: %s" % str(data.get("state", "?")),
	])
	var max_hp: int = int(data.get("max_hp", 0))
	if max_hp > 0:
		lines.append("HP: %d / %d" % [int(data.get("hp", 0)), max_hp])
	lines.append("ID: %s" % str(data.get("id", "?")))
	return "\n".join(lines)


static func _format_unit(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"UNIT",
		"Type: %s" % str(data.get("unit_type", "?")),
		"Player: %s" % str(data.get("player_id", "?")),
		"Position: (%d, %d)" % [q, r],
		"State: %s" % str(data.get("state", "?")),
	])
	var max_hp: int = int(data.get("max_hp", 0))
	if max_hp > 0:
		lines.append("HP: %d / %d" % [int(data.get("hp", data.get("health", 0))), max_hp])
	elif data.has("hp") or data.has("health"):
		lines.append("HP: %s" % str(data.get("hp", data.get("health", "?"))))
	var weapon: String = str(data.get("weapon", ""))
	if weapon != "":
		lines.append("Weapon: %s" % weapon)
	var order: String = str(data.get("order", ""))
	if order != "":
		lines.append("Order: %s" % order)
	var target_id: int = int(data.get("engage_target_id", 0))
	if target_id > 0:
		var target: Dictionary = _find_entity(snapshot, target_id)
		if not target.is_empty():
			var tq: int = int(target.get("q", target.get("anchor_q", -1)))
			var tr: int = int(target.get("r", target.get("anchor_r", -1)))
			var ttype: String = str(target.get("unit_type", target.get("building_type", "target")))
			lines.append("Target: %s (id %d) at (%d, %d)" % [ttype, target_id, tq, tr])
			var tmax: int = int(target.get("max_hp", 0))
			if tmax > 0:
				lines.append("Target HP: %d / %d" % [int(target.get("hp", 0)), tmax])
	lines.append("ID: %s" % str(data.get("id", "?")))
	return "\n".join(lines)


static func _format_construction_site(data: Dictionary, q: int, r: int) -> String:
	var btype: String = str(data.get("building_type", "?"))
	var progress: int = LayerRenderer.site_progress_percent(data)
	var lines: PackedStringArray = PackedStringArray([
		"CONSTRUCTION SITE",
		"Type: %s construction site" % btype,
		"Position: (%d, %d)" % [q, r],
		"State: %s" % str(data.get("state", "?")),
		"Progress: %d%%" % progress,
	])
	if data.has("required_ticks") and int(data.get("required_ticks", 0)) > 0:
		lines.append("Build progress: %d / %d ticks" % [int(data.get("progress", 0)), int(data.get("required_ticks", 0))])
	else:
		var delivered: Dictionary = data.get("delivered", {})
		var required: Dictionary = data.get("required", {})
		if not required.is_empty():
			var parts: PackedStringArray = PackedStringArray()
			for ware_type in required:
				parts.append("%s: %d/%d" % [
					str(ware_type),
					int(delivered.get(ware_type, 0)),
					int(required[ware_type]),
				])
			lines.append("Delivered: %s" % ", ".join(parts))
	lines.append("ID: %s" % str(data.get("id", "?")))
	return "\n".join(lines)


static func _format_tile(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"TERRAIN HEX",
		"Position: (%d, %d)" % [q, r],
		"Terrain: %s" % str(data.get("terrain_type", "?")),
	])
	var layers: Dictionary = snapshot.get("display_layers", {})
	for res in layers.get("resources", []):
		if int(res.get("q", -1)) == q and int(res.get("r", -1)) == r:
			lines.append("Resource: %s (amount %s)" % [
				str(res.get("deposit_type", "?")),
				str(res.get("deposit_amount", "?")),
			])
	for res2 in snapshot.get("resource_fields", []):
		if int(res2.get("q", -1)) == q and int(res2.get("r", -1)) == r:
			lines.append("Resource field: %s" % str(res2.get("field_type", "?")))
	return "\n".join(lines)


static func _find_entity(snapshot: Dictionary, entity_id: int) -> Dictionary:
	for unit in snapshot.get("units", []):
		if int(unit.get("id", -1)) == entity_id:
			return unit
	for building in snapshot.get("buildings", []):
		if int(building.get("id", -1)) == entity_id:
			return building
	return {}
