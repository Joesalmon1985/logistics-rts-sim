extends TestCase

const Helper := preload("res://sim/tests/viewer/hybrid_3d_spectator_test_helper.gd")


func get_name() -> String:
	return "test_hybrid_3d_spectator_camera_controls_exist"


func _run_body() -> void:
	var host := Node.new()
	var ctx: Dictionary = Helper.mount(host)
	var scene: Node = ctx.get("scene")
	var camera: Hybrid3dCameraController = scene.get_node_or_null("%Camera") as Hybrid3dCameraController
	assert_true(camera != null, "camera missing")
	assert_true(camera.has_method("set_mode_name"), "camera mode API")
	assert_true(camera.has_method("jump_to"), "camera jump API")
	Helper.teardown(ctx, host)
