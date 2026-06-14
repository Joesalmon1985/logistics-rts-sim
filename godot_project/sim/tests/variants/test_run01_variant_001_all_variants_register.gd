extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_VARIANT_001_all_variants_register"


func run() -> void:
	var registry := VariantRegistry.create_default()
	var expected: Array = ["economy_trial", "hybrid", "military_trial"]
	var registered: Array = registry.registered_variant_ids()
	assert_eq(JSON.stringify(registered), JSON.stringify(expected))

	for variant_id in expected:
		assert_true(registry.has_variant(variant_id))
		var module: VariantModule = registry.get_variant(variant_id)
		assert_not_null(module)
		assert_true(module.get_subsystem_order().size() >= 1)
