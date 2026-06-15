class_name SetRallyPointCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("SetRallyPointCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	if building.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(building.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if not state.map.in_bounds(HexCoord.new(int(payload.get("q", 0)), int(payload.get("r", 0)))):
		return CommandResult.fail(MilitaryConstants.ERR_OUT_OF_BOUNDS)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var building: Dictionary = MilitaryState.get_building(state, int(payload.get("building_id", 0)))
	building["rally_q"] = int(payload.get("q", 0))
	building["rally_r"] = int(payload.get("r", 0))
	return CommandResult.ok(true, [
		{"type": "rally_point_set", "tick": tick, "payload": {"building_id": int(building.get("id", 0)), "q": int(payload.get("q", 0)), "r": int(payload.get("r", 0))}},
	])
