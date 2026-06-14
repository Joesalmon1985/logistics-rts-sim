class_name SetWarePriorityCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetWarePriorityCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	if not payload.has("priority"):
		return CommandResult.fail("INVALID_PAYLOAD", "Missing priority")

	if payload.has("job_id"):
		var job_id: int = int(payload.get("job_id", 0))
		var job: Dictionary = EconomyState.get_delivery_job(state, job_id)
		if job.is_empty():
			return CommandResult.fail("JOB_NOT_FOUND", "Delivery job not found: %d" % job_id)
		if str(job.get("player_id", player_id)) != player_id:
			return CommandResult.fail("NOT_OWNER", "Delivery job owned by another player")
		return CommandResult.ok()

	var ware_type: String = str(payload.get("ware_type", ""))
	if ware_type == "":
		return CommandResult.fail("INVALID_PAYLOAD", "Missing ware_type or job_id")
	var config: EconomyConfig = EconomyState.get_config(state)
	if not config.wares.has(ware_type):
		return CommandResult.fail("WARE_UNKNOWN", "Unknown ware type: %s" % ware_type)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var priority: int = int(payload.get("priority", 0))
	if payload.has("job_id"):
		var job_id: int = int(payload.get("job_id", 0))
		EconomyPrioritySystem.set_ware_priority(state, job_id, priority)
		var events: Array = [{
			"type": "ware_priority_changed",
			"tick": tick,
			"payload": {
				"job_id": job_id,
				"priority": priority,
			},
		}]
		return CommandResult.ok(true, events)

	var ware_type: String = str(payload.get("ware_type", ""))
	var priorities: Dictionary = EconomyState.get_meta_value(state, "ware_priorities", {})
	priorities[ware_type] = priority
	EconomyState.set_meta_value(state, "ware_priorities", priorities)

	var events: Array = [{
		"type": "ware_priority_changed",
		"tick": tick,
		"payload": {
			"player_id": player_id,
			"ware_type": ware_type,
			"priority": priority,
		},
	}]
	return CommandResult.ok(true, events)
