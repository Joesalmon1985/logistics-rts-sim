extends "res://sim/tests/test_case.gd"

## SETTLER-OBJ-001 — composite objective evaluator tracks buildings, connection, and wares.


func get_name() -> String:
	return "test_settler_objective_001_composite"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario("scenarios/economy/settler_mvp.json")
	var state: GameState = runner.create_game(scenario)
	var config: Dictionary = SettlerObjective.load_config("config/settler_mvp_objective.json")

	var incomplete: Dictionary = SettlerObjective.evaluate(state, config, "p1")
	assert_false(bool(incomplete.get("complete", true)), "starts incomplete")

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var hq: Dictionary = EconomyState.get_building(state, int(EconomyState.get_meta_value(state, "hq_building_id", 0)))
	var hq_node: int = int(hq.get("road_node_id", 0))

	for building_type in ["woodcutter", "forester", "sawmill", "quarry"]:
		var hex: Vector2i = _find_legal_hex(state, building_type)
		assert_true(hex.x >= 0, "legal hex for %s" % building_type)
		var bid: int = EconomyInit.spawn_completed_building(state, building_type, hex.x, hex.y, "p1", cfg)
		assert_true(bid > 0, "spawned %s" % building_type)
		_connect_to_hq(state, hex.x, hex.y, hq_node, "p1")

	var hq_store: int = int(hq.get("store_id", 0))
	var store: Dictionary = EconomyState.get_ware_store(state, hq_store)
	EconomyState.inventory_add(store, "planks", 10)
	EconomyState.inventory_add(store, "stone", 10)
	EconomyState.set_ware_store(state, hq_store, store)

	var complete: Dictionary = SettlerObjective.evaluate(state, config, "p1")
	assert_true(bool(complete.get("complete", false)), "objective complete when requirements met")


func _find_legal_hex(state: GameState, building_type: String) -> Vector2i:
	for r in range(state.map.height):
		for q in range(state.map.width):
			var check: Dictionary = EconomyMapRules.can_place_building(state, building_type, q, r, "p1")
			if check.get("ok", false):
				return Vector2i(q, r)
	return Vector2i(-1, -1)


func _connect_to_hq(state: GameState, q: int, r: int, hq_node: int, player_id: String) -> void:
	var building: Dictionary = {}
	for b in EconomyState.all_buildings(state):
		if int(b.get("anchor_q", -1)) == q and int(b.get("anchor_r", -1)) == r:
			building = b
			break
	var target_node: int = int(building.get("road_node_id", 0))
	if target_node <= 0 or hq_node <= 0:
		return
	if EconomyRoadSystem.is_network_connected(state, hq_node, target_node):
		return
	var hq: Dictionary = EconomyState.get_road_node(state, hq_node)
	var start := Vector2i(int(hq.get("q", 0)), int(hq.get("r", 0)))
	var goal := Vector2i(q, r)
	_greedy_road_chain(state, player_id, start, goal)


func _greedy_road_chain(state: GameState, player_id: String, start: Vector2i, goal: Vector2i) -> void:
	var current := start
	var guard: int = 0
	while current != goal and guard < 40:
		guard += 1
		var best: Vector2i = Vector2i(-1, -1)
		var best_dist: int = 9999
		for nb in HexCoord.new(current.x, current.y).neighbors():
			if not state.map.in_bounds(nb):
				continue
			var next := Vector2i(nb.q, nb.r)
			var dist: int = HexCoord.new(next.x, next.y).distance(HexCoord.new(goal.x, goal.y))
			if dist < best_dist:
				var hex_a := HexCoord.new(current.x, current.y)
				var hex_b := HexCoord.new(next.x, next.y)
				if EconomyRoadSystem.has_road_between(state, hex_a, hex_b):
					best_dist = dist
					best = next
				else:
					var check: Dictionary = EconomyMapRules.can_place_road(state, hex_a, hex_b, player_id)
					if check.get("ok", false):
						best_dist = dist
						best = next
		if best.x < 0:
			break
		var a := HexCoord.new(current.x, current.y)
		var b := HexCoord.new(best.x, best.y)
		if not EconomyRoadSystem.has_road_between(state, a, b):
			EconomyRoadSystem.place_road(state, a, b, player_id)
		current = best
