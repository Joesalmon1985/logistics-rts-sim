extends SceneTree

## RUN_05C.2 economy spectator capture + contact sheet (pixel sanity checks only).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const OUTPUT_SUBDIR: String = "training_runs/visual_review/run_05c_economy_spectator"
const MILESTONE_TICKS: Array[int] = [0, 10, 30, 60]

var _ctx: Dictionary = {}
var _controller: EconomySpectatorController
var _scene: Node
var _phase: String = "init"
var _wait_frames: int = 0
var _current_tick: int = 0
var _milestone_index: int = 0
var _do_assert: bool = false
var _output_dir: String = ""
var _manifest: Array = []
var _images: Dictionary = {}
var _capture_ok: bool = true
var _fail_reason: String = ""


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg == "--assert":
			_do_assert = true
	_output_dir = _resolve_output_dir()
	DirAccess.make_dir_recursive_absolute(_output_dir)
	_ctx = ViewerTestHarness.mount(root, "spectator")
	if not bool(_ctx.get("ok", false)):
		print("ECON SPECTATOR CAPTURE FAIL: mount")
		quit(1)
		return
	_scene = _ctx.get("scene")
	_controller = _scene.controller if _scene != null else null
	if _controller == null:
		print("ECON SPECTATOR CAPTURE FAIL: no controller")
		ViewerTestHarness.teardown(_ctx)
		quit(1)
		return
	_controller.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	_current_tick = 0
	_milestone_index = 0
	_phase = "wait_capture"
	_wait_frames = 8


func _process(_delta: float) -> bool:
	match _phase:
		"wait_capture":
			if _wait_frames > 0:
				_wait_frames -= 1
				return false
			_capture_milestone()
			if _milestone_index >= MILESTONE_TICKS.size():
				_wait_frames = 30
				_phase = "teardown_wait"
				return false
			_phase = "step_to_next"
			return false
		"step_to_next":
			var target: int = MILESTONE_TICKS[_milestone_index]
			while _current_tick < target:
				_controller.step_tick()
				if _scene != null and _scene.has_method("_refresh_map"):
					_scene._refresh_map()
				_current_tick += 1
			_phase = "wait_capture"
			_wait_frames = 8
			return false
		"teardown_wait":
			if _wait_frames > 0:
				_wait_frames -= 1
				return false
			_finish()
			return true
	return false


func _capture_milestone() -> void:
	var tick: int = MILESTONE_TICKS[_milestone_index]
	var img: Image = ViewerTestHarness.capture(_ctx)
	var png_name: String = "spectator_tick%d.png" % tick
	var png_path: String = _output_dir.path_join(png_name)
	var saved: bool = img != null and not img.is_empty() and img.save_png(png_path) == OK
	_images[tick] = img
	var render: Dictionary = _controller.get_render_data()
	var states: Dictionary = _controller.get_sprite_states()
	var family_eval: Dictionary = EconomySpectatorMetrics.evaluate_visual_families(
		render,
		_controller.snapshot,
		states,
	)
	_manifest.append({
		"tick": tick,
		"png": png_name,
		"png_saved": saved,
		"sprite_count": states.size(),
		"exposable_families": family_eval.get("exposable_families", {}),
		"active_families": family_eval.get("active_families", {}),
	})
	_milestone_index += 1


func _finish() -> void:
	if _do_assert:
		_run_assertions()
	var manifest_path: String = _output_dir.path_join("capture_manifest.json")
	var html_path: String = _output_dir.path_join("contact_sheet.html")
	_write_json(manifest_path, {"panels": _manifest, "output_dir": _output_dir})
	_write_text(html_path, _build_contact_sheet_html())
	ViewerTestHarness.teardown(_ctx)
	if not _capture_ok:
		print("ECON SPECTATOR CAPTURE FAIL: %s" % _fail_reason)
		quit(1)
		return
	var delta_0_30: int = EconomySpectatorMetrics.image_pixel_delta(_images.get(0), _images.get(30))
	print("ECON SPECTATOR CAPTURE PASS: %s pixel_delta_0_30=%d" % [_output_dir, delta_0_30])
	quit(0)


