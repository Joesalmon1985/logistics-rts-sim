extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_052_trained_inference_mode"


func _run_body() -> void:
	var state := Helper.load_4p_state(15)
	var bot := ModularNeuralHybridBot.new("p1")
	bot.use_trained_checkpoint = true
	bot.use_heuristic_teacher = false
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, "p1")
	bot.observe(snap, "p1")
	bot.decide(state.tick)
	var telem: Dictionary = bot.get_last_telemetry()
	assert_eq(str(telem.get("inference_mode", "")), "trained_neural", "trained inference")
