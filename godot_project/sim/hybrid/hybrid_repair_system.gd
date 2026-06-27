class_name HybridRepairSystem
extends RefCounted

## Ware-fed repair jobs for hybrid variant (HYB-LOGREP-001).


static func tick(state: GameState) -> void:
	if not HybridConfig.is_hybrid(state):
		MilitaryRepairSystem.tick(state)
		return
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var hcfg: HybridConfig = HybridState.get_config(state)
	var hp_per_tick: int = cfg.global_int("repair_hp_per_tick", 8)
	var ware_costs: Dictionary = hcfg.repair_costs_per_tick
	var completed: Array = []
	for job in MilitaryState.all_repair_jobs(state):
		if _process_job(state, job, cfg, hp_per_tick, ware_costs):
			completed.append(int(job.get("id", 0)))
	for job_id in completed:
		MilitaryState.remove_repair_job(state, job_id)


static func _process_job(
	state: GameState,
	job: Dictionary,
	cfg: MilitaryConfig,
	hp_per_tick: int,
	ware_costs: Dictionary,
) -> bool:
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
	if not HybridState.has_wares_in_hq(state, player_id, ware_costs):
		job["paused_reason"] = HybridConstants.ERR_INSUFFICIENT_WARES
		MilitaryState.emit_event(state, "input_missing", {
			"target_id": int(job.get("target_id", 0)),
			"reason": HybridConstants.ERR_INSUFFICIENT_WARES,
		})
		return false
	job["paused_reason"] = ""
	if not HybridState.spend_wares_from_hq(state, player_id, ware_costs):
		job["paused_reason"] = HybridConstants.ERR_INSUFFICIENT_WARES
		return false
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
