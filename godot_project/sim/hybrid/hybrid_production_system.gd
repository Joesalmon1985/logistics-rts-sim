class_name HybridProductionSystem
extends RefCounted

## Ware-fed military production for hybrid variant (HYB-LOGPROD-001, HYB-MIL-001).


static func can_queue(state: GameState, building: Dictionary, unit_type: String, player_id: String) -> Dictionary:
	var base: Dictionary = MilitaryProductionSystem.can_queue(state, building, unit_type)
	if not base.get("ok", false):
		return base
	if not HybridConfig.is_hybrid(state):
		return base
	var costs: Dictionary = HybridState.get_config(state).get_unit_production_cost(unit_type)
	if costs.is_empty():
		return {"ok": false, "reason": HybridConstants.ERR_INSUFFICIENT_WARES}
	if not HybridState.has_wares_in_hq(state, player_id, costs):
		return {"ok": false, "reason": HybridConstants.ERR_INSUFFICIENT_WARES}
	return {"ok": true, "reason": "", "ware_costs": costs}


static func queue_unit(
	state: GameState,
	building_id: int,
	building: Dictionary,
	unit_type: String,
	player_id: String,
) -> CommandResult:
	var check: Dictionary = can_queue(state, building, unit_type, player_id)
	if not check.get("ok", false):
		return CommandResult.fail(str(check.get("reason", "REJECTED")))
	var costs: Dictionary = check.get("ware_costs", {})
	if not HybridState.spend_wares_from_hq(state, player_id, costs):
		return CommandResult.fail(HybridConstants.ERR_INSUFFICIENT_WARES)

	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var udef: Dictionary = cfg.get_unit_def(unit_type)
	var queue: Dictionary = MilitaryState.ensure_production_queue(state, building_id, player_id)
	queue["items"].append({
		"unit_type": unit_type,
		"progress": 0,
		"required_ticks": int(udef.get("build_ticks", 1)),
		"ware_costs": costs.duplicate(true),
		"paid_with_wares": 1,
	})
	HybridState.emit_event(state, "ware_delivered_to_military_production", {
		"building_id": building_id,
		"unit_type": unit_type,
		"ware_costs": costs,
	})
	return CommandResult.ok(true, [
		{"type": "production_queued", "tick": state.tick, "payload": {
			"building_id": building_id,
			"unit_type": unit_type,
			"ware_costs": costs,
		}},
	])
