extends "res://sim/tests/test_case.gd"

## MIL-POWER-001/003: when demand exceeds supply the player is underpowered and
## unit production is blocked; restoring supply re-enables production.


func get_name() -> String:
	return "test_MIL_POWER_001_underpowered_blocks_production"


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
	var barracks_id: int = _building_of_type(state, "barracks")

	# One tick to recompute power: hub supply 50 < demand (20+30+30)=80 -> deficit.
	state = runner.run_ticks(state, 1)
	var player: Dictionary = MilitaryState.get_player(state, "p1")
	assert_eq(int(player.get("power_demand", 0)), 80, "demand summed")
	assert_eq(int(player.get("power_supply", 0)), 50, "supply from hub")
	assert_eq(int(player.get("powered", -1)), 0, "player underpowered")

	# Production command rejected while unpowered.
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_false(blocked.success, "production blocked while unpowered")
	assert_eq(blocked.error_code, MilitaryConstants.ERR_UNPOWERED)

	# Add a generator (supply 100) -> total 150 >= 80 -> powered.
	MilitaryConstructionSystem.spawn_building(state, "power_generator", 2, 8, "p1", MilitaryState.get_config(state))
	state = runner.run_ticks(state, 1)
	assert_eq(int(MilitaryState.get_player(state, "p1").get("powered", -1)), 1, "power restored")

	# Now production is accepted and a unit eventually spawns.
	var ok: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	assert_true(ok.success, "production accepted once powered")
	state = runner.run_ticks(state, 30)
	assert_true(_count_units_of_type(state, "rifle_squad") >= 1, "rifle squad produced")


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
