extends "res://sim/tests/test_case.gd"

## ECON-SCEN-tiny_success — minimal win-path scenario boots, runs, and replays deterministically.

const SCENARIO := "scenarios/economy/tiny_success_map.json"
const TICKS := 50


func get_name() -> String:
	return "test_scenario_tiny_success_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	assert_true(hq_id > 0, "HQ must exist in tiny_success_map")
	assert_true(not EconomyState.all_buildings(state).is_empty(), "buildings present after init")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
