class_name MockEconomyAtlas
extends RefCounted

## Loads placeholder economy spritesheet metadata into SpriteFrames (RUN_05C).

const ART_DIR: String = "res://client/art"
const JSON_PATH: String = "res://client/art/mock_economy_sprites.json"
const PNG_PATH: String = "res://client/art/mock_economy_sprites.png"

static var _sprite_frames_cache: SpriteFrames = null


static func animation_names() -> PackedStringArray:
	var doc: Dictionary = _load_json()
	if doc.is_empty():
		return PackedStringArray()
	var names: PackedStringArray = PackedStringArray()
	for anim_name in doc.get("animations", {}).keys():
		names.append(str(anim_name))
	names.sort()
	return names


static func has_animation(name: String) -> bool:
	return animation_names().has(name)


static func get_sprite_frames() -> SpriteFrames:
	if _sprite_frames_cache != null:
		return _sprite_frames_cache
	_ensure_assets()
	var doc: Dictionary = _load_json()
	if doc.is_empty():
		push_error("MockEconomyAtlas: missing JSON")
		return SpriteFrames.new()
	var texture: Texture2D = _load_texture()
	if texture == null:
		push_error("MockEconomyAtlas: missing PNG")
		return SpriteFrames.new()
	var frames: Dictionary = doc.get("frames", {})
	var animations: Dictionary = doc.get("animations", {})
	var sprite_frames := SpriteFrames.new()
	for anim_name in animations.keys():
		var anim: Dictionary = animations[anim_name]
		var anim_id: StringName = StringName(str(anim_name))
		if not sprite_frames.has_animation(anim_id):
			sprite_frames.add_animation(anim_id)
		sprite_frames.set_animation_speed(anim_id, float(anim.get("fps", 8)))
		sprite_frames.set_animation_loop(anim_id, bool(anim.get("loop", true)))
		for frame_id in anim.get("frames", []):
			var rect: Dictionary = frames.get(str(frame_id), {})
			if rect.is_empty():
				continue
			var atlas := AtlasTexture.new()
			atlas.atlas = texture
			atlas.region = Rect2(
				int(rect.get("x", 0)),
				int(rect.get("y", 0)),
				int(rect.get("w", 16)),
				int(rect.get("h", 16)),
			)
			sprite_frames.add_frame(anim_id, atlas)
	_sprite_frames_cache = sprite_frames
	return sprite_frames


static func _ensure_assets() -> void:
	var global_png: String = ProjectSettings.globalize_path(PNG_PATH)
	if not FileAccess.file_exists(global_png):
		var GenScript := load("res://client/tools/generate_mock_economy_atlas.gd")
		if GenScript != null:
			GenScript.call("_generate")


static func _load_json() -> Dictionary:
	if not FileAccess.file_exists(ProjectSettings.globalize_path(JSON_PATH)):
		_ensure_assets()
	var f := FileAccess.open(ProjectSettings.globalize_path(JSON_PATH), FileAccess.READ)
	if f == null:
		return {}
	var parsed: Variant = JSON.parse_string(f.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}
	return parsed


static func _load_texture() -> Texture2D:
	var global_png: String = ProjectSettings.globalize_path(PNG_PATH)
	if not FileAccess.file_exists(global_png):
		_ensure_assets()
	var image := Image.load_from_file(global_png)
	if image != null and not image.is_empty():
		return ImageTexture.create_from_image(image)
	var imported: Texture2D = load(PNG_PATH) as Texture2D
	return imported
