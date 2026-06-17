class_name EconomyHumanProofContract
extends RefCounted

## Shared ECON_BOT_002 proof scenario contract for oracle, UI harness, and tests.

const MIN_BUILDINGS: int = 5
const MIN_ROADS: int = 10
const MIN_DELIVERIES: int = 10
const MAX_STALL_TICKS: int = 300


static func proof_scenario() -> Dictionary:
	return EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)


static func create_proof_state(runner: SimulationTickRunner = null) -> GameState:
	var sim_runner: SimulationTickRunner = runner if runner != null else SimulationTickRunner.new()
	var scenario: Dictionary = proof_scenario()
	var state: GameState = sim_runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	return state


static func gate_thresholds() -> Dictionary:
	return {
		"buildings": MIN_BUILDINGS,
		"roads": MIN_ROADS,
		"deliveries": MIN_DELIVERIES,
		"stall_ticks": MAX_STALL_TICKS,
	}


static func evaluate_player_gates(state: GameState, player_id: String) -> Dictionary:
	EconomyDiagnosticsSystem.tick(state)
	var scores: Dictionary = state.diagnostics.get("economy_scores", {})
	var stalls: Dictionary = state.diagnostics.get("player_stalls", {})
	var stall_ticks: int = int(stalls.get(player_id, {}).get("stall_ticks", 0))
	var buildings: int = DualBotRunner.count_player_buildings_built(state, player_id)
	var roads: int = DualBotRunner.count_player_roads_built(state, player_id)
	var deliveries: int = DualBotRunner.count_player_deliveries_built(state, player_id)
	var score: int = int(scores.get(player_id, 0))
	var thresholds: Dictionary = gate_thresholds()
	return {
		"player_id": player_id,
		"buildings": buildings,
		"roads": roads,
		"deliveries": deliveries,
		"stall_ticks": stall_ticks,
		"score": score,
		"buildings_pass": buildings >= int(thresholds.get("buildings", MIN_BUILDINGS)),
		"roads_pass": roads >= int(thresholds.get("roads", MIN_ROADS)),
		"deliveries_pass": deliveries >= int(thresholds.get("deliveries", MIN_DELIVERIES)),
		"stall_pass": stall_ticks <= int(thresholds.get("stall_ticks", MAX_STALL_TICKS)),
		"score_pass": score > 0,
		"pass": _player_passes(buildings, roads, deliveries, stall_ticks, score, thresholds),
	}


static func evaluate_match_gates(state: GameState) -> Dictionary:
	var players: Dictionary = {}
	var overall: bool = true
	for player_id in ["p1", "p2"]:
		var report: Dictionary = evaluate_player_gates(state, player_id)
		players[player_id] = report
		if not bool(report.get("pass", false)):
			overall = false
	return {
		"tick": state.tick,
		"max_ticks": int(proof_scenario().get("max_ticks", 800)),
		"players": players,
		"pass": overall,
	}


static func format_gate_line(state: GameState, player_id: String) -> String:
	var g: Dictionary = evaluate_player_gates(state, player_id)
	var thresholds: Dictionary = gate_thresholds()
	return (
		"%s b=%d/%d r=%d/%d d=%d/%d stall=%d/%d score=%d pass=%s"
		% [
			player_id,
			int(g.get("buildings", 0)), int(thresholds.get("buildings", MIN_BUILDINGS)),
			int(g.get("roads", 0)), int(thresholds.get("roads", MIN_ROADS)),
			int(g.get("deliveries", 0)), int(thresholds.get("deliveries", MIN_DELIVERIES)),
			int(g.get("stall_ticks", 0)), int(thresholds.get("stall_ticks", MAX_STALL_TICKS)),
			int(g.get("score", 0)),
			str(g.get("pass", false)),
		]
	)


static func compare_command_availability(state: GameState, player_id: String) -> Dictionary:
	var snapshot: Dictionary = SnapshotExporter.export_debug_snapshot(state, player_id)
	var client_cmds: Array = ClientCommandAvailability.list_from_snapshot(snapshot)
	var sim_cmds: Array = EconomyCommandAvailability.list_candidates(state, player_id)
	var client_types: Dictionary = _command_type_keys(client_cmds)
	var sim_types: Dictionary = _command_type_keys(sim_cmds)
	var only_sim: Array = []
	for key in sim_types.keys():
		if not client_types.has(key):
			only_sim.append(key)
	var only_client: Array = []
	for key in client_types.keys():
		if not sim_types.has(key):
			only_client.append(key)
	return {
		"client_count": client_cmds.size(),
		"sim_count": sim_cmds.size(),
		"only_in_sim": only_sim,
		"only_in_client": only_client,
	}


static func find_woodcutter_oracle_target(state: GameState, player_id: String = "p1") -> Vector2i:
	var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 11))
	for dq in range(-build_radius, build_radius + 1):
		for dr in range(-build_radius, build_radius + 1):
			if abs(dq + dr) > build_radius:
				continue
			var q: int = hq.x + dq
			var r: int = hq.y + dr
			if EconomyMapRules.can_place_building(state, "woodcutter", q, r, player_id).get("ok", false):
				return Vector2i(q, r)
	return Vector2i(-1, -1)


static func _player_passes(
	buildings: int,
	roads: int,
	deliveries: int,
	stall_ticks: int,
	score: int,
	thresholds: Dictionary,
) -> bool:
	return (
		buildings >= int(thresholds.get("buildings", MIN_BUILDINGS))
		and roads >= int(thresholds.get("roads", MIN_ROADS))
		and deliveries >= int(thresholds.get("deliveries", MIN_DELIVERIES))
		and stall_ticks <= int(thresholds.get("stall_ticks", MAX_STALL_TICKS))
		and score > 0
	)


static func _command_type_keys(commands: Array) -> Dictionary:
	var keys: Dictionary = {}
	for entry in commands:
		var cmd_type: String = str(entry.get("type", entry.get("command_type", "")))
		if cmd_type == "":
			continue
		keys[cmd_type] = int(keys.get(cmd_type, 0)) + 1
	return keys
