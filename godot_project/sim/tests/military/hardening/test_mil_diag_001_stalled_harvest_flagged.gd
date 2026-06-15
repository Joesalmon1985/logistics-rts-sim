extends "res://sim/tests/test_case.gd"

## MIL-DIAG-001: stalled harvest loop surfaces idle collectors in diagnostics.


func get_name() -> String:
	return "test_MIL_DIAG_001_stalled_harvest_flagged"


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
				"units": [{"type": "collector", "q": 8, "r": 8, "count": 1}],
			},
		],
		"resource_fields": [{"q": 10, "r": 10, "field_type": "mineral_field"}],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var collector_id: int = _find_collector(state)

	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": _find_field(state)}))
	state = runner.run_ticks(state, 40)

	assert_eq(MilitaryState.player_credits(state, "p1"), 0, "no credits without processor")
	var collector: Dictionary = MilitaryState.get_unit(state, collector_id)
	assert_eq(str(collector.get("state", "")), MilitaryConstants.UNIT_IDLE, "collector idle without processor loop")

	var p1_diag: Dictionary = state.diagnostics.get("military", {}).get("players", {}).get("p1", {})
	assert_true(int(p1_diag.get("idle_units", 0)) >= 1, "idle collector flagged in diagnostics")
	assert_eq(int(p1_diag.get("credits", -1)), 0, "zero credits reflected in diagnostics")


func _find_collector(state: GameState) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == "collector":
			return int(unit.get("id", 0))
	return 0


func _find_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0
