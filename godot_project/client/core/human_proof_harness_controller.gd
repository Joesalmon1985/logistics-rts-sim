class_name HumanProofHarnessController
extends RefCounted

## Dual-turn human proof controller for ECON_BOT_002 scenario (non-authoritative client).

signal snapshot_refreshed(snapshot: Dictionary)
signal command_result(result: Dictionary, dto: Dictionary)
signal gates_updated(gates: Dictionary)

var viewer: ViewerController
var oracle_runner: EconomyHumanProofOracleRunner
var active_player_id: String = "p1"
var scenario: Dictionary = {}
var recorder: ReplayRecorder
var rejected_log: Array = []


func _init() -> void:
	viewer = ViewerController.new()
	oracle_runner = EconomyHumanProofOracleRunner.new()
	oracle_runner.validator = viewer.submitter.validator if viewer.submitter != null else CommandValidator.new(CommandRegistry.create_default())
	recorder = oracle_runner.recorder
	viewer.snapshot_refreshed.connect(_on_viewer_snapshot)
	viewer.command_result.connect(_on_viewer_command_result)


func reset_proof_scenario() -> void:
	scenario = EconomyHumanProofContract.proof_scenario()
	rejected_log.clear()
	oracle_runner.reset()
	viewer.runner = oracle_runner.runner
	active_player_id = "p1"
	viewer.load_demo_state(oracle_runner.state, "p1")
	_emit_gates()


func load_oracle_plan_path(path: String) -> Dictionary:
	var loaded: Dictionary = oracle_runner.load_plan_from_path(path)
	if not bool(loaded.get("ok", false)):
		return loaded
	var summary: Dictionary = oracle_runner.run_plan(loaded.get("plan", {}))
	viewer.state = oracle_runner.state
	active_player_id = "p1"
	viewer.refresh_snapshot()
	_emit_gates()
	return {"ok": true, "summary": summary}


func get_state() -> GameState:
	return viewer.state


func get_snapshot() -> Dictionary:
	return viewer.snapshot


func get_available_commands() -> Array:
	var all_cmds: Array = viewer.get_available_commands()
	var filtered: Array = []
	for entry in all_cmds:
		var payload: Dictionary = entry.get("payload", entry.get("params", {}))
		var owner: String = str(payload.get("player_id", viewer.actor_id))
		if owner == active_player_id or owner == "":
			filtered.append(entry)
	return filtered


func submit_active_command_dto(dto: Dictionary) -> CommandResult:
	if viewer.state == null:
		return CommandResult.fail("NO_STATE", "No game state loaded")
	var payload: Dictionary = dto.get("payload", {}).duplicate(true)
	payload["player_id"] = active_player_id
	dto = dto.duplicate(true)
	dto["payload"] = payload
	dto["actor_id"] = active_player_id
	var result: CommandResult = viewer.submitter.submit_from_dto(viewer.state, dto)
	var command: SimCommand = viewer.submitter.validator.registry.create(
		str(dto.get("type", "")),
		payload,
	)
	if command != null:
		recorder.record(viewer.state.tick, command, result)
	if not result.success:
		rejected_log.append({
			"tick": viewer.state.tick,
			"player_id": active_player_id,
			"type": str(dto.get("type", "")),
			"error_code": result.error_code,
			"message": result.message,
		})
	viewer.refresh_snapshot()
	command_result.emit(result.to_dict(), dto)
	_advance_turn_after_command()
	return result


func submit_noop_for_active_player() -> void:
	var noop := NoopCommand.new(active_player_id, viewer.state.tick)
	var result: CommandResult = viewer.submitter.validator.validate_and_apply(viewer.state, noop)
	recorder.record(viewer.state.tick, noop, result)
	_advance_turn_after_command()


func step_dual_tick() -> void:
	if viewer.state == null:
		return
	if active_player_id != "p1":
		active_player_id = "p1"
	viewer.state = oracle_runner.runner.tick(viewer.state)
	oracle_runner.state = viewer.state
	active_player_id = "p1"
	viewer.refresh_snapshot()
	_emit_gates()


func step_dual_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		step_dual_tick()


func build_gate_panel_text() -> String:
	if viewer.state == null:
		return "Gates: (no state)"
	var report: Dictionary = EconomyHumanProofContract.evaluate_match_gates(viewer.state)
	var thresholds: Dictionary = EconomyHumanProofContract.gate_thresholds()
	var lines: PackedStringArray = PackedStringArray()
	lines.append(
		"Tick %d / %d | Active: %s | Overall pass: %s"
		% [
			int(report.get("tick", 0)),
			int(report.get("max_ticks", 800)),
			active_player_id,
			str(report.get("pass", false)),
		]
	)
	for player_id in ["p1", "p2"]:
		var g: Dictionary = report.get("players", {}).get(player_id, {})
		lines.append(
			"%s  b %d/%d  r %d/%d  d %d/%d  stall %d/%d  score %d"
			% [
				player_id,
				int(g.get("buildings", 0)), int(thresholds.get("buildings", 5)),
				int(g.get("roads", 0)), int(thresholds.get("roads", 10)),
				int(g.get("deliveries", 0)), int(thresholds.get("deliveries", 10)),
				int(g.get("stall_ticks", 0)), int(thresholds.get("stall_ticks", 300)),
				int(g.get("score", 0)),
			]
		)
	return "\n".join(lines)


func build_instructions_text() -> String:
	return (
		"[b]ECON_BOT_002 Human Proof[/b]\n"
		+ "Goal: reach b≥5, r≥10, d≥10 per player by tick 800 (delta from baseline).\n"
		+ "Dual-turn: submit command as active player → switches p1/p2 → after p2, sim ticks.\n"
		+ "Use Step Dual Tick to advance both players with noops.\n"
		+ "Reset loads exact proof scenario (seed 28001, HQ spine, no starter buildings).\n"
		+ "Export saves replay JSON under training_runs/human_proof/."
	)


func export_attempt(export_path: String) -> bool:
	if viewer.state == null:
		return false
	oracle_runner.state = viewer.state
	return oracle_runner.export_replay(export_path)


func export_summary(export_path: String) -> bool:
	if viewer.state == null:
		return false
	oracle_runner.state = viewer.state
	return oracle_runner.export_summary_sidecar(export_path)


func compare_command_gaps() -> Dictionary:
	if viewer.state == null:
		return {}
	return EconomyHumanProofContract.compare_command_availability(viewer.state, active_player_id)


func _advance_turn_after_command() -> void:
	if active_player_id == "p1":
		active_player_id = "p2"
		viewer.actor_id = "p2"
		viewer.refresh_snapshot()
		_emit_gates()
		return
	active_player_id = "p1"
	viewer.actor_id = "p1"
	step_dual_tick()


func _on_viewer_snapshot(snapshot: Dictionary) -> void:
	snapshot_refreshed.emit(snapshot)


func _on_viewer_command_result(result: Dictionary, dto: Dictionary) -> void:
	command_result.emit(result, dto)


func _emit_gates() -> void:
	if viewer.state == null:
		return
	gates_updated.emit(EconomyHumanProofContract.evaluate_match_gates(viewer.state))
