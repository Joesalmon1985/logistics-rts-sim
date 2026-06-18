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
	var errors: Array = cfg.validate(false)
	for err in errors:
		push_warning("EconomyConfig validation: %s" % str(err))
	return cfg


## Cross-reference validation. strict=true is for tests; runtime load_all uses strict=false (warn only).
func validate(strict: bool = false) -> Array[String]:
	var errors: Array[String] = []
	_validate_buildings(errors)
	_validate_recipes(errors)
	_validate_construction_costs(errors)
	if strict and not errors.is_empty():
		for err in errors:
			push_error("EconomyConfig strict validation: %s" % err)
	return errors


func get_building_ports(bdef: Dictionary) -> Array:
	var ports: Array = bdef.get("ports", [])
	if ports.is_empty():
		return [[0, 0]]
	var normalized: Array = []
	for port in ports:
		if typeof(port) != TYPE_ARRAY or port.size() < 2:
			continue
		normalized.append([int(port[0]), int(port[1])])
	if normalized.is_empty():
		return [[0, 0]]
	return normalized


func _validate_ware_list(errors: Array[String], wares_dict: Dictionary, context: String) -> void:
	for ware_type in wares_dict:
		if ware_type == "builder_work":
			continue
		if not wares.has(str(ware_type)):
			errors.append("%s references unknown ware '%s'" % [context, str(ware_type)])


func _validate_buildings(errors: Array[String]) -> void:
	for building_type in buildings:
		var bdef: Dictionary = buildings[building_type]
		var recipe_id: String = str(bdef.get("recipe_id", ""))
		if recipe_id != "" and not recipes.has(recipe_id):
			errors.append("building '%s' references unknown recipe '%s'" % [building_type, recipe_id])
		var worker_type: String = str(bdef.get("worker_type", ""))
		if worker_type != "" and not workers.has(worker_type):
			errors.append("building '%s' references unknown worker_type '%s'" % [building_type, worker_type])
		var upgrade_target: String = str(bdef.get("upgrade_target", ""))
		if upgrade_target != "" and not buildings.has(upgrade_target):
			errors.append("building '%s' references unknown upgrade_target '%s'" % [building_type, upgrade_target])


func _validate_recipes(errors: Array[String]) -> void:
	for recipe_id in recipes:
		var recipe: Dictionary = recipes[recipe_id]
		var building_type: String = str(recipe.get("building", ""))
		if building_type != "" and not buildings.has(building_type):
			errors.append("recipe '%s' references unknown building '%s'" % [recipe_id, building_type])
		_validate_ware_list(errors, recipe.get("inputs", {}), "recipe '%s' input" % recipe_id)
		_validate_ware_list(errors, recipe.get("outputs", {}), "recipe '%s' output" % recipe_id)


func _validate_construction_costs(errors: Array[String]) -> void:
	var costs: Dictionary = construction_costs.get("building_costs", {})
	for building_type in costs:
		if not buildings.has(building_type):
			errors.append("construction_cost for unknown building '%s'" % building_type)
		_validate_ware_list(errors, costs[building_type], "construction_cost '%s'" % building_type)
	var default_cost: Dictionary = construction_costs.get("default_cost", {})
	if not default_cost.is_empty():
		_validate_ware_list(errors, default_cost, "construction_cost default")


func get_building_def(building_type: String) -> Dictionary:
	return buildings.get(building_type, {})


func get_recipe(recipe_id: String) -> Dictionary:
	return recipes.get(recipe_id, {})


func get_construction_cost(building_type: String) -> Dictionary:
	var costs: Dictionary = construction_costs.get("building_costs", {})
	if costs.has(building_type):
		return costs[building_type]
	return construction_costs.get("default_cost", {})


func duplicate_config() -> EconomyConfig:
	var copy := EconomyConfig.new()
	copy.economy = economy.duplicate(true)
	copy.wares = wares.duplicate(true)
	copy.buildings = buildings.duplicate(true)
	copy.recipes = recipes.duplicate(true)
	copy.workers = workers.duplicate(true)
	copy.tools = tools.duplicate(true)
	copy.upgrades = upgrades.duplicate(true)
	copy.construction_costs = construction_costs.duplicate(true)
	copy.terrain = terrain.duplicate(true)
	return copy


func apply_diag_free_construction(builder_work: int = 1) -> void:
	var zero_cost: Dictionary = {"planks": 0, "stone": 0, "tools": 0, "builder_work": maxi(1, builder_work)}
	var building_costs: Dictionary = construction_costs.get("building_costs", {}).duplicate(true)
	for building_type in building_costs.keys():
		building_costs[building_type] = zero_cost.duplicate(true)
	construction_costs["building_costs"] = building_costs
	construction_costs["default_cost"] = zero_cost.duplicate(true)


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
