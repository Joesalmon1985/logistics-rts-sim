extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_003_active_hub_site_prevents_elimination"


func _run_body() -> void:
	var scenario: Dictionary = Helper.load_scenario()
	scenario["players"] = ["p1"]
	scenario["spawns"] = [{"player_id": "p1", "q": 2, "r": 2}]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	Helper.prep_all_spawns(state, scenario)

	var hub_id: int = Helper.building_of_type(state, "command_hub", "p1")
	assert_true(hub_id > 0, "hub required")
	var hub: Dictionary = MilitaryState.get_building(state, hub_id)
	hub["state"] = MilitaryConstants.BLD_DESTROYED
	hub["hp"] = 0
	MilitaryState.add_building(state, hub)
	assert_false(GodStrategyMatchEnd.player_alive(state, "p1"), "destroyed hub alone eliminates")

	var site_result: Dictionary = MilitaryConstructionSystem.create_site(state, "command_hub", 4, 4, "p1")
	assert_true(site_result.get("ok", false), "hub site created: %s" % site_result.get("reason", ""))
	assert_true(GodStrategyMatchEnd.player_alive(state, "p1"), "active hub site keeps player alive")
