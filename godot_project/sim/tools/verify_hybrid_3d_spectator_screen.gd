extends SceneTree

## Verify hybrid 3D spectator screen legibility gates.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_3d_spectator_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _phase: int = 0
var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_3d_spectator")
	_scene = _ctx.get("scene")
	if _scene != null and _scene.has_method("ensure_match_started"):
		_scene.call("ensure_match_started")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		0:
			_check_overview()
			_phase = 1
			_wait = 8
		1:
			_check_building_inspector()
			_phase = 2
			_wait = 8
		2:
			_check_bot_summaries()
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
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES + 4):
		ViewerTestHarness.tick_frames(_ctx)


func _check_overview() -> void:
	if _scene != null and _scene.has_method("advance_for_tests"):
		_scene.call("advance_for_tests", 5)
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	if not bool(review.get("visibility_gate_pass", false)):
		_failures.append("overview:%s" % str(review.get("failures", [])))


func _check_building_inspector() -> void:
	var hex: Vector2i = ReviewUtil.find_first_building_hex(_scene, "headquarters", "p1")
	if hex.x < 0:
		hex = ReviewUtil.find_first_building_hex(_scene, "command_hub", "p1")
	if hex.x >= 0 and _scene.has_method("select_hex_for_tests"):
		_scene.call("select_hex_for_tests", hex.x, hex.y)
	_force_layout()
	var inspector: Dictionary = ReviewUtil.probe_inspector_plain_english(_scene, PackedStringArray(["Headquarters", "Command Hub"]))
	if not bool(inspector.get("passes", false)):
		_failures.append("building_inspector:%s" % str(inspector.get("missing", [])))


func _check_bot_summaries() -> void:
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	var bot_a: Dictionary = review.get("panels", {}).get("bot_a", {})
	var bot_b: Dictionary = review.get("panels", {}).get("bot_b", {})
	if not bool(bot_a.get("visible", false)) or not bool(bot_b.get("visible", false)):
		_failures.append("bot_summaries:not_visible")


func _finish() -> void:
	if _failures.is_empty():
		print("HYBRID_3D_SPECTATOR_SCREEN PASS")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("HYBRID_3D_SPECTATOR_SCREEN FAIL")
		quit(1)
