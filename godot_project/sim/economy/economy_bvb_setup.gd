class_name EconomyBvbSetup
extends RefCounted

## Symmetric resource pockets and map prep for bot-vs-bot economy matches.

const PROFILE_PROOF: String = "proof"
const PROFILE_VISUAL: String = "visual"

const CHECKPOINT_TICKS: Array = [0, 200, 800, 1800, 3000, 5000]


static func prepare_expansion_duel_map(state: GameState, scenario: Dictionary = {}) -> void:
	var width: int = state.map.width
	var height: int = state.map.height
	_flatten_grass(state)
	var mid_q: int = width / 2
	var mid_r: int = height / 2

	for r in range(height):
		for q in range(width):
			var coord := HexCoord.new(q, r)
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null:
				continue
			if q >= mid_q - 1 and q <= mid_q and r >= mid_r - 1 and r <= mid_r:
				tile.tree_density = 0
				tile.deposit_type = ""
				tile.deposit_amount = 0
				tile.buildability = 1

	_seed_player_pocket(state, 4, 4, 8)
	_seed_player_pocket(state, width - 5, height - 5, 8)

	if not scenario.is_empty():
		if bool(scenario.get("apply_starting_buildings", false)):
			_apply_spawn_starting_buildings(state, scenario)
		if bool(scenario.get("connect_spawn_network", false)):
			for spawn in scenario.get("spawns", []):
				if typeof(spawn) != TYPE_DICTIONARY:
					continue
				_connect_spawn_network(state, str(spawn.get("player_id", "p1")))
			EconomyWorkerSystem.tick(state)
		record_match_baselines(state)


static func record_match_baselines(state: GameState) -> void:
	var baselines: Dictionary = {}
	for player_id in _player_ids(state):
		baselines[player_id] = {
			"buildings": _count_active_non_hq_buildings(state, player_id),
			"roads": _count_player_roads(state, player_id),
			"deliveries": EconomyState.get_player_stat(state, player_id, "completed_deliveries_count"),
		}
	EconomyState.set_meta_value(state, "bvb_baselines", baselines)


static func get_baseline(state: GameState, player_id: String, metric: String) -> int:
	var baselines: Dictionary = EconomyState.get_meta_value(state, "bvb_baselines", {})
	var player: Dictionary = baselines.get(player_id, {})
	return int(player.get(metric, 0))


static func _player_ids(state: GameState) -> Array:
	var ids: Array = []
	for player in state.players:
		ids.append(player.player_id)
	if ids.is_empty():
		return ["p1", "p2"]
	return ids


static func _count_active_non_hq_buildings(state: GameState, player_id: String) -> int:
	var hq_id: int = EconomyState.get_hq_building_id(state, player_id)
	var count: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if int(building.get("id", 0)) == hq_id:
			continue
		if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
			count += 1
	return count


static func _count_player_roads(state: GameState, player_id: String) -> int:
	var total: int = 0
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) == player_id:
			total += 1
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) == player_id:
			total += 1
	return total


static func _spawn_kit() -> Dictionary:
	return {
		"starting_wares": {"tools": 12, "planks": 15, "stone": 12, "rations": 20, "meals": 10},
		"starting_carriers": 4,
		"starting_workers": {"carrier": 2, "builder": 2, "woodcutter": 1, "carpenter": 1},
	}


static func expansion_duel_scenario(profile: String = PROFILE_PROOF) -> Dictionary:
	var kit: Dictionary = _spawn_kit()
	var max_ticks: int = 800 if profile == PROFILE_PROOF else 3000
	var scenario := {
		"schema_version": 1,
		"variant": "economy_trial",
		"seed": 28001,
		"map_width": 24,
		"map_height": 24,
		"players": ["p1", "p2"],
		"build_radius": 11,
		"max_ticks": max_ticks,
		"stall_ticks_threshold": 600,
		"bvb_profile": profile,
		"apply_starting_buildings": false,
		"connect_spawn_network": false,
		"spawns": [
			{
				"player_id": "p1",
				"q": 4,
				"r": 4,
				"starting_wares": kit["starting_wares"],
				"starting_carriers": kit["starting_carriers"],
				"starting_workers": kit["starting_workers"],
				"starting_buildings": [],
			},
			{
				"player_id": "p2",
				"q": 19,
				"r": 19,
				"starting_wares": kit["starting_wares"],
				"starting_carriers": kit["starting_carriers"],
				"starting_workers": kit["starting_workers"],
				"starting_buildings": [],
			},
		],
	}
	return scenario


