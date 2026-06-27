class_name SetBuildingStanceCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetBuildingStanceCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "SetBuildingStanceCommand requires hybrid variant")
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = MilitaryState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	var stance: String = str(payload.get("stance", HybridConstants.STANCE_DEFENSIVE))
	if stance not in [HybridConstants.STANCE_PASSIVE, HybridConstants.STANCE_DEFENSIVE, HybridConstants.STANCE_AGGRESSIVE, HybridConstants.STANCE_HOLD]:
		return CommandResult.fail("INVALID_STANCE")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var stance: String = str(payload.get("stance", HybridConstants.STANCE_DEFENSIVE))
	HybridState.set_building_policy(state, building_id, {"stance": stance})
	return CommandResult.ok(true, [
		{"type": "stance_changed", "tick": tick, "payload": {
			"building_id": building_id,
			"stance": stance,
			"player_id": player_id,
		}},
	])
