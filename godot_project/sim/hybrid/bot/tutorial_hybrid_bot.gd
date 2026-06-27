class_name TutorialHybridBot
extends Bot

## Deterministic bot for Hybrid MVP objective (MRP C5).

var _plan_index: int = 0
var _road_queue: Array = []
var _decision_log: Array = []
var _last_progress_tick: int = 0
var _attack_issued: bool = false


func _init(p_player_id: String = "p1") -> void:
	player_id = p_player_id
	actor_id = "tutorial_hybrid_bot_%s" % p_player_id


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_plan_index = 0
	_road_queue.clear()
	_decision_log.clear()
	_last_progress_tick = 0
	_attack_issued = false


func get_decisions() -> Array:
	return _decision_log


func is_complete(snapshot: Dictionary) -> bool:
	return bool(snapshot.get("objective", {}).get("complete", false))


func get_stall_ticks_since_progress(current_tick: int) -> int:
	return current_tick - _last_progress_tick


func decide(_tick: int) -> SimCommand:
	if is_complete(_last_snapshot):
		return null

	if not _road_queue.is_empty():
		return _next_road_command()

	if _plan_index >= _build_plan().size():
		if not _attack_issued:
			return _attack_command()
		return SetWorkerPolicyCommand.new(
			player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id},
		)

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
			if result.success or _building_or_site_exists(str(command.payload.get("building_type", ""))):
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()
		"QueueUnitCommand":
			if result.success:
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()
		"DelegatedAttackCommand":
			if result.success:
				_attack_issued = true
				_last_progress_tick = _tick_from_snapshot()
		"SetWorkerPolicyCommand":
			if result.success and _plan_index == 0:
				_plan_index += 1
				_last_progress_tick = _tick_from_snapshot()


func _build_plan() -> Array:
	if _count_units("rifle_squad") > 0:
		return []
	if _has_military_building("barracks"):
		return [
			{"type": "set_worker_policy"},
			{"type": "queue_unit", "building_type": "barracks", "unit_type": "rifle_squad"},
		]
	return [
		{"type": "set_worker_policy"},
		{"type": "roads_to", "q": 7, "r": 2},
		{"type": "place_building", "building_type": "barracks", "q": 9, "r": 2},
		{"type": "queue_unit", "building_type": "barracks", "unit_type": "rifle_squad"},
	]


func _command_for_action(action: Dictionary) -> SimCommand:
	match str(action.get("type", "")):
		"set_worker_policy":
			_log("SetWorkerPolicyCommand", "auto workers")
			return SetWorkerPolicyCommand.new(player_id, _tick_from_snapshot(), {"policy": "auto", "player_id": player_id})
		"roads_to":
			_queue_roads_to(int(action.get("q", 0)), int(action.get("r", 0)))
			_plan_index += 1
			if _road_queue.is_empty():
				return null
			return _next_road_command()
		"place_building":
			var btype: String = str(action.get("building_type", ""))
			if _building_or_site_exists(btype):
				_plan_index += 1
				return null
			_log("PlaceBuildingPlanCommand", "place %s" % btype)
			return PlaceBuildingPlanCommand.new(player_id, _tick_from_snapshot(), {
				"building_type": btype,
				"q": int(action.get("q", 0)),
				"r": int(action.get("r", 0)),
				"player_id": player_id,
			})
		"queue_unit":
			var bid: int = _building_id(str(action.get("building_type", "")))
			if bid <= 0:
				return null
			if _count_units(str(action.get("unit_type", ""))) > 0:
				_plan_index += 1
				return null
			_log("QueueUnitCommand", "queue %s" % str(action.get("unit_type", "")))
			return QueueUnitCommand.new(player_id, _tick_from_snapshot(), {
				"building_id": bid,
				"unit_type": str(action.get("unit_type", "rifle_squad")),
			})
	return null


func _attack_command() -> SimCommand:
	for building in _all_buildings():
		if str(building.get("building_type", "")) == "command_hub" and str(building.get("player_id", "")) != player_id:
			_log("DelegatedAttackCommand", "attack enemy hub")
			return DelegatedAttackCommand.new(player_id, _tick_from_snapshot(), {
				"target_q": int(building.get("anchor_q", building.get("q", 0))),
				"target_r": int(building.get("anchor_r", building.get("r", 0))),
			})
	return null


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
	var hq_q: int = 2
	var hq_r: int = 2
	for building in _last_snapshot.get("buildings", []):
		if str(building.get("building_type", "")) in ["headquarters", "command_hub"] and str(building.get("player_id", "")) == player_id:
			hq_q = int(building.get("anchor_q", building.get("q", hq_q)))
			hq_r = int(building.get("anchor_r", building.get("r", hq_r)))
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
	_log("PlaceRoadCommand", "connect logistics")
	return PlaceRoadCommand.new(player_id, _tick_from_snapshot(), segment)


func _tick_from_snapshot() -> int:
	return int(_last_snapshot.get("tick", 0))


func _log(command_type: String, reason: String) -> void:
	_decision_log.append({
		"tick": _tick_from_snapshot(),
		"actor_id": actor_id,
		"chosen_command": command_type,
		"reason": reason,
	})
