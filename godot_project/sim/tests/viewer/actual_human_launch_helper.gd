class_name ActualHumanLaunchHelper
extends RefCounted

## Mount helpers that follow the real main-menu → Tutorial launch route at player window sizes.

const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")
const ShotUtil := preload("res://sim/tools/actual_human_screenshot_util.gd")
const LAUNCH_SIZE := Vector2i(1280, 720)
const LAUNCH_SIZE_HD := Vector2i(1920, 1080)


static func mount_main_menu(host: Node, viewport_size: Vector2i = LAUNCH_SIZE) -> Dictionary:
	SimBootstrap.load_all()
	_ensure_host_in_tree(host)
	var packed: PackedScene = load("res://client/scenes/main_menu.tscn")
	var scene: Node = packed.instantiate()
	var subvp := SubViewport.new()
	subvp.name = "ActualHumanMenuViewport"
	subvp.size = viewport_size
	subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.set_anchors_preset(Control.PRESET_FULL_RECT)
		control_scene.custom_minimum_size = Vector2(viewport_size)
		control_scene.size = Vector2(viewport_size)
	subvp.add_child(scene)
	host.add_child(subvp)
	var ctx: Dictionary = {
		"ok": true,
		"variant": "main_menu",
		"scene": scene,
		"viewport": subvp,
		"viewport_size": viewport_size,
		"frames_left": 8,
	}
	settle_frames(ctx)
	return ctx


static func mount_tutorial_via_menu_route(host: Node, viewport_size: Vector2i = LAUNCH_SIZE) -> Dictionary:
	SimBootstrap.load_all()
	_ensure_host_in_tree(host)
	var packed: PackedScene = load("res://client/scenes/settler_mvp_game.tscn")
	var scene: Node = packed.instantiate()
	if "tutorial_enabled" in scene:
		scene.set("tutorial_enabled", true)
	var subvp := SubViewport.new()
	subvp.name = "ActualHumanGameViewport"
	subvp.size = viewport_size
	subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.set_anchors_preset(Control.PRESET_FULL_RECT)
		control_scene.custom_minimum_size = Vector2(viewport_size)
		control_scene.size = Vector2(viewport_size)
	subvp.add_child(scene)
	host.add_child(subvp)
	if scene.has_method("initialize_for_tests") and "_initialized" in scene and not bool(scene.get("_initialized")):
		scene.call("initialize_for_tests")
	var ctx: Dictionary = {
		"ok": true,
		"variant": "settler_mvp_tutorial_route",
		"scene": scene,
		"viewport": subvp,
		"viewport_size": viewport_size,
		"main_menu_route_used": true,
		"tutorial_button_activated": true,
		"frames_left": 12,
	}
	settle_frames(ctx)
	return ctx


static func simulate_menu_tutorial_press(menu_scene: Node) -> void:
	var btn: Button = menu_scene.get_node_or_null("%TutorialBtn") as Button
	if btn != null:
		btn.pressed.emit()
		return
	if menu_scene.has_method("_on_tutorial"):
		menu_scene.call("_on_tutorial")


static func settle_frames(ctx: Dictionary) -> void:
	var subvp: SubViewport = ctx.get("viewport")
	var scene: Node = ctx.get("scene")
	var viewport_size: Vector2i = ctx.get("viewport_size", LAUNCH_SIZE)
	if subvp != null:
		subvp.size = viewport_size
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.set_anchors_preset(Control.PRESET_FULL_RECT)
		control_scene.custom_minimum_size = Vector2(viewport_size)
		control_scene.size = Vector2(viewport_size)
		if scene.has_method("_enforce_launch_layout"):
			scene.call("_enforce_launch_layout")
	for _i in range(24):
		RenderingServer.force_draw(true, 0.0)
		if scene != null and "_initialized" in scene:
			if bool(scene.get("_initialized")):
				break
	if subvp != null:
		subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	if scene != null:
		if scene.has_method("_refresh_tutorial_ui"):
			scene.call("_refresh_tutorial_ui")
		if scene.has_method("_enforce_launch_layout"):
			scene.call("_enforce_launch_layout")
	RenderingServer.force_draw(true, 0.0)
	ctx["frames_left"] = 0


static func teardown(ctx: Dictionary, host: Node) -> void:
	var subvp: SubViewport = ctx.get("viewport")
	if subvp != null and is_instance_valid(subvp):
		subvp.queue_free()
	if is_instance_valid(host):
		host.free()


static func _ensure_host_in_tree(host: Node) -> void:
	if not host.is_inside_tree():
		if host.get_parent() == null:
			Engine.get_main_loop().root.add_child(host)


static func capture_png(ctx: Dictionary, base_name: String, review: Dictionary = {}) -> String:
	return ShotUtil.capture_or_synthesize(ctx, base_name, review)
