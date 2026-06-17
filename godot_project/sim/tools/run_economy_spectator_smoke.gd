extends SceneTree

## RUN_05C economy spectator smoke — GOD-SMOKE-ECON-SPECTATOR-001.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


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
	controller.reset_demo()
	var counts_a: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	var tick_before: int = controller.state.tick if controller.state != null else 0

	for _i in range(80):
		controller.bot_step_once()

	var counts_b: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	var tick_after: int = controller.state.tick if controller.state != null else 0
	var sprite_after: int = controller.get_sprite_states().size()
	if counts_b.is_empty():
		print("GOD-SMOKE-ECON-SPECTATOR-001 FAIL: empty render data")
		return false
	var changed: int = 0
	for key in counts_b.keys():
		if int(counts_b.get(key, 0)) != int(counts_a.get(key, 0)):
			changed += 1
	if tick_after < tick_before + 50:
		print("GOD-SMOKE-ECON-SPECTATOR-001 FAIL: tick did not advance (%d -> %d)" % [tick_before, tick_after])
		return false
	if changed < 1 and sprite_after <= 0:
		print("GOD-SMOKE-ECON-SPECTATOR-001 FAIL: insufficient activity (deltas=%d sprites=%d)" % [changed, sprite_after])
		return false
	print("GOD-SMOKE-ECON-SPECTATOR-001 PASS: render deltas=%d sprites=%d tick=%d" % [
		changed, sprite_after, tick_after,
	])
	return true
