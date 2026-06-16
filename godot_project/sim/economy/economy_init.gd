class_name EconomyInit
extends RefCounted

## Scenario and map bootstrap for the trial economy variant.


static func initialize(state: GameState, scenario: Dictionary, terrain_config: Dictionary) -> void:
	EconomyState.ensure_buckets(state)
	var cfg: EconomyConfig = EconomyState.get_config(state)

	var player_id: String = "p1"
	if not scenario.get("players", []).is_empty():
		player_id = str(scenario["players"][0])
	EconomyState.set_meta_value(state, "primary_player_id", player_id)

	var build_radius: int = int(scenario.get("build_radius", cfg.int_val("build_radius", 8)))
	EconomyState.set_meta_value(state, "build_radius", build_radius)

	EconomyMapRules.apply_hex_metadata_from_map(state, terrain_config)

	var hq_pos: Dictionary = scenario.get("hq_position", {})
	var hq_q: int = int(hq_pos.get("q", state.map.width / 2))
	var hq_r: int = int(hq_pos.get("r", state.map.height / 2))
	EconomyState.set_meta_value(state, "hq_position_q", hq_q)
	EconomyState.set_meta_value(state, "hq_position_r", hq_r)

	EconomyMapRules.set_ownership_radius(state, hq_q, hq_r, build_radius, player_id)

	var hq_id: int = _create_hq(state, hq_q, hq_r, player_id, cfg, scenario)
	EconomyState.set_meta_value(state, "hq_building_id", hq_id)

	_create_starting_workers(state, player_id, cfg)
	_create_starting_carriers(state, player_id, hq_id, cfg)
	_apply_starting_buildings(state, scenario, player_id, cfg)

	EconomyState.set_meta_value(state, "stall_ticks", 0)
	EconomyState.set_meta_value(state, "last_progress_tick", state.tick)
	EconomyState.set_meta_value(state, "progress_score", 0)
	EconomyState.set_meta_value(state, "blocked_routes", {})

	EconomyState.emit_event(state, "economy_initialized", {
		"hq_building_id": hq_id,
		"hq_q": hq_q,
		"hq_r": hq_r,
		"player_id": player_id,
	})


static func _create_hq(
	state: GameState,
	hq_q: int,
	hq_r: int,
	player_id: String,
	cfg: EconomyConfig,
	scenario: Dictionary,
) -> int:
	var bdef: Dictionary = cfg.get_building_def("headquarters")
	var building_id: int = state.entity_ids.allocate()
	var store_id: int = state.entity_ids.allocate()
	var capacity: int = cfg.int_val("hq_storage_capacity", 100)

	var store := EconomyState.create_empty_inventory(capacity)
	for ware_type in cfg.economy.get("starting_wares", {}):
		EconomyState.inventory_add(store, str(ware_type), int(cfg.economy["starting_wares"][ware_type]))
	for ware_type in scenario.get("starting_wares", {}):
		EconomyState.inventory_add(store, str(ware_type), int(scenario["starting_wares"][ware_type]))
	var starting_tools: int = cfg.int_val("starting_tools", 0)
	if starting_tools > 0:
		EconomyState.inventory_add(store, "tools", starting_tools)
	EconomyState.set_ware_store(state, store_id, store)

	var port_data: Dictionary = EconomyRoadSystem.create_building_port_nodes(
		state, building_id, hq_q, hq_r, player_id, cfg, "headquarters",
	)
	var building := {
		"id": building_id,
		"building_type": "headquarters",
		"anchor_q": hq_q,
		"anchor_r": hq_r,
		"player_id": player_id,
		"state": EconomyConstants.BLD_ACTIVE,
		"store_id": store_id,
		"road_node_id": int(port_data.get("road_node_id", 0)),
		"ports": port_data.get("ports", [[0, 0]]),
		"port_node_ids": port_data.get("port_node_ids", []),
		"enabled": 1,
		"priority": 0,
		"worker_type": "",
		"assigned_workers": [],
		"production_progress": 0,
		"paused_reason": "",
	}
	EconomyState.add_building(state, building)
	return building_id


