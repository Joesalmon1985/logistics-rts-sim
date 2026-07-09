class_name GodStrategyMatchEnd
extends RefCounted

## God-strategy match end — hub-site elimination, domination, tick-cap survival score.

const GLOBAL_COLLAPSE_THRESHOLD: int = 9000
const RESTORATION_VICTORY_THRESHOLD: int = 2000


static func is_god_strategy_scenario(scenario: Dictionary) -> bool:
	if bool(scenario.get("god_strategy_mvp_a", false)):
		return true
	if bool(scenario.get("god_strategy", false)):
		return true
	return false


static func evaluate(state: GameState, scenario: Dictionary = {}) -> Dictionary:
	if state == null:
		return _empty_result()

	var alive: Array[String] = []
	var eliminated: Array[String] = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		if player_alive(state, pid):
			alive.append(pid)
		else:
			eliminated.append(pid)

	var winner: String = ""
	var match_over: bool = false
	var end_reason: String = ""

	var pending_winner: String = str(EconomyState.get_meta_value(state, "god_strategy_pending_winner", ""))
	if pending_winner != "":
		return {
			"alive_players": _alive_list(state),
			"eliminated_players": _eliminated_list(state),
			"winner_player_id": pending_winner,
			"match_over": true,
			"victory": true,
			"end_reason": str(EconomyState.get_meta_value(state, "god_strategy_end_reason", "age3_vp")),
			"survival_scores": {},
			"elimination_rule": "command_hub_or_active_hub_site",
			"winner_rule": str(scenario.get("winner_rule", "domination_or_tick_cap_score")),
			"tick": state.tick,
		}

	if PollutionSystem.is_shared_collapse(state):
		return {
			"alive_players": [],
			"eliminated_players": _all_player_ids(state, scenario),
			"winner_player_id": "",
			"match_over": true,
			"victory": false,
			"end_reason": "shared_collapse",
			"survival_scores": {},
			"elimination_rule": "command_hub_or_active_hub_site",
			"winner_rule": str(scenario.get("winner_rule", "domination_or_tick_cap_score")),
			"tick": state.tick,
		}

	var restoration_winner: String = PollutionSystem.restoration_victory_leader(state)
	if restoration_winner != "" and GodStrategyState.current_age_id(state) == "futuristic_collapse":
		return {
			"alive_players": _alive_list(state),
			"eliminated_players": _eliminated_list(state),
			"winner_player_id": restoration_winner,
			"match_over": true,
			"victory": true,
			"end_reason": "restoration_victory",
			"survival_scores": GodStrategyMatchEnd.compute_survival_scores(state),
			"elimination_rule": "command_hub_or_active_hub_site",
			"winner_rule": str(scenario.get("winner_rule", "domination_or_tick_cap_score")),
			"tick": state.tick,
		}

	if alive.size() <= 1:
		match_over = true
		end_reason = "domination"
		if alive.size() == 1:
			winner = alive[0]

	var max_ticks: int = int(scenario.get("max_ticks", 0))
	if not match_over and max_ticks > 0 and state.tick >= max_ticks:
		match_over = true
		end_reason = "tick_cap"
		var scores: Dictionary = compute_survival_scores(state)
		var best_pid: String = ""
		var best_score: int = -1
		for pid in scores.keys():
			var score: int = int(scores[pid])
			if score > best_score:
				best_score = score
				best_pid = str(pid)
			elif score == best_score and str(pid) < best_pid:
				best_pid = str(pid)
		if best_score >= 0 and alive.size() > 1:
			winner = best_pid

	return {
		"alive_players": alive,
		"eliminated_players": eliminated,
		"winner_player_id": winner,
		"match_over": match_over,
		"victory": match_over and winner != "",
		"end_reason": end_reason,
		"survival_scores": compute_survival_scores(state) if match_over else {},
		"elimination_rule": "command_hub_or_active_hub_site",
		"winner_rule": str(scenario.get("winner_rule", "domination_or_tick_cap_score")),
		"tick": state.tick,
	}


