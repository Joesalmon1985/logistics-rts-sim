extends "res://sim/tests/test_case.gd"

## MIL-CONS-001: a placed building advances through construction and completes into
## an active building, emitting construction_completed.


func get_name() -> String:
	return "test_MIL_CONS_001_construction_completes"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 4,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 20,
		"starting_credits": 5000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "power_generator", "q": 6, "r": 6}))

	var build_ticks: int = int(MilitaryState.get_config(state).get_building_def("power_generator").get("build_ticks", 30))
	state = runner.run_ticks(state, build_ticks + 2)

	var found_active: bool = false
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == "power_generator" and str(building.get("state", "")) == MilitaryConstants.BLD_ACTIVE:
			found_active = true
	assert_true(found_active, "generator construction completed and is active")

	var completed_event: bool = false
	for event in state.event_log.events:
		if event.type == "construction_completed":
			completed_event = true
	assert_true(completed_event, "construction_completed event emitted")
