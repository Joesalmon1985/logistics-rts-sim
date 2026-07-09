extends "res://sim/tests/test_case.gd"

const EvalRunner := preload("res://sim/god_strategy/god_strategy_eval_runner.gd")


func get_name() -> String:
	return "test_GOD_A_017_domination_regression"


func _run_body() -> void:
	var out_dir: String = EvalRunner.run_eval_mode(
		"phase0_domination",
		GodStrategyConstants.PROOF_TICK_CAP,
		true,
		EvalRunner.SCENARIO_REGRESSION,
	)
	assert_true(out_dir != "", "phase0 domination eval must produce output dir")
	assert_true(EvalRunner.evaluate_domination_pass(out_dir), "p1 must win by domination, not tick cap")
	var summary: Dictionary = JSON.parse_string(
		FileAccess.get_file_as_string(out_dir.path_join("match_summary.json")),
	)
	var match_end: Dictionary = summary.get("match_end", {})
	assert_eq(str(match_end.get("end_reason", "")), "domination", "match must end by domination")
	assert_eq(str(match_end.get("winner_player_id", "")), "p1", "p1 must be sole survivor")
	var eliminated: Array = match_end.get("eliminated_players", [])
	assert_true(eliminated.size() >= 3, "three passive opponents must be eliminated")
