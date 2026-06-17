class_name EconomyHumanProofOracleRunner
extends RefCounted

## Headless dual-turn command-plan runner for ECON_BOT_002 proof scenario.

var runner: SimulationTickRunner
var validator: CommandValidator
var recorder: ReplayRecorder
var scenario: Dictionary = {}
var state: GameState
var print_each_step: bool = false


func _init(p_runner: SimulationTickRunner = null, p_validator: CommandValidator = null) -> void:
	runner = p_runner if p_runner != null else SimulationTickRunner.new()
	validator = p_validator if p_validator != null else CommandValidator.new(CommandRegistry.create_default())
	runner.command_validator = validator
	recorder = ReplayRecorder.new()


func reset() -> GameState:
	scenario = EconomyHumanProofContract.proof_scenario()
	recorder.clear()
	state = EconomyHumanProofContract.create_proof_state(runner)
	return state


func load_plan_from_path(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {"ok": false, "error": "cannot_open:%s" % path}
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		return {"ok": false, "error": "invalid_json"}
	return {"ok": true, "plan": parsed as Dictionary}


func run_plan(plan: Dictionary) -> Dictionary:
	if state == null:
		reset()
	var turns: Array = plan.get("turns", [])
	if turns.is_empty() and plan.has("commands"):
		turns = _pair_flat_commands(plan.get("commands", []))
	var max_ticks: int = int(plan.get("max_ticks", scenario.get("max_ticks", 800)))
	var idle_ticks: int = int(plan.get("idle_ticks_after", plan.get("post_turn_ticks", 0)))
	var idle_budget: int = idle_ticks
	var turn_index: int = 0
	for turn_entry in turns:
		if state.tick >= max_ticks:
			break
		var cmds: Array = []
		if typeof(turn_entry) == TYPE_DICTIONARY and turn_entry.has("commands"):
			cmds = turn_entry.get("commands", [])
		elif typeof(turn_entry) == TYPE_ARRAY:
			cmds = turn_entry
		else:
			continue
		_apply_turn_commands(cmds)
		if print_each_step:
			_print_step("turn_%d" % turn_index)
		state = runner.tick(state)
		turn_index += 1
		if state.tick >= max_ticks:
			break
	var idle_guard: int = 0
	while idle_budget > 0 and state.tick < max_ticks and idle_guard < idle_ticks + 10:
		idle_guard += 1
		state = runner.tick(state)
		idle_budget -= 1
		if print_each_step and idle_budget % 50 == 0:
			_print_step("idle")
	return build_summary()


func apply_single_turn(p1_command: Dictionary, p2_command: Dictionary) -> Dictionary:
	var cmds: Array = []
	if not p1_command.is_empty():
		cmds.append(p1_command)
	if not p2_command.is_empty():
		cmds.append(p2_command)
	_apply_turn_commands(cmds)
	state = runner.tick(state)
	if print_each_step:
		_print_step("manual")
	return build_summary()


func step_idle_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		if state == null:
			return
		state = runner.tick(state)


func build_summary() -> Dictionary:
	EconomyDiagnosticsSystem.tick(state)
	return {
		"tick": state.tick,
		"state": state,
		"scenario": scenario,
		"gates": EconomyHumanProofContract.evaluate_match_gates(state),
		"recorder_entries": recorder.get_command_entries().size(),
	}


func export_replay(path: String) -> bool:
	return ReplayFile.save_from_run(state, recorder, scenario, path)


func export_summary_sidecar(path: String) -> bool:
	var summary: Dictionary = build_summary()
	var gates: Dictionary = summary.get("gates", {})
	var payload := {
		"tick": int(summary.get("tick", 0)),
		"gates": gates,
		"command_count": recorder.get_command_entries().size(),
	}
	return SimPaths.write_text_file(path, JSON.stringify(payload, "\t"))


func _apply_turn_commands(cmds: Array) -> void:
	var p1_done: bool = false
	var p2_done: bool = false
	for entry in cmds:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var player_id: String = str(entry.get("player_id", ""))
		if player_id == "p1" and p1_done:
			continue
		if player_id == "p2" and p2_done:
			continue
		var command: SimCommand = _command_from_entry(entry)
		if command == null:
			continue
		var result: CommandResult = validator.validate_and_apply(state, command)
		recorder.record(state.tick, command, result)
		if player_id == "p1":
			p1_done = true
		elif player_id == "p2":
			p2_done = true
	if not p1_done:
		var noop_p1 := NoopCommand.new("p1", state.tick)
		recorder.record(state.tick, noop_p1, CommandResult.ok())
	if not p2_done:
		var noop_p2 := NoopCommand.new("p2", state.tick)
		recorder.record(state.tick, noop_p2, CommandResult.ok())


func _command_from_entry(entry: Dictionary) -> SimCommand:
	var player_id: String = str(entry.get("player_id", "p1"))
	var cmd_type: String = str(entry.get("type", ""))
	var payload: Dictionary = entry.get("payload", {}).duplicate(true)
	payload["player_id"] = player_id
	payload["tick"] = state.tick
	if cmd_type == "NoopCommand":
		return NoopCommand.new(player_id, state.tick)
	return validator.registry.create(cmd_type, payload)


func _pair_flat_commands(flat: Array) -> Array:
	var turns: Array = []
	var i: int = 0
	while i < flat.size():
		var pair: Array = []
		if i < flat.size():
			pair.append(flat[i])
			i += 1
		if i < flat.size():
			pair.append(flat[i])
			i += 1
		turns.append({"commands": pair})
	return turns


func _print_step(label: String) -> void:
	print("[%s tick=%d] %s" % [label, state.tick, EconomyHumanProofContract.format_gate_line(state, "p1")])
	print("[%s tick=%d] %s" % [label, state.tick, EconomyHumanProofContract.format_gate_line(state, "p2")])
