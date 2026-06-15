class_name CancelProductionCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("CancelProductionCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var queue: Dictionary = MilitaryState.get_production_queue(state, int(payload.get("building_id", 0)))
	if queue.is_empty() or queue.get("items", []).is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(queue.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var queue: Dictionary = MilitaryState.get_production_queue(state, building_id)
	var items: Array = queue.get("items", [])
	var index: int = int(payload.get("index", items.size() - 1))
	if index < 0 or index >= items.size():
		index = items.size() - 1
	var item: Dictionary = items[index]
	items.remove_at(index)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var refund: int = (int(item.get("credit_cost", 0)) * cfg.global_int("cancel_refund_percent", 50)) / 100
	MilitaryState.add_credits(state, player_id, refund)
	return CommandResult.ok(true, [
		{"type": "production_cancelled", "tick": tick, "payload": {"building_id": building_id, "unit_type": str(item.get("unit_type", "")), "refund": refund}},
	])
