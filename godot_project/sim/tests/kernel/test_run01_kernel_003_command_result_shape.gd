extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_RUN01_KERNEL_003_command_result_shape"


func _run_body() -> void:
	var ok_result := CommandResult.ok(true, ["evt_001"], "done")
	var fail_result := CommandResult.fail("TERRAIN_BLOCKED", "blocked")

	for result in [ok_result, fail_result]:
		assert_has_keys(
			result.to_dict(),
			["success", "error_code", "message", "events", "state_changed", "accepted", "reason"],
		)

	assert_true(ok_result.success)
	assert_false(fail_result.success)
	assert_eq(ok_result.to_dict()["accepted"], true)
	assert_eq(fail_result.to_dict()["accepted"], false)
	assert_eq(fail_result.error_code, "TERRAIN_BLOCKED")
