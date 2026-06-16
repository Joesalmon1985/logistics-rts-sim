extends "res://sim/tests/test_case.gd"

const MAX_TICKS: int = 500
const MAX_ROADS: int = 45


func get_name() -> String:
	return "test_ECON_BOT_G001_road_cap"


func _run_body() -> void:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	scenario["max_ticks"] = MAX_TICKS
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)

	for player_id in ["p1", "p2"]:
		var roads: int = DualBotRunner.count_player_roads_built(state, player_id)
		assert_true(roads <= MAX_ROADS, "%s roads within cap" % player_id)

	var recorder := dual.get_recorder()
	var roads_after_100: int = 0
	for entry in recorder.get_command_entries():
		if int(entry.get("tick", 0)) <= 100:
			continue
		var cmd: Dictionary = entry.get("command", {})
		if str(cmd.get("type", "")) == "PlaceRoadCommand" and bool(entry.get("accepted", false)):
			roads_after_100 += 1
	assert_true(roads_after_100 <= MAX_ROADS, "no runaway road placement after tick 100")
