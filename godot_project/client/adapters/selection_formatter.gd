class_name SelectionFormatter
extends RefCounted

## Formats snapshot selection data for human-readable inspector display.


static func format(selection: Dictionary, snapshot: Dictionary = {}, mode_context: Dictionary = {}) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {})
	var q: int = int(data.get("q", data.get("anchor_q", -1)))
	var r: int = int(data.get("r", data.get("anchor_r", -1)))
	if q < 0 and data.has("anchor_q"):
		q = int(data.get("anchor_q", -1))
		r = int(data.get("anchor_r", -1))

	match kind:
		"building":
			return _format_building(data, q, r, snapshot)
		"unit":
			return _format_unit(data, q, r, snapshot)
		"construction_site":
			return _format_construction_site(data, q, r, snapshot)
		"carrier":
			return _format_carrier(data, q, r, snapshot)
		"forest":
			return _format_forest(data, q, r, snapshot)
		"tile":
			return _format_tile(data, q, r, snapshot)
		"road_node":
			return _format_road_node(data, q, r, snapshot, mode_context)
		"road":
			return _format_road(data, q, r, snapshot, mode_context)
		_:
			return _format_empty(q, r, mode_context)


static func _format_building(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var btype: String = str(data.get("building_type", "?"))
	var name: String = BuildingCatalog.display_name(btype)
	var lines: PackedStringArray = PackedStringArray([
		name,
		BuildingCatalog.role_text(btype),
		"",
		"Status: %s" % _human_status(data),
		"Position: (%d, %d)" % [q, r],
	])
	if btype in ["headquarters", "command_hub"]:
		lines.append(_hq_stock_summary(snapshot, data))
		lines.append(_road_connection_line(q, r, snapshot))
		lines.append("Tip: Build roads from here to new buildings.")
	else:
		lines.append(_store_line_for_building(int(data.get("id", 0)), snapshot))
		lines.append(_recipe_line(btype))
		lines.append(_road_connection_line(q, r, snapshot))
	var paused: String = str(data.get("paused_reason", ""))
	if paused != "":
		lines.append("Waiting: %s" % SettlerMvpController.human_reason(paused))
	_append_blocked_from_diagnostics(lines, btype, snapshot)
	var tip: String = BuildingCatalog.tip_text(btype)
	if tip != "":
		lines.append("Tip: %s" % tip)
	return "\n".join(lines)


static func _format_construction_site(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var btype: String = str(data.get("building_type", "?"))
	var name: String = BuildingCatalog.display_name(btype)
	var progress: int = LayerRenderer.site_progress_percent(data)
	var lines: PackedStringArray = PackedStringArray([
		"Construction Site: %s" % name,
		BuildingCatalog.role_text(btype),
		"",
		"Status: %s" % _site_status(data, progress),
		"Position: (%d, %d)" % [q, r],
	])
	var delivered: Dictionary = data.get("delivered", {})
	var required: Dictionary = data.get("required", {})
	if not required.is_empty():
		lines.append("Materials:")
		for ware_type in required:
			lines.append("- %s: %d / %d" % [ware_type, int(delivered.get(ware_type, 0)), int(required[ware_type])])
	lines.append(_road_connection_line(q, r, snapshot))
	lines.append("Tip: Press Play to let carriers deliver materials.")
	return "\n".join(lines)


static func _format_tile(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var terrain: String = str(data.get("terrain_type", "grass"))
	var lines: PackedStringArray = PackedStringArray([
		"Terrain: %s" % terrain.capitalize(),
		"Hex: (%d, %d)" % [q, r],
		"Buildable: %s" % ("yes" if _is_buildable_terrain(terrain) else "no"),
	])
	var density: int = _tree_density_at(q, r, snapshot)
	if density > 0:
		lines.append("Tree density: %d" % density)
	var deposit: String = _deposit_at(q, r, snapshot)
	if deposit != "":
		lines.append("Deposit: %s" % deposit)
	var on_hex: String = _occupant_summary(q, r, snapshot)
	if on_hex != "":
		lines.append(on_hex)
	if terrain == "forest":
		lines.append("Tip: Good area for a Woodcutter or Forester nearby.")
	elif deposit == "rock" or terrain == "mountain":
		lines.append("Tip: Quarry needs rock or quarryable terrain nearby.")
	return "\n".join(lines)


static func _format_forest(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var density: int = int(data.get("tree_density", 0))
	var lines: PackedStringArray = PackedStringArray([
		"Terrain: Forest",
		"Hex: (%d, %d)" % [q, r],
		"Tree density: %d" % density,
		"Buildable: yes (forest buildings only)",
	])
	lines.append(_occupant_summary(q, r, snapshot))
	lines.append("Tip: Woodcutters harvest here; Foresters renew trees.")
	return "\n".join(lines)


static func _format_carrier(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"Carrier",
		"Carries goods along roads.",
		"",
		"Status: %s" % _carrier_status_text(data),
	])
	var ware: String = str(data.get("ware_type", ""))
	if ware != "":
		lines.append("Carrying: %s x%d" % [ware.capitalize(), int(data.get("ware_amount", 0))])
		lines.append("Current task: delivering %s" % ware)
	else:
		lines.append("Carrying: (nothing)")
	var from_to: Dictionary = _carrier_from_to_display(data, snapshot)
	if from_to.has("from"):
		lines.append("From: %s" % str(from_to["from"]))
	if from_to.has("to"):
		lines.append("To: %s" % str(from_to["to"]))
	elif not from_to.is_empty():
		lines.append("Route: in progress")
	lines.append("Tip: Carriers move automatically when roads connect buildings.")
	return "\n".join(lines)


static func _format_unit(data: Dictionary, q: int, r: int, snapshot: Dictionary) -> String:
	var utype: String = str(data.get("unit_type", "worker"))
	var lines: PackedStringArray = PackedStringArray([
		"Worker: %s" % utype.capitalize(),
		"Position: (%d, %d)" % [q, r],
		"Status: %s" % str(data.get("state", "?")).capitalize(),
	])
	var order: String = str(data.get("order", ""))
	if order != "":
		lines.append("Task: %s" % order.replace("_", " "))
	return "\n".join(lines)


static func _format_road_node(data: Dictionary, q: int, r: int, snapshot: Dictionary, mode_context: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"Road flag / node",
		"Hex: (%d, %d)" % [q, r],
		"Connected to HQ network: %s" % ("yes" if _road_connected_to_hq(q, r, snapshot) else "unknown"),
	])
	if bool(mode_context.get("road_mode", false)):
		lines.append("Road mode: click another networked hex as destination.")
	return "\n".join(lines)


static func _format_road(data: Dictionary, q: int, r: int, snapshot: Dictionary, mode_context: Dictionary) -> String:
	var lines: PackedStringArray = PackedStringArray([
		"Road segment",
		"Near hex: (%d, %d)" % [q, r],
		"Player: %s" % str(data.get("player_id", "?")),
	])
	if bool(mode_context.get("road_mode", false)):
		lines.append("Road mode: click start on network, then destination.")
	return "\n".join(lines)


static func _format_empty(q: int, r: int, mode_context: Dictionary) -> String:
	if q < 0:
		return "Click the map to inspect terrain, buildings, roads, or carriers."
	var lines: PackedStringArray = PackedStringArray(["Empty hex (%d, %d)" % [q, r]])
	if bool(mode_context.get("build_mode", false)):
		lines.append("Placement mode: move over the map and click a valid hex.")
	elif bool(mode_context.get("road_mode", false)):
		lines.append("Road mode: select start, then destination.")
	else:
		lines.append("Click buildings, sites, roads, or terrain to inspect.")
	return "\n".join(lines)


static func _human_status(data: Dictionary) -> String:
	var state: String = str(data.get("state", "working")).to_lower()
	var paused: String = str(data.get("paused_reason", ""))
	if paused != "":
		return "Waiting — %s" % SettlerMvpController.human_reason(paused)
	match state:
		"working", "active":
			return "Working"
		"idle":
			return "Idle"
		"blocked":
			return "Blocked"
		_:
			return state.capitalize()


static func _site_status(data: Dictionary, progress: int) -> String:
	if progress >= 100:
		return "Complete"
	var delivered: Dictionary = data.get("delivered", {})
	var required: Dictionary = data.get("required", {})
	for ware_type in required:
		if int(delivered.get(ware_type, 0)) < int(required[ware_type]):
			return "Waiting for materials"
	return "Under construction (%d%%)" % progress


static func _store_line_for_building(building_id: int, snapshot: Dictionary) -> String:
	var render: Dictionary = LayerRenderer.build_render_data(snapshot)
	for store in render.get("building_stores", []):
		if int(store.get("building_id", -1)) == building_id:
			var label: String = str(store.get("label", ""))
			if label == "" or label == "(empty)":
				return "Local storage: empty"
			return "Local storage: %s" % label
	return "Local storage: empty"


static func _hq_stock_summary(snapshot: Dictionary, building: Dictionary = {}) -> String:
	var store_id: int = int(building.get("store_id", 0))
	if store_id > 0:
		var store: Dictionary = _ware_store_lookup(snapshot).get(store_id, {})
		var wares: Dictionary = store.get("wares", {})
		if typeof(wares) == TYPE_DICTIONARY and not wares.is_empty():
			var lines: PackedStringArray = PackedStringArray(["Stored goods:"])
			for ware_type in ["timber", "planks", "stone", "builder_work"]:
				if wares.has(ware_type) and int(wares[ware_type]) > 0:
					lines.append("- %s: %d" % [ware_type.capitalize(), int(wares[ware_type])])
			if lines.size() > 1:
				return "\n".join(lines)
	var totals: Dictionary = snapshot.get("diagnostics", {}).get("ware_totals", {}).get("by_type", {})
	var parts: PackedStringArray = PackedStringArray()
	for ware in ["timber", "planks", "stone"]:
		if totals.has(ware):
			parts.append("- %s: %d" % [ware.capitalize(), int(totals[ware])])
	if parts.is_empty():
		return "Stored goods:\n- (none yet)"
	return "Stored goods:\n%s" % "\n".join(parts)


static func _recipe_line(btype: String) -> String:
	match btype:
		"woodcutter":
			return "Output: timber (from forest)"
		"forester":
			return "Renews forest tree density"
		"sawmill":
			return "Inputs: timber — Output: planks"
		"quarry":
			return "Output: stone (from rock)"
		_:
			return ""


static func _road_connection_line(q: int, r: int, snapshot: Dictionary) -> String:
	var connected: String = "yes" if _hex_has_road_network(q, r, snapshot) else "no — build roads to HQ"
	return "Road connection: %s" % connected


static func _append_blocked_from_diagnostics(lines: PackedStringArray, btype: String, snapshot: Dictionary) -> void:
	for prod in snapshot.get("diagnostics", {}).get("production", []):
		if str(prod.get("building_type", "")) == btype:
			var reason: String = str(prod.get("paused_reason", ""))
			if reason != "":
				lines.append("Blocked: %s" % SettlerMvpController.human_reason(reason))


static func _is_buildable_terrain(terrain: String) -> bool:
	return terrain in ["grass", "forest", "desert"]


static func _tree_density_at(q: int, r: int, snapshot: Dictionary) -> int:
	for td in snapshot.get("display_layers", {}).get("tree_density", []):
		if int(td.get("q", -1)) == q and int(td.get("r", -1)) == r:
			return int(td.get("tree_density", 0))
	return 0


static func _deposit_at(q: int, r: int, snapshot: Dictionary) -> String:
	for res in snapshot.get("display_layers", {}).get("resources", []):
		if int(res.get("q", -1)) == q and int(res.get("r", -1)) == r:
			return str(res.get("deposit_type", ""))
	return ""


static func _occupant_summary(q: int, r: int, snapshot: Dictionary) -> String:
	for site in snapshot.get("construction_sites", []):
		if int(site.get("anchor_q", site.get("q", -1))) == q and int(site.get("anchor_r", site.get("r", -1))) == r:
			return "Construction site: %s" % BuildingCatalog.display_name(str(site.get("building_type", "")))
	for building in snapshot.get("buildings", []):
		if int(building.get("anchor_q", building.get("q", -1))) == q and int(building.get("anchor_r", building.get("r", -1))) == r:
			return "Building: %s" % BuildingCatalog.display_name(str(building.get("building_type", "")))
	return ""


static func _hex_has_road_network(q: int, r: int, snapshot: Dictionary) -> bool:
	for node in snapshot.get("road_nodes", []):
		if int(node.get("q", -1)) == q and int(node.get("r", -1)) == r:
			return true
	for road in snapshot.get("roads", []):
		if (int(road.get("from_q", -1)) == q and int(road.get("from_r", -1)) == r) or (int(road.get("to_q", -1)) == q and int(road.get("to_r", -1)) == r):
			return true
	return false


static func _road_connected_to_hq(_q: int, _r: int, snapshot: Dictionary) -> bool:
	return _hex_has_road_network(_q, _r, snapshot)


static func _carrier_status_text(data: Dictionary) -> String:
	var state: String = str(data.get("state", "idle")).to_lower()
	match state:
		"moving", "assigned":
			return "Delivering"
		"blocked":
			return "Blocked"
		_:
			return "Idle"


## Display-only: resolve carrier origin/destination labels from snapshot (never affects sim).
static func _carrier_from_to_display(data: Dictionary, snapshot: Dictionary) -> Dictionary:
	var out: Dictionary = {}
	var job_id: int = int(data.get("job_id", 0))
	for job in snapshot.get("delivery_jobs", []):
		if int(job.get("id", -1)) != job_id:
			continue
		out["from"] = _building_label_for_store(snapshot, str(job.get("source_store_id", "")))
		out["to"] = _entity_label(snapshot, str(job.get("dest_entity_id", "")), str(job.get("dest_entity_type", "")))
		return out
	var route: Array = data.get("route_points", [])
	if route.size() >= 2:
		var start_q: int = int(route[0].get("q", -1))
		var start_r: int = int(route[0].get("r", -1))
		var end_q: int = int(route[route.size() - 1].get("q", -1))
		var end_r: int = int(route[route.size() - 1].get("r", -1))
		out["from"] = _building_at_hex(snapshot, start_q, start_r)
		out["to"] = _building_at_hex(snapshot, end_q, end_r)
	return out


static func _building_at_hex(snapshot: Dictionary, q: int, r: int) -> String:
	for building in snapshot.get("buildings", []):
		if BuildingCatalog.occupies_hex(building, q, r):
			return BuildingCatalog.display_name(str(building.get("building_type", "")))
	for site in snapshot.get("construction_sites", []):
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		if sq == q and sr == r:
			return "Construction Site: %s" % BuildingCatalog.display_name(str(site.get("building_type", "")))
	return "Road node (%d,%d)" % [q, r]


static func _building_label_for_store(snapshot: Dictionary, store_id_str: String) -> String:
	var store_id: int = int(store_id_str)
	for building in snapshot.get("buildings", []):
		if int(building.get("store_id", -1)) == store_id:
			return BuildingCatalog.display_name(str(building.get("building_type", "")))
	return "Store %s" % store_id_str


static func _entity_label(snapshot: Dictionary, entity_id: String, entity_type: String) -> String:
	var eid: int = int(entity_id)
	if entity_type == "building":
		for building in snapshot.get("buildings", []):
			if int(building.get("id", -1)) == eid:
				return BuildingCatalog.display_name(str(building.get("building_type", "")))
	if entity_type == "construction_site":
		for site in snapshot.get("construction_sites", []):
			if int(site.get("id", -1)) == eid:
				return "Construction Site: %s" % BuildingCatalog.display_name(str(site.get("building_type", "")))
	return "%s %s" % [entity_type, entity_id]


static func _ware_store_lookup(snapshot: Dictionary) -> Dictionary:
	var lookup: Dictionary = {}
	var entities: Dictionary = snapshot.get("entities", {})
	for store in entities.get("ware_stores", []):
		if typeof(store) != TYPE_DICTIONARY:
			continue
		lookup[int(store.get("id", 0))] = store
	return lookup
