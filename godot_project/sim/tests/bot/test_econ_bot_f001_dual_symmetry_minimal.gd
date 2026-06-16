extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 400


func get_name() -> String:
	return "test_ECON_BOT_F001_dual_symmetry_minimal"


func _run_body() -> void:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	scenario["max_ticks"] = MAX_TICKS
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)

	for player_id in ["p1", "p2"]:
		assert_true(
			DualBotRunner.count_player_buildings_built(state, player_id) >= 1,
			"%s bot-built buildings" % player_id,
		)
		assert_true(
			DualBotRunner.count_player_deliveries_built(state, player_id) >= 1,
			"%s bot-built deliveries" % player_id,
		)

	var p1_roads: int = DualBotRunner.count_player_roads_built(state, "p1")
	var p2_roads: int = DualBotRunner.count_player_roads_built(state, "p2")
	var max_roads: int = maxi(p1_roads, p2_roads)
	var min_roads: int = mini(p1_roads, p2_roads)
	if max_roads > 0:
		assert_true(float(min_roads) / float(max_roads) >= 0.5, "road counts within 50% symmetry")
