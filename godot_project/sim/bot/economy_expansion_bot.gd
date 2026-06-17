class_name EconomyExpansionBot
extends Bot

## Rule-based economy expansion bot using snapshot + legal-command shortlist only (RUN_02.8).

const ACTOR_PREFIX: String = "economy_expansion_bot"
const MAX_BOOTSTRAP_ROADS: int = 12
const MAX_TOTAL_ROAD_SEGMENTS: int = 45
const CHAIN_TYPES: Array[String] = ["woodcutter", "storehouse", "forester", "sawmill", "workshop"]

var _observation: EconomyBotObservation = null
var _decision_log: Array = []
var _road_queue: Array = []
var _chain_index: int = 0


func _init(p_player_id: String = "p1") -> void:
	actor_id = "%s_%s" % [ACTOR_PREFIX, p_player_id]
	player_id = p_player_id


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	actor_id = "%s_%s" % [ACTOR_PREFIX, p_player_id]
	_decision_log.clear()
	_road_queue.clear()
	_chain_index = 0


func get_decisions() -> Array:
	return _decision_log


func observe(snapshot: Dictionary, p_player_id: String) -> void:
	super.observe(snapshot, p_player_id)
	_observation = EconomyBotObservation.from_snapshot(snapshot, player_id)
	_advance_chain_index()


func is_complete(_snapshot: Dictionary) -> bool:
	return false


func decide(_tick: int) -> SimCommand:
	if _observation == null:
		return null

	var road_cmds: Array = _commands_of_type("PlaceRoadCommand")
	var building_cmds: Array = _commands_of_type("PlaceBuildingPlanCommand")

	if not _has_active_construction() and _chain_index < CHAIN_TYPES.size():
		var chain_type: String = CHAIN_TYPES[_chain_index]
		var chain_target: Vector2i = _target_for_type(chain_type)
		if chain_target.x >= 0 and _hex_has_road_adjacency(chain_target):
			var chain_build: SimCommand = _pick_building_at(
				building_cmds, chain_type, chain_target.x, chain_target.y
			)
			if chain_build != null:
				_road_queue.clear()
				_log_decision(chain_build.type, "place %s" % chain_type)
				return chain_build

	if not _road_queue.is_empty():
		var queued: SimCommand = _next_queued_road(road_cmds)
		if queued != null:
			_log_decision(queued.type, "queued road segment")
			return queued

	if _has_active_construction():
		var connect: Array = _roads_connecting_sites(road_cmds)
		if not connect.is_empty():
			return _command_from_entry(connect[0])
		if _sites_have_delivery_progress():
			return _noop_command()
		if _road_segments() < MAX_TOTAL_ROAD_SEGMENTS:
			var toward_site: Array = _roads_toward_active_sites(road_cmds)
			if not toward_site.is_empty():
				return _command_from_entry(toward_site[0])
			if not road_cmds.is_empty():
				return _command_from_entry(road_cmds[0])
		return _noop_command()

	if _chain_index >= CHAIN_TYPES.size():
		return _noop_command()

	var building_type: String = CHAIN_TYPES[_chain_index]
	var target: Vector2i = _target_for_type(building_type)
	if target.x < 0:
		return _noop_command()

	if not _hex_has_road_adjacency(target):
		if building_type in ["storehouse", "sawmill", "workshop"]:
			var connected_build: SimCommand = _pick_building_at(building_cmds, building_type, -1, -1)
			if connected_build != null:
				_road_queue.clear()
				_log_decision(connected_build.type, "place %s at connected site" % building_type)
				return connected_build
		if _road_segments() >= MAX_TOTAL_ROAD_SEGMENTS:
			if building_type != "woodcutter":
				var cap_build: SimCommand = _pick_building_at(building_cmds, building_type, -1, -1)
				if cap_build != null:
					_road_queue.clear()
					_log_decision(cap_build.type, "place %s at cap" % building_type)
					return cap_build
			return _noop_command()
		if _road_queue.is_empty():
			_queue_roads_to(target.x, target.y)
		var bootstrap: SimCommand = _next_queued_road(road_cmds)
		if bootstrap != null:
			_log_decision(bootstrap.type, "bootstrap road toward %s" % building_type)
			return bootstrap
		var toward: Array = _roads_toward_hex(road_cmds, target)
		if not toward.is_empty():
			return _command_from_entry(toward[0])
		if not road_cmds.is_empty():
			return _command_from_entry(road_cmds[0])
		return _noop_command()

	var build_cmd: SimCommand = _pick_building_at(building_cmds, building_type, target.x, target.y)
	if build_cmd == null:
		build_cmd = _pick_building_at(building_cmds, building_type, -1, -1)
	if build_cmd != null:
		_log_decision(build_cmd.type, "place %s" % building_type)
		return build_cmd

	if _road_segments() < MAX_TOTAL_ROAD_SEGMENTS and not road_cmds.is_empty():
		return _command_from_entry(road_cmds[0])
	return _noop_command()


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	if command.type == "PlaceRoadCommand" and not _road_queue.is_empty():
		if result.success or str(result.error_code) == "ROAD_EXISTS":
			_road_queue.pop_front()


