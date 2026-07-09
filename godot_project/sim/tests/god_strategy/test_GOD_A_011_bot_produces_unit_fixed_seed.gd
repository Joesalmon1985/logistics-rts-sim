extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_011_bot_produces_unit_fixed_seed"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_regression(GodStrategyConstants.PROOF_TICK_CAP)
	var progression: Dictionary = result.get("p1_progression", {})
	var state: GameState = result["state"]
	assert_true(
		bool(progression.get("milestones", {}).get("unit_produced", false))
		or Helper.count_units(state, "rifle_squad", "p1") > 0
		or bool(progression.get("milestones", {}).get("unit_queued", false)),
		"p1 should queue or produce rifle_squad: %s" % JSON.stringify(progression),
	)
