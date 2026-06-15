extends "res://sim/tests/test_case.gd"

## MIL-TECH-004: missing tech prerequisites block unit production queue.


func get_name() -> String:
	return "test_MIL_TECH_004_prerequisite_blocks_unit_queue"


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
					{"type": "research_lab", "q": 10, "r": 2},
					{"type": "power_generator", "q": 2, "r": 8},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	var plant_id: int = _building_of_type(state, "vehicle_plant")
	var validator := CommandValidator.new(CommandRegistry.create_default())

	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	assert_false(blocked.success, "tank blocked before research")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_MISSING_PREREQUISITE)

	validator.validate_and_apply(state, StartResearchCommand.new("p1", 0, {"tech_id": "advanced_weapons"}))
	var research_ticks: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("research_ticks", 60))
	state = runner.run_ticks(state, research_ticks + 2)

	var ok: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	assert_true(ok.success, "tank queue accepted after research unlock")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
