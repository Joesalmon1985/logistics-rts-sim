extends SceneTree

## RUN_02.8 bot-vs-bot economy visual review bundle (non-headless SubViewport capture).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const EconomyBvbReview := preload("res://client/demo/economy_bvb_review.gd")

const OUTPUT_SUBDIR: String = "training_runs/visual_review/run_02_8_economy_bvb"

var _ctx: Dictionary = {}
var _panels: Array = []
var _panel_index: int = 0
var _phase: String = "wait_frames"
var _frames_left: int = 0
var _output_dir: String = ""
var _manifest: Array = []


func _initialize() -> void:
	BootstrapScript.load_all()
	_output_dir = _resolve_output_dir()
	DirAccess.make_dir_recursive_absolute(_output_dir)
	_panels = _panel_definitions()
	call_deferred("_begin")


func _begin() -> void:
	_ctx = ViewerTestHarness.mount(root, "economy")
	if not bool(_ctx.get("ok", false)):
		print("VISUAL REVIEW FAIL: mount — %s" % str(_ctx.get("reason", "?")))
		quit(1)
		return
	_panel_index = 0
	_start_panel()


func _process(_delta: float) -> bool:
	if _ctx.is_empty():
		return false
	if _phase == "wait_frames":
		if _frames_left > 0:
			_frames_left -= 1
			return false
		_capture_current_panel()
		_panel_index += 1
		if _panel_index >= _panels.size():
			_finish()
			return false
		_start_panel()
		return false
	return false


func _start_panel() -> void:
	var panel: Dictionary = _panels[_panel_index]
	var builder: Callable = panel.get("builder", Callable())
	if not builder.is_valid():
		print("VISUAL REVIEW FAIL: invalid builder for %s" % str(panel.get("id", "?")))
		quit(1)
		return
	var built: Variant = builder.call()
	var controller: ViewerController = _ctx.get("controller")
	if typeof(built) == TYPE_DICTIONARY:
		controller.load_snapshot_view(built, "p1")
	else:
		controller.load_demo_state(built as GameState, "p1")
	if str(panel.get("overlay", "")) == "logistics":
		controller.set_layer_visible("logistics", true)
		controller.set_layer_visible("roads", true)
		controller.set_layer_visible("resources", true)
		controller.set_layer_visible("forest_density", true)
	else:
		controller.set_layer_visible("logistics", true)
	var extra_ticks: int = int(panel.get("extra_ticks", 0))
	if extra_ticks > 0:
		controller.step_ticks(extra_ticks)
	var scene: ViewerRoot = _ctx.get("scene")
	if scene != null:
		scene._refresh_map_layers()
	var map: MapViewport = _ctx.get("map")
	if map != null and not controller.get_render_data().is_empty():
		map.fit_to_content()
		map.queue_redraw()
	_phase = "wait_frames"
	_frames_left = int(panel.get("wait_frames", 8))


func _capture_current_panel() -> void:
	var panel: Dictionary = _panels[_panel_index]
	var controller: ViewerController = _ctx.get("controller")
	var render: Dictionary = controller.get_render_data()
	var counts: Dictionary = LayerRenderer.count_entities(render)
	var snapshot: Dictionary = controller.snapshot
	var png_name: String = "%s.png" % str(panel.get("id", "panel"))
	var png_path: String = _output_dir.path_join(png_name)
	var img: Image = ViewerTestHarness.capture(_ctx)
	var saved: bool = img != null and not img.is_empty() and img.save_png(png_path) == OK
	var scores: Dictionary = snapshot.get("diagnostics", {}).get("economy_scores", {})
	_manifest.append({
		"id": panel.get("id", ""),
		"title": panel.get("title", ""),
		"notes": panel.get("notes", ""),
		"look_for": panel.get("look_for", []),
		"png": png_name,
		"png_saved": saved,
		"tick": int(snapshot.get("tick", 0)),
		"economy_scores": scores,
		"render_counts": counts,
	})
	print("Captured %s -> %s (saved=%s) scores=%s" % [panel.id, png_path, str(saved), str(scores)])


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	var manifest_path: String = _output_dir.path_join("capture_manifest.json")
	var html_path: String = _output_dir.path_join("contact_sheet.html")
	_write_json(manifest_path, {"panels": _manifest, "output_dir": _output_dir})
	_write_text(html_path, _build_contact_sheet_html())
	print("VISUAL REVIEW PASS: %d panels" % _manifest.size())
	print("Output dir: %s" % _output_dir)
	print("Contact sheet: %s" % html_path)
	print("EXIT: 0 (pass)")
	quit(0)


func _panel_definitions() -> Array:
	return [
		{
			"id": "01_start",
			"title": "Start",
			"builder": Callable(EconomyBvbReview, "load_checkpoint").bind(0),
			"notes": "Dual-player spawns after map prep, before bot expansion.",
			"look_for": ["p1/p2 HQ", "ownership overlay"],
		},
		{
			"id": "02_early",
			"title": "Early expansion",
			"builder": Callable(EconomyBvbReview, "load_checkpoint").bind(200),
			"notes": "Initial roads, worker assignment, first production buildings.",
			"look_for": ["player-colored roads", "production buildings"],
		},
		{
			"id": "03_mid",
			"title": "Mid match",
			"builder": Callable(EconomyBvbReview, "load_checkpoint").bind(800),
			"notes": "Logistics and deliveries across both economies.",
			"look_for": ["carriers", "ware routes", "dual scoreboard"],
		},
		{
			"id": "04_late",
			"title": "Late match",
			"builder": Callable(EconomyBvbReview, "load_checkpoint").bind(1800),
			"notes": "Mature networks and rising scores.",
			"look_for": ["dual scoreboard", "ownership wash"],
		},
		{
			"id": "05_final",
			"title": "Final",
			"builder": Callable(EconomyBvbReview, "load_final_checkpoint"),
			"notes": "End-state after long visual profile run.",
			"look_for": ["both economies expanded", "final scores"],
		},
		{
			"id": "06_logistics",
			"title": "Logistics overlay",
			"builder": Callable(EconomyBvbReview, "load_final_checkpoint"),
			"notes": "Final checkpoint with logistics layers toggled on.",
			"look_for": ["roads", "carriers", "ware flow"],
			"overlay": "logistics",
		},
	]


func _resolve_output_dir() -> String:
	var project_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	return project_root.path_join(OUTPUT_SUBDIR)


func _build_contact_sheet_html() -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>RUN_02.8 Bot-vs-Bot Economy</title>")
	parts.append("<style>body{font-family:sans-serif;max-width:1200px;margin:1rem auto;background:#111;color:#eee}")
	parts.append("h1,h2{color:#f2a826}.panel{margin:2rem 0;padding:1rem;border:1px solid #444}")
	parts.append("img{max-width:100%}</style></head><body>")
	parts.append("<h1>RUN_02.8 — Bot-vs-Bot Economy Visual Review</h1>")
	for entry in _manifest:
		parts.append("<div class=\"panel\"><h2>%s — %s</h2>" % [entry.id, entry.title])
		if bool(entry.get("png_saved", false)):
			parts.append("<img src=\"%s\">" % entry.png)
		parts.append("<p>Tick %d | scores %s</p><p>%s</p></div>" % [entry.tick, str(entry.economy_scores), entry.notes])
	parts.append("</body></html>")
	return "".join(parts)


func _write_json(path: String, data: Variant) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(data, "\t"))


func _write_text(path: String, text: String) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(text)
