extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_015_micro_worker_policy"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_worker_policy.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	assert_true(
		bool(progression.get("milestones", {}).get("worker_policy_set", false)),
		"worker policy milestone expected",
	)
	assert_true(
		Helper.phase_index(str(progression.get("furthest_phase", ""))) >= Helper.phase_index("worker_policy"),
		"furthest_phase should reach worker_policy",
	)
