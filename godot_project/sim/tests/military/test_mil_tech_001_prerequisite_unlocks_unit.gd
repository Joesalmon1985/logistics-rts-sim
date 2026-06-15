extends "res://sim/tests/test_case.gd"

## MIL-TECH-001: an advanced unit is gated by research. Queuing it before research
## is rejected; after the research completes the unit can be queued.


func get_name() -> String:
	return "test_MIL_TECH_001_prerequisite_unlocks_unit"


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
	assert_eq(int(MilitaryState.get_player(state, "p1").get("powered", -1)), 1, "base is powered")

	var plant_id: int = _building_of_type(state, "vehicle_plant")
	var validator := CommandValidator.new(CommandRegistry.create_default())

	# Tank requires advanced_weapons -> rejected before research.
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	assert_false(blocked.success, "tank blocked before research")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_MISSING_PREREQUISITE)

	# Start research.
	var started: CommandResult = validator.validate_and_apply(state, StartResearchCommand.new("p1", 0, {"tech_id": "advanced_weapons"}))
	assert_true(started.success, "research started")

	var research_ticks: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("research_ticks", 60))
	state = runner.run_ticks(state, research_ticks + 2)

	assert_true(MilitaryState.player_has_tech(state, "p1", "advanced_weapons"), "tech unlocked")
	var completed: bool = false
	for event in state.event_log.events:
		if event.type == "research_completed":
			completed = true
	assert_true(completed, "research_completed emitted")

	# Now tank can be queued.
	var ok: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	assert_true(ok.success, "tank queued after research")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
