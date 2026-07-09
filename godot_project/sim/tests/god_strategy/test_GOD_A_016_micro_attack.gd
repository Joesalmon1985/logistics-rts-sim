extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_016_micro_attack"


func _run_body() -> void:
	var result: Dictionary = Helper.run_scripted_micro("micro_delegated_attack.json", GodStrategyConstants.MICRO_TICK_CAP, true)
	var progression: Dictionary = result.get("p1_progression", {})
	var state: GameState = result["state"]
	assert_true(
		bool(progression.get("milestones", {}).get("attack_issued", false))
		or bool(progression.get("milestones", {}).get("enemy_hub_damaged", false))
		or str(result.get("match_end", {}).get("end_reason", "")) == "domination",
		"delegated attack or hub damage expected",
	)
