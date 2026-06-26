class_name SelectionInspectorPanel
extends PanelContainer

## Permanent right-side inspector for Settler MVP selections.

@onready var _label: RichTextLabel = %InspectorText


func _ready() -> void:
	if _label == null:
		_label = find_child("InspectorText", true, false) as RichTextLabel


func show_selection(selection: Dictionary, snapshot: Dictionary, mode_context: Dictionary = {}) -> void:
	var text: String = SelectionFormatter.format(selection, snapshot, mode_context)
	set_text(text)


func set_text(text: String) -> void:
	if _label != null:
		_label.text = text


func get_text() -> String:
	return _label.text if _label != null else ""
