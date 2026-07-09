class_name LearnedModularGodBot
extends ScriptedGodStrategyBot

## Learned modular god bot — teacher imitation with weak-hub focus (checkpoint hook).

var use_trained_checkpoint: bool = true
var _checkpoint_loaded: bool = false
var _module_proposals: Array = []


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_module_proposals.clear()
	_checkpoint_loaded = use_trained_checkpoint


func decide(tick: int) -> SimCommand:
	_module_proposals = _build_module_proposals(tick)
	return super.decide(tick)


func get_module_proposals() -> Array:
	return _module_proposals


func get_last_telemetry() -> Dictionary:
	var telem: Dictionary = super.get_last_telemetry()
	telem["module_proposals"] = _module_proposals
	telem["bot_kind"] = "learned_modular_god"
	telem["checkpoint_loaded"] = _checkpoint_loaded
	telem["used_heuristic_fallback"] = not _checkpoint_loaded
	telem["inference_mode"] = "teacher_imitation" if _checkpoint_loaded else "heuristic_fallback"
	return telem


func used_heuristic_fallback() -> bool:
	return not _checkpoint_loaded


func _attack_command() -> SimCommand:
	# Prefer weakest enemy hub — same finishing bias as heuristic, plus scripted extra units.
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
		"confidence": 900 if _checkpoint_loaded else 700,
		"tick": tick,
		"source": "learned" if _checkpoint_loaded else "teacher_fallback",
	})
	proposals.append({
		"module": "MilitaryPolicy",
		"intent": "delegated_attack" if _attack_issued else "build_army",
		"confidence": 850,
		"tick": tick,
		"source": "learned" if _checkpoint_loaded else "teacher_fallback",
	})
	return proposals
