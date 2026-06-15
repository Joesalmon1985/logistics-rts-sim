class_name ViewerRoot
extends Control

const SCREEN_SIZE := Vector2i(1920, 1080)

var controller: ViewerController
var demo_builder: Callable
var build_tutorial_builder: Callable
var variant_label: String = "viewer"
var viewer_title: String = "Debug Viewer"
var help_text: String = ""
var map_title: String = "Map"

@onready var _map: MapViewport = %MapViewport
@onready var _command_list: ItemList = %CommandList
@onready var _inspector: RichTextLabel = %Inspector
@onready var _event_log: RichTextLabel = %EventLog
@onready var _status: Label = %StatusLabel
@onready var _debug: Label = %DebugLabel
@onready var _summary: Label = %SummaryLabel
@onready var _legend: RichTextLabel = %Legend
@onready var _result: Label = %ResultLabel
@onready var _help: RichTextLabel = %HelpPanel
@onready var _commands_header: Label = %CommandsHeader
@onready var _title: Label = %TitleLabel
@onready var _checklist: Label = get_node_or_null("%ChecklistLabel")
@onready var _logistics_filter_btn: Button = get_node_or_null("%LogisticsFilterBtn")

var _logistics_event_filter: bool = false


func _ready() -> void:
	set_deferred("size", SCREEN_SIZE)
	if controller == null:
		controller = ViewerController.new()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.command_result.connect(_on_command_result)
	_map.selection_clicked.connect(_on_map_selection)
	_map.show_grid = true
	_map.show_coord_labels = true
	_map.map_title = map_title
	_setup_panel_labels()
	_build_legend()
	_apply_help_text()
	if demo_builder.is_valid():
		reset_demo()
	_show_welcome_inspector()
	call_deferred("_deferred_map_refresh")


func _deferred_map_refresh() -> void:
	await get_tree().process_frame
	await get_tree().process_frame
	_refresh_map_layers()


func _setup_panel_labels() -> void:
	_title.text = viewer_title
	if has_node("%ResetBtn"):
		get_node("%ResetBtn").text = "Load Production Demo"
	if has_node("%BuildTutorialBtn"):
		get_node("%BuildTutorialBtn").text = "Load Build Tutorial"
	if has_node("%LogisticsBtn"):
		get_node("%LogisticsBtn").text = "Toggle Logistics"
	if has_node("%ForestBtn"):
		get_node("%ForestBtn").text = "Toggle Forest D"
	if has_node("%StepBtn"):
		get_node("%StepBtn").text = "Step Tick"
	if has_node("%Step10Btn"):
		get_node("%Step10Btn").text = "Step 10"
	if has_node("%StepEventBtn"):
		get_node("%StepEventBtn").text = "Step Until Event"
	if has_node("%SubmitBtn"):
		get_node("%SubmitBtn").text = "Submit Command"
	if has_node("%InvalidBtn"):
		get_node("%InvalidBtn").text = "Test Invalid"
	if has_node("%GridBtn"):
		get_node("%GridBtn").text = "Toggle Coords"
	if has_node("%RoadsBtn"):
		get_node("%RoadsBtn").text = "Toggle Roads"
	if has_node("%ShotBtn"):
		get_node("%ShotBtn").text = "Screenshot"
	if has_node("%BotBtn"):
		get_node("%BotBtn").visible = variant_label in ["economy", "military"]


func _apply_help_text() -> void:
	if help_text != "":
		_help.text = help_text
	else:
		_help.text = (
			"[b]Quick start[/b]\n"
			+ "1. Map loads automatically — click a hex to inspect it.\n"
			+ "2. Pick a command from the list, then press Submit Command.\n"
			+ "3. Use Step Tick / Step 10 / Step Until Event to advance.\n"
			+ "4. Load/Reset Demo restores the starting state."
		)


func reset_demo() -> void:
	_load_demo_from_builder(demo_builder, "production demo")


func load_build_tutorial() -> void:
	if build_tutorial_builder.is_valid():
		_load_demo_from_builder(build_tutorial_builder, "build tutorial")
	elif demo_builder.is_valid():
		_load_demo_from_builder(demo_builder, "demo")


func _load_demo_from_builder(builder: Callable, label: String) -> void:
	if controller == null:
		controller = ViewerController.new()
	if builder.is_valid():
		controller.load_demo_state(builder.call(), "p1")
	_append_event("Demo loaded (%s — %s)" % [variant_label, label])
	_show_welcome_inspector()
	_refresh_map_layers()


