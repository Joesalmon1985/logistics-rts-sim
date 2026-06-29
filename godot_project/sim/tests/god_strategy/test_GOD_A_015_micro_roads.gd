extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_015_micro_roads"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_roads_to_woodcutter.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	var blocker: String = GodStrategyMapViability.classify_stall(progression)
	assert_true(blocker != "road_ownership", "road ownership stall: %s" % JSON.stringify(progression))
	assert_true(
		bool(progression.get("milestones", {}).get("road_segment_placed", false))
		or Helper.phase_index(str(progression.get("furthest_phase", ""))) >= Helper.phase_index("woodcutter"),
		"roads should progress past ownership stall",
	)
