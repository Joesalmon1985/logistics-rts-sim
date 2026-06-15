class_name GuardCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("GuardCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	if unit.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(unit.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
		return CommandResult.fail(MilitaryConstants.ERR_DEAD_UNIT)
	var target_id: int = int(payload.get("target_id", 0))
	if target_id > 0:
		var target: Dictionary = MilitaryCombatSystem.target_info(state, target_id)
		if target.is_empty() or not bool(target.get("alive", false)):
			return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	var q: int = int(payload.get("q", int(unit.get("q", 0))))
	var r: int = int(payload.get("r", int(unit.get("r", 0))))
	MilitaryOrderSystem.issue_guard(state, unit, int(payload.get("target_id", 0)), q, r)
	return CommandResult.ok(true, [
		{"type": "order_issued", "tick": tick, "payload": {"unit_id": int(unit.get("id", 0)), "order": "guard", "target_id": int(payload.get("target_id", 0)), "q": q, "r": r}},
	])
