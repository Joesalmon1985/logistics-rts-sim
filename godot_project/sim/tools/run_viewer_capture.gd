extends SceneTree

## Viewer screenshot capture for RUN_05 (1920x1080 SubViewport; non-headless).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

var _ctx: Dictionary = {}
var _variant: String = "economy"
var _assert_pixels: bool = false
var _ready: bool = false


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg.begins_with("--variant="):
			_variant = arg.substr("--variant=".length())
		elif arg == "--assert":
			_assert_pixels = true
	call_deferred("_setup_capture")


func _setup_capture() -> void:
	_ctx = ViewerTestHarness.mount(root, _variant)
	if not bool(_ctx.get("ok", false)):
		print("CAPTURE FAIL: mount %s — %s" % [_variant, str(_ctx.get("reason", "?"))])
		quit(1)


func _process(_delta: float) -> bool:
	if _ctx.is_empty():
		return false
	if not _ready:
		if ViewerTestHarness.tick_frames(_ctx):
			_ready = true
			call_deferred("_finish_capture")
		return false
	return false


func _finish_capture() -> void:
	var img: Image = ViewerTestHarness.capture(_ctx)
	var ok: bool = false
	if img != null and not img.is_empty():
		var dir: String = ProjectSettings.globalize_path("user://screenshots")
		DirAccess.make_dir_recursive_absolute(dir)
		var path: String = dir.path_join("run_05_%s_demo.png" % _variant)
		ok = img.save_png(path) == OK
		if ok:
			print("Saved screenshot: %s" % path)
	if ok and _assert_pixels:
		var check: Dictionary
		if _variant == "economy":
			check = ViewerPixelProbe.assert_economy_terrain_visible(img)
		else:
			check = ViewerPixelProbe.assert_military_entities_visible(img)
		if not bool(check.get("ok", false)):
			print("GOD-CAPTURE-001 FAIL: %s" % str(check.get("reason", "?")))
			ok = false
		else:
			print("GOD-CAPTURE-001 PASS: %s" % _variant)
	ViewerTestHarness.teardown(_ctx)
	if ok:
		print("CAPTURE PASS: %s" % _variant)
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("CAPTURE FAIL: %s" % _variant)
		print("EXIT: 1 (fail)")
		quit(1)
