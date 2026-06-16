extends SceneTree

## GOD-ROLL-001 — capture rollout observer screenshots at fixed steps (non-headless).


const ObserverScene := "res://client/scenes/rollout_observer_demo.tscn"
const SIZE := Vector2i(1920, 1080)
const MODES: Array[String] = ["world", "economy", "military", "neural", "debug"]
const SETTLE_FRAMES := 8

var _root: RolloutObserverRoot
var _subvp: SubViewport
var _steps: Array[int] = []
var _step_idx: int = 0
var _mode_idx: int = 0
var _frames_left: int = SETTLE_FRAMES
var _output_dir: String = ""
var _rollout_path: String = ""
var _phase: String = "loading"
var _prev_tick: int = -1
var _loaded: bool = false
var _manifest: Dictionary = {
	"schema_version": 1,
	"resolution": {"width": SIZE.x, "height": SIZE.y},
	"rollout_path": "",
	"screenshots": [],
	"warnings": [],
}


func _initialize() -> void:
	SimBootstrap.load_all()
	_rollout_path = _parse_arg("rollout-path", "")
	var steps_raw: String = _parse_arg("steps", "0,1,2")
	if _rollout_path == "":
		_rollout_path = "res://sim/tests/fixtures/visual_rollouts/minimal_economy"
	for part in steps_raw.split(",", false):
		_steps.append(int(part.strip_edges()))
	_output_dir = _parse_arg("output", "")
	if _output_dir == "":
		_output_dir = ProjectSettings.globalize_path("user://rollout_capture")
	DirAccess.make_dir_recursive_absolute(_output_dir)
	_manifest["rollout_path"] = _rollout_path

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


func _process(_delta: float) -> bool:
	if _root == null:
		return false
	if _phase == "loading":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		var loaded: Dictionary = _root.load_rollout_path(_rollout_path)
		if not loaded.get("ok", false):
			_fail("load rollout: %s" % str(loaded.get("error", "")))
			return false
		_loaded = true
		_populate_manifest_metadata()
		_phase = "warmup"
		_frames_left = SETTLE_FRAMES
		return false
	if not _loaded:
		return false
	if _phase == "warmup":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		_phase = "capture"
		_seek_step(_steps[_step_idx])
		_frames_left = SETTLE_FRAMES
		return false
	if _phase == "capture":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		_root.controller.set_interface_mode(MODES[_mode_idx])
		_root._refresh_all()
		_frames_left = SETTLE_FRAMES
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
		if img.get_width() != SIZE.x or img.get_height() != SIZE.y:
			_fail("unexpected capture size %dx%d" % [img.get_width(), img.get_height()])
			return false
		var out_path: String = _output_dir.path_join("%03d_%s.png" % [step, mode])
		if img.save_png(out_path) != OK:
			_fail("save failed: %s" % out_path)
			return false
		var png_bytes: PackedByteArray = img.save_png_to_buffer()
		var shot_entry: Dictionary = {
			"step_index": step,
			"mode": mode,
			"path": out_path,
			"sha256": _sha256_hex(png_bytes),
			"overlay": _root.controller.capture_manifest_step_entry().get("overlay", {}),
		}
		_manifest["screenshots"].append(shot_entry)
		_mode_idx += 1
		if _mode_idx >= MODES.size():
			_check_mode_uniqueness(step)
			_mode_idx = 0
			var state_path: String = _output_dir.path_join("%03d_state.json" % step)
			SimPaths.write_text_file(state_path, JSON.stringify(_root.controller.snapshot, "\t"))
			var tick: int = int(_root.controller.snapshot.get("tick", 0))
			if _prev_tick >= 0 and step > 0 and tick == _prev_tick:
				_manifest["warnings"].append("tick unchanged at step %d" % step)
			_prev_tick = tick
			_step_idx += 1
			if _step_idx >= _steps.size():
				_write_manifest()
				print("GOD-ROLL-001 PASS: captured %d steps to %s" % [_steps.size(), _output_dir])
				print("EXIT: 0 (pass)")
				quit(0)
				return false
			_seek_step(_steps[_step_idx])
		_phase = "capture"
		_frames_left = 4
	return false


func _populate_manifest_metadata() -> void:
	var ctrl: RolloutController = _root.controller
	_manifest["rollout_id"] = ctrl.folder_path.get_file()
	_manifest["game_type"] = str(ctrl.metadata.get("game_type", ""))
	_manifest["policy_type"] = str(ctrl.metadata.get("policy_type", ""))
	_manifest["seed"] = ctrl.metadata.get("seed")
	_manifest["outcome"] = str(ctrl.metrics.get("final_outcome", ""))
	_manifest["fitness"] = ctrl.metrics.get("total_score")
	_manifest["captured_steps"] = _steps.duplicate()
	_manifest["captured_modes"] = MODES.duplicate()
	_manifest["screenshots_intentionally_unavailable"] = false
	if bool(ctrl.metadata.get("trained_available", true)) == false:
		_manifest["trained_unavailable_reason"] = str(ctrl.metadata.get("trained_absence_reason", ""))


func _check_mode_uniqueness(step: int) -> void:
	var hashes: Dictionary = {}
	var paths: Array = []
	for mode in MODES:
		var path: String = _output_dir.path_join("%03d_%s.png" % [step, mode])
		paths.append(path)
		if not FileAccess.file_exists(path):
			continue
		var img := Image.load_from_file(path)
		if img == null:
			continue
		hashes[mode] = _sha256_hex(img.save_png_to_buffer())
	if hashes.size() <= 1:
		return
	var unique: Dictionary = {}
	for mode in hashes.keys():
		unique[hashes[mode]] = unique.get(hashes[mode], []) + [mode]
	if unique.size() == 1:
		var msg: String = (
			"step %d mode screenshots are byte-identical for modes: %s"
			% [step, ", ".join(hashes.keys())]
		)
		_manifest["warnings"].append(msg)
		push_warning(msg)


func _sha256_hex(data: PackedByteArray) -> String:
	var ctx := HashingContext.new()
	ctx.start(HashingContext.HASH_SHA256)
	ctx.update(data)
	return ctx.finish().hex_encode()


func _write_manifest() -> void:
	var manifest_path: String = _output_dir.path_join("capture_manifest.json")
	SimPaths.write_text_file(manifest_path, JSON.stringify(_manifest, "\t"))


func _seek_step(target: int) -> void:
	_root.controller.jump_to_start()
	while _root.controller.step_index < target:
		if not _root.controller.step_forward():
			break
	while _root.controller.step_index > target:
		if not _root.controller.step_back():
			break
	_root._refresh_all()
	if _root._map != null:
		_root._map.fit_to_hexes(_root.controller.get_focus_hexes())
		_root._map.queue_redraw()


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
