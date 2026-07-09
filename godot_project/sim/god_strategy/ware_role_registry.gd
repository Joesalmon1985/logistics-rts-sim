class_name WareRoleRegistry
extends RefCounted

## Functional ware roles mapped to existing economy ware IDs (backward compatible).

const ROLE_TO_WARE: Dictionary = {
	"raw_wood": "timber",
	"structural_wood": "planks",
	"stone_block": "stone",
	"metal_ingot": "iron_ingots",
	"food_ration": "rations",
	"tool_kit": "tools",
}


static func resolve(role_or_ware: String) -> String:
	return str(ROLE_TO_WARE.get(role_or_ware, role_or_ware))


static func role_for_ware(ware_id: String) -> String:
	for role in ROLE_TO_WARE.keys():
		if str(ROLE_TO_WARE[role]) == ware_id:
			return str(role)
	return ware_id
