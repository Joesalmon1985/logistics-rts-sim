extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	scenario["max_ticks"] = 800
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.attach_scenario_config(state, scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)
	for pid in ["p1", "p2"]:
		print("%s b=%d r=%d d=%d noop=%s" % [
			pid,
			DualBotRunner.count_player_buildings_built(state, pid),
			DualBotRunner.count_player_roads_built(state, pid),
			DualBotRunner.count_player_deliveries_built(state, pid),
			dual.bot_p1.last_noop_reason if pid == "p1" else dual.bot_p2.last_noop_reason,
		])
	var hist: Dictionary = {}
	for entry in dual.get_recorder().get_command_entries():
		var t: String = str(entry.get("command", {}).get("type", "?"))
		hist[t] = int(hist.get(t, 0)) + 1
	print("hist=%s" % str(hist))
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) == "p1":
			print("bld=%s/%s" % [building.get("building_type"), building.get("state")])
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) == "p2":
			print("p2_bld=%s/%s" % [building.get("building_type"), building.get("state")])
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) == "p1":
			print("site=%s/%s" % [site.get("building_type"), site.get("state")])
	var bot_cmds: Array = EconomyCommandAvailability.list_bot_candidate_commands(state, "p1")
	var full_cmds: Array = EconomyCommandAvailability.list_candidates(state, "p1")
	var bot_store: int = 0
	var full_store: int = 0
	for entry in bot_cmds:
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		if str(params.get("building_type", "")) == "storehouse":
			bot_store += 1
	for entry in full_cmds:
		var params2: Dictionary = entry.get("params", entry.get("payload", {}))
		if str(params2.get("building_type", "")) == "storehouse":
			full_store += 1
	print("p1 storehouse cmds bot=%d full=%d" % [bot_store, full_store])
	quit(0)
