class_name MilitaryMvpTestHelper
extends RefCounted

## Shared mount/wait helpers for Skirmish MVP viewer tests.

const HUB_Q: int = 3
const HUB_R: int = 3


static func mount_military(host: Node) -> Dictionary:
	SimBootstrap.load_all()
	if not host.is_inside_tree():
		if host.get_parent() == null:
			Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "military_mvp")
	_force_layout(ctx)
	var scene: Node = ctx.get("scene")
	if scene != null:
		scene.call("initialize_for_tests")
		if scene is Control:
			(scene as Control).queue_redraw()
	settle_frames(ctx)
	return ctx


static func invoke_play(scene: Node) -> void:
	if scene.has_method("invoke_play"):
		scene.call("invoke_play")
		return
	get_controller(scene).set_paused(false)


static func invoke_pause(scene: Node) -> void:
	if scene.has_method("invoke_pause"):
		scene.call("invoke_pause")
		return
	get_controller(scene).set_paused(true)


static func invoke_step(scene: Node) -> void:
	if scene.has_method("invoke_step"):
		scene.call("invoke_step")
		return
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null and controller.paused:
		controller.step_tick()


static func get_current_tick(scene: Node) -> int:
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null:
		return controller.get_current_tick()
	return 0


static func get_status_text(scene: Node) -> String:
	if scene.has_method("get_status_text"):
		return str(scene.call("get_status_text"))
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null:
		return controller.build_status_text()
	return ""


static func get_inspector_text(scene: Node) -> String:
	if scene.has_method("get_inspector_text"):
		return str(scene.call("get_inspector_text"))
	return ""


static func is_sim_paused(scene: Node) -> bool:
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null:
		return controller.paused
	return true


static func run_tick(scene: Node) -> void:
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null and not controller.paused:
		controller.step_tick()
	if scene.has_method("_refresh_panels"):
		scene.call("_refresh_panels")


static func get_controller(scene: Node) -> MilitaryMvpController:
	return scene.controller if "controller" in scene else null


static func get_map(scene: Node) -> MapViewport:
	return scene.find_child("MapViewport", true, false) as MapViewport


static func simulate_map_click_at_hex(scene: Node, q: int, r: int) -> void:
	var map: MapViewport = get_map(scene)
	if map == null:
		return
	if map.render_data.is_empty() and "controller" in scene:
		map.set_render_data(scene.controller.get_render_data())
	if map.size.x < 10 or map.size.y < 10:
		map.size = Vector2(900, 600)
	map.fit_to_content()
	var local: Vector2 = map.hex_center_local(q, r)
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = local
	map._gui_input(press)
	if scene.has_method("_on_map_click"):
		scene.call("_on_map_click", q, r)
	var controller: MilitaryMvpController = get_controller(scene)
	if controller != null:
		var selection: Dictionary = controller.get_selection_at(q, r)
		var inspector: RichTextLabel = scene.find_child("Inspector", true, false) as RichTextLabel
		if inspector != null:
			inspector.text = MilitaryCatalog.format_selection(selection, controller.get_snapshot())


static func _force_layout(ctx: Dictionary, fill_viewport: bool = false) -> void:
	var subvp: SubViewport = ctx.get("viewport")
	var scene: Node = ctx.get("scene")
	if subvp != null:
		subvp.size = ViewerTestHarness.SIZE
	if scene is Control:
		var control_scene: Control = scene as Control
		if fill_viewport:
			control_scene.set_anchors_preset(Control.PRESET_FULL_RECT)
		else:
			control_scene.set_anchors_preset(Control.PRESET_TOP_LEFT)
		control_scene.position = Vector2.ZERO
		control_scene.custom_minimum_size = ViewerTestHarness.SIZE
		control_scene.size = ViewerTestHarness.SIZE


static func settle_frames(ctx: Dictionary) -> void:
	_force_layout(ctx)
	while not ViewerTestHarness.tick_frames(ctx):
		pass
	var subvp: SubViewport = ctx.get("viewport")
	if subvp != null:
		subvp.render_target_update_mode = SubViewport.UPDATE_ONCE
		subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	RenderingServer.force_draw(true, 0.0)


static func teardown(ctx: Dictionary, host: Node) -> void:
	ViewerTestHarness.teardown(ctx)
	if is_instance_valid(host):
		host.free()


static func simulate_key(scene: Node, keycode: Key) -> void:
	var event := InputEventKey.new()
	event.keycode = keycode
	event.pressed = true
	event.echo = false
	if scene.has_method("_handle_keyboard_shortcut"):
		scene.call("_handle_keyboard_shortcut", event)
