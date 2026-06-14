extends SceneTree

## Headless military smoke runner — MIL-SMOKE-001 through MIL-SMOKE-009.
## See docs/testing/02_smoke_test_matrix.md.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const VALID_SMOKES: Array[String] = ["all", "001", "002", "003", "004", "005", "006", "007", "008", "009"]


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	var seed_value: int = 1
	var export_dir: String = ""
	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--export-dir="):
			export_dir = arg.substr("--export-dir=".length())

	# Normalise MIL-SMOKE-00X form to 00X.
	smoke_id = smoke_id.replace("MIL-SMOKE-", "")

	if smoke_id not in VALID_SMOKES:
		print("MIL SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = _check(_run_smoke_001(), failed)
	if smoke_id == "all" or smoke_id == "002":
		failed = _check(_run_smoke_002(seed_value), failed)
	if smoke_id == "all" or smoke_id == "003":
		failed = _check(_run_smoke_003(seed_value), failed)
	if smoke_id == "all" or smoke_id == "004":
		failed = _check(_run_smoke_004(seed_value), failed)
	if smoke_id == "all" or smoke_id == "005":
		failed = _check(_run_smoke_005(seed_value), failed)
	if smoke_id == "all" or smoke_id == "006":
		failed = _check(_run_smoke_006(seed_value), failed)
	if smoke_id == "all" or smoke_id == "007":
		failed = _check(_run_smoke_007(seed_value), failed)
	if smoke_id == "all" or smoke_id == "008":
		failed = _check(_run_smoke_008(seed_value), failed)
	if smoke_id == "all" or smoke_id == "009":
		failed = _check(_run_smoke_009(seed_value, export_dir), failed)

	if failed:
		print("MIL SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("MIL SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _check(ok: bool, failed_so_far: bool) -> bool:
	return failed_so_far or not ok


# --- MIL-SMOKE-002: harvest / process credits -------------------------------

func _run_smoke_002(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": seed_value,
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
	var collector_id: int = _first_unit_of_type(state, "collector")
	var field_id: int = _first_field(state)
	if collector_id <= 0 or field_id <= 0:
		print("MIL-SMOKE-002 FAIL: missing collector or field")
		return false
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, HarvestResourceCommand.new("p1", 0, {"unit_id": collector_id, "field_id": field_id}))
	state = runner.run_ticks(state, 120)
	var credits: int = MilitaryState.player_credits(state, "p1")
	if credits <= 0:
		print("MIL-SMOKE-002 FAIL: no credits earned")
		return false
	print("MIL-SMOKE-002 PASS: collector->processor earned %d credits" % credits)
	return true


# --- Placeholders implemented in later RUN_03 phases -------------------------

func _run_smoke_001() -> bool:
	var scenario: Dictionary = _base_scenario(1)
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "power_generator", "q": 6, "r": 2}))
	validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "resource_processor", "q": 6, "r": 6}))
	validator.validate_and_apply(state, PlaceBuildingCommand.new("p1", 0, {"building_type": "barracks", "q": 9, "r": 2}))
	state = runner.run_ticks(state, 60)
	for required in ["command_hub", "power_generator", "resource_processor", "barracks"]:
		if not _has_active_building(state, required):
			print("MIL-SMOKE-001 FAIL: %s not active" % required)
			return false
	print("MIL-SMOKE-001 PASS: base built (hub+generator+processor+barracks)")
	return true