func _advance_chain_index() -> void:
	var previous_index: int = _chain_index
	while _chain_index < CHAIN_TYPES.size():
		if not _observation.has_completed_building_type(CHAIN_TYPES[_chain_index]):
			break
		_chain_index += 1
	if _chain_index != previous_index:
		_road_queue.clear()


func _target_for_type(building_type: String) -> Vector2i:
	var forests: Array = _observation.forest_hexes()
	var hq: Vector2i = _observation.hq_position()
	match building_type:
		"woodcutter":
			return _pick_forest_hex(forests, 0)
		"forester":
			return _pick_forest_hex(forests, 1)
		"storehouse":
			return Vector2i(hq.x + 1, hq.y)
		"sawmill":
			return Vector2i(hq.x + 2, hq.y)
		"workshop":
			return Vector2i(hq.x + 3, hq.y + 1)
	return Vector2i(hq.x + 2, hq.y)


func _pick_forest_hex(forests: Array, preferred_index: int) -> Vector2i:
	if forests.is_empty():
		return Vector2i(-1, -1)
	if preferred_index >= 0 and preferred_index < forests.size():
		var preferred: Vector2i = forests[preferred_index]
		if not _hex_has_structure(preferred):
			return preferred
	for hex in forests:
		if not _hex_has_structure(hex):
			return hex
	return Vector2i(-1, -1)


func _hex_has_structure(hex: Vector2i) -> bool:
	if hex.x < 0:
		return false
	for building in _observation.own_buildings():
		if int(building.get("anchor_q", building.get("q", -99))) == hex.x \
				and int(building.get("anchor_r", building.get("r", -99))) == hex.y:
			return true
	for site in _observation.own_construction_sites():
		if int(site.get("anchor_q", site.get("q", -99))) == hex.x \
				and int(site.get("anchor_r", site.get("r", -99))) == hex.y:
			return true
	return false


func _commands_of_type(cmd_type: String) -> Array:
	var result: Array = []
	for entry in _observation.legal_commands:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		if str(entry.get("type", entry.get("command_type", ""))) == cmd_type:
			result.append(entry)
	result.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return _entry_sort_key(a) < _entry_sort_key(b)
	)
	return result


func _has_active_construction() -> bool:
	for site in _observation.own_construction_sites():
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			return true
	return false


func _sites_have_delivery_progress() -> bool:
	for site in _observation.own_construction_sites():
		for ware_type in site.get("delivered", {}):
			if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
				return true
		if int(site.get("builder_work_remaining", 0)) < int(site.get("required", {}).get("builder_work", 99)):
			return true
	return false


func _roads_connecting_sites(road_cmds: Array) -> Array:
	var result: Array = []
	for entry in road_cmds:
		if "Connect site" in str(entry.get("label", "")):
			result.append(entry)
	return result


func _road_segments() -> int:
	return _observation.own_roads().size() + _observation.own_road_nodes().size()


func _hex_has_road_adjacency(hex: Vector2i) -> bool:
	if hex.x < 0:
		return false
	for road in _observation.own_roads():
		for end in [
			Vector2i(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99)))),
			Vector2i(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99)))),
		]:
			if HexCoord.new(hex.x, hex.y).distance(HexCoord.new(end.x, end.y)) <= 1:
				return true
	for node in _observation.own_road_nodes():
		if HexCoord.new(hex.x, hex.y).distance(HexCoord.new(int(node.get("q", -99)), int(node.get("r", -99)))) <= 1:
			return true
	return false


