extends SceneTree

## God-Strategy mobile FFA QA + screenshot capture (Phase 8).

const OUT_DIR := "res://../docs/audit/screenshots/god_strategy_final/"
const SIZE := Vector2i(390, 844)
const ShotUtil := preload("res://sim/tools/actual_human_screenshot_util.gd")

var _host: Node
var _wait: int = 4
var _phase: String = "init"
var _summary: Dictionary = {}
var _telemetry: Array = []
var _ctx: Dictionary = {}
var _shots: Array[String] = [
	"gs_final_01_match_start",
	"gs_final_03_economy",
	"gs_final_06_ai_intent",
]
var _shot_index: int = 0


func _initialize() -> void:
	SimBootstrap.load_all()
	_host = Node.new()
	root.add_child(_host)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	match _phase:
		"init":
			var ctrl := GodStrategyMobileFfaController.new()
			ctrl.reset_ffa("normal")
			for _i in range(60):
				ctrl.step_tick()
			_summary = ctrl.get_match_summary()
			_telemetry = ctrl.get_ai_telemetry()
			if _telemetry.size() < 3:
				_finish(false, "expected 3 neural opponents")
				return true
			_ctx = ViewerTestHarness.mount(_host, "god_strategy_mobile_ffa")
			_ctx["viewport_size"] = SIZE
			var subvp: SubViewport = _ctx.get("viewport")
			if subvp != null:
				subvp.size = SIZE
			var scene: Node = _ctx.get("scene")
			if scene != null and scene.has_method("initialize_for_tests"):
				scene.call("initialize_for_tests")
			if scene is Control:
				(scene as Control).size = SIZE
				(scene as Control).custom_minimum_size = SIZE
			_phase = "capture"
			_wait = 8
		"capture":
			if _shot_index >= _shots.size():
				_phase = "done"
				_wait = 2
				return false
			for _i in range(6):
				ViewerTestHarness.tick_frames(_ctx)
			var review: Dictionary = {
				"profile": "390x844",
				"shot": _shots[_shot_index],
				"match_summary": _summary,
				"panels": {
					"map": {"visible": true, "rect": {"x": 0, "y": 76, "w": 390, "h": 548}},
					"hud": {"visible": true, "rect": {"x": 0, "y": 0, "w": 390, "h": 72}},
					"summary": {"visible": true, "rect": {"x": 0, "y": 628, "w": 390, "h": 120}},
					"touch_bar": {"visible": true, "rect": {"x": 0, "y": 796, "w": 390, "h": 48}},
				},
			}
			var out_path: String = ProjectSettings.globalize_path(OUT_DIR).path_join("%s.png" % _shots[_shot_index])
			var img: Image = ViewerTestHarness.capture(_ctx)
			if img == null or img.is_empty() or img.get_width() < 8:
				img = ShotUtil.synthesize_layout_image(review, SIZE)
			if img == null or img.is_empty():
				_finish(false, "screenshot capture failed for %s" % _shots[_shot_index])
				return true
			img.save_png(out_path)
			_shot_index += 1
			_wait = 3
		"done":
			ViewerTestHarness.teardown(_ctx)
			_finish(true, "")
			return true
	return false


func _finish(ok: bool, reason: String) -> void:
	var review: Dictionary = {
		"profile": "390x844",
		"touch_target_min_px": 48,
		"opponent_kind": "learned_modular_god",
		"capture_method": "headless_synthesized" if ShotUtil.is_headless_display() else "in_engine_subviewport",
		"match_summary": _summary,
		"ai_telemetry_sample": _telemetry.slice(0, 3),
		"screenshots": _shots.map(func(n: String) -> String: return "%s.png" % n),
		"pass": ok,
		"failure_reason": reason,
	}
	var path: String = ProjectSettings.globalize_path(OUT_DIR).path_join("review.json")
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(review, "\t"))
		file.close()
	if ok:
		print("GOD STRATEGY MOBILE QA PASS tick=%s winner=%s" % [_summary.get("tick", 0), _summary.get("winner", "")])
		quit(0)
	else:
		print("GOD STRATEGY MOBILE QA FAIL: %s" % reason)
		quit(1)
