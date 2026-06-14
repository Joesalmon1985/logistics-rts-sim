extends "res://sim/tests/test_case.gd"

## ECON-SCEN-mine_food — mine/food support scenario boots and replays deterministically.

const SCENARIO := "scenarios/economy/mine_food_support_map.json"
const TICKS := 120


func get_name() -> String:
	return "test_scenario_mine_food_support_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	assert_true(state.diagnostics.has("shortages"), "shortage diagnostics expected for mine_food map")
	assert_true(state.diagnostics.has("resources"), "resource diagnostics expected for mine_food map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
