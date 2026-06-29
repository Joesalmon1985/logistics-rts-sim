extends "res://sim/tests/test_case.gd"

const EvalRunner := preload("res://sim/god_strategy/god_strategy_eval_runner.gd")


func get_name() -> String:
	return "test_GOD_A_007_eval_writes_artefacts"


func _run_body() -> void:
	var out_dir: String = EvalRunner.run_eval_mode("test_1_scripted_vs_passive", 200, true)
	assert_true(DirAccess.dir_exists_absolute(out_dir), "eval output dir exists")
	assert_true(FileAccess.file_exists(out_dir.path_join("match_summary.json")), "match_summary.json")
	assert_true(
		FileAccess.file_exists(out_dir.path_join("winner.txt"))
		or FileAccess.file_exists(out_dir.path_join("no_winner_tick_cap.txt")),
		"winner or tick-cap marker",
	)
