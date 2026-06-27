class_name HybridVariant
extends VariantStubBase

func _init() -> void:
	variant_id = "hybrid"


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	var state := _build_initial_state(variant_id, scenario, config)
	HybridInit.initialize(state, scenario, config)
	return state


func get_subsystem_order() -> Array[String]:
	return HybridTick.get_subsystem_order()


func step_subsystem(name: String, state: GameState, rng: SeededRng) -> void:
	HybridTick.step_subsystem(name, state, rng)
