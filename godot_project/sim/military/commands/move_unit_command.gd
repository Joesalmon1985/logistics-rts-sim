class_name MoveUnitCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("MoveUnitCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	MilitaryState.ensure_buckets(state)
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	if unit.is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_NO_SUCH_ENTITY)
	if str(unit.get("player_id", "")) != player_id:
		return CommandResult.fail(MilitaryConstants.ERR_NOT_OWNER)
	if str(unit.get("state", "")) == MilitaryConstants.UNIT_DEAD:
		return CommandResult.fail(MilitaryConstants.ERR_DEAD_UNIT)
	var q: int = int(payload.get("q", 0))
	var r: int = int(payload.get("r", 0))
	if not state.map.in_bounds(HexCoord.new(q, r)):
		return CommandResult.fail(MilitaryConstants.ERR_OUT_OF_BOUNDS)
	if int(unit.get("q", 0)) == q and int(unit.get("r", 0)) == r:
		return CommandResult.ok()
	if not MilitaryMapRules.is_passable(state, q, r):
		return CommandResult.fail(MilitaryConstants.ERR_UNREACHABLE)
	if MilitaryPathfinding.find_path(state, int(unit.get("q", 0)), int(unit.get("r", 0)), q, r).is_empty():
		return CommandResult.fail(MilitaryConstants.ERR_UNREACHABLE)
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var unit: Dictionary = MilitaryState.get_unit(state, int(payload.get("unit_id", 0)))
	MilitaryOrderSystem.issue_move(state, unit, int(payload.get("q", 0)), int(payload.get("r", 0)))
	return CommandResult.ok(true, [
		{"type": "order_issued", "tick": tick, "payload": {"unit_id": int(unit.get("id", 0)), "order": "move", "q": int(payload.get("q", 0)), "r": int(payload.get("r", 0))}},
	])
