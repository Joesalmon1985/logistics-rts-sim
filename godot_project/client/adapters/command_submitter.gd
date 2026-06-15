class_name CommandSubmitter
extends RefCounted

var registry: CommandRegistry
var validator: CommandValidator


func _init(p_registry: CommandRegistry = null, p_validator: CommandValidator = null) -> void:
	registry = p_registry if p_registry != null else CommandRegistry.create_default()
	validator = p_validator if p_validator != null else CommandValidator.new(registry)


func submit_from_dto(state: GameState, dto: Dictionary) -> CommandResult:
	var command_type: String = str(dto.get("type", ""))
	var payload: Dictionary = dto.get("payload", {}).duplicate(true)
	var player_id: String = str(payload.get("player_id", dto.get("actor_id", "p1")))
	var tick: int = int(payload.get("tick", state.tick))
	payload["player_id"] = player_id
	payload["tick"] = tick
	var cmd: SimCommand = registry.create(command_type, payload)
	if cmd == null:
		return CommandResult.fail("UNKNOWN_COMMAND", "Unknown command type: %s" % command_type)
	cmd.player_id = player_id
	cmd.tick = tick
	return validator.validate_and_apply(state, cmd)


func submit_invalid_test(state: GameState, player_id: String = "p1") -> CommandResult:
	var cmd := InvalidTestCommand.new(player_id, state.tick)
	return validator.validate_and_apply(state, cmd)
