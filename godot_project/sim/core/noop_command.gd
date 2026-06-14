class_name NoopCommand
extends SimCommand

func _init(command_player_id: String = "p1", command_tick: int = 0) -> void:
	super._init("NoopCommand", command_player_id, command_tick, {})


func validate(_state: GameState) -> CommandResult:
	return CommandResult.ok()


func apply(_state: GameState) -> CommandResult:
	return CommandResult.ok(false)
