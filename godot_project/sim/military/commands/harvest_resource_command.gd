class_name HarvestResourceCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("HarvestResourceCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	if unit.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(unit.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
		return CommandResult.fail(MilitaryConstants.ERR_DEAD_UNIT)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	if int(cfg.get_unit_def(str(unit.get("unit_type", ""))).get("can_harvest", 0)) <= 0:
		return CommandResult.fail(MilitaryConstants.ERR_INVALID_TARGET)
	var field: Dictionary = MilitaryState.get_resource_field(state, int(payload.get("field_id", 0)))
	if field.is_empty() or int(field.get("amount", 0)) <= 0:
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	MilitaryResourceSystem.assign_harvest(state, unit, int(payload.get("field_id", 0)))
	return CommandResult.ok(true, [
		{"type": "order_issued", "tick": tick, "payload": {"unit_id": int(unit.get("id", 0)), "order": "harvest", "field_id": int(payload.get("field_id", 0))}},
	])
