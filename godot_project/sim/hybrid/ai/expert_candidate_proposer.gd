class_name ExpertCandidateProposer
extends RefCounted

## Heuristic expert candidate proposers for modular hybrid AI (PRD A.4).


static func propose_all(
	state: GameState,
	player_id: String,
	snapshot: Dictionary,
	strategic_intent: String,
) -> Array:
	var candidates: Array = []
	candidates.append_array(_economy_expert(state, player_id, snapshot))
	candidates.append_array(_logistics_expert(state, player_id, snapshot))
	candidates.append_array(_military_production_expert(state, player_id, snapshot))
	candidates.append_array(_combat_expert(state, player_id, snapshot, strategic_intent))
	candidates.append_array(_defence_expert(state, player_id, snapshot, strategic_intent))
	candidates.append_array(_scouting_expert(state, player_id, snapshot))
	return candidates


static func _make_candidate(
	source_expert: String,
	macro_id: int,
	player_id: String,
	snapshot: Dictionary,
	urgency: float,
	reason: String,
) -> Dictionary:
	var legal: bool = HybridAiActionSpace._precondition_pass(null, player_id, snapshot, macro_id)
	return {
		"candidate_id": "%s_%d" % [source_expert, macro_id],
		"source_expert": source_expert,
		"macro_action_id": macro_id,
		"macro_action_params": {},
		"precondition_pass": legal,
		"legal_mask_pass": legal,
		"urgency_score": urgency,
		"value_estimate": urgency,
		"risk_score": 0.1,
		"plain_english_reason": reason,
		"compiled_command_preview": HybridAiActionSpace.macro_name(macro_id),
	}


static func _economy_expert(_state: GameState, player_id: String, snapshot: Dictionary) -> Array:
	return [
		_make_candidate("economy", HybridAiActionSpace.MACRO_BUILD_WOOD_ECONOMY, player_id, snapshot, 0.7, "Expand wood economy near HQ"),
		_make_candidate("economy", HybridAiActionSpace.MACRO_BUILD_SAWMILL, player_id, snapshot, 0.6, "Add sawmill for plank production"),
	]


static func _logistics_expert(_state: GameState, player_id: String, snapshot: Dictionary) -> Array:
	return [
		_make_candidate("logistics", HybridAiActionSpace.MACRO_CONNECT_SITE, player_id, snapshot, 0.65, "Connect nearest important site"),
		_make_candidate("logistics", HybridAiActionSpace.MACRO_REPAIR_ROAD, player_id, snapshot, 0.5, "Repair disconnected road"),
	]


static func _military_production_expert(_state: GameState, player_id: String, snapshot: Dictionary) -> Array:
	return [
		_make_candidate("military_production", HybridAiActionSpace.MACRO_BUILD_BARRACKS, player_id, snapshot, 0.75, "Build barracks for rifle squads"),
		_make_candidate("military_production", HybridAiActionSpace.MACRO_QUEUE_RIFLE, player_id, snapshot, 0.8, "Queue rifle squad"),
	]


static func _combat_expert(
	_state: GameState,
	player_id: String,
	snapshot: Dictionary,
	strategic_intent: String,
) -> Array:
	if not HybridAiActionSpace._has_visible_enemy(snapshot):
		return [
			_make_candidate("combat", HybridAiActionSpace.MACRO_NOOP, player_id, snapshot, 0.1, "No visible enemy — wait"),
		]
	var urgency: float = 0.9 if strategic_intent == "attack_to_eliminate" else 0.5
	return [
		_make_candidate("combat", HybridAiActionSpace.MACRO_ATTACK_WEAKEST, player_id, snapshot, urgency, "Attack weakest visible enemy"),
	]


static func _defence_expert(
	_state: GameState,
	player_id: String,
	snapshot: Dictionary,
	strategic_intent: String,
) -> Array:
	if strategic_intent != "defend":
		return []
	return [
		_make_candidate("defence", HybridAiActionSpace.MACRO_DEFEND_BASE, player_id, snapshot, 0.85, "Defend Command Hub"),
		_make_candidate("defence", HybridAiActionSpace.MACRO_BUILD_GUARD_POST, player_id, snapshot, 0.7, "Build guard post"),
	]


static func _scouting_expert(_state: GameState, player_id: String, snapshot: Dictionary) -> Array:
	return [
		_make_candidate("scouting", HybridAiActionSpace.MACRO_EXPAND_RESOURCES, player_id, snapshot, 0.55, "Expand toward resource cluster"),
	]
