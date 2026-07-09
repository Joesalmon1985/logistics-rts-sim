class_name GodStrategyResourceAudit
extends RefCounted

const CHAIN_BUILDINGS: Array[String] = ["woodcutter", "sawmill", "barracks"]
const QUEUE_COST: Dictionary = {"tools": 1, "rations": 2}


static func can_afford_chain_start(scenario: Dictionary) -> Dictionary:
	var wares: Dictionary = scenario.get("starting_wares", {})
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var costs: Dictionary = {"planks": 0, "stone": 0, "tools": 0}
	for building_type in CHAIN_BUILDINGS:
		var building_costs: Dictionary = cfg.get_construction_cost(building_type)
		for ware_type in building_costs.keys():
			if str(ware_type) == "builder_work":
				continue
			costs[ware_type] = int(costs.get(ware_type, 0)) + int(building_costs[ware_type])
	for ware_type in QUEUE_COST.keys():
		costs[ware_type] = int(costs.get(ware_type, 0)) + int(QUEUE_COST[ware_type])

	var failures: Array = []
	for ware_type in costs.keys():
		if int(wares.get(ware_type, 0)) < int(costs[ware_type]):
			failures.append({
				"ware": ware_type,
				"required": int(costs[ware_type]),
				"available": int(wares.get(ware_type, 0)),
			})

	var workers: Dictionary = scenario.get("starting_workers", {})
	var builder_need: int = 1
	var carrier_need: int = 1
	if int(workers.get("builder", 0)) < builder_need:
		failures.append({"worker": "builder", "required": builder_need, "available": int(workers.get("builder", 0))})
	if int(workers.get("carrier", 0)) < carrier_need:
		failures.append({"worker": "carrier", "required": carrier_need, "available": int(workers.get("carrier", 0))})

	return {
		"ok": failures.is_empty(),
		"total_costs": costs,
		"failures": failures,
	}
