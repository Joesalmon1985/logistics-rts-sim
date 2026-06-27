class_name Hybrid3dInspectorPanel
extends RefCounted

## Plain-English hybrid inspector for 3D spectator selection.


static func format_selection(selection: Dictionary, snapshot: Dictionary) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {})
	match kind:
		"economy_building":
			return _format_economy_building(data, snapshot)
		"military_building":
			return _format_military_building(data, snapshot)
		"construction_site":
			return _format_construction_site(data)
		"unit":
			return _format_unit(data)
		"carrier":
			return _format_carrier(data)
		"terrain":
			return _format_terrain(data)
		_:
			if int(data.get("q", -1)) >= 0:
				return "[b]Empty hex[/b]\n\nNo selectable object here.\n\nTip: Click buildings, carriers, or soldiers."
			return "[b]Nothing selected[/b]\n\nClick the 3D world to inspect roads, buildings, carriers, or soldiers."


static func _format_economy_building(building: Dictionary, _snapshot: Dictionary) -> String:
	var btype: String = str(building.get("building_type", "building"))
	var player_id: String = str(building.get("player_id", "p1"))
	var name: String = BuildingCatalog.display_name(btype)
	var role: String = BuildingCatalog.role_text(btype)
	var tip: String = BuildingCatalog.tip_text(btype)
	var status: String = str(building.get("status", "operating"))
	return (
		"[b]%s[/b] — Bot %s\n\n%s\n\nStatus: %s\nConnected to road: %s\n\nTip: %s"
		% [
			name,
			player_id.to_upper(),
			role,
			status,
			"yes" if bool(building.get("road_connected", true)) else "no",
			tip if tip != "" else "Watch carriers on nearby roads.",
		]
	)


static func _format_military_building(building: Dictionary, snapshot: Dictionary) -> String:
	var btype: String = str(building.get("building_type", "command_hub"))
	var player_id: String = str(building.get("player_id", "p1"))
	var name: String = MilitaryCatalog.display_name(btype)
	var role: String = MilitaryCatalog.role_text(btype)
	var blocked: String = _queue_blocked_reason(building, snapshot)
	var queue_line: String = _queue_line_for_building(building, snapshot)
	return (
		"[b]%s[/b] — Bot %s\n\n%s\n\nStatus: %s\nQueue: %s\nBlocked reason: %s\n\nTip: Watch the road from the quarry or sawmill to see whether materials are arriving."
		% [
			name,
			player_id.to_upper(),
			role,
			str(building.get("status", "operating")),
			queue_line,
			blocked if blocked != "" else "(none)",
		]
	)


static func _format_construction_site(site: Dictionary) -> String:
	var target: String = BuildingCatalog.display_name(str(site.get("building_type", "building")))
	var progress: int = int(site.get("progress", 0))
	var required: int = int(site.get("required_progress", 100))
	var missing: String = str(site.get("missing_ware", site.get("blocked_reason", "")))
	return (
		"[b]Construction site[/b]\n\nBuilding: %s\nProgress: %d / %d\nMissing: %s\n\nTip: Carriers must deliver wares along connected roads."
		% [target, progress, required, missing if missing != "" else "(none)"]
	)


static func _format_unit(unit: Dictionary) -> String:
	var utype: String = str(unit.get("unit_type", "unit"))
	var player_id: String = str(unit.get("player_id", "p1"))
	var name: String = MilitaryCatalog.display_name(utype)
	var role: String = MilitaryCatalog.role_text(utype)
	var hp: int = int(unit.get("hp", unit.get("health", 0)))
	var max_hp: int = int(unit.get("max_hp", unit.get("max_health", hp)))
	var order: String = str(unit.get("order", unit.get("stance", "idle")))
	return (
		"[b]%s[/b] — Bot %s\n\n%s\n\nHealth: %d / %d\nOrder: %s\n\nTip: Press F to follow this unit with the camera."
		% [name, player_id.to_upper(), role, hp, max_hp, order]
	)


static func _format_carrier(carrier: Dictionary) -> String:
	var player_id: String = str(carrier.get("player_id", "p1"))
	var ware: String = str(carrier.get("carried_ware", carrier.get("ware", "goods")))
	return (
		"[b]Carrier[/b] — Bot %s\n\nCarrying: %s\n\nTip: Toggle the supply overlay to highlight delivery routes."
		% [player_id.to_upper(), ware if ware != "" else "empty"]
	)


static func _format_terrain(tile: Dictionary) -> String:
	return (
		"[b]Terrain hex[/b]\n\nType: %s\n\nTip: Roads and buildings must respect terrain build rules."
		% str(tile.get("terrain_type", tile.get("terrain", "grass")))
	)


static func _queue_line_for_building(building: Dictionary, snapshot: Dictionary) -> String:
	var building_id: int = int(building.get("id", 0))
	for queue in snapshot.get("production_queues", []):
		if int(queue.get("building_id", -1)) != building_id:
			continue
		var items: Array = queue.get("items", [])
		if items.is_empty():
			return "(idle)"
		var labels: PackedStringArray = PackedStringArray()
		for item in items:
			labels.append(MilitaryCatalog.display_name(str(item.get("unit_type", "?"))))
		return ", ".join(labels)
	return "(idle)"


static func _queue_blocked_reason(building: Dictionary, snapshot: Dictionary) -> String:
	var building_id: int = int(building.get("id", 0))
	for queue in snapshot.get("production_queues", []):
		if int(queue.get("building_id", -1)) != building_id:
			continue
		for item in queue.get("items", []):
			var reason: String = str(item.get("blocked_reason", ""))
			if reason != "":
				return MilitaryCatalog.human_reason(reason)
	return ""
