extends SceneTree

## Flow verification — hybrid 3D spectator advances toward combat or timeout.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

var _ctx: Dictionary = {}
var _scene: Node
var _wait: int = 8
var _started: bool = false
var _done: bool = false
var _result: Dictionary = {}


func _initialize() -> void:
	BootstrapScript.load_all()
	_ctx = ViewerTestHarness.mount(root, "hybrid_3d_spectator")
	_scene = _ctx.get("scene")
	_force_layout()


func _process(_delta: float) -> bool:
	if _wait > 0:
		_wait -= 1
		return false
	if not _started:
		_started = true
		if _scene != null and _scene.has_method("advance_until_combat_for_tests"):
			_result = _scene.call("advance_until_combat_for_tests", 400)
		_finish()
		return true
	return false


func _force_layout() -> void:
	var scene: Control = _scene as Control
	if scene != null:
		scene.size = ViewerTestHarness.SIZE
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES):
		ViewerTestHarness.tick_frames(_ctx)


func _finish() -> void:
	if _done:
		return
	_done = true
	var saw_units: bool = bool(_result.get("saw_units", false))
	var saw_combat: bool = bool(_result.get("saw_combat", false))
	var finished: bool = bool(_result.get("finished", false))
	if saw_units or saw_combat or finished:
		print("HYBRID_3D_SPECTATOR_FLOW PASS steps=%d units=%s combat=%s finished=%s" % [
			int(_result.get("steps", 0)), saw_units, saw_combat, finished,
		])
		quit(0)
		return
	var progress: bool = bool(_result.get("progress", false))
	if progress:
		print("HYBRID_3D_SPECTATOR_FLOW PASS progress steps=%d result=%s" % [
			int(_result.get("steps", 0)), str(_result),
		])
		quit(0)
		return
	print("HYBRID_3D_SPECTATOR_FLOW FAIL result=%s" % str(_result))
	quit(1)
