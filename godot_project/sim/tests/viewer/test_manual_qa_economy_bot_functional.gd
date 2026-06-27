extends TestCase

const SCENARIO := "scenarios/economy/settler_mvp.json"


func get_name() -> String:
	return "test_manual_qa_economy_bot_functional"


func _run_body() -> void:
	SimBootstrap.load_all()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["seed"] = 42
	var runner := SimulationTickRunner.new()
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = 5000
	var state: GameState = runner.create_game(scenario)
	var bot := TutorialEconomyBot.new()
	state = bot_runner.run_until_complete(state, bot)
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_true(bool(snapshot.get("objective", {}).get("complete", false)), "economy bot completes")
	assert_true(bot.get_decisions().size() > 0, "decisions logged")
