class_name SettlerMvpGame
extends Control

## Settler MVP — human-playable settlement logistics mode.

const SCREEN_SIZE := Vector2i(1920, 1080)
const LAUNCH_MIN_SIZE := Vector2i(1280, 720)
const RIGHT_PANEL_MIN_WIDTH := 380.0
const TICK_BASE_SEC: float = 0.25
const PAN_STEP_PX: float = 48.0
const ZOOM_STEP: float = 1.12

const BUILD_KEY_MAP: Dictionary = {
	KEY_1: "woodcutter",
	KEY_2: "forester",
	KEY_3: "sawmill",
	KEY_4: "quarry",
}

@export var tutorial_enabled: bool = false
static var pending_tutorial: bool = false

var controller: SettlerMvpController = SettlerMvpController.new()
var tutorial: SettlerTutorialController = SettlerTutorialController.new()

@onready var _map: MapViewport = %MapViewport
@onready var _status: Label = %StatusLabel
@onready var _objective: RichTextLabel = %ObjectivePanel
@onready var _stock: Label = %StockLabel
@onready var _warnings: RichTextLabel = %WarningsPanel
@onready var _inspector: RichTextLabel = %Inspector
@onready var _event_log: RichTextLabel = %EventLog
@onready var _help: RichTextLabel = %HelpPanel
@onready var _preview: Label = %PreviewLabel
@onready var _victory: Label = %VictoryLabel
@onready var _play_btn: Button = %PlayBtn
@onready var _map_area: PanelContainer = %MapArea
@onready var _build_palette: BuildPalette = %BuildPalette
@onready var _mode_banner: PanelContainer = %ModeBanner
@onready var _mode_title: Label = %ModeTitle
@onready var _mode_instruction: Label = %ModeInstruction
@onready var _tutorial_panel: PanelContainer = %TutorialPanel
@onready var _tutorial_title: Label = %TutorialTitle
@onready var _tutorial_progress: Label = %TutorialProgress
@onready var _tutorial_instruction: RichTextLabel = %TutorialInstruction
@onready var _tutorial_next: Button = %TutorialNextBtn
@onready var _tutorial_back: Button = %TutorialBackBtn
@onready var _tutorial_skip: Button = %TutorialSkipBtn
@onready var _tutorial_exit: Button = %TutorialExitBtn
@onready var _tutorial_next_reason: Label = %TutorialNextReasonLabel
@onready var _tutorial_step_kind: Label = %TutorialStepKind
@onready var _mode_status: Label = %ModeStatusLabel

var _sprite_layer: SettlerSpriteLayer

var _tick_timer: Timer
var _event_lines: PackedStringArray = PackedStringArray()
var _map_fitted: bool = false
var _help_visible: bool = true
var _initialized: bool = false
var _last_selection: Dictionary = {}


func enable_tutorial_for_tests() -> void:
	tutorial_enabled = true
	tutorial.enabled = true
	if _tutorial_panel != null:
		_tutorial_panel.visible = true
	tutorial.start(controller.get_snapshot(), _map)
	_refresh_tutorial_ui()


func initialize_for_tests() -> void:
	if _initialized:
		return
	_setup_scene()


func _bind_ui_nodes() -> void:
	_map = find_child("MapViewport", true, false) as MapViewport
	_status = find_child("StatusLabel", true, false) as Label
	_objective = find_child("ObjectivePanel", true, false) as RichTextLabel
	_stock = find_child("StockLabel", true, false) as Label
	_warnings = find_child("WarningsPanel", true, false) as RichTextLabel
	_inspector = find_child("Inspector", true, false) as RichTextLabel
	_event_log = find_child("EventLog", true, false) as RichTextLabel
	_help = find_child("HelpPanel", true, false) as RichTextLabel
	_preview = find_child("PreviewLabel", true, false) as Label
	_victory = find_child("VictoryLabel", true, false) as Label
	_play_btn = find_child("PlayBtn", true, false) as Button
	_map_area = find_child("MapArea", true, false) as PanelContainer
	_build_palette = find_child("BuildPalette", true, false) as BuildPalette
	_mode_banner = find_child("ModeBanner", true, false) as PanelContainer
	_mode_title = find_child("ModeTitle", true, false) as Label
	_mode_instruction = find_child("ModeInstruction", true, false) as Label
	_tutorial_panel = find_child("TutorialPanel", true, false) as PanelContainer
	_tutorial_title = find_child("TutorialTitle", true, false) as Label
	_tutorial_progress = find_child("TutorialProgress", true, false) as Label
	_tutorial_instruction = find_child("TutorialInstruction", true, false) as RichTextLabel
	_tutorial_next = find_child("TutorialNextBtn", true, false) as Button
	_tutorial_back = find_child("TutorialBackBtn", true, false) as Button
	_tutorial_skip = find_child("TutorialSkipBtn", true, false) as Button
	_tutorial_exit = find_child("TutorialExitBtn", true, false) as Button
	_tutorial_next_reason = find_child("TutorialNextReasonLabel", true, false) as Label
	_tutorial_step_kind = find_child("TutorialStepKind", true, false) as Label
	_mode_status = find_child("ModeStatusLabel", true, false) as Label
	_sprite_layer = find_child("SettlerSpriteLayer", true, false) as SettlerSpriteLayer