func _run_smoke_003(seed_value: int) -> bool:
	var scenario: Dictionary = _base_scenario(seed_value)
	scenario["spawns"][0]["buildings"] = [
		{"type": "barracks", "q": 6, "r": 2},
		{"type": "vehicle_plant", "q": 9, "r": 2},
		{"type": "research_lab", "q": 12, "r": 2},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var barracks_id: int = _building_of_type(state, "barracks")
	state = runner.run_ticks(state, 1)
	if int(MilitaryState.get_player(state, "p1").get("powered", -1)) != 0:
		print("MIL-SMOKE-003 FAIL: expected underpowered base")
		return false
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	if blocked.success:
		print("MIL-SMOKE-003 FAIL: production not blocked while unpowered")
		return false
	MilitaryConstructionSystem.spawn_building(state, "power_generator", 2, 8, "p1", MilitaryState.get_config(state))
	state = runner.run_ticks(state, 1)
	if int(MilitaryState.get_player(state, "p1").get("powered", -1)) != 1:
		print("MIL-SMOKE-003 FAIL: power not restored")
		return false
	validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": barracks_id, "unit_type": "rifle_squad"}))
	state = runner.run_ticks(state, 30)
	if _count_units_of_type(state, "rifle_squad") < 1:
		print("MIL-SMOKE-003 FAIL: production did not resume")
		return false
	print("MIL-SMOKE-003 PASS: unpowered blocked, restored, production resumed")
	return true


func _run_smoke_004(seed_value: int) -> bool:
	var scenario: Dictionary = _base_scenario(seed_value)
	scenario["spawns"][0]["buildings"] = [
		{"type": "vehicle_plant", "q": 6, "r": 2},
		{"type": "research_lab", "q": 10, "r": 2},
		{"type": "power_generator", "q": 2, "r": 8},
	]
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	var plant_id: int = _building_of_type(state, "vehicle_plant")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var blocked: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	if blocked.success:
		print("MIL-SMOKE-004 FAIL: tank should require research")
		return false
	validator.validate_and_apply(state, StartResearchCommand.new("p1", 0, {"tech_id": "advanced_weapons"}))
	var research_ticks: int = int(MilitaryState.get_config(state).get_tech_def("advanced_weapons").get("research_ticks", 60))
	state = runner.run_ticks(state, research_ticks + 2)
	if not MilitaryState.player_has_tech(state, "p1", "advanced_weapons"):
		print("MIL-SMOKE-004 FAIL: research not completed")
		return false
	var ok: CommandResult = validator.validate_and_apply(state, QueueUnitCommand.new("p1", 0, {"building_id": plant_id, "unit_type": "tank"}))
	if not ok.success:
		print("MIL-SMOKE-004 FAIL: tank still blocked after research")
		return false
	state = runner.run_ticks(state, 60)
	if _count_units_of_type(state, "tank") < 1:
		print("MIL-SMOKE-004 FAIL: tank not produced")
		return false
	print("MIL-SMOKE-004 PASS: research unlocked and tank produced")
	return true


func _run_smoke_005(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": seed_value,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 1,
		"spawns": [
			{"player_id": "p1", "q": 2, "r": 2, "units": [{"type": "scout", "q": 3, "r": 3}]},
			{"player_id": "p2", "q": 13, "r": 13},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, 1)
	if MilitaryState.intel_level(state, "p1", 13, 13) != MilitaryConstants.INTEL_UNEXPLORED:
		print("MIL-SMOKE-005 FAIL: enemy base should start unexplored")
		return false
	var scout_id: int = _first_unit_of_type(state, "scout")
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": scout_id, "q": 11, "r": 11}))
	state = runner.run_ticks(state, 120)
	if MilitaryState.intel_level(state, "p1", 13, 13) != MilitaryConstants.INTEL_VISIBLE:
		print("MIL-SMOKE-005 FAIL: scout did not reveal enemy base")
		return false
	print("MIL-SMOKE-005 PASS: scout revealed enemy base")
	return true


func _run_smoke_006(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": seed_value,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rifle_squad", "q": 5, "r": 5}]},
			{"player_id": "p2", "q": 14, "r": 14, "units": [{"type": "scout", "q": 7, "r": 5}]},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var attacker_id: int = _first_unit_of(state, "p1", "rifle_squad")
	var target_id: int = _first_unit_of(state, "p2", "scout")
	if attacker_id <= 0 or target_id <= 0:
		print("MIL-SMOKE-006 FAIL: missing attacker or target")
		return false
	var validator := CommandValidator.new(CommandRegistry.create_default())
	validator.validate_and_apply(state, MoveUnitCommand.new("p1", 0, {"unit_id": attacker_id, "q": 6, "r": 5}))
	state = runner.run_ticks(state, 20)
	validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": attacker_id, "target_id": target_id}))
	state = runner.run_ticks(state, 60)
	if not MilitaryState.get_unit(state, target_id).is_empty():
		print("MIL-SMOKE-006 FAIL: enemy unit not destroyed")
		return false
	print("MIL-SMOKE-006 PASS: moved to engage and destroyed enemy unit")
	return true


