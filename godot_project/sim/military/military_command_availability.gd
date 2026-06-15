class_name MilitaryCommandAvailability
extends RefCounted

const MAX_PLACE_CANDIDATES: int = 12
const MAX_MOVE_CANDIDATES: int = 18
const PLACE_TYPES: Array[String] = ["power_plant", "resource_processor", "barracks"]
const QUEUE_UNIT_TYPES: Array[String] = ["infantry", "scout"]


static func list_candidates(state: GameState, actor_id: String) -> Array:
	var results: Array = []
	MilitaryState.ensure_buckets(state)
	var tick: int = state.tick

	_try_place_buildings(results, state, actor_id, tick)
	_try_queue_units(results, state, actor_id, tick)
	_try_move_units(results, state, actor_id, tick)
	_try_stop_units(results, state, actor_id, tick)
	return results


static func _hub_coord(state: GameState, actor_id: String) -> HexCoord:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == "command_hub":
			if str(building.get("player_id", "")) == actor_id:
				return HexCoord.new(int(building.get("q", 0)), int(building.get("r", 0)))
	return HexCoord.new(state.map.width / 2, state.map.height / 2)


static func _try_place_buildings(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	var hub: HexCoord = _hub_coord(state, actor_id)
	var build_radius: int = int(MilitaryState.get_meta_value(state, "build_radius", 10))
	var added: int = 0
	for building_type in PLACE_TYPES:
		for coord in _hexes_near(state, hub, build_radius):
			if added >= MAX_PLACE_CANDIDATES:
				return
			var payload: Dictionary = {
				"building_type": building_type,
				"q": coord.q,
				"r": coord.r,
				"player_id": actor_id,
			}
			var cmd := PlaceBuildingCommand.new(actor_id, tick, payload)
			if cmd.validate(state).success:
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Place %s at %d,%d" % [building_type, coord.q, coord.r],
					{"q": coord.q, "r": coord.r, "building_type": building_type},
				))
				added += 1


static func _try_queue_units(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != actor_id:
			continue
		if str(building.get("building_type", "")) != "barracks":
			continue
		var building_id: int = int(building.get("id", 0))
		for unit_type in QUEUE_UNIT_TYPES:
			var payload: Dictionary = {
				"building_id": building_id,
				"unit_type": unit_type,
				"player_id": actor_id,
			}
			var cmd := QueueUnitCommand.new(actor_id, tick, payload)
			if cmd.validate(state).success:
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Queue %s at barracks %d" % [unit_type, building_id],
					{"building_id": building_id, "unit_type": unit_type},
				))


static func _try_move_units(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	var added: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != actor_id:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		var unit_id: int = int(unit.get("id", 0))
		var uq: int = int(unit.get("q", 0))
		var ur: int = int(unit.get("r", 0))
		for coord in _hexes_near(state, HexCoord.new(uq, ur), 2):
			if added >= MAX_MOVE_CANDIDATES:
				return
			var payload: Dictionary = {"unit_id": unit_id, "q": coord.q, "r": coord.r, "player_id": actor_id}
			var cmd := MoveUnitCommand.new(actor_id, tick, payload)
			if cmd.validate(state).success:
				results.append(CommandAvailabilityDto.from_command(
					cmd,
					"Move unit %d to %d,%d" % [unit_id, coord.q, coord.r],
					{"unit_id": unit_id, "q": coord.q, "r": coord.r},
				))
				added += 1


static func _try_stop_units(results: Array, state: GameState, actor_id: String, tick: int) -> void:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) != actor_id:
			continue
		if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
			continue
		var unit_id: int = int(unit.get("id", 0))
		var payload: Dictionary = {"unit_id": unit_id, "player_id": actor_id}
		var cmd := StopUnitCommand.new(actor_id, tick, payload)
		if cmd.validate(state).success:
			results.append(CommandAvailabilityDto.from_command(
				cmd,
				"Stop unit %d" % unit_id,
				{"unit_id": unit_id},
			))


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
