class_name CultureDraftSystem
extends RefCounted

const CONFIG_PATH: String = "res://sim/data/config/god_strategy/culture/prehistoric_deck.json"


static func load_deck(age_id: String) -> Array:
	var path: String = "res://sim/data/config/god_strategy/culture/%s_deck.json" % age_id
	if not FileAccess.file_exists(path):
		path = CONFIG_PATH
	var raw: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
	if typeof(raw) != TYPE_DICTIONARY:
		return []
	return raw.get("cards", [])


static func start_draft_for_current_age(state: GameState) -> void:
	var age_id: String = GodStrategyState.current_age_id(state)
	var deck: Array = load_deck(age_id)
	var rng: SeededRng = SeededRng.from_seed(state.seed + state.tick + GodStrategyState.current_age_index(state))
	var shuffled: Array = _shuffle_deterministic(deck, rng)
	var bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_DRAFT)
	bucket.clear()
	bucket["age_id"] = age_id
	bucket["pick_index"] = 0
	bucket["hands"] = {}
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		if pid == "":
			continue
		var hand: Array = []
		for i in range(GodStrategyConstants.DRAFT_PICKS_PER_AGE):
			if shuffled.is_empty():
				break
			var card_index: int = rng.next_int(0, shuffled.size() - 1)
			hand.append(shuffled[card_index].duplicate(true))
		bucket["hands"][pid] = hand
		bucket["picks"] = bucket.get("picks", {})
		bucket["picks"][pid] = []


static func _shuffle_deterministic(cards: Array, rng: SeededRng) -> Array:
	var result: Array = cards.duplicate()
	for i in range(result.size() - 1, 0, -1):
		var j: int = rng.next_int(0, i)
		var tmp: Variant = result[i]
		result[i] = result[j]
		result[j] = tmp
	return result


static func pick_next_card(state: GameState, player_id: String) -> Dictionary:
	var bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_DRAFT)
	var hands: Dictionary = bucket.get("hands", {})
	var hand: Array = hands.get(player_id, [])
	if hand.is_empty():
		return {}
	var card: Dictionary = hand[0]
	hand.remove_at(0)
	hands[player_id] = hand
	var picks: Dictionary = bucket.get("picks", {})
	var player_picks: Array = picks.get(player_id, [])
	player_picks.append(card.duplicate(true))
	picks[player_id] = player_picks
	bucket["picks"] = picks
	_apply_card_effect(state, player_id, card)
	return card


static func complete_draft_for_all_players(state: GameState) -> void:
	for _round in range(GodStrategyConstants.DRAFT_PICKS_PER_AGE):
		for player in MilitaryState.all_players(state):
			pick_next_card(state, str(player.get("player_id", "")))


static func draft_active(state: GameState) -> bool:
	var bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_DRAFT)
	var hands: Dictionary = bucket.get("hands", {})
	for pid in hands.keys():
		if (hands[pid] as Array).size() > 0:
			return true
	return false


static func draft_complete(state: GameState) -> bool:
	var bucket: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_DRAFT)
	for player in MilitaryState.all_players(state):
		var pid: String = str(player.get("player_id", ""))
		var picks: Array = bucket.get("picks", {}).get(pid, [])
		if picks.size() < GodStrategyConstants.DRAFT_PICKS_PER_AGE:
			return false
	return true


static func has_unlock(state: GameState, player_id: String, unlock_id: String) -> bool:
	var unlocks: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_UNLOCKS)
	return unlocks.get(player_id, {}).has(unlock_id)


static func _apply_card_effect(state: GameState, player_id: String, card: Dictionary) -> void:
	var unlocks: Dictionary = GodStrategyState.get_bucket(state, GodStrategyConstants.BUCKET_UNLOCKS)
	if not unlocks.has(player_id):
		unlocks[player_id] = {}
	var unlock_id: String = str(card.get("unlock_id", ""))
	if unlock_id != "":
		unlocks[player_id][unlock_id] = card.duplicate(true)
	var vp_gain: int = int(card.get("vp", 0))
	if vp_gain > 0:
		VictoryPointSystem.add_vp(state, player_id, vp_gain)
