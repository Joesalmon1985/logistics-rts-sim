extends "res://sim/tests/test_case.gd"

## MIL-VISION-002: a powered radar tower extends visible intel range.


func get_name() -> String:
	return "test_MIL_VISION_002_radar_extends_sight"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 43,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	assert_eq(MilitaryState.intel_level(state, "p1", 12, 2), MilitaryConstants.INTEL_UNEXPLORED, "hex beyond hub sight unexplored")

	MilitaryConstructionSystem.spawn_building(state, "radar_tower", 6, 2, "p1", MilitaryState.get_config(state))
	state = runner.run_ticks(state, 1)
	assert_eq(MilitaryState.intel_level(state, "p1", 12, 2), MilitaryConstants.INTEL_VISIBLE, "radar extends sight to distant hex")
