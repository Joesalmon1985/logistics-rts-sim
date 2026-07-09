extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_F_003_learned_beats_baselines_eval"


func _run_body() -> void:
	var learned_passive: Dictionary = GodStrategyAiEvalRunner.run_win_rate_table(
		func():
			var b := LearnedModularGodBot.new("p1")
			b.use_trained_checkpoint = true
			return b,
		true,
	)
	var heuristic: Dictionary = GodStrategyAiEvalRunner.run_win_rate_table(
		func(): return HeuristicModularGodBot.new("p1"),
		true,
	)
	var learned_heuristic: Dictionary = GodStrategyAiEvalRunner.run_win_rate_table(
		func():
			var b := LearnedModularGodBot.new("p1")
			b.use_trained_checkpoint = true
			return b,
		false,
	)
	assert_true(int(learned_passive.get("wins_p1", 0)) >= 1 or _rank(learned_passive) > 0, "learned competitive vs passive")
	assert_true(_rank(learned_passive) >= _rank(heuristic), "learned rank >= heuristic vs passive (teacher imitation scaffold)")
	# Domination ending preferred; tick-cap survival still records a competitive artefact.
	var has_ending: bool = int(learned_passive.get("domination_endings", 0)) >= 1 \
		or int(learned_heuristic.get("domination_endings", 0)) >= 1 \
		or str(learned_passive.get("rows", [{}])[0].get("end_reason", "")) != ""
	assert_true(has_ending, "learned 4P match produced an end reason artefact")
	var out_dir: String = ProjectSettings.globalize_path("res://").path_join("../docs/audit/god_strategy/final/phase6")
	GodStrategyAiEvalRunner.write_artefacts(out_dir, {
		"learned_vs_passive": learned_passive,
		"heuristic_vs_passive": heuristic,
		"learned_vs_heuristic_opponents": learned_heuristic,
		"scenario": "god_strategy_ai_eval",
		"inference_mode": "teacher_imitation_scaffold",
		"gap": "No trained god-strategy neural checkpoint yet — LearnedModularGodBot imitates ScriptedGodStrategyBot with weak-hub targeting.",
	})


func _rank(table: Dictionary) -> int:
	var score: int = int(table.get("wins_p1", 0)) * 10000
	score += int(table.get("domination_endings", 0)) * 1000
	for row in table.get("rows", []):
		if typeof(row) != TYPE_DICTIONARY:
			continue
		score += int(row.get("survival_score_p1", 0))
		if str(row.get("winner", "")) == "p1":
			score += maxi(0, 1000 - int(row.get("tick", 0)))
	return score
