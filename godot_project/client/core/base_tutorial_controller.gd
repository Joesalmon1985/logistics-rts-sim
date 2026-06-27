class_name BaseTutorialController
extends RefCounted

## Shared tutorial navigation — client-side only; never mutates sim directly.

signal step_changed(step_index: int, step: Dictionary)
signal tutorial_completed()
signal tutorial_exited()

var enabled: bool = false
var steps: Array = []
var step_index: int = 0
var exited: bool = false


func current_step() -> Dictionary:
	if step_index < 0 or step_index >= steps.size():
		return {}
	return steps[step_index]


func advance_manual() -> void:
	_advance()


func retreat_manual() -> void:
	if step_index <= 0:
		return
	step_index -= 1
	step_changed.emit(step_index, current_step())


func skip_or_exit() -> void:
	exited = true
	tutorial_exited.emit()


func step_kind(step: Dictionary) -> String:
	var explicit: String = str(step.get("step_kind", ""))
	if explicit in ["reading", "action", "completion"]:
		return explicit
	var completion: String = str(step.get("completion", ""))
	if completion == "manual_next":
		return "reading"
	if completion in ["objective_complete", "victory"]:
		return "completion"
	return "action"


func step_kind_label(step: Dictionary) -> String:
	match step_kind(step):
		"reading":
			return "Read"
		"completion":
			return "Complete"
		_:
			return "Try this"


func next_button_enabled(step: Dictionary) -> bool:
	if step.is_empty():
		return false
	if str(step.get("completion", "")) == "manual_next":
		return true
	return bool(step.get("allow_manual_override", true))


func next_disabled_reason(step: Dictionary) -> String:
	if step.is_empty():
		return ""
	if next_button_enabled(step):
		return ""
	return str(step.get(
		"next_disabled_reason",
		"Complete the action above to continue, or click Next to skip ahead."
	))


func back_button_enabled() -> bool:
	return step_index > 0


func _advance() -> void:
	step_index += 1
	if step_index >= steps.size():
		tutorial_completed.emit()
		return
	step_changed.emit(step_index, current_step())
