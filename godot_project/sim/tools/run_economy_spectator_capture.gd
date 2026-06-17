extends SceneTree

## RUN_05C.2 economy spectator capture + contact sheet (pixel sanity checks only).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const OUTPUT_SUBDIR: String = EconomySpectatorMetrics.CAPTURE_REVIEW_SUBDIR
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
	var activity_score: int = EconomySpectatorMetrics.activity_score(
		render,
		_controller.snapshot,
		states,
	)
	var pixel_delta_0: int = 0
	if tick > 0 and _images.has(0) and img != null:
		pixel_delta_0 = EconomySpectatorMetrics.image_pixel_delta(_images.get(0), img)
	_manifest.append({
		"tick": tick,
		"png": png_name,
		"png_saved": saved,
		"sprite_count": states.size(),
		"activity_score": activity_score,
		"meaningful_count": _meaningful_count_at_tick(tick),
		"pixel_delta_from_tick0": pixel_delta_0,
		"exposable_families": family_eval.get("exposable_families", {}),
		"active_families": family_eval.get("active_families", {}),
		"family_passes": bool(family_eval.get("passes", false)),
	})
	_milestone_index += 1


func _finish() -> void:
	if _do_assert:
		_run_assertions()
	var manifest_path: String = _output_dir.path_join("capture_manifest.json")
	var html_path: String = _output_dir.path_join("contact_sheet.html")
	_write_json(manifest_path, {"panels": _manifest, "output_dir": _output_dir, "manifest_path": manifest_path})
	_write_text(html_path, _build_contact_sheet_html())
	_write_text(_output_dir.path_join("REVIEW_NOTES.md"), _build_review_notes())
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


func _meaningful_count_at_tick(tick: int) -> int:
	if tick <= 0:
		return 0
	var probe := EconomySpectatorController.new()
	probe.reset_demo(EconomySpectatorController.DEMO_PRODUCTION_CHAIN)
	var before_render: Dictionary = probe.get_render_data()
	var before_sprites: Dictionary = probe.get_sprite_states()
	var before_snap: Dictionary = probe.snapshot.duplicate(true)
	var events_before: int = probe.state.event_log.events.size() if probe.state != null else 0
	for _i in range(tick):
		probe.step_tick()
	var activity: Dictionary = EconomySpectatorMetrics.measure_activity(
		LayerRenderer.count_entities(before_render),
		LayerRenderer.count_entities(probe.get_render_data()),
		EconomySpectatorMetrics.chain_fingerprint(before_render),
		EconomySpectatorMetrics.chain_fingerprint(probe.get_render_data()),
		before_sprites,
		probe.get_sprite_states(),
		before_snap,
		probe.snapshot,
		events_before,
		probe.state.event_log.events.size() if probe.state != null else 0,
	)
	return int(activity.get("meaningful_count", 0))


func _resolve_output_dir() -> String:
	return EconomySpectatorMetrics.capture_review_dir()


func _build_contact_sheet_html() -> String:
	var manifest_name: String = "capture_manifest.json"
	var parts: PackedStringArray = PackedStringArray()
	parts.append("<html><head><title>RUN_05C.3 Economy Spectator</title>")
	parts.append("<style>body{font-family:sans-serif;background:#1a1a1a;color:#eee}table{border-collapse:collapse}td,th{border:1px solid #444;padding:8px;vertical-align:top}th{background:#333}a{color:#8cf}</style></head><body>")
	parts.append("<h1>RUN_05C.3 Economy Spectator Contact Sheet</h1>")
	parts.append("<p>Authoritative metrics in <a href='%s'>%s</a>; pixels are sanity checks only.</p>" % [manifest_name, manifest_name])
	parts.append("<table><tr><th>Tick</th><th>Frame</th><th>sprites</th><th>activity</th><th>meaningful</th><th>Δpx@0</th><th>families</th></tr>")
	for entry in _manifest:
		var tick: int = int(entry.get("tick", 0))
		var png: String = str(entry.get("png", ""))
		var active: Dictionary = entry.get("active_families", {})
		var family_keys: Array = active.keys()
		family_keys.sort()
		var family_text: String = ", ".join(family_keys)
		parts.append(
			"<tr><td>%d</td><td><img src='%s' width='480'/></td><td>%d</td><td>%d</td><td>%d</td><td>%d</td><td>%s</td></tr>"
			% [
				tick,
				png,
				int(entry.get("sprite_count", 0)),
				int(entry.get("activity_score", 0)),
				int(entry.get("meaningful_count", 0)),
				int(entry.get("pixel_delta_from_tick0", 0)),
				family_text,
			]
		)
	parts.append("</table></body></html>")
	return "\n".join(parts)


func _build_review_notes() -> String:
	var lines: PackedStringArray = PackedStringArray()
	lines.append("# RUN_05C.3 Economy Spectator Review Notes")
	lines.append("")
	lines.append("Generated by `run_economy_spectator_capture.gd -- --assert`.")
	lines.append("Visual polish only — production-chain demo, not bot proof.")
	lines.append("")
	lines.append("| Tick | sprites | activity | meaningful | Δpx@0 | families |")
	lines.append("|------|---------|----------|------------|-------|----------|")
	for entry in _manifest:
		var active: Dictionary = entry.get("active_families", {})
		var family_keys: Array = active.keys()
		family_keys.sort()
		lines.append(
			"| %d | %d | %d | %d | %d | %s |"
			% [
				int(entry.get("tick", 0)),
				int(entry.get("sprite_count", 0)),
				int(entry.get("activity_score", 0)),
				int(entry.get("meaningful_count", 0)),
				int(entry.get("pixel_delta_from_tick0", 0)),
				", ".join(family_keys),
			]
		)
	var delta_0_30: int = EconomySpectatorMetrics.image_pixel_delta(_images.get(0), _images.get(30))
	lines.append("")
	lines.append("Pixel delta tick0→tick30 (sanity): %d" % delta_0_30)
	lines.append("Manifest: capture_manifest.json")
	return "\n".join(lines)


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