static func player_alive(state: GameState, player_id: String) -> bool:
	var player: Dictionary = MilitaryState.get_player(state, player_id)
	if player.is_empty():
		return false
	if int(player.get("defeated", 0)) != 0:
		return false

	var has_destroyed_hub: bool = false
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			has_destroyed_hub = true
			continue
		return true

	if _has_active_hub_construction_site(state, player_id):
		return true

	if has_destroyed_hub:
		return false

	return false


static func _has_active_hub_construction_site(state: GameState, player_id: String) -> bool:
	for site in MilitaryState.all_construction_sites(state):
		if str(site.get("player_id", "")) != player_id:
			continue
		if str(site.get("building_type", "")) != "command_hub":
			continue
		if str(site.get("state", "")) == MilitaryConstants.CONS_ACTIVE:
			return true
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) != player_id:
			continue
		if str(site.get("building_type", "")) != "command_hub":
			continue
		var site_state: String = str(site.get("state", ""))
		if site_state in [EconomyConstants.CONS_ACTIVE, EconomyConstants.CONS_BLOCKED]:
			return true
	return false


static func compute_survival_scores(state: GameState) -> Dictionary:
	var scores: Dictionary = {}
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		var score: int = 0
		if player_alive(state, pid):
			score += 1000
		for unit in MilitaryState.all_units(state):
			if str(unit.get("player_id", "")) == pid:
				score += 10
		for building in EconomyState.all_buildings(state):
			if str(building.get("player_id", "")) == pid:
				score += 5
		for building in MilitaryState.all_buildings(state):
			if str(building.get("player_id", "")) == pid and str(building.get("state", "")) != MilitaryConstants.BLD_DESTROYED:
				score += 5
		scores[pid] = score
	return scores


static func _empty_result() -> Dictionary:
	return {
		"alive_players": [],
		"eliminated_players": [],
		"winner_player_id": "",
		"match_over": false,
		"victory": false,
		"end_reason": "",
		"survival_scores": {},
		"elimination_rule": "command_hub_or_active_hub_site",
		"winner_rule": "domination_or_tick_cap_score",
		"tick": 0,
	}


static func _alive_list(state: GameState) -> Array[String]:
	var alive: Array[String] = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid != "" and player_alive(state, pid):
			alive.append(pid)
	return alive


static func _eliminated_list(state: GameState) -> Array[String]:
	var eliminated: Array[String] = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid != "" and not player_alive(state, pid):
			eliminated.append(pid)
	return eliminated


static func _all_player_ids(state: GameState, scenario: Dictionary) -> Array:
	var ids: Array = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid != "":
			ids.append(pid)
	if ids.is_empty():
		return scenario.get("players", ["p1"])
	return ids


# Keep local helpers as fallbacks if PollutionSystem fails to register (parse errors).
static func _is_shared_collapse(state: GameState) -> bool:
	if not state.entities.has("god_strategy_pollution"):
		return false
	var bucket: Variant = state.entities["god_strategy_pollution"]
	if typeof(bucket) != TYPE_DICTIONARY:
		return false
	return int(bucket.get("global_pollution", 0)) >= GLOBAL_COLLAPSE_THRESHOLD


static func _restoration_victory_leader(state: GameState) -> String:
	if GodStrategyState.current_age_id(state) != "futuristic_collapse":
		return ""
	if not state.entities.has("god_strategy_pollution"):
		return ""
	var bucket: Dictionary = state.entities["god_strategy_pollution"]
	if int(bucket.get("global_pollution", 0)) > RESTORATION_VICTORY_THRESHOLD:
		return ""
	if int(bucket.get("restoration_score", 0)) < 5000:
		return ""
	var best_pid: String = ""
	var best: int = -1
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "" or not player_alive(state, pid):
			continue
		var score: int = int(bucket.get("restoration_score", 0)) + VictoryPointSystem.get_vp(state, pid) * 100
		if score > best:
			best = score
			best_pid = pid
	return best_pid
