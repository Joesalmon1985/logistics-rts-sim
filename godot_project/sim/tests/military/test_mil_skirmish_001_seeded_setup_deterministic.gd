extends "res://sim/tests/test_case.gd"

## MIL-SKIRMISH-001: the seeded skirmish hook produces a deterministic scenario
## (identical seed => identical setup) with symmetric spawns and a seeded map that
## creates a playable game with one command hub per player.


func get_name() -> String:
	return "test_MIL_SKIRMISH_001_seeded_setup_deterministic"


func _run_body() -> void:
	var a: Dictionary = MilitarySkirmish.build_scenario(7, 2, 24, 24)
	var b: Dictionary = MilitarySkirmish.build_scenario(7, 2, 24, 24)
	assert_eq(JSON.stringify(a), JSON.stringify(b), "same seed yields identical scenario")
	assert_eq(int(a.get("spawns", []).size()), 2, "two spawns for 1v1")

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(a)
	assert_eq(state.map.width, 24, "seeded map generated at requested width")
	assert_eq(MilitaryState.all_players(state).size(), 2, "two players created")
	assert_eq(_hub_count(state, "p1"), 1, "p1 has a command hub")
	assert_eq(_hub_count(state, "p2"), 1, "p2 has a command hub")

	# Identical seed also produces identical post-run replay hash.
	var state_run: GameState = runner.run_ticks(state, 20)
	var h1: String = ReplayHash.compute_state_hash(state_run)
	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.create_game(MilitarySkirmish.build_scenario(7, 2, 24, 24))
	state2 = runner2.run_ticks(state2, 20)
	assert_eq(h1, ReplayHash.compute_state_hash(state2), "seeded skirmish is deterministic")


func _hub_count(state: GameState, player_id: String) -> int:
	var n: int = 0
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) == player_id and str(building.get("building_type", "")) == "command_hub":
			n += 1
	return n
