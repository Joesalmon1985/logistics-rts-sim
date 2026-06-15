class_name MilitaryPathfinding
extends RefCounted

## Hex-neighbour A* over the shared axial hex map. Steps only through hex
## neighbours (never axial q/r Manhattan shortcuts — RUN_02 lesson). Integer
## costs only. Returns an Array of {q, r} dictionaries from the first step to the
## goal (excluding the start), or [] if no path exists.

const MAX_EXPANSIONS: int = 4096


static func find_path(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> Array:
	if from_q == to_q and from_r == to_r:
		return []
	var goal := HexCoord.new(to_q, to_r)
	if not state.map.in_bounds(goal):
		return []
	# Allow pathing toward an impassable/occupied goal by stopping adjacent only
	# when the goal itself is passable; otherwise require passable goal.
	var goal_passable: bool = MilitaryMapRules.is_passable(state, to_q, to_r)
	if not goal_passable:
		return []

	var start_key: String = MilitaryState.coord_key(from_q, from_r)
	var goal_key: String = MilitaryState.coord_key(to_q, to_r)

	var open: Array = [start_key]
	var came_from: Dictionary = {}
	var g_score: Dictionary = {start_key: 0}
	var f_score: Dictionary = {start_key: _heuristic(from_q, from_r, to_q, to_r)}
	var closed: Dictionary = {}
	var expansions: int = 0

	while not open.is_empty() and expansions < MAX_EXPANSIONS:
		expansions += 1
		var current_key: String = _lowest_f(open, f_score)
		if current_key == goal_key:
			return _reconstruct(came_from, current_key)
		open.erase(current_key)
		closed[current_key] = true
		var cur: Vector2i = _parse_key(current_key)
		for neighbour in HexCoord.new(cur.x, cur.y).neighbors():
			var nq: int = neighbour.q
			var nr: int = neighbour.r
			var nkey: String = MilitaryState.coord_key(nq, nr)
			if closed.has(nkey):
				continue
			if not MilitaryMapRules.is_passable(state, nq, nr):
				continue
			var step_cost: int = MilitaryMapRules.movement_cost(state, nq, nr)
			var tentative: int = int(g_score.get(current_key, 0)) + step_cost
			if tentative < int(g_score.get(nkey, 1 << 30)):
				came_from[nkey] = current_key
				g_score[nkey] = tentative
				f_score[nkey] = tentative + _heuristic(nq, nr, to_q, to_r)
				if nkey not in open:
					open.append(nkey)
	return []


static func _heuristic(aq: int, ar: int, bq: int, br: int) -> int:
	return HexCoord.new(aq, ar).distance(HexCoord.new(bq, br))


static func _lowest_f(open: Array, f_score: Dictionary) -> String:
	var best_key: String = str(open[0])
	var best_val: int = int(f_score.get(best_key, 1 << 30))
	for key in open:
		var v: int = int(f_score.get(key, 1 << 30))
		if v < best_val:
			best_val = v
			best_key = str(key)
	return best_key


static func _parse_key(key: String) -> Vector2i:
	var parts: PackedStringArray = key.split(",")
	return Vector2i(int(parts[0]), int(parts[1]))


static func _reconstruct(came_from: Dictionary, current_key: String) -> Array:
	var path: Array = []
	var key: String = current_key
	while came_from.has(key):
		var c: Vector2i = _parse_key(key)
		path.push_front({"q": c.x, "r": c.y})
		key = str(came_from[key])
	return path
