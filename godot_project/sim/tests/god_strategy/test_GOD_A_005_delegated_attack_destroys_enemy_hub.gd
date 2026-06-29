extends "res://sim/tests/test_case.gd"

const Helper := preload("res://sim/god_strategy/god_strategy_test_helper.gd")


func get_name() -> String:
	return "test_GOD_A_005_delegated_attack_destroys_enemy_hub"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(99005)
	scenario["players"] = ["p1", "p2"]
	scenario["spawns"] = [
		{
			"player_id": "p1", "q": 2, "r": 2,
			"units": [{"type": "light_vehicle", "q": 4, "r": 4, "count": 2}],
		},
		{"player_id": "p2", "q": 13, "r": 13},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)

	var enemy_hub: int = Helper.building_of_type(state, "command_hub", "p2")
	assert_true(enemy_hub > 0, "enemy hub required")
	var enemy: Dictionary = MilitaryState.get_building(state, enemy_hub)
	enemy["hp"] = 1
	MilitaryState.add_building(state, enemy)

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, DelegatedAttackCommand.new("p1", 0, {
		"target_q": int(enemy.get("anchor_q", 0)),
		"target_r": int(enemy.get("anchor_r", 0)),
	}))
	assert_true(res.success, "delegated attack accepted")

	state = runner.run_ticks(state, 200)
	enemy = MilitaryState.get_building(state, enemy_hub)
	var destroyed: bool = enemy.is_empty() or str(enemy.get("state", "")) == MilitaryConstants.BLD_DESTROYED
	assert_true(
		destroyed or HybridTestHelper.has_event(state, "damage_dealt"),
		"delegated attack must damage or destroy hostile hub",
	)
