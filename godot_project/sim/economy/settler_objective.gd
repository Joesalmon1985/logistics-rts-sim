class_name SettlerObjective
extends RefCounted

## Authoritative composite objective evaluator for the Settler MVP scenario.


const BUILDING_LABELS: Dictionary = {
	"woodcutter": "Woodcutter Lodge",
	"forester": "Forester Camp",
	"sawmill": "Sawmill",
	"quarry": "Quarry",
}


static func load_config(relative_path: String) -> Dictionary:
	if relative_path == "":
		return {}
	return ConfigLoader.load_json(relative_path)


static func evaluate(state: GameState, config: Dictionary, player_id: String = "p1") -> Dictionary:
	if config.is_empty():
		return {"complete": false, "requirements": [], "victory": false}

	var requirements: Array = []
	var all_done: bool = true

	var hq_node_id: int = _hq_road_node_id(state, player_id)
	var required_buildings: Array = config.get("required_buildings", [])
	var require_connection: bool = bool(config.get("require_connection", false))

	for building_type in required_buildings:
		var btype: String = str(building_type)
		var building: Dictionary = _find_active_building(state, player_id, btype)
		var built: bool = not building.is_empty()
		var connected: bool = true
		if built and require_connection:
			var node_id: int = int(building.get("road_node_id", 0))
			connected = hq_node_id > 0 and node_id > 0 and EconomyRoadSystem.is_network_connected(
				state, hq_node_id, node_id,
			)
		var done: bool = built and connected
		if not done:
			all_done = false
		var detail: String = ""
		if built and not connected:
			detail = "No road connection to headquarters."
		elif not built:
			detail = "Not built yet."
		requirements.append({
			"key": "building_%s" % btype,
			"label": BUILDING_LABELS.get(btype, btype),
			"target": 1,
			"current": 1 if done else 0,
			"done": done,
			"detail": detail,
		})

	var ware_thresholds: Dictionary = config.get("ware_thresholds", {})
	for ware_type in ware_thresholds:
		var target: int = int(ware_thresholds[ware_type])
		var current: int = _player_ware_total(state, player_id, str(ware_type))
		var done_ware: bool = current >= target
		if not done_ware:
			all_done = false
		requirements.append({
			"key": "ware_%s" % str(ware_type),
			"label": "Store %s" % str(ware_type),
			"target": target,
			"current": current,
			"done": done_ware,
			"detail": "" if done_ware else "%d / %d stored." % [current, target],
		})

	return {
		"complete": all_done,
		"victory": all_done,
		"requirements": requirements,
		"player_id": player_id,
	}


static func _hq_road_node_id(state: GameState, player_id: String) -> int:
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_ids", {}).get(player_id, 0))
	if hq_id <= 0:
		hq_id = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	if hq_id <= 0:
		return 0
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	return int(hq.get("road_node_id", 0))


static func _find_active_building(state: GameState, player_id: String, building_type: String) -> Dictionary:
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("state", "")) != EconomyConstants.BLD_ACTIVE:
			continue
		return building
	return {}


static func _player_ware_total(state: GameState, player_id: String, ware_type: String) -> int:
	var total: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		total += int(store.get("wares", {}).get(ware_type, 0))
	return total
