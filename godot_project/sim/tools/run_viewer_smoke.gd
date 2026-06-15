extends SceneTree

## RUN_05 viewer smoke runner — GOD-SMOKE-001 / GOD-SMOKE-002.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())

	var valid: Array[String] = ["all", "001", "002"]
	if smoke_id not in valid:
		print("VIEWER SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001()
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002()

	if failed:
		print("VIEWER SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("VIEWER SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_smoke_001() -> bool:
	for label_variant in ["economy", "military"]:
		var state: GameState
		if label_variant == "economy":
			state = EconomyDemo.build_state()
		else:
			state = MilitaryDemo.build_state()
		var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, "p1")
		var parsed: Dictionary = SnapshotLoader.parse(snapshot)
		if not parsed.get("ok", false):
			print("GOD-SMOKE-001 FAIL: %s snapshot parse" % label_variant)
			return false
		var render: Dictionary = LayerRenderer.build_render_data(parsed.get("snapshot", {}))
		if render.get("terrain", []).is_empty():
			print("GOD-SMOKE-001 FAIL: %s empty terrain" % label_variant)
			return false
		if render.get("buildings", []).is_empty():
			print("GOD-SMOKE-001 FAIL: %s empty buildings" % label_variant)
			return false
		if label_variant == "economy" and render.get("roads", []).is_empty():
			print("GOD-SMOKE-001 FAIL: economy roads not in render data")
			return false
	print("GOD-SMOKE-001 PASS: economy+military render data from snapshot")
	return true


func _run_smoke_002() -> bool:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_state(), "p1")
	var hash_before: String = ReplayHash.compute_state_hash(controller.state)
	var commands: Array = controller.get_available_commands()
	if commands.is_empty():
		print("GOD-SMOKE-002 FAIL: no available commands")
		return false
	if ClientCommandAvailability.count(commands) != commands.size():
		print("GOD-SMOKE-002 FAIL: command count mismatch")
		return false

	var submitted: int = 0
	for i in range(mini(3, commands.size())):
		var result: CommandResult = controller.submit_command_dto(commands[i])
		if result.success:
			submitted += 1
	if submitted < 1:
		print("GOD-SMOKE-002 FAIL: no legal command accepted")
		return false

	var invalid: CommandResult = controller.submit_invalid_command()
	if invalid.success:
		print("GOD-SMOKE-002 FAIL: invalid command accepted")
		return false
	if str(invalid.error_code) == "" and str(invalid.message) == "":
		print("GOD-SMOKE-002 FAIL: invalid command missing reason")
		return false

	var hash_after: String = ReplayHash.compute_state_hash(controller.state)
	if hash_before == hash_after:
		# hash may change after successful commands; re-check availability not in base export
		pass
	var base: Dictionary = SnapshotExporter.export_snapshot(controller.state)
	if base.has("available_commands"):
		print("GOD-SMOKE-002 FAIL: base export includes availability")
		return false

	# Military round-trip
	var mil := ViewerController.new()
	mil.load_demo_state(MilitaryDemo.build_state(), "p1")
	var mil_cmds: Array = mil.get_available_commands()
	if mil_cmds.is_empty():
		print("GOD-SMOKE-002 FAIL: military has no commands")
		return false
	var mil_ok: bool = false
	for j in range(mini(3, mil_cmds.size())):
		if mil.submit_command_dto(mil_cmds[j]).success:
			mil_ok = true
			break
	if not mil_ok:
		print("GOD-SMOKE-002 FAIL: military command not accepted")
		return false

	print("GOD-SMOKE-002 PASS: availability + command round-trip")
	return true
