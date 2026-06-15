class_name ViewerRoot
extends Control

const SCREEN_SIZE := Vector2i(1920, 1080)

var controller: ViewerController
var demo_builder: Callable
var variant_label: String = "viewer"

@onready var _map: MapViewport = %MapViewport
@onready var _command_list: ItemList = %CommandList
@onready var _inspector: RichTextLabel = %Inspector
@onready var _event_log: RichTextLabel = %EventLog
@onready var _status: Label = %StatusLabel
@onready var _debug: Label = %DebugLabel
@onready var _legend: RichTextLabel = %Legend
@onready var _result: Label = %ResultLabel


func _ready() -> void:
	size = SCREEN_SIZE
	if controller == null:
		controller = ViewerController.new()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.command_result.connect(_on_command_result)
	_map.selection_clicked.connect(_on_map_selection)
	_build_legend()
	if demo_builder.is_valid():
		reset_demo()


func reset_demo() -> void:
	if controller == null:
		controller = ViewerController.new()
	if demo_builder.is_valid():
		controller.load_demo_state(demo_builder.call(), "p1")
	_append_event("Demo reset (%s)" % variant_label)


func _on_snapshot_refreshed(snapshot: Dictionary) -> void:
	_map.set_render_data(controller.get_render_data())
	_refresh_commands()
	_update_debug(snapshot)
	_status.text = "Tick %d | Player %s | Commands %d" % [
		int(snapshot.get("tick", 0)),
		controller.actor_id,
		ClientCommandAvailability.count(controller.get_available_commands()),
	]


func _refresh_commands() -> void:
	_command_list.clear()
	for cmd in controller.get_available_commands():
		_command_list.add_item("%s" % str(cmd.get("label", cmd.get("type", "?"))))


func _update_debug(snapshot: Dictionary) -> void:
	var counts: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	_debug.text = "Variant: %s | Source: in-process | Tick: %d | Avail: %d | Bld: %d | Units: %d" % [
		str(snapshot.get("variant", "")),
		int(snapshot.get("tick", 0)),
		ClientCommandAvailability.count(controller.get_available_commands()),
		int(counts.get("buildings", 0)),
		int(counts.get("units", 0)),
	]


func _on_command_result(result: Dictionary, _dto: Dictionary) -> void:
	var msg: String = "OK" if bool(result.get("success", false)) else str(result.get("reason", result.get("error_code", "?")))
	_result.text = "Last result: %s" % msg
	_append_event("Command: %s" % msg)


func _on_map_selection(q: int, r: int) -> void:
	var sel: Dictionary = controller.get_selection_at(q, r)
	_inspector.text = "[b]Selection %d,%d[/b]\n%s" % [q, r, JSON.stringify(sel.get("data", {}), "\t")]


func _append_event(text: String) -> void:
	_event_log.text = text + "\n" + _event_log.text


func _build_legend() -> void:
	_legend.text = (
		"[b]Legend[/b]\n"
		+ "Green = grass | Dark green = forest | Blue = water\n"
		+ "Gold hex = HQ/hub | Brown = woodcutter | Orange = power\n"
		+ "Blue-gray = barracks/processor | Red dot = unit\n"
		+ "Yellow lines = roads | White outline = selection"
	)


func _on_reset_pressed() -> void:
	reset_demo()


func _on_step_pressed() -> void:
	controller.step_tick()
	_append_event("Step tick -> %d" % controller.state.tick)


func _on_submit_pressed() -> void:
	var idx: Array = _command_list.get_selected_items()
	if idx.is_empty():
		_result.text = "Select a command first"
		return
	var commands: Array = controller.get_available_commands()
	if int(idx[0]) >= commands.size():
		return
	controller.submit_command_dto(commands[int(idx[0])])


func _on_invalid_pressed() -> void:
	controller.submit_invalid_command()


func _on_toggle_grid_pressed() -> void:
	_map.show_grid = not _map.show_grid
	_map.queue_redraw()


func _on_toggle_roads_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("roads", true)
	controller.set_layer_visible("roads", vis)
	_map.set_render_data(controller.get_render_data())


func _on_screenshot_pressed() -> void:
	var filename: String = "run_05_%s_demo.png" % variant_label
	var dir: String = "user://screenshots"
	DirAccess.make_dir_recursive_absolute(dir)
	await get_tree().process_frame
	var img: Image = get_viewport().get_texture().get_image()
	var path: String = dir.path_join(filename)
	if img.save_png(path) == OK:
		_result.text = "Screenshot saved: %s" % path
		_append_event("Screenshot: %s" % path)
	else:
		_result.text = "Screenshot failed"
