extends SceneTree

## Capture manual playability screenshots via main menu routes.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const Helper := preload("res://sim/tests/viewer/manual_playability_capture_helper.gd")
const ReviewUtil := preload("res://sim/tools/manual_playability_review_util.gd")
const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const MilitaryHelper := preload("res://sim/tests/viewer/military_mvp_test_helper.gd")

const HQ_Q: int = 6
const HQ_R: int = 6
const HUB_Q: int = 3
const HUB_R: int = 3

var _host: Node
var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 4
var _phase: String = "init"
var _step: int = 0
var _mode: String = "all"
var _entries: Array = []
var _all_ok: bool = true
var _modes_queue: Array = []
var _current_route: String = ""
var _shots_for_mode: Array = []


func _initialize() -> void:
	BootstrapScript.load_all()
	ReviewUtil.ensure_output_dir()
	_host = Node.new()
	root.add_child(_host)
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--mode="):
			_mode = arg.substr("--mode=".length())
	_modes_queue = _resolve_modes()
	print("CAPTURE METHOD: %s" % Helper.capture_method())


func _resolve_modes() -> Array:
	if _mode == "all":
		return [
			"settler_tutorial", "skirmish_tutorial", "hybrid_tutorial",
			"skirmish_mvp", "hybrid_mvp", "hybrid_3d",
		]
	return [_mode]


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"init":
			if _modes_queue.is_empty():
				_finish()
				return true
			_current_route = str(_modes_queue.pop_front())
			_launch_mode()
			_phase = "mode_start"
			_step = 0
			_wait = 6
		"mode_start":
			_run_mode_step()
		"done":
			_finish()
			return true
	return false


func _launch_mode() -> void:
	if is_instance_valid(_host):
		_host.queue_free()
	_host = Node.new()
	root.add_child(_host)
	_ctx = Helper.launch_via_menu_route(_host, _current_route)
	_scene = _ctx.get("scene")
	_shots_for_mode = _shot_plan(_current_route)


func _shot_plan(route: String) -> Array:
	match route:
		"settler_tutorial":
			return [
				{"file": "settler_tutorial_00_main_menu", "action": "menu_only"},
				{"file": "settler_tutorial_01_start_prompt", "action": "none"},
				{"file": "settler_tutorial_02_after_next_prompt", "action": "tutorial_next"},
				{"file": "settler_tutorial_03_after_back_prompt", "action": "tutorial_back"},
				{"file": "settler_tutorial_04_hq_selected", "action": "select_hq"},
				{"file": "settler_tutorial_05_unpaused_tick_advancing", "action": "play"},
				{"file": "settler_tutorial_06_road_mode", "action": "road"},
				{"file": "settler_tutorial_07_build_woodcutter_mode", "action": "woodcutter"},
			]
		"skirmish_tutorial":
			return [
				{"file": "skirmish_tutorial_00_main_menu", "action": "menu_only"},
				{"file": "skirmish_tutorial_01_start_prompt", "action": "none"},
				{"file": "skirmish_tutorial_02_after_next_prompt", "action": "tutorial_next"},
				{"file": "skirmish_tutorial_03_after_back_prompt", "action": "tutorial_back"},
				{"file": "skirmish_tutorial_04_command_hub_selected", "action": "select_hub"},
				{"file": "skirmish_tutorial_05_unpaused_tick_advancing", "action": "play"},
				{"file": "skirmish_tutorial_06_build_or_queue_panel", "action": "queue_panel"},
				{"file": "skirmish_tutorial_07_unit_selected", "action": "select_unit"},
				{"file": "skirmish_tutorial_08_attack_order", "action": "attack"},
			]
		"hybrid_tutorial":
			return [
				{"file": "hybrid_tutorial_00_main_menu", "action": "menu_only"},
				{"file": "hybrid_tutorial_01_start_prompt", "action": "none"},
				{"file": "hybrid_tutorial_02_after_next_prompt", "action": "tutorial_next"},
				{"file": "hybrid_tutorial_03_after_back_prompt", "action": "tutorial_back"},
				{"file": "hybrid_tutorial_04_hq_or_hybrid_base_selected", "action": "select_hq"},
				{"file": "hybrid_tutorial_05_unpaused_tick_advancing", "action": "play"},
				{"file": "hybrid_tutorial_06_economy_to_military_supply_visible", "action": "supply"},
				{"file": "hybrid_tutorial_07_military_posture_or_order_panel", "action": "attack"},
				{"file": "hybrid_tutorial_08_objective_progress", "action": "objective"},
			]
		"skirmish_mvp":
			return [
				{"file": "skirmish_mvp_00_launch", "action": "none"},
				{"file": "skirmish_mvp_01_unit_or_base_selected", "action": "select_hub"},
				{"file": "skirmish_mvp_02_unpaused", "action": "play"},
				{"file": "skirmish_mvp_03_move_or_attack_command", "action": "attack"},
				{"file": "skirmish_mvp_04_production_or_build_queue", "action": "queue_panel"},
				{"file": "skirmish_mvp_05_victory_or_progress", "action": "objective"},
			]
		"hybrid_mvp":
			return [
				{"file": "hybrid_mvp_00_launch", "action": "none"},
				{"file": "hybrid_mvp_01_economy_selected", "action": "select_hq"},
				{"file": "hybrid_mvp_02_military_selected", "action": "attack"},
				{"file": "hybrid_mvp_03_unpaused", "action": "play"},
				{"file": "hybrid_mvp_04_supply_chain_visible", "action": "supply"},
				{"file": "hybrid_mvp_05_attack_or_posture_visible", "action": "attack"},
				{"file": "hybrid_mvp_06_victory_or_progress", "action": "objective"},
			]
		"hybrid_3d":
			return [
				{"file": "hybrid_3d_00_launch", "action": "none"},
				{"file": "hybrid_3d_01_camera_moved", "action": "camera"},
				{"file": "hybrid_3d_02_bot_activity", "action": "play"},
				{"file": "hybrid_3d_03_logistics_to_combat_visible", "action": "supply"},
				{"file": "hybrid_3d_04_hud_readable", "action": "objective"},
			]
	return []


