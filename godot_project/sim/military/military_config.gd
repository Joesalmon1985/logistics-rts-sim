class_name MilitaryConfig
extends RefCounted

## Normalised, cached access to all military_*.json config data.

var buildings: Dictionary = {}
var units: Dictionary = {}
var weapons: Dictionary = {}
var armour_types: Dictionary = {}
var damage_modifiers: Dictionary = {}
var damage_default_modifier: int = 100
var tech: Dictionary = {}
var power: Dictionary = {}
var globals: Dictionary = {}
var resources: Dictionary = {}
var orders: Dictionary = {}
var scenarios: Dictionary = {}
var terrain_defs: Dictionary = {}


static func load_all() -> MilitaryConfig:
	var cfg := MilitaryConfig.new()
	cfg.buildings = ConfigLoader.load_military_buildings_config().get("buildings", {})
	cfg.units = ConfigLoader.load_military_units_config().get("units", {})
	cfg.weapons = ConfigLoader.load_military_weapons_config().get("weapons", {})
	cfg.armour_types = ConfigLoader.load_military_armour_types_config().get("armour_types", {})
	var dm: Dictionary = ConfigLoader.load_military_damage_modifiers_config()
	cfg.damage_modifiers = dm.get("modifiers", {})
	cfg.damage_default_modifier = int(dm.get("default_modifier", 100))
	cfg.tech = ConfigLoader.load_military_tech_tree_config().get("tech", {})
	cfg.power = ConfigLoader.load_military_power_config()
	cfg.globals = cfg.power.get("globals", {})
	cfg.resources = ConfigLoader.load_military_resources_config()
	cfg.orders = ConfigLoader.load_military_orders_config()
	cfg.scenarios = ConfigLoader.load_military_scenarios_config()
	cfg.terrain_defs = ConfigLoader.load_terrain_config().get("terrain_defs", {})
	return cfg


func terrain_movement_cost(terrain_type: String) -> int:
	var cost: int = int(terrain_defs.get(terrain_type, {}).get("movement_cost", 1))
	return cost if cost > 0 else 1


func get_building_def(building_type: String) -> Dictionary:
	return buildings.get(building_type, {})


func get_unit_def(unit_type: String) -> Dictionary:
	return units.get(unit_type, {})


func get_weapon_def(weapon_id: String) -> Dictionary:
	return weapons.get(weapon_id, {})


func get_tech_def(tech_id: String) -> Dictionary:
	return tech.get(tech_id, {})


func get_field_def(field_type: String) -> Dictionary:
	return resources.get("field_types", {}).get(field_type, {})


func damage_modifier(damage_type: String, armour_type: String) -> int:
	var by_type: Dictionary = damage_modifiers.get(damage_type, {})
	return int(by_type.get(armour_type, damage_default_modifier))


func global_int(key: String, default_value: int = 0) -> int:
	return int(globals.get(key, default_value))


func resource_int(key: String, default_value: int = 0) -> int:
	return int(resources.get(key, default_value))


func order_int(key: String, default_value: int = 0) -> int:
	return int(orders.get(key, default_value))


func power_flag(key: String, default_value: int = 0) -> int:
	return int(power.get(key, default_value))
