class_name EconomySpriteLayer
extends Node2D

## Pools AnimatedSprite2D nodes driven by spectator sprite_states (viewer-only).

const PULSE_ANIMATIONS: Array[String] = [
	"construction_complete_pulse",
	"storage_activity_pulse",
	"building_activity_pulse",
]

var _sprites: Dictionary = {}
var _labels: Dictionary = {}
var _sprite_frames: SpriteFrames = null
var _map_viewport: MapViewport = null
var active_count: int = 0
var show_sprites: bool = true


func setup(map_viewport: MapViewport) -> void:
	_map_viewport = map_viewport
	_sprite_frames = MockEconomyAtlas.get_sprite_frames()


func apply_sprite_states(states: Dictionary) -> void:
	if _sprite_frames == null or _map_viewport == null:
		active_count = 0
		return
	var used_keys: Dictionary = {}
	active_count = 0
	if not show_sprites:
		for key in _sprites.keys():
			var sprite: AnimatedSprite2D = _sprites[key] as AnimatedSprite2D
			if sprite != null:
				sprite.visible = false
		return
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
			sprite.scale = Vector2(0.75, 0.75)
			add_child(sprite)
			_sprites[key] = sprite
		sprite.sprite_frames = _sprite_frames
		if sprite.animation != animation:
			sprite.play(animation)
		elif not sprite.is_playing():
			sprite.play(animation)
		var pos: Vector2 = _position_for_entry(entry)
		sprite.position = pos
		sprite.visible = true
		used_keys[key] = true
		active_count += 1
		_update_ware_label(key, entry, pos)
		if animation in PULSE_ANIMATIONS:
			_schedule_pulse_hide(key, 0.45)
	for key in _sprites.keys():
		if not used_keys.has(key):
			var sprite: AnimatedSprite2D = _sprites[key] as AnimatedSprite2D
			if sprite != null:
				sprite.visible = false
			var label: Label = _labels.get(key) as Label
			if label != null:
				label.visible = false


func _position_for_entry(entry: Dictionary) -> Vector2:
	var q: float = float(entry.get("q", 0))
	var r: float = float(entry.get("r", 0))
	var source: Dictionary = entry.get("source", {}) if typeof(entry.get("source", {})) == TYPE_DICTIONARY else {}
	var route_points: Array = source.get("route_points", [])
	if route_points.size() >= 2:
		var a: Dictionary = route_points[0]
		var b: Dictionary = route_points[1]
		var aq: float = float(a.get("q", q))
		var ar: float = float(a.get("r", r))
		var bq: float = float(b.get("q", q))
		var br: float = float(b.get("r", r))
		q = aq + (bq - aq) * 0.35
		r = ar + (br - ar) * 0.35
	return _map_viewport.hex_center_pixel_float(q, r)


func _update_ware_label(key: String, entry: Dictionary, pos: Vector2) -> void:
	var grounding: Dictionary = entry.get("grounding", {}) if typeof(entry.get("grounding", {})) == TYPE_DICTIONARY else {}
	var ware: String = str(grounding.get("value", ""))
	var anim: String = str(entry.get("animation", ""))
	if not (anim.begins_with("ware_") or anim.begins_with("carrier_")) or ware == "":
		var label_hide: Label = _labels.get(key) as Label
		if label_hide != null:
			label_hide.visible = false
		return
	var label: Label = _labels.get(key) as Label
	if label == null:
		label = Label.new()
		label.add_theme_font_size_override("font_size", 9)
		add_child(label)
		_labels[key] = label
	label.text = ware.substr(0, 1).to_upper()
	label.position = pos + Vector2(8, -10)
	label.visible = true


func _schedule_pulse_hide(key: String, duration: float) -> void:
	if not is_inside_tree():
		return
	var timer := get_tree().create_timer(duration)
	timer.timeout.connect(_on_pulse_hide_timeout.bind(key), CONNECT_ONE_SHOT)


func _on_pulse_hide_timeout(key: String) -> void:
	if not is_instance_valid(self):
		return
	var sprite: AnimatedSprite2D = _sprites.get(key) as AnimatedSprite2D
	if is_instance_valid(sprite):
		sprite.visible = false
		sprite.queue_free()
	_sprites.erase(key)
	var label: Label = _labels.get(key) as Label
	if is_instance_valid(label):
		label.queue_free()
	_labels.erase(key)


func clear_all() -> void:
	for key in _sprites.keys():
		var sprite: AnimatedSprite2D = _sprites[key] as AnimatedSprite2D
		if sprite != null:
			sprite.queue_free()
	for key in _labels.keys():
		var label: Label = _labels.get(key) as Label
		if label != null:
			label.queue_free()
	_sprites.clear()
	_labels.clear()
	active_count = 0
