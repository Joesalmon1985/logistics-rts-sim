extends "res://sim/tests/test_case.gd"

## MIL-VISION-001/002: with fog enabled, a unit reveals hexes within its sight
## (VISIBLE) while distant hexes stay UNEXPLORED; once the unit leaves, previously
## seen hexes demote to EXPLORED (shroud) rather than becoming visible or unexplored.


func get_name() -> String:
	return "test_MIL_VISION_001_fog_reveal_demote"


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
	var unit_id: int = _first_unit_of_type(state, "rifle_squad")

	state = runner.run_ticks(state, 1)
	assert_eq(MilitaryState.intel_level(state, "p1", 8, 8), MilitaryConstants.INTEL_VISIBLE, "unit hex visible")
	assert_eq(MilitaryState.intel_level(state, "p1", 14, 14), MilitaryConstants.INTEL_UNEXPLORED, "distant hex unexplored")

	# Teleport the unit far away and recompute vision.
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	unit["q"] = 13
	unit["r"] = 1
	unit["order"] = {"type": MilitaryConstants.ORDER_NONE}
	unit["path"] = []
	state = runner.run_ticks(state, 1)

	assert_eq(MilitaryState.intel_level(state, "p1", 8, 8), MilitaryConstants.INTEL_EXPLORED, "old hex demoted to explored")
	assert_eq(MilitaryState.intel_level(state, "p1", 13, 1), MilitaryConstants.INTEL_VISIBLE, "new hex visible")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
