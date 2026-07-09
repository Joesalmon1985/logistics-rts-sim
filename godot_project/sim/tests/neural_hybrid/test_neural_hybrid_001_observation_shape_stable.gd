extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_001_observation_shape_stable"


func _run_body() -> void:
	var state := Helper.load_4p_state(10)
	var obs: Dictionary = HybridAiObservationExporter.export_observation(state, "p1")
	assert_eq(obs.get("vector", []).size(), 13, "vector size")
