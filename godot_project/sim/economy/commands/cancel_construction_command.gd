class_name CancelConstructionCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("CancelConstructionCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var site_id: int = int(payload.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if site.is_empty():
		return CommandResult.fail("SITE_NOT_FOUND", "Construction site not found: %d" % site_id)
	if str(site.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Construction site owned by another player")
	var site_state: String = str(site.get("state", ""))
	if site_state == EconomyConstants.CONS_COMPLETED:
		return CommandResult.fail("SITE_COMPLETED", "Construction site already completed")
	if site_state == EconomyConstants.CONS_CANCELLED:
		return CommandResult.fail("SITE_CANCELLED", "Construction site already cancelled")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var site_id: int = int(payload.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	var result: Dictionary = EconomyConstructionSystem.cancel_site(state, site_id)
	if not result.get("ok", false):
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var events: Array = [{
		"type": "construction_cancelled",
		"tick": tick,
		"payload": {
			"site_id": site_id,
			"building_type": str(site.get("building_type", "")),
			"upgrade_source_id": int(site.get("upgrade_source_id", 0)),
		},
	}]
	return CommandResult.ok(true, events)
