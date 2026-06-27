class_name HybridMvpGame
extends Control

## Hybrid MVP — human-playable logistics RTS with delegated military control.

const SCREEN_SIZE := Vector2i(1920, 1080)
const LAUNCH_MIN_SIZE := Vector2i(1280, 720)
const TICK_BASE_SEC: float = 0.25

@export var tutorial_enabled: bool = false
static var pending_tutorial: bool = false

var controller: HybridMvpController = HybridMvpController.new()
var tutorial: HybridTutorialController = HybridTutorialController.new()

var _map: MapViewport
var _status: Label
var _objective: RichTextLabel
var _supply: Label
var _production: RichTextLabel
var _orders: RichTextLabel
var _warnings: RichTextLabel
var _inspector: RichTextLabel
var _help: RichTextLabel
var _victory: Label
var _play_btn: Button
var _mode_banner: PanelContainer
var _mode_title: Label
var _mode_instruction: Label
var _tutorial_panel: PanelContainer
var _tutorial_title: Label
var _tutorial_progress: Label
var _tutorial_instruction: RichTextLabel
var _tutorial_next: Button
var _tutorial_skip: Button
var _tutorial_exit: Button
var _build_palette: VBoxContainer

var _sprite_layer: HybridSpriteLayer
var _tick_timer: Timer
var _initialized: bool = false


func _bind_ui_nodes() -> void:
	_map = find_child("MapViewport", true, false) as MapViewport
	_status = find_child("StatusLabel", true, false) as Label
	_objective = find_child("ObjectivePanel", true, false) as RichTextLabel
	_supply = find_child("SupplyLabel", true, false) as Label
	_production = find_child("ProductionPanel", true, false) as RichTextLabel
	_orders = find_child("OrdersPanel", true, false) as RichTextLabel
	_warnings = find_child("WarningsPanel", true, false) as RichTextLabel
	_inspector = find_child("Inspector", true, false) as RichTextLabel
	_help = find_child("HelpPanel", true, false) as RichTextLabel
	_victory = find_child("VictoryLabel", true, false) as Label
	_play_btn = find_child("PlayBtn", true, false) as Button
	_mode_banner = find_child("ModeBanner", true, false) as PanelContainer
	_mode_title = find_child("ModeTitle", true, false) as Label
	_mode_instruction = find_child("ModeInstruction", true, false) as Label
	_tutorial_panel = find_child("TutorialPanel", true, false) as PanelContainer
	_tutorial_title = find_child("TutorialTitle", true, false) as Label
	_tutorial_progress = find_child("TutorialProgress", true, false) as Label
	_tutorial_instruction = find_child("TutorialInstruction", true, false) as RichTextLabel
	_tutorial_next = find_child("TutorialNextBtn", true, false) as Button
	_tutorial_skip = find_child("TutorialSkipBtn", true, false) as Button
	_tutorial_exit = find_child("TutorialExitBtn", true, false) as Button
	_build_palette = find_child("BuildPalette", true, false) as VBoxContainer
	_sprite_layer = find_child("HybridSpriteLayer", true, false) as HybridSpriteLayer


func enable_tutorial_for_tests() -> void:
	if not _initialized:
		_setup_scene()
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


func is_tutorial_enabled() -> bool:
	return tutorial_enabled and tutorial.enabled and not tutorial.exited


func get_tutorial_title_text() -> String:
	return _tutorial_title.text if _tutorial_title != null else ""


func get_tutorial_instruction_text() -> String:
	return _tutorial_instruction.get_parsed_text() if _tutorial_instruction != null else ""


func get_objective_text() -> String:
	return _objective.get_parsed_text() if _objective != null else ""


func invoke_build_mode(building_type: String) -> void:
	controller.set_build_mode(building_type)
	_refresh_mode_ui()


func invoke_attack_mode() -> void:
	controller.set_attack_mode(true)
	_refresh_mode_ui()


func invoke_cancel_modes() -> void:
	controller.clear_modes()
	_refresh_mode_ui()


func _ready() -> void:
	if _initialized:
		return
	custom_minimum_size = Vector2(LAUNCH_MIN_SIZE)
	_setup_scene()


func _setup_scene() -> void:
	_bind_ui_nodes()
	if _map == null:
		push_error("HybridMvpGame: MapViewport not found")
		return
	if pending_tutorial:
		tutorial_enabled = true
		pending_tutorial = false
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.message_logged.connect(_on_message_logged)
	controller.victory_achieved.connect(_on_victory)
	_map.selection_clicked.connect(_on_map_click)
	_map.enable_hover = true
	_map.show_logistics = true
	_map.show_forest_density = true
	_map.enable_camera_control = true
	if _sprite_layer == null:
		_sprite_layer = HybridSpriteLayer.new()
		_sprite_layer.name = "HybridSpriteLayer"
		_map.add_child(_sprite_layer)
	_sprite_layer.setup(_map)
	_setup_tick_timer()
	_wire_buttons()
	_help.text = controller.build_help_text()
	tutorial.enabled = tutorial_enabled
	if _tutorial_panel != null:
		_tutorial_panel.visible = tutorial_enabled
	controller.reset_game()
	if tutorial_enabled:
		tutorial.start(controller.get_snapshot(), _map)
	_refresh_all()
	_initialized = true


