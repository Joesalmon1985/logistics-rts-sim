class_name VictoryPointSystem
extends RefCounted


static func set_vp(state: GameState, player_id: String, vp: int) -> void:
	var player: Dictionary = MilitaryState.get_player(state, player_id)
	player["god_strategy_vp"] = maxi(0, vp)


static func add_vp(state: GameState, player_id: String, delta: int) -> void:
	set_vp(state, player_id, get_vp(state, player_id) + delta)


static func get_vp(state: GameState, player_id: String) -> int:
	return int(MilitaryState.get_player(state, player_id).get("god_strategy_vp", 0))


static func players_at_or_above(state: GameState, threshold: int) -> Array[String]:
	var result: Array[String] = []
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid != "" and get_vp(state, pid) >= threshold:
			result.append(pid)
	return result


static func evaluate_age3_end(state: GameState) -> Dictionary:
	var qualified: Array[String] = players_at_or_above(state, GodStrategyConstants.AGE3_VP_THRESHOLD)
	if qualified.size() == 1:
		return {
			"match_over": true,
			"winner_player_id": qualified[0],
			"end_reason": "age3_vp",
			"advance_to_age4": false,
		}
	if qualified.size() >= 2:
		return {
			"match_over": false,
			"winner_player_id": "",
			"end_reason": "age3_multi_vp_tie",
			"advance_to_age4": true,
		}
	return {
		"match_over": false,
		"winner_player_id": "",
		"end_reason": "",
		"advance_to_age4": false,
	}
