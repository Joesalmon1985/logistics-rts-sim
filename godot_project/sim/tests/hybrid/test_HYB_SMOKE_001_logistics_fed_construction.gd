extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_001_logistics_fed_construction"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(701)
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	var result: Dictionary = EconomyConstructionSystem.create_site(state, "barracks", 8, 4, "p1")
	assert_true(result.get("ok", false), "barracks site must be created")
	var site_id: int = int(result.get("site_id", 0))
	var site: Dictionary = EconomyState.get_construction_site(state, site_id)
	if not site.has("delivered"):
		site["delivered"] = {}
	for ware_type in site.get("required", {}):
		if str(ware_type) != "builder_work":
			site["delivered"][ware_type] = int(site["required"][ware_type])
	site["builder_work_remaining"] = 1
	EconomyState.add_construction_site(state, site)
	state = runner.tick(state)
	assert_true(HybridTestHelper.building_of_type(state, "barracks") > 0, "barracks must complete via wares")
	assert_true(HybridTestHelper.has_event(state, "military_construction_completed"), "military_construction_completed expected")
