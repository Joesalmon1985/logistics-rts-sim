class_name MilitaryBuildPalette
extends VBoxContainer

## Build and production buttons for Skirmish MVP.

signal build_selected(building_type: String)
signal queue_selected(unit_type: String)
signal cancel_selected()

const BUILD_TYPES: Array[String] = ["power_generator", "barracks", "resource_processor"]
const QUEUE_TYPES: Array[String] = ["rifle_squad"]

const TOOLTIPS: Dictionary = {
	"power_generator": "Power Generator — supplies power for your base.",
	"barracks": "Barracks — trains infantry squads.",
	"resource_processor": "Supply Depot — turns harvested minerals into credits.",
	"rifle_squad": "Queue Rifle Squad — requires an operational Barracks.",
	"cancel": "Cancel current build or queue mode (Esc).",
}

var _buttons: Dictionary = {}
var _active_build: String = ""
var _active_queue: String = ""


func ensure_built() -> void:
	if _buttons.is_empty():
		_build_ui()


func _ready() -> void:
	ensure_built()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()
	_buttons.clear()
	var row1 := HBoxContainer.new()
	row1.name = "Row1"
	add_child(row1)
	for btype in BUILD_TYPES:
		_add_btn(row1, btype, MilitaryCatalog.display_name(btype), "build")
	var row2 := HBoxContainer.new()
	row2.name = "Row2"
	add_child(row2)
	for utype in QUEUE_TYPES:
		_add_btn(row2, utype, "Queue %s" % MilitaryCatalog.display_name(utype), "queue")
	_add_btn(row2, "cancel", "Cancel", "cancel")


func _add_btn(parent: Node, key: String, label_text: String, kind: String) -> void:
	var btn := TextureButton.new()
	btn.name = "%sBtn" % key.capitalize()
	btn.custom_minimum_size = Vector2(88, 72)
	btn.ignore_texture_size = true
	btn.stretch_mode = TextureButton.STRETCH_KEEP_ASPECT_CENTERED
	btn.tooltip_text = str(TOOLTIPS.get(key, label_text))
	var tex: Texture2D = _texture_for(key)
	if tex != null:
		btn.texture_normal = tex
	var cap: Label = Label.new()
	cap.text = label_text
	cap.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	cap.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	cap.custom_minimum_size = Vector2(88, 0)
	var wrap := VBoxContainer.new()
	wrap.add_child(btn)
	wrap.add_child(cap)
	parent.add_child(wrap)
	_buttons[key] = btn
	btn.pressed.connect(func(): _on_pressed(key, kind))


func _texture_for(key: String) -> Texture2D:
	if key == "cancel":
		return SettlerAssetRegistry.get_icon("cancel")
	if key in BUILD_TYPES:
		return MilitaryAssetRegistry.texture_for_entity("building", key)
	if key in QUEUE_TYPES:
		return MilitaryAssetRegistry.texture_for_entity("unit", key)
	return null


func _on_pressed(key: String, kind: String) -> void:
	match kind:
		"build":
			build_selected.emit(key)
		"queue":
			queue_selected.emit(key)
		"cancel":
			cancel_selected.emit()


func set_active_build(building_type: String) -> void:
	_active_build = building_type
	_active_queue = ""
	_refresh_styles()


func set_active_queue(unit_type: String) -> void:
	_active_queue = unit_type
	_active_build = ""
	_refresh_styles()


func clear_active() -> void:
	_active_build = ""
	_active_queue = ""
	_refresh_styles()


func get_button(key: String) -> BaseButton:
	return _buttons.get(key) as BaseButton


func _refresh_styles() -> void:
	for key in _buttons:
		var btn: TextureButton = _buttons[key] as TextureButton
		if btn == null:
			continue
		var active: bool = (key == _active_build) or (key == _active_queue)
		btn.modulate = Color(1.15, 1.15, 0.75) if active else Color(1, 1, 1)
