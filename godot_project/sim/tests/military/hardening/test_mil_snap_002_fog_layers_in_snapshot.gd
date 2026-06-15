extends "res://sim/tests/test_case.gd"

## MIL-SNAP-002: fog/intel vision layers are exported in military snapshots.


func get_name() -> String:
	return "test_MIL_SNAP_002_fog_layers_in_snapshot"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 41,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 8, "r": 8}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_has_keys(snapshot, ["vision"], "snapshot includes vision layer")
	var vision: Dictionary = snapshot.get("vision", {})
	assert_true(vision.has("p1"), "vision layer includes player p1")

	var levels: Dictionary = vision.get("p1", {}).get("levels", {})
	assert_true(levels.size() > 0, "intel levels populated")
	assert_eq(int(levels.get(MilitaryState.coord_key(8, 8), -1)), MilitaryConstants.INTEL_VISIBLE, "visible hex exported")
	assert_false(levels.has(MilitaryState.coord_key(14, 14)), "unexplored hex omitted from vision export")
	assert_eq(MilitaryState.intel_level(state, "p1", 14, 14), MilitaryConstants.INTEL_UNEXPLORED, "unexplored in live state")
