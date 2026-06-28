class_name HybridMapSetup
extends RefCounted

## Shared hybrid map preparation for scenarios and spectators.


static func prepare_playable_map(
	state: GameState,
	q0: int,
	r0: int,
	q1: int,
	r1: int,
	hq_q: int,
	hq_r: int,
) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			var tile: HexTile = state.map.get_tile(HexCoord.new(q, r))
			if tile == null:
				continue
			if tile.terrain_type in ["water", "mountain"]:
				state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
	_seed_forest_pocket(state, hq_q + 5, hq_r, 2)
	_seed_forest_pocket(state, hq_q + 6, hq_r - 1, 1)
	_seed_rock_pocket(state, hq_q + 5, hq_r + 2, 2)
	_seed_rock_pocket(state, hq_q + 6, hq_r + 3, 1)


static func _seed_forest_pocket(state: GameState, center_q: int, center_r: int, radius: int) -> void:
	for dq in range(-radius, radius + 1):
		for dr in range(-radius, radius + 1):
			if abs(dq + dr) > radius:
				continue
			var coord := HexCoord.new(center_q + dq, center_r + dr)
			if not state.map.in_bounds(coord):
				continue
			var tile := HexTile.new("forest", 0, 1, 1)
			tile.tree_density = 80
			tile.deposit_type = "forest"
			tile.deposit_revealed = 1
			state.map.set_tile(coord, tile)


static func _seed_rock_pocket(state: GameState, center_q: int, center_r: int, radius: int) -> void:
	for dq in range(-radius, radius + 1):
		for dr in range(-radius, radius + 1):
			if abs(dq + dr) > radius:
				continue
			var coord := HexCoord.new(center_q + dq, center_r + dr)
			if not state.map.in_bounds(coord):
				continue
			var tile := HexTile.new("rock", 0, 1, 0)
			tile.deposit_type = "rock"
			tile.deposit_amount = 150
			tile.deposit_revealed = 1
			state.map.set_tile(coord, tile)
