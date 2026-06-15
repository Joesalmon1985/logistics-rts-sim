class_name MilitaryDiagnosticsSystem
extends RefCounted

## Builds deterministic, integer-only diagnostics for bots/observers and emits the
## per-tick `tick_advanced` event. Diagnostics are part of hashed state, so values
## must be integers and stably ordered.


static func tick(state: GameState) -> void:
	state.event_log.append_event("tick_advanced", state.tick, {"tick": state.tick, "variant": state.variant_id})

	var per_player: Dictionary = {}
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		per_player[pid] = {
			"credits": int(player.get("credits", 0)),
			"power_supply": int(player.get("power_supply", 0)),
			"power_demand": int(player.get("power_demand", 0)),
			"powered": int(player.get("powered", 1)),
			"hub_count": int(player.get("hub_count", 0)),
			"defeated": int(player.get("defeated", 0)),
			"unit_count": 0,
			"building_count": 0,
			"idle_units": 0,
		}

	for unit in MilitaryState.all_units(state):
		var pid_u: String = str(unit.get("player_id", ""))
		if per_player.has(pid_u):
			per_player[pid_u]["unit_count"] += 1
			if str(unit.get("state", "")) == MilitaryConstants.UNIT_IDLE:
				per_player[pid_u]["idle_units"] += 1

	for building in MilitaryState.all_buildings(state):
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		var pid_b: String = str(building.get("player_id", ""))
		if per_player.has(pid_b):
			per_player[pid_b]["building_count"] += 1

	state.diagnostics["military"] = {
		"players": per_player,
		"active_projectiles": MilitaryState.all_projectiles(state).size(),
		"construction_sites": _active_sites(state),
		"repair_jobs": MilitaryState.all_repair_jobs(state).size(),
		"victory_state": str(MilitaryState.get_meta_value(state, "victory_state", MilitaryConstants.VICTORY_NONE)),
	}


static func tick_statistics(state: GameState) -> void:
	# Stall detection based on aggregate progress score (credits + entities).
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var score: int = 0
	for player in MilitaryState.all_players(state):
		score += int(player.get("credits", 0))
	score += MilitaryState.all_units(state).size() * 10
	score += MilitaryState.all_buildings(state).size() * 10

	var prev: int = int(MilitaryState.get_meta_value(state, "progress_score", -1))
	if score != prev:
		MilitaryState.set_meta_value(state, "progress_score", score)
		MilitaryState.set_meta_value(state, "last_progress_tick", state.tick)
		MilitaryState.set_meta_value(state, "stall_ticks", 0)
	else:
		var stall: int = int(MilitaryState.get_meta_value(state, "stall_ticks", 0)) + 1
		MilitaryState.set_meta_value(state, "stall_ticks", stall)
		if stall == cfg.global_int("stall_tick_threshold", 200):
			MilitaryState.emit_event(state, "simulation_stalled", {"tick": state.tick, "stall_ticks": stall})


static func _active_sites(state: GameState) -> int:
	var count: int = 0
	for site in MilitaryState.all_construction_sites(state):
		if str(site.get("state", "")) == MilitaryConstants.CONS_ACTIVE:
			count += 1
	return count
