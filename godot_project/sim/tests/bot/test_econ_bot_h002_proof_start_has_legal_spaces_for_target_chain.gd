extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BOT_H002_proof_start_has_legal_spaces_for_target_chain"


func _run_body() -> void:
	var state: GameState = EconomyHumanProofContract.create_proof_state()
	for player_id in ["p1", "p2"]:
		for building_type in EconomyBvbSetup.CHAIN_BUILDING_TYPES:
			var found: Vector2i = EconomyBvbSetup.find_chain_building_hex(state, player_id, building_type)
			assert_true(found.x >= 0, "%s legal %s at proof start" % [player_id, building_type])
