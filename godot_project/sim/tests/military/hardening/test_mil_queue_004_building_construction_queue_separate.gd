extends "res://sim/tests/test_case.gd"

## MIL-QUEUE-004: building construction sites are separate from unit production queues.


func get_name() -> String:
	return "test_MIL_QUEUE_004_building_construction_queue_separate"


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
	validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "radar_tower", "q": 10, "r": 6}))
	validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "scout"}))

	assert_eq(MilitaryState.all_construction_sites(state).size(), 1, "construction site active")
	var queue: Dictionary = MilitaryState.get_production_queue(state, barracks_id)
	assert_eq(queue.get("items", []).size(), 1, "production queue has one item")

	state = runner.run_ticks(state, 5)
	assert_true(MilitaryState.all_construction_sites(state).size() >= 1, "construction still in progress")
	assert_true(MilitaryState.get_production_queue(state, barracks_id).get("items", []).size() >= 1, "production still queued")

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_true(snapshot.get("construction_sites", []).size() >= 1, "construction in snapshot")
	assert_true(snapshot.get("production_queues", []).size() >= 1, "production queues in snapshot")


func _building_of_type(state: GameState, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
