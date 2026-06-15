class_name EconomyDemo
extends RefCounted

## Curated economy trial demo state for RUN_05 viewer.


static func build_state() -> GameState:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 42,
		"map_width": 16,
		"map_height": 16,
		"players": ["p1"],
		"hq_position": {"q": 4, "r": 4},
		"build_radius": 10,
		"starting_wares": {"planks": 20, "tools": 5},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 15, 15)
	state.map.set_tile(HexCoord.new(8, 4), _forest_tile())
	state.map.set_tile(HexCoord.new(9, 5), _forest_tile())
	_connect_roads(state, 4, 4, 6, 4)
	EconomyInit._spawn_completed_building(
		state, "woodcutter", 8, 4, "p1", EconomyState.get_config(state),
	)
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
