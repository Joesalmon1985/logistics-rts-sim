class_name InvalidTestCommand
extends SimCommand

func _init(command_player_id: String = "p1", command_tick: int = 0) -> void:
	super._init("InvalidTestCommand", command_player_id, command_tick, {})


func validate(_state: GameState) -> CommandResult:
	return CommandResult.fail("INVALID_COMMAND", "Command is always invalid for testing")


func apply(_state: GameState) -> CommandResult:
	return CommandResult.fail("INVALID_COMMAND", "Command is always invalid for testing")
