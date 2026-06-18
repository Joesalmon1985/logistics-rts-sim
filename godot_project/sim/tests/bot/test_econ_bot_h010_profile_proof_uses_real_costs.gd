extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BOT_H010_profile_proof_uses_real_costs"


func _run_body() -> void:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	assert_false(bool(scenario.get("diag_mode", false)), "PROFILE_PROOF diag_mode false")

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.attach_scenario_config(state, scenario)
	assert_false(EconomyState.has_config_override(state), "PROFILE_PROOF has no config override")

	var woodcutter_cost: Dictionary = EconomyBvbSetup.get_proof_construction_cost("woodcutter")
	assert_true(int(woodcutter_cost.get("planks", 0)) > 0, "woodcutter planks not free")
	assert_true(int(woodcutter_cost.get("stone", 0)) > 0, "woodcutter stone not free")
	assert_true(int(woodcutter_cost.get("builder_work", 0)) > 0, "builder_work not zeroed")

	var kit: Dictionary = EconomyBvbSetup.proof_spawn_kit()
	var proof_scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var p1_spawn: Dictionary = proof_scenario.get("spawns", [{}])[0]
	var starting: Dictionary = p1_spawn.get("starting_wares", {})
	assert_eq(starting, kit["starting_wares"], "PROFILE_PROOF starting wares unchanged")

	assert_eq(EconomyHumanProofContract.MIN_BUILDINGS, 5, "buildings threshold unchanged")
	assert_eq(EconomyHumanProofContract.MIN_ROADS, 10, "roads threshold unchanged")
	assert_eq(EconomyHumanProofContract.MIN_DELIVERIES, 10, "deliveries threshold unchanged")