func _ready() -> void:
	if _initialized:
		return
	custom_minimum_size = Vector2(LAUNCH_MIN_SIZE)
	if not (get_parent() is SubViewport):
		var win: Window = get_window()
		if win != null:
			win.min_size = LAUNCH_MIN_SIZE
	_setup_scene()
	call_deferred("_focus_play_on_start")


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		call_deferred("_enforce_launch_layout")


func _setup_scene() -> void:
	_bind_ui_nodes()
	if _map == null:
		push_error("SettlerMvpGame: MapViewport not found")
		return
	if pending_tutorial:
		tutorial_enabled = true
		pending_tutorial = false
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.message_logged.connect(_on_message_logged)
	controller.victory_achieved.connect(_on_victory)
	_map.selection_clicked.connect(_on_map_click)
	_map.hover_changed.connect(_on_map_hover)
	if _sprite_layer == null and _map != null:
		_sprite_layer = SettlerSpriteLayer.new()
		_sprite_layer.name = "SettlerSpriteLayer"
		_sprite_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
		_sprite_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_map.add_child(_sprite_layer)
	if _sprite_layer != null:
		_sprite_layer.setup(_map)
		_sprite_layer.animation_enabled = true
	_map.hover_changed.connect(func(_q, _r):
		if _sprite_layer != null:
			_sprite_layer.queue_redraw()
	)
	_map.enable_hover = true
	_map.auto_fit_on_data_change = not _map_fitted
	_map.show_grid = false
	_map.show_coord_labels = false
	_map.show_logistics = true
	_map.show_forest_density = true
	_map.show_internal_title_bar = false
	_map.use_settler_sprites = true
	_map.mouse_filter = Control.MOUSE_FILTER_STOP
	_map.enable_camera_control = true
	_ensure_right_panel_scroll()
	_enforce_launch_layout()
	_configure_input_filters()
	_setup_tick_timer()
	_help.text = controller.build_help_text()
	_wire_build_palette()
	_wire_time_buttons()
	_wire_tutorial()
	tutorial.enabled = tutorial_enabled
	if _tutorial_panel != null:
		_tutorial_panel.visible = tutorial_enabled
	if _build_palette != null:
		_build_palette.ensure_built()
	_start_game()
	_initialized = true
	call_deferred("_enforce_launch_layout")


func _configure_input_filters() -> void:
	_set_decorative_control(_status)
	_set_decorative_control(_stock)
	_set_decorative_control(_preview)
	_set_decorative_control(_victory)
	_set_decorative_richtext(_objective)
	_set_decorative_richtext(_warnings)
	_set_decorative_richtext(_inspector)
	_set_decorative_richtext(_event_log)
	_set_decorative_richtext(_help)
	_set_decorative_richtext(_tutorial_instruction)
	_set_decorative_control(_mode_title)
	_set_decorative_control(_mode_instruction)
	var title: Label = get_node_or_null("RootVBox/ContentHBox/RightPanel/TitleLabel") as Label
	_set_decorative_control(title)
	var build_header: Label = get_node_or_null("RootVBox/ContentHBox/RightPanel/BuildHeader") as Label
	_set_decorative_control(build_header)
	if _map_area != null:
		_map_area.mouse_filter = Control.MOUSE_FILTER_PASS


