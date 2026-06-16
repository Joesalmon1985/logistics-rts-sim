extends TestCase

## RUN_02.5b — eval_build_obvious declares worker_policy victory (intended design).


func get_name() -> String:
	return "test_eval_build_obvious_worker_policy_victory"


func _run_body() -> void:
	var scenario_path := "res://sim/data/scenarios/economy/eval_build_obvious.json"
	var text: String = FileAccess.get_file_as_string(scenario_path)
	assert_false(text.is_empty(), "scenario readable")
	var parsed: Variant = JSON.parse_string(text)
	assert_true(typeof(parsed) == TYPE_DICTIONARY, "scenario json")
	var victory: Dictionary = parsed.get("victory", {})
	assert_eq(str(victory.get("type", "")), "worker_policy", "victory type documented")
	assert_eq(str(victory.get("policy", "")), "auto", "expected worker policy")
