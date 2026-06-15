extends "res://sim/tests/test_case.gd"

## MIL-REPLAY-002: skirmish scenarios produce deterministic replay hashes.


func get_name() -> String:
	return "test_MIL_REPLAY_002_skirmish_replay_hash"


func _run_body() -> void:
	var ticks: int = 40
	var scenario: Dictionary = MilitarySkirmish.build_scenario(99, 2, 24, 24)

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.run_ticks(runner.create_game(scenario), ticks)
	var hash_a: String = ReplayHash.compute_state_hash(state)

	var runner2 := SimulationTickRunner.new()
	var state2: GameState = runner2.run_ticks(runner2.create_game(MilitarySkirmish.build_scenario(99, 2, 24, 24)), ticks)
	var hash_b: String = ReplayHash.compute_state_hash(state2)

	assert_eq(hash_a, hash_b, "skirmish replay hash matches across reruns")
	assert_true(hash_a.length() > 0, "hash is non-empty")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_eq(int(snapshot.get("tick", -1)), ticks, "snapshot tick matches run length")
