extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_009_map_viability_all_spawns"


func _run_body() -> void:
	var scenario: Dictionary = Helper.load_scenario()
	var state: GameState = Helper.create_prepared_state(0)
	var report: Dictionary = GodStrategyMapViability.validate_all_spawns(state, scenario)
	assert_true(bool(report.get("ok", false)), GodStrategyMapViability.format_failure(report))
	for pid in ["p1", "p2", "p3", "p4"]:
		var player_report: Dictionary = report["players"][pid]
		for building_type in GodStrategyMapViability.MVP_BUILDINGS:
			assert_true(
				player_report.get("sites", {}).has(building_type),
				"%s needs legal %s" % [pid, building_type],
			)
