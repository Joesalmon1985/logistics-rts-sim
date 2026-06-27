class_name TutorialMilitaryBot
extends Bot

## Deterministic bot for Skirmish MVP objective (MRP Phase B4).

const ACTOR: String = "tutorial_military_bot"

var _plan_index: int = 0
var _decision_log: Array = []
var _attacked_unit_ids: Array = []
var _enemy_hub_q: int = 12
var _enemy_hub_r: int = 12
var _stall_ticks: int = 0
var _last_progress_tick: int = 0


func _init() -> void:
	actor_id = ACTOR


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_plan_index = 0
	_decision_log.clear()
	_attacked_unit_ids.clear()
	_enemy_hub_q = 12
	_enemy_hub_r = 12
	_stall_ticks = 0
	_last_progress_tick = 0


func get_decisions() -> Array:
	return _decision_log


func is_complete(snapshot: Dictionary) -> bool:
	var military: Dictionary = snapshot.get("diagnostics", {}).get("military", {})
	if str(military.get("victory_state", "")) != MilitaryConstants.VICTORY_WON:
		return false
	return true


func decide(_tick: int) -> SimCommand:
	_update_enemy_hub_from_snapshot()

	if is_complete(_last_snapshot):
		return null

	if _ready_combat_units() >= 1:
		var rush_cmd: SimCommand = _next_attack_command()
		if rush_cmd != null:
			return rush_cmd

	var build_cmd: SimCommand = _next_build_command()
	if build_cmd != null:
		return build_cmd

	var prod_cmd: SimCommand = _next_production_command()
	if prod_cmd != null:
		return prod_cmd

	if _ready_combat_units() >= 2:
		var attack_cmd: SimCommand = _next_attack_command()
		if attack_cmd != null:
			return attack_cmd

	return null


