extends "res://sim/tests/test_case.gd"

## MIL-QUEUE-005: production queues stall when the producer is unpowered.


func get_name() -> String:
	return "test_MIL_QUEUE_005_unpowered_queue_blocked"


func _run_body() -> void:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 4,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 50000,
		"fog_enabled": 0,
		"spawns": [
			{
				"player_id": "p1", "q": 2, "r": 2,
				"buildings": [
					{"type": "barracks", "q": 6, "r": 2},
					{"type": "vehicle_plant", "q": 9, "r": 2},
					{"type": "research_lab", "q": 12, "r": 2},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	var barracks_id: int = _building_of_type(state, "barracks")

	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_false(blocked.success, "cannot queue while unpowered")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_UNPOWERED)

	MilitaryConstructionSystem.spawn_building(state, "power_generator", 2, 8, "p1", MilitaryState.get_config(state))
	state = runner.run_ticks(state, 1)
	validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))

	# Remove power again by disabling generator.
	var gen: Dictionary = MilitaryState.get_building(state, _building_of_type(state, "power_generator"))
	gen["enabled"] = 0
	state = runner.run_ticks(state, 5)

	var queue: Dictionary = MilitaryState.get_production_queue(state, barracks_id)
	assert_eq(str(queue.get("blocked_reason", "")), MilitaryConstants.ERR_UNPOWERED, "queue blocked reason set")
	assert_eq(_count_units_of_type(state, "rifle_squad"), 0, "no unit spawned while blocked")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0


func _count_units_of_type(state: GameState, unit_type: String) -> int:
	var n: int = 0
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			n += 1
	return n
