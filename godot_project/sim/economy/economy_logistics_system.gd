class_name EconomyLogisticsSystem
extends RefCounted

## Delivery jobs, ware reservation, carrier routing and movement.


static func tick(state: GameState) -> void:
	_prune_finished_jobs(state)
	_process_waiting_jobs(state)
	_forward_surplus_outputs(state)
	_move_carriers(state)
	EconomyRoadSystem.tick_congestion_decay(state)


static func _prune_finished_jobs(state: GameState) -> void:
	var jobs: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)
	var remove_keys: Array = []
	for key in jobs:
		var status: String = str(jobs[key].get("status", ""))
		if status in [EconomyConstants.JOB_DELIVERED, EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED]:
			remove_keys.append(key)
	for key in remove_keys:
		jobs.erase(key)


static func request_delivery(
	state: GameState,
	ware_type: String,
	amount: int,
	dest_entity_id: int,
	dest_entity_type: String,
	player_id: String,
	priority: int = 0,
) -> int:
	if _has_pending_job(state, dest_entity_id, ware_type):
		return 0

	var source_store_id: int = _find_source_store_for_ware(
		state,
		player_id,
		ware_type,
		_dest_node_id_for_coords(state, dest_entity_id, dest_entity_type, player_id),
	)
	if source_store_id <= 0:
		return 0
	var source_store: Dictionary = EconomyState.get_ware_store(state, source_store_id)
	var dest_store_id: int = _dest_store_id(state, dest_entity_id, dest_entity_type)
	var job_id: int = state.entity_ids.allocate()
	var job := {
		"id": job_id,
		"status": EconomyConstants.JOB_CREATED,
		"ware_type": ware_type,
		"amount": amount,
		"source_store_id": source_store_id,
		"dest_store_id": dest_store_id,
		"dest_entity_id": dest_entity_id,
		"dest_entity_type": dest_entity_type,
		"player_id": player_id,
		"priority": priority,
		"carrier_id": 0,
		"block_reason": "",
	}
	EconomyState.add_delivery_job(state, job)
	EconomyState.emit_event(state, "delivery_job_created", {
		"job_id": job_id,
		"ware_type": ware_type,
		"amount": amount,
		"dest_entity_id": dest_entity_id,
	})
	return job_id


static func request_building_input(state: GameState, building: Dictionary) -> void:
	var store_id: int = int(building.get("store_id", 0))
	if store_id <= 0:
		return
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	var recipe: Dictionary = cfg.get_recipe(str(bdef.get("recipe_id", "")))
	for ware_type in recipe.get("inputs", {}):
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		var have: int = int(store.get("wares", {}).get(ware_type, 0))
		var need: int = int(recipe["inputs"][ware_type])
		if have < need:
			request_delivery(
				state,
				str(ware_type),
				need - have,
				int(building.get("id", 0)),
				"building",
				str(building.get("player_id", "p1")),
				int(building.get("priority", 0)),
			)


static func _process_waiting_jobs(state: GameState) -> void:
	var jobs: Array = EconomyPrioritySystem.sort_delivery_jobs(EconomyState.all_delivery_jobs(state))
	for job in jobs:
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_DELIVERED, EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED]:
			continue
		if status == EconomyConstants.JOB_IN_TRANSIT:
			continue
		_advance_job(state, job)