static func _create_starting_workers(state: GameState, player_id: String, cfg: EconomyConfig) -> void:
	var hq_store_id: int = _hq_store_id(state)
	var worker_counts: Dictionary = cfg.economy.get("starting_workers_per_type", {})
	for worker_type in worker_counts:
		var count: int = int(worker_counts[worker_type])
		for _i in range(count):
			var worker_id: int = state.entity_ids.allocate()
			var worker := {
				"id": worker_id,
				"worker_type": str(worker_type),
				"player_id": player_id,
				"state": EconomyConstants.WORKER_IDLE,
				"home_store_id": hq_store_id,
				"assigned_building_id": 0,
				"has_tool": 0,
			}
			EconomyState.add_worker(state, worker)


static func _create_starting_carriers(state: GameState, player_id: String, hq_building_id: int, cfg: EconomyConfig) -> void:
	var hq: Dictionary = EconomyState.get_building(state, hq_building_id)
	var node_id: int = int(hq.get("road_node_id", 0))
	var carrier_workers: int = int(cfg.economy.get("starting_workers_per_type", {}).get("carrier", 2))
	for _i in range(maxi(1, carrier_workers)):
		var carrier_id: int = state.entity_ids.allocate()
		var carrier := {
			"id": carrier_id,
			"player_id": player_id,
			"state": EconomyConstants.CARRIER_IDLE,
			"current_node_id": node_id,
			"job_id": 0,
			"ware_type": "",
			"ware_amount": 0,
			"route_node_ids": [],
			"route_index": 0,
			"edge_progress": 0,
		}
		EconomyState.add_carrier(state, carrier)


static func _apply_starting_buildings(state: GameState, scenario: Dictionary, player_id: String, cfg: EconomyConfig) -> void:
	for entry in scenario.get("starting_buildings", []):
		var building_type: String = str(entry.get("type", entry.get("building_type", "")))
		var q: int = int(entry.get("q", 0))
		var r: int = int(entry.get("r", 0))
		if building_type == "":
			continue
		var check: Dictionary = EconomyMapRules.can_place_building(state, building_type, q, r, player_id)
		if not check.get("ok", false):
			continue
		_spawn_completed_building(state, building_type, q, r, player_id, cfg)


static func _spawn_completed_building(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
	cfg: EconomyConfig,
) -> int:
	var bdef: Dictionary = cfg.get_building_def(building_type)
	var building_id: int = state.entity_ids.allocate()
	var store_id: int = 0
	if int(bdef.get("storage_capacity", 0)) > 0:
		store_id = state.entity_ids.allocate()
		var cap: int = int(bdef.get("storage_capacity", cfg.int_val("building_input_capacity", 20)))
		EconomyState.set_ware_store(state, store_id, EconomyState.create_empty_inventory(cap))

	var port_data: Dictionary = EconomyRoadSystem.create_building_port_nodes(
		state, building_id, anchor_q, anchor_r, player_id, cfg, building_type,
	)
	var building := {
		"id": building_id,
		"building_type": building_type,
		"anchor_q": anchor_q,
		"anchor_r": anchor_r,
		"player_id": player_id,
		"state": EconomyConstants.BLD_ACTIVE,
		"store_id": store_id,
		"road_node_id": int(port_data.get("road_node_id", 0)),
		"ports": port_data.get("ports", [[0, 0]]),
		"port_node_ids": port_data.get("port_node_ids", []),
		"enabled": 1,
		"priority": 0,
		"worker_type": str(bdef.get("worker_type", "")),
		"assigned_workers": [],
		"production_progress": 0,
		"paused_reason": "",
	}
	EconomyState.add_building(state, building)
	return building_id


static func _hq_store_id(state: GameState) -> int:
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	return int(hq.get("store_id", 0))
