class_name SetProductionPriorityCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetProductionPriorityCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail("BUILDING_NOT_FOUND", "Building not found: %d" % building_id)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Building owned by another player")
	if not payload.has("priority"):
		return CommandResult.fail("INVALID_PAYLOAD", "Missing priority")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var priority: int = int(payload.get("priority", 0))
	EconomyPrioritySystem.set_production_priority(state, building_id, priority)

	var events: Array = [{
		"type": "production_priority_changed",
		"tick": tick,
		"payload": {
			"building_id": building_id,
			"priority": priority,
		},
	}]
	return CommandResult.ok(true, events)
