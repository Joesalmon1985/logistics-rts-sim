extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_010_bot_reaches_barracks_fixed_seed"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_regression(GodStrategyConstants.PROOF_TICK_CAP)
	var progression: Dictionary = result.get("p1_progression", {})
	var furthest_idx: int = Helper.phase_index(str(progression.get("furthest_phase", "")))
	assert_true(
		furthest_idx >= Helper.phase_index("barracks")
		or bool(progression.get("milestones", {}).get("barracks_placed", false)),
		"p1 should reach barracks phase within proof cap: %s" % JSON.stringify(progression),
	)
