extends Control

## Main menu for WideRTS playable modes.


func _ready() -> void:
	SimBootstrap.load_all()
	_connect_buttons()


func _connect_buttons() -> void:
	var new_game: Button = get_node_or_null("%NewSettlerBtn")
	if new_game != null:
		new_game.pressed.connect(_on_new_settler)
	var debug: Button = get_node_or_null("%DebugViewersBtn")
	if debug != null:
		debug.pressed.connect(_on_debug_viewers)
	var quit_btn: Button = get_node_or_null("%QuitBtn")
	if quit_btn != null:
		quit_btn.pressed.connect(func(): get_tree().quit())


func _on_new_settler() -> void:
	get_tree().change_scene_to_file("res://client/scenes/settler_mvp_game.tscn")


func _on_debug_viewers() -> void:
	get_tree().change_scene_to_file("res://client/scenes/economy_viewer_demo.tscn")
