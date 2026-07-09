extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_013_full_chain_layout_sweep"


func _run_body() -> void:
	Helper.write_layout_sweep_artefact()
	for scenario_path in [Helper.SCENARIO_PATH, Helper.REGRESSION_SCENARIO_PATH]:
		var state: GameState = Helper.create_prepared_state(0, scenario_path)
		var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
		var spawn_report: Dictionary = GodStrategyMapViability.validate_spawn(state, "p1")
		assert_true(
			bool(spawn_report.get("ok", false)),
			"p1 spawn viability + road path on %s: %s" % [
				scenario_path,
				GodStrategyMapViability.format_failure({"players": {"p1": spawn_report}}),
			],
		)
