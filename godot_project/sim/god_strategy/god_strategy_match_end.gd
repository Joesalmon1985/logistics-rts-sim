class_name GodStrategyMatchEnd
extends RefCounted

## God-strategy match end — hub-site elimination, domination, tick-cap survival score.


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
