extends Control

## RUN_05C.1 economy spectator — production-chain visual groundwork with placeholder sprites.

const SCREEN_SIZE := Vector2i(1920, 1080)
const AUTOPLAY_BASE_SEC: float = 0.35

var controller: EconomySpectatorController = EconomySpectatorController.new()

@onready var _map: MapViewport = %MapViewport
@onready var _sprite_layer: EconomySpriteLayer = %SpriteLayer
@onready var _status: Label = %StatusLabel
@onready var _debug: Label = %DebugLabel
@onready var _summary: Label = %SummaryLabel
@onready var _bot_status: Label = %BotStatusLabel
@onready var _activity: Label = %ActivityLabel
@onready var _event_log: RichTextLabel = %EventLog
@onready var _legend: RichTextLabel = %Legend
@onready var _help: RichTextLabel = %HelpPanel
@onready var _title: Label = %TitleLabel
@onready var _result: Label = %ResultLabel
@onready var _speed_label: Label = %SpeedLabel

var _autoplay_timer: Timer


func _ready() -> void:
	size = SCREEN_SIZE
	SimBootstrap.load_all()
	controller.snapshot_refreshed.connect(_on_snapshot_refreshed)
	controller.bot_action_applied.connect(_on_bot_action)
	_sprite_layer.setup(_map)
	_map.show_grid = true
	_map.show_coord_labels = false
	_map.show_logistics = true
	_map.map_title = "Economy spectator map"
	_setup_labels()
	_build_legend()
	_setup_autoplay_timer()
	controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	_refresh_map()


func _setup_labels() -> void:
	_title.text = "Economy Spectator — Visual Groundwork (RUN_05C.1)"
	_help.text = (
		"[b]Production chain spectator[/b] — seed 42, HQ → woodcutter → sawmill.\n"
		+ "[color=#ffaa00]Amber sprites[/color] = logistics | "
		+ "[color=#9a6b3a]Brown[/color] = timber/ware | "
		+ "[color=#55cc66]Green[/color] = working buildings.\n"
		+ "Play/Pause steps sim ticks. All visuals derive from real snapshot state.\n"
		+ "[i]Dual-bot view[/i] is manual diagnostic only — not used for smoke/tests."
	)


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


func _build_legend() -> void:
	_legend.text = (
		"[b]Sprite legend[/b]\n"
		+ "[color=#ffaa00]●[/color] carrier_move / road_flow | "
		+ "[color=#9a6b3a]■[/color] ware_log | "
		+ "[color=#888888]▣[/color] construction_progress\n"
		+ "[color=#55cc66]▣[/color] building_working | "
		+ "[color=#cc4444]![/color] blocked/warning | "
		+ "Idle/stall states remain visible when chain pauses"
	)


func _on_snapshot_refreshed(_snapshot: Dictionary) -> void:
	_refresh_map()
	_update_panels()


func _refresh_map() -> void:
	if _map == null or _sprite_layer == null:
		return
	_map.show_forest_density = controller.layer_visibility.get("forest_density", true)
	_map.show_logistics = controller.layer_visibility.get("logistics", true)
	_map.set_render_data(controller.get_render_data())
	_sprite_layer.apply_sprite_states(controller.get_sprite_states())


func _update_panels() -> void:
	var snap: Dictionary = controller.snapshot
	_status.text = "Tick %d | %s | %s" % [
		int(snap.get("tick", 0)),
		controller.demo_label,
		"AUTOPLAY" if controller.autoplay_enabled else "PAUSED",
	]
	var counts: Dictionary = LayerRenderer.count_entities(controller.get_render_data())
	_debug.text = (
		"roads %d | sites %d | carriers %d | sprites %d | events %d"
		% [
			int(counts.get("roads", 0)),
			int(counts.get("construction_sites", 0)),
			int(counts.get("carriers", 0)),
			_sprite_layer.active_count,
			controller.event_log_lines.size(),
		]
	)
	_summary.text = controller.build_summary_text()
	_bot_status.text = controller.build_bot_status_text()
	_activity.text = controller.build_activity_hint()
	_event_log.text = controller.build_event_log_text()
	_speed_label.text = "Speed: %.1fx" % controller.speed_multiplier


func _on_bot_action(player_id: String, command_type: String, success: bool, _message: String) -> void:
	_result.text = "%s %s %s" % [player_id, command_type, "OK" if success else "FAIL"]


func _on_reset_pressed() -> void:
	controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	_result.text = "Production chain demo reset"


func _on_dual_bot_pressed() -> void:
	controller.reset_demo(EconomySpectatorController.DEMO_DUAL_BOT)
	_result.text = "Switched to dual-bot diagnostic view (manual only)"


func _on_play_pause_pressed() -> void:
	controller.autoplay_enabled = not controller.autoplay_enabled
	if controller.autoplay_enabled:
		_autoplay_timer.start()
	else:
		_autoplay_timer.stop()
	_update_panels()
	_update_autoplay_timer()


func _on_bot_step_pressed() -> void:
	controller.autoplay_step()
	_result.text = "Step complete"


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


func _on_toggle_logistics_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("logistics", true)
	controller.set_layer_visible("logistics", vis)
	_refresh_map()


func _on_toggle_roads_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("roads", true)
	controller.set_layer_visible("roads", vis)
	_refresh_map()


func _on_toggle_ownership_pressed() -> void:
	var vis: bool = not controller.layer_visibility.get("ownership", true)
	controller.set_layer_visible("ownership", vis)
	_refresh_map()


func _on_screenshot_pressed() -> void:
	var path: String = SimPaths.default_export_file("economy_spectator_%d.png" % int(controller.snapshot.get("tick", 0)))
	var img: Image = get_viewport().get_texture().get_image()
	if img != null and img.save_png(path) == OK:
		_result.text = "Screenshot: %s" % path
	else:
		_result.text = "Screenshot failed"


func _on_autoplay_tick() -> void:
	if controller.autoplay_enabled:
		controller.autoplay_step()
