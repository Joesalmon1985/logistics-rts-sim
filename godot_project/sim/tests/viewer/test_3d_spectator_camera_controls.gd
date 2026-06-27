extends TestCase

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_3d_spectator_camera_controls"


func _run_body() -> void:
	var host := Node.new()
	Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = CaptureHelper.launch_via_menu_route(host, "hybrid_3d")
	var scene: Node = ctx.get("scene")
	Helper.force_layout(ctx)
	var camera: Hybrid3dCameraController = scene.get_node_or_null("%Camera") as Hybrid3dCameraController
	assert_not_null(camera, "camera controller")
	assert_true(camera.has_method("jump_to"), "camera jump API")
	camera.set_mode_name("free")
	camera.mode = Hybrid3dCameraController.Mode.FREE
	var before: Vector3 = camera.position
	camera.jump_to(Vector3(8, 0, 8))
	var after: Vector3 = camera.position
	assert_true(before.distance_to(after) > 0.01, "camera position changes after jump_to")
	CaptureHelper.teardown(ctx, host)
