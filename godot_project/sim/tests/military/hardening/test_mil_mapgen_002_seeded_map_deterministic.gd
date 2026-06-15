extends "res://sim/tests/test_case.gd"

## MIL-MAPGEN-002: seeded skirmish map generation is deterministic.


func get_name() -> String:
	return "test_MIL_MAPGEN_002_seeded_map_deterministic"


func _run_body() -> void:
	var a: Dictionary = MilitarySkirmish.build_scenario(7, 2, 24, 24)
	var b: Dictionary = MilitarySkirmish.build_scenario(7, 2, 24, 24)
	assert_eq(JSON.stringify(a), JSON.stringify(b), "same seed yields identical scenario JSON")

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(a)
	assert_eq(state.map.width, 24, "generated map width matches request")
	assert_eq(state.map.height, 24, "generated map height matches request")

	var state_run: GameState = runner.run_ticks(state, 20)
	var h1: String = ReplayHash.compute_state_hash(state_run)
	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.create_game(MilitarySkirmish.build_scenario(7, 2, 24, 24))
	state2 = runner2.run_ticks(state2, 20)
	assert_eq(h1, ReplayHash.compute_state_hash(state2), "identical seed produces identical replay hash")
