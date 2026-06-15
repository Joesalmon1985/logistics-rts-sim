class_name PolicyEpisodeResult
extends RefCounted

## Actor-centric win/loss/draw from authoritative state (RUN_06b).

const FITNESS_WIN: float = 1.0
const FITNESS_DRAW: float = 0.5
const FITNESS_LOSS: float = 0.0


static func compute_episode_result(
	state: GameState,
	actor_id: String,
	scenario: Dictionary = {},
) -> Dictionary:
	if state == null:
		return _result("draw", "", "no_state", FITNESS_DRAW)

	match state.variant_id:
		"military_trial":
			return _military_result(state, actor_id)
		"economy_trial":
			return _economy_result(state, actor_id, scenario)
		_:
			return _result("draw", "", "unknown_variant", FITNESS_DRAW)


static func _military_result(state: GameState, actor_id: String) -> Dictionary:
	var decided: int = int(MilitaryState.get_meta_value(state, "victory_decided", 0))
	if decided != 1:
		return _result("loss", "", "timeout_or_ongoing", FITNESS_LOSS)

	var winner: String = str(MilitaryState.get_meta_value(state, "winner_player_id", ""))
	var victory_state: String = str(MilitaryState.get_meta_value(state, "victory_state", ""))
	if victory_state == MilitaryConstants.VICTORY_DRAW or winner == "":
		return _result("draw", "", "draw", FITNESS_DRAW)
	if winner == actor_id:
		return _result("win", winner, "military_victory", FITNESS_WIN)
	return _result("loss", winner, "military_defeat", FITNESS_LOSS)


static func _economy_result(state: GameState, actor_id: String, scenario: Dictionary) -> Dictionary:
	var victory: Dictionary = scenario.get("victory", {})
	var vtype: String = str(victory.get("type", ""))
	if vtype == "":
		return _result("loss", "", "no_victory_config", FITNESS_LOSS)

	match vtype:
		"complete_building":
			var target: String = str(victory.get("building", ""))
			for building in EconomyState.all_buildings(state):
				if str(building.get("player_id", "")) != actor_id:
					continue
				if str(building.get("building_type", "")) == target:
					if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
						return _result("win", actor_id, "building_complete", FITNESS_WIN)
			return _result("loss", "", "building_incomplete", FITNESS_LOSS)
		"construction_started":
			var site_target: String = str(victory.get("building", ""))
			for site in EconomyState.all_construction_sites(state):
				if str(site.get("player_id", "")) != actor_id:
					continue
				if str(site.get("building_type", "")) == site_target:
					if str(site.get("state", "")) != EconomyConstants.CONS_COMPLETED:
						return _result("win", actor_id, "construction_started", FITNESS_WIN)
			return _result("loss", "", "construction_not_started", FITNESS_LOSS)
		"ware_threshold":
			var ware: String = str(victory.get("ware", "wood"))
			var amount: int = int(victory.get("amount", 1))
			var total: int = _player_ware_total(state, actor_id, ware)
			if total >= amount:
				return _result("win", actor_id, "ware_threshold", FITNESS_WIN)
			return _result("loss", "", "ware_below_threshold", FITNESS_LOSS)
		"worker_policy":
			var expected: String = str(victory.get("policy", "auto"))
			var policies: Dictionary = EconomyState.get_meta_value(state, "worker_policies", {})
			if str(policies.get(actor_id, "")) == expected:
				return _result("win", actor_id, "worker_policy", FITNESS_WIN)
			return _result("loss", "", "worker_policy_not_set", FITNESS_LOSS)
		_:
			return _result("loss", "", "unsupported_victory_type", FITNESS_LOSS)


static func _player_ware_total(state: GameState, actor_id: String, ware_type: String) -> int:
	var total: int = 0
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != actor_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0:
			continue
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		total += int(store.get("wares", {}).get(ware_type, 0))
	return total


static func _result(outcome: String, winner_player_id: String, reason: String, fitness: float) -> Dictionary:
	return {
		"outcome": outcome,
		"winner_player_id": winner_player_id,
		"reason": reason,
		"fitness": fitness,
	}
