extends "res://sim/tests/test_case.gd"

## MIL-REPAIR-004: repair stalls when the depot loses power mid-job.


func get_name() -> String:
	return "test_MIL_REPAIR_004_unpowered_repair_stalled"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 61,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [
					{"type": "repair_depot", "q": 8, "r": 8},
					{"type": "power_generator", "q": 2, "r": 8},
				],
				"units": [{"type": "rifle_squad", "q": 9, "r": 8}],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)

	var unit_id: int = _first_unit_of_type(state, "rifle_squad")
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	unit["hp"] = 50

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, RepairTargetCommand.new("p1", 0, {"target_id": unit_id}))
	assert_true(res.success, "repair job accepted while powered")

	# Overload power so the repair depot becomes unpowered.
	MilitaryConstructionSystem.spawn_building(state, "barracks", 6, 2, "p1", MilitaryState.get_config(state))
	MilitaryConstructionSystem.spawn_building(state, "vehicle_plant", 9, 2, "p1", MilitaryState.get_config(state))
	MilitaryConstructionSystem.spawn_building(state, "research_lab", 12, 2, "p1", MilitaryState.get_config(state))
	var gen: Dictionary = MilitaryState.get_building(state, _building_of_type(state, "power_generator"))
	gen["enabled"] = 0
	state = runner.run_ticks(state, 15)

	assert_eq(int(MilitaryState.get_unit(state, unit_id).get("hp", 0)), 50, "repair did not progress while unpowered")
	assert_true(MilitaryState.all_repair_jobs(state).size() >= 1, "repair job still active")
	var job: Dictionary = MilitaryState.all_repair_jobs(state)[0]
	assert_eq(str(job.get("paused_reason", "")), MilitaryConstants.ERR_UNPOWERED, "repair job paused with unpowered reason")


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
