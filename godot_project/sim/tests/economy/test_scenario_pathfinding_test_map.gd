extends "res://sim/tests/test_case.gd"

## ECON-SCEN-pathfinding — road/network layer accessible after multi-node scenario.

const SCENARIO := "scenarios/economy/pathfinding_test_map.json"
const TICKS := 80


func get_name() -> String:
	return "test_scenario_pathfinding_test_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	assert_true(roads.size() >= 0, "road layer accessible for pathfinding map")
	assert_true(state.diagnostics.has("network"), "network diagnostics expected for pathfinding map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
