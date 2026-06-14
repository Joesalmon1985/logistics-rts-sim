extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_STOR_001_storage_full_blocks_input"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 43,
		"map_width": 4,
		"map_height": 4,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var store := EconomyState.create_empty_inventory(2)
	assert_true(EconomyState.inventory_add(store, "timber", 1), "first unit must fit")
	assert_true(EconomyState.inventory_add(store, "planks", 1), "second unit must fit")
	assert_false(EconomyState.inventory_add(store, "stone", 1), "full store must reject additional input")
	assert_eq(EconomyState.inventory_free(store), 0)

	var store_id: int = state.entity_ids.allocate()
	EconomyState.set_ware_store(state, store_id, store)

	var building_id: int = state.entity_ids.allocate()
	var building := {
		"id": building_id,
		"building_type": "sawmill",
		"anchor_q": 0,
		"anchor_r": 0,
		"player_id": "p1",
		"state": EconomyConstants.BLD_ACTIVE,
		"store_id": store_id,
		"road_node_id": 0,
		"enabled": 1,
		"priority": 0,
		"worker_type": "carpenter",
		"assigned_workers": [999],
		"production_progress": 2,
		"paused_reason": "",
	}
	EconomyState.add_building(state, building)

	EconomyProductionSystem.tick(state)
	var updated: Dictionary = EconomyState.get_building(state, building_id)
	assert_eq(str(updated.get("state", "")), EconomyConstants.BLD_PAUSED_NO_OUTPUT)
	assert_eq(str(updated.get("paused_reason", "")), "output_blocked")

	var blocked_event: bool = false
	for event in state.event_log.events:
		if str(event.type) == "output_blocked":
			blocked_event = true
			break
	assert_true(blocked_event, "output_blocked event expected when storage is full")
