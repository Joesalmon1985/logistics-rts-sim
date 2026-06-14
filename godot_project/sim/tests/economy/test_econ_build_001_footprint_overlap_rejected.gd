extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BUILD_001_footprint_overlap_rejected"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 17,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 3, "r": 3},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var hq_q: int = int(EconomyState.get_meta_value(state, "hq_position_q", 3))
	var hq_r: int = int(EconomyState.get_meta_value(state, "hq_position_r", 3))
	var check: Dictionary = EconomyMapRules.can_place_building(state, "storehouse", hq_q, hq_r, "p1")
	assert_false(check.get("ok", true), "overlapping HQ footprint must be rejected")
	assert_eq(str(check.get("reason", "")), "FOOTPRINT_OVERLAP")

	var sites_before: int = EconomyState.all_construction_sites(state).size()
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": hq_q,
		"r": hq_r,
		"player_id": "p1",
	})
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var apply_result: CommandResult = validator.validate_and_apply(state, cmd)
	assert_false(apply_result.success, "command must fail on footprint overlap")
	assert_eq(sites_before, EconomyState.all_construction_sites(state).size(), "no construction site created")
