extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_HYB_SMOKE_008_win_loss_stall"


func _run_body() -> void:
	var scenario: Dictionary = HybridTestHelper.base_scenario(708)
	scenario["players"] = ["p1", "p2"]
	scenario["stall_ticks_threshold"] = 5
	scenario["spawns"] = [
		{"player_id": "p1", "q": 2, "r": 2, "units": [{"type": "light_vehicle", "q": 4, "r": 4, "count": 1}]},
		{"player_id": "p2", "q": 13, "r": 13},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var enemy_hub: int = HybridTestHelper.building_of_type(state, "command_hub", "p2")
	var enemy: Dictionary = MilitaryState.get_building(state, enemy_hub)
	enemy["hp"] = 1
	MilitaryState.add_building(state, enemy)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, DelegatedAttackCommand.new("p1", 0, {
		"target_q": int(enemy.get("anchor_q", 0)),
		"target_r": int(enemy.get("anchor_r", 0)),
	}))
	state = runner.run_ticks(state, 150)
	var victory: bool = HybridTestHelper.has_event(state, "player_victory") or HybridTestHelper.has_event(state, "player_defeated")
	var stall: bool = HybridTestHelper.has_event(state, "hybrid_stalled")
	assert_true(victory or stall, "win/loss or stall diagnostic must fire")
