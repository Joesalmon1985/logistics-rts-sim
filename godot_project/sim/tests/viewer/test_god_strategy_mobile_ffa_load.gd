extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_god_strategy_mobile_ffa_load"


func _run_body() -> void:
	var ctrl := GodStrategyMobileFfaController.new()
	ctrl.reset_ffa("normal")
	assert_eq(ctrl.ai_bots.size(), 3, "three neural god opponents")
	for pid in ["p2", "p3", "p4"]:
		assert_true(ctrl.ai_bots[pid] is LearnedModularGodBot, pid)
		var bot: LearnedModularGodBot = ctrl.ai_bots[pid]
		assert_true(bot.use_trained_checkpoint, "checkpoint mode — no silent scripted fallback in product")
	var summary: Dictionary = ctrl.get_match_summary()
	assert_eq(str(summary.get("opponent_kind", "")), "learned_modular_god", "product opponent kind")
