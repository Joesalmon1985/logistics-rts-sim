class_name EnableMilitaryBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("EnableMilitaryBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	return _validate_owned_building(state, player_id, int(payload.get("building_id", 0)))


func apply(state: GameState) -> CommandResult:
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	building["enabled"] = 1
	return CommandResult.ok(true, [
		{"type": "building_enabled", "tick": tick, "payload": {"building_id": int(building.get("id", 0))}},
	])


static func _validate_owned_building(state: GameState, player_id: String, building_id: int) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var building: Dictionary = MilitaryState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	return CommandResult.ok()