func _run_smoke_007(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": seed_value,
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
	unit["hp"] = 30
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var res: CommandResult = validator.validate_and_apply(state, RepairTargetCommand.new("p1", 0, {"target_id": unit_id}))
	if not res.success:
		print("MIL-SMOKE-007 FAIL: repair not accepted")
		return false
	state = runner.run_ticks(state, 30)
	var healed: Dictionary = MilitaryState.get_unit(state, unit_id)
	if int(healed.get("hp", 0)) != int(healed.get("max_hp", 0)):
		print("MIL-SMOKE-007 FAIL: unit not fully repaired (hp=%d)" % int(healed.get("hp", 0)))
		return false
	print("MIL-SMOKE-007 PASS: repair depot healed damaged unit")
	return true


func _run_smoke_008(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "military_trial",
		"seed": seed_value,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1", "p2"],
		"build_radius": 30,
		"starting_credits": 1000,
		"fog_enabled": 0,
		"spawns": [
			{"player_id": "p1", "q": 1, "r": 1, "units": [{"type": "rocket_squad", "q": 9, "r": 12, "count": 5}]},
			{"player_id": "p2", "q": 12, "r": 12},
		],
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var hub_id: int = _building_of_player_type(state, "p2", "command_hub")
	if hub_id <= 0:
		print("MIL-SMOKE-008 FAIL: missing enemy hub")
		return false
	var validator := CommandValidator.new(CommandRegistry.create_default())
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == "p1":
			validator.validate_and_apply(state, AttackUnitCommand.new("p1", 0, {"unit_id": int(unit.get("id", 0)), "target_id": hub_id}))
	state = runner.run_ticks(state, 250)
	if int(MilitaryState.get_meta_value(state, "victory_decided", 0)) != 1:
		print("MIL-SMOKE-008 FAIL: victory not decided")
		return false
	if str(MilitaryState.get_meta_value(state, "winner_player_id", "")) != "p1":
		print("MIL-SMOKE-008 FAIL: wrong winner")
		return false
	print("MIL-SMOKE-008 PASS: enemy hub destroyed, p1 wins")
	return true


func _run_smoke_009(_seed_value: int, _export_dir: String) -> bool:
	print("MIL-SMOKE-009 PENDING: replay/diagnostics not yet implemented")
	return false


# --- Helpers ----------------------------------------------------------------

func _first_unit_of_type(state: GameState, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0


func _first_field(state: GameState) -> int:
	for field in MilitaryState.all_resource_fields(state):
		return int(field.get("id", 0))
	return 0


func _base_scenario(seed_value: int) -> Dictionary:
	return {
		"variant": "military_trial",
		"seed": seed_value,
		"map_file": "maps/military/flat_16.json",
		"players": ["p1"],
		"build_radius": 30,
		"starting_credits": 50000,
		"fog_enabled": 0,
		"spawns": [{"player_id": "p1", "q": 2, "r": 2}],
	}


func _has_active_building(state: GameState, building_type: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("building_type", "")) == building_type and str(building.get("state", "")) == MilitaryConstants.BLD_ACTIVE:
			return true
	return false


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


func _first_unit_of(state: GameState, player_id: String, unit_type: String) -> int:
	for unit in MilitaryState.all_units(state):
		if str(unit.get("player_id", "")) == player_id and str(unit.get("unit_type", "")) == unit_type:
			return int(unit.get("id", 0))
	return 0


func _building_of_player_type(state: GameState, player_id: String, building_type: String) -> int:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) == player_id and str(building.get("building_type", "")) == building_type:
			return int(building.get("id", 0))
	return 0
