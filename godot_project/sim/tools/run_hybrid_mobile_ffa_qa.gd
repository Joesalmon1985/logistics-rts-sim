extends SceneTree

## Hybrid mobile FFA QA (PRD D).


func _initialize() -> void:
	SimBootstrap.load_all()
	var ctrl := HybridMobileFfaController.new()
	ctrl.reset_ffa("normal")
	for _i in range(40):
		ctrl.step_tick()
	var summary: Dictionary = ctrl.get_match_summary()
	var telemetry: Array = ctrl.get_ai_telemetry()
	if telemetry.size() < 1:
		print("FFA QA FAIL: no AI telemetry")
		quit(1)
	print("FFA QA PASS tick=%s alive=%s" % [summary.get("tick", 0), str(summary.get("alive", []))])
	quit(0)
