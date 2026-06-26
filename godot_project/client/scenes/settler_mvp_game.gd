extends Control

## Settler MVP — human-playable settlement logistics mode.

const SCREEN_SIZE := Vector2i(1920, 1080)
const TICK_BASE_SEC: float = 0.25
const PAN_STEP_PX: float = 48.0
const ZOOM_STEP: float = 1.12

const BUILD_KEY_MAP: Dictionary = {
	KEY_1: "woodcutter",
	KEY_2: "forester",
	KEY_3: "sawmill",
	KEY_4: "quarry",
}

var controller: SettlerMvpController = SettlerMvpController.new()

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

var _tick_timer: Timer
var _event_lines: PackedStringArray = PackedStringArray()
var _map_fitted: bool = false
var _help_visible: bool = true
var _initialized: bool = false


func initialize_for_tests() -> void:
	if _initialized:
		return
	_bind_ui_nodes()
	SimBootstrap.load_all()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.message_logged.connect(_on_message_logged)
	controller.victory_achieved.connect(_on_victory)
	_map.selection_clicked.connect(_on_map_click)
	_map.hover_changed.connect(_on_map_hover)
	_map.enable_camera_control = true
	_map.enable_hover = true
	_map.auto_fit_on_data_change = not _map_fitted
	_map.show_grid = false
	_map.show_coord_labels = false
	_map.show_logistics = true
	_map.show_forest_density = true
	_map.show_internal_title_bar = false
	_map.mouse_filter = Control.MOUSE_FILTER_STOP
	_configure_input_filters()
	_setup_tick_timer()
	_help.text = controller.build_help_text()
	_wire_build_buttons()
	_start_game()
	_initialized = true


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


func _ready() -> void:
	if _initialized:
		return
	custom_minimum_size = Vector2(1280, 720)
	if not (get_parent() is SubViewport):
		set_deferred("size", SCREEN_SIZE)
	_bind_ui_nodes()
	SimBootstrap.load_all()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.message_logged.connect(_on_message_logged)
	controller.victory_achieved.connect(_on_victory)
	_map.selection_clicked.connect(_on_map_click)
	_map.hover_changed.connect(_on_map_hover)
	_map.enable_camera_control = true
	_map.enable_hover = true
	_map.auto_fit_on_data_change = not _map_fitted
	_map.show_grid = false
	_map.show_coord_labels = false
	_map.show_logistics = true
	_map.show_forest_density = true
	_map.show_internal_title_bar = false
	_map.mouse_filter = Control.MOUSE_FILTER_STOP
	_configure_input_filters()
	_setup_tick_timer()
	_help.text = controller.build_help_text()
	_wire_build_buttons()
	_start_game()
	_initialized = true
	call_deferred("_focus_play_on_start")


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
	var title: Label = get_node_or_null("RootVBox/ContentHBox/RightPanel/TitleLabel") as Label
	_set_decorative_control(title)
	var build_header: Label = get_node_or_null("RootVBox/ContentHBox/RightPanel/BuildHeader") as Label
	_set_decorative_control(build_header)
	if _map_area != null:
		_map_area.mouse_filter = Control.MOUSE_FILTER_PASS


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
	if _play_btn != null:
		_play_btn.grab_focus()


func _setup_tick_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.one_shot = false
	add_child(_tick_timer)
	_tick_timer.timeout.connect(_on_tick_timer)
	_update_tick_timer()


func _wire_build_buttons() -> void:
	_connect_btn("%RoadBtn", func(): controller.set_road_mode(true))
	_connect_btn("%WoodcutterBtn", func(): controller.set_build_mode("woodcutter"))
	_connect_btn("%ForesterBtn", func(): controller.set_build_mode("forester"))
	_connect_btn("%SawmillBtn", func(): controller.set_build_mode("sawmill"))
	_connect_btn("%QuarryBtn", func(): controller.set_build_mode("quarry"))
	_connect_btn("%CancelBtn", func(): controller.clear_modes(); _preview.text = "")
	_connect_btn("%PauseBtn", invoke_pause)
	_connect_btn("%PlayBtn", invoke_play)
	_connect_btn("%Speed1Btn", func(): invoke_set_speed(1.0))
	_connect_btn("%Speed2Btn", func(): invoke_set_speed(2.0))
	_connect_btn("%Speed4Btn", func(): invoke_set_speed(4.0))
	_connect_btn("%StepBtn", invoke_step)
	_connect_btn("%NewGameBtn", func(): _start_game())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))


