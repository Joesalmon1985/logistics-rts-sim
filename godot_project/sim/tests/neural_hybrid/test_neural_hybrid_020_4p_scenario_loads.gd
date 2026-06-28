extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_020_4p_scenario_loads"


func _run_body() -> void:
	var state := Helper.load_4p_state(1)
	assert_eq(state.players.size(), 4, "player count")
