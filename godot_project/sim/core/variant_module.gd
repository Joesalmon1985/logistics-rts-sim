class_name VariantModule
extends RefCounted

var variant_id: String = ""


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	push_error("VariantModule.create_initial_state not implemented")
	return GameState.new()


func get_subsystem_order() -> Array[String]:
	return []


func step_subsystem(_name: String, _state: GameState, _rng: SeededRng) -> void:
	pass
