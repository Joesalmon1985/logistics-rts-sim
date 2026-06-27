extends SceneTree

## Tutorial economy bot smoke — completes Settler MVP objective (MRP Track A).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const SCENARIO := "scenarios/economy/settler_mvp.json"
const ARTIFACT_DIR := "res://../docs/audit/bot_runs/"


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var seed_value: int = 42
	var max_ticks: int = 5000
	var stall_limit: int = 500
	for arg in args:
		if arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))

	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	if scenario.is_empty():
		print("TUTORIAL BOT SMOKE FAIL: scenario load failed")
		_quit(1)
		return
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = max_ticks
	var state: GameState = runner.create_game(scenario)
	var bot := TutorialEconomyBot.new()
	state = bot_runner.run_until_complete(state, bot)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var objective: Dictionary = snapshot.get("objective", {})
	var complete: bool = bool(objective.get("complete", false))
	var stall: int = bot.get_stall_ticks_since_progress(state.tick)

	if not complete:
		print("TUTORIAL BOT SMOKE FAIL: objective not complete at tick %d" % state.tick)
		for req in objective.get("requirements", []):
			if not bool(req.get("done", false)):
				print("  incomplete: %s — %s" % [str(req.get("label", "")), str(req.get("detail", ""))])
		_quit(1)
		return

	if stall > stall_limit:
		print("TUTORIAL BOT SMOKE FAIL: stall %d ticks exceeds limit %d" % [stall, stall_limit])
		_quit(1)
		return

	_export_artifacts(state, bot, bot_runner, snapshot, scenario)
	print("TUTORIAL BOT SMOKE PASS: objective complete tick=%d decisions=%d" % [
		state.tick, bot.get_decisions().size(),
	])
	_quit(0)


func _export_artifacts(
	state: GameState,
	bot: TutorialEconomyBot,
	bot_runner: BotRunner,
	snapshot: Dictionary,
	scenario: Dictionary,
) -> void:
	var root: String = ProjectSettings.globalize_path(ARTIFACT_DIR)
	DirAccess.make_dir_recursive_absolute(root)

	var summary := {
		"seed": scenario.get("seed", 42),
		"complete": true,
		"final_tick": state.tick,
		"decision_count": bot.get_decisions().size(),
		"stall_ticks": bot.get_stall_ticks_since_progress(state.tick),
	}
	var summary_path: String = root.path_join("tutorial_bot_smoke_summary.json")
	var f := FileAccess.open(summary_path, FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()

	var csv_path: String = root.path_join("tutorial_bot_decisions.csv")
	var cf := FileAccess.open(csv_path, FileAccess.WRITE)
	if cf:
		cf.store_line("tick,actor_id,chosen_command,reason")
		for d in bot.get_decisions():
			cf.store_line("%d,%s,%s,%s" % [
				int(d.get("tick", 0)),
				str(d.get("actor_id", "")),
				str(d.get("chosen_command", "")),
				str(d.get("reason", "")).replace(",", ";"),
			])
		cf.close()

	var snap_path: String = root.path_join("tutorial_bot_final_snapshot.json")
	var sf := FileAccess.open(snap_path, FileAccess.WRITE)
	if sf:
		sf.store_string(JSON.stringify(snapshot, "\t"))
		sf.close()

	summary["decision_log_path"] = csv_path
	summary["final_snapshot_path"] = snap_path
	ManualPlayabilityBotExport.write_summary("economy_bot_summary.json", summary)


func _quit(code: int) -> void:
	print("EXIT: %d (%s)" % [code, "pass" if code == 0 else "fail"])
	call_deferred("quit", code)
