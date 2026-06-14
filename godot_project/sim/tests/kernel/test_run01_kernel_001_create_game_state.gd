extends "res://sim/tests/test_case.gd"

const TestCaseScript = preload("res://sim/tests/test_case.gd")


func get_name() -> String:
	return "test_RUN01_KERNEL_001_create_game_state"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 42,
		"map_width": 4,
		"map_height": 4,
		"players": ["p1"],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	assert_not_null(state, "state should be created")
	assert_eq(state.variant_id, "economy_trial")
	assert_eq(state.seed, 42)
	assert_eq(state.tick, 0)
	assert_not_null(state.map)
	assert_eq(state.map.width, 4)
	assert_eq(state.players.size(), 1)
	assert_true(state.event_log.events.size() >= 1, "game_created event expected")
	assert_eq(state.event_log.events[0].type, "game_created")
