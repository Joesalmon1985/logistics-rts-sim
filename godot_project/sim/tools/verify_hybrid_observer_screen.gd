extends SceneTree

## Verify hybrid observer screen legibility gates (MRP C2).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_observer_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_observer")
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
			_check_barracks_selected()
			_phase = 2
			_wait = 6
		2:
			_check_supply_chain()
			_phase = 3
			_wait = 6
		3:
			_check_production()
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
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("overview:%s" % str(review.get("failures", [])))


func _check_barracks_selected() -> void:
	var hex: Vector2i = ReviewUtil.find_first_military_building_hex(_scene, "barracks", "p1")
	if hex.x >= 0 and _scene.has_method("select_hex_for_tests"):
		_scene.call("select_hex_for_tests", hex.x, hex.y)
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("barracks_selected:%s" % str(review.get("failures", [])))


func _check_supply_chain() -> void:
	if _scene != null and "controller" in _scene:
		var controller = _scene.controller
		if controller != null:
			controller.step_ticks(15)
			if _scene.has_method("_update_panels"):
				_scene.call("_update_panels")
	_force_layout()
	var chain: RichTextLabel = _scene.get_node_or_null("%ChainPanel") as RichTextLabel
	var text: String = chain.get_parsed_text() if chain != null else ""
	if not ("military" in text.to_lower()):
		_failures.append("supply_chain:missing_military_text")


func _check_production() -> void:
	if _scene != null and "controller" in _scene:
		var controller = _scene.controller
		if controller != null:
			controller.step_until_event()
			if _scene.has_method("_update_panels"):
				_scene.call("_update_panels")
	_force_layout()
	var production: RichTextLabel = _scene.get_node_or_null("%ProductionPanel") as RichTextLabel
	var text: String = production.get_parsed_text() if production != null else ""
	if text.length() < 8:
		_failures.append("production:empty_panel")


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	if _failures.is_empty():
		print("VERIFY HYBRID OBSERVER SCREEN PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY HYBRID OBSERVER SCREEN FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
