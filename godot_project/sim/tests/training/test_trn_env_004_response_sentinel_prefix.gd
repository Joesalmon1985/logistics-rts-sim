extends "res://sim/tests/test_case.gd"

const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")


func get_name() -> String:
	return "test_TRN_ENV_004_response_sentinel_prefix"


func _run_body() -> void:
	SimBootstrap.load_all()
	var formatted: String = TrainingEnvServerScript.format_response({"ok": true, "test": 1})
	assert_true(formatted.begins_with("@@RUN06_JSON@@"), "sentinel prefix")
	var json_text: String = formatted.substr("@@RUN06_JSON@@".length())
	var parsed: Variant = JSON.parse_string(json_text)
	assert_true(typeof(parsed) == TYPE_DICTIONARY, "valid json payload")
	assert_true((parsed as Dictionary).get("ok", false), "payload ok")
