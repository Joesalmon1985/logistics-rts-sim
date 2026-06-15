class_name PatrolCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("PatrolCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	if unit.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(unit.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
		return CommandResult.fail(MilitaryConstants.ERR_DEAD_UNIT)
	if _waypoints().size() < 2:
		return CommandResult.fail(MilitaryConstants.ERR_INVALID_PAYLOAD)
	for wp in _waypoints():
		if not state.map.in_bounds(HexCoord.new(int(wp.get("q", 0)), int(wp.get("r", 0)))):
			return CommandResult.fail(MilitaryConstants.ERR_OUT_OF_BOUNDS)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	MilitaryOrderSystem.issue_patrol(state, unit, _waypoints())
	return CommandResult.ok(true, [
		{"type": "order_issued", "tick": tick, "payload": {"unit_id": int(unit.get("id", 0)), "order": "patrol", "waypoints": _waypoints()}},
	])


func _waypoints() -> Array:
	if payload.has("waypoints"):
		return payload.get("waypoints", [])
	# Convenience: current position implied first; patrol to (q,r) and back is
	# handled by the unit's start position being captured at issue time.
	if payload.has("q"):
		return [{"q": int(payload.get("from_q", 0)), "r": int(payload.get("from_r", 0))}, {"q": int(payload.get("q", 0)), "r": int(payload.get("r", 0))}]
	return []
