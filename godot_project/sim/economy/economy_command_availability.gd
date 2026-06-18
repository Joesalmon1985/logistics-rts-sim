class_name EconomyCommandAvailability
extends RefCounted

const MAX_ROAD_CANDIDATES: int = 64
const MAX_BUILDING_CANDIDATES: int = 32
const BOT_MAX_ROAD_CANDIDATES: int = 24
const BOT_MAX_BUILDING_CANDIDATES: int = 24
const BOT_FRONTIER_NODE_THRESHOLD: int = 6
const BOT_BUILD_SEARCH_RADIUS: int = 6
const BOT_FOREST_SEARCH_RADIUS: int = 8
const BOT_MAX_ROAD_START_HEXES: int = 12

const BUILDING_TYPES: Array[String] = ["storehouse", "woodcutter", "sawmill", "workshop"]
const FOREST_BUILDING_TYPES: Array[String] = ["woodcutter", "forester"]


## Deterministic bot action shortlist (not the complete legal command universe).
## Every picked command still passes CommandValidator.validate_and_apply in DualBotRunner.
static func list_bot_candidate_commands(state: GameState, actor_id: String) -> Array:
	var results: Array = list_bot_candidates(state, actor_id)
	if not _has_pass_in_results(results):
		var pass_cmd := NoopCommand.new(actor_id, state.tick)
		results.append(CommandAvailabilityDto.from_command(pass_cmd, "Pass / wait", {}))
	return CommandAvailabilityDto.sort_commands(results)


static func list_bot_candidates(state: GameState, actor_id: String) -> Array:
	var results: Array = []
	EconomyState.ensure_buckets(state)
	var tick: int = state.tick
	var edge_keys: Dictionary = _player_road_edge_keys(state, actor_id)
	var node_keys: Dictionary = _player_road_node_keys(state, actor_id)
	# Worker policy is not useful for expansion duel — skip to save validate budget.
	_try_roads(results, state, actor_id, tick, true, edge_keys)
	_try_buildings(results, state, actor_id, tick, BOT_MAX_BUILDING_CANDIDATES, true, node_keys)
	_try_forest_buildings(results, state, actor_id, tick, 12, true)
	return results


static func _has_pass_in_results(results: Array) -> bool:
	for entry in results:
		if str(entry.get("type", "")) == "NoopCommand":
			return true
	return false


static func _actor_hq(state: GameState, actor_id: String) -> Vector2i:
	return EconomyState.get_hq_position(state, actor_id)


static func list_candidates(state: GameState, actor_id: String) -> Array:
	var results: Array = []
	EconomyState.ensure_buckets(state)
	var tick: int = state.tick

	_try_policy(results, state, actor_id, tick)
	_try_roads(results, state, actor_id, tick, false)
	_try_buildings(results, state, actor_id, tick, MAX_BUILDING_CANDIDATES)
	_try_forest_buildings(results, state, actor_id, tick, 8)
	return results


static func _try_policy(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	for policy in ["auto", "manual"]:
		var cmd := SetWorkerPolicyCommand.new(actor_id, tick, {"policy": policy, "player_id": actor_id})
		if cmd.validate(state).success:
			results.append(CommandAvailabilityDto.from_command(
				cmd,
				"Workers: Set worker policy %s" % policy,
				{"policy": policy},
			))


static func _try_roads(
	results: Array,
	state: GameState,
	actor_id: String,
	tick: int,
	bot_mode: bool,
	edge_keys: Dictionary = {},
) -> void:
	var added: int = 0
	var seen: Dictionary = {}
	var max_roads: int = BOT_MAX_ROAD_CANDIDATES if bot_mode else MAX_ROAD_CANDIDATES
	var edges: Dictionary = edge_keys if not edge_keys.is_empty() else _player_road_edge_keys(state, actor_id)
	var start_hexes: Array[HexCoord] = _road_start_hexes(state, actor_id, bot_mode, edges)
	if bot_mode:
		start_hexes = _prioritize_construction_site_starts(state, actor_id, start_hexes)
	for start in start_hexes:
		if added >= max_roads:
			break
		for neighbor in start.neighbors():
			if not state.map.in_bounds(neighbor):
				continue
			var key: String = "%d,%d->%d,%d" % [start.q, start.r, neighbor.q, neighbor.r]
			if seen.has(key):
				continue
			seen[key] = true
			var payload: Dictionary = {
				"from_q": start.q,
				"from_r": start.r,
				"to_q": neighbor.q,
				"to_r": neighbor.r,
				"player_id": actor_id,
			}
			var hex_a := HexCoord.new(start.q, start.r)
			var hex_b := HexCoord.new(neighbor.q, neighbor.r)
			var cmd := PlaceRoadCommand.new(actor_id, tick, payload)
			if _road_legal(state, hex_a, hex_b, actor_id, edges):
				var label: String = "Roads: Place road %d,%d -> %d,%d" % [start.q, start.r, neighbor.q, neighbor.r]
				for site in EconomyState.all_construction_sites(state):
					if int(site.get("anchor_q", -1)) == start.q and int(site.get("anchor_r", -1)) == start.r:
						label = "Roads: Connect site at %d,%d -> %d,%d" % [start.q, start.r, neighbor.q, neighbor.r]
						break
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					label,
					{"from_q": start.q, "from_r": start.r, "to_q": neighbor.q, "to_r": neighbor.r},
				))
				added += 1


