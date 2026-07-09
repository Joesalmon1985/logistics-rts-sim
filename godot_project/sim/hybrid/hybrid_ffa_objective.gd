class_name HybridFfaObjective
extends RefCounted

## Four-player FFA objective — sole remaining player after Command Hub elimination.


static func evaluate(state: GameState, config: Dictionary = {}) -> Dictionary:
	if state == null:
		return _empty_result()

	var alive: Array[String] = []
	var eliminated: Array[String] = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		if _player_alive(state, pid):
			alive.append(pid)
		else:
			eliminated.append(pid)

	var winner: String = ""
	var match_over: bool = false
	if alive.size() <= 1:
		match_over = true
		if alive.size() == 1:
			winner = alive[0]

	return {
		"alive_players": alive,
		"eliminated_players": eliminated,
		"winner_player_id": winner,
		"match_over": match_over,
		"victory": match_over,
		"elimination_rule": str(config.get("elimination_rule", "command_hub_destroyed")),
		"winner_rule": str(config.get("winner_rule", "sole_remaining_player")),
		"tick": state.tick,
	}


static func _player_alive(state: GameState, player_id: String) -> bool:
	var player: Dictionary = MilitaryState.get_player(state, player_id)
	if player.is_empty():
		return false
	if int(player.get("defeated", 0)) != 0:
		return false
	for building in MilitaryState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			return false
		return true
	# No command hub found — treat as eliminated.
	return false


static func _empty_result() -> Dictionary:
	return {
		"alive_players": [],
		"eliminated_players": [],
		"winner_player_id": "",
		"match_over": false,
		"victory": false,
		"elimination_rule": "command_hub_destroyed",
		"winner_rule": "sole_remaining_player",
		"tick": 0,
	}
