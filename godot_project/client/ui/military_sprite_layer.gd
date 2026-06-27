class_name MilitarySpriteLayer
extends Node2D

## Pools Sprite2D nodes driven by military spectator sprite_states (viewer-only).

var _sprites: Dictionary = {}
var _map_viewport: MapViewport = null
var active_count: int = 0
var show_sprites: bool = true


func setup(map_viewport: MapViewport) -> void:
	_map_viewport = map_viewport
	MilitaryAssetRegistry.load_registry()


func apply_sprite_states(states: Dictionary) -> void:
	if _map_viewport == null:
		active_count = 0
		return
	var used_keys: Dictionary = {}
	active_count = 0
	if not show_sprites:
		for key in _sprites.keys():
			var sprite: Sprite2D = _sprites[key] as Sprite2D
			if sprite != null:
				sprite.visible = false
		active_count = 0
		return
	var capped: Dictionary = MilitarySpriteStateSelector.cap_states(states)
	for key in capped.keys():
		var entry: Dictionary = capped[key]
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var tex: Texture2D = _texture_for_entry(entry)
		if tex == null:
			continue
		var sprite: Sprite2D = _sprites.get(key) as Sprite2D
		if sprite == null:
			sprite = Sprite2D.new()
			sprite.centered = true
			sprite.scale = Vector2(0.72, 0.72)
			add_child(sprite)
			_sprites[key] = sprite
		sprite.texture = tex
		sprite.position = _position_for_entry(entry)
		sprite.visible = true
		used_keys[key] = true
		active_count += 1
	for key in _sprites.keys():
		if not used_keys.has(key):
			var sprite: Sprite2D = _sprites[key] as Sprite2D
			if sprite != null:
				sprite.visible = false


func _texture_for_entry(entry: Dictionary) -> Texture2D:
	var kind: String = str(entry.get("entity_kind", ""))
	match kind:
		"building":
			return MilitaryAssetRegistry.texture_for_entity("building", str(entry.get("sim_type", "")))
		"unit":
			return MilitaryAssetRegistry.texture_for_entity("unit", str(entry.get("sim_type", "")))
		"construction_site":
			return MilitaryAssetRegistry.texture_for_entity("construction_site", "")
		"effect":
			return MilitaryAssetRegistry.texture_for_effect(str(entry.get("effect_key", "")))
	return null


func _position_for_entry(entry: Dictionary) -> Vector2:
	var q: float = float(entry.get("q", 0))
	var r: float = float(entry.get("r", 0))
	var source: Dictionary = entry.get("source", {}) if typeof(entry.get("source", {})) == TYPE_DICTIONARY else {}
	var route_points: Array = source.get("route_points", [])
	if route_points.size() >= 2:
		var a: Dictionary = route_points[0]
		var b: Dictionary = route_points[1]
		q = float(a.get("q", q)) + (float(b.get("q", q)) - float(a.get("q", q))) * 0.35
		r = float(a.get("r", r)) + (float(b.get("r", r)) - float(a.get("r", r))) * 0.35
	return _map_viewport.hex_center_pixel_float(q, r)


func clear_all() -> void:
	for key in _sprites.keys():
		var sprite: Sprite2D = _sprites[key] as Sprite2D
		if sprite != null:
			sprite.queue_free()
	_sprites.clear()
	active_count = 0
