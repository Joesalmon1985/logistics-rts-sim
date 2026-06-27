extends SceneTree

## Headless smoke — hybrid bot-vs-bot 3D spectator scenario advances without hang.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const SCENARIO := "scenarios/hybrid/hybrid_bot_vs_bot_3d_spectator.json"
const MAX_STEPS := 120

var _controller: Hybrid3dSpectatorController


func _initialize() -> void:
	BootstrapScript.load_all()
	_controller = Hybrid3dSpectatorController.new()
	_controller.reset_match()
	for _i in range(MAX_STEPS):
		_controller.bot_step_once()
	_finish()


func _finish() -> void:
	var snap: Dictionary = _controller.snapshot
	var tick: int = int(snap.get("tick", 0))
	if tick <= 0:
		print("HYBRID_BOT_VS_BOT_3D_SMOKE FAIL tick=%d" % tick)
		quit(1)
	print("HYBRID_BOT_VS_BOT_3D_SMOKE PASS tick=%d buildings=%d units=%d" % [
		tick,
		snap.get("buildings", []).size() + snap.get("military_buildings", []).size(),
		snap.get("units", []).size(),
	])
	quit(0)
