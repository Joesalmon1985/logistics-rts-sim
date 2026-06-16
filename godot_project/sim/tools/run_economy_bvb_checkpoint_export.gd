extends SceneTree

## Export snapshot checkpoints from one long visual-profile dual-bot run.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const OUTPUT_SUBDIR: String = "training_runs/visual_review/run_02_8_economy_bvb/checkpoints"


func _initialize() -> void:
	BootstrapScript.load_all()
	var max_ticks: int = 3000
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))

	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_VISUAL)
	scenario["max_ticks"] = max_ticks
	var checkpoint_ticks: Array = EconomyBvbSetup.checkpoint_ticks_for_max(max_ticks)
	var output_dir: String = _resolve_output_dir()
	DirAccess.make_dir_recursive_absolute(output_dir)

	var manifest: Array = []
	var wall_start: int = Time.get_ticks_msec()

	var on_checkpoint := func(state: GameState, tick: int) -> void:
		EconomyDiagnosticsSystem.tick(state)
		var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
		var file_name: String = "tick_%05d.json" % tick
		var file_path: String = output_dir.path_join(file_name)
		var f := FileAccess.open(file_path, FileAccess.WRITE)
		if f == null:
			push_error("Cannot write checkpoint %s" % file_path)
			return
		f.store_string(JSON.stringify(snapshot, "\t"))
		manifest.append({
			"tick": tick,
			"file": file_name,
			"economy_scores": snapshot.get("diagnostics", {}).get("economy_scores", {}),
		})
		print("Checkpoint tick=%d -> %s" % [tick, file_path])

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	dual.timing_enabled = true
	state = dual.run_match(state, scenario, checkpoint_ticks, on_checkpoint)

	var timing: Dictionary = dual.get_timing_summary()
	var manifest_path: String = output_dir.path_join("checkpoint_manifest.json")
	var mf := FileAccess.open(manifest_path, FileAccess.WRITE)
	if mf != null:
		mf.store_string(JSON.stringify({
			"max_ticks": max_ticks,
			"final_tick": state.tick,
			"wall_ms": Time.get_ticks_msec() - wall_start,
			"ms_per_tick": timing.get("ms_per_tick", 0.0),
			"hash": ReplayHash.compute_state_hash(state),
			"checkpoints": manifest,
		}, "\t"))

	print("CHECKPOINT EXPORT PASS: %d checkpoints wall=%dms ms/tick=%.2f" % [
		manifest.size(),
		Time.get_ticks_msec() - wall_start,
		float(timing.get("ms_per_tick", 0.0)),
	])
	print("EXIT: 0 (pass)")
	quit(0)


func _resolve_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("..").path_join(OUTPUT_SUBDIR)
