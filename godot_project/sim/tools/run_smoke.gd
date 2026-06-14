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

	for arg in args:
		if arg.begins_with("--variant="):
			variant_id = arg.substr("--variant=".length())
		elif arg.begins_with("--ticks="):
			ticks = int(arg.substr("--ticks=".length()))
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--scenario="):
			scenario_path = arg.substr("--scenario=".length())
		elif arg == "--no-hash-check":
			check_hash = false

	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	if scenario.is_empty():
		print("SMOKE FAIL: could not load scenario %s" % scenario_path)
		call_deferred("quit", 1)
		return

	scenario["variant"] = variant_id
	scenario["seed"] = seed_value

	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)

	var game_created: bool = false
	for event in state.event_log.events:
		if event.type == "game_created":
			game_created = true
			break
	if not game_created:
		print("SMOKE FAIL: KERN-SMOKE-001 game_created event missing")
		call_deferred("quit", 1)
		return
	print("KERN-SMOKE-001 PASS: game_created")

	runner.submit_command(state, NoopCommand.new("p1", state.tick))
	state = runner.run_ticks(state, ticks)

	var tick_advanced_count: int = 0
	for event in state.event_log.events:
		if event.type == "tick_advanced":
			tick_advanced_count += 1
	if tick_advanced_count < ticks:
		print(
			"SMOKE FAIL: KERN-SMOKE-002 expected %d tick_advanced, got %d"
			% [ticks, tick_advanced_count]
		)
		call_deferred("quit", 1)
		return
	print("KERN-SMOKE-002 PASS: %d tick_advanced events" % tick_advanced_count)

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if int(snapshot.get("schema_version", 0)) != 1:
		print("SMOKE FAIL: KERN-SMOKE-003 invalid schema_version")
		call_deferred("quit", 1)
		return
	var parsed: Variant = JSON.parse_string(JSON.stringify(snapshot))
	if typeof(parsed) != TYPE_DICTIONARY:
		print("SMOKE FAIL: KERN-SMOKE-003 snapshot not JSON-compatible")
		call_deferred("quit", 1)
		return
	print("KERN-SMOKE-003 PASS: valid snapshot JSON schema_version=1")

	var hash_value: String = ReplayHash.compute_state_hash(state)
	print("Replay hash: %s" % hash_value)

	var expected_hash: Variant = scenario.get("expected_hash")
	if check_hash and expected_hash != null and str(expected_hash) != "null" and str(expected_hash) != "":
		if not ReplayHash.matches(str(expected_hash), hash_value):
			print("SMOKE FAIL: KERN-SMOKE-004 hash mismatch")
			print("  expected: %s" % str(expected_hash))
			print("  actual:   %s" % hash_value)
			call_deferred("quit", 1)
			return
		print("KERN-SMOKE-004 PASS: replay hash match")
	else:
		print("KERN-SMOKE-004 SKIP: expected_hash not set (record hash above)")

	print("SMOKE PASS variant=%s ticks=%d seed=%d" % [variant_id, ticks, seed_value])
	call_deferred("quit", 0)
