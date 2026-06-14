class_name SimulationTickRunner
extends RefCounted

var variant_registry: VariantRegistry
var command_validator: CommandValidator


func _init(
	p_variant_registry: VariantRegistry = null,
	p_command_validator: CommandValidator = null,
) -> void:
	variant_registry = p_variant_registry if p_variant_registry != null else VariantRegistry.create_default()
	command_validator = p_command_validator if p_command_validator != null else CommandValidator.new()


func create_game(scenario: Dictionary) -> GameState:
	var variant_id: String = str(scenario.get("variant", ""))
	var module: VariantModule = variant_registry.get_variant(variant_id)
	if module == null:
		push_error("SimulationTickRunner.create_game: unknown variant %s" % variant_id)
		return GameState.new()

	var terrain_config: Dictionary = ConfigLoader.load_terrain_config()
	return module.create_initial_state(scenario, terrain_config)


func submit_command(state: GameState, command: SimCommand) -> void:
	state.command_queue.append(command)


func tick(state: GameState) -> GameState:
	var module: VariantModule = variant_registry.get_variant(state.variant_id)
	if module == null:
		push_error("SimulationTickRunner.tick: unknown variant %s" % state.variant_id)
		return state

	while not state.command_queue.is_empty():
		var command: SimCommand = state.command_queue.pop_front()
		var result: CommandResult = command_validator.validate_and_apply(state, command)
		for event_data in result.events:
			if typeof(event_data) == TYPE_DICTIONARY:
				state.event_log.append_event(
					str(event_data.get("type", "unknown")),
					int(event_data.get("tick", state.tick)),
					event_data.get("payload", {}),
				)

	var rng := SeededRng.new(state.rng_state)
	for subsystem_name in module.get_subsystem_order():
		module.step_subsystem(subsystem_name, state, rng)

	state.rng_state = rng.serialize()
	state.tick += 1
	return state


func run_ticks(state: GameState, count: int) -> GameState:
	for _i in range(count):
		state = tick(state)
	return state
