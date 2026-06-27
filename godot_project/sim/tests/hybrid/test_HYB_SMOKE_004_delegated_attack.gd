extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_004_delegated_attack"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(704)
	scenario["players"] = ["p1", "p2"]
	scenario["spawns"] = [
		{
			"player_id": "p1", "q": 2, "r": 2,
			"buildings": [{"type": "power_generator", "q": 5, "r": 2}],
			"units": [{"type": "light_vehicle", "q": 4, "r": 4, "count": 2}],
		},
		{"player_id": "p2", "q": 13, "r": 13},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	HybridTestHelper.flatten_grass(state, 0, 0, 15, 15)
	state = runner.run_ticks(state, 1)
	var enemy_hub: int = HybridTestHelper.building_of_type(state, "command_hub", "p2")
	assert_true(enemy_hub > 0, "enemy hub required")
	var enemy: Dictionary = MilitaryState.get_building(state, enemy_hub)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, DelegatedAttackCommand.new("p1", 0, {
		"target_q": int(enemy.get("anchor_q", 0)),
		"target_r": int(enemy.get("anchor_r", 0)),
	}))
	assert_true(res.success, "delegated attack must be accepted: %s" % res.reason_code)
	state = runner.run_ticks(state, 120)
	var hub: Dictionary = MilitaryState.get_building(state, enemy_hub)
	var defeated: bool = hub.is_empty() or str(hub.get("state", "")) == MilitaryConstants.BLD_DESTROYED
	if not defeated:
		defeated = int(MilitaryState.get_player(state, "p2").get("hub_count", 1)) <= 0
	assert_true(defeated or HybridTestHelper.has_event(state, "damage_dealt"), "delegated attack must damage or destroy hostile hub")
