extends Control

## God-Strategy mobile FFA menu.


func _ready() -> void:
	_build_ui()


func _build_ui() -> void:
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	var vbox := VBoxContainer.new()
	center.add_child(vbox)
	var title := Label.new()
	title.text = "God-Strategy 4P FFA"
	vbox.add_child(title)
	var sub := Label.new()
	sub.text = "Human vs 3 neural god opponents"
	vbox.add_child(sub)
	var play := Button.new()
	play.text = "Play"
	play.custom_minimum_size = Vector2(220, 52)
	play.pressed.connect(func(): _start())
	vbox.add_child(play)
	var menu := Button.new()
	menu.text = "Main Menu"
	menu.custom_minimum_size = Vector2(220, 48)
	menu.pressed.connect(func(): get_tree().change_scene_to_file("res://client/scenes/main_menu.tscn"))
	vbox.add_child(menu)


func _start() -> void:
	var scene: PackedScene = load("res://client/scenes/god_strategy_mobile_ffa_game.tscn")
	get_tree().root.add_child(scene.instantiate())
	queue_free()