static func _advance_job(state: GameState, job: Dictionary) -> void:
	var job_id: int = int(job.get("id", 0))
	var source_store_id: int = int(job.get("source_store_id", 0))
	var source_store: Dictionary = EconomyState.get_ware_store(state, source_store_id)
	var ware_type: String = str(job.get("ware_type", ""))
	var amount: int = int(job.get("amount", 0))

	if not EconomyState.inventory_has(source_store, ware_type, amount):
		var better_source: int = _find_source_store_for_ware(
			state,
			str(job.get("player_id", "p1")),
			ware_type,
			_dest_node_id(state, job),
		)
		if better_source > 0 and better_source != source_store_id:
			job["source_store_id"] = better_source
			source_store_id = better_source
			source_store = EconomyState.get_ware_store(state, source_store_id)
			_update_job(state, job)
		if not EconomyState.inventory_has(source_store, ware_type, amount):
			job["status"] = EconomyConstants.JOB_WAITING_WARE
			job["block_reason"] = "waiting_ware"
			_update_job(state, job)
			return

	var source_node_id: int = _store_node_id(state, source_store_id, _dest_node_id(state, job))
	var dest_node_id: int = _dest_node_id(state, job, source_node_id)
	if source_node_id > 0 and dest_node_id > 0:
		source_node_id = _store_node_id(state, source_store_id, dest_node_id)
	if source_node_id <= 0 or dest_node_id <= 0:
		job["status"] = EconomyConstants.JOB_BLOCKED
		job["block_reason"] = "missing_node"
		_update_job(state, job)
		EconomyState.emit_event(state, "delivery_job_blocked", {"job_id": job_id, "reason": "missing_node"})
		return

	var route: Array = EconomyPathfinder.find_route(
		state,
		source_node_id,
		dest_node_id,
		str(job.get("player_id", "p1")),
	)
	if route.is_empty():
		job["status"] = EconomyConstants.JOB_WAITING_ROUTE
		job["block_reason"] = "no_route"
		_update_job(state, job)
		EconomyState.emit_event(state, "delivery_job_blocked", {"job_id": job_id, "reason": "no_route"})
		return

	var carrier: Dictionary = _take_idle_carrier(state, str(job.get("player_id", "p1")))
	if carrier.is_empty():
		job["status"] = EconomyConstants.JOB_WAITING_CARRIER
		job["block_reason"] = "no_carrier"
		_update_job(state, job)
		return

	if not EconomyState.inventory_reserve(source_store, ware_type, amount):
		job["status"] = EconomyConstants.JOB_WAITING_WARE
		job["block_reason"] = "waiting_ware"
		return
	EconomyState.set_ware_store(state, source_store_id, source_store)
	EconomyState.emit_event(state, "ware_reserved", {
		"job_id": job_id,
		"ware": ware_type,
		"amount": amount,
	})

	carrier["state"] = EconomyConstants.CARRIER_ASSIGNED
	carrier["job_id"] = job_id
	carrier["ware_type"] = ware_type
	carrier["ware_amount"] = amount
	carrier["route_node_ids"] = route
	carrier["route_index"] = 0
	carrier["edge_progress"] = 0
	carrier["current_node_id"] = source_node_id
	_update_carrier(state, carrier)

	job["status"] = EconomyConstants.JOB_IN_TRANSIT
	job["carrier_id"] = int(carrier.get("id", 0))
	_update_job(state, job)
	EconomyState.emit_event(state, "delivery_job_assigned", {
		"job_id": job_id,
		"carrier_id": int(carrier.get("id", 0)),
	})


static func _move_carriers(state: GameState) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var ticks_per_edge: int = cfg.int_val("carrier_speed_ticks_per_edge", 1)
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("state", "")) not in [EconomyConstants.CARRIER_ASSIGNED, EconomyConstants.CARRIER_MOVING]:
			continue
		var route: Array = carrier.get("route_node_ids", [])
		var route_index: int = int(carrier.get("route_index", 0))
		if route_index >= route.size() - 1:
			_deliver_carrier_load(state, carrier)
			continue

		carrier["state"] = EconomyConstants.CARRIER_MOVING
		carrier["edge_progress"] = int(carrier.get("edge_progress", 0)) + 1
		if carrier["edge_progress"] < ticks_per_edge:
			_update_carrier(state, carrier)
			continue

		var from_node_id: int = int(route[route_index])
		var to_node_id: int = int(route[route_index + 1])
		var segment_id: int = EconomyRoadSystem.segment_between_nodes(state, from_node_id, to_node_id)
		if segment_id > 0:
			EconomyRoadSystem.increment_congestion(state, segment_id)

		carrier["route_index"] = route_index + 1
		carrier["current_node_id"] = to_node_id
		carrier["edge_progress"] = 0
		_update_carrier(state, carrier)
		EconomyState.emit_event(state, "ware_moved", {
			"carrier_id": int(carrier.get("id", 0)),
			"node_id": to_node_id,
			"ware": str(carrier.get("ware_type", "")),
		})

		if int(carrier["route_index"]) >= route.size() - 1:
			_deliver_carrier_load(state, carrier)