func _connect_btn(node_path: String, callback: Callable) -> void:
	var btn: Button = get_node_or_null(node_path)
	if btn != null:
		btn.pressed.connect(callback)


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
	controller.set_build_mode(building_type)


func invoke_road_mode() -> void:
	controller.set_road_mode(true)


func invoke_cancel_modes() -> void:
	controller.clear_modes()
	_preview.text = ""
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
	return _inspector.text if _inspector != null else ""


func get_selected_hex() -> Vector2i:
	return Vector2i(_map.selected_q, _map.selected_r)


func is_sim_paused() -> bool:
	return controller.paused


func get_current_tick() -> int:
	return controller.get_current_tick()


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
	_refresh_all()
	call_deferred("_focus_play_on_start")


func _update_tick_timer() -> void:
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
			return true
		KEY_MINUS, KEY_KP_SUBTRACT:
			_map.zoom_by_factor(1.0 / ZOOM_STEP)
			return true
		KEY_W, KEY_UP:
			_map.pan_by_screen_delta(Vector2(0, PAN_STEP_PX))
			return true
		KEY_S, KEY_DOWN:
			_map.pan_by_screen_delta(Vector2(0, -PAN_STEP_PX))
			return true
		KEY_A, KEY_LEFT:
			_map.pan_by_screen_delta(Vector2(PAN_STEP_PX, 0))
			return true
		KEY_D, KEY_RIGHT:
			_map.pan_by_screen_delta(Vector2(-PAN_STEP_PX, 0))
			return true
	if BUILD_KEY_MAP.has(event.keycode):
		invoke_build_mode(str(BUILD_KEY_MAP[event.keycode]))
		return true
	return false


func _on_snapshot_refreshed(_snapshot: Dictionary) -> void:
	_refresh_all()


func _on_message_logged(text: String) -> void:
	_append_event(text)


func _on_victory() -> void:
	_victory.visible = true
	_victory.text = "Victory! Starter economy objective complete."


func _on_map_click(q: int, r: int) -> void:
	controller.handle_hex_click(q, r)
	var selection: Dictionary = controller.get_selection_at(q, r)
	_inspector.text = SelectionFormatter.format(selection, controller.get_snapshot())
	_refresh_map_overlays()


func _on_map_hover(q: int, r: int) -> void:
	controller.handle_hex_hover(q, r)
	_refresh_map_overlays()
	var preview: Dictionary = controller.get_placement_preview()
	if preview.get("active", false):
		var reason: String = str(preview.get("reason", ""))
		if preview.get("valid", false):
			_preview.text = "Valid placement"
		else:
			_preview.text = reason if reason != "" else "Invalid placement"
	elif controller.road_mode_active:
		var road_prev: Dictionary = controller.get_road_preview()
		if road_prev.get("active", false):
			_preview.text = "Road route OK" if road_prev.get("valid", false) else str(road_prev.get("reason", "Invalid route"))
		else:
			_preview.text = "Road mode: select destination"
	else:
		_preview.text = ""


func _refresh_all() -> void:
	_refresh_map()
	_refresh_panels()


func _refresh_map() -> void:
	_map.set_render_data(controller.get_render_data())
	if not _map_fitted:
		_map_fitted = true
		_map.auto_fit_on_data_change = false
	_refresh_map_overlays()


func _refresh_map_overlays() -> void:
	_map.set_placement_ghost(controller.get_placement_preview())
	var road_prev: Dictionary = controller.get_road_preview()
	_map.set_road_preview(
		road_prev.get("hexes", []),
		bool(road_prev.get("valid", false)),
	)


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
