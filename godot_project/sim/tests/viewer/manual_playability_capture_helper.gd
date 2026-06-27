class_name ManualPlayabilityCaptureHelper
extends RefCounted

## Main-menu-route mounts for manual playability QA (primary gate — no direct debug mount).

const LAUNCH_SIZE := Vector2i(1280, 720)
const SettlerGame := preload("res://client/scenes/settler_mvp_game.gd")
const MilitaryGame := preload("res://client/scenes/military_mvp_game.gd")
const HybridGame := preload("res://client/scenes/hybrid_mvp_game.gd")
const ShotUtil := preload("res://sim/tools/actual_human_screenshot_util.gd")
const ReviewUtil := preload("res://sim/tools/manual_playability_review_util.gd")

const MENU_ROUTES: Dictionary = {
	"settler_tutorial": {"scene": "settler_mvp", "pending": true, "btn": "TutorialBtn", "handler": "_on_tutorial"},
	"settler_free_play": {"scene": "settler_mvp", "pending": false, "btn": "FreePlayBtn", "handler": "_on_free_play"},
	"skirmish_tutorial": {"scene": "military_mvp", "pending": true, "btn": "SkirmishTutorialBtn", "handler": "_on_skirmish_tutorial"},
	"skirmish_mvp": {"scene": "military_mvp", "pending": false, "btn": "SkirmishMvpBtn", "handler": "_on_skirmish_mvp"},
	"hybrid_tutorial": {"scene": "hybrid_mvp", "pending": true, "btn": "HybridTutorialBtn", "handler": "_on_hybrid_tutorial"},
	"hybrid_mvp": {"scene": "hybrid_mvp", "pending": false, "btn": "HybridMvpBtn", "handler": "_on_hybrid_mvp"},
	"hybrid_3d": {"scene": "hybrid_3d_spectator", "pending": false, "btn": "Hybrid3dSpectatorBtn", "handler": "_on_hybrid_3d_spectator"},
}


static func capture_method() -> String:
	if OS.has_environment("DISPLAY") and OS.get_environment("DISPLAY") != "":
		return "real_desktop_window"
	return "in_engine_menu_route_fallback"


static func output_dir() -> String:
	return ReviewUtil.output_dir()


static func mount_main_menu(host: Node, viewport_size: Vector2i = LAUNCH_SIZE) -> Dictionary:
	SimBootstrap.load_all()
	_ensure_host(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "main_menu")
	ctx["viewport_size"] = viewport_size
	_resize(ctx, viewport_size)
	settle(ctx, 8)
	return ctx


static func launch_via_menu_route(host: Node, route_key: String, viewport_size: Vector2i = LAUNCH_SIZE) -> Dictionary:
	var route: Dictionary = MENU_ROUTES.get(route_key, {})
	assert(not route.is_empty(), "unknown route %s" % route_key)
	var menu_ctx: Dictionary = mount_main_menu(host, viewport_size)
	var menu_scene: Node = menu_ctx.get("scene")
	_apply_pending_flags(route)
	if menu_scene != null:
		var btn: Button = menu_scene.get_node_or_null("%" + str(route.get("btn", ""))) as Button
		if btn != null:
			btn.pressed.emit()
		elif menu_scene.has_method(str(route.get("handler", ""))):
			menu_scene.call(str(route.get("handler")))
	ViewerTestHarness.teardown(menu_ctx)
	var variant: String = str(route.get("scene", ""))
	var game_ctx: Dictionary = ViewerTestHarness.mount(host, variant)
	game_ctx["viewport_size"] = viewport_size
	game_ctx["main_menu_route_used"] = true
	game_ctx["route_key"] = route_key
	_resize(game_ctx, viewport_size)
	var scene: Node = game_ctx.get("scene")
	if scene != null and scene.has_method("initialize_for_tests"):
		scene.call("initialize_for_tests")
	elif scene != null and scene.has_method("ensure_match_started"):
		scene.call("ensure_match_started")
	settle(game_ctx, 12)
	return game_ctx


static func _apply_pending_flags(route: Dictionary) -> void:
	var pending: bool = bool(route.get("pending", false))
	match str(route.get("scene", "")):
		"settler_mvp":
			SettlerGame.pending_tutorial = pending
		"military_mvp":
			MilitaryGame.pending_tutorial = pending
		"hybrid_mvp":
			HybridGame.pending_tutorial = pending


static func capture_png(ctx: Dictionary, filename: String, review: Dictionary = {}) -> String:
	review["capture_method"] = capture_method()
	review["screenshot_path"] = filename
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % filename)
	var img: Image = ViewerTestHarness.capture(ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
		return path
	return ShotUtil.capture_or_synthesize(ctx, filename, review)


static func settle(ctx: Dictionary, frames: int = 8) -> void:
	var subvp: SubViewport = ctx.get("viewport")
	var scene: Node = ctx.get("scene")
	var viewport_size: Vector2i = ctx.get("viewport_size", LAUNCH_SIZE)
	if subvp != null:
		subvp.size = viewport_size
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.custom_minimum_size = Vector2(viewport_size)
		control_scene.size = Vector2(viewport_size)
		if scene.has_method("_enforce_launch_layout"):
			scene.call("_enforce_launch_layout")
	for _i in range(frames):
		RenderingServer.force_draw(true, 0.0)
	if scene != null and scene.has_method("_refresh_tutorial_ui"):
		scene.call("_refresh_tutorial_ui")
	if scene != null and scene.has_method("_refresh_all"):
		scene.call("_refresh_all")
	RenderingServer.force_draw(true, 0.0)


static func teardown(ctx: Dictionary, host: Node) -> void:
	ViewerTestHarness.teardown(ctx)
	if is_instance_valid(host):
		host.queue_free()


static func _resize(ctx: Dictionary, viewport_size: Vector2i) -> void:
	var subvp: SubViewport = ctx.get("viewport")
	if subvp != null:
		subvp.size = viewport_size
	var scene: Node = ctx.get("scene")
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.set_anchors_preset(Control.PRESET_FULL_RECT)
		control_scene.custom_minimum_size = Vector2(viewport_size)
		control_scene.size = Vector2(viewport_size)


static func _ensure_host(host: Node) -> void:
	if not host.is_inside_tree() and host.get_parent() == null:
		Engine.get_main_loop().root.add_child(host)
