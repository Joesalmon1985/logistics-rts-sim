extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_016_micro_queue_unit"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_queue_rifle.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	var state: GameState = result["state"]
	assert_true(
		_queue_progress(state, progression),
		"unit queue or production expected: %s" % JSON.stringify(progression),
	)


func _queue_progress(state: GameState, progression: Dictionary) -> bool:
	if bool(progression.get("milestones", {}).get("unit_queued", false)):
		return true
	if bool(progression.get("milestones", {}).get("unit_produced", false)):
		return true
	if Helper.count_units(state, "rifle_squad", "p1") > 0:
		return true
	if Helper.phase_index(str(progression.get("furthest_phase", ""))) >= Helper.phase_index("queue_unit"):
		return true
	return false
