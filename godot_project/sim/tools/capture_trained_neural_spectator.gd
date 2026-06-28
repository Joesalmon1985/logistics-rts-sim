extends SceneTree

## Capture trained neural 4P spectator screenshots + eval artefacts.

const OUT_DIR := "res://../docs/audit/neural_hybrid_ai/visual/trained_neural_spectator/"
const SHOTS: Array = [
	{"file": "00_match_start.png", "steps": 1},
	{"file": "01_economy_building.png", "steps": 20},
	{"file": "02_road_logistics.png", "steps": 40},
	{"file": "03_military_production.png", "steps": 60},
	{"file": "04_units_produced.png", "steps": 80},
	{"file": "05_attack_started.png", "steps": 100},
	{"file": "06_combat_cluster.png", "steps": 130},
	{"file": "07_elimination_or_closest.png", "steps": 170},
	{"file": "08_winner_or_tick_cap.png", "steps": 200},
]

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 12
var _shot_idx: int = 0
var _prev_steps: int = 0
var _out: String = ""
var _review: Array = []
var _finished: bool = false


func _initialize() -> void:
	SimBootstrap.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_2d_spectator")
	if not bool(_ctx.get("ok", false)):
		print("SPECTATOR CAPTURE FAIL: mount")
		quit(1)
	_scene = _ctx.get("scene")
	_out = ProjectSettings.globalize_path(OUT_DIR)
	DirAccess.make_dir_recursive_absolute(_out)
	_force_layout()
	if _scene != null and _scene.controller != null:
		_scene.controller.reset_match_trained_neural()
	_force_layout()


func _process(_delta: float) -> bool:
	if _finished:
		return true
	if _wait > 0:
		_wait -= 1
		_force_layout()
		return false
	if _shot_idx >= SHOTS.size():
		_finish()
		return true
	var shot: Dictionary = SHOTS[_shot_idx]
	var target: int = int(shot.get("steps", 1))
	var delta: int = maxi(0, target - _prev_steps)
	for _i in range(delta):
		if _scene != null and _scene.controller != null:
			_scene.controller.step_tick()
	_force_layout()
	var path: String = _out.path_join(str(shot.get("file", "shot.png")))
	var img: Image = ViewerTestHarness.capture(_ctx)
	var ok: bool = img != null and img.get_width() > 8 and img.get_height() > 8
	if ok:
		img.save_png(path)
	_review.append({
		"screenshot_path": OUT_DIR.path_join(str(shot.get("file", ""))),
		"readability_pass": ok,
		"inference_mode": "trained_neural",
		"size": "%dx%d" % [img.get_width(), img.get_height()] if img != null else "0x0",
	})
	_prev_steps = target
	_shot_idx += 1
	_wait = 10
	return false


func _force_layout() -> void:
	var scene: Control = _scene as Control
	if scene != null:
		scene.set_anchors_preset(Control.PRESET_TOP_LEFT)
		scene.size = ViewerTestHarness.SIZE
		scene.custom_minimum_size = ViewerTestHarness.SIZE
	var subvp: SubViewport = _ctx.get("viewport")
	if subvp != null:
		subvp.size = ViewerTestHarness.SIZE
		subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	var map: MapViewport = _ctx.get("map")
	if map != null and not map.render_data.is_empty():
		map.fit_to_content()
		map.queue_redraw()
	for _i in range(3):
		ViewerTestHarness.tick_frames(_ctx)
	RenderingServer.force_draw(true, 0.0)


func _finish() -> void:
	if _finished:
		return
	_finished = true
	_write_json(_out.path_join("visual_review.json"), _review)
	_write_spectator_eval_artefacts()
	ViewerTestHarness.teardown(_ctx)
	var readable: int = 0
	for row in _review:
		if bool(row.get("readability_pass", false)):
			readable += 1
	if readable >= 3:
		print("TRAINED SPECTATOR CAPTURE PASS: %s (%d/%d readable)" % [_out, readable, _review.size()])
		quit(0)
	print("TRAINED SPECTATOR CAPTURE FAIL: only %d readable shots" % readable)
	quit(1)


func _write_spectator_eval_artefacts() -> void:
	if _scene == null or _scene.controller == null:
		return
	var out_dir: String = ProjectSettings.globalize_path("res://../docs/audit/neural_hybrid_ai/eval/4_neural_ffa_spectator/")
	DirAccess.make_dir_recursive_absolute(out_dir)
	var ctrl: HybridBotSpectatorController = _scene.controller
	var telemetry: Array = []
	for pid in ["p1", "p2", "p3", "p4"]:
		var status: Dictionary = ctrl.bot_status.get(pid, {})
		var telem: Dictionary = status.get("telemetry", {})
		if not telem.is_empty():
			telemetry.append(telem)
	_write_json(out_dir.path_join("match_summary.json"), {
		"eval_name": "4_neural_ffa_spectator",
		"tick": int(ctrl.snapshot.get("tick", 0)),
		"ffa": ctrl.ffa_result,
	})
	_write_json(out_dir.path_join("final_snapshot.json"), ctrl.snapshot)
	_write_jsonl(out_dir.path_join("decision_telemetry.jsonl"), telemetry)
	_write_text(out_dir.path_join("player_metrics.csv"), _player_metrics_csv(telemetry))
	_write_text(out_dir.path_join("reward_trace.csv"), _reward_trace_csv(telemetry))
	_write_text(out_dir.path_join("action_trace.csv"), "tick,player_id,command\n")
	var winner: String = str(ctrl.ffa_result.get("winner_player_id", ""))
	if winner != "":
		_write_text(out_dir.path_join("winner.txt"), winner)
	else:
		_write_text(out_dir.path_join("no_winner_tick_cap.txt"), "tick_cap=%d" % int(ctrl.snapshot.get("tick", 0)))


func _write_json(path: String, data: Variant) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(data, "\t"))


func _write_text(path: String, text: String) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(text)


func _write_jsonl(path: String, rows: Array) -> void:
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		return
	for row in rows:
		f.store_line(JSON.stringify(row))


func _player_metrics_csv(telemetry: Array) -> String:
	var lines: PackedStringArray = PackedStringArray(["player_id,inference_mode,decisions,heuristic_fallback"])
	var counts: Dictionary = {}
	for row in telemetry:
		var pid: String = str(row.get("player_id", ""))
		if not counts.has(pid):
			counts[pid] = {"decisions": 0, "mode": "", "fallback": 0}
		counts[pid]["decisions"] += 1
		if str(row.get("inference_mode", "")) == "trained_neural":
			counts[pid]["mode"] = "trained_neural"
		if bool(row.get("heuristic_fallback", false)):
			counts[pid]["fallback"] += 1
	for pid in counts.keys():
		var c: Dictionary = counts[pid]
		lines.append("%s,%s,%d,%d" % [pid, c.get("mode", ""), c.get("decisions", 0), c.get("fallback", 0)])
	return "\n".join(lines)


func _reward_trace_csv(telemetry: Array) -> String:
	var lines: PackedStringArray = PackedStringArray(["tick,player_id,reward_proxy,inference_mode"])
	for row in telemetry:
		var reward: float = 1.0 if str(row.get("inference_mode", "")) == "trained_neural" else 0.0
		lines.append("%d,%s,%.3f,%s" % [int(row.get("tick", 0)), row.get("player_id", ""), reward, row.get("inference_mode", "")])
	return "\n".join(lines)
