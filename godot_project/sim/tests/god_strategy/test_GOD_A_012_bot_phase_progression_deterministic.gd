extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_012_bot_phase_progression_deterministic"


func _run_body() -> void:
	var cap: int = 800
	var first: Dictionary = Helper.run_scripted_regression(cap)
	var second: Dictionary = Helper.run_scripted_regression(cap)
	var p1a: String = str(first.get("p1_progression", {}).get("furthest_phase", ""))
	var p1b: String = str(second.get("p1_progression", {}).get("furthest_phase", ""))
	assert_eq(p1a, p1b, "furthest_phase must be deterministic at fixed seed")
	assert_eq(
		ReplayHash.compute_state_hash(first["state"]),
		ReplayHash.compute_state_hash(second["state"]),
		"replay hash must match for deterministic regression",
	)
