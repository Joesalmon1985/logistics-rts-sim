extends TestCase

## VIS-CMD-001 — command round-trip through CommandValidator with snapshot refresh.


func get_name() -> String:
	return "test_VIS_CMD_001_command_roundtrip"


func _run_body() -> void:
	_test_economy_roundtrip()
	_test_rejected_command()


func _test_economy_roundtrip() -> void:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_state(), "p1")
	var commands: Array = controller.get_available_commands()
	assert_true(commands.size() >= 1, "at least one available command")
	var before_tick: int = controller.state.tick
	var dto: Dictionary = commands[0]
	var result: CommandResult = controller.submit_command_dto(dto)
	assert_true(result.success, "legal command accepted: %s" % result.error_code)
	assert_true(controller.snapshot.has("tick"), "snapshot refreshed")
	assert_true(controller.command_history.size() >= 1, "command history recorded")
	assert_true(bool(controller.command_history[0].get("success", false)), "command history success flag")


func _test_rejected_command() -> void:
	var controller := ViewerController.new()
	controller.load_demo_state(EconomyDemo.build_state(), "p1")
	var before_hash: String = ReplayHash.compute_state_hash(controller.state)
	var result: CommandResult = controller.submit_invalid_command()
	assert_false(result.success, "invalid command rejected")
	assert_true(str(result.error_code) != "" or str(result.message) != "", "readable rejection reason")
	var after_hash: String = ReplayHash.compute_state_hash(controller.state)
	assert_eq(before_hash, after_hash, "invalid command did not mutate state")
