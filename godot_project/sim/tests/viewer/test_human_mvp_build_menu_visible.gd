extends TestCase

const SettlerMvpTestHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")


func get_name() -> String:
	return "test_human_mvp_build_menu_visible"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = SettlerMvpTestHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	var palette: BuildPalette = scene.get_node_or_null("%BuildPalette") as BuildPalette
	assert_true(palette != null, "build palette missing")
	if palette != null:
		palette.ensure_built()
	assert_true(palette.get_button("woodcutter") != null, "woodcutter button")
	assert_true(palette.get_button("road") != null, "road button")
	SettlerMvpTestHelper.teardown(ctx, host)
