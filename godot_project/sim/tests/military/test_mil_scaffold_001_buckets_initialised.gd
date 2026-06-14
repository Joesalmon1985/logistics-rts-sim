extends "res://sim/tests/test_case.gd"

## MIL-SCAFFOLD-001: creating a military game initialises every namespaced bucket
## under GameState.entities and seeds the primary player with starting credits.


func get_name() -> String:
	return "test_MIL_SCAFFOLD_001_buckets_initialised"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game({
		"variant": "military_trial",
		"seed": 7,
		"map_width": 12,
		"map_height": 12,
		"players": ["p1"],
	})

	for bucket in MilitaryConstants.ALL_BUCKETS:
		assert_true(state.entities.has(bucket), "missing bucket %s" % bucket)

	assert_eq(str(MilitaryState.get_meta_value(state, "primary_player_id", "")), "p1")
	var player: Dictionary = MilitaryState.get_player(state, "p1")
	assert_false(player.is_empty(), "player p1 must exist")
	assert_true(int(player.get("credits", 0)) > 0, "player starts with credits")
	assert_true(int(player.get("hub_count", 0)) >= 1, "player starts with a command hub")
