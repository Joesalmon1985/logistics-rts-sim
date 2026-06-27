extends SceneTree

## Tutorial military bot smoke — completes Skirmish MVP objective (MRP Phase B4).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const SCENARIO := "scenarios/military/military_mvp_skirmish.json"
const ARTIFACT_DIR := "res://../docs/audit/bot_runs/"


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var seed_value: int = 42
	var max_ticks: int = 8000
	var stall_limit: int = 800
	for arg in args:
		if arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))

	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	if scenario.is_empty():
		print("TUTORIAL MILITARY BOT SMOKE FAIL: scenario load failed")
		_quit(1)
		return
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = max_ticks
	var state: GameState = runner.create_game(scenario)
	var bot := TutorialMilitaryBot.new()
	state = bot_runner.run_until_complete(state, bot)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var military: Dictionary = snapshot.get("diagnostics", {}).get("military", {})
	var complete: bool = str(military.get("victory_state", "")) == MilitaryConstants.VICTORY_WON
	var stall: int = bot.get_stall_ticks_since_progress(state.tick)

	if not complete:
		print("TUTORIAL MILITARY BOT SMOKE FAIL: victory not achieved at tick %d" % state.tick)
		print("  victory_state=%s" % str(military.get("victory_state", "")))
		_quit(1)
		return

	if stall > stall_limit:
		print("TUTORIAL MILITARY BOT SMOKE FAIL: stall %d ticks exceeds limit %d" % [stall, stall_limit])
		_quit(1)
		return

	_export_artifacts(state, bot, bot_runner, snapshot, scenario)
	print("TUTORIAL MILITARY BOT SMOKE PASS: victory tick=%d decisions=%d" % [
		state.tick, bot.get_decisions().size(),
	])
	_quit(0)


func _export_artifacts(
	state: GameState,
	bot: TutorialMilitaryBot,
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
	var summary_path: String = root.path_join("tutorial_military_bot_smoke_summary.json")
	var f := FileAccess.open(summary_path, FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()

	var csv_path: String = root.path_join("tutorial_military_bot_decisions.csv")
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


func _quit(code: int) -> void:
	print("EXIT: %d (%s)" % [code, "pass" if code == 0 else "fail"])
	call_deferred("quit", code)
