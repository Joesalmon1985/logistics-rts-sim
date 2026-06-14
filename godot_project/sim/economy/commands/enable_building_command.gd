class_name EnableBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("EnableBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail("BUILDING_NOT_FOUND", "Building not found: %d" % building_id)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Building owned by another player")
	if int(building.get("enabled", 1)) == 1:
		return CommandResult.fail("ALREADY_ENABLED", "Building already enabled")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	building["enabled"] = 1
	building["state"] = EconomyConstants.BLD_ACTIVE
	building["paused_reason"] = ""
	EconomyState.add_building(state, building)

	var events: Array = [{
		"type": "production_started",
		"tick": tick,
		"payload": {
			"building_id": building_id,
			"building_type": str(building.get("building_type", "")),
		},
	}]
	return CommandResult.ok(true, events)
