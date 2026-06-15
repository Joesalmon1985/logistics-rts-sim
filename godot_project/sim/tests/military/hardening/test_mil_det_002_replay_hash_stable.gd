extends "res://sim/tests/test_case.gd"

## MIL-DET-002: identical seed and commands yield a stable replay hash.


func get_name() -> String:
	return "test_MIL_DET_002_replay_hash_stable"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 71,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [{"type": "power_generator", "q": 6, "r": 2}],
				"units": [{"type": "rifle_squad", "q": 8, "r": 8}],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 10)
	var h1: String = ReplayHash.compute_state_hash(state)

	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.create_game(scenario)
	state2 = runner2.run_ticks(state2, 10)
	var h2: String = ReplayHash.compute_state_hash(state2)

	assert_eq(h1, h2, "replay hash stable for identical scenario and tick count")
	assert_true(h1.length() > 0, "hash is non-empty")

	var runner3 := SimulationTickRunner.new()
	var state3: GameState = runner3.create_game(scenario)
	state3 = runner3.run_ticks(state3, 11)
	var h3: String = ReplayHash.compute_state_hash(state3)
	assert_true(h1 != h3, "different tick counts produce different hashes")
