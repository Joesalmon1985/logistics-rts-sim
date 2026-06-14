class_name MilitaryRepairSystem
extends RefCounted

## Repair depot heals nearby friendly units/buildings for credits over ticks.
## Requires a powered repair depot in range and available credits, else stalls
## (MIL-REPAIR-004).


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var hp_per_tick: int = cfg.global_int("repair_hp_per_tick", 8)
	var credit_per_tick: int = cfg.global_int("repair_credit_per_tick", 1)
	var completed: Array = []
	for job in MilitaryState.all_repair_jobs(state):
		if _process_job(state, job, cfg, hp_per_tick, credit_per_tick):
			completed.append(int(job.get("id", 0)))
	for job_id in completed:
		MilitaryState.remove_repair_job(state, job_id)


static func _process_job(state: GameState, job: Dictionary, cfg: MilitaryConfig, hp_per_tick: int, credit_per_tick: int) -> bool:
	var target: Dictionary = MilitaryCombatSystem.target_info(state, int(job.get("target_id", 0)))
	if target.is_empty() or not bool(target.get("alive", false)):
		return true
	var entity: Dictionary = _entity_dict(state, int(job.get("target_id", 0)))
	if entity.is_empty():
		return true
	if int(entity.get("hp", 0)) >= int(entity.get("max_hp", 0)):
		MilitaryState.emit_event(state, "repair_completed", {"target_id": int(job.get("target_id", 0))})
		return true

	var player_id: String = str(job.get("player_id", ""))
	if not _has_powered_depot_in_range(state, int(target.get("q", 0)), int(target.get("r", 0)), player_id, cfg):
		job["paused_reason"] = MilitaryConstants.ERR_UNPOWERED
		return false
	if MilitaryState.player_credits(state, player_id) < credit_per_tick:
		job["paused_reason"] = MilitaryConstants.ERR_INSUFFICIENT_CREDITS
		return false
	job["paused_reason"] = ""
	MilitaryState.spend_credits(state, player_id, credit_per_tick)
	entity["hp"] = mini(int(entity.get("max_hp", 0)), int(entity.get("hp", 0)) + hp_per_tick)
	MilitaryState.emit_event(state, "repair_progress", {
		"target_id": int(job.get("target_id", 0)),
		"hp": int(entity.get("hp", 0)),
	})
	if int(entity.get("hp", 0)) >= int(entity.get("max_hp", 0)):
		MilitaryState.emit_event(state, "repair_completed", {"target_id": int(job.get("target_id", 0))})
		return true
	return false


static func _entity_dict(state: GameState, target_id: int) -> Dictionary:
	var unit: Dictionary = MilitaryState.get_unit(state, target_id)
	if not unit.is_empty():
		return unit
	return MilitaryState.get_building(state, target_id)


static func _has_powered_depot_in_range(state: GameState, q: int, r: int, player_id: String, cfg: MilitaryConfig) -> bool:
	var rng: int = cfg.global_int("repair_range", 4)
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "repair_depot":
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		if not MilitaryPowerSystem.building_powered(state, building):
			continue
		if MilitaryMapRules.hex_distance(q, r, int(building.get("anchor_q", 0)), int(building.get("anchor_r", 0))) <= rng:
			return true
	return false


static func can_repair(state: GameState, player_id: String, target_id: int) -> Dictionary:
	var target: Dictionary = MilitaryCombatSystem.target_info(state, target_id)
	if target.is_empty() or not bool(target.get("alive", false)):
		return {"ok": false, "reason": MilitaryConstants.ERR_NO_SUCH_ENTITY}
	if str(target.get("player_id", "")) != player_id:
		return {"ok": false, "reason": MilitaryConstants.ERR_NOT_OWNER}
	var entity: Dictionary = _entity_dict(state, target_id)
	if int(entity.get("hp", 0)) >= int(entity.get("max_hp", 0)):
		return {"ok": false, "reason": MilitaryConstants.ERR_CANNOT_REPAIR}
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	if not _has_powered_depot_in_range(state, int(target.get("q", 0)), int(target.get("r", 0)), player_id, cfg):
		return {"ok": false, "reason": MilitaryConstants.ERR_CANNOT_REPAIR}
	return {"ok": true, "reason": ""}
