extends "res://sim/tests/test_case.gd"

## MIL-BUILD-004: building/unit prerequisites are enforced before placement or production.


func get_name() -> String:
	return "test_MIL_BUILD_004_missing_prerequisite_rejected"


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
					{"type": "vehicle_plant", "q": 6, "r": 2},
					{"type": "power_generator", "q": 2, "r": 8},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	assert_false(
		MilitaryMapRules.prerequisites_met(state, "p1", {"requires_building": ["barracks"], "requires_tech": []}),
		"missing required building detected",
	)

	var plant_id: int = _building_of_type(state, "vehicle_plant")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	assert_false(blocked.success, "production rejected without prerequisite tech")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_MISSING_PREREQUISITE)


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
