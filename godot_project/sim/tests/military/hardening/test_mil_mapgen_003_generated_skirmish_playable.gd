extends "res://sim/tests/test_case.gd"

## MIL-MAPGEN-003: a generated skirmish scenario is playable (hubs, ticks, commands).


func get_name() -> String:
	return "test_MIL_MAPGEN_003_generated_skirmish_playable"


func _run_body() -> void:
	var scenario: Dictionary = MilitarySkirmish.build_scenario(11, 2, 24, 24)
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	assert_eq(MilitaryState.all_players(state).size(), 2, "two players created")
	assert_eq(_hub_count(state, "p1"), 1, "p1 has command hub")
	assert_eq(_hub_count(state, "p2"), 1, "p2 has command hub")
	assert_true(MilitaryState.all_resource_fields(state).size() >= 1, "resource fields placed")

	state = runner.run_ticks(state, 30)

	var spawn: Dictionary = scenario["spawns"][0]
	var placed: bool = false
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var hub_q: int = int(spawn.get("q", 0))
	var hub_r: int = int(spawn.get("r", 0))
	for radius in range(1, 10):
		for dq in range(-radius, radius + 1):
			for dr in range(-radius, radius + 1):
				if absi(dq) + absi(dr) != radius:
					continue
				var q: int = hub_q + dq
				var r: int = hub_r + dr
				var check: Dictionary = MilitaryMapRules.can_place_building(state, "radar_tower", q, r, "p1")
				if not check.get("ok", false):
					continue
				var res: CommandResult = validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "radar_tower", "q": q, "r": r}))
				if res.success:
					placed = true
					break
			if placed:
				break
		if placed:
			break
	assert_true(placed, "placement works on generated map within build radius")

	var diag: Dictionary = state.diagnostics.get("military", {})
	assert_true(diag.has("players"), "diagnostics available after skirmish ticks")


func _hub_count(state: GameState, player_id: String) -> int:
	var n: int = 0
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) == player_id and str(building.get("building_type", "")) == "command_hub":
			n += 1
	return n
