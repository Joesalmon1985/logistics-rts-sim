extends "res://sim/tests/test_case.gd"

## Settler MVP scenario boots with HQ, resources, roads, and objective state.

const SCENARIO := "scenarios/economy/settler_mvp.json"


func get_name() -> String:
	return "test_scenario_settler_mvp"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed")

	var state: GameState = runner.create_game(scenario)
	assert_eq(state.players.size(), 1, "single player")
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	assert_true(hq_id > 0, "HQ exists")

	var forest_found: bool = false
	var rock_found: bool = false
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		if tile.terrain_type == "forest" and tile.tree_density >= 20:
			forest_found = true
		if tile.deposit_type == "rock" and tile.deposit_revealed > 0:
			rock_found = true
	assert_true(forest_found, "forest cluster present")
	assert_true(rock_found, "rock deposit present")

	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	assert_true(roads.size() >= 2, "starting roads placed via EconomyRoadSystem")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_true(snapshot.has("objective"), "objective in snapshot")
	assert_false(bool(snapshot["objective"].get("complete", true)), "objective incomplete at start")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario hash stable")
