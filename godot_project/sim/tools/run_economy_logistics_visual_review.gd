extends SceneTree

## RUN_02.7 deterministic economy logistics visual review bundle (non-headless SubViewport capture).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const EconomyLogisticsReview := preload("res://client/demo/economy_logistics_review.gd")

const OUTPUT_SUBDIR: String = "training_runs/visual_review/run_02_7_economy_logistics"

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
	var state: GameState = builder.call()
	var controller: ViewerController = _ctx.get("controller")
	controller.load_demo_state(state, "p1")
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
	var entry := {
		"id": panel.get("id", ""),
		"title": panel.get("title", ""),
		"notes": panel.get("notes", ""),
		"look_for": panel.get("look_for", []),
		"render_only": panel.get("render_only", []),
		"png": png_name,
		"png_saved": saved,
		"tick": int(snapshot.get("tick", 0)),
		"render_counts": counts,
		"diagnostics": snapshot.get("diagnostics", {}),
		"snapshot_markers": {
			"road_nodes": snapshot.get("road_nodes", []).size(),
			"delivery_jobs": snapshot.get("delivery_jobs", []).size(),
			"blocked_job_markers": snapshot.get("blocked_job_markers", []).size(),
			"carrier_markers": snapshot.get("carrier_markers", []).size(),
		},
	}
	_manifest.append(entry)
	print("Captured %s -> %s (saved=%s)" % [entry.id, png_path, str(saved)])


func _finish() -> void:
	ViewerTestHarness.teardown(_ctx)
	var manifest_path: String = _output_dir.path_join("capture_manifest.json")
	var html_path: String = _output_dir.path_join("contact_sheet.html")
	var readme_path: String = _output_dir.path_join("README.txt")
	_write_json(manifest_path, {"panels": _manifest, "output_dir": _output_dir})
	_write_text(html_path, _build_contact_sheet_html())
	_write_text(readme_path, _build_readme())
	print("VISUAL REVIEW PASS: %d panels" % _manifest.size())
	print("Output dir: %s" % _output_dir)
	print("Contact sheet: %s" % html_path)
	print("Manifest: %s" % manifest_path)
	print("EXIT: 0 (pass)")
	quit(0)


func _panel_definitions() -> Array:
	return [
		{
			"id": "01_production_chain_t0",
			"title": "Production chain — start",
			"builder": Callable(EconomyLogisticsReview, "build_production_chain_t0"),
			"extra_ticks": 0,
			"notes": "HQ (multi-port), woodcutter on forest, sawmill, yellow roads, cyan road-node dots, orange port diamonds.",
			"look_for": ["roads", "road_node_markers", "building_ports", "carriers (idle at HQ)"],
			"render_only": [],
		},
		{
			"id": "02_production_chain_t30",
			"title": "Production chain — after 30 ticks",
			"builder": Callable(EconomyLogisticsReview, "build_production_chain_after").bind(30),
			"extra_ticks": 0,
			"notes": "Expect dashed amber ware routes, carrier movement, store chips T:/P:, lower forest D:n.",
			"look_for": ["ware_routes", "delivery_jobs lines", "carriers moving", "production_activity"],
			"render_only": [],
		},
		{
			"id": "03_build_tutorial_site",
			"title": "Build tutorial — storehouse site placed",
			"builder": Callable(EconomyLogisticsReview, "build_build_tutorial_with_site"),
			"extra_ticks": 0,
			"notes": "Construction site hex with delivered/required ware labels; checklist in side panel.",
			"look_for": ["construction_sites", "roads toward site"],
			"render_only": [],
		},
		{
			"id": "04_smoke006_mid",
			"title": "ECON-SMOKE-006 — construction mid-flight",
			"builder": Callable(EconomyLogisticsReview, "build_smoke_006_at").bind(80),
			"extra_ticks": 0,
			"notes": "HQ→road network→storehouse site; carriers delivering planks; builder_work not started until materials complete.",
			"look_for": ["construction_sites progress", "carriers", "ware_routes"],
			"render_only": ["builder_work_remaining only in inspector/snapshot until materials done"],
		},
		{
			"id": "05_smoke006_complete",
			"title": "ECON-SMOKE-006 — storehouse complete",
			"builder": Callable(EconomyLogisticsReview, "build_smoke_006_at").bind(500),
			"extra_ticks": 0,
			"notes": "Completed storehouse building; site removed; logistics network intact.",
			"look_for": ["storehouse building", "roads", "ports on HQ"],
			"render_only": [],
		},
		{
			"id": "06_blocked_no_route",
			"title": "Blocked job — no route",
			"builder": Callable(EconomyLogisticsReview, "build_blocked_no_route"),
			"extra_ticks": 0,
			"notes": "Isolated sawmill with no road link; blocked delivery job may show red line if endpoints resolve.",
			"look_for": ["blocked delivery_jobs label with (no_route) if drawn"],
			"render_only": ["block_reason in snapshot diagnostics.delivery and render_data delivery_jobs"],
		},
		{
			"id": "07_no_carrier",
			"title": "Blocked job — no carrier",
			"builder": Callable(EconomyLogisticsReview, "build_no_carrier_wait"),
			"extra_ticks": 0,
			"notes": "Road connected but all carriers removed; job waits for carrier.",
			"look_for": ["delivery job waiting", "roads present", "no moving carriers"],
			"render_only": ["block_reason no_carrier in diagnostics"],
		},
		{
			"id": "08_tool_shortage",
			"title": "Tool shortage — woodcutter stalled",
			"builder": Callable(EconomyLogisticsReview, "build_tool_shortage"),
			"extra_ticks": 0,
			"notes": "Woodcutter without tools in HQ; paused_reason tool_missing in snapshot.",
			"look_for": ["woodcutter with no worker badge / stalled production"],
			"render_only": ["diagnostics.shortages.tools > 0 in summary panel if wired"],
		},
		{
			"id": "09_brick_kiln",
			"title": "Config-only brick chain",
			"builder": Callable(EconomyLogisticsReview, "build_brick_kiln_active"),
			"extra_ticks": 0,
			"notes": "brick_kiln from config only; bricks in store after ticks; proves config-driven chain.",
			"look_for": ["brick_kiln building", "store chips if bricks produced"],
			"render_only": ["bricks count in snapshot/building_stores"],
		},
	]


