extends TestCase

## VIS-ROLL-001 / VIS-ROLL-001b — rollout schema and alignment.


func get_name() -> String:
	return "test_VIS_ROLL_001_rollout_schema_alignment"


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _run_body() -> void:
	var loaded: Dictionary = RolloutLoader.load_folder(FIXTURE)
	assert_true(loaded.get("ok", false), "fixture loads: %s" % str(loaded.get("error", "")))
	assert_eq(int(loaded.get("command_count", -1)), 3, "command count")
	assert_eq(int(loaded.get("snapshot_count", -1)), 4, "snapshot count")
	var align: Dictionary = RolloutLoader.validate_alignment(3, 4)
	assert_true(align.get("ok", false), "alignment rule N+1")
	var bad: Dictionary = RolloutLoader.validate_alignment(3, 3)
	assert_false(bad.get("ok", true), "misalignment detected")
