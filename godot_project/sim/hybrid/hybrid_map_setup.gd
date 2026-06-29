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
	map_center_q: int = -1,
	map_center_r: int = -1,
	player_id: String = "",
) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			var tile: HexTile = state.map.get_tile(HexCoord.new(q, r))
			if tile == null:
				continue
			if tile.terrain_type in ["water", "mountain"]:
				state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))

	var dir_q: int = 1
	var dir_r: int = 0
	if map_center_q >= 0 and map_center_r >= 0:
		dir_q = clampi(map_center_q - hq_q, -1, 1)
		dir_r = clampi(map_center_r - hq_r, -1, 1)
		if dir_q == 0 and dir_r == 0:
			dir_q = 1

	var perp_q: int = -dir_r
	var perp_r: int = dir_q
	_seed_forest_pocket(state, hq_q + dir_q * 5 + perp_q * 0, hq_r + dir_r * 5 + perp_r * 0, 2, player_id)
	_seed_forest_pocket(state, hq_q + dir_q * 6 + perp_q * 0, hq_r + dir_r * 6 + perp_r * 0 - dir_q, 1, player_id)
	_seed_rock_pocket(state, hq_q + dir_q * 5 + perp_q * 0, hq_r + dir_r * 5 + perp_r * 0 + 2, 2, player_id)
	_seed_rock_pocket(state, hq_q + dir_q * 6 + perp_q * 0, hq_r + dir_r * 6 + perp_r * 0 + 3, 1, player_id)
	_ensure_mvp_grass_slots(state, hq_q, hq_r, dir_q, dir_r, perp_q, perp_r, player_id)
	_claim_spawn_corridor(state, hq_q, hq_r, dir_q, dir_r, perp_q, perp_r, player_id)


static func _claim_spawn_corridor(
	state: GameState,
	hq_q: int,
	hq_r: int,
	dir_q: int,
	dir_r: int,
	perp_q: int,
	perp_r: int,
	player_id: String,
) -> void:
	if player_id == "":
		return
	for step in range(0, 10):
		for perp in range(-4, 5):
			var q: int = hq_q + dir_q * step + perp_q * perp
			var r: int = hq_r + dir_r * step + perp_r * perp
			_set_owned_grass(state, q, r, player_id)


static func _set_owned_grass(state: GameState, q: int, r: int, player_id: String) -> void:
	var coord := HexCoord.new(q, r)
	if not state.map.in_bounds(coord):
		return
	var existing: HexTile = state.map.get_tile(coord)
	if existing != null and existing.owner_player_id != "" and existing.owner_player_id != player_id:
		return
	var tile := HexTile.new("grass", 0, 1, 1)
	if existing != null:
		tile.terrain_type = existing.terrain_type
		tile.tree_density = existing.tree_density
		tile.deposit_type = existing.deposit_type
		tile.deposit_amount = existing.deposit_amount
		tile.deposit_revealed = existing.deposit_revealed
		tile.buildability = existing.buildability
	tile.owner_player_id = player_id
	state.map.set_tile(coord, tile)


static func _ensure_mvp_grass_slots(
	state: GameState,
	hq_q: int,
	hq_r: int,
	dir_q: int,
	dir_r: int,
	perp_q: int,
	perp_r: int,
	player_id: String = "",
) -> void:
	for step in range(1, 9):
		for perp in range(-3, 4):
			if abs(perp) > step:
				continue
			var q: int = hq_q + dir_q * step + perp_q * perp
			var r: int = hq_r + dir_r * step + perp_r * perp
			if player_id != "":
				_set_owned_grass(state, q, r, player_id)
			else:
				_set_buildable_grass(state, q, r)


static func _set_buildable_grass(state: GameState, q: int, r: int) -> void:
	var coord := HexCoord.new(q, r)
	if not state.map.in_bounds(coord):
		return
	state.map.set_tile(coord, HexTile.new("grass", 0, 1, 1))


static func _seed_forest_pocket(state: GameState, center_q: int, center_r: int, radius: int, player_id: String = "") -> void:
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
			if player_id != "":
				tile.owner_player_id = player_id
			state.map.set_tile(coord, tile)


static func _seed_rock_pocket(state: GameState, center_q: int, center_r: int, radius: int, player_id: String = "") -> void:
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
			if player_id != "":
				tile.owner_player_id = player_id
			state.map.set_tile(coord, tile)
