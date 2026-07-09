extends TestCase


func get_name() -> String:
	return "test_neural_hybrid_051_modular_bot_loads_trained_checkpoint"


func _run_body() -> void:
	var bot := ModularNeuralHybridBot.new("p1")
	bot.use_trained_checkpoint = true
	bot.use_heuristic_teacher = false
	bot.reset("p1")
	assert_true(bot.checkpoint_loaded(), "checkpoint loaded")
	assert_false(bot.used_heuristic_fallback(), "no fallback at init")
