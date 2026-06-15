extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_MINE_004_basic_and_deep_tier"


func _run_body() -> void:
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var basic_recipe: Dictionary = cfg.get_recipe("coal_from_deposit")
	var deep_recipe: Dictionary = cfg.get_recipe("coal_from_deposit_deep")
	assert_eq(int(basic_recipe.get("outputs", {}).get("coal", 0)), 1)
	assert_eq(int(deep_recipe.get("outputs", {}).get("coal", 0)), 2, "deep tier must produce more per cycle")

	var basic_def: Dictionary = cfg.get_building_def("coal_mine")
	var deep_def: Dictionary = cfg.get_building_def("deep_coal_mine")
	assert_eq(str(basic_def.get("upgrade_target", "")), "deep_coal_mine")
	assert_eq(str(basic_def.get("food_ware", "")), "rations")
	assert_eq(str(deep_def.get("food_ware", "")), "meals")
