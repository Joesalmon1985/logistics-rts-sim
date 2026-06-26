class_name MainMenu
extends Control

## Main menu for WideRTS playable modes.

const GAME_SCENE := "res://client/scenes/settler_mvp_game.tscn"
const SettlerGameScript := preload("res://client/scenes/settler_mvp_game.gd")


func _ready() -> void:
	SimBootstrap.load_all()
	_connect_buttons()


func _connect_buttons() -> void:
	var tutorial: Button = get_node_or_null("%TutorialBtn")
	if tutorial != null:
		tutorial.pressed.connect(_on_tutorial)
	var free_play: Button = get_node_or_null("%FreePlayBtn")
	if free_play != null:
		free_play.pressed.connect(_on_free_play)
	var legacy: Button = get_node_or_null("%NewSettlerBtn")
	if legacy != null:
		legacy.pressed.connect(_on_free_play)
	var debug: Button = get_node_or_null("%DebugViewersBtn")
	if debug != null:
		debug.pressed.connect(_on_debug_viewers)
	var quit_btn: Button = get_node_or_null("%QuitBtn")
	if quit_btn != null:
		quit_btn.pressed.connect(func(): get_tree().quit())


func _on_tutorial() -> void:
	_change_game(true)


func _on_free_play() -> void:
	_change_game(false)


func _on_debug_viewers() -> void:
	get_tree().change_scene_to_file("res://client/scenes/economy_viewer_demo.tscn")


func _change_game(with_tutorial: bool) -> void:
	SettlerGameScript.pending_tutorial = with_tutorial
	get_tree().change_scene_to_file(GAME_SCENE)
