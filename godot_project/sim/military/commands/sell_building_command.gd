class_name SellBuildingCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SellBuildingCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(str(building.get("building_type", "")))
	var refund: int = (int(bdef.get("credit_cost", 0)) * cfg.global_int("sell_refund_percent", 50)) / 100
	var owner_id: String = str(building.get("player_id", ""))
	MilitaryState.add_credits(state, owner_id, refund)
	building["state"] = MilitaryConstants.BLD_DESTROYED
	building["hp"] = 0
	if int(bdef.get("is_command_hub", 0)) > 0:
		var p: Dictionary = MilitaryState.get_player(state, owner_id)
		p["hub_count"] = maxi(0, int(p.get("hub_count", 0)) - 1)
	return CommandResult.ok(true, [
		{"type": "building_sold", "tick": tick, "payload": {"building_id": int(building.get("id", 0)), "refund": refund}},
	])
