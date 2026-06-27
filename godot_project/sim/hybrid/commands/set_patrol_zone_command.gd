class_name SetPatrolZoneCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetPatrolZoneCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "SetPatrolZoneCommand requires hybrid variant")
	var waypoints: Array = payload.get("waypoints", [])
	if waypoints.is_empty():
		return CommandResult.fail("MISSING_WAYPOINTS")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var order_id: int = HybridState.add_delegated_order(state, {
		"type": HybridConstants.ORDER_PATROL,
		"player_id": player_id,
		"building_id": int(payload.get("building_id", 0)),
		"waypoints": payload.get("waypoints", []).duplicate(true),
		"active": 1,
	})
	return CommandResult.ok(true, [
		{"type": "delegated_order_issued", "tick": tick, "payload": {
			"order_id": order_id,
			"order_type": HybridConstants.ORDER_PATROL,
		}},
	])
