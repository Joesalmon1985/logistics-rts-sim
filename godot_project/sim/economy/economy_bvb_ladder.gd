class_name EconomyBvbLadder
extends RefCounted

## Minimal ablation ladder for ECON_BOT_002 bot diagnostics (RUN_02.8F).

const MIN_BUILDINGS: int = EconomyBvbSetup.LADDER_MIN_BUILDINGS


static func run_profile(profile: String, max_ticks: int = 800) -> Dictionary:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(profile)
	scenario["max_ticks"] = max_ticks
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.attach_scenario_config(state, scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)
	return _build_profile_row(profile, state, dual.bot_p1, dual.bot_p2)


static func run_ladder(max_ticks: int = 800, wall_budget_ms: int = 0) -> Dictionary:
	var wall_start: int = Time.get_ticks_msec()
	var rows: Array = []
	var first_failing_rung: Variant = null
	var classification: String = "all_passed"
	var previous_profile: String = ""
	var previous_passed: bool = true

	for profile in EconomyBvbSetup.ladder_profile_order():
		if wall_budget_ms > 0 and Time.get_ticks_msec() - wall_start >= wall_budget_ms:
			break
		var row: Dictionary = run_profile(profile, max_ticks)
		rows.append(row)
		if not bool(row.get("pass", false)) and first_failing_rung == null:
			first_failing_rung = profile
			classification = _classify_failure(profile, row, previous_profile, previous_passed)
		previous_passed = bool(row.get("pass", false))
		previous_profile = profile

	return {
		"profiles": rows,
		"first_failing_rung": first_failing_rung,
		"classification": classification,
		"wall_ms": Time.get_ticks_msec() - wall_start,
	}


static func write_ladder_summary(summary: Dictionary, output_path: String) -> bool:
	return SimPaths.write_text_file(output_path, JSON.stringify(summary, "\t"))


static func _build_profile_row(
	profile: String,
	state: GameState,
	bot_p1: EconomyExpansionBot,
	bot_p2: EconomyExpansionBot,
) -> Dictionary:
	var row_pass: bool = true
	var row := {
		"profile": profile,
		"p1_buildings": DualBotRunner.count_player_buildings_built(state, "p1"),
		"p2_buildings": DualBotRunner.count_player_buildings_built(state, "p2"),
		"p1_roads": DualBotRunner.count_player_roads_built(state, "p1"),
		"p2_roads": DualBotRunner.count_player_roads_built(state, "p2"),
		"p1_deliveries": DualBotRunner.count_player_deliveries_built(state, "p1"),
		"p2_deliveries": DualBotRunner.count_player_deliveries_built(state, "p2"),
		"p1_last_noop_reason": bot_p1.last_noop_reason if bot_p1 != null else "",
		"p2_last_noop_reason": bot_p2.last_noop_reason if bot_p2 != null else "",
		"tick": state.tick,
	}
	for player_id in ["p1", "p2"]:
		var built: int = DualBotRunner.count_player_buildings_built(state, player_id)
		if built < MIN_BUILDINGS:
			row_pass = false
	row["pass"] = row_pass
	return row


static func _classify_failure(
	profile: String,
	row: Dictionary,
	previous_profile: String,
	previous_passed: bool,
) -> String:
	if profile == EconomyBvbSetup.PROFILE_DIAG_FREE_EVERYTHING:
		return _classify_free_everything(row)
	if profile == EconomyBvbSetup.PROFILE_DIAG_PREBUILT_ROADS:
		if previous_profile == EconomyBvbSetup.PROFILE_DIAG_FREE_EVERYTHING and previous_passed:
			return "placement_heuristic_failure"
		return "road_planning_failure"
	if profile == EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_ABUNDANT:
		if previous_passed:
			return "delivery_logistics_failure"
		return "resource_sufficiency_failure"
	if profile == EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_EXACT:
		return "resource_sufficiency_failure"
	if profile == EconomyBvbSetup.PROFILE_PROOF:
		if previous_passed:
			return "gate_tick_budget_mismatch"
		return "resource_sufficiency_failure"
	return "placement_heuristic_failure"


static func _classify_free_everything(row: Dictionary) -> String:
	var probe_state: GameState = EconomyHumanProofContract.create_proof_state()
	for player_id in ["p1", "p2"]:
		var full_cmds: Array = EconomyCommandAvailability.list_candidates(probe_state, player_id)
		var bot_cmds: Array = EconomyCommandAvailability.list_bot_candidate_commands(probe_state, player_id)
		if not _commands_include_building(full_cmds, "woodcutter"):
			return "command_availability_failure"
		if not _commands_include_building(bot_cmds, "woodcutter"):
			return "bot_shortlist_failure"
	var p1_b: int = int(row.get("p1_buildings", 0))
	var p2_b: int = int(row.get("p2_buildings", 0))
	if p1_b <= 0 and p2_b <= 0:
		return "placement_heuristic_failure"
	return "bot_shortlist_failure"


static func _commands_include_building(commands: Array, building_type: String) -> bool:
	for entry in commands:
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var cmd_type: String = str(entry.get("type", entry.get("command_type", "")))
		if cmd_type != "PlaceBuildingPlanCommand":
			continue
		var params: Dictionary = entry.get("params", entry.get("payload", {}))
		if str(params.get("building_type", "")) == building_type:
			return true
	return false
