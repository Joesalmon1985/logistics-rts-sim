extends "res://sim/tests/test_case.gd"

## ECON-SCEN-forest — forest density summary is tracked over the scenario.

const SCENARIO := "scenarios/economy/forest_depletion_regrowth_map.json"
const TICKS := 120


func get_name() -> String:
	return "test_scenario_forest_depletion_regrowth_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	var summary: Variant = EconomyState.get_meta_value(state, "forest_summary", {})
	assert_true(typeof(summary) == TYPE_DICTIONARY, "forest summary tracked")
	assert_true(state.diagnostics.has("resources"), "resource diagnostics expected for forest map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
