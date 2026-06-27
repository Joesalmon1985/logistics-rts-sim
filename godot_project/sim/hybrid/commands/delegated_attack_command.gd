class_name DelegatedAttackCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("DelegatedAttackCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "DelegatedAttackCommand requires hybrid variant")
	var target_q: int = int(payload.get("target_q", 0))
	var target_r: int = int(payload.get("target_r", 0))
	if not HybridState.target_visible_to_player(state, player_id, target_q, target_r):
		return CommandResult.fail(HybridConstants.ERR_TARGET_NOT_VISIBLE)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var order_id: int = HybridState.add_delegated_order(state, {
		"type": HybridConstants.ORDER_ATTACK,
		"player_id": player_id,
		"target_q": int(payload.get("target_q", 0)),
		"target_r": int(payload.get("target_r", 0)),
		"building_id": int(payload.get("building_id", 0)),
		"active": 1,
	})
	return CommandResult.ok(true, [
		{"type": "delegated_order_issued", "tick": tick, "payload": {
			"order_id": order_id,
			"order_type": HybridConstants.ORDER_ATTACK,
			"player_id": player_id,
		}},
	])
