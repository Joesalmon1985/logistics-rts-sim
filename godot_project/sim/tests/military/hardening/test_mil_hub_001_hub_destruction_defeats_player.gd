extends "res://sim/tests/test_case.gd"

## MIL-HUB-001: destroying a player's command hub defeats that player.


func get_name() -> String:
	return "test_MIL_HUB_001_hub_destruction_defeats_player"


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
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rocket_squad", "q": 9, "r": 12, "count": 3}]},
			{"player_id": "p2", "q": 12, "r": 12},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var hub_id: int = _hub_of(state, "p2")
	assert_true(hub_id > 0, "p2 command hub exists")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == "p1":
			validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": int(unit.get("id", 0)), "target_id": hub_id}))

	state = runner.run_ticks(state, 200)
	assert_true(int(MilitaryState.get_player(state, "p2").get("defeated", 0)) > 0, "p2 defeated after hub destroyed")
	assert_eq(int(MilitaryState.get_player(state, "p2").get("hub_count", 1)), 0, "hub count zeroed in player state")

	var saw_destroyed: bool = false
	for event in state.event_log.events:
		if event.type == "building_destroyed":
			saw_destroyed = true
	assert_true(saw_destroyed, "building_destroyed event emitted")


func _hub_of(state: GameState, player_id: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) == player_id and str(building.get("building_type", "")) == "command_hub":
			return int(building.get("id", 0))
	return 0
