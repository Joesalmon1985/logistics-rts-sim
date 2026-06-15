class_name Bot
extends RefCounted

## Formal bot interface (RUN_04). Bots are in-engine GDScript clients of the command API.

var actor_id: String = "bot"
var player_id: String = "p1"

var _last_snapshot: Dictionary = {}


func reset(p_player_id: String = "p1") -> void:
	player_id = p_player_id
	_last_snapshot.clear()


func observe(snapshot: Dictionary, p_player_id: String) -> void:
	player_id = p_player_id
	_last_snapshot = snapshot.duplicate(true)


func decide(_tick: int) -> SimCommand:
	return null


func get_decisions() -> Array:
	return []


func is_complete(_snapshot: Dictionary) -> bool:
	return false


func on_command_applied(_command: SimCommand, _result: CommandResult) -> void:
	pass
