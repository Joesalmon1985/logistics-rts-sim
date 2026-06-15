class_name CancelMilitaryConstructionCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("CancelMilitaryConstructionCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var site: Dictionary = MilitaryState.get_construction_site(state, int(payload.get("site_id", 0)))
	if site.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(site.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(site.get("state", "")) != MilitaryConstants.CONS_ACTIVE:
		return CommandResult.fail("NOT_ACTIVE")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var site: Dictionary = MilitaryState.get_construction_site(state, int(payload.get("site_id", 0)))
	site["state"] = MilitaryConstants.CONS_CANCELLED
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var refund: int = (int(site.get("credit_cost", 0)) * cfg.global_int("cancel_refund_percent", 50)) / 100
	MilitaryState.add_credits(state, str(site.get("player_id", "")), refund)
	return CommandResult.ok(true, [
		{"type": "construction_cancelled", "tick": tick, "payload": {"site_id": int(site.get("id", 0)), "refund": refund}},
	])
