class_name ScriptedGodStrategyBot
extends Bot

## Spawn-relative scripted bot for God-Strategy MVP A — legal placement queries only.

var _plan_index: int = 0
var _road_queue: Array = []
var _attack_issued: bool = false
var _phase: String = "init"
var _last_rejection_reason: String = ""
var _stall_ticks_in_phase: int = 0
var _commands_rejected_count: int = 0
var _last_intent: String = ""
var _last_target: Dictionary = {}
var _cached_placements: Dictionary = {}
var _bound_state: GameState = null


func _init(p_player_id: String = "p1") -> void:
	player_id = p_player_id
	actor_id = "scripted_god_strategy_bot_%s" % p_player_id


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_plan_index = 0
	_road_queue.clear()
	_attack_issued = false
	_phase = "init"
	_last_rejection_reason = ""
	_stall_ticks_in_phase = 0
	_commands_rejected_count = 0
	_last_intent = ""
	_last_target = {}
	_cached_placements.clear()
	_bound_state = null


func get_last_telemetry() -> Dictionary:
	return {
		"player_id": player_id,
		"phase": _phase,
		"intent": _last_intent,
		"last_rejection_reason": _last_rejection_reason,
		"stall_ticks_in_phase": _stall_ticks_in_phase,
		"commands_rejected_count": _commands_rejected_count,
		"target_q": int(_last_target.get("q", -1)),
		"target_r": int(_last_target.get("r", -1)),
		"plan_index": _plan_index,
	}


func decide(_tick: int) -> SimCommand:
	_stall_ticks_in_phase += 1
	if not _road_queue.is_empty():
		_phase = "roads"
		return _next_road_command()

	if _count_units("rifle_squad") > 0:
		if not _attack_issued:
			_phase = "attack"
			return _attack_command()
		_phase = "hold"
		return SetWorkerPolicyCommand.new(
			player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
		)

	if _plan_index >= _build_plan().size():
		_phase = "wait_production"
		return SetWorkerPolicyCommand.new(
			player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
		)

	var action: Dictionary = _build_plan()[_plan_index]
	if str(action.get("type", "")) == "place_building" and _has_pending_construction():
		_phase = "wait_construction"
		return null
	return _command_for_action(action)


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	if not result.success:
		_commands_rejected_count += 1
		_last_rejection_reason = str(result.error_code)
		if command.type == "PlaceBuildingPlanCommand":
			var btype: String = str(command.payload.get("building_type", ""))
			_cached_placements.erase(btype)
		return

	_last_rejection_reason = ""
	_stall_ticks_in_phase = 0
	match command.type:
		"PlaceRoadCommand":
			if not _road_queue.is_empty() and (
				result.success
				or str(result.error_code) in ["ROAD_EXISTS", "NOT_OWNER", "TERRAIN_BLOCKED", "OUT_OF_BOUNDS"]
			):
				_road_queue.pop_front()
		"PlaceBuildingPlanCommand":
			if result.success or _building_or_site_exists(str(command.payload.get("building_type", ""))):
				_plan_index += 1
		"QueueUnitCommand":
			if result.success:
				_plan_index += 1
		"DelegatedAttackCommand":
			if result.success:
				_attack_issued = true
		"SetWorkerPolicyCommand":
			if result.success and _plan_index == 0:
				_plan_index += 1


func _build_plan() -> Array:
	if _has_military_building("barracks"):
		return [
			{"type": "set_worker_policy"},
			{"type": "queue_unit", "building_type": "barracks", "unit_type": "rifle_squad"},
		]
	if _has_economy_building("sawmill"):
		return [
			{"type": "set_worker_policy"},
			{"type": "place_building", "building_type": "barracks"},
		]
	if _has_economy_building("woodcutter"):
		return [
			{"type": "set_worker_policy"},
			{"type": "place_building", "building_type": "sawmill"},
		]
	return [
		{"type": "set_worker_policy"},
		{"type": "place_building", "building_type": "woodcutter"},
		{"type": "place_building", "building_type": "sawmill"},
		{"type": "place_building", "building_type": "barracks"},
		{"type": "queue_unit", "building_type": "barracks", "unit_type": "rifle_squad"},
	]