static func _prioritize_construction_site_starts(
	state: GameState,
	actor_id: String,
	start_hexes: Array[HexCoord],
) -> Array[HexCoord]:
	var site_starts: Array[HexCoord] = []
	var other_starts: Array[HexCoord] = []
	var site_keys: Dictionary = {}
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) != actor_id:
			continue
		var sq: int = int(site.get("anchor_q", site.get("q", -1)))
		var sr: int = int(site.get("anchor_r", site.get("r", -1)))
		if sq < 0:
			continue
		site_keys["%d,%d" % [sq, sr]] = true
	for start in start_hexes:
		var key: String = "%d,%d" % [start.q, start.r]
		if site_keys.has(key):
			site_starts.append(start)
		else:
			other_starts.append(start)
	var ordered: Array[HexCoord] = []
	ordered.append_array(site_starts)
	ordered.append_array(other_starts)
	return ordered


static func _road_start_hexes(
	state: GameState,
	actor_id: String,
	bot_mode: bool = false,
	edge_keys: Dictionary = {},
) -> Array[HexCoord]:
	var coords: Array[HexCoord] = []
	var seen: Dictionary = {}
	var node_count: int = 0
	var all_nodes: Array[HexCoord] = []
	for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
		if typeof(node) != TYPE_DICTIONARY:
			continue
		if str(node.get("player_id", "")) != actor_id:
			continue
		node_count += 1
		all_nodes.append(HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0))))

	if bot_mode and node_count >= BOT_FRONTIER_NODE_THRESHOLD:
		var edges: Dictionary = edge_keys if not edge_keys.is_empty() else _player_road_edge_keys(state, actor_id)
		for coord in _road_frontier_nodes(state, all_nodes, BOT_MAX_ROAD_START_HEXES, edges):
			var key: String = "%d,%d" % [coord.q, coord.r]
			if not seen.has(key):
				seen[key] = true
				coords.append(coord)
	else:
		for coord in all_nodes:
			var key: String = "%d,%d" % [coord.q, coord.r]
			if not seen.has(key):
				seen[key] = true
				coords.append(coord)

	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) != actor_id:
			continue
		var sq: int = int(site.get("anchor_q", site.get("q", 0)))
		var sr: int = int(site.get("anchor_r", site.get("r", 0)))
		var key2: String = "%d,%d" % [sq, sr]
		if not seen.has(key2):
			seen[key2] = true
			coords.append(HexCoord.new(sq, sr))
	if not bot_mode or node_count < BOT_FRONTIER_NODE_THRESHOLD:
		var hq_pos: Vector2i = _actor_hq(state, actor_id)
		for coord2 in _hexes_near(state, HexCoord.new(hq_pos.x, hq_pos.y), 2):
			var key3: String = "%d,%d" % [coord2.q, coord2.r]
			if not seen.has(key3):
				seen[key3] = true
				coords.append(coord2)
	coords.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	return coords


static func _road_frontier_nodes(
	state: GameState,
	all_nodes: Array[HexCoord],
	cap: int,
	edge_keys: Dictionary = {},
) -> Array[HexCoord]:
	var frontier: Array[HexCoord] = []
	for coord in all_nodes:
		for neighbor in coord.neighbors():
			if not state.map.in_bounds(neighbor):
				continue
			if not _has_road_edge(coord, neighbor, edge_keys):
				frontier.append(coord)
				break
	frontier.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	if frontier.size() <= cap:
		return frontier
	return frontier.slice(0, cap)


