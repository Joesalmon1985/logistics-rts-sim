class_name EconomyInit
extends RefCounted

## Scenario and map bootstrap for the trial economy variant.


static func initialize(state: GameState, scenario: Dictionary, terrain_config: Dictionary) -> void:
	EconomyState.ensure_buckets(state)
	var cfg: EconomyConfig = EconomyState.get_config(state)

	var primary_player_id: String = "p1"
	if not scenario.get("players", []).is_empty():
		primary_player_id = str(scenario["players"][0])
	EconomyState.set_meta_value(state, "primary_player_id", primary_player_id)

	var build_radius: int = int(scenario.get("build_radius", cfg.int_val("build_radius", 8)))
	EconomyState.set_meta_value(state, "build_radius", build_radius)

	EconomyMapRules.apply_hex_metadata_from_map(state, terrain_config)

	var spawns: Array = scenario.get("spawns", [])
	var hq_positions: Dictionary = {}
	var hq_building_ids: Dictionary = {}

	if spawns.is_empty():
		var player_id: String = primary_player_id
		var hq_pos: Dictionary = scenario.get("hq_position", {})
		var hq_q: int = int(hq_pos.get("q", state.map.width / 2))
		var hq_r: int = int(hq_pos.get("r", state.map.height / 2))
		hq_positions[player_id] = {"q": hq_q, "r": hq_r}
		EconomyMapRules.set_ownership_radius(state, hq_q, hq_r, build_radius, player_id)
		var hq_id: int = _create_hq(state, hq_q, hq_r, player_id, cfg, scenario)
		hq_building_ids[player_id] = hq_id
		EconomyState.set_meta_value(state, "hq_building_ids", hq_building_ids)
		EconomyState.set_meta_value(state, "hq_positions", hq_positions)
		_create_starting_workers(state, player_id, cfg, scenario, {})
		_create_starting_carriers(state, player_id, hq_id, cfg, {})
		_apply_starting_buildings(state, scenario, player_id, cfg)
	else:
		for spawn in spawns:
			var pid: String = str(spawn.get("player_id", "p1"))
			var hq_q2: int = int(spawn.get("q", state.map.width / 2))
			var hq_r2: int = int(spawn.get("r", state.map.height / 2))
			hq_positions[pid] = {"q": hq_q2, "r": hq_r2}
			EconomyMapRules.set_ownership_radius(state, hq_q2, hq_r2, build_radius, pid)
			var spawn_scenario: Dictionary = _merge_spawn_scenario(scenario, spawn)
			var hq_id2: int = _create_hq(state, hq_q2, hq_r2, pid, cfg, spawn_scenario)
			hq_building_ids[pid] = hq_id2
			EconomyState.set_meta_value(state, "hq_building_ids", hq_building_ids)
			EconomyState.set_meta_value(state, "hq_positions", hq_positions)
			_create_starting_workers(state, pid, cfg, scenario, spawn)
			_create_starting_carriers(state, pid, hq_id2, cfg, spawn)
			_apply_starting_buildings(state, spawn_scenario, pid, cfg)

	EconomyState.set_meta_value(state, "hq_positions", hq_positions)
	EconomyState.set_meta_value(state, "hq_building_ids", hq_building_ids)

	var primary_hq: Dictionary = hq_positions.get(primary_player_id, {"q": 0, "r": 0})
	EconomyState.set_meta_value(state, "hq_position_q", int(primary_hq.get("q", 0)))
	EconomyState.set_meta_value(state, "hq_position_r", int(primary_hq.get("r", 0)))
	EconomyState.set_meta_value(state, "hq_building_id", int(hq_building_ids.get(primary_player_id, 0)))

	EconomyState.set_meta_value(state, "stall_ticks", 0)
	EconomyState.set_meta_value(state, "last_progress_tick", state.tick)
	EconomyState.set_meta_value(state, "progress_score", 0)
	EconomyState.set_meta_value(state, "blocked_routes", {})
	EconomyState.set_meta_value(state, "player_economy_stats", {})
	EconomyState.set_meta_value(state, "player_score_history", {})
	EconomyState.set_meta_value(
		state,
		"stall_ticks_threshold",
		int(scenario.get("stall_ticks_threshold", cfg.int_val("stall_ticks_threshold", 200))),
	)

	EconomyState.emit_event(state, "economy_initialized", {
		"player_count": hq_building_ids.size(),
		"hq_building_ids": hq_building_ids,
		"player_id": primary_player_id,
	})

	_store_objective_config(state, scenario)
	_apply_starting_roads(state, scenario, primary_player_id)


