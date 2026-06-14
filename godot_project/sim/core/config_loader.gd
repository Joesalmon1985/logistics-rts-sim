class_name ConfigLoader
extends RefCounted

const DATA_ROOT: String = "res://sim/data/"


static func load_json(relative_path: String) -> Dictionary:
	var path: String = DATA_ROOT + relative_path
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("ConfigLoader: cannot open %s" % path)
		return {}
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		push_error("ConfigLoader: invalid JSON in %s" % path)
		return {}
	return parsed


static func load_variants_config() -> Dictionary:
	var config: Dictionary = load_json("config/variants.json")
	_validate_schema_version(config, "variants")
	if not config.has("variants"):
		push_error("ConfigLoader: variants.json missing variants key")
	return config


static func load_terrain_config() -> Dictionary:
	var config: Dictionary = load_json("config/terrain.json")
	_validate_schema_version(config, "terrain")
	return config


static func load_scenario(relative_path: String) -> Dictionary:
	var config: Dictionary = load_json(relative_path)
	_validate_schema_version(config, "scenario")
	if not config.has("variant"):
		push_error("ConfigLoader: scenario missing variant")
	return config


static func _validate_schema_version(config: Dictionary, label: String) -> void:
	if not config.has("schema_version"):
		push_error("ConfigLoader: %s missing schema_version" % label)
		return
	if int(config["schema_version"]) != 1:
		push_error("ConfigLoader: unsupported schema_version in %s" % label)
