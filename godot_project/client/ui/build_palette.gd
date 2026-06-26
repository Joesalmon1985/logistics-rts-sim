class_name BuildPalette
extends VBoxContainer

## Icon build buttons with active state and tooltips.

signal build_selected(building_type: String)
signal road_selected()
signal cancel_selected()

const BUILD_TYPES: Array[String] = ["woodcutter", "forester", "sawmill", "quarry"]
const TOOLTIPS: Dictionary = {
	"road": "Road mode — click start on your network, then destination.",
	"woodcutter": "Woodcutter Lodge — harvests timber from forest hexes.",
	"forester": "Forester Camp — regrows forest for sustainable wood.",
	"sawmill": "Sawmill — converts timber into planks.",
	"quarry": "Quarry — mines stone from rock terrain.",
	"cancel": "Cancel current build or road mode (Esc).",
}

var _buttons: Dictionary = {}
var _active_type: String = ""
var _road_active: bool = false


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
	_add_btn(row1, "road", "Road", true)
	for btype in BUILD_TYPES.slice(0, 2):
		_add_btn(row1, btype, BuildingCatalog.display_name(btype), false)
	var row2 := HBoxContainer.new()
	row2.name = "Row2"
	add_child(row2)
	for btype in BUILD_TYPES.slice(2, 4):
		_add_btn(row2, btype, BuildingCatalog.display_name(btype), false)
	_add_btn(row2, "cancel", "Cancel", true)


func _add_btn(parent: Node, key: String, label_text: String, is_action: bool) -> void:
	var btn := TextureButton.new()
	btn.name = "%sBtn" % key.capitalize()
	btn.custom_minimum_size = Vector2(88, 72)
	btn.ignore_texture_size = true
	btn.stretch_mode = TextureButton.STRETCH_KEEP_ASPECT_CENTERED
	btn.tooltip_text = str(TOOLTIPS.get(key, label_text))
	var tex: Texture2D = SettlerAssetRegistry.get_icon(key) if key != "cancel" else SettlerAssetRegistry.get_icon("cancel")
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
	btn.pressed.connect(func(): _on_pressed(key))


func _on_pressed(key: String) -> void:
	match key:
		"road":
			road_selected.emit()
		"cancel":
			cancel_selected.emit()
		_:
			build_selected.emit(key)


func set_active_build(building_type: String) -> void:
	_active_type = building_type
	_road_active = false
	_refresh_styles()


func set_road_active(active: bool) -> void:
	_road_active = active
	if active:
		_active_type = ""
	_refresh_styles()


func clear_active() -> void:
	_active_type = ""
	_road_active = false
	_refresh_styles()


func get_button(key: String) -> BaseButton:
	return _buttons.get(key) as BaseButton


func _refresh_styles() -> void:
	for key in _buttons:
		var btn: TextureButton = _buttons[key] as TextureButton
		if btn == null:
			continue
		var active: bool = (_road_active and key == "road") or (_active_type != "" and key == _active_type)
		btn.modulate = Color(1.15, 1.15, 0.75) if active else Color(1, 1, 1)
