class_name HeuristicModularGodBot
extends ScriptedGodStrategyBot

## Rule-based modular god bot — inspectable proposals; stronger than Phase 0 scripted baseline.

var _module_proposals: Array = []


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_module_proposals.clear()


func decide(tick: int) -> SimCommand:
	_module_proposals = _build_module_proposals(tick)
	return super.decide(tick)


func get_module_proposals() -> Array:
	return _module_proposals


func get_last_telemetry() -> Dictionary:
	var telem: Dictionary = super.get_last_telemetry()
	telem["module_proposals"] = _module_proposals
	telem["bot_kind"] = "heuristic_modular_god"
	return telem


func _attack_command() -> SimCommand:
	# Prefer the weakest enemy hub (lowest HP) to finish eliminations faster than scripted.
	var best: Dictionary = {}
	var best_hp: int = 999999
	for building in _all_buildings():
		if str(building.get("building_type", "")) != "command_hub":
			continue
		if str(building.get("player_id", "")) == player_id:
			continue
		if str(building.get("state", "")) == MilitaryConstants.BLD_DESTROYED:
			continue
		var hp: int = int(building.get("hp", building.get("hit_points", 999999)))
		if hp < best_hp:
			best_hp = hp
			best = building
	if best.is_empty():
		return super._attack_command()
	_last_intent = "delegated_attack"
	_last_target = {
		"q": int(best.get("anchor_q", best.get("q", 0))),
		"r": int(best.get("anchor_r", best.get("r", 0))),
	}
	return DelegatedAttackCommand.new(player_id, _tick_from_snapshot(), {
		"target_q": int(_last_target.get("q", 0)),
		"target_r": int(_last_target.get("r", 0)),
	})


func _build_module_proposals(tick: int) -> Array:
	var proposals: Array = []
	proposals.append({
		"module": "EconomyPolicy",
		"intent": _phase,
		"confidence": 800,
		"tick": tick,
	})
	proposals.append({
		"module": "MilitaryPolicy",
		"intent": "delegated_attack" if _attack_issued else "build_army",
		"confidence": 750,
		"tick": tick,
	})
	if _bound_state != null and CultureDraftSystem.draft_active(_bound_state):
		proposals.append({
			"module": "CultureDraftPolicy",
			"intent": "draft_pick",
			"confidence": 900,
			"tick": tick,
		})
	return proposals
