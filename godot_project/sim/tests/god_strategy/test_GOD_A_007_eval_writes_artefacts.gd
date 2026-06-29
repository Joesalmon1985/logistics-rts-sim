extends "res://sim/tests/test_case.gd"

const EvalRunner := preload("res://sim/god_strategy/god_strategy_eval_runner.gd")


func get_name() -> String:
	return "test_GOD_A_007_eval_writes_artefacts"


func _run_body() -> void:
	var out_dir: String = EvalRunner.run_eval_mode(
		"test_1_scripted_vs_passive",
		GodStrategyConstants.SMOKE_TICK_CAP,
		true,
	)
	assert_true(DirAccess.dir_exists_absolute(out_dir), "eval output dir exists")
	assert_true(FileAccess.file_exists(out_dir.path_join("match_summary.json")), "match_summary.json")
	assert_true(FileAccess.file_exists(out_dir.path_join("bot_phase_progression.json")), "bot_phase_progression.json")
	assert_true(FileAccess.file_exists(out_dir.path_join("match_end_reason.txt")), "match_end_reason.txt")
	assert_true(
		FileAccess.file_exists(out_dir.path_join("winner.txt"))
		or FileAccess.file_exists(out_dir.path_join("no_winner_tick_cap.txt")),
		"winner or tick-cap marker",
	)
	var progression: Dictionary = JSON.parse_string(
		FileAccess.get_file_as_string(out_dir.path_join("bot_phase_progression.json")),
	)
	assert_true(progression.has("p1"), "p1 progression required")
	var p1: Dictionary = progression["p1"]
	assert_true(str(p1.get("furthest_phase", "")) != "", "furthest_phase recorded")
