class_name EconomyRoadSystem
extends RefCounted

## Road segments, nodes, congestion, and connectivity.


static func road_key(hex_a: HexCoord, hex_b: HexCoord) -> String:
	if hex_a.key() < hex_b.key():
		return "%s|%s" % [hex_a.key(), hex_b.key()]
	return "%s|%s" % [hex_b.key(), hex_a.key()]


static func has_road_between(state: GameState, hex_a: HexCoord, hex_b: HexCoord) -> bool:
	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	var key: String = road_key(hex_a, hex_b)
	for road_key_id in roads:
		var road: Dictionary = roads[road_key_id]
		if road.get("edge_key", "") == key:
			return true
	return false


static func get_road_between(state: GameState, hex_a: HexCoord, hex_b: HexCoord) -> Dictionary:
	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	var key: String = road_key(hex_a, hex_b)
	for road_key_id in roads:
		var road: Dictionary = roads[road_key_id]
		if road.get("edge_key", "") == key:
			return road
	return {}


static func place_road(
	state: GameState,
	hex_a: HexCoord,
	hex_b: HexCoord,
	player_id: String,
) -> Dictionary:
	var check: Dictionary = EconomyMapRules.can_place_road(state, hex_a, hex_b, player_id)
	if not check.get("ok", false):
		return {"ok": false, "reason": str(check.get("reason", "REJECTED")), "road_id": 0}

	var road_id: int = state.entity_ids.allocate()
	var road := {
		"id": road_id,
		"hex_a_q": hex_a.q,
		"hex_a_r": hex_a.r,
		"hex_b_q": hex_b.q,
		"hex_b_r": hex_b.r,
		"edge_key": road_key(hex_a, hex_b),
		"player_id": player_id,
		"congestion": 0,
	}
	EconomyState.add_road(state, road)
	_ensure_node_at_hex(state, hex_a, player_id, 0)
	_ensure_node_at_hex(state, hex_b, player_id, 0)
	_link_nodes_for_segment(state, road)
	EconomyMapRules.sync_road_segment_refs(state)
	EconomyState.emit_event(state, "road_placed", {"road_id": road_id, "player_id": player_id})
	return {"ok": true, "reason": "", "road_id": road_id}


static func remove_road(state: GameState, road_id: int) -> Dictionary:
	var road: Dictionary = EconomyState.get_road(state, road_id)
	if road.is_empty():
		return {"ok": false, "reason": "ROAD_NOT_FOUND"}
	if int(road.get("congestion", 0)) > 0:
		return {"ok": false, "reason": "ROAD_IN_USE"}

	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	roads.erase(str(road_id))
	_unlink_nodes_for_segment(state, road)
	EconomyMapRules.sync_road_segment_refs(state)
	EconomyState.emit_event(state, "road_removed", {"road_id": road_id})
	return {"ok": true, "reason": ""}


static func create_building_road_node(
	state: GameState,
	building_id: int,
	q: int,
	r: int,
	player_id: String,
) -> int:
	return _ensure_node_at_hex(state, HexCoord.new(q, r), player_id, building_id)


static func _ensure_node_at_hex(
	state: GameState,
	coord: HexCoord,
	player_id: String,
	building_id: int,
) -> int:
	var existing_id: int = _find_node_at(state, coord.q, coord.r)
	if existing_id > 0:
		if building_id > 0:
			var node: Dictionary = EconomyState.get_road_node(state, existing_id)
			node["building_id"] = building_id
			EconomyState.add_road_node(state, node)
		return existing_id

	var cfg: EconomyConfig = EconomyState.get_config(state)
	var node_id: int = state.entity_ids.allocate()
	var node := {
		"id": node_id,
		"q": coord.q,
		"r": coord.r,
		"building_id": building_id,
		"player_id": player_id,
		"segment_ids": [],
		"inventory": EconomyState.create_empty_inventory(cfg.int_val("road_node_buffer_capacity", 5)),
	}
	EconomyState.add_road_node(state, node)
	return node_id


static func _find_node_at(state: GameState, q: int, r: int) -> int:
	var nodes: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES)
	for key in nodes:
		var node: Dictionary = nodes[key]
		if int(node.get("q", -1)) == q and int(node.get("r", -1)) == r:
			return int(node.get("id", 0))
	return 0


static func _link_nodes_for_segment(state: GameState, road: Dictionary) -> void:
	var node_a: int = _find_node_at(state, int(road.get("hex_a_q", 0)), int(road.get("hex_a_r", 0)))
	var node_b: int = _find_node_at(state, int(road.get("hex_b_q", 0)), int(road.get("hex_b_r", 0)))
	var road_id: int = int(road.get("id", 0))
	for node_id in [node_a, node_b]:
		var node: Dictionary = EconomyState.get_road_node(state, node_id)
		var segment_ids: Array = node.get("segment_ids", [])
		if not segment_ids.has(road_id):
			segment_ids.append(road_id)
		node["segment_ids"] = segment_ids
		EconomyState.add_road_node(state, node)


static func _unlink_nodes_for_segment(state: GameState, road: Dictionary) -> void:
	var road_id: int = int(road.get("id", 0))
	var nodes: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES)
	for key in nodes:
		var node: Dictionary = nodes[key]
		var segment_ids: Array = node.get("segment_ids", [])
		segment_ids.erase(road_id)
		node["segment_ids"] = segment_ids
		EconomyState.add_road_node(state, node)


static func increment_congestion(state: GameState, road_id: int) -> void:
	var road: Dictionary = EconomyState.get_road(state, road_id)
	if road.is_empty():
		return
	road["congestion"] = int(road.get("congestion", 0)) + 1
	EconomyState.add_road(state, road)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	if road["congestion"] >= cfg.int_val("road_congestion_limit", 3):
		EconomyState.emit_event(state, "road_congested", {"road_id": road_id, "congestion": road["congestion"]})


static func decrement_congestion(state: GameState, road_id: int) -> void:
	var road: Dictionary = EconomyState.get_road(state, road_id)
	if road.is_empty():
		return
	road["congestion"] = maxi(0, int(road.get("congestion", 0)) - 1)
	EconomyState.add_road(state, road)


static func segment_between_nodes(state: GameState, from_node_id: int, to_node_id: int) -> int:
	var from_node: Dictionary = EconomyState.get_road_node(state, from_node_id)
	var to_node: Dictionary = EconomyState.get_road_node(state, to_node_id)
	if from_node.is_empty() or to_node.is_empty():
		return 0
	var hex_a := HexCoord.new(int(from_node.get("q", 0)), int(from_node.get("r", 0)))
	var hex_b := HexCoord.new(int(to_node.get("q", 0)), int(to_node.get("r", 0)))
	var road: Dictionary = get_road_between(state, hex_a, hex_b)
	return int(road.get("id", 0))


static func is_network_connected(state: GameState, from_node_id: int, to_node_id: int) -> bool:
	return not EconomyPathfinder.find_route(state, from_node_id, to_node_id).is_empty()


static func tick_congestion_decay(state: GameState) -> void:
	var roads: Dictionary = EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS)
	for key in roads:
		var road: Dictionary = roads[key]
		if int(road.get("congestion", 0)) > 0:
			road["congestion"] = maxi(0, int(road["congestion"]) - 1)
			EconomyState.add_road(state, road)