func _show_welcome_inspector() -> void:
	_inspector.text = (
		"Welcome!\n\n"
		+ "Click any hex on the map to inspect terrain, buildings, construction sites, or units.\n"
		+ "Legal commands appear in the list below — select one, then Submit Command.\n"
		+ "Building placement creates a construction site (not an instant building)."
	)


func _on_snapshot_refreshed(snapshot: Dictionary) -> void:
	_refresh_map_layers()
	_update_attack_target_highlight()
	_refresh_commands()
	_update_debug(snapshot)
	_update_summary()
	_update_checklist()
	var cmd_count: int = ClientCommandAvailability.count(controller.get_available_commands())
	_commands_header.text = "Legal commands available: %d (select one, then Submit)" % cmd_count
	_status.text = "Tick %d  |  Active player: %s  |  Phase: debug step-through" % [
		int(snapshot.get("tick", 0)),
		controller.actor_id,
	]


func _refresh_map_layers() -> void:
	_map.show_forest_density = controller.layer_visibility.get("forest_density", true)
	_map.show_logistics = controller.layer_visibility.get("logistics", true)
	_map.production_pulse_ids = controller.production_pulse_ids.duplicate()
	_map.set_render_data(controller.get_render_data())


func _update_checklist() -> void:
	if _checklist == null:
		return
	if variant_label != "economy":
		_checklist.text = ""
		return
	var text: String = controller.build_checklist_text()
	_checklist.text = text if text != "" else "(Place a construction site to see build checklist)"


func _update_attack_target_highlight() -> void:
	var target: Dictionary = controller.last_attack_target
	if target.is_empty():
		_map.set_attack_target(-1, -1)
	else:
		_map.set_attack_target(int(target.get("q", -1)), int(target.get("r", -1)))


func _refresh_commands() -> void:
	_command_list.clear()
	var commands: Array = controller.get_available_commands()
	for cmd in commands:
		_command_list.add_item(str(cmd.get("label", cmd.get("type", "?"))))
	if not commands.is_empty():
		_command_list.select(0)


func _update_debug(snapshot: Dictionary) -> void:
	var counts: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	_debug.text = "Snapshot: %s | tick %d | bld %d | sites %d | roads %d | units %d | carriers %d" % [
		str(snapshot.get("variant", "")),
		int(snapshot.get("tick", 0)),
		int(counts.get("buildings", 0)),
		int(counts.get("construction_sites", 0)),
		int(counts.get("roads", 0)),
		int(counts.get("units", 0)),
		int(counts.get("carriers", 0)),
	]


func _update_summary() -> void:
	if _summary != null:
		_summary.text = controller.build_summary_text(variant_label)


func _on_command_result(result: Dictionary, dto: Dictionary) -> void:
	var label: String = str(dto.get("label", dto.get("type", "command")))
	var msg: String
	if bool(result.get("success", false)):
		msg = "ACCEPTED: %s" % label
		if str(dto.get("type", "")) == "PlaceBuildingPlanCommand" and variant_label == "economy":
			var site: Dictionary = controller.last_placed_site
			if not site.is_empty():
				_map.set_selection(int(site.get("q", -1)), int(site.get("r", -1)))
				var sel: Dictionary = controller.get_selection_at(int(site.get("q", -1)), int(site.get("r", -1)))
				_inspector.text = SelectionFormatter.format(sel, controller.snapshot)
	else:
		msg = "REJECTED: %s — %s" % [label, str(result.get("reason", result.get("error_code", "?")))]
	_result.text = msg
	_append_event(msg)


func _on_map_selection(q: int, r: int) -> void:
	_map.set_selection(q, r)
	var sel: Dictionary = controller.get_selection_at(q, r)
	_inspector.text = SelectionFormatter.format(sel, controller.snapshot)


func _append_event(text: String) -> void:
	var stamp: String = "[tick %d] " % controller.state.tick if controller.state != null else ""
	_event_log.text = stamp + text + "\n" + _event_log.text


func _build_legend() -> void:
	if variant_label == "military":
		_legend.text = (
			"[b]Legend — Military[/b]\n"
			+ "[color=#4791ff]Blue[/color] = p1 | [color=#e84848]Red[/color] = p2 enemy\n"
			+ "Gold = Command Hub | Orange = Power Generator | Dashed hex + ring = construction site\n"
			+ "HP bar above units/buildings | Red ring = attack target hex\n"
			+ "Yellow lines = roads | White coords | Yellow ring = selected hex"
		)
	else:
		_legend.text = (
			"[b]Legend — Economy[/b]\n"
			+ "Light green = grass | Dark green = forest | [color=#75ff75]D:n[/color] = tree density\n"
			+ "Dashed hex + green ring = construction site (% progress)\n"
			+ "[color=#f2a826]Dashed amber[/color] = carrier route | Orange dot = carrier (ware label)\n"
			+ "Store chips: T:timber P:planks | W# = workers | P# = production progress\n"
			+ "Brown = woodcutter | Tan = sawmill | Yellow lines = roads"
		)


