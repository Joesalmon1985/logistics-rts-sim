class_name SettlerMvpTestHelper
extends RefCounted

## Shared mount/wait helpers for Settler MVP viewer tests.


static func mount_settler(host: Node) -> Dictionary:
	SimBootstrap.load_all()
	if not host.is_inside_tree():
		if host.get_parent() == null:
			Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "settler_mvp")
	_force_layout(ctx)
	var scene: Node = ctx.get("scene")
	if scene != null:
		scene.call("initialize_for_tests")
		if scene is Control:
			(scene as Control).queue_redraw()
	settle_frames(ctx)
	return ctx


static func scene_call(scene: Node, method: String, args: Array = []) -> Variant:
	if not scene.has_method(method):
		push_error("SettlerMvpTestHelper: missing %s on %s" % [method, scene])
		return null
	return scene.callv(method, args)


static func invoke_play(scene: Node) -> void:
	if scene.has_method("invoke_play"):
		scene.call("invoke_play")
		return
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null:
		controller.set_paused(false)


static func invoke_pause(scene: Node) -> void:
	if scene.has_method("invoke_pause"):
		scene.call("invoke_pause")
		return
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null:
		controller.set_paused(true)


static func invoke_step(scene: Node) -> void:
	if scene.has_method("invoke_step"):
		scene.call("invoke_step")
		return
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null and controller.paused:
		controller.step_tick()


static func get_current_tick(scene: Node) -> int:
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null:
		return controller.get_current_tick()
	return int(scene_call(scene, "get_current_tick"))


static func get_status_text(scene: Node) -> String:
	if scene.has_method("get_status_text"):
		var text: String = str(scene.call("get_status_text"))
		if text != "":
			return text
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null:
		return controller.build_status_text()
	return ""


static func get_inspector_text(scene: Node) -> String:
	if scene.has_method("get_inspector_text"):
		var text: String = str(scene.call("get_inspector_text"))
		if text != "":
			return text
	return ""


static func is_sim_paused(scene: Node) -> bool:
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null:
		return controller.paused
	return bool(scene_call(scene, "is_sim_paused"))


static func run_tick(scene: Node) -> void:
	var controller: SettlerMvpController = get_controller(scene)
	if controller != null and not controller.paused:
		controller.step_tick()
	if scene.has_method("_refresh_panels"):
		scene.call("_refresh_panels")


static func get_controller(scene: Node) -> SettlerMvpController:
	return scene.controller if "controller" in scene else null


static func invoke_build_mode(scene: Node, building_type: String) -> void:
	if scene.has_method("invoke_build_mode"):
		scene.call("invoke_build_mode", building_type)
		return
	get_controller(scene).set_build_mode(building_type)


static func is_help_visible(scene: Node) -> bool:
	if scene.has_method("is_help_visible"):
		return bool(scene.call("is_help_visible"))
	return true


static func get_map(scene: Node) -> MapViewport:
	return scene.find_child("MapViewport", true, false) as MapViewport


static func get_selected_hex(scene: Node) -> Vector2i:
	var map: MapViewport = get_map(scene)
	if map == null:
		return Vector2i(-1, -1)
	return Vector2i(map.selected_q, map.selected_r)


static func simulate_map_click_at_hex(scene: Node, q: int, r: int) -> void:
	var map: MapViewport = scene.find_child("MapViewport", true, false) as MapViewport
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
	var inspector: RichTextLabel = scene.find_child("Inspector", true, false) as RichTextLabel
	if inspector != null and "controller" in scene:
		var selection: Dictionary = scene.controller.get_selection_at(q, r)
		inspector.text = SelectionFormatter.format(
			selection,
			scene.controller.get_snapshot(),
			scene.controller.get_mode_context() if scene.controller.has_method("get_mode_context") else {},
		)


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
	host.queue_free()


static func simulate_key(scene: Node, keycode: Key) -> void:
	var event := InputEventKey.new()
	event.keycode = keycode
	event.pressed = true
	event.echo = false
	if scene.has_method("_handle_keyboard_shortcut"):
		scene.call("_handle_keyboard_shortcut", event)


static func decorative_labels(scene: Node) -> Array:
	return [
		scene.get_node_or_null("RootVBox/TopBar/StatusLabel"),
		scene.get_node_or_null("RootVBox/ContentHBox/RightPanel/TitleLabel"),
		scene.get_node_or_null("RootVBox/ContentHBox/RightPanel/BuildHeader"),
	]


static func focus_is_decorative(scene: Node) -> bool:
	var focus_owner: Control = scene.get_viewport().gui_get_focus_owner()
	if focus_owner == null:
		return false
	for node in decorative_labels(scene):
		if node == focus_owner:
			return true
	return false


static func map_fully_covered_by_stop(scene: Node, map: MapViewport) -> bool:
	var map_rect: Rect2 = map.get_rect()
	var covered: bool = false
	_walk(scene, func(node: Control) -> void:
		if node == map:
			return
		if node is Button:
			return
		if node.mouse_filter != Control.MOUSE_FILTER_STOP:
			return
		var rect: Rect2 = node.get_rect()
		if rect.size.x > 4 and rect.size.y > 4 and rect.encloses(map_rect):
			covered = true
	)
	return covered


static func _walk(node: Node, callback: Callable) -> void:
	if node is Control:
		callback.call(node)
	for child in node.get_children():
		_walk(child, callback)
