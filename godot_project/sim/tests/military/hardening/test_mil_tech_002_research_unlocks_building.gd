extends "res://sim/tests/test_case.gd"

## MIL-TECH-002: research lab consumes credits/ticks and unlocks tech entries.


func get_name() -> String:
	return "test_MIL_TECH_002_research_unlocks_building"


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
					{"type": "research_lab", "q": 10, "r": 2},
					{"type": "power_generator", "q": 2, "r": 8},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	var credits_before: int = MilitaryState.player_credits(state, "p1")
	var cost: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("credit_cost", 0))

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var started: CommandResult = validator.validate_and_apply(state, StartResearchCommand.new("p1", 0, {"tech_id": "advanced_weapons"}))
	assert_true(started.success, "research started")
	assert_eq(MilitaryState.player_credits(state, "p1"), credits_before - cost, "research consumed credits")

	var research_ticks: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("research_ticks", 60))
	state = runner.run_ticks(state, research_ticks + 2)
	assert_true(MilitaryState.player_has_tech(state, "p1", "advanced_weapons"), "tech unlocked")

	var saw_completed: bool = false
	for event in state.event_log.events:
		if event.type == "research_completed":
			saw_completed = true
	assert_true(saw_completed, "research_completed event emitted")

	var tech_layer: Dictionary = SnapshotExporter.export_snapshot(state).get("tech", {})
	assert_true(tech_layer.has("p1"), "tech layer in snapshot for p1")
	assert_true(MilitaryState.player_has_tech(state, "p1", "advanced_weapons"), "unlocked tech in state")
