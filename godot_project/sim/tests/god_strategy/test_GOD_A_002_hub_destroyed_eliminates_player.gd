extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_002_hub_destroyed_eliminates_player"


func _run_body() -> void:
	var scenario: Dictionary = Helper.load_scenario()
	scenario["players"] = ["p1", "p2"]
	scenario["spawns"] = [
		{"player_id": "p1", "q": 2, "r": 2},
		{"player_id": "p2", "q": 13, "r": 13},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	Helper.prep_all_spawns(state, scenario)

	var hub_id: int = Helper.building_of_type(state, "command_hub", "p2")
	assert_true(hub_id > 0, "enemy hub required")
	var hub: Dictionary = MilitaryState.get_building(state, hub_id)
	hub["hp"] = 1
	hub["state"] = MilitaryConstants.BLD_ACTIVE
	MilitaryState.add_building(state, hub)

	assert_true(GodStrategyMatchEnd.player_alive(state, "p2"), "p2 alive before hub destroyed")
	hub["state"] = MilitaryConstants.BLD_DESTROYED
	hub["hp"] = 0
	MilitaryState.add_building(state, hub)
	assert_false(GodStrategyMatchEnd.player_alive(state, "p2"), "p2 eliminated when hub destroyed")

	var end_result: Dictionary = GodStrategyMatchEnd.evaluate(state, scenario)
	assert_true("p2" in end_result.get("eliminated_players", []), "p2 in eliminated list")
