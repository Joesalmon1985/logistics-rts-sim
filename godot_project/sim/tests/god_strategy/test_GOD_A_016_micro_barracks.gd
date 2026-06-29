extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_016_micro_barracks"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_barracks_placement.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	var state: GameState = result["state"]
	assert_true(
		_barracks_progress(state, progression),
		"barracks placement expected: %s" % JSON.stringify(progression),
	)


func _barracks_progress(state: GameState, progression: Dictionary) -> bool:
	if bool(progression.get("milestones", {}).get("barracks_placed", false)):
		return true
	if Helper.building_of_type(state, "barracks", "p1") > 0:
		return true
	if Helper.phase_index(str(progression.get("furthest_phase", ""))) >= Helper.phase_index("barracks"):
		return true
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("building_type", "")) == "barracks" and str(site.get("player_id", "")) == "p1":
			return true
	return false
