extends "res://sim/tests/test_case.gd"

## MIL-SMOKE-008: destroying the enemy command hub triggers win/loss resolution.


func get_name() -> String:
	return "test_MIL_SMOKE_008_win_loss_triggers"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 31,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rocket_squad", "q": 9, "r": 12, "count": 5}]},
			{"player_id": "p2", "q": 12, "r": 12},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var hub_id: int = _hub_of(state, "p2")
	assert_true(hub_id > 0, "enemy hub exists")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == "p1":
			validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": int(unit.get("id", 0)), "target_id": hub_id}))

	state = runner.run_ticks(state, 250)
	assert_eq(int(MilitaryState.get_meta_value(state, "victory_decided", 0)), 1, "victory decided")
	assert_eq(str(MilitaryState.get_meta_value(state, "winner_player_id", "")), "p1", "p1 wins")
	assert_eq(str(MilitaryState.get_meta_value(state, "victory_state", "")), MilitaryConstants.VICTORY_WON, "victory state is won")

	var diag_victory: String = str(state.diagnostics.get("military", {}).get("victory_state", ""))
	assert_eq(diag_victory, MilitaryConstants.VICTORY_WON, "victory reflected in diagnostics")

	var saw_victory: bool = false
	for event in state.event_log.events:
		if event.type == "player_victory":
			saw_victory = true
	assert_true(saw_victory, "player_victory event emitted")


func _hub_of(state: GameState, player_id: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) == player_id and str(building.get("building_type", "")) == "command_hub":
			return int(building.get("id", 0))
	return 0
