extends "res://sim/tests/test_case.gd"

## MIL-PROD-001: a queued unit consumes credits, completes after build_ticks, and
## spawns; with a rally point set the new unit is ordered to move toward it.


func get_name() -> String:
	return "test_MIL_PROD_001_queue_spawns_at_rally"


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
					{"type": "power_generator", "q": 2, "r": 8},
				],
			},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var barracks_id: int = _building_of_type(state, "barracks")
	var validator := CommandValidator.new(CommandRegistry.create_default())

	validator.validate_and_apply(state, SetRallyPointCommand.new("p1", 0, {"building_id": barracks_id, "q": 12, "r": 12}))
	var credits_before: int = MilitaryState.player_credits(state, "p1")
	var cost: int = int(MilitaryState.get_config(state).get_unit_def("rifle_squad").get("credit_cost", 0))
	var res: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_true(res.success, "queue accepted")
	assert_eq(MilitaryState.player_credits(state, "p1"), credits_before - cost, "credits spent on queue")

	state = runner.run_ticks(state, 25)
	var unit_id: int = _first_unit_of_type(state, "rifle_squad")
	assert_true(unit_id > 0, "rifle squad spawned")

	var produced_event: bool = false
	for event in state.event_log.events:
		if event.type == "production_completed":
			produced_event = true
	assert_true(produced_event, "production_completed emitted")

	# Rally point => unit has a move order toward (12,12).
	var unit: Dictionary = MilitaryState.get_unit(state, unit_id)
	var order: Dictionary = unit.get("order", {})
	assert_eq(str(order.get("type", "")), MilitaryConstants.ORDER_MOVE, "spawned unit moves to rally")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0


func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
