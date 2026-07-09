extends TestCase


func get_name() -> String:
	return "test_mobile_ffa_004_three_modular_neural_opponents_load"


func _run_body() -> void:
	var ctrl := HybridMobileFfaController.new()
	ctrl.reset_ffa("normal")
	assert_eq(ctrl.ai_bots.size(), 3, "three AI opponents")
	for pid in ["p2", "p3", "p4"]:
		assert_true(ctrl.ai_bots[pid] is ModularNeuralHybridBot, pid)
