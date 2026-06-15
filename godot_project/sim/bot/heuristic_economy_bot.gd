class_name HeuristicEconomyBot
extends Bot

## Deterministic economy bot using the formal Bot interface (BOT-SMOKE-001 / ECON-BOT-001).

const ACTOR: String = "heuristic_economy_bot"

var _plan_index: int = 0
var _road_queue: Array = []
var _decision_log: Array = []
var _validator: CommandValidator


func _init() -> void:
	actor_id = ACTOR
	_validator = CommandValidator.new(CommandRegistry.create_default())


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_plan_index = 0
	_road_queue.clear()
	_decision_log.clear()


func get_decisions() -> Array:
	return _decision_log


func is_complete(snapshot: Dictionary) -> bool:
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "workshop":
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				return true
	return false


func decide(_tick: int) -> SimCommand:
	if is_complete(_last_snapshot):
		return null

	if _plan_index >= _build_plan().size() and _road_queue.is_empty():
		return null

	if not _road_queue.is_empty():
		return _next_road_command()

	var action: Dictionary = _build_plan()[_plan_index]
	if str(action.get("type", "")) == "place_building" and _has_pending_construction():
		return null

	return _command_for_action(action)


func _command_for_action(action: Dictionary) -> SimCommand:
	var action_type: String = str(action.get("type", ""))
	match action_type:
		"roads_to":
			_queue_roads_to(int(action.get("q", 0)), int(action.get("r", 0)))
			_plan_index += 1
			_log_decision("queue_roads", "connect network")
			if _road_queue.is_empty():
				return null
			return _next_road_command()
		"set_worker_policy":
			_plan_index += 1
			_log_decision("SetWorkerPolicyCommand", "auto workers")
			return SetWorkerPolicyCommand.new(
				player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
			)
		"place_building":
			_log_decision("PlaceBuildingPlanCommand", "place %s" % str(action.get("building_type", "")))
			return PlaceBuildingPlanCommand.new(
				player_id,
				_tick_from_snapshot(),
				{
					"building_type": str(action.get("building_type", "")),
					"q": int(action.get("q", 0)),
					"r": int(action.get("r", 0)),
					"player_id": player_id,
				},
			)
	return null


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	match command.type:
		"PlaceRoadCommand":
			if not _road_queue.is_empty() and (result.success or str(result.error_code) == "ROAD_EXISTS"):
				_road_queue.pop_front()
		"PlaceBuildingPlanCommand":
			if result.success or str(result.error_code) in ["BUILDING_EXISTS", "SITE_EXISTS"]:
				_plan_index += 1


func _build_plan() -> Array:
	return [
		{"type": "set_worker_policy"},
		{"type": "roads_to", "q": 9, "r": 2},
		{"type": "place_building", "building_type": "woodcutter", "q": 9, "r": 2},
		{"type": "roads_to", "q": 7, "r": 4},
		{"type": "place_building", "building_type": "sawmill", "q": 7, "r": 4},
		{"type": "roads_to", "q": 1, "r": 7},
		{"type": "place_building", "building_type": "quarry", "q": 1, "r": 7},
		{"type": "roads_to", "q": 1, "r": 1},
		{"type": "place_building", "building_type": "coal_mine", "q": 1, "r": 1},
		{"type": "roads_to", "q": 7, "r": 1},
		{"type": "place_building", "building_type": "iron_mine", "q": 7, "r": 1},
		{"type": "roads_to", "q": 7, "r": 9},
		{"type": "place_building", "building_type": "smelter", "q": 7, "r": 9},
		{"type": "roads_to", "q": 9, "r": 6},
		{"type": "place_building", "building_type": "workshop", "q": 9, "r": 6},
	]


func _has_pending_construction() -> bool:
	for site in _last_snapshot.get("construction_sites", []):
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			return true
	return false


func _queue_roads_to(target_q: int, target_r: int) -> void:
	var hq_q: int = 0
	var hq_r: int = 0
	for building in _last_snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "headquarters":
			hq_q = int(building.get("anchor_q", building.get("q", 0)))
			hq_r = int(building.get("anchor_r", building.get("r", 0)))
			break
	var current := HexCoord.new(hq_q, hq_r)
	var target := HexCoord.new(target_q, target_r)
	var guard: int = 0
	while (current.q != target.q or current.r != target.r) and guard < 256:
		guard += 1
		var best_neighbor: HexCoord = current
		var best_dist: int = current.distance(target)
		for neighbour in current.neighbors():
			var dist: int = neighbour.distance(target)
			if dist < best_dist:
				best_dist = dist
				best_neighbor = neighbour
		if best_neighbor.q == current.q and best_neighbor.r == current.r:
			break
		_road_queue.append({
			"from_q": current.q,
			"from_r": current.r,
			"to_q": best_neighbor.q,
			"to_r": best_neighbor.r,
		})
		current = best_neighbor


func _next_road_command() -> SimCommand:
	var segment: Dictionary = _road_queue[0]
	_log_decision("PlaceRoadCommand", "connect logistics")
	return PlaceRoadCommand.new(player_id, _tick_from_snapshot(), segment)


func pop_road_if_applied(result: CommandResult) -> void:
	if _road_queue.is_empty():
		return
	if result.success or str(result.error_code) == "ROAD_EXISTS":
		_road_queue.pop_front()


func _tick_from_snapshot() -> int:
	return int(_last_snapshot.get("tick", 0))


func _log_decision(chosen: String, reason: String) -> void:
	_decision_log.append({
		"tick": _tick_from_snapshot(),
		"actor_id": actor_id,
		"legal_actions_count": _build_plan().size() - _plan_index,
		"chosen_command": chosen,
		"reason": reason,
		"expected_effect": chosen,
	})
