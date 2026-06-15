class_name EconomyCommandAvailability
extends RefCounted

const MAX_ROAD_CANDIDATES: int = 32
const MAX_BUILDING_CANDIDATES: int = 16

const BUILDING_TYPES: Array[String] = ["storehouse", "woodcutter", "sawmill"]


static func list_candidates(state: GameState, actor_id: String) -> Array:
	var results: Array = []
	EconomyState.ensure_buckets(state)
	var tick: int = state.tick

	_try_policy(results, state, actor_id, tick)
	_try_roads(results, state, actor_id, tick)
	_try_buildings(results, state, actor_id, tick)
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


static func _try_roads(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	var added: int = 0
	var seen: Dictionary = {}
	var start_hexes: Array[HexCoord] = _road_start_hexes(state, actor_id)
	for start in start_hexes:
		if added >= MAX_ROAD_CANDIDATES:
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
			var cmd := PlaceRoadCommand.new(actor_id, tick, payload)
			if cmd.validate(state).success:
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


static func _road_start_hexes(state: GameState, actor_id: String) -> Array[HexCoord]:
	var coords: Array[HexCoord] = []
	var seen: Dictionary = {}
	for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
		if typeof(node) != TYPE_DICTIONARY:
			continue
		if str(node.get("player_id", "")) != actor_id:
			continue
		var coord := HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))
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
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	for coord2 in _hexes_near(state, HexCoord.new(hq_q, hq_r), 2):
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


static func _try_buildings(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var added: int = 0
	var road_adjacent: Array[HexCoord] = _road_adjacent_build_hexes(state, actor_id, build_radius)

	for building_type in BUILDING_TYPES:
		var coords: Array[HexCoord] = road_adjacent if building_type == "storehouse" else _hexes_near(state, HexCoord.new(hq_q, hq_r), build_radius)
		for coord in coords:
			if added >= MAX_BUILDING_CANDIDATES:
				break
			var payload: Dictionary = {
				"building_type": building_type,
				"q": coord.q,
				"r": coord.r,
				"player_id": actor_id,
			}
			var cmd := PlaceBuildingPlanCommand.new(actor_id, tick, payload)
			if cmd.validate(state).success:
				var display_name: String = building_type.replace("_", " ").capitalize()
				var suffix: String = " (road-adjacent)" if building_type == "storehouse" and _is_road_adjacent(state, coord) else ""
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Buildings: Place %s Construction Site at %d,%d%s" % [display_name, coord.q, coord.r, suffix],
					{"q": coord.q, "r": coord.r, "building_type": building_type},
				))
				added += 1


static func _road_adjacent_build_hexes(state: GameState, actor_id: String, build_radius: int) -> Array[HexCoord]:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var coords: Array[HexCoord] = []
	for coord in _hexes_near(state, HexCoord.new(hq_q, hq_r), build_radius):
		if _is_road_adjacent(state, coord):
			coords.append(coord)
	return coords


static func _is_road_adjacent(state: GameState, coord: HexCoord) -> bool:
	for neighbor in coord.neighbors():
		if not state.map.in_bounds(neighbor):
			continue
		for node in EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROAD_NODES).values():
			if int(node.get("q", -1)) == neighbor.q and int(node.get("r", -1)) == neighbor.r:
				return true
		if EconomyRoadSystem.has_road_between(state, coord, neighbor) or EconomyRoadSystem.has_road_between(state, neighbor, coord):
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