static func checkpoint_ticks_for_max(max_ticks: int) -> Array:
	var result: Array = []
	for tick in CHECKPOINT_TICKS:
		if int(tick) <= max_ticks:
			result.append(int(tick))
	if result.is_empty() or int(result[-1]) != max_ticks:
		result.append(max_ticks)
	return result


static func _apply_spawn_starting_buildings(state: GameState, scenario: Dictionary) -> void:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var player_id: String = str(spawn.get("player_id", "p1"))
		var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
		for entry in spawn.get("starting_buildings", []):
			var building_type: String = ""
			var q: int = -1
			var r: int = -1
			if typeof(entry) == TYPE_DICTIONARY:
				building_type = str(entry.get("type", entry.get("building_type", "")))
				if entry.has("q") and entry.has("r"):
					q = int(entry.get("q", -1))
					r = int(entry.get("r", -1))
			else:
				building_type = str(entry)
			if building_type == "":
				continue
			if q < 0 or r < 0 or not EconomyMapRules.can_place_building(state, building_type, q, r, player_id).get("ok", false):
				var found: Vector2i = _find_spawn_building_hex(state, player_id, building_type, hq)
				if found.x < 0:
					continue
				q = found.x
				r = found.y
			EconomyInit.spawn_completed_building(state, building_type, q, r, player_id, cfg)


static func _find_spawn_building_hex(
	state: GameState,
	player_id: String,
	building_type: String,
	hq: Vector2i,
) -> Vector2i:
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var candidates: Array[HexCoord] = []
	for dq in range(-build_radius, build_radius + 1):
		for dr in range(-build_radius, build_radius + 1):
			if abs(dq + dr) > build_radius:
				continue
			var coord := HexCoord.new(hq.x + dq, hq.y + dr)
			if state.map.in_bounds(coord):
				candidates.append(coord)
	candidates.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		var da: int = a.distance(HexCoord.new(hq.x, hq.y))
		var db: int = b.distance(HexCoord.new(hq.x, hq.y))
		if da != db:
			return da < db
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	for coord in candidates:
		if EconomyMapRules.can_place_building(state, building_type, coord.q, coord.r, player_id).get("ok", false):
			return Vector2i(coord.q, coord.r)
	return Vector2i(-1, -1)


static func _connect_spawn_network(state: GameState, player_id: String) -> void:
	var hq_pos: Vector2i = EconomyState.get_hq_position(state, player_id)
	var hq_coord := HexCoord.new(hq_pos.x, hq_pos.y)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) == "headquarters":
			continue
		var target := HexCoord.new(int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0)))
		_connect_hex_path(state, hq_coord, target, player_id)


static func _connect_hex_path(
	state: GameState,
	from_coord: HexCoord,
	to_coord: HexCoord,
	player_id: String,
) -> void:
	var current := from_coord
	while current.q != to_coord.q:
		var step: int = 1 if to_coord.q > current.q else -1
		var next := HexCoord.new(current.q + step, current.r)
		EconomyRoadSystem.place_road(state, current, next, player_id)
		current = next
	while current.r != to_coord.r:
		var step_r: int = 1 if to_coord.r > current.r else -1
		var next_r := HexCoord.new(current.q, current.r + step_r)
		EconomyRoadSystem.place_road(state, current, next_r, player_id)
		current = next_r


static func _flatten_grass(state: GameState) -> void:
	for key in state.map.tiles:
		var tile: HexTile = state.map.tiles[key]
		tile.terrain_type = "grass"
		tile.height_level = 1
		tile.passability = 1
		tile.buildability = 1


static func _seed_player_pocket(state: GameState, center_q: int, center_r: int, radius: int) -> void:
	for dq in range(-radius, radius + 1):
		for dr in range(-radius, radius + 1):
			if abs(dq + dr) > radius:
				continue
			var coord := HexCoord.new(center_q + dq, center_r + dr)
			if not state.map.in_bounds(coord):
				continue
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null:
				continue
			if dq == 0 and dr == 0:
				continue
			if (dq + dr) % 3 == 0:
				tile.terrain_type = "forest"
				tile.tree_density = 80
				tile.deposit_type = "forest"
				tile.deposit_revealed = 1
			elif (dq + dr) % 3 == 1:
				tile.deposit_type = "iron"
				tile.deposit_amount = 500
				tile.deposit_revealed = 1
				tile.buildability = 0
