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


static func load_economy_config() -> Dictionary:
	var config: Dictionary = load_json("config/economy.json")
	_validate_schema_version(config, "economy")
	return config


static func load_wares_config() -> Dictionary:
	var config: Dictionary = load_json("config/wares.json")
	_validate_schema_version(config, "wares")
	return config


static func load_buildings_config() -> Dictionary:
	var config: Dictionary = load_json("config/buildings.json")
	_validate_schema_version(config, "buildings")
	return config


static func load_recipes_config() -> Dictionary:
	var config: Dictionary = load_json("config/recipes.json")
	_validate_schema_version(config, "recipes")
	return config


static func load_workers_config() -> Dictionary:
	var config: Dictionary = load_json("config/workers.json")
	_validate_schema_version(config, "workers")
	return config


static func load_tools_config() -> Dictionary:
	var config: Dictionary = load_json("config/tools.json")
	_validate_schema_version(config, "tools")
	return config


static func load_upgrades_config() -> Dictionary:
	var config: Dictionary = load_json("config/upgrades.json")
	_validate_schema_version(config, "upgrades")
	return config


static func load_construction_costs_config() -> Dictionary:
	var config: Dictionary = load_json("config/construction_costs.json")
	_validate_schema_version(config, "construction_costs")
	return config


static func _validate_schema_version(config: Dictionary, label: String) -> void:
	if not config.has("schema_version"):
		push_error("ConfigLoader: %s missing schema_version" % label)
		return
	if int(config["schema_version"]) != 1:
		push_error("ConfigLoader: unsupported schema_version in %s" % label)
