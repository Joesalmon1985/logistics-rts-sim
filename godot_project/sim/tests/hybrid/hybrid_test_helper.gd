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
