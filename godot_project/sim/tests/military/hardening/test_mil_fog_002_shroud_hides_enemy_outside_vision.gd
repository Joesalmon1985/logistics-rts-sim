extends "res://sim/tests/test_case.gd"

## MIL-FOG-002: explored shroud hides areas outside current unit sight.


func get_name() -> String:
	return "test_MIL_FOG_002_shroud_hides_enemy_outside_vision"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 42,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 8, "r": 8}]}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var unit_id: int = _first_unit_of_type(state, "rifle_squad")

	state = runner.run_ticks(state, 1)
	assert_eq(MilitaryState.intel_level(state, "p1", 8, 8), MilitaryConstants.INTEL_VISIBLE, "unit hex initially visible")

	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	unit["q"] = 13
	unit["r"] = 1
	unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
	unit["path"] = []
	state = runner.run_ticks(state, 1)

	assert_eq(MilitaryState.intel_level(state, "p1", 8, 8), MilitaryConstants.INTEL_EXPLORED, "old hex demoted to shroud (explored)")
	assert_eq(MilitaryState.intel_level(state, "p1", 13, 1), MilitaryConstants.INTEL_VISIBLE, "new position visible")
	assert_eq(MilitaryState.intel_level(state, "p1", 14, 14), MilitaryConstants.INTEL_UNEXPLORED, "never-seen hex still unexplored")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
