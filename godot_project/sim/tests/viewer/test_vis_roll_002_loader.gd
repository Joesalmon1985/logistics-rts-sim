extends TestCase

## VIS-ROLL-002 — RolloutLoader loads metadata and snapshot sequence.


func get_name() -> String:
	return "test_VIS_ROLL_002_rollout_loader"


const FIXTURE := "res://sim/tests/fixtures/visual_rollouts/minimal_economy"


func _run_body() -> void:
	var loaded: Dictionary = RolloutLoader.load_folder(FIXTURE)
	assert_true(loaded.get("ok", false), str(loaded.get("error", "")))
	var metadata: Dictionary = loaded.get("metadata", {})
	assert_eq(int(metadata.get("schema_version", 0)), 1, "schema version")
	assert_true(metadata.has("policy_type"), "policy_type present")
	var snap0: Dictionary = RolloutLoader.load_snapshot_at(FIXTURE, 0)
	assert_true(snap0.get("ok", false), "snapshot 0 loads")
	assert_true(snap0.get("snapshot", {}).has("variant"), "snapshot has variant")
