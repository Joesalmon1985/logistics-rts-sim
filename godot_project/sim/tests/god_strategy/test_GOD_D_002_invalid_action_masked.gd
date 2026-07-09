extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_D_002_invalid_action_masked"


func _run_body() -> void:
	var obs: Dictionary = {
		"hunger": 950,
		"tiredness": 100,
		"happiness": 800,
		"safety": 800,
	}
	var mask: Dictionary = WorkerPolicyTypes.build_legal_mask(obs)
	assert_eq(int(mask.get(WorkerPolicyTypes.ACTION_SEEK_FOOD, 1)), 0, "seek food masked when satiated")
	var action: String = RandomLegalTestWorkerPolicy.decide(obs, mask, SeededRng.from_seed(42))
	assert_false(action == WorkerPolicyTypes.ACTION_SEEK_FOOD, "random policy respects mask")
