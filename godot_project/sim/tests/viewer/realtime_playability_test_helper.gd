class_name RealtimePlayabilityTestHelper
extends RefCounted

## Shared checks for real-time default behaviour across player-facing modes.

const CaptureHelper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")


static func mount_route(host: Node, route_key: String) -> Dictionary:
	return CaptureHelper.launch_via_menu_route(host, route_key)


static func check_starts_playing(scene: Node, route_key: String) -> Dictionary:
	var paused: bool = _is_paused(scene, route_key)
	if paused:
		return {"ok": false, "msg": "%s starts paused" % route_key}
	var status: String = _status_text(scene, route_key)
	if not status.contains("Playing"):
		return {"ok": false, "msg": "%s status missing Playing: %s" % [route_key, status]}
	return {"ok": true, "msg": ""}


static func check_tick_advances_without_play_or_step(ctx: Dictionary, route_key: String) -> Dictionary:
	var scene: Node = ctx.get("scene")
	var tick0: int = current_tick(scene, route_key)
	for _i in range(6):
		ViewerTestHarness.tick_frames(ctx)
		if scene.has_method("_on_tick_timer"):
			scene.call("_on_tick_timer")
		elif scene.has_method("_on_autoplay_tick"):
			scene.call("_on_autoplay_tick")
	var tick1: int = current_tick(scene, route_key)
	if tick1 <= tick0:
		return {"ok": false, "msg": "%s tick did not advance without Play/Step (%d -> %d)" % [route_key, tick0, tick1]}
	return {"ok": true, "msg": ""}


static func check_pause_stops_tick(ctx: Dictionary, route_key: String) -> Dictionary:
	var scene: Node = ctx.get("scene")
	_invoke_pause(scene, route_key)
	var tick0: int = current_tick(scene, route_key)
	for _i in range(4):
		ViewerTestHarness.tick_frames(ctx)
		if scene.has_method("_on_tick_timer"):
			scene.call("_on_tick_timer")
	var tick1: int = current_tick(scene, route_key)
	if tick1 != tick0:
		return {"ok": false, "msg": "%s tick advanced while paused (%d -> %d)" % [route_key, tick0, tick1]}
	return {"ok": true, "msg": ""}


static func check_space_resumes_tick(ctx: Dictionary, route_key: String) -> Dictionary:
	var scene: Node = ctx.get("scene")
	if scene.has_method("_handle_keyboard_shortcut"):
		var event := InputEventKey.new()
		event.keycode = KEY_SPACE
		event.pressed = true
		scene.call("_handle_keyboard_shortcut", event)
	elif scene.has_method("invoke_play_toggle"):
		scene.call("invoke_play_toggle")
	else:
		_invoke_play(scene, route_key)
	var tick0: int = current_tick(scene, route_key)
	for _i in range(4):
		ViewerTestHarness.tick_frames(ctx)
		if scene.has_method("_on_tick_timer"):
			scene.call("_on_tick_timer")
		elif scene.has_method("_on_autoplay_tick"):
			scene.call("_on_autoplay_tick")
	var tick1: int = current_tick(scene, route_key)
	if tick1 <= tick0:
		return {"ok": false, "msg": "%s tick did not resume after Space/play (%d -> %d)" % [route_key, tick0, tick1]}
	return {"ok": true, "msg": ""}


static func forest_or_resource_visible(scene: Node, _route_key: String) -> bool:
	var map: MapViewport = scene.find_child("MapViewport", true, false) as MapViewport
	if map == null:
		return false
	var render: Dictionary = map.render_data
	if render.is_empty() and scene.has_method("get_render_data"):
		render = scene.call("get_render_data")
	for td in render.get("tree_density", []):
		if int(td.get("tree_density", 0)) >= 40:
			return true
	for res in render.get("resources", []):
		var dtype: String = str(res.get("deposit_type", res.get("field_type", "")))
		if dtype in ["forest", "rock"]:
			return true
	for tile in render.get("terrain", []):
		if str(tile.get("terrain_type", "")) in ["forest", "rock"]:
			return true
	return false


static func current_tick(scene: Node, route_key: String) -> int:
	if scene.has_method("get_current_tick"):
		return int(scene.call("get_current_tick"))
	if "controller" in scene and scene.controller.has_method("get_snapshot"):
		return int(scene.controller.get_snapshot().get("tick", 0))
	if "controller" in scene and "snapshot" in scene.controller:
		return int(scene.controller.snapshot.get("tick", 0))
	return 0


static func _is_paused(scene: Node, route_key: String) -> bool:
	if route_key == "hybrid_3d":
		return not bool(scene.get("controller").autoplay_enabled) if "controller" in scene else true
	if scene.has_method("is_sim_paused"):
		return bool(scene.call("is_sim_paused"))
	if "controller" in scene:
		var ctrl: Variant = scene.controller
		if ctrl != null and "paused" in ctrl:
			return bool(ctrl.paused)
		if ctrl != null and "autoplay_enabled" in ctrl:
			return not bool(ctrl.autoplay_enabled)
	return true


static func _status_text(scene: Node, route_key: String) -> String:
	if scene.has_method("get_status_text"):
		return str(scene.call("get_status_text"))
	if "controller" in scene and scene.controller.has_method("build_status_text"):
		return scene.controller.build_status_text()
	if route_key == "hybrid_3d" and "controller" in scene:
		return scene.controller.build_transport_bar_text()
	return ""


static func _invoke_pause(scene: Node, route_key: String) -> void:
	if route_key == "hybrid_3d":
		if scene.has_method("_on_play_pause_pressed") and "controller" in scene and scene.controller.autoplay_enabled:
			scene.call("_on_play_pause_pressed")
		return
	if scene.has_method("invoke_pause"):
		scene.call("invoke_pause")
	elif "controller" in scene and scene.controller.has_method("set_paused"):
		scene.controller.set_paused(true)


static func _invoke_play(scene: Node, route_key: String) -> void:
	if route_key == "hybrid_3d":
		if scene.has_method("_on_play_pause_pressed") and "controller" in scene and not scene.controller.autoplay_enabled:
			scene.call("_on_play_pause_pressed")
		return
	if scene.has_method("invoke_play"):
		scene.call("invoke_play")
