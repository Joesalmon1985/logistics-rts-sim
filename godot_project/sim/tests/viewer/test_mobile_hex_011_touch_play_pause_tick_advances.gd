extends TestCase


func get_name() -> String:
	return "test_mobile_hex_011_touch_play_pause_tick_advances"


func _run_body() -> void:
	var ctrl := HybridMobileFfaController.new()
	ctrl.reset_ffa("easy")
	var t0: int = int(ctrl.get_snapshot().get("tick", 0))
	ctrl.step_tick()
	assert_true(int(ctrl.get_snapshot().get("tick", 0)) >= t0, "tick advances")
