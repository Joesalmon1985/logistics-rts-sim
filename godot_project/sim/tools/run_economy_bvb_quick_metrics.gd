extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var max_ticks: int = 500
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg.begins_with("--max-ticks="):
			max_ticks = int(arg.substr("--max-ticks=".length()))
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	scenario["max_ticks"] = max_ticks
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	dual.timing_enabled = true
	var t0: int = Time.get_ticks_msec()
	state = dual.run_match(state, scenario)
	var ms: int = Time.get_ticks_msec() - t0
	var timing: Dictionary = dual.get_timing_summary()
	var hist: Dictionary = {}
	for entry in dual.get_recorder().get_command_entries():
		var t: String = str(entry.get("command", {}).get("type", "?"))
		hist[t] = int(hist.get(t, 0)) + 1
	print("command_hist=%s" % str(hist))
	print("ticks=%d wall_ms=%d ms_per_tick=%.2f" % [state.tick, ms, float(timing.get("ms_per_tick", 0.0))])
	for pid in ["p1", "p2"]:
		print("%s b=%d r=%d d=%d" % [
			pid,
			DualBotRunner.count_player_buildings_built(state, pid),
			DualBotRunner.count_player_roads_built(state, pid),
			DualBotRunner.count_player_deliveries_built(state, pid),
		])
	print("timing=%s" % str(timing.get("totals_us", {})))
	var candidates: Array = EconomyCommandAvailability.list_bot_candidate_commands(state, "p1")
	var building_cmds: int = 0
	for entry in candidates:
		var cmd_type: String = str(entry.get("type", entry.get("command_type", "")))
		if cmd_type == "PlaceBuildingPlanCommand":
			building_cmds += 1
	print("p1 building candidates=%d total=%d" % [building_cmds, candidates.size()])
	var sites_p1: int = 0
	var active_p1: int = 0
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) == "p1":
			sites_p1 += 1
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) == "p1" and str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
			if str(building.get("building_type", "")) != "headquarters":
				active_p1 += 1
	print("p1 active_buildings=%d construction_sites=%d" % [active_p1, sites_p1])
	print("EXIT: 0 (pass)")
	quit(0)
