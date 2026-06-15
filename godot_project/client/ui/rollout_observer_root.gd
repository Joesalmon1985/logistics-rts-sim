class_name RolloutObserverRoot
extends Control

const SCREEN_SIZE := Vector2i(1920, 1080)

var controller: RolloutController
var _play_timer: Timer
var _play_speed: float = 1.0

var _map: MapViewport
var _status: Label
var _action: RichTextLabel
var _metrics: Label
var _event_log: RichTextLabel
var _legend: RichTextLabel
var _inspector: RichTextLabel
var _debug: Label
var _debug_json: RichTextLabel
var _debug_panel: PanelContainer


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	custom_minimum_size = SCREEN_SIZE
	size = SCREEN_SIZE
	controller = RolloutController.new()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	_build_ui()
	_play_timer = Timer.new()
	_play_timer.one_shot = false
	_play_timer.timeout.connect(_on_play_tick)
	add_child(_play_timer)


func load_rollout_path(path: String) -> Dictionary:
	var result: Dictionary = controller.load_rollout(path)
	if result.get("ok", false):
		_refresh_all()
		call_deferred("_deferred_map_refresh")
	return result


func _deferred_map_refresh() -> void:
	await get_tree().process_frame
	await get_tree().process_frame
	if _map != null:
		_map.fit_to_content()
		_map.queue_redraw()


func _build_ui() -> void:
	var root := HBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 8)
	add_child(root)

	var map_panel := PanelContainer.new()
	map_panel.custom_minimum_size = Vector2(1280, 1080)
	map_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_child(map_panel)
	_map = MapViewport.new()
	_map.name = "MapViewport"
	_map.custom_minimum_size = Vector2(640, 480)
	_map.show_grid = true
	_map.show_coord_labels = false
	_map.map_title = "Rollout map"
	_map.selection_clicked.connect(_on_map_selection)
	map_panel.add_child(_map)

	var side := VBoxContainer.new()
	side.custom_minimum_size = Vector2(620, 1080)
	side.size_flags_horizontal = Control.SIZE_FILL
	root.add_child(side)

	_status = _make_label("Status")
	side.add_child(_status)

	side.add_child(_make_timeline_row())
	side.add_child(_make_mode_row())

	_action = RichTextLabel.new()
	_action.custom_minimum_size = Vector2(0, 100)
	_action.scroll_active = true
	_action.text = "Action panel"
	side.add_child(_action)

	_metrics = _make_label("Metrics")
	side.add_child(_metrics)

	_event_log = RichTextLabel.new()
	_event_log.custom_minimum_size = Vector2(0, 120)
	_event_log.scroll_active = true
	side.add_child(_event_log)

	_legend = RichTextLabel.new()
	_legend.custom_minimum_size = Vector2(0, 100)
	_legend.text = "[b]Legend[/b] — see mode"
	side.add_child(_legend)

	_inspector = RichTextLabel.new()
	_inspector.custom_minimum_size = Vector2(0, 120)
	_inspector.scroll_active = true
	_inspector.text = "Click map to inspect."
	side.add_child(_inspector)

	_debug = _make_label("Debug")
	side.add_child(_debug)

	_debug_panel = PanelContainer.new()
	_debug_panel.visible = false
	_debug_json = RichTextLabel.new()
	_debug_json.custom_minimum_size = Vector2(0, 160)
	_debug_json.scroll_active = true
	_debug_panel.add_child(_debug_json)
	side.add_child(_debug_panel)


func _make_label(text: String) -> Label:
	var label := Label.new()
	label.text = text
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return label


