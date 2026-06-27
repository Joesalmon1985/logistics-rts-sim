extends SceneTree

## Capture military spectator screenshot set + visual review artefacts.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/military_spectator_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _step: int = 0
var _shots: Array = []
var _all_ok: bool = true
var _steps: Array = [
	{"file": "military_spectator_00_overview", "action": "none"},
	{"file": "military_spectator_01_unit_selected", "action": "select_rifle"},
	{"file": "military_spectator_02_combat_event", "action": "step_event"},
	{"file": "military_spectator_03_production_queue", "action": "select_hub"},
]


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "military_spectator")
	_scene = _ctx.get("scene")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	if _step >= _steps.size():
		_finish()
		return true
	_run_step()
	_wait = 8
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


func _run_step() -> void:
	var step: Dictionary = _steps[_step]
	match str(step.get("action", "")):
		"select_rifle":
			var hex: Vector2i = ReviewUtil.find_first_unit_hex(_scene, "rifle_squad", "p1")
			if hex.x >= 0 and _scene.has_method("select_hex_for_tests"):
				_scene.call("select_hex_for_tests", hex.x, hex.y)
		"step_event":
			if _scene != null and "controller" in _scene:
				var controller = _scene.controller
				if controller != null:
					controller.step_until_event()
					if _scene.has_method("_update_panels"):
						_scene.call("_update_panels")
		"select_hub":
			var hub: Vector2i = ReviewUtil.find_first_building_hex(_scene, "command_hub", "p1")
			if hub.x >= 0 and _scene.has_method("select_hex_for_tests"):
				_scene.call("select_hex_for_tests", hub.x, hub.y)
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	review["file"] = str(step.get("file", "shot"))
	review["screenshot_path"] = _save_shot(str(step.get("file", "shot")))
	if not bool(review.get("visibility_gate_pass", false)):
		_all_ok = false
	_shots.append(review)
	_step += 1


func _save_shot(base: String) -> String:
	var dir: String = ReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base)
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	ReviewUtil.write_visual_review({"shots": _shots, "all_ok": _all_ok})
	if _all_ok:
		print("MILITARY SPECTATOR CAPTURE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("MILITARY SPECTATOR CAPTURE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