func _roads_toward_active_sites(road_cmds: Array) -> Array:
	for site in _observation.own_construction_sites():
		var q: int = int(site.get("anchor_q", site.get("q", -1)))
		var r: int = int(site.get("anchor_r", site.get("r", -1)))
		if q >= 0:
			var scored: Array = _roads_toward_hex(road_cmds, Vector2i(q, r))
			if not scored.is_empty():
				return scored
	return []


func _roads_toward_hex(road_cmds: Array, target: Vector2i) -> Array:
	if target.x < 0:
		return []
	var scored: Array = []
	for entry in road_cmds:
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		var q: int = int(params.get("to_q", params.get("q", 0)))
		var r: int = int(params.get("to_r", params.get("r", 0)))
		var dist: int = HexCoord.new(q, r).distance(HexCoord.new(target.x, target.y))
		scored.append({"entry": entry, "dist": dist})
	scored.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		if int(a.get("dist", 0)) != int(b.get("dist", 0)):
			return int(a.get("dist", 0)) < int(b.get("dist", 0))
		return _entry_sort_key(a.get("entry", {})) < _entry_sort_key(b.get("entry", {}))
	)
	var result: Array = []
	for item in scored:
		result.append(item.get("entry", {}))
	return result


func _queue_roads_to(target_q: int, target_r: int) -> void:
	if not _road_queue.is_empty():
		return
	var hq: Vector2i = _observation.hq_position()
	var current := HexCoord.new(hq.x, hq.y)
	var target := HexCoord.new(target_q, target_r)
	var guard: int = 0
	while (current.q != target.q or current.r != target.r) and guard < MAX_BOOTSTRAP_ROADS:
		guard += 1
		var best_neighbor: HexCoord = current
		var best_dist: int = current.distance(target)
		for neighbour in current.neighbors():
			var dist: int = neighbour.distance(target)
			if dist < best_dist:
				best_dist = dist
				best_neighbor = neighbour
		if best_neighbor.q == current.q and best_neighbor.r == current.r:
			break
		_road_queue.append({
			"from_q": current.q, "from_r": current.r,
			"to_q": best_neighbor.q, "to_r": best_neighbor.r,
		})
		current = best_neighbor


func _next_queued_road(road_cmds: Array) -> SimCommand:
	if _road_queue.is_empty():
		return null
	var segment: Dictionary = _road_queue[0]
	for entry in road_cmds:
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		if int(params.get("from_q", -1)) == int(segment.get("from_q", -2)) \
				and int(params.get("from_r", -1)) == int(segment.get("from_r", -2)) \
				and int(params.get("to_q", -1)) == int(segment.get("to_q", -2)) \
				and int(params.get("to_r", -1)) == int(segment.get("to_r", -2)):
			return _command_from_entry(entry)
	if not road_cmds.is_empty():
		_road_queue.pop_front()
	return null


func _pick_building_at(building_cmds: Array, building_type: String, q: int, r: int) -> SimCommand:
	for entry in building_cmds:
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		if str(params.get("building_type", "")) != building_type:
			continue
		if q >= 0 and (int(params.get("q", -1)) != q or int(params.get("r", -1)) != r):
			continue
		return _command_from_entry(entry)
	return null


func _command_from_entry(entry: Dictionary) -> SimCommand:
	var cmd_type: String = str(entry.get("command_type", entry.get("type", "")))
	var params: Dictionary = entry.get("params", entry.get("payload", {})).duplicate(true)
	params["player_id"] = player_id
	match cmd_type:
		"PlaceRoadCommand":
			return PlaceRoadCommand.new(player_id, _observation.tick, params)
		"PlaceBuildingPlanCommand":
			return PlaceBuildingPlanCommand.new(player_id, _observation.tick, params)
		"NoopCommand":
			return NoopCommand.new(player_id, _observation.tick)
	return null


func _noop_command() -> SimCommand:
	return NoopCommand.new(player_id, _observation.tick)


func _entry_sort_key(entry: Dictionary) -> String:
	var params: Dictionary = entry.get("params", entry.get("payload", {}))
	return "%s,%03d,%03d,%s" % [
		str(entry.get("command_type", entry.get("type", ""))),
		int(params.get("q", params.get("to_q", 0))),
		int(params.get("r", params.get("to_r", 0))),
		str(params.get("building_type", "")),
	]


func _log_decision(command_type: String, reason: String) -> void:
	_decision_log.append({
		"tick": _observation.tick,
		"actor_id": actor_id,
		"chosen_command": command_type,
		"reason": reason,
	})
