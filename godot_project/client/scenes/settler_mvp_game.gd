extends Control

## Settler MVP — human-playable settlement logistics mode.

const SCREEN_SIZE := Vector2i(1920, 1080)
const TICK_BASE_SEC: float = 0.25

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

var _tick_timer: Timer
var _event_lines: PackedStringArray = PackedStringArray()
var _map_fitted: bool = false


func _ready() -> void:
	set_deferred("size", SCREEN_SIZE)
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
	_map.map_title = "Settler MVP"
	_setup_tick_timer()
	_help.text = controller.build_help_text()
	_wire_build_buttons()
	_start_game()


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
	_connect_btn("%PauseBtn", func(): controller.set_paused(true); _update_tick_timer())
	_connect_btn("%PlayBtn", func(): controller.set_paused(false); _update_tick_timer())
	_connect_btn("%Speed1Btn", func(): controller.set_speed(1.0); _update_tick_timer())
	_connect_btn("%Speed2Btn", func(): controller.set_speed(2.0); _update_tick_timer())
	_connect_btn("%Speed4Btn", func(): controller.set_speed(4.0); _update_tick_timer())
	_connect_btn("%StepBtn", func(): controller.step_tick(); _refresh_panels())
	_connect_btn("%NewGameBtn", func(): _start_game())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))


func _connect_btn(node_path: String, callback: Callable) -> void:
	var btn: Button = get_node_or_null(node_path)
	if btn != null:
		btn.pressed.connect(callback)


func _start_game() -> void:
	controller.reset_game()
	_event_lines = PackedStringArray()
	_victory.visible = false
	_map_fitted = false
	_map.auto_fit_on_data_change = true
	_refresh_all()


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
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		controller.clear_modes()
		_preview.text = ""
		_refresh_map_overlays()


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
