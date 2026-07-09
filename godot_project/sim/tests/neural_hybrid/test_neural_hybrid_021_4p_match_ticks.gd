extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_021_4p_match_ticks"


func _run_body() -> void:
	var state := Helper.load_4p_state(25)
	assert_true(state.tick > 5, "ticks advanced")
