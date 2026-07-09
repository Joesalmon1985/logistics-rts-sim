class_name HybridNeuralInference
extends RefCounted

## Godot runtime forward pass for exported modular policy JSON.

const DEFAULT_CHECKPOINT := "res://sim/hybrid/ai/checkpoints/latest_modular_policy.json"

var _loaded: bool = false
var _meta: Dictionary = {}
var _vector_size: int = 13
var _hidden: int = 64
var _macro_count: int = 16
var _max_candidates: int = 24
var _expert_names: Array = []
var _encoder_fc1_w: PackedFloat32Array = PackedFloat32Array()
var _encoder_fc1_b: PackedFloat32Array = PackedFloat32Array()
var _encoder_fc2_w: PackedFloat32Array = PackedFloat32Array()
var _encoder_fc2_b: PackedFloat32Array = PackedFloat32Array()
var _heads: Dictionary = {}


static func default_checkpoint_path() -> String:
	return DEFAULT_CHECKPOINT


func load_checkpoint(path: String = "") -> bool:
	var p: String = path if path != "" else DEFAULT_CHECKPOINT
	if not FileAccess.file_exists(p):
		_loaded = false
		return false
	var text: String = FileAccess.get_file_as_string(p)
	var parsed: Variant = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		_loaded = false
		return false
	var data: Dictionary = parsed
	_vector_size = int(data.get("vector_size", 13))
	_hidden = int(data.get("hidden", 64))
	_macro_count = int(data.get("macro_count", 16))
	_max_candidates = int(data.get("max_candidates", 24))
	_expert_names = data.get("expert_names", [])
	_meta = data.get("meta", {})
	var enc: Dictionary = data.get("encoder", {})
	_encoder_fc1_w = _as_packed(enc.get("fc1", {}).get("weight", []))
	_encoder_fc1_b = _as_packed(enc.get("fc1", {}).get("bias", []))
	_encoder_fc2_w = _as_packed(enc.get("fc2", {}).get("weight", []))
	_encoder_fc2_b = _as_packed(enc.get("fc2", {}).get("bias", []))
	_heads = data.get("heads", {})
	_loaded = true
	return true


func is_loaded() -> bool:
	return _loaded


func get_checkpoint_meta() -> Dictionary:
	return _meta.duplicate(true)


func infer(obs_vector: Array, legal_mask: PackedInt32Array) -> Dictionary:
	if not _loaded:
		return {"ok": false, "error": "checkpoint_not_loaded"}
	var x: PackedFloat32Array = _obs_to_packed(obs_vector)
	var h1: PackedFloat32Array = _linear_relu(x, _vector_size, _hidden, _encoder_fc1_w, _encoder_fc1_b)
	var h: PackedFloat32Array = _linear_relu(h1, _hidden, _hidden, _encoder_fc2_w, _encoder_fc2_b)

	var candidates: Array = []
	for expert in _expert_names:
		var head: Dictionary = _heads.get(str(expert), {})
		if head.is_empty():
			continue
		var logits: PackedFloat32Array = _linear(
			h, _hidden, _macro_count,
			_as_packed(head.get("weight", [])),
			_as_packed(head.get("bias", [])),
		)
		_apply_legal_mask(logits, legal_mask)
		var best_macro: int = _argmax(logits)
		var best_score: float = logits[best_macro] if best_macro < logits.size() else 0.0
		candidates.append({
			"source_expert": str(expert),
			"macro_action_id": best_macro,
			"urgency_score": best_score,
			"candidate_id": "%s_%d" % [expert, best_macro],
			"plain_english_reason": "neural %s proposes %s" % [expert, HybridAiActionSpace.macro_name(best_macro)],
		})

	var arb_head: Dictionary = _heads.get("arbiter", {})
	var arb_logits: PackedFloat32Array = _linear(
		h, _hidden, _max_candidates,
		_as_packed(arb_head.get("weight", [])),
		_as_packed(arb_head.get("bias", [])),
	)
	var n_cand: int = mini(candidates.size(), _max_candidates)
	var chosen_idx: int = 0
	var best_arb: float = -1e9
	for i in range(n_cand):
		if i >= arb_logits.size():
			break
		if arb_logits[i] > best_arb:
			best_arb = arb_logits[i]
			chosen_idx = i
	var chosen: Dictionary = candidates[chosen_idx] if chosen_idx < candidates.size() else {}
	return {
		"ok": true,
		"inference_mode": "trained_neural",
		"checkpoint_meta": _meta,
		"candidates": candidates,
		"chosen_candidate_index": chosen_idx,
		"chosen_macro_action": int(chosen.get("macro_action_id", 0)),
		"candidate_source_expert": str(chosen.get("source_expert", "")),
		"plain_english_reason": str(chosen.get("plain_english_reason", "")),
		"arbiter_scores": _arbiter_score_map(candidates, arb_logits, n_cand),
	}


static func _arbiter_score_map(candidates: Array, arb_logits: PackedFloat32Array, n: int) -> Dictionary:
	var out: Dictionary = {}
	for i in range(n):
		if i >= candidates.size():
			break
		var c: Dictionary = candidates[i]
		out[str(c.get("candidate_id", i))] = float(arb_logits[i]) if i < arb_logits.size() else 0.0
	return out


static func _obs_to_packed(obs_vector: Array) -> PackedFloat32Array:
	var out := PackedFloat32Array()
	for v in obs_vector:
		out.append(float(v))
	return out


static func _as_packed(values: Variant) -> PackedFloat32Array:
	var out := PackedFloat32Array()
	if values is Array:
		for v in values:
			if v is Array:
				for vv in v:
					out.append(float(vv))
			else:
				out.append(float(v))
	return out


static func _linear(inp: PackedFloat32Array, in_dim: int, out_dim: int, w: PackedFloat32Array, b: PackedFloat32Array) -> PackedFloat32Array:
	var out := PackedFloat32Array()
	out.resize(out_dim)
	for o in range(out_dim):
		var sum: float = b[o] if o < b.size() else 0.0
		for i in range(in_dim):
			var wi: int = o * in_dim + i
			if wi < w.size() and i < inp.size():
				sum += w[wi] * inp[i]
		out[o] = sum
	return out


static func _linear_relu(inp: PackedFloat32Array, in_dim: int, out_dim: int, w: PackedFloat32Array, b: PackedFloat32Array) -> PackedFloat32Array:
	var out := _linear(inp, in_dim, out_dim, w, b)
	for i in range(out.size()):
		out[i] = maxf(0.0, out[i])
	return out


static func _apply_legal_mask(logits: PackedFloat32Array, legal_mask: PackedInt32Array) -> void:
	for i in range(mini(logits.size(), legal_mask.size())):
		if legal_mask[i] == 0:
			logits[i] = -1e9


static func _argmax(values: PackedFloat32Array) -> int:
	var best_i: int = 0
	var best_v: float = -1e18
	for i in range(values.size()):
		if values[i] > best_v:
			best_v = values[i]
			best_i = i
	return best_i
