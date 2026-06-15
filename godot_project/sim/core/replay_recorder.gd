class_name ReplayRecorder
extends RefCounted

var entries: Array = []


func clear() -> void:
	entries.clear()


func record(tick: int, command: SimCommand, result: CommandResult) -> void:
	if command == null:
		return
	entries.append({
		"tick": tick,
		"command": command.to_dict(),
		"accepted": result.success,
		"error_code": "" if result.success else str(result.error_code),
	})


func get_command_entries() -> Array:
	return entries.duplicate(true)
