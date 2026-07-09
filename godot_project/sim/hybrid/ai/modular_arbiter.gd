class_name ModularArbiter
extends RefCounted

## Chooses one legal expert candidate or safe noop (PRD A.4.6).


static func choose(
	candidates: Array,
	legal_mask: PackedInt32Array,
	strategic_intent: String,
) -> Dictionary:
	var best: Dictionary = {}
	var best_score: float = -1.0
	for c in candidates:
		if not bool(c.get("legal_mask_pass", false)):
			continue
		var macro_id: int = int(c.get("macro_action_id", 0))
		if macro_id >= 0 and macro_id < legal_mask.size() and legal_mask[macro_id] == 0:
			continue
		var score: float = float(c.get("urgency_score", 0.0))
		if strategic_intent == "defend" and str(c.get("source_expert", "")) == "defence":
			score += 0.2
		if strategic_intent == "attack_to_eliminate" and str(c.get("source_expert", "")) == "combat":
			score += 0.25
		if score > best_score:
			best_score = score
			best = c

	if best.is_empty():
		return {
			"chosen_candidate_id": "noop",
			"chosen_macro_action": HybridAiActionSpace.MACRO_NOOP,
			"confidence": 0.0,
			"plain_english_reason": "All candidates illegal or blocked — wait",
			"arbiter_scores": {},
		}

	return {
		"chosen_candidate_id": str(best.get("candidate_id", "")),
		"chosen_candidate_index": _candidate_index(candidates, best),
		"chosen_macro_action": int(best.get("macro_action_id", 0)),
		"confidence": best_score,
		"plain_english_reason": str(best.get("plain_english_reason", "")),
		"candidate_source_expert": str(best.get("source_expert", "")),
		"arbiter_scores": _score_map(candidates),
	}


static func _candidate_index(candidates: Array, chosen: Dictionary) -> int:
	var cid: String = str(chosen.get("candidate_id", ""))
	for i in range(candidates.size()):
		if str(candidates[i].get("candidate_id", "")) == cid:
			return i
	return 0


static func _score_map(candidates: Array) -> Dictionary:
	var scores: Dictionary = {}
	for c in candidates:
		scores[str(c.get("candidate_id", ""))] = float(c.get("urgency_score", 0.0))
	return scores
