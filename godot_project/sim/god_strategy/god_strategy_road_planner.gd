class_name GodStrategyRoadPlanner
extends RefCounted

## Ownership-aware road segment planning for god-strategy bots.


static func plan_road_segments(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> Array:
	var goal_q: int = to_q
	var goal_r: int = to_r
	var start := _resolve_road_start(state, from_q, from_r, goal_q, goal_r)
	if start.q == goal_q and start.r == goal_r:
		return []

	for goal in _road_goal_candidates(state, goal_q, goal_r):
		var path: Array = _find_path(state, player_id, start, goal)
		if path.size() >= 2:
			return _path_to_segments(path)
	return []


static func _road_goal_candidates(state: GameState, to_q: int, to_r: int) -> Array:
	var goals: Array = []
	var center := HexCoord.new(to_q, to_r)
	if _hex_supports_road_endpoint(state, center):
		goals.append(center)
	for neighbour in center.neighbors():
		if state.map.in_bounds(neighbour) and _hex_supports_road_endpoint(state, neighbour):
			goals.append(neighbour)
	goals.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	return goals


static func _hex_supports_road_endpoint(state: GameState, coord: HexCoord) -> bool:
	var tile: HexTile = state.map.get_tile(coord)
	if tile == null:
		return false
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var tdef: Dictionary = cfg.get_terrain_def(tile.terrain_type)
	return int(tdef.get("road_legal", 0)) > 0


static func _path_to_segments(path: Array) -> Array:
	var segments: Array = []
	for i in range(path.size() - 1):
		var a: HexCoord = path[i]
		var b: HexCoord = path[i + 1]
		segments.append({
			"from_q": a.q,
			"from_r": a.r,
			"to_q": b.q,
			"to_r": b.r,
		})
	return segments


static func _resolve_road_start(state: GameState, from_q: int, from_r: int, goal_q: int, goal_r: int) -> HexCoord:
	var start := HexCoord.new(from_q, from_r)
	if _hex_supports_road_endpoint(state, start):
		return start
	var goal := HexCoord.new(goal_q, goal_r)
	var best: HexCoord = start
	var best_dist: int = start.distance(goal)
	for neighbour in start.neighbors():
		if not state.map.in_bounds(neighbour):
			continue
		if not _hex_supports_road_endpoint(state, neighbour):
			continue
		var dist: int = neighbour.distance(goal)
		if dist < best_dist:
			best_dist = dist
			best = neighbour
	return best


static func has_road_path(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> bool:
	return not plan_road_segments(state, player_id, from_q, from_r, to_q, to_r).is_empty()


static func has_delivery_path(
	state: GameState,
	player_id: String,
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
) -> bool:
	var start := _resolve_road_start(state, from_q, from_r, to_q, to_r)
	var from_node: int = EconomyRoadSystem.nearest_connected_road_node(state, start.q, start.r, player_id)
	var to_node: int = EconomyRoadSystem.nearest_connected_road_node(state, to_q, to_r, player_id, from_node)
	if from_node <= 0 or to_node <= 0:
		return false
	return not EconomyPathfinder.find_route(state, from_node, to_node, player_id).is_empty()


static func _find_path(state: GameState, player_id: String, start: HexCoord, goal: HexCoord) -> Array:
	var frontier: Array = [start]
	var came_from: Dictionary = {start.key(): null}
	var guard: int = 0
	while not frontier.is_empty() and guard < 4096:
		guard += 1
		var current: HexCoord = frontier.pop_front()
		if current.q == goal.q and current.r == goal.r:
			return _reconstruct(came_from, start, goal)
		for neighbour in current.neighbors():
			if not state.map.in_bounds(neighbour):
				continue
			var key: String = neighbour.key()
			if came_from.has(key):
				continue
			if not _step_legal(state, player_id, current, neighbour):
				continue
			came_from[key] = current
			frontier.append(neighbour)
	return []


static func _step_legal(state: GameState, player_id: String, from_hex: HexCoord, to_hex: HexCoord) -> bool:
	var check: Dictionary = EconomyMapRules.can_place_road(state, from_hex, to_hex, player_id)
	if check.get("ok", false):
		return true
	if str(check.get("reason", "")) == "ROAD_EXISTS":
		return true
	return false


static func _reconstruct(came_from: Dictionary, start: HexCoord, goal: HexCoord) -> Array:
	var path: Array = [goal]
	var current: HexCoord = goal
	var guard: int = 0
	while guard < 512:
		guard += 1
		var prev: Variant = came_from.get(current.key())
		if prev == null:
			break
		current = prev
		path.push_front(current)
		if current.q == start.q and current.r == start.r:
			break
	if path.is_empty() or (path[0] as HexCoord).q != start.q or (path[0] as HexCoord).r != start.r:
		return []
	return path
