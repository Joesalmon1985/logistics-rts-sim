class_name MilitaryTechSystem
extends RefCounted

## Research advances per player when a powered research lab exists; completed tech
## unlocks gated buildings/units.


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var per_tick: int = cfg.global_int("research_progress_per_tick", 1)
	for player in MilitaryState.all_players(state):
		var player_id: String = str(player.get("player_id", ""))
		var research: Dictionary = MilitaryState.get_research(state, player_id)
		if research.is_empty():
			continue
		if int(research.get("active", 0)) <= 0:
			continue
		if not _has_powered_lab(state, player_id):
			research["paused_reason"] = MilitaryConstants.ERR_UNPOWERED
			continue
		research["paused_reason"] = ""
		research["progress"] = int(research.get("progress", 0)) + per_tick
		if int(research.get("progress", 0)) >= int(research.get("required_ticks", 1)):
			_complete(state, player, research, cfg)


static func _complete(state: GameState, player: Dictionary, research: Dictionary, cfg: MilitaryConfig) -> void:
	var tech_id: String = str(research.get("tech_id", ""))
	research["active"] = 0
	research["progress"] = 0
	var unlocked: Array = player.get("tech_unlocked", [])
	if tech_id not in unlocked:
		unlocked.append(tech_id)
	player["tech_unlocked"] = unlocked
	MilitaryState.emit_event(state, "research_completed", {
		"player_id": str(player.get("player_id", "")),
		"tech_id": tech_id,
		"unlocks": cfg.get_tech_def(tech_id).get("unlocks", []),
	})


static func _has_powered_lab(state: GameState, player_id: String) -> bool:
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "research_lab":
			continue
		if str(building.get("state", "")) != MilitaryConstants.BLD_ACTIVE:
			continue
		if MilitaryPowerSystem.building_powered(state, building):
			return true
	return false


static func can_start(state: GameState, player_id: String, tech_id: String) -> Dictionary:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var tdef: Dictionary = cfg.get_tech_def(tech_id)
	if tdef.is_empty():
		return {"ok": false, "reason": MilitaryConstants.ERR_NO_RESEARCH}
	if MilitaryState.player_has_tech(state, player_id, tech_id):
		return {"ok": false, "reason": MilitaryConstants.ERR_ALREADY_RESEARCHED}
	var research: Dictionary = MilitaryState.get_research(state, player_id)
	if int(research.get("active", 0)) > 0:
		return {"ok": false, "reason": MilitaryConstants.ERR_RESEARCH_BUSY}
	if not _has_powered_lab(state, player_id):
		return {"ok": false, "reason": MilitaryConstants.ERR_UNPOWERED}
	if not MilitaryMapRules.prerequisites_met(state, player_id, tdef):
		return {"ok": false, "reason": MilitaryConstants.ERR_MISSING_PREREQUISITE}
	return {"ok": true, "reason": ""}
