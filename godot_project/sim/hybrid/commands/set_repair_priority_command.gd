class_name SetRepairPriorityCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetRepairPriorityCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	if not HybridConfig.is_hybrid(state):
		return CommandResult.fail("WRONG_VARIANT", "SetRepairPriorityCommand requires hybrid variant")
	var target_id: int = int(payload.get("target_id", 0))
	var check: Dictionary = MilitaryRepairSystem.can_repair(state, player_id, target_id)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var target_id: int = int(payload.get("target_id", 0))
	var priority: int = int(payload.get("priority", 0))
	var job_id: int = state.entity_ids.allocate()
	MilitaryState.add_repair_job(state, {
		"id": job_id,
		"target_id": target_id,
		"player_id": player_id,
		"priority": priority,
		"paused_reason": "",
	})
	return CommandResult.ok(true, [
		{"type": "repair_priority_set", "tick": tick, "payload": {
			"job_id": job_id,
			"target_id": target_id,
			"priority": priority,
		}},
	])