func _get_right_panel() -> VBoxContainer:
	var scroll: ScrollContainer = find_child("RightPanelScroll", true, false) as ScrollContainer
	if scroll != null:
		return scroll.get_child(0) as VBoxContainer
	return find_child("RightPanel", true, false) as VBoxContainer


func _ensure_right_panel_scroll() -> void:
	# RightPanelScroll is authored in settler_mvp_game.tscn (HSplitContainer layout).
	pass


func _enforce_launch_layout() -> void:
	if not is_inside_tree():
		return
	var viewport_w: float = get_viewport_rect().size.x
	if viewport_w < 200.0:
		return
	var right_w: float = clampf(RIGHT_PANEL_MIN_WIDTH, 340.0, viewport_w * 0.4)
	var split_x: int = int(clampf(viewport_w - right_w - 6.0, 320.0, viewport_w - right_w))
	var hsplit: HSplitContainer = get_node_or_null("RootVBox/ContentHBox") as HSplitContainer
	if hsplit != null:
		hsplit.split_offset = split_x
	var scroll: ScrollContainer = find_child("RightPanelScroll", true, false) as ScrollContainer
	if scroll != null:
		scroll.custom_minimum_size.x = right_w
	if _map != null:
		_map.custom_minimum_size = Vector2.ZERO
		_map.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_map.size_flags_vertical = Control.SIZE_EXPAND_FILL
	if _map_area != null:
		_map_area.custom_minimum_size = Vector2.ZERO
		_map_area.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_map_area.size_flags_stretch_ratio = 1.0


func get_viewport_size_for_probes() -> Vector2i:
	var vp: Viewport = get_viewport()
	if vp == null:
		return LAUNCH_MIN_SIZE
	return Vector2i(vp.get_visible_rect().size)


func get_right_panel_global_rect() -> Rect2:
	var scroll: Control = find_child("RightPanelScroll", true, false) as Control
	if scroll != null:
		return scroll.get_global_rect()
	var right: Control = find_child("RightPanel", true, false) as Control
	if right == null:
		return Rect2()
	return right.get_global_rect()


func get_map_viewport_global_rect() -> Rect2:
	if _map == null:
		return Rect2()
	return _map.get_global_rect()


func get_mode_status_text() -> String:
	return _mode_status.text if _mode_status != null else ""


func get_help_text() -> String:
	if _help == null:
		return ""
	return _help.get_parsed_text()


func _set_decorative_control(node: Control) -> void:
	if node == null:
		return
	node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	node.focus_mode = Control.FOCUS_NONE


func _set_decorative_richtext(node: RichTextLabel) -> void:
	if node == null:
		return
	node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	node.focus_mode = Control.FOCUS_NONE


func _focus_play_on_start() -> void:
	if _play_btn != null and is_inside_tree():
		_play_btn.grab_focus()


func _setup_tick_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.one_shot = false
	add_child(_tick_timer)
	_tick_timer.timeout.connect(_on_tick_timer)
	_update_tick_timer()


func _wire_build_palette() -> void:
	if _build_palette == null:
		_wire_legacy_build_buttons()
		return
	_build_palette.build_selected.connect(func(t): _set_build_mode(t))
	_build_palette.road_selected.connect(invoke_road_mode)
	_build_palette.cancel_selected.connect(invoke_cancel_modes)


func _wire_legacy_build_buttons() -> void:
	_connect_btn("%RoadBtn", invoke_road_mode)
	_connect_btn("%WoodcutterBtn", func(): _set_build_mode("woodcutter"))
	_connect_btn("%ForesterBtn", func(): _set_build_mode("forester"))
	_connect_btn("%SawmillBtn", func(): _set_build_mode("sawmill"))
	_connect_btn("%QuarryBtn", func(): _set_build_mode("quarry"))
	_connect_btn("%CancelBtn", invoke_cancel_modes)


func _wire_time_buttons() -> void:
	_connect_btn("%PauseBtn", invoke_pause)
	_connect_btn("%PlayBtn", invoke_play)
	_connect_btn("%Speed1Btn", func(): invoke_set_speed(1.0))
	_connect_btn("%Speed2Btn", func(): invoke_set_speed(2.0))
	_connect_btn("%Speed4Btn", func(): invoke_set_speed(4.0))
	_connect_btn("%StepBtn", invoke_step)
	_connect_btn("%NewGameBtn", func(): _start_game())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))


