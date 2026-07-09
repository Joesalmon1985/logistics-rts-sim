extends TestCase

const MANIFEST := "res://../docs/audit/neural_hybrid_ai/training/latest_checkpoint_manifest.json"
const GODOT_CKPT := "res://sim/hybrid/ai/checkpoints/latest_modular_policy.json"


func get_name() -> String:
	return "test_neural_hybrid_050_checkpoint_manifest_exists"


func _run_body() -> void:
	assert_true(FileAccess.file_exists(GODOT_CKPT), "godot checkpoint json")
	var manifest_path: String = ProjectSettings.globalize_path(MANIFEST)
	assert_true(FileAccess.file_exists(manifest_path), "manifest")
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))
	assert_true(parsed is Dictionary, "manifest parse")
	var stages: Array = parsed.get("meta", {}).get("trained_stages", [])
	assert_true(stages.size() >= 5, "trained stages recorded")