static func _deliver_carrier_load(state: GameState, carrier: Dictionary) -> void:
	var job_id: int = int(carrier.get("job_id", 0))
	var job: Dictionary = EconomyState.get_delivery_job(state, job_id)
	var ware_type: String = str(carrier.get("ware_type", ""))
	var amount: int = int(carrier.get("ware_amount", 0))
	var source_store_id: int = int(job.get("source_store_id", 0))
	var source_store: Dictionary = EconomyState.get_ware_store(state, source_store_id)

	EconomyState.inventory_unreserve(source_store, ware_type, amount)
	EconomyState.inventory_remove(source_store, ware_type, amount)
	EconomyState.set_ware_store(state, source_store_id, source_store)
	EconomyState.emit_event(state, "ware_picked_up", {
		"job_id": job_id,
		"ware": ware_type,
		"amount": amount,
	})

	var delivered: bool = _apply_delivery(state, job, ware_type, amount)
	if delivered:
		job["status"] = EconomyConstants.JOB_DELIVERED
		EconomyState.increment_player_stat(
			state,
			str(job.get("player_id", "p1")),
			"completed_deliveries_count",
		)
		EconomyState.emit_event(state, "ware_delivered", {
			"job_id": job_id,
			"ware": ware_type,
			"amount": amount,
			"dest_entity_id": int(job.get("dest_entity_id", 0)),
		})
	else:
		job["status"] = EconomyConstants.JOB_FAILED
		job["block_reason"] = "delivery_rejected"
		EconomyState.emit_event(state, "delivery_job_blocked", {"job_id": job_id, "reason": "delivery_rejected"})

	_update_job(state, job)
	carrier["state"] = EconomyConstants.CARRIER_IDLE
	carrier["job_id"] = 0
	carrier["ware_type"] = ""
	carrier["ware_amount"] = 0
	carrier["route_node_ids"] = []
	carrier["route_index"] = 0
	carrier["edge_progress"] = 0
	_update_carrier(state, carrier)


static func _apply_delivery(state: GameState, job: Dictionary, ware_type: String, amount: int) -> bool:
	var dest_type: String = str(job.get("dest_entity_type", ""))
	var dest_id: int = int(job.get("dest_entity_id", 0))
	if dest_type == "construction_site":
		return EconomyConstructionSystem.deliver_to_site(state, dest_id, ware_type, amount)
	if dest_type == "building":
		var building: Dictionary = EconomyState.get_building(state, dest_id)
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			return false
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		if not EconomyState.inventory_add(store, ware_type, amount):
			EconomyState.emit_event(state, "storage_full", {"building_id": dest_id})
			return false
		EconomyState.set_ware_store(state, store_id, store)
		return true
	return false


static func _has_pending_job(state: GameState, dest_entity_id: int, ware_type: String) -> bool:
	for job in EconomyState.all_delivery_jobs(state):
		if int(job.get("dest_entity_id", 0)) != dest_entity_id:
			continue
		if str(job.get("ware_type", "")) != ware_type:
			continue
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_DELIVERED, EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED]:
			continue
		if status == EconomyConstants.JOB_WAITING_WARE:
			var source_store: Dictionary = EconomyState.get_ware_store(
				state,
				int(job.get("source_store_id", 0)),
			)
			if not EconomyState.inventory_has(source_store, ware_type, 1):
				continue
		return true
	return false


