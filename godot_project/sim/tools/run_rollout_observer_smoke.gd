extends SceneTree

## VIS-ROLL-005 — headless rollout observer smoke.


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _initialize() -> void:
	SimBootstrap.load_all()
	var controller := RolloutController.new()
	var loaded: Dictionary = controller.load_rollout(FIXTURE)
	if not loaded.get("ok", false):
		_fail("load failed: %s" % str(loaded.get("error", "")))
		return
	var render0: Dictionary = controller.get_render_data()
	if render0.get("terrain", []).is_empty():
		_fail("empty terrain at step 0")
		return
	if not controller.step_forward():
		_fail("step forward failed")
		return
	if controller.get_previous_command().is_empty():
		_fail("previous command missing after step forward")
		return
	if not controller.step_back():
		_fail("step back failed")
		return
	print("VIS-ROLL-005 PASS: rollout observer smoke")
	print("EXIT: 0 (pass)")
	quit(0)


func _fail(message: String) -> void:
	push_error(message)
	print("VIS-ROLL-005 FAIL: %s" % message)
	print("EXIT: 1 (fail)")
	quit(1)
