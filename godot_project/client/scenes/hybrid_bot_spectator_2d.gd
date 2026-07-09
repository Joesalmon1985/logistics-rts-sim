extends Control

## 2D four-player hybrid bot spectator (PRD Part B).

const SCREEN_SIZE := Vector2i(1920, 1080)

var controller: HybridBotSpectatorController = HybridBotSpectatorController.new()

var _map: MapViewport
var _sprite_layer: HybridSpriteLayer
var _scoreboard: RichTextLabel
var _inspector: RichTextLabel
var _status: Label
var _autoplay_timer: Timer


func ensure_match_started() -> void:
	if controller.state == null:
		controller.reset_match()
	_refresh_all()


func _ready() -> void:
	size = SCREEN_SIZE
	if controller == null:
		controller = HybridBotSpectatorController.new()
	_build_ui()
	SimBootstrap.load_all()
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	_setup_autoplay()
	controller.reset_match()
	_refresh_all()


func _build_ui() -> void:
	_map = MapViewport.new()
	_map.name = "MapViewport"
	_map.unique_name_in_owner = true
	_map.set_anchors_preset(Control.PRESET_FULL_RECT)
	_map.offset_top = 48
	_map.offset_bottom = -180
	_map.enable_camera_control = true
	add_child(_map)

	_sprite_layer = HybridSpriteLayer.new()
	_sprite_layer.name = "SpriteLayer"
	_map.add_child(_sprite_layer)
	_sprite_layer.setup(_map)

	_status = Label.new()
	_status.name = "StatusLabel"
	_status.set_anchors_preset(Control.PRESET_TOP_WIDE)
	_status.offset_bottom = 44
	add_child(_status)

	_scoreboard = RichTextLabel.new()
	_scoreboard.name = "Scoreboard"
	_scoreboard.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	_scoreboard.offset_top = -176
	_scoreboard.custom_minimum_size.y = 80
	add_child(_scoreboard)

	_inspector = RichTextLabel.new()
	_inspector.name = "Inspector"
	_inspector.set_anchors_preset(Control.PRESET_RIGHT_WIDE)
	_inspector.offset_left = -320
	_inspector.offset_top = 48
	_inspector.offset_bottom = -180
	add_child(_inspector)

	var bar := HBoxContainer.new()
	bar.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	bar.offset_top = -44
	for spec in [
		["PlayPauseBtn", "Play/Pause"],
		["StepBtn", "Step"],
		["ResetBtn", "Reset"],
		["MenuBtn", "Menu"],
	]:
		var btn := Button.new()
		btn.name = spec[0]
		btn.unique_name_in_owner = true
		btn.text = spec[1]
		btn.custom_minimum_size = Vector2(120, 40)
		bar.add_child(btn)
	add_child(bar)

	_connect_btn("%PlayPauseBtn", _on_play_pause)
	_connect_btn("%StepBtn", func(): controller.step_tick(); _refresh_all())
	_connect_btn("%ResetBtn", func(): controller.reset_match(); _refresh_all())
	_connect_btn("%MenuBtn", func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	_map.selection_clicked.connect(func(q, r): controller.select_hex(q, r); _refresh_all())


func _connect_btn(path: String, cb: Callable) -> void:
	var btn: Button = get_node_or_null(path) as Button
	if btn != null:
		btn.pressed.connect(cb)


func _setup_autoplay() -> void:
	_autoplay_timer = Timer.new()
	_autoplay_timer.wait_time = 0.35
	_autoplay_timer.autostart = true
	add_child(_autoplay_timer)
	_autoplay_timer.timeout.connect(_on_autoplay)


func _on_autoplay() -> void:
	if controller.autoplay_enabled and not controller.paused:
		controller.step_tick()
		_refresh_all()


func _on_play_pause() -> void:
	controller.paused = not controller.paused


func _on_snapshot_refreshed(_snap: Dictionary) -> void:
	_refresh_all()


func _refresh_all() -> void:
	if _map != null:
		var render: Dictionary = LayerRenderer.build_render_data(controller.snapshot)
		_map.set_render_data(render)
	if _sprite_layer != null:
		_sprite_layer.set_snapshot(controller.snapshot)
	if _status != null:
		_status.text = "Hybrid 2D Spectator | tick %d | %s" % [
			int(controller.snapshot.get("tick", 0)),
			"paused" if controller.paused else "playing",
		]
	if _scoreboard != null:
		_scoreboard.text = "\n".join(controller.get_scoreboard_lines())
	if _inspector != null:
		_inspector.text = controller.get_inspector_text()
