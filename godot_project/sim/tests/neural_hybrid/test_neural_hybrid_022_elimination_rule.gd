extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_022_elimination_rule"


func _run_body() -> void:
	var state := Helper.load_4p_state(10)
	var ffa: Dictionary = HybridFfaObjective.evaluate(state)
	assert_has_keys(ffa, ["winner_rule", "alive_players"], "ffa keys")
