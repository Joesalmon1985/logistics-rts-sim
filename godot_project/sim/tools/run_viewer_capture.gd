extends SceneTree

## Headless viewer screenshot capture for RUN_05 (1920x1080 SubViewport render).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const SIZE := Vector2i(1920, 1080)

const SCENES: Dictionary = {
	"economy": "res://client/scenes/economy_viewer_demo.tscn",
	"military": "res://client/scenes/military_viewer_demo.tscn",
}

var _viewport: SubViewport
var _variant: String = "economy"
var _frames_left: int = 4


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg in args:
		if arg.begins_with("--variant="):
			_variant = arg.substr("--variant=".length())
	call_deferred("_setup_capture")


func _setup_capture() -> void:
	if not SCENES.has(_variant):
		print("CAPTURE FAIL: unknown variant %s" % _variant)
		quit(1)
		return

	var packed: PackedScene = load(str(SCENES[_variant]))
	if packed == null:
		print("CAPTURE FAIL: could not load scene")
		quit(1)
		return

	var scene: Control = packed.instantiate()
	_viewport = SubViewport.new()
	_viewport.size = SIZE
	_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	scene.custom_minimum_size = SIZE
	scene.size = SIZE
	_viewport.add_child(scene)
	root.add_child(_viewport)
	call_deferred("_tick_capture")


func _tick_capture() -> void:
	_frames_left -= 1
	if _frames_left > 0:
		call_deferred("_tick_capture")
		return
	_finish_capture()


func _finish_capture() -> void:
	var tex: ViewportTexture = _viewport.get_texture()
	var ok: bool = false
	if tex != null:
		var img: Image = tex.get_image()
		if img != null and not img.is_empty():
			var dir: String = ProjectSettings.globalize_path("user://screenshots")
			DirAccess.make_dir_recursive_absolute(dir)
			var path: String = dir.path_join("run_05_%s_demo.png" % _variant)
			ok = img.save_png(path) == OK
			if ok:
				print("Saved screenshot: %s" % path)
	if ok:
		print("CAPTURE PASS: %s" % _variant)
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		print("CAPTURE FAIL: %s" % _variant)
		print("EXIT: 1 (fail)")
		quit(1)
