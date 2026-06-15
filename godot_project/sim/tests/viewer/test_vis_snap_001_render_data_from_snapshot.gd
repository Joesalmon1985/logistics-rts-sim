extends TestCase

## VIS-SNAP-001 — viewer render data from snapshot only.


func get_name() -> String:
	return "test_VIS_SNAP_001_render_data_from_snapshot"


func _run_body() -> void:
	_test_economy_snapshot()
	_test_military_snapshot()
	_test_invalid_schema()


func _test_economy_snapshot() -> void:
	var state: GameState = EconomyDemo.build_state()
	var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, "p1")
	var parsed: Dictionary = SnapshotLoader.parse(snapshot)
	assert_true(parsed.get("ok", false), "economy snapshot parse ok")
	var render: Dictionary = LayerRenderer.build_render_data(parsed.get("snapshot", {}))
	assert_true(render.get("terrain", []).size() > 0, "economy terrain layer")
	assert_true(render.get("buildings", []).size() > 0, "economy buildings layer")
	var counts: Dictionary = LayerRenderer.count_entities(render)
	assert_true(int(counts.get("buildings", 0)) > 0, "economy building count")
	assert_true(int(counts.get("carriers", 0)) >= 1, "economy carrier count")
	assert_true(int(counts.get("tree_density", 0)) >= 1, "economy tree_density layer")


func _test_military_snapshot() -> void:
	var state: GameState = MilitaryDemo.build_state()
	var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, "p1")
	var parsed: Dictionary = SnapshotLoader.parse(snapshot)
	assert_true(parsed.get("ok", false), "military snapshot parse ok")
	var render: Dictionary = LayerRenderer.build_render_data(parsed.get("snapshot", {}))
	assert_true(render.get("terrain", []).size() > 0, "military terrain layer")
	assert_true(render.get("buildings", []).size() > 0, "military buildings layer")


func _test_invalid_schema() -> void:
	var bad: Dictionary = {"schema_version": 999, "variant": "economy_trial"}
	var parsed: Dictionary = SnapshotLoader.parse(bad)
	assert_false(parsed.get("ok", true), "invalid schema rejected")
