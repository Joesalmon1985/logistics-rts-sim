extends "res://sim/tests/test_case.gd"

## ECON-SCEN-tool_shortage — tool/worker shortage diagnostics tracked.

const SCENARIO := "scenarios/economy/tool_shortage_worker_block_map.json"
const TICKS := 80


func get_name() -> String:
	return "test_scenario_tool_shortage_worker_block_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	var shortages: Dictionary = state.diagnostics.get("shortages", {})
	assert_true(typeof(shortages) == TYPE_DICTIONARY, "shortage diagnostics present for tool_shortage map")
	assert_has_keys(shortages, ["food", "tools", "workers"], "shortage diagnostics fields present")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
