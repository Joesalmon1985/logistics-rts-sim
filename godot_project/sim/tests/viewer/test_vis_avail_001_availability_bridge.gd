extends TestCase

## VIS-AVAIL-001 — sim-owned availability bridge; excluded from replay hash path.


func get_name() -> String:
	return "test_VIS_AVAIL_001_availability_bridge"


func _run_body() -> void:
	_test_economy_availability()
	_test_hash_exclusion()


func _test_economy_availability() -> void:
	var state: GameState = EconomyDemo.build_state()
	var from_provider: Array = CommandAvailabilityProvider.list_available_commands(state, "p1")
	var from_client: Array = ClientCommandAvailability.list_from_state(state, "p1")
	assert_eq(from_client.size(), from_provider.size(), "client count matches provider")
	assert_true(from_provider.size() >= 1, "provider returns commands")
	for cmd in from_provider:
		assert_has_keys(cmd, ["id", "label", "type", "actor_id", "payload", "enabled", "source"])
		assert_eq(str(cmd.get("source", "")), "sim")
	# Deterministic: two calls match
	var again: Array = CommandAvailabilityProvider.list_available_commands(state, "p1")
	assert_eq(from_provider.size(), again.size())
	if not from_provider.is_empty():
		assert_eq(str(from_provider[0].get("id", "")), str(again[0].get("id", "")))


func _test_hash_exclusion() -> void:
	var state: GameState = EconomyDemo.build_state()
	var hash_before: String = ReplayHash.compute_state_hash(state)
	var debug_snap: Dictionary = SnapshotExporter.export_debug_snapshot(state, "p1")
	assert_true(debug_snap.has("available_commands"), "debug snapshot has availability")
	var hash_after: String = ReplayHash.compute_state_hash(state)
	assert_eq(hash_before, hash_after, "debug export did not change replay hash")
	var base_snap: Dictionary = SnapshotExporter.export_snapshot(state)
	assert_false(base_snap.has("available_commands"), "base export excludes availability")
