class_name SetConstructionPriorityCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetConstructionPriorityCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var site_id: int = int(payload.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if site.is_empty():
		return CommandResult.fail("SITE_NOT_FOUND", "Construction site not found: %d" % site_id)
	if str(site.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Construction site owned by another player")
	if not payload.has("priority"):
		return CommandResult.fail("INVALID_PAYLOAD", "Missing priority")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var site_id: int = int(payload.get("site_id", 0))
	var priority: int = int(payload.get("priority", 0))
	EconomyPrioritySystem.set_construction_priority(state, site_id, priority)

	var events: Array = [{
		"type": "construction_priority_changed",
		"tick": tick,
		"payload": {
			"site_id": site_id,
			"priority": priority,
		},
	}]
	return CommandResult.ok(true, events)
