extends "res://sim/tests/test_case.gd"

## MIL-ECON-005: delivered cargo at a resource processor converts to player credits.


func get_name() -> String:
	return "test_MIL_ECON_005_processor_converts_to_credits"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 5,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 20,
		"starting_credits": 0,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [{"type": "resource_processor", "q": 6, "r": 6}],
				"units": [{"type": "collector", "q": 8, "r": 8, "count": 1}],
			},
		],
		"resource_fields": [{"q": 10, "r": 10, "field_type": "mineral_field"}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	assert_eq(MilitaryState.player_credits(state, "p1"), 0, "starts with zero credits")

	var collector_id: int = _find_collector(state)
	var field_id: int = _find_field(state)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": field_id}))
	state = runner.run_ticks(state, 80)

	assert_true(MilitaryState.player_credits(state, "p1") > 0, "processor converted cargo to credits")
	var saw_delivery: bool = false
	for event in state.event_log.events:
		if event.type == "resource_delivered":
			saw_delivery = true
	assert_true(saw_delivery, "resource_delivered event emitted")

	var p1_diag: Dictionary = state.diagnostics.get("military", {}).get("players", {}).get("p1", {})
	assert_true(int(p1_diag.get("credits", 0)) > 0, "credits reflected in diagnostics")


func _find_collector(state: GameState) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "collector":
			return int(unit.get("id", 0))
	return 0


func _find_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0
