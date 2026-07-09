extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_001_scenario_loads"


func _run_body() -> void:
	var scenario: Dictionary = Helper.load_scenario()
	assert_eq(str(scenario.get("variant", "")), "hybrid", "variant")
	assert_eq(int(scenario.get("seed", 0)), 99001, "seed")
	assert_true(bool(scenario.get("god_strategy_mvp_a", false)), "god_strategy flag")
	assert_eq(scenario.get("players", []).size(), 4, "player count")
