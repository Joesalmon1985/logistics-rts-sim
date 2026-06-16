extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_CONFIG_validation_strict_mode"


func _run_body() -> void:
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var errors: Array = cfg.validate(true)
	assert_true(errors.is_empty(), "production config must pass strict validation: %s" % str(errors))

	var broken := EconomyConfig.new()
	broken.wares = {"timber": {}}
	broken.buildings = {
		"bad_building": {"recipe_id": "missing_recipe", "worker_type": "ghost_worker"},
	}
	broken.recipes = {
		"bad_recipe": {
			"building": "missing_building",
			"inputs": {"unknown_ware": 1},
			"outputs": {"also_unknown": 1},
		},
	}
	broken.workers = {}
	broken.construction_costs = {
		"building_costs": {"orphan_building": {"fake_ware": 1}},
	}
	var broken_errors: Array = broken.validate(true)
	assert_true(broken_errors.size() >= 4, "broken config must report multiple validation errors")
