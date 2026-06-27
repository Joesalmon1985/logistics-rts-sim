extends SceneTree

## Tutorial hybrid bot smoke — completes Hybrid MVP objective (MRP C5).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const SCENARIO := "scenarios/hybrid/hybrid_mvp_bot_smoke.json"
const ARTIFACT_DIR := "res://../docs/audit/bot_runs/"


func _initialize() -> void:
	BootstrapScript.load_all()
	var seed_value: int = 42
	var max_ticks: int = 600
	var stall_limit: int = 500
	for arg in OS.get_cmdline_args() + OS.get_cmdline_user_args():
		if arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))

	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	if scenario.is_empty():
		print("TUTORIAL HYBRID BOT SMOKE FAIL: scenario load failed")
		_quit(1)
		return
	print("TUTORIAL HYBRID BOT SMOKE: seed=%d max_ticks=%d" % [seed_value, max_ticks])
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = max_ticks
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 1)
	var bot := TutorialHybridBot.new("p1")
	state = bot_runner.run_until_complete(state, bot)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var objective: Dictionary = snapshot.get("objective", {})
	var complete: bool = bool(objective.get("complete", false))
	var stall: int = bot.get_stall_ticks_since_progress(state.tick)

	if not complete:
		print("TUTORIAL HYBRID BOT SMOKE FAIL: objective not complete at tick %d" % state.tick)
		print("  bot decisions: %d" % bot.get_decisions().size())
		for d in bot.get_decisions():
			print("    %s" % str(d))
		for req in objective.get("requirements", []):
			if not bool(req.get("done", false)):
				print("  incomplete: %s — %s" % [str(req.get("label", "")), str(req.get("detail", ""))])
		_quit(1)
		return

	if stall > stall_limit:
		print("TUTORIAL HYBRID BOT SMOKE FAIL: stall %d exceeds limit %d" % [stall, stall_limit])
		_quit(1)
		return

	_export_artifacts(state, bot, snapshot, scenario)
	print("TUTORIAL HYBRID BOT SMOKE PASS: objective complete tick=%d decisions=%d" % [
		state.tick, bot.get_decisions().size(),
	])
	_quit(0)


func _export_artifacts(state: GameState, bot: TutorialHybridBot, snapshot: Dictionary, scenario: Dictionary) -> void:
	var root: String = ProjectSettings.globalize_path(ARTIFACT_DIR)
	DirAccess.make_dir_recursive_absolute(root)
	var summary := {
		"seed": scenario.get("seed", 42),
		"complete": true,
		"final_tick": state.tick,
		"decision_count": bot.get_decisions().size(),
	}
	var f := FileAccess.open(root.path_join("tutorial_hybrid_bot_smoke_summary.json"), FileAccess.WRITE)
	if f:
		f.store_string(JSON.stringify(summary, "\t"))
		f.close()
	ManualPlayabilityBotExport.write_summary("hybrid_bot_summary.json", summary)


func _quit(code: int) -> void:
	print("EXIT: %d (%s)" % [code, "pass" if code == 0 else "fail"])
	call_deferred("quit", code)
