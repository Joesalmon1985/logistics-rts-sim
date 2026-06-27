extends TestCase

const SCENARIO := "scenarios/military/military_mvp_skirmish.json"


func get_name() -> String:
	return "test_manual_qa_military_bot_functional"


func _run_body() -> void:
	SimBootstrap.load_all()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["seed"] = 42
	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 8000
	var state: GameState = runner.create_game(scenario)
	var bot := TutorialMilitaryBot.new()
	state = bot_runner.run_until_complete(state, bot)
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	var diag: Dictionary = snapshot.get("diagnostics", {}).get("military", {})
	assert_true(str(diag.get("victory_state", "")) == MilitaryConstants.VICTORY_WON, "military bot wins")
