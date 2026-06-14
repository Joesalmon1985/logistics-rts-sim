class_name DisableMilitaryBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("DisableMilitaryBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	return EnableMilitaryBuildingCommand._validate_owned_building(state, player_id, int(payload.get("building_id", 0)))


func apply(state: GameState) -> CommandResult:
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	building["enabled"] = 0
	return CommandResult.ok(true, [
		{"type": "building_disabled", "tick": tick, "payload": {"building_id": int(building.get("id", 0))}},
	])
