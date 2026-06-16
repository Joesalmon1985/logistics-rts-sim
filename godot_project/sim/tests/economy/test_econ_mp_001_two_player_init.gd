extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MP_001_two_player_init"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/two_player_init_test.json")
	var state: GameState = runner.create_game(scenario)

	assert_eq(state.players.size(), 2, "two players in state")
	assert_true(_player_has_hq(state, "p1"), "p1 HQ exists")
	assert_true(_player_has_hq(state, "p2"), "p2 HQ exists")
	assert_true(_player_has_workers(state, "p1"), "p1 workers exist")
	assert_true(_player_has_workers(state, "p2"), "p2 workers exist")
	assert_true(_player_has_carriers(state, "p1"), "p1 carriers exist")
	assert_true(_player_has_carriers(state, "p2"), "p2 carriers exist")
	assert_true(_player_has_starter_wares(state, "p1"), "p1 starter wares")
	assert_true(_player_has_starter_wares(state, "p2"), "p2 starter wares")
	assert_true(_ownership_distinct(state), "ownership hexes distinct")
	assert_true(_entities_tagged(state, "p1"), "p1 entities tagged")
	assert_true(_entities_tagged(state, "p2"), "p2 entities tagged")

	var sp_scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/tiny_success_map.json")
	var sp_state: GameState = runner.create_game(sp_scenario)
	assert_true(int(EconomyState.get_meta_value(sp_state, "hq_building_id", 0)) > 0, "SP scenario still boots")


func _player_has_hq(state: GameState, player_id: String) -> bool:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	if hq_id <= 0:
		return false
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	return str(hq.get("building_type", "")) == "headquarters" and str(hq.get("player_id", "")) == player_id


func _player_has_workers(state: GameState, player_id: String) -> bool:
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) == player_id:
			return true
	return false


func _player_has_carriers(state: GameState, player_id: String) -> bool:
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("player_id", "")) == player_id:
			return true
	return false


func _player_has_starter_wares(state: GameState, player_id: String) -> bool:
	var store_id: int = EconomyState.get_hq_store_id(state, player_id)
	var store: Dictionary = EconomyState.get_ware_store(state, store_id)
	return int(store.get("wares", {}).get("tools", 0)) > 0


func _ownership_distinct(state: GameState) -> bool:
	var p1_hexes: int = 0
	var p2_hexes: int = 0
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		if tile.owner_player_id == "p1":
			p1_hexes += 1
		elif tile.owner_player_id == "p2":
			p2_hexes += 1
	return p1_hexes > 0 and p2_hexes > 0


func _entities_tagged(state: GameState, player_id: String) -> bool:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	for building in EconomyState.all_buildings(state):
		if int(building.get("id", 0)) == hq_id:
			if str(building.get("player_id", "")) != player_id:
				return false
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) == player_id:
			if int(worker.get("home_store_id", 0)) != EconomyState.get_hq_store_id(state, player_id):
				return false
	return true
