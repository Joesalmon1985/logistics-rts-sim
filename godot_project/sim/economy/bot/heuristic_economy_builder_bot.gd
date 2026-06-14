class_name HeuristicEconomyBuilderBot
extends RefCounted

## Deterministic scripted economy planner for tiny_success_map (ECON-BOT-001 / ECON-SMOKE-005).

const ACTOR_ID: String = "heuristic_economy_builder"

var _player_id: String = "p1"
var _plan_index: int = 0
var _road_queue: Array = []
var _decisions: Array = []
var _validator: CommandValidator


func _init() -> void:
	_validator = CommandValidator.new(CommandRegistry.create_default())


func reset(player_id: String = "p1") -> void:
	_player_id = player_id
	_plan_index = 0
	_road_queue.clear()
	_decisions.clear()


func get_decisions() -> Array:
	return _decisions


func has_workshop_complete(state: GameState) -> bool:
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == "workshop":
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				return true
	return false


func tick_act(state: GameState, _runner: SimulationTickRunner) -> bool:
	if has_workshop_complete(state):
		return false

	if _plan_index >= _build_plan().size():
		return false

	if not _road_queue.is_empty():
		return _execute_road(state)

	var action: Dictionary = _build_plan()[_plan_index]
	if str(action.get("type", "")) == "place_building" and _has_pending_construction(state):
		return false

	return _execute_action(state, action)


func _execute_action(state: GameState, action: Dictionary) -> bool:
	var action_type: String = str(action.get("type", ""))
	var legal_count: int = _build_plan().size() - _plan_index
	var reason: String = ""
	var expected: String = ""

	match action_type:
		"roads_to":
			_queue_roads_to(state, int(action.get("q", 0)), int(action.get("r", 0)))
			_plan_index += 1
			_log_decision(state, legal_count, "queue_roads", "connect network", "roads queued")
			return not _road_queue.is_empty()
		"set_worker_policy":
			var policy_cmd := SetWorkerPolicyCommand.new(
				_player_id, state.tick, {"policy": "auto", "player_id": _player_id},
			)
			var policy_result: CommandResult = _validator.validate_and_apply(state, policy_cmd)
			_log_decision(state, legal_count, policy_cmd.type, "auto workers", "policy set")
			if policy_result.success:
				_plan_index += 1
			return true
		"place_building":
			var cmd := PlaceBuildingPlanCommand.new(
				_player_id,
				state.tick,
				{
					"building_type": str(action.get("building_type", "")),
					"q": int(action.get("q", 0)),
					"r": int(action.get("r", 0)),
					"player_id": _player_id,
				},
			)
			reason = "place %s" % str(action.get("building_type", ""))
			expected = "construction site"
			var result: CommandResult = _validator.validate_and_apply(state, cmd)
			_log_decision(state, legal_count, cmd.type, reason, expected if result.success else result.error_code)
			if result.success:
				_plan_index += 1
			elif str(result.error_code) in ["BUILDING_EXISTS", "SITE_EXISTS"]:
				_plan_index += 1
			return true
	return false


func _build_plan() -> Array:
	## Minimal logistics chain for tiny_success_map; HQ starting rations feed mines.
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


func _has_pending_construction(state: GameState) -> bool:
	for site in EconomyState.all_construction_sites(state):
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			return true
	return false


func _has_active_building(state: GameState, building_type: String) -> bool:
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				return true
	return false


func _has_building_or_site(state: GameState, building_type: String) -> bool:
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return true
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("building_type", "")) == building_type:
			return true
	return false


func _queue_roads_to(state: GameState, target_q: int, target_r: int) -> void:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
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


func _execute_road(state: GameState) -> bool:
	var segment: Dictionary = _road_queue[0]
	var cmd := PlaceRoadCommand.new(_player_id, state.tick, segment)
	var result: CommandResult = _validator.validate_and_apply(state, cmd)
	if result.success or str(result.error_code) == "ROAD_EXISTS":
		_road_queue.pop_front()
	_log_decision(
		state,
		1,
		"PlaceRoadCommand",
		"connect logistics",
		"road" if result.success else str(result.error_code),
	)
	return true


func _log_decision(
	state: GameState,
	legal_count: int,
	chosen: String,
	reason: String,
	expected_effect: String,
) -> void:
	_decisions.append({
		"tick": state.tick,
		"actor_id": ACTOR_ID,
		"legal_actions_count": legal_count,
		"chosen_command": chosen,
		"reason": reason,
		"expected_effect": expected_effect,
	})
