extends "res://sim/tests/test_case.gd"

## ECON-SCEN-blocked_route — no-route / blocked delivery diagnostics scenario.

const SCENARIO := "scenarios/economy/blocked_route_map.json"
const TICKS := 80


func get_name() -> String:
	return "test_scenario_blocked_route_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	assert_true(state.diagnostics.has("delivery"), "delivery diagnostics present for blocked-route map")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
