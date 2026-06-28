extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_040_expert_candidate_schema_stable"


func _run_body() -> void:
	var state := Helper.load_4p_state(5)
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, "p1")
	var cands: Array = ExpertCandidateProposer.propose_all(state, "p1", snap, "expand")
	assert_true(not cands.is_empty(), "candidates")
	var c0: Dictionary = cands[0]
	assert_has_keys(c0, ["candidate_id", "source_expert", "plain_english_reason"], "candidate schema")
