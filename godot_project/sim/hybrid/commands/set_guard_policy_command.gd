class_name SetGuardPolicyCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetGuardPolicyCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "SetGuardPolicyCommand requires hybrid variant")
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = MilitaryState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = MilitaryState.get_building(state, building_id)
	var order_id: int = HybridState.add_delegated_order(state, {
		"type": HybridConstants.ORDER_GUARD,
		"player_id": player_id,
		"building_id": building_id,
		"guard_q": int(payload.get("guard_q", building.get("anchor_q", 0))),
		"guard_r": int(payload.get("guard_r", building.get("anchor_r", 0))),
		"active": 1,
	})
	return CommandResult.ok(true, [
		{"type": "delegated_order_issued", "tick": tick, "payload": {
			"order_id": order_id,
			"order_type": HybridConstants.ORDER_GUARD,
		}},
	])
