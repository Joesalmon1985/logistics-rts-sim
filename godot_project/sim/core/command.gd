class_name SimCommand
extends RefCounted

var type: String = ""
var player_id: String = ""
var tick: int = 0
var payload: Dictionary = {}


func _init(
	command_type: String = "",
	command_player_id: String = "",
	command_tick: int = 0,
	command_payload: Dictionary = {},
) -> void:
	type = command_type
	player_id = command_player_id
	tick = command_tick
	payload = command_payload.duplicate(true)


func validate(_state: GameState) -> CommandResult:
	return CommandResult.ok()


func apply(_state: GameState) -> CommandResult:
	return CommandResult.ok()


func to_dict() -> Dictionary:
	return {
		"type": type,
		"player_id": player_id,
		"tick": tick,
		"payload": payload.duplicate(true),
	}
