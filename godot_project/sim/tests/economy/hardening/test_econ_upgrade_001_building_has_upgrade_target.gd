extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_UPGRADE_001_building_has_upgrade_target"


func _run_body() -> void:
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var sawmill: Dictionary = cfg.get_building_def("sawmill")
	var coal_mine: Dictionary = cfg.get_building_def("coal_mine")
	assert_eq(str(sawmill.get("upgrade_target", "")), "enhanced_sawmill")
	assert_eq(str(coal_mine.get("upgrade_target", "")), "deep_coal_mine")
	assert_false(cfg.get_upgrade_def("sawmill").is_empty(), "upgrade data must exist for sawmill")
	assert_false(cfg.get_upgrade_def("coal_mine").is_empty(), "upgrade data must exist for coal_mine")
