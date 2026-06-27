extends Control

## MRP Phase C6 — 3D hybrid bot-vs-bot walkaround spectator.

const SCREEN_SIZE := Vector2i(1920, 1080)
const AUTOPLAY_BASE_SEC: float = 0.4
const SCENE_PATH := "res://client/scenes/hybrid_3d_bot_spectator.tscn"

var controller: Hybrid3dSpectatorController = Hybrid3dSpectatorController.new()

@onready var _world_viewport: SubViewport = %WorldViewport
@onready var _world_renderer: Hybrid3dWorldRenderer = %WorldRenderer
@onready var _camera: Hybrid3dCameraController = %Camera
@onready var _hud_root: Control = %HudRoot
@onready var _result: Label = %ResultLabel

var _autoplay_timer: Timer


func ensure_match_started() -> void:
	if controller == null:
		return
	if controller.dual == null or controller.snapshot.is_empty():
		controller.reset_match()
	_refresh_all()


func _ready() -> void:
	size = SCREEN_SIZE
	SimBootstrap.load_all()
	BuildingCatalog.load_catalog()
	MilitaryCatalog.load_catalog()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	_setup_autoplay_timer()
	_connect_buttons()
	controller.reset_match()
	_refresh_all()


func _setup_autoplay_timer() -> void:
	_autoplay_timer = Timer.new()
	_autoplay_timer.one_shot = false
	add_child(_autoplay_timer)
	_autoplay_timer.timeout.connect(_on_autoplay_tick)


func _connect_buttons() -> void:
	_connect_btn("%PlayPauseBtn", _on_play_pause_pressed)
	_connect_btn("%StepTickBtn", _on_step_tick_pressed)
	_connect_btn("%Step10Btn", func(): _step_many(10))
	_connect_btn("%Step50Btn", func(): _step_many(50))
	_connect_btn("%SpeedBtn", _on_speed_pressed)
	_connect_btn("%FollowBotABtn", func(): _set_follow("p1"))
	_connect_btn("%FollowBotBBtn", func(): _set_follow("p2"))
	_connect_btn("%FollowBattleBtn", func(): _set_follow("battle"))
	_connect_btn("%FollowObjectiveBtn", func(): _set_follow("objective"))
	_connect_btn("%ToggleRoadsBtn", func(): _toggle_overlay("roads"))
	_connect_btn("%ToggleSupplyBtn", func(): _toggle_overlay("supply"))
	_connect_btn("%ToggleCombatBtn", func(): _toggle_overlay("combat_events"))
	_connect_btn("%ResetBtn", _on_reset_pressed)
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	_connect_btn("%ScreenshotBtn", _on_screenshot_pressed)


func _connect_btn(path: String, callable: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(callable)


func _on_snapshot_refreshed(_snapshot: Dictionary) -> void:
	_refresh_all()


func _refresh_all() -> void:
	if _world_renderer != null:
		_world_renderer.apply_snapshot(
			controller.snapshot,
			controller.overlay_visibility,
			controller.selected_q,
			controller.selected_r,
		)
	if _camera != null:
		_camera.set_mode_name(controller.camera_mode)
		_camera.follow_position = controller.get_follow_position()
	Hybrid3dSpectatorHud.refresh(_hud_root, controller)


func _on_autoplay_tick() -> void:
	if controller.autoplay_enabled:
		controller.autoplay_step()


func _on_play_pause_pressed() -> void:
	controller.autoplay_enabled = not controller.autoplay_enabled
	if controller.autoplay_enabled:
		_autoplay_timer.start()
	else:
		_autoplay_timer.stop()
	_update_autoplay_timer()
	_refresh_all()


func _on_step_tick_pressed() -> void:
	controller.step_tick()
	_result.text = "Advanced one tick"


func _step_many(count: int) -> void:
	controller.step_ticks(count)
	_result.text = "Stepped %d ticks" % count


func _on_speed_pressed() -> void:
	if controller.speed_multiplier < 1.5:
		controller.speed_multiplier = 2.0
	elif controller.speed_multiplier < 3.5:
		controller.speed_multiplier = 4.0
	else:
		controller.speed_multiplier = 1.0
	_update_autoplay_timer()
	_refresh_all()


func _update_autoplay_timer() -> void:
	var interval: float = AUTOPLAY_BASE_SEC / maxf(controller.speed_multiplier, 0.25)
	_autoplay_timer.wait_time = interval
	if controller.autoplay_enabled:
		_autoplay_timer.start(interval)


func _on_reset_pressed() -> void:
	controller.reset_match()
	_result.text = "Match reset"


func _set_follow(target: String) -> void:
	controller.set_follow_target(target)
	if _camera != null:
		_camera.jump_to(controller.get_follow_position())
	_refresh_all()


func _toggle_overlay(name: String) -> void:
	var vis: bool = not controller.overlay_visibility.get(name, true)
	controller.set_overlay_visible(name, vis)
	_refresh_all()


func _on_screenshot_pressed() -> void:
	var path: String = SimPaths.default_export_file("hybrid_3d_spectator_%d.png" % int(controller.snapshot.get("tick", 0)))
	await get_tree().process_frame
	var img: Image = _world_viewport.get_texture().get_image()
	if img != null and img.save_png(path) == OK:
		_result.text = "Screenshot: %s" % path
	else:
		_result.text = "Screenshot failed"


func select_hex_for_tests(q: int, r: int) -> void:
	controller.set_selection(q, r)
	_refresh_all()


func follow_bot_for_tests(player_id: String) -> void:
	_set_follow(player_id)


func get_world_entity_count() -> Dictionary:
	return _world_renderer.get_entity_count() if _world_renderer != null else {}


func advance_for_tests(steps: int) -> void:
	controller.step_ticks(steps)


func advance_until_combat_for_tests(max_steps: int = 600) -> Dictionary:
	return controller.advance_until_combat_or_cap(max_steps)


static func scene_path() -> String:
	return SCENE_PATH
