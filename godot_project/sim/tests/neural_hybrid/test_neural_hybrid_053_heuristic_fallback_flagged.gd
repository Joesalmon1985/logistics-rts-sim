extends TestCase


func get_name() -> String:
	return "test_neural_hybrid_053_heuristic_fallback_flagged"


func _run_body() -> void:
	var bot := ModularNeuralHybridBot.new("p1")
	bot.use_trained_checkpoint = false
	bot.use_heuristic_teacher = true
	var snap: Dictionary = {"tick": 1, "player_id": "p1", "buildings": [], "units": [], "military_buildings": []}
	bot.observe(snap, "p1")
	bot.decide(1)
	var telem: Dictionary = bot.get_last_telemetry()
	assert_true(bool(telem.get("heuristic_fallback", false)) or str(telem.get("inference_mode", "")).begins_with("heuristic"), "fallback flagged")
