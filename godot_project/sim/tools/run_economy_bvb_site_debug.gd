extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var bot := EconomyExpansionBot.new("p1")
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 200
	state = bot_runner.run_until_complete(state, bot)
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) != "p1":
			continue
		print("site=%s" % str(site))
	var jobs: int = 0
	for job in EconomyState.all_delivery_jobs(state):
		if str(job.get("player_id", "")) == "p1":
			jobs += 1
			print("job=%s" % str(job))
	print("job_count=%d deliveries=%d roads=%d" % [
		jobs,
		DualBotRunner.count_player_deliveries_built(state, "p1"),
		DualBotRunner.count_player_roads_built(state, "p1"),
	])
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) != "p1":
			continue
		var dest_node: int = EconomyRoadSystem.create_building_road_node(
			state, 0, int(site.get("anchor_q", 0)), int(site.get("anchor_r", 0)), "p1",
		)
		var hq := EconomyState.get_building(state, EconomyState.get_hq_building_id(state, "p1"))
		var hq_node: int = int(hq.get("road_node_id", 0))
		var route: Array = EconomyPathfinder.find_route(state, hq_node, dest_node, "p1")
		print("dest_node=%d hq_node=%d route_len=%d" % [dest_node, hq_node, route.size()])
	var hq := EconomyState.get_building(state, EconomyState.get_hq_building_id(state, "p1"))
	print("hq_store=%s" % str(EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))))
	var building_count: int = 0
	for building in EconomyState.all_buildings(state):
		building_count += 1
		if str(building.get("player_id", "")) == "p1":
			print("building=%s/%s @ %s,%s" % [
				building.get("building_type"),
				building.get("state"),
				building.get("anchor_q"),
				building.get("anchor_r"),
			])
	print("total_buildings=%d buildings_built=%d" % [
		building_count,
		DualBotRunner.count_player_buildings_built(state, "p1"),
	])
	quit(0)


func _has_active_woodcutter(state: GameState, player_id: String) -> bool:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		if str(building.get("building_type", "")) == "woodcutter" and str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
			return true
	return false
