extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_HUMAN_PROOF_004_oracle_one_building"


func _run_body() -> void:
	var plan_path: String = ProjectSettings.globalize_path(
		"res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"
	)
	var oracle := EconomyHumanProofOracleRunner.new()
	oracle.reset()
	var loaded: Dictionary = oracle.load_plan_from_path(plan_path)
	assert_true(bool(loaded.get("ok", false)), "fixture load")
	var summary: Dictionary = oracle.run_plan(loaded.get("plan", {}))
	var p1: Dictionary = summary.get("gates", {}).get("players", {}).get("p1", {})
	assert_true(int(p1.get("buildings", 0)) >= 1, "p1 completed one building via oracle")
