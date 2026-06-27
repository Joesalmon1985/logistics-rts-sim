extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_scene_not_empty"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	Helper.force_layout(ctx)
	var scene: Node = ctx.get("scene")
	var camera: Camera3D = scene.get_node_or_null("%Camera") as Camera3D
	assert_not_null(camera, "camera exists")
	assert_true(camera.current, "camera active")
	var status: Label = scene.get_node_or_null("%StatusLabel") as Label
	assert_not_null(status, "HUD status label")
	assert_false(status.text.is_empty(), "HUD readable")
	CaptureHelper.teardown(ctx, host)
