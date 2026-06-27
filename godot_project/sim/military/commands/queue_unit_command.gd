class_name QueueUnitCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("QueueUnitCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	var unit_type: String = str(payload.get("unit_type", ""))
	var check: Dictionary = MilitaryProductionSystem.can_queue(state, building, unit_type)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	if HybridConfig.is_hybrid(state):
		var hybrid_check: Dictionary = HybridProductionSystem.can_queue(state, building, unit_type, player_id)
		if not hybrid_check.get("ok", false):
			return CommandResult.fail(str(hybrid_check.get("reason", "REJECTED")))
		return CommandResult.ok()
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var cost: int = int(cfg.get_unit_def(unit_type).get("credit_cost", 0))
	if MilitaryState.player_credits(state, player_id) < cost:
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var building: Dictionary = MilitaryState.get_building(state, building_id)
	var unit_type: String = str(payload.get("unit_type", ""))
	if HybridConfig.is_hybrid(state):
		return HybridProductionSystem.queue_unit(state, building_id, building, unit_type, player_id)
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var udef: Dictionary = cfg.get_unit_def(unit_type)
	var cost: int = int(udef.get("credit_cost", 0))
	if not MilitaryState.spend_credits(state, player_id, cost):
		return CommandResult.fail(MilitaryConstants.ERR_INSUFFICIENT_CREDITS)
	var queue: Dictionary = MilitaryState.ensure_production_queue(state, building_id, player_id)
	queue["items"].append({
		"unit_type": unit_type,
		"progress": 0,
		"required_ticks": int(udef.get("build_ticks", 1)),
		"credit_cost": cost,
	})
	return CommandResult.ok(true, [
		{"type": "production_queued", "tick": tick, "payload": {"building_id": building_id, "unit_type": unit_type, "cost": cost}},
	])