func _command_for_action(action: Dictionary) -> SimCommand:
	match str(action.get("type", "")):
		"set_worker_policy":
			_phase = "worker_policy"
			_last_intent = "set_worker_policy"
			return SetWorkerPolicyCommand.new(
				player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
			)
		"place_building":
			var btype: String = str(action.get("building_type", ""))
			if _building_or_site_exists(btype):
				_plan_index += 1
				return null
			var placement: Vector2i = _legal_placement(btype)
			if placement.x < 0:
				_phase = "place_%s" % btype
				_last_intent = "place_%s_blocked" % btype
				_last_rejection_reason = "NO_LEGAL_SITE"
				return null
			_last_target = {"q": placement.x, "r": placement.y}
			_phase = "place_%s" % btype
			_last_intent = "place_%s" % btype
			_queue_roads_to(placement.x, placement.y)
			if not _road_queue.is_empty():
				return _next_road_command()
			return PlaceBuildingPlanCommand.new(player_id, _tick_from_snapshot(), {
				"building_type": btype,
				"q": placement.x,
				"r": placement.y,
				"player_id": player_id,
			})
		"queue_unit":
			var bid: int = _building_id(str(action.get("building_type", "")))
			if bid <= 0:
				return null
			if _count_units(str(action.get("unit_type", ""))) > 0:
				_plan_index += 1
				return null
			_phase = "queue_unit"
			_last_intent = "queue_%s" % str(action.get("unit_type", ""))
			return QueueUnitCommand.new(player_id, _tick_from_snapshot(), {
				"building_id": bid,
				"unit_type": str(action.get("unit_type", "rifle_squad")),
			})
	return null


func _legal_placement(building_type: String) -> Vector2i:
	if _bound_state == null:
		return Vector2i(-1, -1)
	if _cached_placements.has(building_type):
		var cached: Dictionary = _cached_placements[building_type]
		return Vector2i(int(cached.get("q", -1)), int(cached.get("r", -1)))
	var found: Vector2i = GodStrategyMapViability.find_legal_hex(_runtime_state(), player_id, building_type)
	if found.x >= 0:
		_cached_placements[building_type] = {"q": found.x, "r": found.y}
	return found


func bind_state(state: GameState) -> void:
	_bound_state = state


func _runtime_state() -> GameState:
	return _bound_state


func _attack_command() -> SimCommand:
	var nearest: Dictionary = {}
	var nearest_dist: int = 999999
	var hq: Vector2i = _hq_position()
	for building in _all_buildings():
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("player_id", "")) == player_id:
			continue
		var tq: int = int(building.get("anchor_q", building.get("q", 0)))
		var tr: int = int(building.get("anchor_r", building.get("r", 0)))
		var dist: int = HexCoord.new(hq.x, hq.y).distance(HexCoord.new(tq, tr))
		if dist < nearest_dist:
			nearest_dist = dist
			nearest = building
	if nearest.is_empty():
		return null
	_last_intent = "delegated_attack"
	_last_target = {
		"q": int(nearest.get("anchor_q", nearest.get("q", 0))),
		"r": int(nearest.get("anchor_r", nearest.get("r", 0))),
	}
	return DelegatedAttackCommand.new(player_id, _tick_from_snapshot(), {
		"target_q": int(_last_target.get("q", 0)),
		"target_r": int(_last_target.get("r", 0)),
	})


func _hq_position() -> Vector2i:
	for building in _last_snapshot.get("buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"] and str(building.get("player_id", "")) == player_id:
			return Vector2i(
				int(building.get("anchor_q", building.get("q", 0))),
				int(building.get("anchor_r", building.get("r", 0))),
			)
	return Vector2i(0, 0)


func _all_buildings() -> Array:
	var merged: Array = []
	for b in _last_snapshot.get("buildings", []):
		merged.append(b)
	for b in _last_snapshot.get("military_buildings", []):
		merged.append(b)
	return merged


func _building_id(building_type: String) -> int:
	for building in _all_buildings():
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	return 0


func _count_units(unit_type: String) -> int:
	var count: int = 0
	for unit in _last_snapshot.get("units", []):
		if str(unit.get("unit_type", "")) == unit_type and str(unit.get("player_id", "")) == player_id:
			count += 1
	return count


func _has_military_building(building_type: String) -> bool:
	for b in _all_buildings():
		if str(b.get("building_type", "")) == building_type and str(b.get("player_id", "")) == player_id:
			return true
	return false


func _has_economy_building(building_type: String) -> bool:
	for b in _last_snapshot.get("buildings", []):
		if str(b.get("building_type", "")) == building_type and str(b.get("player_id", "")) == player_id:
			return true
	return false


func _building_or_site_exists(building_type: String) -> bool:
	for b in _last_snapshot.get("buildings", []):
		if str(b.get("building_type", "")) == building_type and str(b.get("player_id", "")) == player_id:
			return true
	for s in _last_snapshot.get("construction_sites", []):
		if str(s.get("building_type", "")) == building_type and str(s.get("player_id", "")) == player_id:
			return true
	return false


func _has_pending_construction() -> bool:
	for s in _last_snapshot.get("construction_sites", []):
		if str(s.get("player_id", "")) == player_id:
			return true
	return false


func _queue_roads_to(target_q: int, target_r: int) -> void:
	var hq: Vector2i = _hq_position()
	var current := HexCoord.new(hq.x, hq.y)
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
	_last_intent = "place_road"
	return PlaceRoadCommand.new(player_id, _tick_from_snapshot(), segment)


func _tick_from_snapshot() -> int:
	return int(_last_snapshot.get("tick", 0))
