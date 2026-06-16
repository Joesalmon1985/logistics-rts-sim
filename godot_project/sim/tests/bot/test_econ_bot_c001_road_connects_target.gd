extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 80
const MAX_ROADS_BUILT: int = 25


func get_name() -> String:
	return "test_ECON_BOT_C001_road_connects_target"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)

	var bot := EconomyExpansionBot.new("p1")
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = MAX_TICKS
	state = bot_runner.run_until_complete(state, bot)

	var sites: Array = []
	for site in state.entities.get(EconomyConstants.BUCKET_CONSTRUCTION_SITES, {}).values():
		if str(site.get("player_id", "")) == "p1":
			sites.append(site)
	assert_true(not sites.is_empty(), "p1 has a construction site")

	var connected: bool = false
	for site in sites:
		if _site_is_road_adjacent(state, site, "p1"):
			connected = true
			break
	assert_true(connected, "construction site hex is road-adjacent")

	var roads_built: int = DualBotRunner.count_player_roads_built(state, "p1")
	assert_true(roads_built <= MAX_ROADS_BUILT, "roads built within connect budget")


func _site_is_road_adjacent(state: GameState, site: Dictionary, player_id: String) -> bool:
	var q: int = int(site.get("anchor_q", site.get("q", -1)))
	var r: int = int(site.get("anchor_r", site.get("r", -1)))
	if q < 0:
		return false
	var site_coord := HexCoord.new(q, r)
	for neighbor in site_coord.neighbors():
		if _hex_has_player_road(state, neighbor, player_id):
			return true
	return false


func _hex_has_player_road(state: GameState, coord: HexCoord, player_id: String) -> bool:
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) != player_id:
			continue
		var a := HexCoord.new(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99))))
		var b := HexCoord.new(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99))))
		if coord.distance(a) == 0 or coord.distance(b) == 0:
			return true
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) != player_id:
			continue
		if coord.distance(HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))) == 0:
			return true
	return false
