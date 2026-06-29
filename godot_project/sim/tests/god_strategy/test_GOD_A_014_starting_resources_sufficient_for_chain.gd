extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_014_starting_resources_sufficient_for_chain"


func _run_body() -> void:
	for scenario_path in [Helper.SCENARIO_PATH, Helper.REGRESSION_SCENARIO_PATH]:
		var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
		var audit: Dictionary = GodStrategyResourceAudit.can_afford_chain_start(scenario)
		assert_true(bool(audit.get("ok", false)), "%s resource audit: %s" % [scenario_path, JSON.stringify(audit.get("failures", []))])
