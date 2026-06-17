class_name EconomySpriteLayer
extends Node2D

## Pools AnimatedSprite2D nodes driven by spectator sprite_states (viewer-only).

const PULSE_ANIMATIONS: Array[String] = [
	"construction_complete_pulse",
	"storage_activity_pulse",
	"building_activity_pulse",
]

var _sprites: Dictionary = {}
var _sprite_frames: SpriteFrames = null
var _map_viewport: MapViewport = null
var active_count: int = 0


func setup(map_viewport: MapViewport) -> void:
	_map_viewport = map_viewport
	_sprite_frames = MockEconomyAtlas.get_sprite_frames()


func apply_sprite_states(states: Dictionary) -> void:
	if _sprite_frames == null or _map_viewport == null:
		active_count = 0
		return
	var used_keys: Dictionary = {}
	active_count = 0
	for key in states.keys():
		var entry: Dictionary = states[key]
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var animation: String = str(entry.get("animation", ""))
		if animation == "" or not _sprite_frames.has_animation(StringName(animation)):
			continue
		var sprite: AnimatedSprite2D = _sprites.get(key) as AnimatedSprite2D
		if sprite == null:
			sprite = AnimatedSprite2D.new()
			sprite.centered = true
			sprite.scale = Vector2(1.4, 1.4)
			add_child(sprite)
			_sprites[key] = sprite
		sprite.sprite_frames = _sprite_frames
		if sprite.animation != animation:
			sprite.play(animation)
		elif not sprite.is_playing():
			sprite.play(animation)
		var pos: Vector2 = _map_viewport.hex_center_pixel_float(
			float(entry.get("q", 0)),
			float(entry.get("r", 0)),
		)
		sprite.position = pos
		sprite.visible = true
		used_keys[key] = true
		active_count += 1
		if animation in PULSE_ANIMATIONS:
			_schedule_pulse_hide(key, sprite, 0.45)
	for key in _sprites.keys():
		if not used_keys.has(key):
			var sprite: AnimatedSprite2D = _sprites[key] as AnimatedSprite2D
			if sprite != null:
				sprite.visible = false


func _schedule_pulse_hide(key: String, sprite: AnimatedSprite2D, duration: float) -> void:
	var timer := get_tree().create_timer(duration)
	timer.timeout.connect(func() -> void:
		if is_instance_valid(sprite):
			sprite.visible = false
		_sprites.erase(key)
		if is_instance_valid(sprite):
			sprite.queue_free()
	, CONNECT_ONE_SHOT)


func clear_all() -> void:
	for key in _sprites.keys():
		var sprite: AnimatedSprite2D = _sprites[key] as AnimatedSprite2D
		if sprite != null:
			sprite.queue_free()
	_sprites.clear()
	active_count = 0
