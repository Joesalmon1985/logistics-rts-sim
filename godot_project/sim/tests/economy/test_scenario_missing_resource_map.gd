extends "res://sim/tests/test_case.gd"

## ECON-SCEN-missing_resource — placing a deposit-bound building without its deposit is rejected.

const SCENARIO := "scenarios/economy/missing_resource_map.json"
const TICKS := 80


func get_name() -> String:
	return "test_scenario_missing_resource_map"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	assert_true(not scenario.is_empty(), "scenario load failed: %s" % SCENARIO)

	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, TICKS)

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "quarry",
		"q": 5,
		"r": 5,
		"player_id": "p1",
	})
	var result: CommandResult = validator.validate(state, cmd)
	assert_false(result.success, "quarry must reject placement without a matching deposit")

	var hash_a: String = ReplayHash.compute_state_hash(state)
	var state_b: GameState = runner.create_game(scenario)
	state_b = runner.run_ticks(state_b, TICKS)
	assert_eq(hash_a, ReplayHash.compute_state_hash(state_b), "scenario replay hash stable: %s" % SCENARIO)
