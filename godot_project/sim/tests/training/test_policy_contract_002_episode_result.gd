extends "res://sim/tests/test_case.gd"

## POLICY-CONTRACT — episode result win/loss/draw mapping.


func get_name() -> String:
	return "test_POLICY_CONTRACT_002_episode_result"


func _run_body() -> void:
	SimBootstrap.load_all()

	var focus_run: Dictionary = PolicyEvalRunner.run_episode(
		TrainingEnvServer.new(),
		"scenarios/military/eval_focus_fire_hub.json",
		21,
		"scripted_good",
		"p1",
		0,
		250,
	)
	assert_true(focus_run.get("ok", false), "scripted focus fire episode ok")
	var focus_episode: Dictionary = focus_run.get("episode_result", {})
	assert_eq(str(focus_episode.get("outcome", "")), "win", "scripted should win focus fire scenario")
	assert_eq(float(focus_episode.get("fitness", 0.0)), 1.0, "win fitness")

	var focus_pass: Dictionary = PolicyEvalRunner.run_episode(
		TrainingEnvServer.new(),
		"scenarios/military/eval_focus_fire_hub.json",
		21,
		"pass_only",
		"p1",
		0,
		120,
	)
	assert_true(focus_pass.get("ok", false), "pass focus fire episode ok")
	assert_true(float(focus_pass.get("episode_result", {}).get("fitness", 1.0)) < 1.0, "pass_only should not win focus fire")

	var survive_pass: Dictionary = PolicyEvalRunner.run_episode(
		TrainingEnvServer.new(),
		"scenarios/military/eval_survive_rush.json",
		21,
		"pass_only",
		"p1",
		0,
		120,
	)
	assert_true(survive_pass.get("ok", false), "pass survive rush episode ok")
	assert_true(float(survive_pass.get("episode_result", {}).get("fitness", 1.0)) < 1.0, "pass_only should not win survive rush")

	var survive_scripted: Dictionary = PolicyEvalRunner.run_episode(
		TrainingEnvServer.new(),
		"scenarios/military/eval_survive_rush.json",
		21,
		"scripted_good",
		"p1",
		0,
		250,
	)
	assert_true(survive_scripted.get("ok", false), "scripted survive rush episode ok")
	assert_eq(float(survive_scripted.get("episode_result", {}).get("fitness", 0.0)), 1.0, "scripted should win survive rush")
