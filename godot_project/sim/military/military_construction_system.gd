class_name MilitaryConstructionSystem
extends RefCounted

## Advances building construction sites; completes them into active buildings.
## Gated by current player power state (computed earlier in the tick).


static func tick(state: GameState) -> void:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var per_tick: int = cfg.global_int("construction_progress_per_tick", 1)
	for site in MilitaryState.all_construction_sites(state):
		if str(site.get("state", "")) != MilitaryConstants.CONS_ACTIVE:
			continue
		var player: Dictionary = MilitaryState.get_player(state, str(site.get("player_id", "")))
		if int(player.get("powered", 1)) <= 0:
			site["paused_reason"] = MilitaryConstants.ERR_UNPOWERED
			continue
		site["paused_reason"] = ""
		var progress: int = int(site.get("progress", 0)) + per_tick
		site["progress"] = progress
		if progress >= int(site.get("required_ticks", 1)):
			_complete_site(state, site, cfg)


static func create_site(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
) -> Dictionary:
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(building_type)
	if bdef.is_empty():
		return {"ok": false, "reason": MilitaryConstants.ERR_UNKNOWN_BUILDING}
	var site_id: int = state.entity_ids.allocate()
	var site := {
		"id": site_id,
		"building_type": building_type,
		"anchor_q": anchor_q,
		"anchor_r": anchor_r,
		"player_id": player_id,
		"progress": 0,
		"required_ticks": int(bdef.get("build_ticks", 1)),
		"credit_cost": int(bdef.get("credit_cost", 0)),
		"state": MilitaryConstants.CONS_ACTIVE,
		"paused_reason": "",
	}
	MilitaryState.add_construction_site(state, site)
	return {"ok": true, "site_id": site_id}


static func _complete_site(state: GameState, site: Dictionary, cfg: MilitaryConfig) -> void:
	site["state"] = MilitaryConstants.CONS_COMPLETED
	var building_id: int = spawn_building(
		state,
		str(site.get("building_type", "")),
		int(site.get("anchor_q", 0)),
		int(site.get("anchor_r", 0)),
		str(site.get("player_id", "")),
		cfg,
	)
	MilitaryState.emit_event(state, "construction_completed", {
		"site_id": int(site.get("id", 0)),
		"building_id": building_id,
		"building_type": str(site.get("building_type", "")),
	})


static func spawn_building(
	state: GameState,
	building_type: String,
	anchor_q: int,
	anchor_r: int,
	player_id: String,
	cfg: MilitaryConfig,
) -> int:
	var bdef: Dictionary = cfg.get_building_def(building_type)
	var building_id: int = state.entity_ids.allocate()
	var building := {
		"id": building_id,
		"building_type": building_type,
		"anchor_q": anchor_q,
		"anchor_r": anchor_r,
		"player_id": player_id,
		"state": MilitaryConstants.BLD_ACTIVE,
		"hp": int(bdef.get("hp", 100)),
		"max_hp": int(bdef.get("hp", 100)),
		"powered": 1,
		"enabled": 1,
		"sight": int(bdef.get("sight", 3)),
		"weapon": str(bdef.get("weapon", "")),
		"weapon_cooldown": 0,
		"rally_q": -1,
		"rally_r": -1,
	}
	MilitaryState.add_building(state, building)
	var player: Dictionary = MilitaryState.get_player(state, player_id)
	if int(bdef.get("is_command_hub", 0)) > 0:
		player["hub_count"] = int(player.get("hub_count", 0)) + 1
	return building_id
