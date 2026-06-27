class_name TutorialEconomyBot
extends Bot

## Deterministic bot for Settler MVP objective (MRP Track A).

const ACTOR: String = "tutorial_economy_bot"

var _plan_index: int = 0
var _road_queue: Array = []
var _decision_log: Array = []
var _objective_config: Dictionary = {}
var _stall_ticks: int = 0
var _last_progress_tick: int = 0


func _init() -> void:
	actor_id = ACTOR


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_plan_index = 0
	_road_queue.clear()
	_decision_log.clear()
	_objective_config = SettlerObjective.load_config("config/settler_mvp_objective.json")
	_stall_ticks = 0
	_last_progress_tick = 0


func get_decisions() -> Array:
	return _decision_log


func is_complete(snapshot: Dictionary) -> bool:
	var objective: Dictionary = snapshot.get("objective", {})
	return bool(objective.get("complete", false))


func decide(_tick: int) -> SimCommand:
	if is_complete(_last_snapshot):
		return null

	if _plan_index >= _build_plan().size() and _road_queue.is_empty():
		_stall_ticks += 1
		if _stall_ticks <= 3:
			return SetWorkerPolicyCommand.new(
				player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
			)
		return null

	if not _road_queue.is_empty():
		return _next_road_command()

	var action: Dictionary = _build_plan()[_plan_index]
	if str(action.get("type", "")) == "place_building" and _has_pending_construction():
		return null

	return _command_for_action(action)


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	match command.type:
		"PlaceRoadCommand":
			if not _road_queue.is_empty() and (result.success or str(result.error_code) == "ROAD_EXISTS"):
				_road_queue.pop_front()
				_last_progress_tick = _tick_from_snapshot()
		"PlaceBuildingPlanCommand":
			if result.success or str(result.error_code) in ["BUILDING_EXISTS", "SITE_EXISTS"]:
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()
			elif _building_or_site_exists(str(command.payload.get("building_type", ""))):
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()
		"SetWorkerPolicyCommand":
			if result.success:
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()


func get_stall_ticks_since_progress(current_tick: int) -> int:
	return current_tick - _last_progress_tick


func _command_for_action(action: Dictionary) -> SimCommand:
	var action_type: String = str(action.get("type", ""))
	match action_type:
		"roads_to":
			var from_q: int = int(action.get("from_q", -1))
			var from_r: int = int(action.get("from_r", -1))
			_queue_roads_to(int(action.get("q", 0)), int(action.get("r", 0)), from_q, from_r)
			_plan_index += 1
			_log_decision("queue_roads", "connect to (%d,%d)" % [int(action.get("q", 0)), int(action.get("r", 0))])
			if _road_queue.is_empty():
				return null
			return _next_road_command()
		"set_worker_policy":
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


func _build_plan() -> Array:
	## Settler MVP map: forest at r=3 east; rock at q=1-3,r=1 northwest.
	return [
		{"type": "set_worker_policy"},
		{"type": "roads_to", "q": 10, "r": 3, "from_q": 8, "from_r": 6},
		{"type": "place_building", "building_type": "woodcutter", "q": 10, "r": 3},
		{"type": "roads_to", "q": 11, "r": 3, "from_q": 10, "from_r": 3},
		{"type": "place_building", "building_type": "forester", "q": 11, "r": 3},
		{"type": "roads_to", "q": 7, "r": 4, "from_q": 11, "from_r": 3},
		{"type": "place_building", "building_type": "sawmill", "q": 7, "r": 4},
		{"type": "roads_to", "q": 2, "r": 1, "from_q": 7, "from_r": 4},
		{"type": "place_building", "building_type": "quarry", "q": 2, "r": 1},
	]


func _has_pending_construction() -> bool:
	for site in _last_snapshot.get("construction_sites", []):
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			return true
	return false


func _building_or_site_exists(building_type: String) -> bool:
	for building in _last_snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == building_type:
			return true
	for site in _last_snapshot.get("construction_sites", []):
		if str(site.get("building_type", "")) == building_type:
			return true
	return false


func _queue_roads_to(target_q: int, target_r: int, from_q: int = -1, from_r: int = -1) -> void:
	var start_q: int = from_q
	var start_r: int = from_r
	if start_q < 0 or start_r < 0:
		start_q = 6
		start_r = 6
		for building in _last_snapshot.get("buildings", []):
			if str(building.get("building_type", "")) in ["headquarters", "hq"]:
				start_q = int(building.get("anchor_q", building.get("q", start_q)))
				start_r = int(building.get("anchor_r", building.get("r", start_r)))
				break
	var current := HexCoord.new(start_q, start_r)
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


func _tick_from_snapshot() -> int:
	return int(_last_snapshot.get("tick", 0))


func _log_decision(chosen: String, reason: String) -> void:
	_decision_log.append({
		"tick": _tick_from_snapshot(),
		"actor_id": actor_id,
		"legal_actions_count": maxi(0, _build_plan().size() - _plan_index),
		"chosen_command": chosen,
		"reason": reason,
		"expected_effect": chosen,
	})