func _make_timeline_row() -> HBoxContainer:
	var row := HBoxContainer.new()
	for spec in [
		["|<", Callable(self, "_on_jump_start")],
		["<", Callable(self, "_on_step_back")],
		["Play", Callable(self, "_on_play_pause")],
		[">", Callable(self, "_on_step_forward")],
		[">|", Callable(self, "_on_jump_end")],
	]:
		var btn := Button.new()
		btn.text = spec[0]
		btn.pressed.connect(spec[1])
		row.add_child(btn)
	var speed := HSlider.new()
	speed.min_value = 0.25
	speed.max_value = 4.0
	speed.step = 0.25
	speed.value = 1.0
	speed.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	speed.value_changed.connect(func(v: float) -> void: _play_speed = v)
	row.add_child(speed)
	return row


func _make_mode_row() -> HBoxContainer:
	var row := HBoxContainer.new()
	for mode in InterfaceModePresets.MODES:
		var btn := Button.new()
		btn.text = mode.capitalize()
		btn.pressed.connect(_on_mode_pressed.bind(mode))
		row.add_child(btn)
	var dbg := Button.new()
	dbg.text = "JSON"
	dbg.pressed.connect(func() -> void: _debug_panel.visible = not _debug_panel.visible)
	row.add_child(dbg)
	return row


func _on_mode_pressed(mode: String) -> void:
	controller.set_interface_mode(mode)
	_refresh_map()


func _on_jump_start() -> void:
	_stop_play()
	controller.jump_to_start()
	_refresh_all()


func _on_jump_end() -> void:
	_stop_play()
	controller.jump_to_end()
	_refresh_all()


func _on_step_back() -> void:
	_stop_play()
	controller.step_back()
	_refresh_all()


func _on_step_forward() -> void:
	_stop_play()
	controller.step_forward()
	_refresh_all()


func _on_play_pause() -> void:
	if _play_timer.is_stopped():
		_play_timer.wait_time = 1.0 / maxf(_play_speed, 0.1)
		_play_timer.start()
	else:
		_stop_play()


func _on_play_tick() -> void:
	if not controller.step_forward():
		_stop_play()
	_refresh_all()


func _stop_play() -> void:
	_play_timer.stop()


func _on_snapshot_refreshed(_snapshot: Dictionary) -> void:
	_refresh_map()


func _refresh_all() -> void:
	_status.text = controller.build_status_text()
	_action.text = controller.build_action_text()
	_metrics.text = controller.build_metrics_text()
	_debug.text = controller.build_debug_text()
	_debug_json.text = controller.build_debug_json_preview()
	_event_log.text = _format_events()
	_legend.text = _build_legend()
	_refresh_map()


func _format_events() -> String:
	var lines: PackedStringArray = PackedStringArray()
	for event in controller.get_events_for_step():
		lines.append(
			"- %s (tick %s)" % [str(event.get("event_type", "?")), str(event.get("tick", "?"))]
		)
	if lines.is_empty():
		return "Event log — (no events this step)"
	return "Event log — step %d\n" % controller.step_index + "\n".join(lines)


func _build_legend() -> String:
	match controller.interface_mode:
		"military":
			return "[b]Military[/b] Blue=p1 Red=p2 | HP bars | Fog from snapshot"
		"economy":
			return "[b]Economy[/b] Routes=amber | Forest D | Construction sites"
		"neural":
			return "[b]Neural[/b] Magenta target = current decision target"
		"debug":
			return "[b]Debug[/b] All layers + JSON panel"
		_:
			return "[b]World[/b] Terrain, buildings, units, roads, ownership"


func _refresh_map() -> void:
	_map.show_forest_density = controller.layer_visibility.get("forest_density", true)
	_map.show_logistics = controller.layer_visibility.get("logistics", true)
	var target: Dictionary = controller.command_target
	if target.is_empty():
		_map.set_attack_target(-1, -1)
	else:
		_map.set_attack_target(int(target.get("q", -1)), int(target.get("r", -1)))
	_map.set_render_data(controller.get_render_data())


func _on_map_selection(q: int, r: int) -> void:
	_map.set_selection(q, r)
	var sel: Dictionary = controller.get_selection_at(q, r)
	_inspector.text = SelectionFormatter.format(sel, controller.snapshot)
