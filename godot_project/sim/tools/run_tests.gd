extends SceneTree

## Headless test runner for the authoritative simulation core (DDR-032).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

const TEST_SCRIPTS: Array[String] = [
	"res://sim/tests/kernel/test_run01_kernel_001_create_game_state.gd",
	"res://sim/tests/kernel/test_run01_kernel_002_seeded_rng_reproducible.gd",
	"res://sim/tests/kernel/test_run01_kernel_003_command_result_shape.gd",
	"res://sim/tests/kernel/test_run01_kernel_004_invalid_command_does_not_mutate_state.gd",
	"res://sim/tests/kernel/test_run01_kernel_005_event_serialization.gd",
	"res://sim/tests/kernel/test_run01_kernel_006_snapshot_json_shape.gd",
	"res://sim/tests/kernel/test_run01_kernel_007_replay_hash_reproducible.gd",
	"res://sim/tests/hex/test_run01_hex_001_hex_has_six_neighbours.gd",
	"res://sim/tests/hex/test_run01_hex_002_hex_distance.gd",
	"res://sim/tests/hex/test_run01_hex_003_seeded_map_reproducible.gd",
	"res://sim/tests/variants/test_run01_variant_001_all_variants_register.gd",
	"res://sim/tests/variants/test_run01_variant_002_all_variants_smoke_headless.gd",
	"res://sim/tests/determinism/test_run01_determinism_001_no_float_in_hashed_state_fixture.gd",
]


func _initialize() -> void:
	BootstrapScript.load_all()
	var expected: int = TEST_SCRIPTS.size()
	var passed: int = 0
	var failed: int = 0
	var errored: int = 0
	var ran: int = 0
	print("RUN_01 headless test harness — %d tests expected" % expected)

	for script_path in TEST_SCRIPTS:
		var script: Script = load(script_path)
		if script == null:
			errored += 1
			ran += 1
			print("ERROR %s (could not load script)" % script_path)
			continue

		if not script.can_instantiate():
			errored += 1
			ran += 1
			print("ERROR %s (script cannot instantiate)" % script_path)
			continue

		var test_instance: Object = script.new()
		if test_instance == null:
			errored += 1
			ran += 1
			print("ERROR %s (could not instantiate)" % script_path)
			continue

		var test_name: String = script_path.get_file()
		if test_instance.has_method("get_name"):
			test_name = str(test_instance.call("get_name"))

		test_instance.call("run")
		ran += 1

		var completed: bool = true
		if test_instance.has_method("run_completed"):
			completed = bool(test_instance.call("run_completed"))

		if not completed:
			errored += 1
			print("ERROR %s — run did not complete (possible script error)" % test_name)
			continue

		var ok: bool = bool(test_instance.call("passed"))
		if ok:
			passed += 1
			print("PASS %s" % test_name)
		else:
			failed += 1
			print("FAIL %s — %s" % [test_name, str(test_instance.call("failure_message"))])

	var exit_code: int = 0
	if failed > 0 or errored > 0 or ran != expected:
		exit_code = 1

	print("—")
	print(
		"Results: %d/%d ran, %d passed, %d failed, %d errored"
		% [ran, expected, passed, failed, errored]
	)
	if ran != expected:
		print("ERROR: expected %d tests but ran %d" % [expected, ran])
	if exit_code == 0:
		print("EXIT: 0 (pass)")
	else:
		print("EXIT: 1 (fail)")
	call_deferred("quit", exit_code)