func _wire_tutorial() -> void:
	tutorial.step_changed.connect(_on_tutorial_step_changed)
	tutorial.tutorial_completed.connect(_on_tutorial_completed)
	if _tutorial_next != null:
		_tutorial_next.pressed.connect(func(): tutorial.advance_manual(); _refresh_tutorial_ui())
	if _tutorial_back != null:
		_tutorial_back.pressed.connect(func(): tutorial.retreat_manual(); _refresh_tutorial_ui())
	if _tutorial_skip != null:
		_tutorial_skip.pressed.connect(_skip_tutorial)
	if _tutorial_exit != null:
		_tutorial_exit.pressed.connect(_exit_tutorial_to_menu)


func _connect_btn(node_path: String, callback: Callable) -> void:
	var btn: Button = get_node_or_null(node_path)
	if btn != null:
		btn.pressed.connect(callback)


func _set_build_mode(building_type: String) -> void:
	controller.set_build_mode(building_type)
	_refresh_mode_ui()


func invoke_play() -> void:
	controller.set_paused(false)
	_update_tick_timer()
	_refresh_panels()


func invoke_pause() -> void:
	controller.set_paused(true)
	_update_tick_timer()
	_refresh_panels()


func invoke_step() -> void:
	if not controller.paused:
		return
	controller.step_tick()
	_refresh_panels()


func invoke_set_speed(multiplier: float) -> void:
	controller.set_speed(multiplier)
	_update_tick_timer()
	_refresh_panels()


func invoke_build_mode(building_type: String) -> void:
	_set_build_mode(building_type)


func invoke_road_mode() -> void:
	controller.set_road_mode(true)
	_refresh_mode_ui()


func invoke_cancel_modes() -> void:
	controller.clear_modes()
	_preview.text = ""
	_refresh_mode_ui()
	_refresh_map_overlays()


func toggle_help_visible() -> void:
	_help_visible = not _help_visible
	if _help != null:
		_help.visible = _help_visible


func is_help_visible() -> bool:
	return _help_visible


func get_status_text() -> String:
	return _status.text if _status != null else ""


func get_inspector_text() -> String:
	if _inspector == null:
		return ""
	return _inspector.get_parsed_text()


func get_selected_hex() -> Vector2i:
	return Vector2i(_map.selected_q, _map.selected_r)


func is_sim_paused() -> bool:
	return controller.paused


func get_current_tick() -> int:
	return controller.get_current_tick()


func is_tutorial_enabled() -> bool:
	return tutorial_enabled and tutorial.enabled


func get_tutorial_step_index() -> int:
	return tutorial.step_index


func simulate_map_click_at_hex(q: int, r: int) -> void:
	var local: Vector2 = _map.hex_center_local(q, r)
	simulate_map_click_at_local(local)


func simulate_map_click_at_local(local: Vector2) -> void:
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = local
	_map._gui_input(press)


func process_sim_frames(count: int) -> void:
	for _i in range(count):
		if not controller.paused:
			_on_tick_timer()
		await Engine.get_main_loop().process_frame


func _start_game() -> void:
	controller.reset_game()
	_event_lines = PackedStringArray()
	_victory.visible = false
	_map_fitted = false
	_map.auto_fit_on_data_change = true
	if tutorial.enabled:
		tutorial.start(controller.get_snapshot(), _map)
		_refresh_tutorial_ui()
	_refresh_all()
	call_deferred("_focus_play_on_start")


func _skip_tutorial() -> void:
	tutorial.enabled = false
	tutorial_enabled = false
	if _tutorial_panel != null:
		_tutorial_panel.visible = false
	_append_event("Tutorial skipped — use the build menu and inspector to continue.")


func get_tutorial_title_text() -> String:
	return _tutorial_title.text if _tutorial_title != null else ""


func get_tutorial_instruction_text() -> String:
	if _tutorial_instruction == null:
		return ""
	return _tutorial_instruction.get_parsed_text()


func get_mode_banner_title() -> String:
	return _mode_title.text if _mode_title != null else ""


func get_objective_text() -> String:
	if _objective == null:
		return ""
	return _objective.get_parsed_text()


func _exit_tutorial_to_menu() -> void:
	tutorial.skip_or_exit()
	get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn")


func _on_tutorial_step_changed(_idx: int, _step: Dictionary) -> void:
	_refresh_tutorial_ui()


