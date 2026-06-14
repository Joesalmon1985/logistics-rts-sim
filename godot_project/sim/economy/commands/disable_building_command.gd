class_name DisableBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("DisableBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail("BUILDING_NOT_FOUND", "Building not found: %d" % building_id)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Building owned by another player")
	if int(building.get("enabled", 1)) == 0:
		return CommandResult.fail("ALREADY_DISABLED", "Building already disabled")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	building["enabled"] = 0
	building["state"] = EconomyConstants.BLD_DISABLED
	building["paused_reason"] = "disabled_by_player"
	EconomyState.add_building(state, building)

	var events: Array = [{
		"type": "production_paused",
		"tick": tick,
		"payload": {
			"building_id": building_id,
			"building_type": str(building.get("building_type", "")),
			"reason": "disabled_by_player",
		},
	}]
	return CommandResult.ok(true, events)
