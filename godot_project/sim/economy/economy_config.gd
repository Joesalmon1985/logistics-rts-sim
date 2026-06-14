class_name EconomyConfig
extends RefCounted

var economy: Dictionary = {}
var wares: Dictionary = {}
var buildings: Dictionary = {}
var recipes: Dictionary = {}
var workers: Dictionary = {}
var tools: Dictionary = {}
var upgrades: Dictionary = {}
var construction_costs: Dictionary = {}
var terrain: Dictionary = {}


static func load_all() -> EconomyConfig:
	var cfg := EconomyConfig.new()
	cfg.economy = ConfigLoader.load_economy_config()
	cfg.wares = ConfigLoader.load_wares_config().get("wares", {})
	cfg.buildings = ConfigLoader.load_buildings_config().get("buildings", {})
	cfg.recipes = ConfigLoader.load_recipes_config().get("recipes", {})
	cfg.workers = ConfigLoader.load_workers_config().get("worker_types", {})
	cfg.tools = ConfigLoader.load_tools_config()
	cfg.upgrades = ConfigLoader.load_upgrades_config().get("upgrades", {})
	cfg.construction_costs = ConfigLoader.load_construction_costs_config()
	cfg.terrain = ConfigLoader.load_terrain_config()
	return cfg


func get_building_def(building_type: String) -> Dictionary:
	return buildings.get(building_type, {})


func get_recipe(recipe_id: String) -> Dictionary:
	return recipes.get(recipe_id, {})


func get_construction_cost(building_type: String) -> Dictionary:
	var costs: Dictionary = construction_costs.get("building_costs", {})
	if costs.has(building_type):
		return costs[building_type]
	return construction_costs.get("default_cost", {})


func get_upgrade_def(source_type: String) -> Dictionary:
	return upgrades.get(source_type, {})


func get_tool_requirement(worker_type: String) -> Dictionary:
	var reqs: Dictionary = tools.get("tool_requirements", {})
	return reqs.get(worker_type, {"ware": "tools", "amount": 0})


func get_terrain_def(terrain_type: String) -> Dictionary:
	var defs: Dictionary = terrain.get("terrain_defs", {})
	return defs.get(terrain_type, {"passability": 1, "buildability": 1, "road_legal": 1, "movement_cost": 1})


func int_val(key: String, default_value: int = 0) -> int:
	return int(economy.get(key, default_value))