func _run_mode_step() -> void:
	if _step == 0 and _shots_for_mode.size() > 0 and str(_shots_for_mode[0].get("action", "")) == "menu_only":
		var menu_ctx: Dictionary = Helper.mount_main_menu(_host)
		_capture(menu_ctx.get("scene"), _shots_for_mode[0], _current_route)
		Helper.teardown(menu_ctx, _host)
		_host = Node.new()
		root.add_child(_host)
		_ctx = Helper.launch_via_menu_route(_host, _current_route)
		_scene = _ctx.get("scene")
		_step += 1
		_wait = 4
		return
	if _step >= _shots_for_mode.size():
		Helper.teardown(_ctx, _host)
		_host = Node.new()
		root.add_child(_host)
		_phase = "init"
		_wait = 2
		return
	var shot: Dictionary = _shots_for_mode[_step]
	if str(shot.get("action", "")) != "menu_only":
		_apply_action(str(shot.get("action", "")))
		_capture(_scene, shot, _current_route)
	_step += 1
	_wait = 4


func _apply_action(action: String) -> void:
	if _scene == null:
		return
	match action:
		"tutorial_next":
			if _scene.has_method("simulate_tutorial_next"):
				_scene.call("simulate_tutorial_next")
		"tutorial_back":
			if _scene.has_method("simulate_tutorial_back"):
				_scene.call("simulate_tutorial_back")
		"select_hq":
			if _scene.has_method("simulate_map_click_at_hex"):
				_scene.call("simulate_map_click_at_hex", HQ_Q, HQ_R)
			elif _scene.has_method("_on_map_click"):
				_scene.call("_on_map_click", 2, 2)
		"select_hub":
			MilitaryHelper.simulate_map_click_at_hex(_scene, HUB_Q, HUB_R)
		"play":
			SettlerHelper.invoke_play(_scene)
			if _scene.has_method("process_sim_frames"):
				_scene.call("process_sim_frames", 3)
			else:
				for _i in range(3):
					SettlerHelper.invoke_step(_scene)
		"road":
			if _scene.has_method("invoke_road_mode"):
				_scene.call("invoke_road_mode")
		"woodcutter":
			if _scene.has_method("invoke_build_mode"):
				_scene.call("invoke_build_mode", "woodcutter")
		"queue_panel":
			pass
		"select_unit":
			pass
		"attack":
			if _scene.has_method("invoke_attack_mode"):
				_scene.call("invoke_attack_mode")
		"camera", "supply", "objective":
			pass
	if _current_route == "hybrid_3d" and _scene != null and _scene.has_method("ensure_match_started"):
		_scene.call("ensure_match_started")
	Helper.settle(_ctx, 4)


func _capture(scene: Node, shot: Dictionary, route: String) -> void:
	var review: Dictionary = ReviewUtil.probe_scene(scene, str(shot.get("file", "")), route)
	review["screenshot_path"] = Helper.capture_png(_ctx if scene == _scene else {"viewport": scene.get_viewport(), "scene": scene}, str(shot.get("file", "")), review)
	if not ReviewUtil.evaluate_entry(review):
		_all_ok = false
	_entries.append(review)


func _finish() -> void:
	ReviewUtil.write_review_files(_entries)
	print("CAPTURE ENTRIES: %d" % _entries.size())
	if _all_ok:
		print("MANUAL PLAYABILITY CAPTURE PASS")
		_quit(0)
	else:
		print("MANUAL PLAYABILITY CAPTURE FAIL")
		_quit(1)


func _quit(code: int) -> void:
	call_deferred("quit", code)
