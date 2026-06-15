extends "res://sim/tests/test_case.gd"

## MIL-DAMAGE-002: armour class and damage-type modifiers apply deterministically.


func get_name() -> String:
	return "test_MIL_DAMAGE_002_armour_modifier_applied"


func _run_body() -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(GameState.new())
	var modifier: int = cfg.damage_modifier("small_arms", "vehicle")
	assert_eq(modifier, 75, "small_arms vs vehicle modifier is 75%")

	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": 11,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 5, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "light_vehicle", "q": 7, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var attacker_id: int = _first_unit_of(state, "p1", "rifle_squad")
	var target_id: int = _first_unit_of(state, "p2", "light_vehicle")
	var hp_before: int = int(MilitaryState.get_unit(state, target_id).get("hp", 0))

	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": attacker_id, "target_id": target_id}))
	state = runner.run_ticks(state, 1)

	var expected_damage: int = 15 * modifier / 100
	assert_eq(int(MilitaryState.get_unit(state, target_id).get("hp", -1)), hp_before - expected_damage, "armour modifier applied to rifle vs vehicle")

	var saw_damaged: bool = false
	for event in state.event_log.events:
		if event.type == "unit_damaged":
			saw_damaged = true
	assert_true(saw_damaged, "unit_damaged event emitted")


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0
