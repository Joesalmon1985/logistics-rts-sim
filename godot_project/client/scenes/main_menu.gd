class_name MainMenu
extends Control

## Main menu for WideRTS playable modes.

const GAME_SCENE := "res://client/scenes/settler_mvp_game.tscn"
const HYBRID_SCENE := "res://client/scenes/hybrid_mvp_game.tscn"
const SKIRMISH_SCENE := "res://client/scenes/military_mvp_game.tscn"
const SettlerGameScript := preload("res://client/scenes/settler_mvp_game.gd")
const HybridGameScript := preload("res://client/scenes/hybrid_mvp_game.gd")
const MilitaryGameScript := preload("res://client/scenes/military_mvp_game.gd")


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
	var hybrid_3d: Button = get_node_or_null("%Hybrid3dSpectatorBtn")
	if hybrid_3d != null:
		hybrid_3d.pressed.connect(_on_hybrid_3d_spectator)
	var hybrid_mvp: Button = get_node_or_null("%HybridMvpBtn")
	if hybrid_mvp != null:
		hybrid_mvp.pressed.connect(_on_hybrid_mvp)
	var hybrid_tutorial: Button = get_node_or_null("%HybridTutorialBtn")
	if hybrid_tutorial != null:
		hybrid_tutorial.pressed.connect(_on_hybrid_tutorial)
	var skirmish: Button = get_node_or_null("%SkirmishMvpBtn")
	if skirmish != null:
		skirmish.pressed.connect(_on_skirmish_mvp)
	var skirmish_tutorial: Button = get_node_or_null("%SkirmishTutorialBtn")
	if skirmish_tutorial != null:
		skirmish_tutorial.pressed.connect(_on_skirmish_tutorial)
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


func _on_hybrid_3d_spectator() -> void:
	get_tree().change_scene_to_file("res://client/scenes/hybrid_3d_bot_spectator.tscn")


func _on_hybrid_mvp() -> void:
	HybridGameScript.pending_tutorial = false
	get_tree().change_scene_to_file(HYBRID_SCENE)


func _on_hybrid_tutorial() -> void:
	HybridGameScript.pending_tutorial = true
	get_tree().change_scene_to_file(HYBRID_SCENE)


func _on_skirmish_mvp() -> void:
	MilitaryGameScript.pending_tutorial = false
	get_tree().change_scene_to_file(SKIRMISH_SCENE)


func _on_skirmish_tutorial() -> void:
	MilitaryGameScript.pending_tutorial = true
	get_tree().change_scene_to_file(SKIRMISH_SCENE)


func _change_game(with_tutorial: bool) -> void:
	SettlerGameScript.pending_tutorial = with_tutorial
	get_tree().change_scene_to_file(GAME_SCENE)
