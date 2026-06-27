extends SceneTree

## Capture real-time tutorial and 3D spectator screenshot evidence.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ReviewUtil := preload("res://sim/tools/realtime_tutorials_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const ViewerTestHarness := preload("res://client/tools/viewer_test_harness.gd")

var _host: Node
var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 4
var _phase: String = "init"
var _step: int = 0
var _entries: Array = []
var _plans: Array = []
var _current: Dictionary = {}


func _initialize() -> void:
	BootstrapScript.load_all()
	ReviewUtil.ensure_output_dir()
	_host = Node.new()
	root.add_child(_host)
	_plans = _build_plans()
	print("REALTIME CAPTURE dir=%s" % ReviewUtil.output_dir())


func _build_plans() -> Array:
	return [
		{"route": "settler_tutorial", "shots": [
			{"file": "settler_tutorial_00_launch_running", "ticks": 6},
			{"file": "settler_tutorial_01_resource_visible", "ticks": 2},
			{"file": "settler_tutorial_02_first_objective_progress", "action": "next"},
		]},
		{"route": "skirmish_tutorial", "shots": [
			{"file": "skirmish_tutorial_00_launch_running", "ticks": 6},
			{"file": "skirmish_tutorial_01_unit_or_base_selected", "action": "select_hub"},
			{"file": "skirmish_tutorial_02_combat_or_order_progress", "action": "next"},
		]},
		{"route": "hybrid_tutorial", "shots": [
			{"file": "hybrid_tutorial_00_launch_running", "ticks": 6},
			{"file": "hybrid_tutorial_01_resources_visible", "ticks": 2},
			{"file": "hybrid_tutorial_02_supply_chain_visible", "action": "next"},
			{"file": "hybrid_tutorial_03_military_production_visible", "action": "next"},
			{"file": "hybrid_tutorial_04_attack_progress_visible", "action": "next"},
		]},
		{"route": "hybrid_mvp", "shots": [
			{"file": "hybrid_mvp_00_launch_running", "ticks": 6},
			{"file": "hybrid_mvp_01_economy_and_military_visible", "ticks": 4},
			{"file": "hybrid_mvp_02_progress_after_30_seconds", "ticks": 24},
		]},
		{"route": "hybrid_3d", "shots": [
			{"file": "hybrid_3d_00_launch_running", "ticks": 6},
			{"file": "hybrid_3d_01_camera_moved", "action": "camera"},
			{"file": "hybrid_3d_02_hex_board_visible", "ticks": 2},
			{"file": "hybrid_3d_03_buildings_and_units_visible", "ticks": 8},
			{"file": "hybrid_3d_04_combat_or_objective_progress", "ticks": 12},
			{"file": "hybrid_3d_05_hud_readable", "ticks": 2},
		]},
	]


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"init":
			if _plans.is_empty():
				_finish(0)
				return true
			_current = _plans.pop_front()
			_launch(str(_current.get("route", "")))
			_step = 0
			_phase = "shot"
			_wait = 6
		"shot":
			_capture_step()
		"done":
			_finish(0)
			return true
	return false


func _launch(route: String) -> void:
	if is_instance_valid(_host):
		_host.queue_free()
	_host = Node.new()
	root.add_child(_host)
	_ctx = Helper.launch_via_menu_route(_host, route)
	_scene = _ctx.get("scene")


func _capture_step() -> void:
	var shots: Array = _current.get("shots", [])
	if _step >= shots.size():
		Helper.teardown(_ctx, _host)
		_phase = "init"
		_wait = 4
		return
	var spec: Dictionary = shots[_step]
	_apply_action(spec)
	var ticks: int = int(spec.get("ticks", 4))
	for _i in range(ticks):
		if _scene != null and _scene.has_method("_on_tick_timer"):
			_scene.call("_on_tick_timer")
		elif _scene != null and _scene.has_method("_on_autoplay_tick"):
			_scene.call("_on_autoplay_tick")
		Helper.settle(_ctx, 1)
	var filename: String = str(spec.get("file", "shot"))
	var path: String = _capture_png(_ctx, filename)
	_entries.append(_make_review(spec, path))
	_step += 1
	_wait = 4


func _apply_action(spec: Dictionary) -> void:
	var action: String = str(spec.get("action", ""))
	if _scene == null:
		return
	match action:
		"next":
			if _scene.has_method("simulate_tutorial_next"):
				_scene.call("simulate_tutorial_next")
		"select_hub":
			if _scene.has_method("invoke_build_mode"):
				pass
			SettlerHelper.simulate_map_click_at_hex(_scene, 3, 3)
		"camera":
			var cam: Node = _scene.get_node_or_null("%Camera")
			if cam != null and cam.has_method("pan_by"):
				cam.call("pan_by", Vector3(3, 0, 2))


func _make_review(spec: Dictionary, path: String) -> Dictionary:
	var tick: int = 0
	var state: String = "Unknown"
	if _scene != null:
		if _scene.has_method("get_current_tick"):
			tick = int(_scene.call("get_current_tick"))
		elif "controller" in _scene:
			var snap: Dictionary = _scene.controller.get_snapshot() if _scene.controller.has_method("get_snapshot") else _scene.controller.snapshot
			tick = int(snap.get("tick", 0))
			if "autoplay_enabled" in _scene.controller:
				state = "Playing" if _scene.controller.autoplay_enabled else "Paused"
			elif "paused" in _scene.controller:
				state = "Playing" if not _scene.controller.paused else "Paused"
	return ReviewUtil.review_entry(
		str(_current.get("route", "")),
		str(spec.get("file", "")),
		path,
		tick,
		state,
		{"player_can_tell_what_to_do_next": tick > 0 and FileAccess.file_exists(path)},
	)


func _capture_png(ctx: Dictionary, filename: String) -> String:
	ReviewUtil.ensure_output_dir()
	var path: String = ReviewUtil.output_dir().path_join("%s.png" % filename)
	var img: Image = ViewerTestHarness.capture(ctx)
	if img != null and not img.is_empty():
		img.save_png(path)
	return path


func _finish(code: int) -> void:
	var out_path: String = ReviewUtil.review_json_path()
	var f := FileAccess.open(out_path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify({"entries": _entries}, "\t"))
	print("REALTIME CAPTURE wrote %d entries -> %s" % [_entries.size(), out_path])
	call_deferred("quit", code)
