class_name EconomyUpgradeSystem
extends RefCounted

## Building upgrades via construction at the same footprint.


static func tick(state: GameState) -> void:
	pass


static func start_upgrade(state: GameState, building_id: int, player_id: String) -> Dictionary:
	var building: Dictionary = EconomyState.get_building(state, building_id)
	if building.is_empty():
		return {"ok": false, "reason": "BUILDING_NOT_FOUND"}
	if str(building.get("player_id", "")) != player_id:
		return {"ok": false, "reason": "NOT_OWNER"}
	if str(building.get("state", "")) != EconomyConstants.BLD_ACTIVE:
		return {"ok": false, "reason": "BUILDING_NOT_ACTIVE"}

	var source_type: String = str(building.get("building_type", ""))
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var upgrade_def: Dictionary = cfg.get_upgrade_def(source_type)
	if upgrade_def.is_empty():
		return {"ok": false, "reason": "NO_UPGRADE"}

	var target_type: String = str(upgrade_def.get("target", ""))
	var cost: Dictionary = upgrade_def.get("cost", {})
	var anchor_q: int = int(building.get("anchor_q", 0))
	var anchor_r: int = int(building.get("anchor_r", 0))

	var result: Dictionary = EconomyConstructionSystem.create_site(
		state,
		target_type,
		anchor_q,
		anchor_r,
		player_id,
		building_id,
		cost,
	)
	if not result.get("ok", false):
		return result

	building["state"] = EconomyConstants.BLD_UNDER_CONSTRUCTION
	EconomyState.add_building(state, building)
	EconomyState.emit_event(state, "upgrade_started", {
		"building_id": building_id,
		"target_type": target_type,
		"site_id": int(result.get("site_id", 0)),
	})
	return result