static func _try_buildings(
	results: Array,
	state: GameState,
	actor_id: String,
	tick: int,
	max_candidates: int,
	bot_mode: bool = false,
	node_keys: Dictionary = {},
) -> void:
	var hq_pos: Vector2i = _actor_hq(state, actor_id)
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var search_radius: int = build_radius if bot_mode else build_radius
	var added: int = 0
	var keys: Dictionary = node_keys if not node_keys.is_empty() else _player_road_node_keys(state, actor_id)
	var edge_keys: Dictionary = _player_road_edge_keys(state, actor_id)

	for building_type in BUILDING_TYPES:
		var coords: Array[HexCoord] = []
		var type_cap: int = 6 if bot_mode else max_candidates
		var type_added: int = 0
		if building_type == "storehouse":
			coords = _storehouse_candidate_hexes(state, actor_id, search_radius, keys, edge_keys)
		elif building_type in ["sawmill", "workshop"]:
			coords = _road_network_build_hexes(state, actor_id, search_radius, keys, edge_keys)
		else:
			coords = _hexes_near(state, HexCoord.new(hq_pos.x, hq_pos.y), search_radius)
		if bot_mode and building_type in ["storehouse", "sawmill", "workshop"]:
			coords = _merge_unique_hex_coords(
				coords,
				_hexes_near(state, HexCoord.new(hq_pos.x, hq_pos.y), search_radius),
			)
		for coord in coords:
			if type_added >= type_cap or added >= max_candidates:
				break
			if not _building_legal(state, building_type, coord.q, coord.r, actor_id):
				continue
			var payload: Dictionary = {
				"building_type": building_type,
				"q": coord.q,
				"r": coord.r,
				"player_id": actor_id,
			}
			var cmd := PlaceBuildingPlanCommand.new(actor_id, tick, payload)
			var display_name: String = building_type.replace("_", " ").capitalize()
			var suffix: String = " (road-adjacent)" if building_type == "storehouse" and _is_road_adjacent(state, coord, actor_id, keys, edge_keys) else ""
			results.append(CommandAvailabilityDto.from_command(
				cmd,
				"Buildings: Place %s Construction Site at %d,%d%s" % [display_name, coord.q, coord.r, suffix],
				{"q": coord.q, "r": coord.r, "building_type": building_type},
			))
			type_added += 1
			added += 1


static func _merge_unique_hex_coords(primary: Array[HexCoord], extra: Array[HexCoord]) -> Array[HexCoord]:
	var seen: Dictionary = {}
	var merged: Array[HexCoord] = []
	for coord in primary:
		var key: String = "%d,%d" % [coord.q, coord.r]
		if seen.has(key):
			continue
		seen[key] = true
		merged.append(coord)
	for coord in extra:
		var key2: String = "%d,%d" % [coord.q, coord.r]
		if seen.has(key2):
			continue
		seen[key2] = true
		merged.append(coord)
	return merged


static func _storehouse_candidate_hexes(
	state: GameState,
	actor_id: String,
	search_radius: int,
	node_keys: Dictionary,
	edge_keys: Dictionary,
) -> Array[HexCoord]:
	var coords: Array[HexCoord] = _road_adjacent_build_hexes(state, actor_id, search_radius, node_keys, edge_keys)
	var seen: Dictionary = {}
	for coord in coords:
		seen["%d,%d" % [coord.q, coord.r]] = true
	for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
		if typeof(node) != TYPE_DICTIONARY:
			continue
		if str(node.get("player_id", "")) != actor_id:
			continue
		var node_coord := HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))
		for candidate in _hexes_near(state, node_coord, 1):
			var key: String = "%d,%d" % [candidate.q, candidate.r]
			if seen.has(key):
				continue
			if not _is_road_adjacent(state, candidate, actor_id, node_keys, edge_keys):
				continue
			seen[key] = true
			coords.append(candidate)
	coords.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	return coords


static func _road_network_build_hexes(
	state: GameState,
	actor_id: String,
	search_radius: int,
	node_keys: Dictionary,
	edge_keys: Dictionary,
) -> Array[HexCoord]:
	return _storehouse_candidate_hexes(state, actor_id, search_radius, node_keys, edge_keys)


