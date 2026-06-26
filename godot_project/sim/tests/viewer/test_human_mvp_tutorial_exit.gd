extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_tutorial_exit"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	scene.set("tutorial_enabled", true)
	scene.call("initialize_for_tests")
	scene.tutorial.skip_or_exit()
	assert_true(scene.tutorial.exited, "tutorial marked exited")
	SettlerMvpTestHelper.teardown(ctx, host)
