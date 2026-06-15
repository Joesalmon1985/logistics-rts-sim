class_name EconomyDemo
extends RefCounted

## Curated economy trial demo state for RUN_05 viewer — compact, readable layout.


static func build_state() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 42,
		"map_width": 12,
		"map_height": 12,
		"players": ["p1"],
		"hq_position": {"q": 5, "r": 5},
		"build_radius": 8,
		"starting_wares": {"planks": 20, "tools": 5},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 11, 11)
	# Forest patch east of HQ — woodcutter target
	state.map.set_tile(HexCoord.new(8, 5), _forest_tile())
	state.map.set_tile(HexCoord.new(9, 5), _forest_tile())
	# Road chain HQ (5,5) -> (6,5) -> (7,5) -> woodcutter site
	_connect_roads(state, 5, 5, 8, 5)
	EconomyInit._spawn_completed_building(
		state, "woodcutter", 8, 5, "p1", EconomyState.get_config(state),
	)
	# Sawmill south of HQ (visible second building)
	EconomyInit._spawn_completed_building(
		state, "sawmill", 5, 7, "p1", EconomyState.get_config(state),
	)
	_connect_roads(state, 5, 5, 5, 7)
	return state


static func _flatten_grass(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			var coord := HexCoord.new(q, r)
			if state.map.in_bounds(coord):
				state.map.set_tile(coord, HexTile.new("grass", 0, 1, 1))


static func _forest_tile() -> HexTile:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = 80
	tile.deposit_type = "forest"
	tile.deposit_amount = 50
	tile.deposit_revealed = 1
	return tile


static func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	var current := HexCoord.new(from_q, from_r)
	var target := HexCoord.new(to_q, to_r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
