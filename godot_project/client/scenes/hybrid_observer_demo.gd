extends Control

## MRP Phase C2 — hybrid observer (economy feeding military watch mode).

const SCREEN_SIZE := Vector2i(1920, 1080)
const AUTOPLAY_BASE_SEC: float = 0.35

var controller: HybridObserverController = HybridObserverController.new()

@onready var _map: MapViewport = %MapViewport
@onready var _sprite_layer: HybridSpriteLayer = %SpriteLayer
@onready var _status: Label = %StatusLabel
@onready var _summary: Label = %SummaryLabel
@onready var _chain: RichTextLabel = %ChainPanel
@onready var _production: RichTextLabel = %ProductionPanel
@onready var _orders: RichTextLabel = %OrdersPanel
@onready var _inspector: RichTextLabel = %Inspector
@onready var _event_log: RichTextLabel = %EventLog
@onready var _help: RichTextLabel = %HelpPanel
@onready var _title: Label = %TitleLabel
@onready var _top_status: Label = %TopStatusLabel
@onready var _recent_changes: RichTextLabel = %RecentChangesPanel
@onready var _capture_path: Label = %CapturePathLabel
@onready var _result: Label = %ResultLabel
@onready var _speed_label: Label = %SpeedLabel
@onready var _activity: Label = %ActivityLabel

var _autoplay_timer: Timer


func _ready() -> void:
	size = SCREEN_SIZE
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	_sprite_layer.setup(_map)
	_map.selection_clicked.connect(_on_map_selection)
	_map.show_grid = true
	_map.show_coord_labels = false
	_map.show_logistics = true
	_map.map_title = "Hybrid observer — logistics to military"
	_setup_labels()
	_setup_autoplay_timer()
	controller.reset_demo()
	_refresh_map()
	_update_panels()


func _setup_labels() -> void:
	_title.text = "Hybrid Observer — Economy Feeds Military"
	_top_status.text = "Watch roads, carriers, military construction, production, and delegated attack"
	_help.text = (
		"Observer demo: HQ logistics supply Barracks construction and unit training.\n"
		+ "Summary shows stockpile and power. Carriers deliver wares along roads.\n"
		+ "Step ticks to watch the full chain. Click map hexes for plain-English inspector text."
	)
	_capture_path.text = "Review captures: %s" % HybridObserverReviewUtil.screenshot_output_dir()


func _setup_autoplay_timer() -> void:
	_autoplay_timer = Timer.new()
	_autoplay_timer.one_shot = false
	add_child(_autoplay_timer)
	_autoplay_timer.timeout.connect(_on_autoplay_tick)
	_update_autoplay_timer()


func _update_autoplay_timer() -> void:
	var interval: float = AUTOPLAY_BASE_SEC / maxf(controller.speed_multiplier, 0.25)
	_autoplay_timer.wait_time = interval
	if controller.autoplay_enabled and not _autoplay_timer.is_stopped():
		_autoplay_timer.start(interval)


func _on_snapshot_refreshed(_snapshot: Dictionary) -> void:
	_refresh_map()
	_update_panels()


func _refresh_map() -> void:
	if _map == null or _sprite_layer == null:
		return
	_sprite_layer.use_sprites = controller.layer_visibility.get("sprites", true)
	_sprite_layer.set_snapshot(controller.snapshot)
	_map.set_render_data(controller.get_render_data())
	_sprite_layer.apply_military_states(controller.get_military_sprite_states())
	if controller.selected_q >= 0:
		_map.set_selection(controller.selected_q, controller.selected_r)


func _update_panels() -> void:
	_status.text = controller.build_transport_bar_text()
	_summary.text = controller.build_summary_text()
	_chain.text = controller.build_chain_text()
	_production.text = controller.build_production_text()
	_orders.text = controller.build_orders_text()
	_inspector.text = controller.build_inspector_text()
	_activity.text = controller.build_activity_hint()
	_recent_changes.text = controller.build_recent_changes_panel_text()
	_event_log.text = controller.build_event_log_text()
	_speed_label.text = "Speed cycle: %.1fx" % controller.speed_multiplier


func _on_map_selection(q: int, r: int) -> void:
	controller.set_selection(q, r)
	_map.set_selection(q, r)
	_inspector.text = controller.build_inspector_text()


func select_hex_for_tests(q: int, r: int) -> void:
	_on_map_selection(q, r)


func _on_reset_pressed() -> void:
	controller.reset_demo()
	_result.text = "Hybrid observer reset"


func _on_play_pause_pressed() -> void:
	controller.autoplay_enabled = not controller.autoplay_enabled
	if controller.autoplay_enabled:
		_autoplay_timer.start()
	else:
		_autoplay_timer.stop()
	_update_panels()
	_update_autoplay_timer()


func _on_step_tick_pressed() -> void:
	var before: int = controller.state.tick if controller.state != null else 0
	controller.step_tick()
	_result.text = "Tick %d -> %d" % [before, controller.state.tick if controller.state != null else 0]


func _on_step10_pressed() -> void:
	controller.step_ticks(10)
	_result.text = "Stepped 10 ticks"


func _on_step50_pressed() -> void:
	controller.step50()
	_result.text = "Stepped 50 ticks"


func _on_step_event_pressed() -> void:
	var result: Dictionary = controller.step_until_event()
	if bool(result.get("found", false)):
		_result.text = "Stopped on %s after %d steps" % [str(result.get("event", "")), int(result.get("steps", 0))]
	else:
		_result.text = "No event within cap"


func _on_speed_pressed() -> void:
	if controller.speed_multiplier < 1.5:
		controller.speed_multiplier = 2.0
	elif controller.speed_multiplier < 3.5:
		controller.speed_multiplier = 4.0
	else:
		controller.speed_multiplier = 1.0
	_update_autoplay_timer()
	_update_panels()


func _on_toggle_sprites_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("sprites", true)
	controller.set_layer_visible("sprites", vis)
	_refresh_map()


func _on_screenshot_pressed() -> void:
	var path: String = SimPaths.default_export_file("hybrid_observer_%d.png" % int(controller.snapshot.get("tick", 0)))
	var img: Image = get_viewport().get_texture().get_image()
	if img != null and img.save_png(path) == OK:
		_result.text = "Screenshot: %s" % path
	else:
		_result.text = "Screenshot failed"


func _on_autoplay_tick() -> void:
	if controller.autoplay_enabled:
		controller.autoplay_step()
