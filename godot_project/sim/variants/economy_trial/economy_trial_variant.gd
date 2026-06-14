class_name EconomyTrialVariant
extends VariantStubBase

func _init() -> void:
	variant_id = "economy_trial"


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	var state := _build_initial_state(variant_id, scenario, config)
	EconomyInit.initialize(state, scenario, config)
	return state


func get_subsystem_order() -> Array[String]:
	return EconomyTick.get_subsystem_order()


func step_subsystem(name: String, state: GameState, rng: SeededRng) -> void:
	EconomyTick.step_subsystem(name, state, rng)
