extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")

## SETTLER-MVP-TIME-001 — time controls and help text visible.


func get_name() -> String:
	return "test_SETTLER_MVP_TIME_001_time_controls_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	assert_true(bool(ctx.get("ok", false)), "settler mount failed")
	var scene: Node = ctx.get("scene")
	for node_name in ["PlayBtn", "PauseBtn", "StepBtn", "Speed1Btn", "Speed2Btn", "Speed4Btn"]:
		var btn: Button = scene.get_node_or_null("%" + node_name) as Button
		assert_not_null(btn, "missing %s" % node_name)
		assert_true(btn.visible, "%s not visible" % node_name)
	var status: Label = scene.get_node_or_null("%StatusLabel") as Label
	assert_not_null(status, "status label missing")
	assert_true(status.text.contains("Simulation:"), "status missing Simulation label")
	assert_true(status.text.contains("Tick:"), "status missing Tick label")
	var controller: SettlerMvpController = SettlerMvpTestHelper.get_controller(scene)
	var help: RichTextLabel = scene.get_node_or_null("%HelpPanel") as RichTextLabel
	assert_not_null(help, "help missing")
	var help_source: String = help.text if help.text != "" else controller.build_help_text()
	assert_true(help_source.contains("Press Space or Play"), "help missing play instructions")
	SettlerMvpTestHelper.teardown(ctx, host)