func _run_assertions() -> void:
	for entry in _manifest:
		var tick: int = int(entry.get("tick", -1))
		var img: Image = _images.get(tick)
		if not _image_not_blank(img):
			_capture_ok = false
			_fail_reason = "blank image tick %d" % tick
			return
		if not _image_has_multiple_colors(img):
			_capture_ok = false
			_fail_reason = "single color tick %d" % tick
			return
		if not _panel_region_nonempty(img):
			_capture_ok = false
			_fail_reason = "panel empty tick %d" % tick
			return
	var img0: Image = _images.get(0)
	var img30: Image = _images.get(30)
	var delta_0_30: int = EconomySpectatorMetrics.image_pixel_delta(img0, img30)
	if delta_0_30 <= 5:
		_capture_ok = false
		_fail_reason = "tick0->tick30 pixel delta %d <= 5" % delta_0_30
		return
	var img60: Image = _images.get(60)
	if img30 != null and img60 != null:
		var delta_30_60: int = EconomySpectatorMetrics.image_pixel_delta(img30, img60)
		if delta_30_60 <= 5:
			for i in range(_manifest.size()):
				if int(_manifest[i].get("tick", -1)) == 60:
					_manifest[i]["demo_stabilised"] = true
					_manifest[i]["pixel_delta_30_60"] = delta_30_60
					_manifest[i]["note"] = "production chain plateau after tick 30"


func _resolve_output_dir() -> String:
	var repo_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	return repo_root.path_join(OUTPUT_SUBDIR)


func _build_contact_sheet_html() -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append("<html><head><title>RUN_05C Economy Spectator</title></head><body>")
	parts.append("<h1>RUN_05C Economy Spectator Contact Sheet</h1>")
	parts.append("<p>Authoritative metrics in capture_manifest.json; pixels are sanity checks only.</p>")
	parts.append("<table border='1' cellpadding='6'>")
	for entry in _manifest:
		var tick: int = int(entry.get("tick", 0))
		var png: String = str(entry.get("png", ""))
		parts.append("<tr><td>Tick %d</td><td><img src='%s' width='480'/></td><td>sprites=%d</td></tr>"
			% [tick, png, int(entry.get("sprite_count", 0))])
	parts.append("</table></body></html>")
	return "\n".join(parts)


static func _write_json(path: String, data: Dictionary) -> void:
	SimPaths.write_text_file(path, JSON.stringify(data, "\t"))


static func _write_text(path: String, text: String) -> void:
	SimPaths.write_text_file(path, text)


static func _image_not_blank(img: Image) -> bool:
	if img == null or img.is_empty():
		return false
	var non_bg: int = 0
	for y in range(0, img.get_height(), 8):
		for x in range(0, img.get_width(), 8):
			var c: Color = img.get_pixel(x, y)
			if c.a > 0.1 and (c.r > 0.05 or c.g > 0.05 or c.b > 0.05):
				non_bg += 1
	return non_bg > 10


static func _image_has_multiple_colors(img: Image) -> bool:
	var buckets: Dictionary = {}
	for y in range(0, img.get_height(), 12):
		for x in range(0, img.get_width(), 12):
			var c: Color = img.get_pixel(x, y)
			var key: String = "%d,%d,%d" % [int(c.r * 4), int(c.g * 4), int(c.b * 4)]
			buckets[key] = true
	return buckets.size() >= 3


static func _panel_region_nonempty(img: Image) -> bool:
	var w: int = img.get_width()
	var h: int = img.get_height()
	var panel_x: int = int(w * 0.72)
	var lit: int = 0
	for y in range(20, h - 20, 10):
		for x in range(panel_x, w - 10, 10):
			var c: Color = img.get_pixel(x, y)
			if c.a > 0.2:
				lit += 1
	return lit > 5
