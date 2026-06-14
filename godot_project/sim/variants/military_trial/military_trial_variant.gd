class_name MilitaryTrialVariant
extends VariantStubBase

func _init() -> void:
	variant_id = "military_trial"


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	return _build_initial_state(variant_id, scenario, config)


func get_subsystem_order() -> Array[String]:
	return ["map", "military", "diagnostics"]
