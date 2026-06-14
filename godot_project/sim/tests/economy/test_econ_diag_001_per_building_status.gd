extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_DIAG_001_per_building_status"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 97,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_set_forest_tile(state, 4, 2, 80)
	EconomyInit._spawn_completed_building(state, "woodcutter", 4, 2, "p1", EconomyState.get_config(state))
	state = runner.run_ticks(state, 1)

	assert_has_keys(state.diagnostics, ["production"], "diagnostics must include production section")
	var production: Array = state.diagnostics.get("production", [])
	assert_true(production.size() >= 2, "diagnostics must list each building")

	var saw_status: bool = false
	for record in production:
		if str(record.get("building_type", "")) == "headquarters":
			assert_has_keys(record, ["building_id", "building_type", "state", "paused_reason", "workers"])
			saw_status = true
	assert_true(saw_status, "HQ building status must appear in diagnostics.production")


func _set_forest_tile(state: GameState, q: int, r: int, density: int) -> void:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = density
	tile.deposit_type = "forest"
	state.map.set_tile(HexCoord.new(q, r), tile)
