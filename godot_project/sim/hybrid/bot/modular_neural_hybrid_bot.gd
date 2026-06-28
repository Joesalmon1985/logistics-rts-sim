class_name ModularNeuralHybridBot
extends Bot

## Modular hybrid bot — trained neural checkpoint or heuristic teacher fallback.

var use_trained_checkpoint: bool = true
var use_heuristic_teacher: bool = false

var _strategic: StrategicManager = StrategicManager.new()
var _inference: HybridNeuralInference = HybridNeuralInference.new()
var _decision_log: Array = []
var _last_telemetry: Dictionary = {}
var _checkpoint_path: String = ""
var _checkpoint_loaded: bool = false
var _used_heuristic_fallback: bool = false
var _inference_budget_us: int = 12000
var _planning_interval: int = 1
var _reaction_delay_ticks: int = 0
var _ticks_since_decision: int = 0

static var _shared_inference: HybridNeuralInference
static var _shared_loaded_path: String = ""


func _init(p_player_id: String = "p1", checkpoint_path: String = "") -> void:
	player_id = p_player_id
	actor_id = "modular_neural_hybrid_%s" % p_player_id
	_checkpoint_path = checkpoint_path if checkpoint_path != "" else HybridNeuralInference.default_checkpoint_path()
	_ensure_checkpoint_loaded()


func _ensure_checkpoint_loaded() -> void:
	if not use_trained_checkpoint:
		_checkpoint_loaded = false
		return
	if _shared_inference != null and _shared_loaded_path == _checkpoint_path and _shared_inference.is_loaded():
		_inference = _shared_inference
		_checkpoint_loaded = true
		return
	_inference = HybridNeuralInference.new()
	_checkpoint_loaded = _inference.load_checkpoint(_checkpoint_path)
	if _checkpoint_loaded:
		_shared_inference = _inference
		_shared_loaded_path = _checkpoint_path


func reset(p_player_id: String = "p1") -> void:
	super.reset(p_player_id)
	_strategic.reset()
	_decision_log.clear()
	_last_telemetry = {}
	_used_heuristic_fallback = false
	_ticks_since_decision = 0
	_ensure_checkpoint_loaded()


func configure_difficulty(easy: bool = false, hard: bool = false) -> void:
	if easy:
		_reaction_delay_ticks = 4
		_planning_interval = 3
	elif hard:
		_reaction_delay_ticks = 0
		_planning_interval = 1
	else:
		_reaction_delay_ticks = 2
		_planning_interval = 2


func get_decisions() -> Array:
	return _decision_log


func get_last_telemetry() -> Dictionary:
	return _last_telemetry


func used_heuristic_fallback() -> bool:
	return _used_heuristic_fallback


func checkpoint_loaded() -> bool:
	return _checkpoint_loaded


func get_bot_intent_summary() -> String:
	if _last_telemetry.is_empty():
		return "thinking"
	var mode: String = str(_last_telemetry.get("inference_mode", ""))
	var intent: String = str(_last_telemetry.get("strategic_intent", ""))
	var chosen: String = HybridAiActionSpace.macro_name(int(_last_telemetry.get("chosen_action", 0)))
	return "[%s] %s — chosen: %s" % [mode, intent, chosen]


func decide(tick: int) -> SimCommand:
	_ticks_since_decision += 1
	if _ticks_since_decision < _reaction_delay_ticks:
		return null
	if tick % _planning_interval != 0:
		return null

	var start_us: int = Time.get_ticks_usec()
	var snapshot: Dictionary = _last_snapshot.duplicate(true)
	snapshot["player_id"] = player_id
	var intent: String = _strategic.tick_intent(null, player_id, snapshot, tick)
	var mask: PackedInt32Array = HybridAiActionSpace.legal_mask(null, player_id, snapshot)

	var inference_mode: String = "heuristic_fallback"
	var candidates: Array = []
	var arbiter_result: Dictionary = {}
	var macro_id: int = HybridAiActionSpace.MACRO_NOOP
	_used_heuristic_fallback = false

	if use_heuristic_teacher or (use_trained_checkpoint and not _checkpoint_loaded):
		_used_heuristic_fallback = true
		inference_mode = "heuristic_teacher" if use_heuristic_teacher else "heuristic_fallback"
		candidates = ExpertCandidateProposer.propose_all(null, player_id, snapshot, intent)
		arbiter_result = ModularArbiter.choose(candidates, mask, intent)
		macro_id = int(arbiter_result.get("chosen_macro_action", 0))
	elif use_trained_checkpoint and _checkpoint_loaded:
		var obs: Dictionary = HybridAiObservationExporter.export_from_snapshot(snapshot, player_id)
		var neural: Dictionary = _inference.infer(obs.get("vector", []), mask)
		if not bool(neural.get("ok", false)):
			_used_heuristic_fallback = true
			inference_mode = "heuristic_fallback"
			candidates = ExpertCandidateProposer.propose_all(null, player_id, snapshot, intent)
			arbiter_result = ModularArbiter.choose(candidates, mask, intent)
			macro_id = int(arbiter_result.get("chosen_macro_action", 0))
		else:
			inference_mode = "trained_neural"
			candidates = neural.get("candidates", [])
			macro_id = int(neural.get("chosen_macro_action", 0))
			arbiter_result = {
				"chosen_candidate_id": str(neural.get("chosen_candidate_index", 0)),
				"chosen_candidate_index": int(neural.get("chosen_candidate_index", 0)),
				"chosen_macro_action": macro_id,
				"candidate_source_expert": str(neural.get("candidate_source_expert", "")),
				"plain_english_reason": str(neural.get("plain_english_reason", "")),
				"arbiter_scores": neural.get("arbiter_scores", {}),
			}
	else:
		_used_heuristic_fallback = true
		candidates = ExpertCandidateProposer.propose_all(null, player_id, snapshot, intent)
		arbiter_result = ModularArbiter.choose(candidates, mask, intent)
		macro_id = int(arbiter_result.get("chosen_macro_action", 0))

	var compiled: Dictionary = HybridAiActionSpace.compile_command(null, player_id, snapshot, macro_id, tick)
	var cmd: SimCommand = null
	var validation_ok: bool = false
	if bool(compiled.get("ok", false)):
		cmd = compiled.get("command")
		validation_ok = cmd != null

	if Time.get_ticks_usec() - start_us > _inference_budget_us:
		cmd = NoopCommand.new(player_id, tick)
		arbiter_result["plain_english_reason"] = "Inference budget exceeded — safe noop"

	_last_telemetry = {
		"tick": tick,
		"player_id": player_id,
		"strategic_intent": intent,
		"expert_candidates": candidates,
		"chosen_action": macro_id,
		"chosen_candidate_index": int(arbiter_result.get("chosen_candidate_index", 0)),
		"candidate_source_expert": str(arbiter_result.get("candidate_source_expert", "")),
		"candidate_legal": validation_ok,
		"arbiter_scores": arbiter_result.get("arbiter_scores", {}),
		"plain_english_reason": str(arbiter_result.get("plain_english_reason", "")),
		"validation_result": "ok" if validation_ok else "failed",
		"inference_mode": inference_mode,
		"checkpoint_path": _checkpoint_path,
		"checkpoint_loaded": _checkpoint_loaded,
		"heuristic_fallback": _used_heuristic_fallback,
		"checkpoint_meta": _inference.get_checkpoint_meta() if _checkpoint_loaded else {},
	}
	_decision_log.append(_last_telemetry.duplicate(true))
	_ticks_since_decision = 0
	return cmd
