class_name RemoveRoadCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("RemoveRoadCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var road_id: int = int(payload.get("road_id", 0))
	var road: Dictionary = EconomyState.get_road(state, road_id)
	if road.is_empty():
		return CommandResult.fail("ROAD_NOT_FOUND", "Road not found: %d" % road_id)
	if str(road.get("player_id", "")) != player_id:
		return CommandResult.fail("NOT_OWNER", "Road owned by another player")
	if int(road.get("congestion", 0)) > 0:
		return CommandResult.fail("ROAD_IN_USE", "Road segment in use")
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var road_id: int = int(payload.get("road_id", 0))
	var road: Dictionary = EconomyState.get_road(state, road_id)
	var result: Dictionary = EconomyRoadSystem.remove_road(state, road_id)
	if not result.get("ok", false):
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var events: Array = [{
		"type": "road_removed",
		"tick": tick,
		"payload": {
			"road_id": road_id,
			"hex_a_q": int(road.get("hex_a_q", 0)),
			"hex_a_r": int(road.get("hex_a_r", 0)),
			"hex_b_q": int(road.get("hex_b_q", 0)),
			"hex_b_r": int(road.get("hex_b_r", 0)),
		},
	}]
	return CommandResult.ok(true, events)
