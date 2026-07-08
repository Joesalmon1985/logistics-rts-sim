extends SceneTree

## God-Strategy eval harness — domination regression @ proof tick cap.


func _initialize() -> void:
	SimBootstrap.load_all()
	var ok: bool = true
	var regression_dir: String = GodStrategyEvalRunner.run_eval_mode(
		"phase0_domination",
		GodStrategyConstants.PROOF_TICK_CAP,
		true,
		GodStrategyEvalRunner.SCENARIO_REGRESSION,
	)
	if regression_dir == "":
		ok = false
	elif not GodStrategyEvalRunner.evaluate_domination_pass(regression_dir):
		ok = false

	var smoke_dir: String = GodStrategyEvalRunner.run_eval_mode(
		"4p_mvp_a_smoke",
		GodStrategyConstants.SMOKE_TICK_CAP,
		true,
		GodStrategyEvalRunner.SCENARIO_MVP_A,
	)
	if smoke_dir == "":
		ok = false

	if ok:
		print("GOD STRATEGY EVAL PASS")
		quit(0)
	print("GOD STRATEGY EVAL FAIL")
	quit(1)
