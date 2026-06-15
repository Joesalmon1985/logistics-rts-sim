extends "res://sim/tests/test_case.gd"

## MIL-FOG-001: with fog enabled, distant hexes start unexplored.


func get_name() -> String:
	return "test_MIL_FOG_001_unexplored_hidden"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 41,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 3, "r": 3}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	assert_eq(MilitaryState.intel_level(state, "p1", 3, 3), MilitaryConstants.INTEL_VISIBLE, "near unit hex visible")
	assert_eq(MilitaryState.intel_level(state, "p1", 14, 14), MilitaryConstants.INTEL_UNEXPLORED, "distant hex unexplored")

	var vision: Dictionary = SnapshotExporter.export_snapshot(state).get("vision", {})
	assert_true(vision.has("p1"), "vision layer exported for player")
