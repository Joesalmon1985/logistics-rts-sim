class_name TestCase
extends RefCounted

var _failures: Array[String] = []
var _run_completed: bool = false


func get_name() -> String:
	return "unnamed_test"


func run() -> void:
	_run_completed = false
	_failures.clear()
	var body_state := {"done": false}
	_invoke_run_body(body_state)
	if body_state.get("done", false):
		_run_completed = true
	else:
		_failures.append("test body aborted before completion (possible script error)")


func _invoke_run_body(body_state: Dictionary) -> void:
	_run_body()
	body_state["done"] = true


func _run_body() -> void:
	pass


func run_completed() -> bool:
	return _run_completed


func assert_true(condition: bool, message: String = "expected true") -> void:
	if not condition:
		_failures.append(message)


func assert_false(condition: bool, message: String = "expected false") -> void:
	if condition:
		_failures.append(message)


func assert_eq(actual: Variant, expected: Variant, message: String = "") -> void:
	if actual != expected:
		var detail: String = message if message != "" else "values not equal"
		_failures.append("%s (expected=%s actual=%s)" % [detail, str(expected), str(actual)])


func assert_not_null(value: Variant, message: String = "expected non-null") -> void:
	if value == null:
		_failures.append(message)


func assert_has_keys(data: Dictionary, keys: Array, message: String = "") -> void:
	for key in keys:
		if not data.has(key):
			var detail: String = message if message != "" else "missing key"
			_failures.append("%s: %s" % [detail, str(key)])


func passed() -> bool:
	return _failures.is_empty() and _run_completed


func failure_message() -> String:
	if not _run_completed:
		return "run did not complete (possible script error)"
	return "; ".join(_failures)
