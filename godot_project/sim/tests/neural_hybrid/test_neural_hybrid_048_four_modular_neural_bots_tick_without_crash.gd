extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_048_four_modular_neural_bots_tick_without_crash"


func _run_body() -> void:
	var state := Helper.load_4p_state(30)
	assert_true(state.tick > 10, "four bots ticked")
