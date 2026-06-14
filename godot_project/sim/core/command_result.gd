class_name CommandResult
extends RefCounted

var success: bool = false
var error_code: String = ""
var message: String = ""
var events: Array = []
var state_changed: bool = false


static func ok(
	changed: bool = false,
	event_ids: Array = [],
	msg: String = "",
) -> CommandResult:
	var result := CommandResult.new()
	result.success = true
	result.error_code = ""
	result.message = msg
	result.events = event_ids.duplicate()
	result.state_changed = changed
	return result


static func fail(code: String, msg: String = "") -> CommandResult:
	var result := CommandResult.new()
	result.success = false
	result.error_code = code
	result.message = msg if msg != "" else code
	result.events = []
	result.state_changed = false
	return result


func to_dict() -> Dictionary:
	return {
		"success": success,
		"error_code": error_code,
		"message": message,
		"events": events.duplicate(),
		"state_changed": state_changed,
		"accepted": success,
		"reason": null if success else (error_code if error_code != "" else message),
	}
