extends Control

## Mobile hybrid 4P FFA — human P1 vs three modular neural bots (PRD Part D).

const SCREEN_SIZE := Vector2i(390, 844)

var controller: HybridMobileFfaController = HybridMobileFfaController.new()
var difficulty: String = "normal"

var _map: MapViewport
var _status: Label
var _summary: RichTextLabel
var _victory: Label
var _tick_timer: Timer


func _ready() -> void:
	MobileLayoutProfiles.apply_to_control(self, "mobile_portrait_common")
	_build_ui()
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()
	controller.message_logged.connect(_on_message)
	controller.victory_achieved.connect(_on_victory)
	controller.reset_ffa(difficulty)
	_setup_timer()
	_refresh()


func _build_ui() -> void:
	_status = Label.new()
	_status.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_status.offset_bottom = 40
	add_child(_status)

	_map = MapViewport.new()
	_map.name = "MapViewport"
	_map.unique_name_in_owner = true
	_map.set_anchors_preset(Control.PRESET_FULL_RECT)
	_map.offset_top = 44
	_map.offset_bottom = -200
	_map.enable_camera_control = true
	add_child(_map)

	_summary = RichTextLabel.new()
	_summary.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_summary.offset_top = -196
	_summary.custom_minimum_size.y = 100
	add_child(_summary)

	_victory = Label.new()
	_victory.set_anchors_preset(Control.PRESET_CENTER)
	_victory.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(_victory)

	var bar := HBoxContainer.new()
	bar.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	bar.offset_top = -44
	for spec in [["PlayBtn", "Play"], ["PauseBtn", "Pause"], ["StepBtn", "Step"], ["MenuBtn", "Menu"]]:
		var btn := Button.new()
		btn.name = spec[0]
		btn.unique_name_in_owner = true
		btn.text = spec[1]
		btn.custom_minimum_size = Vector2(88, 48)
		bar.add_child(btn)
	add_child(bar)

	_connect("%PlayBtn", func(): controller.paused = false)
	_connect("%PauseBtn", func(): controller.paused = true)
	_connect("%StepBtn", func(): controller.step_tick(); _refresh())
	_connect("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))


func _connect(path: String, cb: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(cb)


func _setup_timer() -> void:
	_tick_timer = Timer.new()
	_tick_timer.wait_time = 0.3
	_tick_timer.autostart = true
	add_child(_tick_timer)
	_tick_timer.timeout.connect(func():
		if not controller.paused and not controller.match_over:
			controller.step_tick()
			_refresh()
	)


func _on_message(text: String) -> void:
	_refresh()


func _on_victory() -> void:
	_victory.text = "Victory!"
	_refresh()


func _refresh() -> void:
	if _map != null:
		_map.set_snapshot(controller.get_snapshot())
	if _status != null:
		_status.text = "Hybrid FFA | tick %d | %s" % [
			int(controller.get_snapshot().get("tick", 0)),
			"over" if controller.match_over else ("paused" if controller.paused else "playing"),
		]
	if _summary != null:
		var s: Dictionary = controller.get_match_summary()
		_summary.text = "Difficulty: %s\nAlive: %s\nAI intent samples: %d" % [
			s.get("difficulty", ""),
			str(s.get("alive", [])),
			controller.get_ai_telemetry().size(),
		]
	if controller.defeat_shown:
		_victory.text = "Defeat"
