class_name SettlerAssetRegistry
extends RefCounted

## Loads asset_registry.json and resolves textures for Settler MVP display.

const REGISTRY_PATH := "res://client/assets/asset_registry.json"

static var _data: Dictionary = {}
static var _textures: Dictionary = {}
static var _loaded: bool = false
static var _missing_logged: Dictionary = {}


static func load_registry() -> Dictionary:
	if _loaded:
		return _data
	var file := FileAccess.open(REGISTRY_PATH, FileAccess.READ)
	if file == null:
		push_warning("SettlerAssetRegistry: cannot open %s" % REGISTRY_PATH)
		_loaded = true
		return _data
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) == TYPE_DICTIONARY:
		_data = parsed
	_loaded = true
	return _data


static func get_texture(category: String, key: String) -> Texture2D:
	load_registry()
	var cache_key := "%s:%s" % [category, key]
	if _textures.has(cache_key):
		return _textures[cache_key]
	var cat: Dictionary = _data.get(category, {})
	var path: String = str(cat.get(key, ""))
	if path == "":
		_log_missing(category, key)
		return null
	var tex: Texture2D = load(path) as Texture2D
	if tex == null:
		_log_missing(category, key)
		return null
	_textures[cache_key] = tex
	return tex


static func get_building_texture(building_type: String) -> Texture2D:
	return get_texture("buildings", building_type)


static func get_icon(building_type: String) -> Texture2D:
	var icon: Texture2D = get_texture("icons", building_type)
	if icon != null:
		return icon
	return get_building_texture(building_type)


static func get_terrain_texture(terrain_type: String) -> Texture2D:
	return get_texture("terrain", terrain_type)


static func get_ui_texture(ui_key: String) -> Texture2D:
	return get_texture("ui", ui_key)


static func required_concepts() -> Array:
	return [
		["buildings", "headquarters"], ["buildings", "woodcutter"], ["buildings", "forester"],
		["buildings", "sawmill"], ["buildings", "quarry"], ["buildings", "construction_site"],
		["logistics", "road"], ["logistics", "road_node"], ["logistics", "carrier"], ["logistics", "builder"],
		["wares", "timber"], ["wares", "planks"], ["wares", "stone"], ["wares", "builder_work"],
		["terrain", "grass"], ["terrain", "forest"], ["terrain", "rock"], ["terrain", "water"],
		["ui", "selection_highlight"], ["ui", "placement_valid"], ["ui", "placement_invalid"],
		["icons", "headquarters"], ["icons", "woodcutter"], ["icons", "forester"],
		["icons", "sawmill"], ["icons", "quarry"], ["icons", "road"], ["icons", "cancel"],
	]


static func path_for(category: String, key: String) -> String:
	load_registry()
	return str(_data.get(category, {}).get(key, ""))


static func file_exists_for(category: String, key: String) -> bool:
	var path: String = path_for(category, key)
	if path == "":
		return false
	return FileAccess.file_exists(path)


static func _log_missing(category: String, key: String) -> void:
	var id := "%s:%s" % [category, key]
	if _missing_logged.has(id):
		return
	_missing_logged[id] = true
	push_warning("SettlerAssetRegistry: missing %s" % id)
