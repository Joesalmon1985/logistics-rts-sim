extends SceneTree

## God-Strategy eval harness — domination regression + 4P scripted FFA.


func _initialize() -> void:
	SimBootstrap.load_all()
	var ok: bool = true
	var passive_dir: String = GodStrategyEvalRunner.run_eval_mode("1_scripted_vs_3_passive", 500, true)
	if passive_dir == "":
		ok = false
	if GodStrategyEvalRunner.run_eval_mode("4_scripted_ffa", 300, false) == "":
		ok = false
	if ok and passive_dir != "":
		var winner_path: String = passive_dir.path_join("winner.txt")
		if FileAccess.file_exists(winner_path) and FileAccess.get_file_as_string(winner_path).strip_edges() == "p1":
			ok = true
		else:
			var stall_path: String = passive_dir.path_join("bot_stall_report.json")
			if FileAccess.file_exists(stall_path):
				var stall: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(stall_path))
				if typeof(stall) == TYPE_DICTIONARY and bool(stall.get("past_barracks_phase", false)):
					ok = true
				else:
					ok = false
			else:
				ok = false
	if ok:
		print("GOD STRATEGY EVAL PASS")
		quit(0)
	print("GOD STRATEGY EVAL FAIL")
	quit(1)