func on_command_applied(command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	if result.success:
		_last_progress_tick = _tick_from_snapshot()
	match command.type:
		"PlaceBuildingCommand":
			if result.success or str(result.error_code) in ["BUILDING_EXISTS", "SITE_EXISTS"]:
				_plan_index += 1
			elif _has_building_or_site(str(command.payload.get("building_type", ""))):
				_plan_index += 1
		"QueueUnitCommand":
			if result.success:
				_last_progress_tick = _tick_from_snapshot()
		"AttackMoveCommand", "AttackUnitCommand", "MoveUnitCommand":
			if result.success:
				_last_progress_tick = _tick_from_snapshot()


func get_stall_ticks_since_progress(current_tick: int) -> int:
	return current_tick - _last_progress_tick


func _build_plan() -> Array:
	return [
		{"type": "place_building", "building_type": "power_generator", "q": 6, "r": 3},
		{"type": "place_building", "building_type": "resource_processor", "q": 6, "r": 6},
		{"type": "place_building", "building_type": "barracks", "q": 9, "r": 3},
	]


func _next_build_command() -> SimCommand:
	for action in _build_plan():
		var building_type: String = str(action.get("building_type", ""))
		if _has_active_building(building_type) or _has_building_or_site(building_type):
			continue
		_log_decision("PlaceBuildingCommand", "place %s" % building_type)
		return PlaceBuildingCommand.new(
			player_id,
			_tick_from_snapshot(),
			{
				"building_type": building_type,
				"q": int(action.get("q", 0)),
				"r": int(action.get("r", 0)),
			},
		)
	return null


func _next_production_command() -> SimCommand:
	if not _has_active_building("barracks"):
		return null
	var barracks_id: int = _building_id("barracks")
	if barracks_id <= 0:
		return null
	if _count_units("rifle_squad") + _queued_units("rifle_squad") >= 6:
		return null
	if _queue_has_unit(barracks_id, "rifle_squad"):
		return null
	_log_decision("QueueUnitCommand", "queue rifle_squad")
	return QueueUnitCommand.new(
		player_id,
		_tick_from_snapshot(),
		{"building_id": barracks_id, "unit_type": "rifle_squad"},
	)


func _queued_units(unit_type: String) -> int:
	var count: int = 0
	for queue in _last_snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		for item in queue.get("items", []):
			if typeof(item) == TYPE_DICTIONARY and str(item.get("unit_type", "")) == unit_type:
				count += 1
	return count


func _queue_has_unit(building_id: int, unit_type: String) -> bool:
	for queue in _last_snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		if int(queue.get("building_id", 0)) != building_id:
			continue
		for item in queue.get("items", []):
			if typeof(item) == TYPE_DICTIONARY and str(item.get("unit_type", "")) == unit_type:
				return true
	return false


func _next_attack_command() -> SimCommand:
	for unit in _all_units():
		if str(unit.get("player_id", "")) != player_id:
			continue
		if str(unit.get("unit_type", "")) != "rifle_squad":
			continue
		var unit_id: int = int(unit.get("id", 0))
		if unit_id in _attacked_unit_ids:
			continue
		_attacked_unit_ids.append(unit_id)
		_log_decision("AttackMoveCommand", "rush enemy hub with unit %d" % unit_id)
		return AttackMoveCommand.new(
			player_id,
			_tick_from_snapshot(),
			{"unit_id": unit_id, "q": _enemy_hub_q, "r": _enemy_hub_r},
		)
	return null


func _update_enemy_hub_from_snapshot() -> void:
	for building in _last_snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "command_hub":
			if str(building.get("player_id", "")) != player_id:
				_enemy_hub_q = int(building.get("anchor_q", building.get("q", _enemy_hub_q)))
				_enemy_hub_r = int(building.get("anchor_r", building.get("r", _enemy_hub_r)))


func _has_active_building(building_type: String) -> bool:
	for building in _all_buildings():
		if str(building.get("building_type", "")) == building_type:
			if str(building.get("state", "")) == MilitaryConstants.BLD_ACTIVE:
				return true
	return false


func _has_building_or_site(building_type: String) -> bool:
	for building in _all_buildings():
		if str(building.get("building_type", "")) == building_type:
			return true
	for site in _last_snapshot.get("construction_sites", []):
		if str(site.get("building_type", "")) == building_type:
			return true
	return false


func _building_id(building_type: String) -> int:
	for building in _all_buildings():
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0


func _count_units(unit_type: String) -> int:
	var count: int = 0
	for unit in _all_units():
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			count += 1
	return count


func _ready_combat_units() -> int:
	var count: int = 0
	for unit in _all_units():
		if str(unit.get("player_id", "")) != player_id:
			continue
		if str(unit.get("unit_type", "")) == "rifle_squad":
			if str(unit.get("state", "")) in [MilitaryConstants.UNIT_IDLE, MilitaryConstants.UNIT_MOVING]:
				count += 1
	return count


func _all_buildings() -> Array:
	var out: Array = []
	var entities: Dictionary = _last_snapshot.get("entities", {})
	if entities.has("mil_buildings"):
		for key in entities["mil_buildings"]:
			out.append(entities["mil_buildings"][key])
	out.append_array(_last_snapshot.get("buildings", []))
	return out


func _all_units() -> Array:
	var out: Array = []
	var entities: Dictionary = _last_snapshot.get("entities", {})
	if entities.has("mil_units"):
		for key in entities["mil_units"]:
			out.append(entities["mil_units"][key])
	out.append_array(_last_snapshot.get("units", []))
	return out


func _tick_from_snapshot() -> int:
	return int(_last_snapshot.get("tick", 0))


func _log_decision(chosen: String, reason: String) -> void:
	_decision_log.append({
		"tick": _tick_from_snapshot(),
		"actor_id": actor_id,
		"legal_actions_count": 1,
		"chosen_command": chosen,
		"reason": reason,
		"expected_effect": chosen,
	})
