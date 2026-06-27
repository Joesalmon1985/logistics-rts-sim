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
var _tutorial_back: Button
var _tutorial_skip: Button
var _tutorial_exit: Button
var _tutorial_next_reason: Label
var _tutorial_step_kind: Label
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
	_tutorial_back = find_child("TutorialBackBtn", true, false) as Button
	_tutorial_skip = find_child("TutorialSkipBtn", true, false) as Button
	_tutorial_exit = find_child("TutorialExitBtn", true, false) as Button
	_tutorial_next_reason = find_child("TutorialNextReasonLabel", true, false) as Label
	_tutorial_step_kind = find_child("TutorialStepKind", true, false) as Label
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
	_update_tick_timer()
	_initialized = true


func _setup_tick_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.one_shot = false
	add_child(_tick_timer)
	_tick_timer.timeout.connect(_on_tick_timer)


func _wire_buttons() -> void:
	_connect_btn("%PlayBtn", invoke_play)
	_connect_btn("%PauseBtn", invoke_pause)
	_connect_btn("%StepBtn", invoke_step)
	_connect_btn("%Speed1Btn", func(): invoke_set_speed(1.0))
	_connect_btn("%Speed2Btn", func(): invoke_set_speed(2.0))
	_connect_btn("%Speed4Btn", func(): invoke_set_speed(4.0))
	_connect_btn("%RoadBtn", func(): controller.set_road_mode(true); _refresh_mode_ui())
	_connect_btn("%AttackBtn", func(): controller.set_attack_mode(true); _refresh_mode_ui())
	_connect_btn("%QueueBtn", func(): controller.queue_rifle_squad_at_selection())
	_connect_btn("%CancelModeBtn", func(): controller.clear_modes(); _refresh_mode_ui())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	_connect_btn("%TutorialNextBtn", func(): tutorial.advance_manual(); _refresh_tutorial_ui())
	_connect_btn("%TutorialBackBtn", func(): tutorial.retreat_manual(); _refresh_tutorial_ui())
	_connect_btn("%TutorialSkipBtn", func(): _skip_tutorial())
	_connect_btn("%TutorialExitBtn", func(): _exit_tutorial_to_menu())
	for btype in HybridMvpController.ECONOMY_BUILD_TYPES + HybridMvpController.MILITARY_BUILD_TYPES:
		var btn: Button = _build_palette.get_node_or_null("Build_%s" % btype) as Button
		if btn != null:
			btn.pressed.connect(func(): controller.set_build_mode(btype); _refresh_mode_ui())


func _connect_btn(path: String, cb: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(cb)


func invoke_play() -> void:
	controller.paused = false
	_update_tick_timer()
	_refresh_status()


func invoke_pause() -> void:
	controller.paused = true
	_update_tick_timer()
	_refresh_status()


func invoke_step() -> void:
	if not controller.paused:
		return
	controller.step_tick()
	_refresh_all()


func invoke_set_speed(multiplier: float) -> void:
	controller.speed_multiplier = multiplier
	_update_tick_timer()
	_refresh_status()


func _update_tick_timer() -> void:
	if _tick_timer == null:
		return
	var interval: float = TICK_BASE_SEC / maxf(controller.speed_multiplier, 0.25)
	_tick_timer.wait_time = interval
	if controller.paused:
		_tick_timer.stop()
	elif _tick_timer.is_inside_tree():
		_tick_timer.start(interval)


func _skip_tutorial() -> void:
	tutorial.enabled = false
	tutorial_enabled = false
	if _tutorial_panel != null:
		_tutorial_panel.visible = false


func _exit_tutorial_to_menu() -> void:
	tutorial.skip_or_exit()
	get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn")


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


func invoke_play_toggle() -> void:
	if controller.paused:
		invoke_play()
	else:
		invoke_pause()


func get_current_tick() -> int:
	return int(controller.get_snapshot().get("tick", 0))


func is_sim_paused() -> bool:
	return controller.paused


func get_tutorial_step_index() -> int:
	return tutorial.step_index


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


func _on_snapshot_refreshed(snap: Dictionary) -> void:
	if tutorial.enabled:
		tutorial.update(snap, _map, controller.get_selection_at(_map.selected_q, _map.selected_r), controller)
	_refresh_all()


func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey and event.pressed and not event.echo):
		return
	if not (event is InputEventKey):
		return
	match (event as InputEventKey).keycode:
		KEY_SPACE:
			invoke_play_toggle()
			get_viewport().set_input_as_handled()
		KEY_PERIOD, KEY_ENTER, KEY_KP_ENTER:
			invoke_step()
			get_viewport().set_input_as_handled()
		KEY_F1:
			invoke_set_speed(1.0)
			get_viewport().set_input_as_handled()
		KEY_F2:
			invoke_set_speed(2.0)
			get_viewport().set_input_as_handled()
		KEY_F3:
			invoke_set_speed(4.0)
			get_viewport().set_input_as_handled()


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
	_update_tick_timer()


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
