class_name CommandValidator
extends RefCounted

var registry: CommandRegistry


func _init(p_registry: CommandRegistry = null) -> void:
	registry = p_registry if p_registry != null else CommandRegistry.create_default()


func validate_and_apply(state: GameState, command: SimCommand) -> CommandResult:
	if command == null:
		return CommandResult.fail("UNKNOWN_COMMAND", "Command is null")
	if HybridState.is_forbidden_player_command(state, command):
		return CommandResult.fail(HybridConstants.ERR_FORBIDDEN_DIRECT, "Direct unit command forbidden in hybrid")
	if not registry.has_type(command.type):
		return CommandResult.fail("UNKNOWN_COMMAND", "Unknown command type: %s" % command.type)

	var before_hash: String = ReplayHash.compute_state_hash(state)
	var validation: CommandResult = command.validate(state)
	if not validation.success:
		return validation

	var apply_result: CommandResult = command.apply(state)
	if not apply_result.success:
		return apply_result

	var after_hash: String = ReplayHash.compute_state_hash(state)
	var changed: bool = before_hash != after_hash
	return CommandResult.ok(changed, apply_result.events, apply_result.message)
