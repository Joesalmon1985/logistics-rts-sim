extends TestCase

const Helper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")


func get_name() -> String:
	return "test_military_mvp_scene_loads"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount_military(host)
	assert_true(bool(ctx.get("ok", false)), "military mount failed")
	var scene: Node = ctx.get("scene")
	assert_true(scene != null, "scene null")
	assert_true(scene.get_node_or_null("%MapViewport") != null, "map missing")
	assert_true(scene.get_node_or_null("%BuildPalette") != null, "build palette missing")
	Helper.teardown(ctx, host)
