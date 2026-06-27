class_name HybridTestHelper
extends RefCounted

## Shared scenario builders for HYB-SMOKE tests.


static func base_scenario(seed_value: int = 707) -> Dictionary:
	return {
		"variant": "hybrid",
		"seed": seed_value,
		"map_width": 16,
		"map_height": 16,
		"players": ["p1"],
		"build_radius": 14,
		"fog_enabled": 0,
		"starting_wares": {
			"planks": 30,
			"stone": 30,
			"tools": 15,
			"rations": 15,
			"iron_ingots": 15,
		},
		"starting_workers": {"builder": 2, "carrier": 3},
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
	}


static func flatten_grass(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))


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


static func connect_roads(state: GameState, validator: CommandValidator, q0: int, r: int, q1: int) -> void:
	for q in range(q0, q1):
		validator.validate_and_apply(state, PlaceRoadCommand.new("p1", 0, {
			"from_q": q, "from_r": r, "to_q": q + 1, "to_r": r,
		}))


static func building_of_type(state: GameState, building_type: String, player_id: String = "p1") -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("player_id", "")) == player_id:
			return int(building.get("id", 0))
	return 0


static func count_units(state: GameState, unit_type: String, player_id: String = "p1") -> int:
	var count: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type and str(unit.get("player_id", "")) == player_id:
			count += 1
	return count


static func has_event(state: GameState, event_type: String) -> bool:
	for event in state.event_log.events:
		if str(event.type) == event_type:
			return true
	return false
