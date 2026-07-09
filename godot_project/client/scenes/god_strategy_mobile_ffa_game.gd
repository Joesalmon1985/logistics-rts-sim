extends Control

## God-Strategy mobile 4P FFA — touch-first human vs three neural god opponents.

const SCREEN_SIZE := Vector2i(390, 844)

var controller: GodStrategyMobileFfaController = GodStrategyMobileFfaController.new()
var difficulty: String = "normal"

var _map: MapViewport
var _status: Label
var _summary: RichTextLabel
var _victory: Label
var _god_hud: Label
var _tick_timer: Timer


func _ready() -> void:
	MobileLayoutProfiles.apply_to_control(self, "mobile_portrait_common")
	_build_ui()
	if not Engine.is_editor_hint() and not _test_mode:
		_boot_match()


func initialize_for_tests() -> void:
	_test_mode = true
	if _status == null:
		_build_ui()
	_boot_match()


var _test_mode: bool = false


func _boot_match() -> void:
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()
	controller.message_logged.connect(_on_message)
	controller.victory_achieved.connect(_on_victory)
	controller.reset_ffa(difficulty)
	if not _test_mode:
		_setup_timer()
	_refresh()


func _build_ui() -> void:
	_status = Label.new()
	_status.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_status.offset_bottom = 40
	add_child(_status)

	_god_hud = Label.new()
	_god_hud.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_god_hud.offset_top = 36
	_god_hud.offset_bottom = 72
	add_child(_god_hud)

	_map = MapViewport.new()
	_map.name = "MapViewport"
	_map.unique_name_in_owner = true
	_map.set_anchors_preset(Control.PRESET_FULL_RECT)
	_map.offset_top = 76
	_map.offset_bottom = -220
	_map.enable_camera_control = true
	add_child(_map)

	_summary = RichTextLabel.new()
	_summary.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_summary.offset_top = -216
	_summary.custom_minimum_size.y = 120
	add_child(_summary)

	_victory = Label.new()
	_victory.set_anchors_preset(Control.PRESET_CENTER)
	_victory.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(_victory)

	var bar := HBoxContainer.new()
	bar.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	bar.offset_top = -48
	for spec in [["PlayBtn", "Play"], ["PauseBtn", "Pause"], ["StepBtn", "Step"], ["AttackBtn", "Attack"], ["MenuBtn", "Menu"]]:
		var btn := Button.new()
		btn.name = spec[0]
		btn.unique_name_in_owner = true
		btn.text = spec[1]
		btn.custom_minimum_size = Vector2(72, 48)
		bar.add_child(btn)
	add_child(bar)

	_connect("%PlayBtn", func(): controller.paused = false)
	_connect("%PauseBtn", func(): controller.paused = true)
	_connect("%StepBtn", func(): controller.step_tick(); _refresh())
	_connect("%AttackBtn", func(): controller.attack_mode_active = not controller.attack_mode_active)
	_connect("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))


func _connect(path: String, cb: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(cb)


func _setup_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.wait_time = 0.35
	_tick_timer.timeout.connect(_on_tick)
	add_child(_tick_timer)
	_tick_timer.start()


func _on_tick() -> void:
	if not controller.paused and not controller.match_over:
		controller.step_tick()
	_refresh()


func _refresh() -> void:
	if _map != null and controller.viewer.snapshot != null:
		_map.set_render_data(controller.get_render_data())
	var summary: Dictionary = controller.get_match_summary()
	_status.text = "God-Strategy FFA | tick %d | %s" % [int(summary.get("tick", 0)), str(summary.get("end_reason", "in_progress"))]
	var overlay: Dictionary = controller.get_god_strategy_overlay()
	var needs_bucket: Dictionary = overlay.get("worker_needs", {})
	var needs: Dictionary = needs_bucket.get("player_averages", {}).get("p1", {})
	var pollution: Dictionary = overlay.get("pollution", {})
	_god_hud.text = "Age %s | welfare h=%s | pollution %s" % [
		str(overlay.get("match", {}).get("current_age_id", "prehistoric")),
		str(needs.get("happiness", "?")),
		str(pollution.get("global_pollution", 0)),
	]
	_summary.text = "[b]Neural opponents[/b]: learned modular god (no fallback)\n%s" % JSON.stringify(summary)
	if controller.victory_shown:
		_victory.text = "Victory!"
	elif controller.defeat_shown:
		_victory.text = "Defeat"


func _on_message(text: String) -> void:
	_summary.text = text + "\n" + _summary.text


func _on_victory() -> void:
	_victory.text = "Victory!"
