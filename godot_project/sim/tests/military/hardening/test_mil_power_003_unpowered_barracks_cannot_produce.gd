extends "res://sim/tests/test_case.gd"

## MIL-POWER-003: underpowered production buildings cannot queue units.


func get_name() -> String:
	return "test_MIL_POWER_003_unpowered_barracks_cannot_produce"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 4,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 50000,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [
					{"type": "barracks", "q": 6, "r": 2},
					{"type": "vehicle_plant", "q": 9, "r": 2},
					{"type": "research_lab", "q": 12, "r": 2},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	assert_eq(int(MilitaryState.get_player(state, "p1").get("powered", -1)), 0, "base underpowered")

	var barracks_id: int = _building_of_type(state, "barracks")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_false(blocked.success, "production blocked while unpowered")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_UNPOWERED)

	var barracks: Dictionary = MilitaryState.get_building(state, barracks_id)
	assert_eq(int(barracks.get("powered", 1)), 0, "barracks marked unpowered in state")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
