extends SceneTree

## Verify military spectator screen legibility gates (plain English + required panels).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/military_spectator_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "military_spectator")
	_scene = _ctx.get("scene")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		0:
			_check_overview()
			_phase = 1
			_wait = 6
		1:
			_check_unit_selected()
			_phase = 2
			_wait = 6
		2:
			_check_combat_event()
			_phase = 3
			_wait = 6
		3:
			_check_production_queue()
			_finish()
			return true
	return false


func _force_layout() -> void:
	var scene: Control = _scene as Control
	if scene != null:
		scene.set_anchors_preset(Control.PRESET_TOP_LEFT)
		scene.size = ViewerTestHarness.SIZE
		scene.custom_minimum_size = ViewerTestHarness.SIZE
	var subvp: SubViewport = _ctx.get("viewport")
	if subvp != null:
		subvp.size = ViewerTestHarness.SIZE
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES):
		ViewerTestHarness.tick_frames(_ctx)


func _check_overview() -> void:
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	review["context"] = "overview"
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("overview:%s" % str(review.get("failures", [])))


func _check_unit_selected() -> void:
	var hex: Vector2i = ReviewUtil.find_first_unit_hex(_scene, "rifle_squad", "p1")
	if hex.x >= 0 and _scene.has_method("select_hex_for_tests"):
		_scene.call("select_hex_for_tests", hex.x, hex.y)
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	var inspector: Dictionary = ReviewUtil.probe_inspector_plain_english(_scene, PackedStringArray(["Rifle Squad"]))
	review["context"] = "unit_selected"
	review["inspector"] = inspector
	if not bool(inspector.get("passes", false)):
		_failures.append("unit_selected:inspector:%s" % str(inspector.get("missing", [])))
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("unit_selected:%s" % str(review.get("failures", [])))


func _check_combat_event() -> void:
	if _scene != null and "controller" in _scene:
		var controller = _scene.controller
		if controller != null:
			controller.step_until_event()
			if _scene.has_method("_update_panels"):
				_scene.call("_update_panels")
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	review["context"] = "combat_event"
	var events: RichTextLabel = _scene.get_node_or_null("%EventLog") as RichTextLabel
	var event_text: String = events.get_parsed_text() if events != null else ""
	if event_text.length() < 4:
		_failures.append("combat_event:empty_event_log")
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("combat_event:%s" % str(review.get("failures", [])))


func _check_production_queue() -> void:
	var production: RichTextLabel = _scene.get_node_or_null("%ProductionQueuesPanel") as RichTextLabel
	var text: String = production.get_parsed_text() if production != null else ""
	if not ("Rifle Squad" in text or "Production queue" in text):
		_failures.append("production_queue:missing_queue_text")
	var hub_hex: Vector2i = ReviewUtil.find_first_building_hex(_scene, "command_hub", "p1")
	if hub_hex.x >= 0 and _scene.has_method("select_hex_for_tests"):
		_scene.call("select_hex_for_tests", hub_hex.x, hub_hex.y)
	_force_layout()
	var inspector: Dictionary = ReviewUtil.probe_inspector_plain_english(_scene, PackedStringArray(["Command Hub"]))
	if not bool(inspector.get("passes", false)):
		_failures.append("production_queue:hub_inspector:%s" % str(inspector.get("missing", [])))


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY MILITARY SPECTATOR SCREEN PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY MILITARY SPECTATOR SCREEN FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