static func _road_adjacent_build_hexes(
	state: GameState,
	actor_id: String,
	build_radius: int,
	node_keys: Dictionary = {},
	edge_keys: Dictionary = {},
) -> Array[HexCoord]:
	var hq_pos: Vector2i = _actor_hq(state, actor_id)
	var keys: Dictionary = node_keys if not node_keys.is_empty() else _player_road_node_keys(state, actor_id)
	var edges: Dictionary = edge_keys if not edge_keys.is_empty() else _player_road_edge_keys(state, actor_id)
	var coords: Array[HexCoord] = []
	for coord in _hexes_near(state, HexCoord.new(hq_pos.x, hq_pos.y), build_radius):
		if _is_road_adjacent(state, coord, actor_id, keys, edges):
			coords.append(coord)
	return coords


static func _player_road_edge_keys(state: GameState, actor_id: String) -> Dictionary:
	var keys: Dictionary = {}
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) != actor_id:
			continue
		var edge_key: String = str(road.get("edge_key", ""))
		if edge_key != "":
			keys[edge_key] = true
	return keys


static func _has_road_edge(hex_a: HexCoord, hex_b: HexCoord, edge_keys: Dictionary) -> bool:
	if edge_keys.is_empty():
		return false
	return edge_keys.has(EconomyRoadSystem.road_key(hex_a, hex_b))


static func _player_road_node_keys(state: GameState, actor_id: String) -> Dictionary:
	var keys: Dictionary = {}
	for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
		if typeof(node) != TYPE_DICTIONARY:
			continue
		if str(node.get("player_id", "")) != actor_id:
			continue
		keys["%d,%d" % [int(node.get("q", 0)), int(node.get("r", 0))]] = true
	return keys


static func _is_road_adjacent(
	state: GameState,
	coord: HexCoord,
	actor_id: String,
	node_keys: Dictionary = {},
	edge_keys: Dictionary = {},
) -> bool:
	var keys: Dictionary = node_keys if not node_keys.is_empty() else _player_road_node_keys(state, actor_id)
	var edges: Dictionary = edge_keys if not edge_keys.is_empty() else _player_road_edge_keys(state, actor_id)
	for neighbor in coord.neighbors():
		if not state.map.in_bounds(neighbor):
			continue
		if keys.has("%d,%d" % [neighbor.q, neighbor.r]):
			return true
		if _has_road_edge(coord, neighbor, edges) or _has_road_edge(neighbor, coord, edges):
			return true
	return false


static func _hexes_near(state: GameState, center: HexCoord, radius: int) -> Array[HexCoord]:
	var coords: Array[HexCoord] = []
	for dq in range(-radius, radius + 1):
		for dr in range(-radius, radius + 1):
			if abs(dq + dr) > radius:
				continue
			var coord := HexCoord.new(center.q + dq, center.r + dr)
			if state.map != null and state.map.in_bounds(coord):
				coords.append(coord)
	coords.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	return coords


static func _try_forest_buildings(
	results: Array,
	state: GameState,
	actor_id: String,
	tick: int,
	max_forest: int,
	bot_mode: bool = false,
) -> void:
	var hq_pos: Vector2i = _actor_hq(state, actor_id)
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var search_radius: int = build_radius if bot_mode else build_radius
	var added: int = 0
	for building_type in FOREST_BUILDING_TYPES:
		var type_added: int = 0
		for coord in _hexes_near(state, HexCoord.new(hq_pos.x, hq_pos.y), search_radius):
			if type_added >= 6 or added >= max_forest:
				break
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null:
				continue
			if tile.owner_player_id != "" and tile.owner_player_id != actor_id:
				continue
			if tile.tree_density < 20:
				continue
			if not _building_legal(state, building_type, coord.q, coord.r, actor_id):
				continue
			var payload: Dictionary = {
				"building_type": building_type,
				"q": coord.q,
				"r": coord.r,
				"player_id": actor_id,
			}
			var cmd := PlaceBuildingPlanCommand.new(actor_id, tick, payload)
			results.append(CommandAvailabilityDto.from_command(
				cmd,
				"Buildings: Place %s on forest at %d,%d" % [building_type, coord.q, coord.r],
				{"q": coord.q, "r": coord.r, "building_type": building_type},
			))
			type_added += 1
			added += 1


static func _road_legal(
	state: GameState,
	hex_a: HexCoord,
	hex_b: HexCoord,
	actor_id: String,
	edge_keys: Dictionary = {},
) -> bool:
	return EconomyMapRules.can_place_road(state, hex_a, hex_b, actor_id, edge_keys).get("ok", false)


static func _building_legal(
	state: GameState,
	building_type: String,
	q: int,
	r: int,
	actor_id: String,
) -> bool:
	return EconomyMapRules.can_place_building(state, building_type, q, r, actor_id).get("ok", false)
