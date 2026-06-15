class_name RepairTargetCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("RepairTargetCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var check: Dictionary = MilitaryRepairSystem.can_repair(state, player_id, int(payload.get("target_id", 0)))
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var target_id: int = int(payload.get("target_id", 0))
	# Avoid duplicate jobs for the same target.
	for job in MilitaryState.all_repair_jobs(state):
		if int(job.get("target_id", 0)) == target_id:
			return CommandResult.ok(false, [])
	var job_id: int = state.entity_ids.allocate()
	MilitaryState.add_repair_job(state, {
		"id": job_id,
		"target_id": target_id,
		"player_id": player_id,
		"paused_reason": "",
	})
	return CommandResult.ok(true, [
		{"type": "repair_started", "tick": tick, "payload": {"job_id": job_id, "target_id": target_id, "player_id": player_id}},
	])