static func _take_idle_carrier(state: GameState, player_id: String) -> Dictionary:
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("player_id", "")) != player_id:
			continue
		if str(carrier.get("state", "")) == EconomyConstants.CARRIER_IDLE:
			return carrier
	return {}


static func _hq_store_id(state: GameState, player_id: String = "") -> int:
	if player_id == "":
		player_id = str(EconomyState.get_meta_value(state, "primary_player_id", "p1"))
	return EconomyState.get_hq_store_id(state, player_id)


static func _find_source_store_for_ware(
	state: GameState,
	player_id: String,
	ware_type: String,
	dest_node_id: int,
) -> int:
	var candidates: Array = []
	var hq_store: int = _hq_store_id(state, player_id)
	if hq_store > 0:
		candidates.append(hq_store)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0 or store_id == hq_store:
			continue
		candidates.append(store_id)

	for store_id in candidates:
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		if not EconomyState.inventory_has(store, ware_type, 1):
			continue
		if dest_node_id <= 0:
			return store_id
		var source_node: int = _store_node_id(state, store_id, dest_node_id)
		if source_node <= 0:
			continue
		if not EconomyPathfinder.find_route(state, source_node, dest_node_id, player_id).is_empty():
			return store_id
	return 0


static func _dest_node_id_for_coords(
	state: GameState,
	dest_entity_id: int,
	dest_entity_type: String,
	player_id: String,
) -> int:
	if dest_entity_type == "building":
		var building: Dictionary = EconomyState.get_building(state, dest_entity_id)
		return _select_entrance_node(state, building, 0, false)
	if dest_entity_type == "construction_site":
		var site: Dictionary = EconomyState.get_construction_site(state, dest_entity_id)
		return EconomyRoadSystem.create_building_road_node(
			state,
			0,
			int(site.get("anchor_q", 0)),
			int(site.get("anchor_r", 0)),
			player_id,
		)
	return 0


static func _dest_store_id(state: GameState, dest_entity_id: int, dest_entity_type: String) -> int:
	if dest_entity_type == "building":
		return int(EconomyState.get_building(state, dest_entity_id).get("store_id", 0))
	if dest_entity_type == "construction_site":
		return 0
	return 0


static func _select_entrance_node(
	state: GameState,
	building: Dictionary,
	peer_node_id: int,
	as_source: bool,
) -> int:
	var port_nodes: Array = building.get("port_node_ids", [])
	var fallback: int = int(building.get("road_node_id", 0))
	if port_nodes.is_empty():
		return fallback
	if peer_node_id <= 0:
		return fallback
	var best_id: int = 0
	var best_len: int = 999999
	for port in port_nodes:
		if typeof(port) != TYPE_DICTIONARY:
			continue
		var node_id: int = int(port.get("node_id", 0))
		if node_id <= 0:
			continue
		var route: Array = []
		var route_player: String = str(building.get("player_id", "p1"))
		if as_source:
			route = EconomyPathfinder.find_route(state, node_id, peer_node_id, route_player)
		else:
			route = EconomyPathfinder.find_route(state, peer_node_id, node_id, route_player)
		if not route.is_empty() and route.size() < best_len:
			best_len = route.size()
			best_id = node_id
	if best_id > 0:
		return best_id
	return fallback


static func _store_node_id(state: GameState, store_id: int, toward_node_id: int = 0) -> int:
	for building in EconomyState.all_buildings(state):
		if int(building.get("store_id", 0)) == store_id:
			return _select_entrance_node(state, building, toward_node_id, true)
	return 0


static func _dest_node_id(state: GameState, job: Dictionary, source_node_hint: int = 0) -> int:
	var dest_type: String = str(job.get("dest_entity_type", ""))
	var dest_id: int = int(job.get("dest_entity_id", 0))
	if dest_type == "building":
		var building: Dictionary = EconomyState.get_building(state, dest_id)
		return _select_entrance_node(state, building, source_node_hint, false)
	if dest_type == "construction_site":
		var site: Dictionary = EconomyState.get_construction_site(state, dest_id)
		return EconomyRoadSystem.create_building_road_node(
			state,
			0,
			int(site.get("anchor_q", 0)),
			int(site.get("anchor_r", 0)),
			str(job.get("player_id", "p1")),
		)
	return 0


