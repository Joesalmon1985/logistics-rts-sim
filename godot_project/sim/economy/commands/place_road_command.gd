class_name PlaceRoadCommand
extends SimCommand

func _init(command_player_id: String, command_tick: int, command_payload: Dictionary = {}) -> void:
	super._init("PlaceRoadCommand", command_player_id, command_tick, command_payload)


func validate(state: GameState) -> CommandResult:
	EconomyState.ensure_buckets(state)
	var hex_a := HexCoord.new(int(payload.get("from_q", 0)), int(payload.get("from_r", 0)))
	var hex_b := HexCoord.new(int(payload.get("to_q", 0)), int(payload.get("to_r", 0)))
	var check: Dictionary = EconomyMapRules.can_place_road(state, hex_a, hex_b, player_id)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	return CommandResult.ok()


func apply(state: GameState) -> CommandResult:
	var hex_a := HexCoord.new(int(payload.get("from_q", 0)), int(payload.get("from_r", 0)))
	var hex_b := HexCoord.new(int(payload.get("to_q", 0)), int(payload.get("to_r", 0)))
	var result: Dictionary = EconomyRoadSystem.place_road(state, hex_a, hex_b, player_id)
	if not result.get("ok", false):
		return CommandResult.fail(str(result.get("reason", "REJECTED")))
	var events: Array = [{
		"type": "road_placed",
		"tick": tick,
		"payload": {
			"road_id": int(result.get("road_id", 0)),
			"player_id": player_id,
			"from_q": hex_a.q,
			"from_r": hex_a.r,
			"to_q": hex_b.q,
			"to_r": hex_b.r,
		},
	}]
	return CommandResult.ok(true, events)