func _resolve_output_dir() -> String:
	var project_root: String = ProjectSettings.globalize_path("res://").path_join("..")
	return project_root.path_join(OUTPUT_SUBDIR)


func _build_contact_sheet_html() -> String:
	var parts: PackedStringArray = PackedStringArray()
	parts.append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">")
	parts.append("<title>RUN_02.7 Economy Logistics Visual Review</title>")
	parts.append("<style>body{font-family:sans-serif;max-width:1200px;margin:1rem auto;background:#111;color:#eee}")
	parts.append("h1,h2{color:#f2a826}.panel{margin:2rem 0;padding:1rem;border:1px solid #444;border-radius:8px}")
	parts.append("img{max-width:100%;border:1px solid #666}.meta{color:#aaa;font-size:0.9rem}ul{color:#ccc}</style></head><body>")
	parts.append("<h1>RUN_02.7 — Economy Logistics Visual Review</h1>")
	parts.append("<p>Authoritative snapshot/export only — no viewer-fabricated logistics state.</p>")
	parts.append("<p>Generated by <code>run_economy_logistics_visual_review.gd</code></p>")
	for entry in _manifest:
		parts.append("<div class=\"panel\">")
		parts.append("<h2>%s — %s</h2>" % [entry.id, entry.title])
		if bool(entry.get("png_saved", false)):
			parts.append("<img src=\"%s\" alt=\"%s\">" % [entry.png, entry.title])
		else:
			parts.append("<p><em>Screenshot save failed</em></p>")
		parts.append("<p class=\"meta\">%s</p>" % entry.notes)
		parts.append("<p class=\"meta\">Tick: %d | render_counts: %s</p>" % [entry.tick, str(entry.render_counts)])
		parts.append("<p><strong>Look for on map:</strong></p><ul>")
		for item in entry.look_for:
			parts.append("<li>%s</li>" % item)
		parts.append("</ul>")
		if not entry.render_only.is_empty():
			parts.append("<p><strong>Snapshot / panel only (may not draw on map):</strong></p><ul>")
			for item in entry.render_only:
				parts.append("<li>%s</li>" % item)
			parts.append("</ul>")
		parts.append("</div>")
	parts.append("</body></html>")
	return "".join(parts)


func _build_readme() -> String:
	return """RUN_02.7 Economy Logistics Visual Review Bundle
================================================

Regenerate (Linux, non-headless — requires display):

  export GODOT=/path/to/Godot_v4.5.2
  export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
  $GODOT --path godot_project --script res://sim/tools/run_economy_logistics_visual_review.gd

Interactive windowed review:

  $GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn

Then: Load Production Demo / Load Build Tutorial, toggle Logistics layer, Step Until Event.

Headless contract check first:

  $GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd

Outputs in this folder (gitignored via training_runs/):
  - contact_sheet.html — open in browser
  - capture_manifest.json — render counts + diagnostics per panel
  - 01_*.png … 09_*.png — SubViewport captures at 1920x1080

All visuals come from SnapshotExporter → LayerRenderer → MapViewport only.
"""


func _write_text(path: String, text: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("Cannot write %s" % path)
		return
	file.store_string(text)
	file.close()


func _write_json(path: String, data: Variant) -> void:
	_write_text(path, JSON.stringify(data, "\t"))
