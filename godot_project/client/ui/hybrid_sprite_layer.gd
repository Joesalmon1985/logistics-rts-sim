class_name HybridSpriteLayer
extends Control

## Draws economy + military sprites over MapViewport (viewer-only).

var map_viewport: MapViewport = null
var use_sprites: bool = true
var animation_enabled: bool = true
var _settler_layer: SettlerSpriteLayer
var _military_layer: MilitarySpriteLayer
var _snapshot: Dictionary = {}


func setup(viewport: MapViewport) -> void:
	map_viewport = viewport
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	if _settler_layer == null:
		_settler_layer = SettlerSpriteLayer.new()
		_settler_layer.name = "SettlerSubLayer"
		_settler_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
		add_child(_settler_layer)
	if _military_layer == null:
		_military_layer = MilitarySpriteLayer.new()
		_military_layer.name = "MilitarySubLayer"
		add_child(_military_layer)
	_settler_layer.setup(viewport)
	_military_layer.setup(viewport)
	SettlerAssetRegistry.load_registry()
	MilitaryAssetRegistry.load_registry()


func set_snapshot(snapshot: Dictionary) -> void:
	_snapshot = snapshot
	if _settler_layer != null:
		_settler_layer.set_snapshot(snapshot)


func apply_military_states(states: Dictionary) -> void:
	if _military_layer != null:
		_military_layer.show_sprites = use_sprites
		_military_layer.apply_sprite_states(states)


func clear_all() -> void:
	if _military_layer != null:
		_military_layer.clear_all()


func _ready() -> void:
	set_process(true)


func _process(_delta: float) -> void:
	if _settler_layer != null:
		_settler_layer.use_sprites = use_sprites
		_settler_layer.animation_enabled = animation_enabled
		_settler_layer.queue_redraw()