static func _update_job(state: GameState, job: Dictionary) -> void:
	var jobs: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_DELIVERY_JOBS)
	jobs[str(job.get("id", 0))] = job


static func _update_carrier(state: GameState, carrier: Dictionary) -> void:
	var carriers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CARRIERS)
	carriers[str(carrier.get("id", 0))] = carrier


static func _forward_surplus_outputs(state: GameState) -> void:
	for building in EconomyState.all_buildings(state):
		var player_id: String = str(building.get("player_id", "p1"))
		var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
		var building_id: int = int(building.get("id", 0))
		if building_id == hq_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		var state_name: String = str(building.get("state", ""))
		var nearly_full: bool = EconomyState.inventory_free(store) <= 2
		var output_blocked: bool = state_name == EconomyConstants.BLD_PAUSED_NO_OUTPUT
		if not nearly_full and not output_blocked:
			continue
		for ware_type in store.get("wares", {}):
			var available: int = int(store["wares"][ware_type]) - int(store.get("reserved", {}).get(ware_type, 0))
			if available <= 0:
				continue
			if _has_outbound_job_from_store(state, store_id, str(ware_type)):
				continue
			var amount: int = mini(available, 3)
			if _forward_ware_to_consumers(
				state,
				str(ware_type),
				amount,
				str(building.get("player_id", "p1")),
				building_id,
			):
				continue
			request_delivery(
				state,
				str(ware_type),
				amount,
				hq_id,
				"building",
				str(building.get("player_id", "p1")),
				1,
			)


static func _has_outbound_job_from_store(state: GameState, store_id: int, ware_type: String) -> bool:
	for job in EconomyState.all_delivery_jobs(state):
		if int(job.get("source_store_id", 0)) != store_id:
			continue
		if str(job.get("ware_type", "")) != ware_type:
			continue
		var status: String = str(job.get("status", ""))
		if status not in [EconomyConstants.JOB_DELIVERED, EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED]:
			return true
	return false


static func _forward_ware_to_consumers(
	state: GameState,
	ware_type: String,
	amount: int,
	player_id: String,
	source_building_id: int,
) -> bool:
	for site in EconomyPrioritySystem.sort_construction_sites(EconomyState.all_construction_sites(state)):
		if str(site.get("state", "")) not in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			continue
		if str(site.get("player_id", "")) != player_id:
			continue
		var needed: int = int(site.get("required", {}).get(ware_type, 0)) - int(site.get("delivered", {}).get(ware_type, 0))
		if needed <= 0:
			continue
		if request_delivery(
			state,
			ware_type,
			mini(amount, needed),
			int(site.get("id", 0)),
			"construction_site",
			player_id,
			int(site.get("priority", 0)) + 1,
		) > 0:
			return true

	for consumer in EconomyPrioritySystem.sort_buildings(EconomyState.all_buildings(state)):
		if int(consumer.get("id", 0)) == source_building_id:
			continue
		if str(consumer.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(consumer.get("store_id", 0))
		if store_id <= 0:
			continue
		var cfg: EconomyConfig = EconomyState.get_config(state)
		var bdef: Dictionary = cfg.get_building_def(str(consumer.get("building_type", "")))
		var recipe: Dictionary = cfg.get_recipe(str(bdef.get("recipe_id", "")))
		if not recipe.get("inputs", {}).has(ware_type):
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		var have: int = int(store.get("wares", {}).get(ware_type, 0))
		var need: int = int(recipe["inputs"][ware_type])
		if have >= need:
			continue
		if request_delivery(
			state,
			ware_type,
			mini(amount, need - have),
			int(consumer.get("id", 0)),
			"building",
			player_id,
			int(consumer.get("priority", 0)) + 1,
		) > 0:
			return true
	return false
