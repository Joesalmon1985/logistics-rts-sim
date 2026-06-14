class_name HybridVariant
extends VariantStubBase

func _init() -> void:
	variant_id = "hybrid"


func create_initial_state(scenario: Dictionary, config: Dictionary) -> GameState:
	return _build_initial_state(variant_id, scenario, config)


func get_subsystem_order() -> Array[String]:
	return ["map", "economy", "military", "hybrid", "diagnostics"]
