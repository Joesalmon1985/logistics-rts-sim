extends "res://sim/tests/test_case.gd"

const MIN_BOT_BUILDINGS: int = 5
const MIN_BOT_ROADS: int = 10
const MIN_BOT_DELIVERIES: int = 10


func get_name() -> String:
	return "test_ECON_BOT_002_dual_bot_expansion_metrics"


func _run_body() -> void:
	var summary: Dictionary = _run_duel_once()
	_assert_minimum_match(summary.get("state", null), summary)


func _run_duel_once() -> Dictionary:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)
	var summary: Dictionary = dual.get_summary(state)
	summary["state"] = state
	summary["commands"] = dual.get_recorder().get_command_entries()
	return summary


func _assert_minimum_match(state: GameState, summary: Dictionary) -> void:
	for player_id in ["p1", "p2"]:
		assert_true(
			DualBotRunner.count_player_buildings_built(state, player_id) >= MIN_BOT_BUILDINGS,
			"%s bot-built buildings" % player_id,
		)
		assert_true(
			DualBotRunner.count_player_roads_built(state, player_id) >= MIN_BOT_ROADS,
			"%s bot-built roads" % player_id,
		)
		assert_true(
			DualBotRunner.count_player_deliveries_built(state, player_id) >= MIN_BOT_DELIVERIES,
			"%s bot-built deliveries" % player_id,
		)

	var scores: Dictionary = summary.get("economy_scores", {})
	assert_true(int(scores.get("p1", 0)) > 0, "p1 score increased")
	assert_true(int(scores.get("p2", 0)) > 0, "p2 score increased")

	for entry in summary.get("commands", []):
		assert_true(bool(entry.get("accepted", false)), "all bot commands accepted")

	var stall_threshold: int = 300
	var player_stalls: Dictionary = state.diagnostics.get("player_stalls", {})
	for player_id in ["p1", "p2"]:
		var stall_ticks: int = int(player_stalls.get(player_id, {}).get("stall_ticks", 0))
		assert_true(stall_ticks <= stall_threshold, "%s stall within threshold" % player_id)
