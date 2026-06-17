extends SceneTree

## RUN_05C economy spectator smoke — GOD-SMOKE-ECON-SPECTATOR-001.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const STEP_COUNT: int = 30


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "001"
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())

	if smoke_id != "001":
		print("ECON SPECTATOR SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	if not _run_smoke_001():
		print("ECON SPECTATOR SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return
	print("ECON SPECTATOR SMOKE PASS")
	print("EXIT: 0 (pass)")
	call_deferred("quit", 0)


func _run_smoke_001() -> bool:
	var controller := EconomySpectatorController.new()
	var activity: Dictionary = controller.measure_activity_after_steps(
		STEP_COUNT,
		EconomySpectatorController.DEMO_PRODUCTION_CHAIN,
	)
	if not EconomySpectatorMetrics.passes_smoke_gate(activity, 2):
		print("GOD-SMOKE-ECON-SPECTATOR-001 FAIL: meaningful_count=%d non_sprite=%d"
			% [int(activity.get("meaningful_count", 0)), int(activity.get("non_sprite_delta_count", 0))])
		var signals: Dictionary = activity.get("signals", {})
		for key in signals.keys():
			print("  signal %s=%s" % [str(key), str(signals[key])])
		return false

	var family_eval: Dictionary = EconomySpectatorMetrics.evaluate_visual_families(
		controller.get_render_data(),
		controller.snapshot,
		controller.get_sprite_states(),
	)
	if not bool(family_eval.get("passes", false)):
		print("GOD-SMOKE-ECON-SPECTATOR-001 FAIL: visual families exposable=%d active=%d"
			% [int(family_eval.get("exposable_count", 0)), int(family_eval.get("active_count", 0))])
		for line in family_eval.get("missing_families", []):
			print("  missing: %s" % str(line))
		return false

	print("GOD-SMOKE-ECON-SPECTATOR-001 PASS: meaningful_count=%d non_sprite=%d events_delta=%d families=%d/%d"
		% [
			int(activity.get("meaningful_count", 0)),
			int(activity.get("non_sprite_delta_count", 0)),
			int(activity.get("events_delta", 0)),
			int(family_eval.get("active_count", 0)),
			int(family_eval.get("exposable_count", 0)),
		])
	return true
