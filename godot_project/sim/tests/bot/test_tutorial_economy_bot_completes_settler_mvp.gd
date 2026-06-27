extends "res://sim/tests/test_case.gd"

## Tutorial economy bot completes Settler MVP objective (MRP Track A).

const SCENARIO := "scenarios/economy/settler_mvp.json"
const MAX_TICKS: int = 1000


func get_name() -> String:
	return "test_tutorial_economy_bot_completes_settler_mvp"


func _run_body() -> void:
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["seed"] = 42
	var bot_runner := BotRunner.new(runner)
	bot_runner.max_ticks = MAX_TICKS
	var state: GameState = runner.create_game(scenario)
	var bot := TutorialEconomyBot.new()
	state = bot_runner.run_until_complete(state, bot)
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_true(bool(snapshot.get("objective", {}).get("complete", false)), "objective complete")
	assert_true(bot.get_decisions().size() > 0, "bot issued decisions")
	assert_true(bot.get_stall_ticks_since_progress(state.tick) <= 500, "stall bounded")
