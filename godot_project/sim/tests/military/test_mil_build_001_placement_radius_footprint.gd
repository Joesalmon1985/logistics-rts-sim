extends "res://sim/tests/test_case.gd"

## MIL-BUILD-001: building placement honours buildability, footprint overlap, and
## build radius; a valid placement spends credits and creates a construction site.


func get_name() -> String:
	return "test_MIL_BUILD_001_placement_radius_footprint"


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

	# Valid placement within radius (generator 2x2 at 5,2; hub footprint covers 2..4).
	var ok_check: Dictionary = MilitaryMapRules.can_place_building(state, "power_generator", 5, 2, "p1")
	assert_true(ok_check.get("ok", false), "valid placement within radius accepted")

	# Footprint overlap with the command hub (anchor 2,2 covers 2..4 x 2..4).
	var overlap: Dictionary = MilitaryMapRules.can_place_building(state, "power_generator", 2, 2, "p1")
	assert_false(overlap.get("ok", true), "overlap rejected")
	assert_eq(str(overlap.get("reason", "")), MilitaryConstants.ERR_FOOTPRINT_OVERLAP)

	# Outside build radius but in-bounds (single-hex radar tower at 12,12).
	var far: Dictionary = MilitaryMapRules.can_place_building(state, "radar_tower", 12, 12, "p1")
	assert_false(far.get("ok", true), "outside radius rejected")
	assert_eq(str(far.get("reason", "")), MilitaryConstants.ERR_OUTSIDE_BUILD_RADIUS)

	# Non-buildable terrain (water hex, checked before radius in the footprint loop).
	state.map.set_tile(HexCoord.new(10, 10), HexTile.new("water", 0, 0, 0))
	var nb: Dictionary = MilitaryMapRules.can_place_building(state, "radar_tower", 10, 10, "p1")
	assert_false(nb.get("ok", true), "non-buildable rejected")
	assert_eq(str(nb.get("reason", "")), MilitaryConstants.ERR_NOT_BUILDABLE)

	# Valid placement via command spends credits and creates a site.
	var credits_before: int = MilitaryState.player_credits(state, "p1")
	var sites_before: int = MilitaryState.all_construction_sites(state).size()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "power_generator", "q": 5, "r": 2}))
	assert_true(res.success, "valid placement command accepted")
	assert_eq(MilitaryState.all_construction_sites(state).size(), sites_before + 1, "construction site created")
	var cost: int = int(MilitaryState.get_config(state).get_building_def("power_generator").get("credit_cost", 0))
	assert_eq(MilitaryState.player_credits(state, "p1"), credits_before - cost, "credits spent")
