extends "res://sim/tests/test_case.gd"

## MIL-BUILD-005: structures outside the build radius are rejected.


func get_name() -> String:
	return "test_MIL_BUILD_005_outside_build_radius_rejected"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 4,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 4,
		"starting_credits": 5000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var far: Dictionary = MilitaryMapRules.can_place_building(state, "radar_tower", 12, 12, "p1")
	assert_false(far.get("ok", true), "outside radius rejected by rules")
	assert_eq(str(far.get("reason", "")), MilitaryConstants.ERR_OUTSIDE_BUILD_RADIUS)

	var sites_before: int = MilitaryState.all_construction_sites(state).size()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "radar_tower", "q": 12, "r": 12}))
	assert_false(res.success, "placement command rejected")
	assert_eq(MilitaryState.all_construction_sites(state).size(), sites_before, "no construction site created")
