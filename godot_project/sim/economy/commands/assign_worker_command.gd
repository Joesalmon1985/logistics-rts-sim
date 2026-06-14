class_name AssignWorkerCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("AssignWorkerCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var building_id: int = int(payload.get("building_id", 0))
	var worker_type: String = str(payload.get("worker_type", ""))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return CommandResult.fail("BUILDING_NOT_FOUND", "Building not found: %d" % building_id)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Building owned by another player")
	if worker_type == "":
		return CommandResult.fail("INVALID_PAYLOAD", "Missing worker_type")

	var config: EconomyConfig = EconomyState.get_config(state)
	if not config.workers.has(worker_type):
		return CommandResult.fail("WORKER_TYPE_UNKNOWN", "Unknown worker type: %s" % worker_type)

	var building_def: Dictionary = config.get_building_def(str(building.get("building_type", "")))
	var expected_type: String = str(building_def.get("worker_type", ""))
	if expected_type != "" and expected_type != worker_type:
		return CommandResult.fail("WORKER_TYPE_MISMATCH", "Building expects worker type %s" % expected_type)

	var slots: int = int(building_def.get("worker_slots", 0))
	if slots <= 0:
		return CommandResult.fail("NO_WORKER_SLOTS", "Building has no worker slots")

	var assigned: Array = building.get("assigned_workers", [])
	if assigned.size() >= slots:
		return CommandResult.fail("WORKER_SLOTS_FULL", "All worker slots filled")

	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building_id: int = int(payload.get("building_id", 0))
	var worker_type: String = str(payload.get("worker_type", ""))
	var building: Dictionary = EconomyState.get_building(state, building_id)
	building["worker_request_type"] = worker_type
	building["worker_request_pending"] = true
	EconomyState.add_building(state, building)

	var events: Array = [{
		"type": "worker_assigned",
		"tick": tick,
		"payload": {
			"building_id": building_id,
			"worker_type": worker_type,
			"requested": true,
		},
	}]
	return CommandResult.ok(true, events)
