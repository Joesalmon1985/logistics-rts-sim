class_name EconomyExpansionBot
extends Bot

## Rule-based economy expansion bot using snapshot + legal commands only (RUN_02.8).

const ACTOR_PREFIX: String = "economy_expansion_bot"

var _observation: EconomyBotObservation = null
var _decision_log: Array = []
var _reject_streak: int = 0
var _candidate_index: int = 0
var _sorted_candidates: Array = []


func _init(p_player_id: String = "p1") -> void:
	actor_id = "%s_%s" % [ACTOR_PREFIX, p_player_id]
	player_id = p_player_id


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	actor_id = "%s_%s" % [ACTOR_PREFIX, p_player_id]
	_decision_log.clear()
	_reject_streak = 0
	_candidate_index = 0
	_sorted_candidates.clear()


func get_decisions() -> Array:
	return _decision_log


func observe(snapshot: Dictionary, p_player_id: String) -> void:
	super.observe(snapshot, p_player_id)
	_observation = EconomyBotObservation.from_snapshot(snapshot, player_id)


func is_complete(_snapshot: Dictionary) -> bool:
	return false


func decide(_tick: int) -> SimCommand:
	if _observation == null:
		return null

	if str(_observation.diagnostics().get("stall", {}).get("primary_reason", "")) != "":
		pass

	_refresh_candidates()
	if _sorted_candidates.is_empty():
		return _noop_command()

	var attempts: int = mini(_sorted_candidates.size(), 12)
	for _i in range(attempts):
		if _candidate_index >= _sorted_candidates.size():
			_candidate_index = 0
		var entry: Dictionary = _sorted_candidates[_candidate_index]
		_candidate_index += 1
		var cmd: SimCommand = _command_from_entry(entry)
		if cmd != null:
			_log_decision(cmd.type, str(entry.get("label", "")))
			return cmd

	return _noop_command()


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if result.success:
		_reject_streak = 0
	else:
		_reject_streak += 1


func _refresh_candidates() -> void:
	_sorted_candidates.clear()
	var road_cmds: Array = []
	var building_cmds: Array = []
	var other_cmds: Array = []

	for entry in _observation.legal_commands:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var cmd_type: String = str(entry.get("type", entry.get("command_type", "")))
		if cmd_type == "SetWorkerPolicyCommand":
			continue
		if cmd_type == "NoopCommand":
			other_cmds.append(entry)
			continue
		match cmd_type:
			"PlaceRoadCommand":
				road_cmds.append(entry)
			"PlaceBuildingPlanCommand":
				building_cmds.append(entry)
			_:
				other_cmds.append(entry)

	_sort_entries(road_cmds)
	_sort_entries(building_cmds)
	_sort_entries(other_cmds)

	var pending_sites: int = _observation.own_construction_sites().size()
	var connect_roads: Array = _roads_connecting_sites(road_cmds)
	var network_size: int = _observation.own_roads().size() + _observation.own_road_nodes().size()

	if pending_sites > 0:
		if _sites_have_delivery_progress():
			_sorted_candidates = _noop_entries(other_cmds)
			return
		if not connect_roads.is_empty():
			_sorted_candidates = connect_roads
		elif network_size > 40:
			_sorted_candidates = _noop_entries(other_cmds)
		else:
			var road_push: Array = _road_toward_forests(road_cmds)
			road_push.append_array(road_cmds.slice(0, mini(road_cmds.size(), 6)))
			_sorted_candidates = road_push
		return

	if network_size < 8:
		var road_first: Array = _road_toward_forests(road_cmds)
		road_first.append_array(road_cmds)
		_sorted_candidates = road_first
		return

	if pending_sites >= 1:
		return

	if not _observation.has_building_type("woodcutter") and not _has_pending_type("woodcutter"):
		_sorted_candidates.append_array(_filter_building_entries(building_cmds, "woodcutter"))
	if not _observation.has_building_type("sawmill") and not _has_pending_type("sawmill"):
		_sorted_candidates.append_array(_filter_building_entries(building_cmds, "sawmill"))
	if not _observation.has_building_type("storehouse") and not _has_pending_type("storehouse"):
		_sorted_candidates.append_array(_filter_building_entries(building_cmds, "storehouse"))
	if not _observation.has_building_type("workshop") and not _has_pending_type("workshop"):
		_sorted_candidates.append_array(_filter_building_entries(building_cmds, "workshop"))

	_sorted_candidates.append_array(building_cmds)
	if _sorted_candidates.is_empty() and not road_cmds.is_empty():
		_sorted_candidates = _road_toward_forests(road_cmds).slice(0, mini(road_cmds.size(), 6))
	_sorted_candidates.append_array(_noop_entries(other_cmds))


func _sites_have_delivery_progress() -> bool:
	for site in _observation.own_construction_sites():
		for ware_type in site.get("delivered", {}):
			if int(site.get("delivered", {}).get(ware_type, 0)) > 0:
				return true
		if int(site.get("progress_percent", 0)) > 0:
			return true
	return false


func _roads_connecting_sites(road_cmds: Array) -> Array:
	var result: Array = []
	for entry in road_cmds:
		var label: String = str(entry.get("label", ""))
		if "Connect site" in label:
			result.append(entry)
	return result


func _noop_entries(other_cmds: Array) -> Array:
	for entry in other_cmds:
		if str(entry.get("type", entry.get("command_type", ""))) == "NoopCommand":
			return [entry]
	return []


func _road_toward_forests(road_cmds: Array) -> Array:
	var forests: Array = _observation.forest_hexes()
	if forests.is_empty():
		return []
	var target: Vector2i = forests[0]
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


func _filter_building_entries(entries: Array, building_type: String) -> Array:
	var result: Array = []
	for entry in entries:
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		if str(params.get("building_type", "")) == building_type:
			result.append(entry)
	return result


func _has_pending_type(building_type: String) -> bool:
	for site in _observation.own_construction_sites():
		if str(site.get("building_type", "")) == building_type:
			return true
	return false


func _sort_entries(entries: Array) -> void:
	entries.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return _entry_sort_key(a) < _entry_sort_key(b)
	)


func _entry_sort_key(entry: Dictionary) -> String:
	var params: Dictionary = entry.get("params", entry.get("payload", {}))
	var q: int = int(params.get("q", params.get("to_q", 0)))
	var r: int = int(params.get("r", params.get("to_r", 0)))
	var cmd_type: String = str(entry.get("command_type", entry.get("type", "")))
	return "%s,%03d,%03d,%s" % [cmd_type, q, r, str(params.get("building_type", ""))]


func _command_from_entry(entry: Dictionary) -> SimCommand:
	var cmd_type: String = str(entry.get("command_type", entry.get("type", "")))
	var params: Dictionary = entry.get("params", entry.get("payload", {})).duplicate(true)
	params["player_id"] = player_id
	match cmd_type:
		"PlaceRoadCommand":
			return PlaceRoadCommand.new(player_id, _observation.tick, params)
		"PlaceBuildingPlanCommand":
			return PlaceBuildingPlanCommand.new(player_id, _observation.tick, params)
		"SetWorkerPolicyCommand":
			return SetWorkerPolicyCommand.new(player_id, _observation.tick, params)
		"NoopCommand":
			return NoopCommand.new(player_id, _observation.tick)
	return null


func _noop_command() -> SimCommand:
	return NoopCommand.new(player_id, _observation.tick)


func _log_decision(command_type: String, reason: String) -> void:
	_decision_log.append({
		"tick": _observation.tick,
		"actor_id": actor_id,
		"chosen_command": command_type,
		"reason": reason,
	})
