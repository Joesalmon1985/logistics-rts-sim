extends "res://sim/tests/test_case.gd"

## Deliberate failing fixture — used only by verify_failure_modes.gd, not the main suite.


func get_name() -> String:
	return "test_RUN01_INTEGRITY_deliberate_fail"


func _run_body() -> void:
	assert_true(false, "deliberate failure for integrity verification")