func _setup_tick_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.one_shot = false
	add_child(_tick_timer)
	_tick_timer.timeout.connect(_on_tick_timer)


func _wire_buttons() -> void:
	_connect_btn("%PlayBtn", _toggle_play)
	_connect_btn("%PauseBtn", func(): controller.paused = true)
	_connect_btn("%StepBtn", func(): controller.step_tick())
	_connect_btn("%Speed1Btn", func(): controller.speed_multiplier = 1.0)
	_connect_btn("%Speed2Btn", func(): controller.speed_multiplier = 2.0)
	_connect_btn("%Speed4Btn", func(): controller.speed_multiplier = 4.0)
	_connect_btn("%RoadBtn", func(): controller.set_road_mode(true); _refresh_mode_ui())
	_connect_btn("%AttackBtn", func(): controller.set_attack_mode(true); _refresh_mode_ui())
	_connect_btn("%QueueBtn", func(): controller.queue_rifle_squad_at_selection())
	_connect_btn("%CancelModeBtn", func(): controller.clear_modes(); _refresh_mode_ui())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	_connect_btn("%TutorialNextBtn", func(): tutorial.advance_manual(); _refresh_tutorial_ui())
	_connect_btn("%TutorialSkipBtn", func(): tutorial.skip_or_exit(); _refresh_tutorial_ui())
	_connect_btn("%TutorialExitBtn", func(): tutorial.skip_or_exit(); _refresh_tutorial_ui())
	for btype in HybridMvpController.ECONOMY_BUILD_TYPES + HybridMvpController.MILITARY_BUILD_TYPES:
		var btn: Button = _build_palette.get_node_or_null("Build_%s" % btype) as Button
		if btn != null:
			btn.pressed.connect(func(): controller.set_build_mode(btype); _refresh_mode_ui())


func _connect_btn(path: String, cb: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(cb)


func _toggle_play() -> void:
	controller.paused = not controller.paused
	if not controller.paused:
		_tick_timer.start(TICK_BASE_SEC / controller.speed_multiplier)
	else:
		_tick_timer.stop()
	_refresh_status()


func _on_tick_timer() -> void:
	if controller.paused:
		return
	controller.step_tick()
	_tick_timer.wait_time = TICK_BASE_SEC / maxf(controller.speed_multiplier, 0.25)


func _on_map_click(q: int, r: int) -> void:
	controller.handle_hex_click(q, r)
	_map.set_selection(q, r)
	_refresh_inspector()
	tutorial.update(controller.get_snapshot(), _map, controller.get_selection_at(q, r), controller)


func _on_snapshot_refreshed(_snap: Dictionary) -> void:
	_refresh_all()


func _on_message_logged(_text: String) -> void:
	pass


func _on_victory() -> void:
	if _victory != null:
		_victory.visible = true
		_victory.text = "Victory!"


func _refresh_all() -> void:
	if _map == null:
		return
	_refresh_map()
	_refresh_status()
	if _objective != null:
		_objective.text = controller.build_objective_text()
	if _supply != null:
		_supply.text = controller.build_supply_text()
	if _production != null:
		_production.text = controller.build_production_text()
	if _orders != null:
		_orders.text = controller.build_orders_text()
	if _warnings != null:
		_warnings.text = controller.build_warnings_text()
	if _inspector != null:
		_inspector.text = controller.build_inspector_text()
	_refresh_mode_ui()
	_refresh_tutorial_ui()


func _refresh_map() -> void:
	_map.set_render_data(controller.get_render_data())
	_sprite_layer.set_snapshot(controller.get_snapshot())
	_sprite_layer.apply_military_states(controller.get_military_sprite_states())


func _refresh_status() -> void:
	_status.text = controller.build_status_text()
	if _play_btn != null:
		_play_btn.text = "Play" if controller.paused else "Pause"


func _refresh_inspector() -> void:
	_inspector.text = controller.build_inspector_text()


func _refresh_mode_ui() -> void:
	var banner: Dictionary = controller.get_mode_banner_text()
	if _mode_title != null:
		_mode_title.text = str(banner.get("title", ""))
	if _mode_instruction != null:
		_mode_instruction.text = str(banner.get("instruction", ""))
	if _mode_banner != null:
		_mode_banner.visible = str(banner.get("title", "")) != ""


func _refresh_tutorial_ui() -> void:
	if not tutorial.enabled or tutorial.exited:
		if _tutorial_panel != null:
			_tutorial_panel.visible = false
		return
	var step: Dictionary = tutorial.current_step()
	if _tutorial_panel != null:
		_tutorial_panel.visible = not step.is_empty()
	if step.is_empty():
		return
	_tutorial_title.text = str(step.get("title", ""))
	_tutorial_instruction.text = str(step.get("instruction", ""))
	_tutorial_progress.text = "Step %d / %d" % [tutorial.step_index + 1, tutorial.steps.size()]
