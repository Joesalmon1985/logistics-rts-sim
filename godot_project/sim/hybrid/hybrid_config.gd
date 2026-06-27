class_name HybridConfig
extends RefCounted

var military_buildings: Dictionary = {}
var unit_production_costs: Dictionary = {}
var repair_costs_per_tick: Dictionary = {}
var research_costs: Dictionary = {}
var forbidden_player_commands: Array = []


static func load_all() -> HybridConfig:
	var raw: Dictionary = ConfigLoader.load_hybrid_config()
	var cfg := HybridConfig.new()
	cfg.military_buildings = raw.get("military_buildings", {})
	cfg.unit_production_costs = raw.get("unit_production_costs", {})
	cfg.repair_costs_per_tick = raw.get("repair_costs_per_tick", {})
	cfg.research_costs = raw.get("research_costs", {})
	cfg.forbidden_player_commands = raw.get("forbidden_player_commands", [])
	return cfg


func is_military_building(building_type: String) -> bool:
	return military_buildings.has(building_type)


func get_construction_cost(building_type: String) -> Dictionary:
	var entry: Dictionary = military_buildings.get(building_type, {})
	return entry.get("construction", {}).duplicate(true)


func get_economy_stub(building_type: String) -> Dictionary:
	var entry: Dictionary = military_buildings.get(building_type, {})
	return entry.get("economy_stub", {}).duplicate(true)


func get_unit_production_cost(unit_type: String) -> Dictionary:
	return unit_production_costs.get(unit_type, {}).duplicate(true)


func get_research_cost(tech_id: String) -> Dictionary:
	return research_costs.get(tech_id, {}).duplicate(true)


func apply_economy_overrides(econ_cfg: EconomyConfig) -> void:
	for building_type in military_buildings.keys():
		var stub: Dictionary = get_economy_stub(building_type)
		if stub.is_empty():
			continue
		var merged: Dictionary = stub.duplicate(true)
		merged["display_name"] = building_type.replace("_", " ").capitalize()
		econ_cfg.buildings[building_type] = merged
		var costs: Dictionary = get_construction_cost(building_type)
		if not costs.is_empty():
			var building_costs: Dictionary = econ_cfg.construction_costs.get("building_costs", {}).duplicate(true)
			building_costs[building_type] = costs.duplicate(true)
			econ_cfg.construction_costs["building_costs"] = building_costs


static func is_hybrid(state: GameState) -> bool:
	return state != null and state.variant_id == "hybrid"
