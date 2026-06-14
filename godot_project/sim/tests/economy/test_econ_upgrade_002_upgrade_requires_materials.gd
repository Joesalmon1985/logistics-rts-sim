extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_UPGRADE_002_upgrade_requires_materials"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 71,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	_flatten_grass_area(state, 1, 1, 7, 4)
	var sawmill_id: int = EconomyInit._spawn_completed_building(
		state,
		"sawmill",
		5,
		2,
		"p1",
		EconomyState.get_config(state),
	)

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var cmd := UpgradeBuildingCommand.new("p1", 0, {"building_id": sawmill_id})
	var result: CommandResult = validator.validate_and_apply(state, cmd)
	assert_true(result.success, "upgrade command should start construction site")

	var sites: Array = EconomyState.all_construction_sites(state)
	assert_eq(sites.size(), 1, "upgrade must create a construction site")
	var site: Dictionary = sites[0]
	assert_true(int(site.get("required", {}).size()) > 0, "upgrade must declare material requirements")
	assert_eq(str(site.get("state", "")), EconomyConstants.CONS_ACTIVE)

	state = runner.run_ticks(state, 2)
	site = EconomyState.get_construction_site(state, int(site.get("id", 0)))
	assert_eq(str(site.get("state", "")), EconomyConstants.CONS_BLOCKED, "missing materials must block upgrade site")

	var diag_construction: Dictionary = state.diagnostics.get("construction", {})
	assert_true(int(diag_construction.get("blocked", 0)) >= 1, "diagnostics must flag blocked upgrade construction")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
