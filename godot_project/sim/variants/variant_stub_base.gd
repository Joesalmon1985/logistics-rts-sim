class_name VariantStubBase
extends VariantModule

const DEFAULT_MAP_WIDTH: int = 8
const DEFAULT_MAP_HEIGHT: int = 8


func _build_initial_state(
	p_variant_id: String,
	scenario: Dictionary,
	terrain_config: Dictionary,
) -> GameState:
	var state := GameState.new()
	state.variant_id = p_variant_id
	state.seed = int(scenario.get("seed", 1))
	state.rng_state = state.seed
	state.tick = 0

	for player_id in scenario.get("players", ["p1"]):
		state.players.append(PlayerState.new(str(player_id)))

	var map_file: String = str(scenario.get("map_file", ""))
	if map_file != "":
		state.map = MapGenerator.load_from_file(ConfigLoader.DATA_ROOT + map_file)
	else:
		var width: int = int(scenario.get("map_width", DEFAULT_MAP_WIDTH))
		var height: int = int(scenario.get("map_height", DEFAULT_MAP_HEIGHT))
		state.map = MapGenerator.generate_from_seed(state.seed, width, height, terrain_config)

	state.event_log.append_event(
		"game_created",
		state.tick,
		{
			"variant": p_variant_id,
			"seed": state.seed,
			"player_count": state.players.size(),
		},
	)
	return state


func get_subsystem_order() -> Array[String]:
	return ["diagnostics"]


func step_subsystem(name: String, state: GameState, _rng: SeededRng) -> void:
	if name == "diagnostics":
		state.event_log.append_event(
			"tick_advanced",
			state.tick,
			{"tick": state.tick, "variant": state.variant_id},
		)
