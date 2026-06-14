class_name VariantRegistry
extends RefCounted

var _variants: Dictionary = {}


func register(variant_id: String, module: VariantModule) -> void:
	_variants[variant_id] = module


func get_variant(variant_id: String) -> VariantModule:
	return _variants.get(variant_id, null)


func has_variant(variant_id: String) -> bool:
	return _variants.has(variant_id)


func registered_variant_ids() -> Array:
	var keys: Array = _variants.keys()
	keys.sort()
	return keys


static func create_default() -> VariantRegistry:
	var registry := VariantRegistry.new()
	registry.register("economy_trial", EconomyTrialVariant.new())
	registry.register("military_trial", MilitaryTrialVariant.new())
	registry.register("hybrid", HybridVariant.new())
	return registry
