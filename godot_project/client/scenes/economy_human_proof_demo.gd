extends ViewerRoot

## RUN_02.8e — ECON_BOT_002 human proof harness (dual-turn, gate overlays).

var harness: HumanProofHarnessController
var _proof_gates: Label
var _export_btn: Button
var _load_oracle_btn: Button
var _step50_btn: Button
var _dual_tick_btn: Button


func _init() -> void:
	variant_label = "economy"
	viewer_title = "ECON_BOT_002 Human Proof Harness"
	map_title = "Proof map (seed 28001)"
	help_text = ""


func _ready() -> void:
	harness = HumanProofHarnessController.new()
	controller = harness.viewer
	harness.snapshot_refreshed.connect(_on_snapshot_refreshed)
	harness.command_result.connect(_on_command_result)
	harness.gates_updated.connect(_on_gates_updated)
	_proof_gates = get_node_or_null("%ProofGatesLabel")
	_export_btn = get_node_or_null("%ExportBtn")
	_load_oracle_btn = get_node_or_null("%LoadOracleBtn")
	_step50_btn = get_node_or_null("%Step50Btn")
	_dual_tick_btn = get_node_or_null("%DualTickBtn")
	super._ready()
	if _help != null:
		_help.text = harness.build_instructions_text()
	if has_node("%ResetBtn"):
		get_node("%ResetBtn").text = "Reset Proof Scenario"
	if has_node("%BuildTutorialBtn"):
		get_node("%BuildTutorialBtn").visible = false
	if _dual_tick_btn != null:
		_dual_tick_btn.text = "Step Dual Tick"
	reset_proof()


func reset_proof() -> void:
	harness.reset_proof_scenario()
	_append_event("Proof scenario loaded (ECON_BOT_002 contract)")
	_refresh_gates_panel()


func _refresh_gates_panel() -> void:
	if _proof_gates != null:
		_proof_gates.text = harness.build_gate_panel_text()
	if _status != null and harness.get_state() != null:
		_status.text = "Tick %d / 800  |  Active player: %s  |  Dual-turn proof" % [
			harness.get_state().tick,
			harness.active_player_id,
		]


func _on_gates_updated(_gates: Dictionary) -> void:
	_refresh_gates_panel()


func _on_reset_pressed() -> void:
	reset_proof()


func _on_submit_pressed() -> void:
	var idx: Array = _command_list.get_selected_items()
	if idx.is_empty():
		_result.text = "Select a command for %s, then Submit." % harness.active_player_id
		return
	var commands: Array = harness.get_available_commands()
	if int(idx[0]) >= commands.size():
		return
	var dto: Dictionary = commands[int(idx[0])]
	var result: CommandResult = harness.submit_active_command_dto(dto)
	var label: String = str(dto.get("label", dto.get("type", "?")))
	if result.success:
		_result.text = "ACCEPTED (%s): %s" % [harness.active_player_id, label]
	else:
		_result.text = "REJECTED (%s): %s — %s" % [harness.active_player_id, label, result.error_code]
	_refresh_commands()


func _on_step_pressed() -> void:
	harness.submit_noop_for_active_player()
	if harness.active_player_id == "p1":
		harness.submit_noop_for_active_player()
	_append_event("Dual noop step")


func _on_step10_pressed() -> void:
	for _i in range(10):
		harness.submit_noop_for_active_player()
		harness.submit_noop_for_active_player()
	_append_event("Dual noop x10")


func _on_dual_tick_pressed() -> void:
	var before: int = harness.get_state().tick
	harness.step_dual_tick()
	_append_event("Dual tick: %d -> %d" % [before, harness.get_state().tick])


func _on_step50_pressed() -> void:
	for _i in range(50):
		harness.submit_noop_for_active_player()
		harness.submit_noop_for_active_player()
	_append_event("Dual noop x50")


func _on_export_pressed() -> void:
	var dir: String = ProjectSettings.globalize_path("res://").path_join("..").path_join("training_runs/human_proof")
	DirAccess.make_dir_recursive_absolute(dir)
	var stamp: String = Time.get_datetime_string_from_system().replace(":", "-")
	var replay_path: String = dir.path_join("econ_bot_002_human_attempt_%s.json" % stamp)
	var ok: bool = harness.export_attempt(replay_path)
	var summary_path: String = dir.path_join("econ_bot_002_human_attempt_%s_summary.json" % stamp)
	var ok2: bool = harness.export_summary(summary_path)
	_result.text = "Export replay=%s summary=%s\n%s" % [str(ok), str(ok2), replay_path]
	_append_event("Exported attempt to %s" % replay_path)


func _on_load_oracle_pressed() -> void:
	var path: String = "res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json"
	var loaded: Dictionary = harness.load_oracle_plan_path(path)
	if bool(loaded.get("ok", false)):
		_result.text = "Loaded oracle one-building plan"
		_append_event("Oracle plan loaded")
	else:
		_result.text = "Oracle load failed: %s" % str(loaded.get("error", "?"))
	_refresh_gates_panel()
	_refresh_commands()


func _refresh_commands() -> void:
	_command_list.clear()
	var commands: Array = harness.get_available_commands()
	for cmd in commands:
		_command_list.add_item(str(cmd.get("label", cmd.get("type", "?"))))
	if not commands.is_empty():
		_command_list.select(0)
	var cmd_count: int = commands.size()
	_commands_header.text = "Legal commands for %s: %d" % [harness.active_player_id, cmd_count]
	var gaps: Dictionary = harness.compare_command_gaps()
	if gaps.get("only_in_sim", []).size() > 0:
		_commands_header.text += " | UI gap: sim-only types present"


func _on_snapshot_refreshed(snapshot: Dictionary) -> void:
	_refresh_map_layers()
	_update_attack_target_highlight()
	_refresh_commands()
	_update_debug(snapshot)
	_update_summary()
	_update_checklist()
	_refresh_gates_panel()
