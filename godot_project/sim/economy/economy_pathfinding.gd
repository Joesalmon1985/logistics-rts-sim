class_name EconomyPathfinder
extends RefCounted

## Deterministic Dijkstra pathfinding on the road-node graph.


static func hex_distance(q_a: int, r_a: int, q_b: int, r_b: int) -> int:
	return HexCoord.new(q_a, r_a).distance(HexCoord.new(q_b, r_b))


static func find_route(
	state: GameState,
	from_node_id: int,
	to_node_id: int,
	player_id: String = "",
) -> Array:
	if from_node_id == to_node_id:
		return [from_node_id]
	if from_node_id <= 0 or to_node_id <= 0:
		_store_failure(state, from_node_id, to_node_id, "INVALID_NODE")
		return []

	var from_node: Dictionary = EconomyState.get_road_node(state, from_node_id)
	var to_node: Dictionary = EconomyState.get_road_node(state, to_node_id)
	if from_node.is_empty() or to_node.is_empty():
		_store_failure(state, from_node_id, to_node_id, "NODE_NOT_FOUND")
		return []

	var route_player: String = player_id
	if route_player != "":
		if str(from_node.get("player_id", "")) != route_player or str(to_node.get("player_id", "")) != route_player:
			_store_failure(state, from_node_id, to_node_id, "PLAYER_MISMATCH")
			return []

	var dist: Dictionary = {str(from_node_id): 0}
	var prev: Dictionary = {}
	var visited: Dictionary = {}
	var frontier: Array = [from_node_id]

	while not frontier.is_empty():
		frontier.sort()
		var current_id: int = int(frontier.pop_front())
		var current_key: String = str(current_id)
		if visited.has(current_key):
			continue
		visited[current_key] = true
		if current_id == to_node_id:
			break

		var current_node: Dictionary = EconomyState.get_road_node(state, current_id)
		for neighbour_id in _neighbour_node_ids(state, current_node, route_player):
			var edge_cost: int = _edge_cost(state, current_id, neighbour_id)
			if edge_cost >= 99:
				continue
			var neighbour_key: String = str(neighbour_id)
			var new_dist: int = int(dist.get(current_key, 99)) + edge_cost
			if not dist.has(neighbour_key) or new_dist < int(dist[neighbour_key]):
				dist[neighbour_key] = new_dist
				prev[neighbour_key] = current_id
				if not frontier.has(neighbour_id):
					frontier.append(neighbour_id)
			elif new_dist == int(dist[neighbour_key]) and neighbour_id < int(prev.get(neighbour_key, 999999)):
				prev[neighbour_key] = current_id

	if not prev.has(str(to_node_id)) and from_node_id != to_node_id:
		_store_failure(state, from_node_id, to_node_id, "NO_PATH")
		return []

	var route: Array = []
	var walk_id: int = to_node_id
	while true:
		route.push_front(walk_id)
		if walk_id == from_node_id:
			break
		var prev_key: String = str(walk_id)
		if not prev.has(prev_key):
			_store_failure(state, from_node_id, to_node_id, "NO_PATH")
			return []
		walk_id = int(prev[prev_key])

	_clear_failure(state, from_node_id, to_node_id)
	return route


static func _neighbour_node_ids(state: GameState, node: Dictionary, player_id: String = "") -> Array:
	var result: Array = []
	var coord := HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))
	for segment_id in node.get("segment_ids", []):
		var road: Dictionary = EconomyState.get_road(state, int(segment_id))
		if road.is_empty():
			continue
		if player_id != "" and str(road.get("player_id", "")) != player_id:
			continue
		var other_coord: HexCoord
		var hex_a := HexCoord.new(int(road.get("hex_a_q", 0)), int(road.get("hex_a_r", 0)))
		var hex_b := HexCoord.new(int(road.get("hex_b_q", 0)), int(road.get("hex_b_r", 0)))
		if hex_a.q == coord.q and hex_a.r == coord.r:
			other_coord = hex_b
		else:
			other_coord = hex_a
		var other_id: int = EconomyRoadSystem._find_node_at(state, other_coord.q, other_coord.r)
		if other_id > 0:
			if player_id != "":
				var other_node: Dictionary = EconomyState.get_road_node(state, other_id)
				if str(other_node.get("player_id", "")) != player_id:
					continue
			if not result.has(other_id):
				result.append(other_id)
	result.sort()
	return result


static func _edge_cost(state: GameState, from_node_id: int, to_node_id: int) -> int:
	var from_node: Dictionary = EconomyState.get_road_node(state, from_node_id)
	var to_node: Dictionary = EconomyState.get_road_node(state, to_node_id)
	var from_coord := HexCoord.new(int(from_node.get("q", 0)), int(from_node.get("r", 0)))
	var to_coord := HexCoord.new(int(to_node.get("q", 0)), int(to_node.get("r", 0)))
	var segment_id: int = EconomyRoadSystem.segment_between_nodes(state, from_node_id, to_node_id)
	if segment_id <= 0:
		return 99
	var road: Dictionary = EconomyState.get_road(state, segment_id)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var cost: int = EconomyMapRules.movement_cost(state, from_coord, to_coord)
	if int(road.get("congestion", 0)) >= cfg.int_val("road_congestion_limit", 3):
		cost += 5
	return cost


static func _failure_key(from_node_id: int, to_node_id: int) -> String:
	return "%d->%d" % [from_node_id, to_node_id]


static func _store_failure(state: GameState, from_node_id: int, to_node_id: int, reason: String) -> void:
	var blocked: Dictionary = EconomyState.get_meta_value(state, "blocked_routes", {})
	if typeof(blocked) != TYPE_DICTIONARY:
		blocked = {}
	blocked[_failure_key(from_node_id, to_node_id)] = {
		"from_node_id": from_node_id,
		"to_node_id": to_node_id,
		"reason": reason,
	}
	EconomyState.set_meta_value(state, "blocked_routes", blocked)
	EconomyState.set_meta_value(state, "last_route_failure", blocked[_failure_key(from_node_id, to_node_id)])
	EconomyState.emit_event(state, "route_blocked", blocked[_failure_key(from_node_id, to_node_id)])


static func _clear_failure(state: GameState, from_node_id: int, to_node_id: int) -> void:
	var blocked: Dictionary = EconomyState.get_meta_value(state, "blocked_routes", {})
	if typeof(blocked) == TYPE_DICTIONARY:
		blocked.erase(_failure_key(from_node_id, to_node_id))
		EconomyState.set_meta_value(state, "blocked_routes", blocked)
