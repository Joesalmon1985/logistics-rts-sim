extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_003_action_mask_shape"


func _run_body() -> void:
	var state := Helper.load_4p_state(5)
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, "p1")
	var mask: PackedInt32Array = HybridAiActionSpace.legal_mask(state, "p1", snap)
	assert_eq(mask.size(), 16, "mask size")