func _on_tutorial_completed() -> void:
	if _tutorial_title != null:
		_tutorial_title.text = "Tutorial complete!"
	if _tutorial_instruction != null:
		_tutorial_instruction.text = "You finished the guided steps. Keep playing or return to the menu."


func _refresh_tutorial_ui() -> void:
	TutorialNavigationUI.refresh(
		tutorial,
		_tutorial_panel,
		_tutorial_title,
		_tutorial_progress,
		_tutorial_instruction,
		_tutorial_back,
		_tutorial_next,
		_tutorial_next_reason,
		_tutorial_skip,
		_tutorial_exit,
		_tutorial_step_kind
	)


func simulate_tutorial_next() -> void:
	if _tutorial_next != null and not _tutorial_next.disabled:
		_tutorial_next.pressed.emit()
	elif tutorial.enabled:
		tutorial.advance_manual()
		_refresh_tutorial_ui()


func simulate_tutorial_back() -> void:
	if _tutorial_back != null and not _tutorial_back.disabled:
		_tutorial_back.pressed.emit()
	elif tutorial.enabled:
		tutorial.retreat_manual()
		_refresh_tutorial_ui()


func _update_tick_timer() -> void:
	if _tick_timer == null or not _tick_timer.is_inside_tree():
		return
	var interval: float = TICK_BASE_SEC / maxf(controller.speed_multiplier, 0.25)
	_tick_timer.wait_time = interval
	if controller.paused:
		_tick_timer.stop()
	else:
		_tick_timer.start(interval)


func _on_tick_timer() -> void:
	if controller.paused:
		return
	controller.step_tick()


func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey and event.pressed and not event.echo):
		return
	if _handle_keyboard_shortcut(event as InputEventKey):
		get_viewport().set_input_as_handled()


func _handle_keyboard_shortcut(event: InputEventKey) -> bool:
	match event.keycode:
		KEY_ESCAPE:
			invoke_cancel_modes()
			return true
		KEY_SPACE:
			if controller.paused:
				invoke_play()
			else:
				invoke_pause()
			return true
		KEY_PERIOD, KEY_ENTER, KEY_KP_ENTER:
			invoke_step()
			return true
		KEY_F1:
			invoke_set_speed(1.0)
			return true
		KEY_F2:
			invoke_set_speed(2.0)
			return true
		KEY_F3:
			invoke_set_speed(4.0)
			return true
		KEY_R:
			invoke_road_mode()
			return true
		KEY_H:
			toggle_help_visible()
			return true
		KEY_EQUAL, KEY_KP_ADD:
			_map.zoom_by_factor(ZOOM_STEP)
			_sprite_layer.queue_redraw()
			return true
		KEY_MINUS, KEY_KP_SUBTRACT:
			_map.zoom_by_factor(1.0 / ZOOM_STEP)
			_sprite_layer.queue_redraw()
			return true
		KEY_W, KEY_UP:
			_map.pan_by_screen_delta(Vector2(0, PAN_STEP_PX))
			_sprite_layer.queue_redraw()
			return true
		KEY_S, KEY_DOWN:
			_map.pan_by_screen_delta(Vector2(0, -PAN_STEP_PX))
			_sprite_layer.queue_redraw()
			return true
		KEY_A, KEY_LEFT:
			_map.pan_by_screen_delta(Vector2(PAN_STEP_PX, 0))
			_sprite_layer.queue_redraw()
			return true
		KEY_D, KEY_RIGHT:
			_map.pan_by_screen_delta(Vector2(-PAN_STEP_PX, 0))
			_sprite_layer.queue_redraw()
			return true
	if BUILD_KEY_MAP.has(event.keycode):
		_set_build_mode(str(BUILD_KEY_MAP[event.keycode]))
		return true
	return false


func _on_snapshot_refreshed(snapshot: Dictionary) -> void:
	if tutorial.enabled:
		tutorial.update(snapshot, _map, _last_selection, controller)
	_refresh_all()


func _on_message_logged(text: String) -> void:
	_append_event(text)


func _on_victory() -> void:
	_victory.visible = true
	_victory.text = "Victory! Starter economy objective complete."


func _on_map_click(q: int, r: int) -> void:
	controller.handle_hex_click(q, r)
	_last_selection = controller.get_selection_at(q, r)
	_update_inspector(_last_selection)
	_refresh_map_overlays()
	_refresh_mode_ui()


