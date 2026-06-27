class_name CancelDelegatedOrderCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("CancelDelegatedOrderCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "CancelDelegatedOrderCommand requires hybrid variant")
	var order_id: int = int(payload.get("order_id", 0))
	var orders: Dictionary = HybridState.get_bucket(state, HybridConstants.BUCKET_DELEGATED_ORDERS)
	if not orders.has(str(order_id)):
		return CommandResult.fail("ORDER_NOT_FOUND")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var order_id: int = int(payload.get("order_id", 0))
	HybridState.remove_delegated_order(state, order_id)
	return CommandResult.ok(true, [
		{"type": "delegated_order_cancelled", "tick": tick, "payload": {"order_id": order_id}},
	])
