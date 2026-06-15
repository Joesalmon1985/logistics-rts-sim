extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_UPGRADE_004_upgraded_changes_capabilities"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 154,
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
		state, "sawmill", 5, 2, "p1", EconomyState.get_config(state),
	)
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var upgrade_def: Dictionary = cfg.get_upgrade_def("sawmill")
	var site_result: Dictionary = EconomyConstructionSystem.create_site(
		state, "enhanced_sawmill", 5, 2, "p1", sawmill_id, upgrade_def.get("cost", {}),
	)
	var site_id: int = int(site_result.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	for ware_type in site.get("required", {}):
		if ware_type == "builder_work":
			continue
		EconomyConstructionSystem.deliver_to_site(state, site_id, str(ware_type), int(site["required"][ware_type]))
	site = EconomyState.get_construction_site(state, site_id)
	site["builder_work_remaining"] = 0
	EconomyState.add_construction_site(state, site)
	EconomyConstructionSystem.tick(state)

	var upgraded: Dictionary = EconomyState.get_building(state, sawmill_id)
	assert_eq(str(upgraded.get("building_type", "")), "enhanced_sawmill")
	var enhanced_def: Dictionary = cfg.get_building_def("enhanced_sawmill")
	assert_eq(int(enhanced_def.get("worker_slots", 0)), 2, "upgraded building exposes changed slot count")
	assert_eq(str(enhanced_def.get("recipe_id", "")), "planks_from_timber_fast")


func _flatten_grass_area(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			state.map.set_tile(HexCoord.new(q, r), HexTile.new("grass", 0, 1, 1))
