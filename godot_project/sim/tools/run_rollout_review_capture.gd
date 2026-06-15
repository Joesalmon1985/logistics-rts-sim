extends SceneTree

## GOD-ROLL-001 — capture rollout observer screenshots at fixed steps (non-headless).


const ObserverScene := "res://client/scenes/rollout_observer_demo.tscn"
const SIZE := Vector2i(1920, 1080)
const MODES: Array[String] = ["world", "economy", "military", "neural", "debug"]

var _root: RolloutObserverRoot
var _subvp: SubViewport
var _steps: Array[int] = []
var _step_idx: int = 0
var _mode_idx: int = 0
var _frames_left: int = 8
var _output_dir: String = ""
var _phase: String = "warmup"
var _prev_tick: int = -1


func _initialize() -> void:
	SimBootstrap.load_all()
	var rollout_path: String = _parse_arg("rollout-path", "")
	var steps_raw: String = _parse_arg("steps", "0,1,2")
	if rollout_path == "":
		rollout_path = "res://sim/tests/fixtures/visual_rollouts/minimal_economy"
	for part in steps_raw.split(",", false):
		_steps.append(int(part.strip_edges()))
	_output_dir = _parse_arg("output", "")
	if _output_dir == "":
		_output_dir = ProjectSettings.globalize_path("user://rollout_capture")
	DirAccess.make_dir_recursive_absolute(_output_dir)

	var packed: PackedScene = load(ObserverScene)
	if packed == null:
		_fail("failed to load observer scene")
		return
	_root = packed.instantiate() as RolloutObserverRoot
	if _root == null:
		_fail("observer scene is not RolloutObserverRoot")
		return
	_subvp = SubViewport.new()
	_subvp.size = SIZE
	_subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	_root.custom_minimum_size = SIZE
	_root.size = SIZE
	_subvp.add_child(_root)
	root.add_child(_subvp)
	var loaded: Dictionary = _root.load_rollout_path(rollout_path)
	if not loaded.get("ok", false):
		_fail("load rollout: %s" % str(loaded.get("error", "")))


func _process(_delta: float) -> bool:
	if _root == null:
		return false
	if _phase == "warmup":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		_phase = "capture"
		_seek_step(_steps[_step_idx])
		_frames_left = 4
		return false
	if _phase == "capture":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		var step: int = _steps[_step_idx]
		var mode: String = MODES[_mode_idx]
		_root.controller.set_interface_mode(mode)
		_frames_left = 4
		_phase = "shot"
		return false
	if _phase == "shot":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		var step: int = _steps[_step_idx]
		var mode: String = MODES[_mode_idx]
		var img: Image = _subvp.get_texture().get_image()
		if img == null or img.is_empty():
			_fail("blank capture step %d mode %s" % [step, mode])
			return false
		var out_path: String = _output_dir.path_join("%03d_%s.png" % [step, mode])
		if img.save_png(out_path) != OK:
			_fail("save failed: %s" % out_path)
			return false
		_mode_idx += 1
		if _mode_idx >= MODES.size():
			_mode_idx = 0
			var state_path: String = _output_dir.path_join("%03d_state.json" % step)
			SimPaths.write_text_file(state_path, JSON.stringify(_root.controller.snapshot, "\t"))
			var tick: int = int(_root.controller.snapshot.get("tick", 0))
			if _prev_tick >= 0 and step > 0 and tick == _prev_tick:
				push_warning("tick unchanged at step %d" % step)
			_prev_tick = tick
			_step_idx += 1
			if _step_idx >= _steps.size():
				print("GOD-ROLL-001 PASS: captured %d steps to %s" % [_steps.size(), _output_dir])
				print("EXIT: 0 (pass)")
				quit(0)
				return false
			_seek_step(_steps[_step_idx])
		_phase = "capture"
		_frames_left = 2
	return false


func _seek_step(target: int) -> void:
	_root.controller.jump_to_start()
	while _root.controller.step_index < target:
		if not _root.controller.step_forward():
			break
	while _root.controller.step_index > target:
		if not _root.controller.step_back():
			break
	_root._refresh_all()


func _parse_arg(key: String, default_value: String) -> String:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--%s=" % key):
			return arg.substr(key.length() + 3)
	return default_value


func _fail(message: String) -> void:
	push_error(message)
	print("GOD-ROLL-001 FAIL: %s" % message)
	print("EXIT: 1 (fail)")
	quit(1)
