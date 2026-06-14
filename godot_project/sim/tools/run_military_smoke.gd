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
	print("MIL-SMOKE-001 PENDING: base build not yet implemented")
	return false


func _run_smoke_003(_seed_value: int) -> bool:
	print("MIL-SMOKE-003 PENDING: power management not yet implemented")
	return false


func _run_smoke_004(_seed_value: int) -> bool:
	print("MIL-SMOKE-004 PENDING: tech unlock not yet implemented")
	return false


func _run_smoke_005(_seed_value: int) -> bool:
	print("MIL-SMOKE-005 PENDING: scouting not yet implemented")
	return false


func _run_smoke_006(_seed_value: int) -> bool:
	print("MIL-SMOKE-006 PENDING: combat not yet implemented")
	return false


func _run_smoke_007(_seed_value: int) -> bool:
	print("MIL-SMOKE-007 PENDING: repair not yet implemented")
	return false


func _run_smoke_008(_seed_value: int) -> bool:
	print("MIL-SMOKE-008 PENDING: win/loss not yet implemented")
	return false


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
