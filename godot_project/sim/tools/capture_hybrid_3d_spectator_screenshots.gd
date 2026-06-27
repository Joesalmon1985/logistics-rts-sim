extends SceneTree

## Capture hybrid 3D spectator screenshot set + visual review artefacts.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/hybrid_3d_spectator_review_util.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 10
var _step: int = 0
var _shots: Array = []
var _steps: Array = [
	{"file": "hybrid_3d_spectator_00_main_menu", "action": "menu_skip"},
	{"file": "hybrid_3d_spectator_01_match_start", "action": "none"},
	{"file": "hybrid_3d_spectator_02_bot_a_base_growing", "action": "advance_40"},
	{"file": "hybrid_3d_spectator_03_bot_b_base_growing", "action": "follow_b"},
	{"file": "hybrid_3d_spectator_04_supply_to_military_site", "action": "toggle_supply"},
	{"file": "hybrid_3d_spectator_05_soldiers_produced", "action": "advance_120"},
	{"file": "hybrid_3d_spectator_06_forces_moving", "action": "follow_battle"},
	{"file": "hybrid_3d_spectator_07_combat", "action": "advance_combat"},
	{"file": "hybrid_3d_spectator_08_victory_or_timeout", "action": "advance_80"},
]


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_3d_spectator")
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
		scene.size = ViewerTestHarness.SIZE
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES + 2):
		ViewerTestHarness.tick_frames(_ctx)


func _run_step() -> void:
	var spec: Dictionary = _steps[_step]
	match str(spec.get("action", "")):
		"advance_40":
			if _scene.has_method("advance_for_tests"):
				_scene.call("advance_for_tests", 40)
		"advance_120":
			if _scene.has_method("advance_for_tests"):
				_scene.call("advance_for_tests", 120)
		"advance_80":
			if _scene.has_method("advance_for_tests"):
				_scene.call("advance_for_tests", 80)
		"advance_combat":
			if _scene.has_method("advance_until_combat_for_tests"):
				_scene.call("advance_until_combat_for_tests", 500)
		"follow_b":
			if _scene.has_method("follow_bot_for_tests"):
				_scene.call("follow_bot_for_tests", "p2")
		"follow_battle":
			if _scene.has_method("follow_bot_for_tests"):
				_scene.call("follow_bot_for_tests", "battle")
		"toggle_supply":
			if "controller" in _scene:
				var controller = _scene.controller
				if controller != null:
					controller.set_overlay_visible("supply", true)
	_force_layout()
	var review: Dictionary = ReviewUtil.probe_scene(_scene)
	review["file"] = str(spec.get("file", ""))
	var path: String = ReviewUtil.ensure_output_dir().path_join("%s.png" % review["file"])
	var img: Image = ViewerTestHarness.capture(_ctx)
	if img != null and img.save_png(path) == OK:
		review["screenshot_path"] = path
		review["png_bytes"] = FileAccess.get_file_as_bytes(path).size()
	else:
		review["screenshot_path"] = ""
		review["png_bytes"] = 0
	_shots.append(review)
	_step += 1


func _finish() -> void:
	var summary: Dictionary = {"shots": _shots}
	ReviewUtil.write_visual_review(summary)
	var ok: bool = true
	for shot in _shots:
		if not bool(shot.get("visibility_gate_pass", false)):
			ok = false
	if ok:
		print("HYBRID_3D_SPECTATOR_CAPTURE PASS shots=%d" % _shots.size())
		call_deferred("quit", 0)
		return
	print("HYBRID_3D_SPECTATOR_CAPTURE FAIL")
	call_deferred("quit", 1)