func _on_reset_pressed() -> void:
	reset_demo()


func _on_step_pressed() -> void:
	var before: int = controller.state.tick
	controller.step_tick()
	_append_event("Step tick: %d -> %d" % [before, controller.state.tick])


func _on_step10_pressed() -> void:
	var before: int = controller.state.tick
	controller.step_ticks(10)
	_append_event("Step 10: %d -> %d" % [before, controller.state.tick])


func _on_step_event_pressed() -> void:
	var before: int = controller.state.tick
	var result: Dictionary = controller.step_until_meaningful_event()
	var after: int = controller.state.tick
	if bool(result.get("found", false)):
		_append_event(
			"Step Until Event: %d steps (%d->%d), stopped on '%s'" % [
				int(result.get("steps", 0)), before, after, str(result.get("event", "")),
			]
		)
		_result.text = "Stopped on event: %s after %d steps" % [str(result.get("event", "")), int(result.get("steps", 0))]
	else:
		_append_event("Step Until Event: %d steps (%d->%d), no meaningful event (cap %d)" % [
			int(result.get("steps", 0)), before, after, ViewerController.STEP_UNTIL_EVENT_MAX,
		])
		_result.text = "No meaningful event within %d steps" % ViewerController.STEP_UNTIL_EVENT_MAX


func _on_submit_pressed() -> void:
	var idx: Array = _command_list.get_selected_items()
	if idx.is_empty():
		_result.text = "Select a command from the list first, then press Submit Command."
		return
	var commands: Array = controller.get_available_commands()
	if int(idx[0]) >= commands.size():
		return
	controller.submit_command_dto(commands[int(idx[0])])


func _on_invalid_pressed() -> void:
	controller.submit_invalid_command()


func _on_toggle_grid_pressed() -> void:
	_map.show_coord_labels = not _map.show_coord_labels
	_map.queue_redraw()
	_append_event("Coordinate labels: %s" % ("ON" if _map.show_coord_labels else "OFF"))


func _on_toggle_roads_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("roads", true)
	controller.set_layer_visible("roads", vis)
	_refresh_map_layers()
	_append_event("Road layer: %s" % ("ON" if vis else "OFF"))


func _on_toggle_logistics_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("logistics", true)
	controller.set_layer_visible("logistics", vis)
	_refresh_map_layers()
	_append_event("Logistics layer: %s" % ("ON" if vis else "OFF"))


func _on_toggle_forest_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("forest_density", true)
	controller.set_layer_visible("forest_density", vis)
	_refresh_map_layers()
	_append_event("Forest density layer: %s" % ("ON" if vis else "OFF"))


func _on_build_tutorial_pressed() -> void:
	load_build_tutorial()


func _on_logistics_filter_pressed() -> void:
	_logistics_event_filter = not _logistics_event_filter
	if _logistics_filter_btn != null:
		_logistics_filter_btn.text = "Logistics Events: %s" % ("ON" if _logistics_event_filter else "OFF")
	_append_event("Logistics event filter: %s" % ("ON" if _logistics_event_filter else "OFF"))


func _on_bot_pressed() -> void:
	_run_one_bot_action()


func _run_one_bot_action() -> void:
	if controller.state == null:
		return
	var bot: Bot = null
	if variant_label == "economy":
		bot = HeuristicEconomyBot.new()
	elif variant_label == "military":
		bot = HeuristicMilitaryBot.new()
	if bot == null:
		_result.text = "Bot step not available for this variant"
		return
	bot.reset(controller.actor_id)
	bot.observe(SnapshotExporter.export_snapshot(controller.state), controller.actor_id)
	var cmd: SimCommand = bot.decide(controller.state.tick)
	if cmd == null:
		_result.text = "Bot: no action this turn"
		_append_event("Bot: no action")
		return
	var dto: Dictionary = {
		"type": cmd.type,
		"label": "Bot: %s" % cmd.type,
		"actor_id": cmd.player_id,
		"payload": cmd.payload.duplicate(true),
	}
	dto["payload"]["player_id"] = cmd.player_id
	dto["payload"]["tick"] = controller.state.tick
	controller.submit_command_dto(dto)


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
