extends SceneTree

## RUN_02.8b perf baseline — reports dual-bot ms/tick at several horizons.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var lines: PackedStringArray = PackedStringArray()
	lines.append("# RUN_02.8b Dual-Bot Performance Baseline")
	lines.append("")
	lines.append("Environment: Linux Godot headless, proof profile (minimal starters).")
	lines.append("")
	for max_ticks in [50, 200]:
		var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
		scenario["max_ticks"] = max_ticks
		var runner := SimulationTickRunner.new()
		var state: GameState = runner.create_game(scenario)
		EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
		var dual := DualBotRunner.new(runner)
		dual.timing_enabled = true
		var wall_start: int = Time.get_ticks_msec()
		state = dual.run_match(state, scenario)
		var wall_ms: int = Time.get_ticks_msec() - wall_start
		var timing: Dictionary = dual.get_timing_summary()
		var ms_per_tick: float = float(timing.get("ms_per_tick", 0.0))
		lines.append("## %d ticks" % max_ticks)
		lines.append("- wall_ms: %d" % wall_ms)
		lines.append("- ms_per_tick (instrumented): %.2f" % ms_per_tick)
		lines.append("- final_tick: %d" % state.tick)
		for pid in ["p1", "p2"]:
			lines.append("- %s bot_built: b=%d r=%d d=%d" % [
				pid,
				DualBotRunner.count_player_buildings_built(state, pid),
				DualBotRunner.count_player_roads_built(state, pid),
				DualBotRunner.count_player_deliveries_built(state, pid),
			])
		var totals: Dictionary = timing.get("totals_us", {})
		if not totals.is_empty():
			lines.append("- timing_us: diag=%d snap=%d shortlist=%d apply=%d sim=%d" % [
				int(totals.get("diagnostics_us", 0)),
				int(totals.get("snapshot_us", 0)),
				int(totals.get("shortlist_us", 0)),
				int(totals.get("bot_apply_us", 0)),
				int(totals.get("sim_tick_us", 0)),
			])
		lines.append("")
	var out_path: String = ProjectSettings.globalize_path("res://").path_join("../docs/audit/RUN_02_8b_PERF_BASELINE.md")
	var f := FileAccess.open(out_path, FileAccess.WRITE)
	if f != null:
		f.store_string("\n".join(lines))
		print("Wrote %s" % out_path)
	for line in lines:
		print(line)
	print("EXIT: 0 (pass)")
	quit(0)
