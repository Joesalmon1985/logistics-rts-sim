extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_F_002_heuristic_beats_scripted_eval"


func _run_body() -> void:
	var scripted: Dictionary = GodStrategyAiEvalRunner.run_win_rate_table(
		func(): return ScriptedGodStrategyBot.new("p1"),
		true,
	)
	var heuristic: Dictionary = GodStrategyAiEvalRunner.run_win_rate_table(
		func(): return HeuristicModularGodBot.new("p1"),
		true,
	)
	# PRD Phase 5: heuristic modular bot beats Phase 0 scripted baseline on committed table.
	var h_score: int = _rank_score(heuristic)
	var s_score: int = _rank_score(scripted)
	assert_true(h_score >= s_score, "heuristic rank_score >= scripted (wins/domination/survival)")
	assert_true(int(heuristic.get("wins_p1", 0)) + int(heuristic.get("domination_endings", 0)) + h_score > 0, "heuristic non-vacuous")
	var out_dir: String = ProjectSettings.globalize_path("res://").path_join("../docs/audit/god_strategy/final/phase5")
	GodStrategyAiEvalRunner.write_artefacts(out_dir, {
		"scripted_vs_passive": scripted,
		"heuristic_vs_passive": heuristic,
		"scenario": "god_strategy_ai_eval",
		"rank_scores": {"scripted": s_score, "heuristic": h_score},
	})


func _rank_score(table: Dictionary) -> int:
	var score: int = int(table.get("wins_p1", 0)) * 10000
	score += int(table.get("domination_endings", 0)) * 1000
	for row in table.get("rows", []):
		if typeof(row) != TYPE_DICTIONARY:
			continue
		score += int(row.get("survival_score_p1", 0))
		# Prefer finishing earlier when winning.
		if str(row.get("winner", "")) == "p1":
			score += maxi(0, 1000 - int(row.get("tick", 0)))
	return score
