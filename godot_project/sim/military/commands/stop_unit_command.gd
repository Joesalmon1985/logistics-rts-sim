class_name StopUnitCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("StopUnitCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	if unit.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(unit.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
		return CommandResult.fail(MilitaryConstants.ERR_DEAD_UNIT)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	MilitaryOrderSystem.issue_stop(state, unit)
	return CommandResult.ok(true, [
		{"type": "order_issued", "tick": tick, "payload": {"unit_id": int(unit.get("id", 0)), "order": "stop"}},
	])
