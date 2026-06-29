extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_016_micro_sawmill"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_sawmill_placement.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	var state: GameState = result["state"]
	assert_true(
		_sawmill_progress(state, progression),
		"sawmill placement expected: %s" % JSON.stringify(progression),
	)


func _sawmill_progress(state: GameState, progression: Dictionary) -> bool:
	if bool(progression.get("milestones", {}).get("sawmill_placed", false)):
		return true
	if Helper.building_of_type(state, "sawmill", "p1") > 0:
		return true
	if Helper.phase_index(str(progression.get("furthest_phase", ""))) >= Helper.phase_index("sawmill"):
		return true
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("building_type", "")) == "sawmill" and str(site.get("player_id", "")) == "p1":
			return true
	return false
