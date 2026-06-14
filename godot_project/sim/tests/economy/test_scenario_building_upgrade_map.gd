extends "res://sim/tests/test_case.gd"

## ECON-SCEN-upgrade — building-upgrade scenario boots with buildings available to upgrade.

const SCENARIO := "scenarios/economy/building_upgrade_map.json"
const TICKS := 120


func get_name() -> String:
	return "test_scenario_building_upgrade_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	assert_true(not EconomyState.all_buildings(state).is_empty(), "buildings exist for upgrade map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
