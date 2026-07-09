extends TestCase

const Helper := preload("res://sim/tests/neural_hybrid/neural_hybrid_test_helper.gd")


func get_name() -> String:
	return "test_neural_hybrid_044_arbiter_selects_only_legal_candidate"


func _run_body() -> void:
	var state := Helper.load_4p_state(5)
	var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, "p1")
	var cands: Array = ExpertCandidateProposer.propose_all(state, "p1", snap, "expand")
	var mask: PackedInt32Array = HybridAiActionSpace.legal_mask(state, "p1", snap)
	var pick: Dictionary = ModularArbiter.choose(cands, mask, "expand")
	assert_true(pick.has("chosen_macro_action"), "arbiter pick")
