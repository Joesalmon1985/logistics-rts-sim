class_name MilitaryVictorySystem
extends RefCounted

## Defeat (hub destroyed), victory (last player standing), and optional stall
## detection.


static func tick(state: GameState) -> void:
	# Defeat: any player with no command hub is defeated.
	for player in MilitaryState.all_players(state):
		if int(player.get("defeated", 0)) > 0:
			continue
		if int(player.get("hub_count", 0)) <= 0:
			player["defeated"] = 1
			MilitaryState.emit_event(state, "player_defeated", {"player_id": str(player.get("player_id", ""))})

	# Victory: exactly one survivor among a multi-player game.
	if int(MilitaryState.get_meta_value(state, "victory_decided", 0)) > 0:
		return
	var players: Array = MilitaryState.all_players(state)
	if players.size() < 2:
		return
	var survivors: Array = []
	for player in players:
		if int(player.get("defeated", 0)) <= 0:
			survivors.append(str(player.get("player_id", "")))
	if survivors.size() == 1:
		MilitaryState.set_meta_value(state, "victory_decided", 1)
		MilitaryState.set_meta_value(state, "victory_state", MilitaryConstants.VICTORY_WON)
		MilitaryState.set_meta_value(state, "winner_player_id", survivors[0])
		MilitaryState.emit_event(state, "player_victory", {"player_id": survivors[0]})
	elif survivors.size() == 0:
		MilitaryState.set_meta_value(state, "victory_decided", 1)
		MilitaryState.set_meta_value(state, "victory_state", MilitaryConstants.VICTORY_DRAW)
		MilitaryState.emit_event(state, "game_draw", {})
