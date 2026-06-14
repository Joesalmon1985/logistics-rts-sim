extends SceneTree

## Headless smoke runner for KERN-SMOKE-001 through KERN-SMOKE-004.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const DEFAULT_TICKS: int = 10
const DEFAULT_SEED: int = 1


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var variant_id: String = "economy_trial"
	var ticks: int = DEFAULT_TICKS
	var seed_value: int = DEFAULT_SEED
	var scenario_path: String = "scenarios/kernel_smoke.json"
	var check_hash: bool = true
	var expected_hash_override: String = ""

	for arg in args:
		if arg.begins_with("--variant="):
			variant_id = arg.substr("--variant=".length())
		elif arg.begins_with("--ticks="):
			ticks = int(arg.substr("--ticks=".length()))
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--scenario="):
			scenario_path = arg.substr("--scenario=".length())
		elif arg.begins_with("--expected-hash="):
			expected_hash_override = arg.substr("--expected-hash=".length())
			check_hash = true
		elif arg == "--no-hash-check":
			check_hash = false

	if ticks <= 0:
		_smoke_fail("invalid --ticks=%d (must be > 0)" % ticks)
		return

	if seed_value < 0:
		_smoke_fail("invalid --seed=%d (must be >= 0)" % seed_value)
		return

	var registry := VariantRegistry.create_default()
	if not registry.has_variant(variant_id):
		_smoke_fail("unknown variant '%s'" % variant_id)
		return

	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	if scenario.is_empty():
		_smoke_fail("could not load scenario %s" % scenario_path)
		return

	scenario["variant"] = variant_id
	scenario["seed"] = seed_value
	if expected_hash_override != "":
		scenario["expected_hash"] = expected_hash_override

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var game_created: bool = false
	for event in state.event_log.events:
		if event.type == "game_created":
			game_created = true
			break
	if not game_created:
		_smoke_fail("KERN-SMOKE-001 game_created event missing")
		return
	print("KERN-SMOKE-001 PASS: game_created")

	runner.submit_command(state, NoopCommand.new("p1", state.tick))
	state = runner.run_ticks(state, ticks)

	var tick_advanced_count: int = 0
	var economy_initialized: bool = false
	for event in state.event_log.events:
		if event.type == "tick_advanced":
			tick_advanced_count += 1
		if event.type == "economy_initialized":
			economy_initialized = true
	if variant_id == "economy_trial":
		if not economy_initialized:
			_smoke_fail("KERN-SMOKE-002 economy_initialized event missing for economy_trial")
			return
		if state.tick != ticks:
			_smoke_fail("KERN-SMOKE-002 expected tick=%d got %d" % [ticks, state.tick])
			return
		print("KERN-SMOKE-002 PASS: economy_initialized and tick=%d" % state.tick)
	elif tick_advanced_count < ticks:
		_smoke_fail(
			"KERN-SMOKE-002 expected %d tick_advanced, got %d" % [ticks, tick_advanced_count]
		)
		return
	else:
		print("KERN-SMOKE-002 PASS: %d tick_advanced events" % tick_advanced_count)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if int(snapshot.get("schema_version", 0)) != 1:
		_smoke_fail("KERN-SMOKE-003 invalid schema_version")
		return
	var parsed: Variant = JSON.parse_string(JSON.stringify(snapshot))
	if typeof(parsed) != TYPE_DICTIONARY:
		_smoke_fail("KERN-SMOKE-003 snapshot not JSON-compatible")
		return
	print("KERN-SMOKE-003 PASS: valid snapshot JSON schema_version=1")

	var hash_value: String = ReplayHash.compute_state_hash(state)
	print("Replay hash: %s" % hash_value)

	var expected_hash: Variant = scenario.get("expected_hash")
	if check_hash and expected_hash != null and str(expected_hash) != "null" and str(expected_hash) != "":
		if not ReplayHash.matches(str(expected_hash), hash_value):
			_smoke_fail("KERN-SMOKE-004 hash mismatch")
			print("  expected: %s" % str(expected_hash))
			print("  actual:   %s" % hash_value)
			return
		print("KERN-SMOKE-004 PASS: replay hash match")
	else:
		print("KERN-SMOKE-004 SKIP: expected_hash not set (record hash above)")

	print("SMOKE PASS variant=%s ticks=%d seed=%d" % [variant_id, ticks, seed_value])
	print("EXIT: 0 (pass)")
	call_deferred("quit", 0)


func _smoke_fail(reason: String) -> void:
	print("SMOKE FAIL: %s" % reason)
	print("EXIT: 1 (fail)")
	call_deferred("quit", 1)
