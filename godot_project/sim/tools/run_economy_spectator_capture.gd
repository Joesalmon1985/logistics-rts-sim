extends SceneTree

## RUN_05C economy spectator pixel capture smoke.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

var _ctx: Dictionary = {}
var _controller: EconomySpectatorController
var _scene: Node
var _phase: String = "init"
var _wait_frames: int = 0
var _step_index: int = 0
var _do_assert: bool = false
var _img_start: Image


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg == "--assert":
			_do_assert = true
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
	_controller.reset_demo()
	_phase = "wait_start"
	_wait_frames = 8


func _process(_delta: float) -> bool:
	match _phase:
		"wait_start":
			if _wait_frames > 0:
				_wait_frames -= 1
				return false
			_img_start = ViewerTestHarness.capture(_ctx)
			_phase = "step"
			_step_index = 0
			return false
		"step":
			if _step_index < 80:
				_controller.bot_step_once()
				if _scene != null and _scene.has_method("_refresh_map"):
					_scene._refresh_map()
				_step_index += 1
				return false
			_wait_frames = 8
			_phase = "wait_active"
			return false
		"wait_active":
			if _wait_frames > 0:
				_wait_frames -= 1
				return false
			_finish_capture()
			return true
	return false


func _finish_capture() -> void:
	var img_active: Image = ViewerTestHarness.capture(_ctx)
	var out_dir: String = SimPaths.default_export_dir().path_join("run_05c_economy_spectator")
	DirAccess.make_dir_recursive_absolute(out_dir)
	var path_start: String = out_dir.path_join("spectator_tick0.png")
	var path_active: String = out_dir.path_join("spectator_tick80.png")
	_img_start.save_png(path_start)
	img_active.save_png(path_active)

	var ok: bool = true
	if _do_assert:
		ok = ok and _image_not_blank(_img_start)
		ok = ok and _image_not_blank(img_active)
		ok = ok and _image_has_multiple_colors(img_active)
		ok = ok and _panel_region_nonempty(img_active)
	ViewerTestHarness.teardown(_ctx)
	if not ok:
		print("ECON SPECTATOR CAPTURE FAIL: pixel assertions")
		quit(1)
		return
	print("ECON SPECTATOR CAPTURE PASS: %s %s" % [path_start, path_active])
	quit(0)


static func _image_not_blank(img: Image) -> bool:
	if img == null or img.is_empty():
		return false
	var w: int = img.get_width()
	var h: int = img.get_height()
	var non_bg: int = 0
	for y in range(0, h, 8):
		for x in range(0, w, 8):
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
