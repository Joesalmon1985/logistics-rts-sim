extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_003_power_radar_tech"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(703)
	scenario["spawns"][0]["buildings"] = [
		{"type": "power_generator", "q": 5, "r": 2},
		{"type": "research_lab", "q": 8, "r": 2},
		{"type": "radar_tower", "q": 11, "r": 2},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 2)
	assert_eq(int(MilitaryState.get_player(state, "p1").get("powered", -1)), 1, "base must be powered")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, StartResearchCommand.new("p1", 0, {"tech_id": "advanced_weapons"}))
	assert_true(res.success, "research start must succeed with wares: %s" % res.reason_code)
	var research_ticks: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("research_ticks", 60))
	state = runner.run_ticks(state, research_ticks + 5)
	assert_true(MilitaryState.player_has_tech(state, "p1", "advanced_weapons"), "research must complete with wares")
	var snap: Dictionary = HybridSnapshotExporter.export_snapshot(state)
	assert_true(snap.has("power"), "snapshot must include power layer")
	assert_true(snap.has("tech"), "snapshot must include tech layer")
	assert_true(snap.has("vision"), "snapshot must include vision layer")
