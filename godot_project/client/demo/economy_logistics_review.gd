class_name EconomyLogisticsReview
extends RefCounted

## Deterministic economy states for RUN_02.7 human visual review (snapshot-authoritative only).


static func build_production_chain_t0() -> GameState:
	return EconomyDemo.build_production_chain()


static func build_production_chain_after(ticks: int) -> GameState:
	var runner := SimulationTickRunner.new()
	return runner.run_ticks(EconomyDemo.build_production_chain(), ticks)


static func build_build_tutorial_with_site() -> GameState:
	var state: GameState = EconomyDemo.build_build_tutorial()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": 6,
		"r": 2,
		"player_id": "p1",
	})
	validator.validate_and_apply(state, cmd)
	return state


static func build_smoke_006_at(ticks: int) -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 607,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 9, 9)
	_connect_roads(state, 2, 2, 6, 3)
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": 6,
		"r": 3,
		"player_id": "p1",
	})
	validator.validate_and_apply(state, cmd)
	return runner.run_ticks(state, ticks)


static func build_tool_shortage() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 114,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	hq_store["wares"] = {}
	hq_store["reserved"] = {}
	EconomyState.set_ware_store(state, hq_store_id, hq_store)
	_flatten_grass(state, 1, 1, 5, 2)
	_connect_roads(state, 1, 1, 5, 2)
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = 80
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(5, 2), tile)
	EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", EconomyState.get_config(state),
	)
	return runner.run_ticks(state, 4)


static func build_brick_kiln_active() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 127,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
		"starting_wares": {"stone": 4, "coal": 4, "tools": 3},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 1, 1, 7, 4)
	_connect_roads(state, 1, 1, 5, 2)
	EconomyInit._spawn_completed_building(
		state, "brick_kiln", 5, 2, "p1", EconomyState.get_config(state),
	)
	var building: Dictionary = EconomyState.get_building(state, EconomyState.all_buildings(state).back().get("id", 0))
	if building.is_empty():
		for b in EconomyState.all_buildings(state):
			if str(b.get("building_type", "")) == "brick_kiln":
				building = b
				break
	var store: Dictionary = EconomyState.get_ware_store(state, int(building.get("store_id", 0)))
	EconomyState.inventory_add(store, "stone", 2)
	EconomyState.inventory_add(store, "coal", 2)
	EconomyState.set_ware_store(state, int(building.get("store_id", 0)), store)
	return runner.run_ticks(state, 15)


static func build_blocked_no_route() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 402,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 1, 1, 6, 3)
	var isolated_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 6, 3, "p1", EconomyState.get_config(state),
	)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)
	var job_id: int = state.entity_ids.allocate()
	EconomyState.add_delivery_job(state, {
		"id": job_id,
		"status": EconomyConstants.JOB_CREATED,
		"ware_type": "timber",
		"amount": 1,
		"source_store_id": hq_store_id,
		"dest_store_id": int(EconomyState.get_building(state, isolated_id).get("store_id", 0)),
		"dest_entity_id": isolated_id,
		"dest_entity_type": "building",
		"player_id": "p1",
		"priority": 0,
		"carrier_id": 0,
		"block_reason": "",
	})
	return runner.tick(state)


static func build_no_carrier_wait() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 403,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
		"starting_workers_per_type": {"carrier": 0},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 1, 1, 6, 3)
	_connect_roads(state, 1, 1, 5, 2)
	var carriers: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_CARRIERS)
	for key in carriers.keys():
		carriers.erase(key)
	var building_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 2, "p1", EconomyState.get_config(state),
	)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq_store_id: int = int(EconomyState.get_building(state, hq_id).get("store_id", 0))
	var hq_store: Dictionary = EconomyState.get_ware_store(state, hq_store_id)
	EconomyState.inventory_add(hq_store, "timber", 5)
	EconomyState.set_ware_store(state, hq_store_id, hq_store)
	EconomyLogisticsSystem.request_delivery(state, "timber", 1, building_id, "building", "p1", 0)
	return runner.tick(state)


static func _flatten_grass(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


static func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	var current := HexCoord.new(from_q, from_r)
	var target := HexCoord.new(to_q, to_r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