func _on_map_hover(q: int, r: int) -> void:
	controller.handle_hex_hover(q, r)
	_refresh_map_overlays()
	var preview: Dictionary = controller.get_placement_preview()
	if preview.get("active", false):
		var reason: String = str(preview.get("reason", ""))
		if preview.get("valid", false):
			_preview.text = "Valid placement — click a green hex to confirm."
		else:
			_preview.text = reason if reason != "" else "Invalid placement — red hex."
	elif controller.road_mode_active:
		var road_prev: Dictionary = controller.get_road_preview()
		if road_prev.get("active", false):
			_preview.text = "Road route OK — click to confirm." if road_prev.get("valid", false) else str(road_prev.get("reason", "Invalid route"))
		elif controller.road_start_hex.x >= 0:
			_preview.text = "Road start selected — now click where the road should go."
		else:
			_preview.text = "Road mode — click a start point on your road network."
	else:
		_preview.text = ""


func _update_inspector(selection: Dictionary) -> void:
	if _inspector == null:
		return
	_inspector.text = SelectionFormatter.format(selection, controller.get_snapshot(), controller.get_mode_context())


func _refresh_all() -> void:
	_refresh_map()
	_refresh_panels()
	_refresh_mode_ui()


func _refresh_map() -> void:
	_map.set_render_data(controller.get_render_data())
	if not _map_fitted:
		_map_fitted = true
		_map.auto_fit_on_data_change = false
	_refresh_map_overlays()
	if _sprite_layer != null:
		_sprite_layer.set_snapshot(controller.get_snapshot())
		_sprite_layer.queue_redraw()


func _refresh_map_overlays() -> void:
	_map.set_placement_ghost(controller.get_placement_preview())
	var road_prev: Dictionary = controller.get_road_preview()
	var start: Vector2i = controller.road_start_hex
	_map.set_road_preview(
		road_prev.get("hexes", []),
		bool(road_prev.get("valid", false)),
		start.x,
		start.y,
	)


func _refresh_mode_ui() -> void:
	var banner: Dictionary = controller.get_mode_banner_text()
	if _mode_banner != null:
		_mode_banner.visible = str(banner.get("title", "")) != ""
		_mode_banner.custom_minimum_size = Vector2(0, 56) if _mode_banner.visible else Vector2(0, 0)
	if _mode_title != null:
		_mode_title.text = str(banner.get("title", ""))
	if _mode_instruction != null:
		_mode_instruction.text = str(banner.get("instruction", ""))
	if _build_palette != null:
		if controller.road_mode_active:
			_build_palette.set_road_active(true)
		elif controller.placement_building_type != "":
			_build_palette.set_active_build(controller.placement_building_type)
		else:
			_build_palette.clear_active()
	_refresh_mode_status()
	_refresh_mode_preview_text()


func _refresh_mode_status() -> void:
	if _mode_status == null:
		return
	if controller.road_mode_active:
		if controller.road_start_hex.x >= 0:
			_mode_status.text = "Mode: Road — pick destination"
		else:
			_mode_status.text = "Mode: Road — pick start"
	elif controller.placement_building_type != "":
		var label: String = SettlerMvpController.BUILD_LABELS.get(
			controller.placement_building_type,
			controller.placement_building_type,
		)
		_mode_status.text = "Mode: Placing %s" % label
	else:
		_mode_status.text = "Mode: Inspect / select"


func _refresh_mode_preview_text() -> void:
	if _preview == null:
		return
	if controller.placement_building_type != "":
		_preview.text = "Move over the map, then click a valid hex."
	elif controller.road_mode_active:
		if controller.road_start_hex.x >= 0:
			_preview.text = "Road mode: select destination"
		else:
			_preview.text = "Road mode: select start on your network"
	elif not controller.road_mode_active and controller.placement_building_type == "":
		_preview.text = ""


func _refresh_panels() -> void:
	_status.text = controller.build_status_text()
	_objective.text = controller.build_objective_text()
	_stock.text = controller.build_stockpile_text()
	_warnings.text = controller.build_warnings_text()
	_update_tick_timer()


func _append_event(text: String) -> void:
	_event_lines.insert(0, text)
	if _event_lines.size() > 40:
		_event_lines.resize(40)
	_event_log.text = "\n".join(_event_lines)
