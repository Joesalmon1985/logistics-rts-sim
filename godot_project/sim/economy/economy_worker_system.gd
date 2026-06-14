class_name EconomyWorkerSystem
extends RefCounted

## HQ/storehouse worker pool, assignment, and tool gating.


static func tick(state: GameState) -> void:
	_release_workers_from_disabled_buildings(state)
	_fulfill_worker_requests(state)


static func _release_workers_from_disabled_buildings(state: GameState) -> void:
	for building in EconomyState.all_buildings(state):
		if int(building.get("enabled", 1)) > 0:
			continue
		_release_building_workers(state, building)


static func _fulfill_worker_requests(state: GameState) -> void:
	var buildings: Array = EconomyPrioritySystem.sort_buildings(EconomyState.all_buildings(state))
	for building in buildings:
		var state_name: String = str(building.get("state", ""))
		if state_name not in [
			EconomyConstants.BLD_ACTIVE,
			EconomyConstants.BLD_PAUSED_MISSING_WORKER,
		]:
			continue
		if int(building.get("enabled", 1)) <= 0:
			continue
		var worker_type: String = str(building.get("worker_type", ""))
		if worker_type == "":
			continue
		var bdef: Dictionary = EconomyState.get_config(state).get_building_def(str(building.get("building_type", "")))
		var slots: int = int(bdef.get("worker_slots", 0))
		var assigned: Array = building.get("assigned_workers", [])
		while assigned.size() < slots:
			var worker: Dictionary = _take_idle_worker(state, worker_type, building)
			if worker.is_empty():
				building["paused_reason"] = "missing_worker"
				EconomyState.emit_event(state, "worker_unavailable", {
					"building_id": int(building.get("id", 0)),
					"worker_type": worker_type,
				})
				break
			assigned.append(int(worker.get("id", 0)))
			worker["state"] = EconomyConstants.WORKER_ASSIGNED
			worker["assigned_building_id"] = int(building.get("id", 0))
			_update_worker(state, worker)
			EconomyState.emit_event(state, "worker_assigned", {
				"worker_id": int(worker.get("id", 0)),
				"building_id": int(building.get("id", 0)),
				"worker_type": worker_type,
			})
		building["assigned_workers"] = assigned
		EconomyState.add_building(state, building)


static func _take_idle_worker(state: GameState, worker_type: String, building: Dictionary) -> Dictionary:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tool_req: Dictionary = cfg.get_tool_requirement(worker_type)
	var tool_ware: String = str(tool_req.get("ware", "tools"))
	var tool_amount: int = int(tool_req.get("amount", 0))

	for worker in EconomyState.all_workers(state):
		if str(worker.get("worker_type", "")) != worker_type:
			continue
		if str(worker.get("state", "")) != EconomyConstants.WORKER_IDLE:
			continue
		if not _worker_reachable_from_building(state, worker, building):
			continue
		if tool_amount > 0:
			if not _consume_tool_for_worker(state, worker, tool_ware, tool_amount):
				EconomyState.emit_event(state, "tool_missing", {
					"worker_type": worker_type,
					"building_id": int(building.get("id", 0)),
				})
				continue
			worker["has_tool"] = 1
			EconomyState.emit_event(state, "tool_consumed", {
				"worker_id": int(worker.get("id", 0)),
				"ware": tool_ware,
				"amount": tool_amount,
			})
		return worker
	return {}


static func _worker_reachable_from_building(state: GameState, worker: Dictionary, building: Dictionary) -> bool:
	var home_store_id: int = int(worker.get("home_store_id", 0))
	var home_node_id: int = _store_node_id(state, home_store_id)
	var building_node_id: int = int(building.get("road_node_id", 0))
	if home_node_id <= 0 or building_node_id <= 0:
		return true
	return not EconomyPathfinder.find_route(state, home_node_id, building_node_id).is_empty()


static func _consume_tool_for_worker(
	state: GameState,
	worker: Dictionary,
	tool_ware: String,
	tool_amount: int,
) -> bool:
	var store_id: int = int(worker.get("home_store_id", 0))
	var store: Dictionary = EconomyState.get_ware_store(state, store_id)
	if not EconomyState.inventory_has(store, tool_ware, tool_amount):
		return false
	EconomyState.inventory_remove(store, tool_ware, tool_amount)
	EconomyState.set_ware_store(state, store_id, store)
	return true


static func _release_building_workers(state: GameState, building: Dictionary) -> void:
	for worker_id in building.get("assigned_workers", []):
		var worker: Dictionary = _find_worker(state, int(worker_id))
		if worker.is_empty():
			continue
		worker["state"] = EconomyConstants.WORKER_IDLE
		worker["assigned_building_id"] = 0
		_update_worker(state, worker)
		EconomyState.emit_event(state, "worker_released", {
			"worker_id": int(worker_id),
			"building_id": int(building.get("id", 0)),
		})
	building["assigned_workers"] = []
	EconomyState.add_building(state, building)


static func _find_worker(state: GameState, worker_id: int) -> Dictionary:
	for worker in EconomyState.all_workers(state):
		if int(worker.get("id", 0)) == worker_id:
			return worker
	return {}


static func _update_worker(state: GameState, worker: Dictionary) -> void:
	var workers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_WORKERS)
	workers[str(worker.get("id", 0))] = worker


static func _store_node_id(state: GameState, store_id: int) -> int:
	for building in EconomyState.all_buildings(state):
		if int(building.get("store_id", 0)) == store_id:
			return int(building.get("road_node_id", 0))
	return 0
