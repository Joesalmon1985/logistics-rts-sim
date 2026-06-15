class_name EconomyCommandAvailability
extends RefCounted

const MAX_ROAD_CANDIDATES: int = 24
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
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var origin := HexCoord.new(hq_q, hq_r)
	var added: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != actor_id:
			continue
		origin = HexCoord.new(
			int(building.get("anchor_q", building.get("q", hq_q))),
			int(building.get("anchor_r", building.get("r", hq_r))),
		)
		break

	var seen: Dictionary = {}
	for start in _hexes_near(state, origin, 2):
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
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Roads: Place road %d,%d -> %d,%d" % [start.q, start.r, neighbor.q, neighbor.r],
					{"from_q": start.q, "from_r": start.r, "to_q": neighbor.q, "to_r": neighbor.r},
				))
				added += 1


static func _try_buildings(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 0))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 0))
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var added: int = 0

	for building_type in BUILDING_TYPES:
		for coord in _hexes_near(state, HexCoord.new(hq_q, hq_r), build_radius):
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
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Buildings: Place %s Construction Site at %d,%d" % [display_name, coord.q, coord.r],
					{"q": coord.q, "r": coord.r, "building_type": building_type},
				))
				added += 1


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
