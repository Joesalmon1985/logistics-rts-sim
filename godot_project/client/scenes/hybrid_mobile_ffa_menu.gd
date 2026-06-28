extends Control

## Difficulty select for Hybrid mobile FFA.

func _ready() -> void:
	_build_ui()


func _build_ui() -> void:
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	var vbox := VBoxContainer.new()
	center.add_child(vbox)
	var title := Label.new()
	title.text = "Hybrid FFA — Choose Difficulty"
	vbox.add_child(title)
	for spec in [["EasyBtn", "easy", "Easy"], ["NormalBtn", "normal", "Normal"], ["HardBtn", "hard", "Hard"]]:
		var btn := Button.new()
		btn.name = spec[0]
		btn.text = spec[2]
		btn.custom_minimum_size = Vector2(200, 48)
		btn.pressed.connect(func(): _start(spec[1]))
		vbox.add_child(btn)
	var menu := Button.new()
	menu.text = "Main Menu"
	menu.custom_minimum_size = Vector2(200, 48)
	menu.pressed.connect(func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	vbox.add_child(menu)


func _start(diff: String) -> void:
	var scene: PackedScene = load("res://client/scenes/hybrid_mobile_ffa_game.tscn")
	var game = scene.instantiate()
	if "difficulty" in game:
		game.difficulty = diff
	get_tree().root.add_child(game)
	queue_free()
