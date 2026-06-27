class_name MilitaryAssetRegistry
extends RefCounted

## Loads military/asset_registry.json for spectator sprites.

const REGISTRY_PATH := "res://client/assets/military/asset_registry.json"

static var _data: Dictionary = {}
static var _textures: Dictionary = {}
static var _loaded: bool = false


static func load_registry() -> Dictionary:
	if _loaded:
		return _data
	var file := FileAccess.open(REGISTRY_PATH, FileAccess.READ)
	if file == null:
		push_warning("MilitaryAssetRegistry: cannot open %s" % REGISTRY_PATH)
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
		return null
	var tex: Texture2D = load(path) as Texture2D
	if tex != null:
		_textures[cache_key] = tex
	return tex


static func texture_for_entity(entity_kind: String, sim_type: String) -> Texture2D:
	var key: String = MilitaryCatalog.catalog_key(sim_type)
	match entity_kind:
		"building":
			return get_texture("buildings", key)
		"unit":
			return get_texture("units", key)
		"construction_site":
			return get_texture("buildings", "construction_site")
		_:
			return null


static func texture_for_effect(effect_key: String) -> Texture2D:
	return get_texture("effects", effect_key)


static func required_concepts() -> Array:
	return [
		["buildings", "command_hub"],
		["buildings", "power_generator"],
		["buildings", "barracks"],
		["buildings", "fabricator"],
		["buildings", "turret"],
		["units", "rifle_squad"],
		["units", "harvester"],
		["effects", "attack_marker"],
	]
