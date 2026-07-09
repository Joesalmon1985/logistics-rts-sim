extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_046_modular_bot_uses_command_api"


func _run_body() -> void:
	var state := Helper.load_4p_state(5)
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, "p2")
	var bot := ModularNeuralHybridBot.new("p2")
	bot.observe(snap, "p2")
	var cmd = bot.decide(state.tick)
	assert_true(cmd == null or cmd is SimCommand, "command api")
