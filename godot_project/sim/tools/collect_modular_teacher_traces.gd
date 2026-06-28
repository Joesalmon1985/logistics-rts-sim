extends SceneTree

## Collect modular teacher traces for neural imitation (RUN actual training).

const SCENARIO := "scenarios/hybrid/hybrid_4p_training_ffa.json"
const OUT_DIR := "res://../docs/audit/neural_hybrid_ai/datasets/"


func _initialize() -> void:
	var seeds: Array = [44001, 44002, 44003]
	var max_ticks: int = 120
	var rows: Array = []
	for seed_val in seeds:
		rows.append_array(_collect_scenario(seed_val, max_ticks))
	var out_path: String = _resolve_out("modular_teacher_traces.jsonl")
	_write_jsonl(out_path, rows)
	print("TEACHER TRACE PASS: %d rows -> %s" % [rows.size(), out_path])
	quit(0)


func _collect_scenario(seed_val: int, max_ticks: int) -> Array:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var quad := QuadBotRunner.new(runner)
	var scenario: Dictionary = ConfigLoader.load_scenario(SCENARIO)
	scenario["seed"] = seed_val
	var state: GameState = runner.create_game(scenario)
	for spawn in scenario.get("spawns", []):
		HybridMapSetup.prepare_playable_map(
			state, 0, 0, state.map.width - 1, state.map.height - 1,
			int(spawn.get("q", 0)), int(spawn.get("r", 0)),
		)
	var bots: Dictionary = {}
	for pid in ["p1", "p2", "p3", "p4"]:
		var bot := ModularNeuralHybridBot.new(pid)
		bot.use_trained_checkpoint = false
		bot.use_heuristic_teacher = true
		quad.set_bot(pid, bot)
		bots[pid] = bot
	var collected: Array = []
	for _t in range(max_ticks):
		EconomyDiagnosticsSystem.tick(state, "teacher")
		for pid in ["p1", "p2", "p3", "p4"]:
			var bot: ModularNeuralHybridBot = bots[pid]
			var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, pid)
			bot.observe(snap, pid)
			var before: Dictionary = bot.get_last_telemetry()
			var cmd: SimCommand = bot.decide(state.tick)
			if cmd != null and cmd.type != "NoopCommand":
				runner.command_validator.validate_and_apply(state, cmd)
			var telem: Dictionary = bot.get_last_telemetry()
			if telem.is_empty():
				continue
			var obs: Dictionary = HybridAiObservationExporter.export_observation(state, pid)
			collected.append({
				"tick": state.tick,
				"player_id": pid,
				"scenario": SCENARIO,
				"seed": seed_val,
				"observation_vector": obs.get("vector", []),
				"legal_action_mask": _mask_to_array(obs.get("legal_action_mask")),
				"expert_candidates": telem.get("expert_candidates", []),
				"chosen_action": telem.get("chosen_action", 0),
				"candidate_source_expert": telem.get("candidate_source_expert", ""),
				"arbiter_scores": telem.get("arbiter_scores", {}),
				"chosen_candidate_index": telem.get("chosen_candidate_index", 0),
				"reward": 0.01,
				"inference_mode": telem.get("inference_mode", "heuristic_teacher"),
			})
		state = runner.tick(state)
		var ffa: Dictionary = HybridFfaObjective.evaluate(state)
		if bool(ffa.get("match_over", false)):
			break
	return collected


func _mask_to_array(mask: Variant) -> Array:
	if mask is PackedInt32Array:
		var out: Array = []
		for v in mask:
			out.append(int(v))
		return out
	return []


func _resolve_out(filename: String) -> String:
	var rel: String = OUT_DIR.path_join(filename)
	return ProjectSettings.globalize_path(rel)


func _write_jsonl(path: String, rows: Array) -> void:
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		push_error("Cannot write %s" % path)
		return
	for row in rows:
		f.store_line(JSON.stringify(row))