static func _merge_spawn_scenario(scenario: Dictionary, spawn: Dictionary) -> Dictionary:
	var merged: Dictionary = scenario.duplicate(true)
	if spawn.has("starting_wares"):
		merged["starting_wares"] = spawn["starting_wares"]
	if spawn.has("starting_buildings"):
		merged["starting_buildings"] = spawn["starting_buildings"]
	return merged


static func _create_hq(
	state: GameState,
	hq_q: int,
	hq_r: int,
	player_id: String,
	cfg: EconomyConfig,
	scenario: Dictionary,
) -> int:
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


static func _create_starting_workers(
	state: GameState,
	player_id: String,
	cfg: EconomyConfig,
	scenario: Dictionary,
	spawn: Dictionary,
) -> void:
	var hq_store_id: int = EconomyState.get_hq_store_id(state, player_id)
	var worker_counts: Dictionary = spawn.get(
		"starting_workers",
		scenario.get("starting_workers", cfg.economy.get("starting_workers_per_type", {})),
	)
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


static func _create_starting_carriers(
	state: GameState,
	player_id: String,
	hq_building_id: int,
	cfg: EconomyConfig,
	spawn: Dictionary,
) -> void:
	var hq: Dictionary = EconomyState.get_building(state, hq_building_id)
	var node_id: int = int(hq.get("road_node_id", 0))
	var default_counts: Dictionary = cfg.economy.get("starting_workers_per_type", {})
	var carrier_workers: int = int(spawn.get("starting_carriers", default_counts.get("carrier", 2)))
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
		var building_type: String = ""
		var q: int = -1
		var r: int = -1
		if typeof(entry) == TYPE_DICTIONARY:
			building_type = str(entry.get("type", entry.get("building_type", "")))
			if entry.has("q"):
				q = int(entry.get("q", 0))
			if entry.has("r"):
				r = int(entry.get("r", 0))
		elif typeof(entry) == TYPE_STRING:
			building_type = str(entry)
		if building_type == "":
			continue
		if q < 0 or r < 0:
			continue
		var check: Dictionary = EconomyMapRules.can_place_building(state, building_type, q, r, player_id)
		if not check.get("ok", false):
			continue
		_spawn_completed_building(state, building_type, q, r, player_id, cfg)


static func spawn_completed_building(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
	cfg: EconomyConfig,
) -> int:
	return _spawn_completed_building(state, building_type, anchor_q, anchor_r, player_id, cfg)


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


static func _store_objective_config(state: GameState, scenario: Dictionary) -> void:
	var objective_path: String = str(scenario.get("objective_config", ""))
	if objective_path != "":
		EconomyState.set_meta_value(state, "objective_config_path", objective_path)


static func _apply_starting_roads(state: GameState, scenario: Dictionary, player_id: String) -> void:
	for segment in scenario.get("starting_roads", []):
		if typeof(segment) != TYPE_DICTIONARY:
			continue
		var from_q: int = int(segment.get("from_q", -1))
		var from_r: int = int(segment.get("from_r", -1))
		var to_q: int = int(segment.get("to_q", -1))
		var to_r: int = int(segment.get("to_r", -1))
		if from_q < 0 or to_q < 0:
			continue
		EconomyRoadSystem.place_road(
			state,
			HexCoord.new(from_q, from_r),
			HexCoord.new(to_q, to_r),
			player_id,
		)
