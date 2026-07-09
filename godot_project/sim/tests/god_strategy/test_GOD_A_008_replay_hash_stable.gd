extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_008_replay_hash_stable"


func _run_body() -> void:
	MilitaryState.reset_config_cache()
	EconomyState.reset_config_cache()
	HybridState.reset_config_cache()

	var hash_a: String = _run_hash()
	var hash_b: String = _run_hash()
	assert_eq(hash_a, hash_b, "replay hash stable for scripted bot match")
	assert_true(hash_a.begins_with("sha256:"), "hash format")


func _run_hash() -> String:
	var result: Dictionary = Helper.run_scripted_match(120)
	return ReplayHash.compute_state_hash(result["state"])
