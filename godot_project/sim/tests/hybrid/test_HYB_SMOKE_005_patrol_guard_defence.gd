extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_005_patrol_guard_defence"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(705)
	scenario["players"] = ["p1", "p2"]
	scenario["spawns"] = [
		{
			"player_id": "p1", "q": 2, "r": 2,
			"buildings": [{"type": "guard_post", "q": 6, "r": 5}],
			"units": [{"type": "rifle_squad", "q": 5, "r": 5, "count": 1}],
		},
		{
			"player_id": "p2", "q": 13, "r": 13,
			"units": [{"type": "scout", "q": 7, "r": 5, "count": 1}],
		},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var post_id: int = HybridTestHelper.building_of_type(state, "guard_post")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var patrol_res: CommandResult = validator.validate_and_apply(state, SetPatrolZoneCommand.new("p1", 0, {
		"building_id": post_id,
		"waypoints": [{"q": 5, "r": 5}, {"q": 8, "r": 5}],
	}))
	assert_true(patrol_res.success, "patrol zone accepted")
	var guard_res: CommandResult = validator.validate_and_apply(state, SetGuardPolicyCommand.new("p1", 0, {
		"building_id": post_id,
	}))
	assert_true(guard_res.success, "guard policy accepted")
	state = runner.run_ticks(state, 60)
	var enemy_alive: bool = false
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == "p2" and str(unit.get("state", "")) != MilitaryConstants.UNIT_DEAD:
			enemy_alive = true
	assert_false(enemy_alive, "patrol/guard policy should engage nearby hostile")
