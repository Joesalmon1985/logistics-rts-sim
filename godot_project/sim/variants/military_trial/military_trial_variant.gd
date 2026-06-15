class_name MilitaryTrialVariant
extends VariantStubBase

func _init() -> void:
	variant_id = "military_trial"


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	var state := _build_initial_state(variant_id, scenario, config)
	MilitaryInit.initialize(state, scenario, config)
	return state


func get_subsystem_order() -> Array[String]:
	return MilitaryTick.get_subsystem_order()


func step_subsystem(name: String, state: GameState, rng: SeededRng) -> void:
	MilitaryTick.step_subsystem(name, state, rng)
