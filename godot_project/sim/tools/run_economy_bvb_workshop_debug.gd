extends SceneTree

## Time-sequenced workshop blocker diagnostics for ECON_BOT_002 (RUN_02.8F).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const OUTPUT_SUBDIR: String = "training_runs/diagnostics/run_02_8f_economy_bvb"
const UPSTREAM_TYPES: Array[String] = ["woodcutter", "storehouse", "forester", "sawmill"]
const ROAD_CAP: int = EconomyExpansionBot.MAX_TOTAL_ROAD_SEGMENTS

const VALID_PROFILES: Array[String] = [
	EconomyBvbSetup.PROFILE_PROOF,
	EconomyBvbSetup.PROFILE_VISUAL,
	EconomyBvbSetup.PROFILE_DIAG_FREE_EVERYTHING,
	EconomyBvbSetup.PROFILE_DIAG_PREBUILT_ROADS,
	EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_ABUNDANT,
	EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_EXACT,
]


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if _has_flag(args, "--list-profiles"):
		_print_profiles()
		print("EXIT: 0 (pass)")
		quit(0)
		return

	var profile: String = _parse_profile(args)
	if profile == "" or profile not in VALID_PROFILES:
		print("WORKSHOP DEBUG FAIL: invalid --profile= (valid profiles below)")
		_print_profiles()
		print("EXIT: 1 (fail)")
		quit(1)
		return

	var max_ticks: int = _parse_int_arg(args, "--max-ticks=", 800)
	var sample_every: int = maxi(1, _parse_int_arg(args, "--sample-every=", 50))
	var output_dir: String = _resolve_output_dir()

	var report: Dictionary = _run_profile(profile, max_ticks, sample_every)
	report["profile"] = profile
	report["max_ticks"] = max_ticks
	report["sample_every"] = sample_every

	var out_path: String = output_dir.path_join("workshop_%s.json" % profile)
	DirAccess.make_dir_recursive_absolute(output_dir)
	if not SimPaths.write_text_file(out_path, JSON.stringify(report, "\t")):
		print("WORKSHOP DEBUG FAIL: cannot write %s" % out_path)
		print("EXIT: 1 (fail)")
		quit(1)
		return

	for row in report.get("players", []):
		if typeof(row) != TYPE_DICTIONARY:
			continue
		print(
			"%s b=%d roads=%d cap_left=%d goal=%s ws_adj=%s ws_route=%s class=%s"
			% [
				str(row.get("player_id", "?")),
				int(row.get("buildings_completed", 0)),
				int(row.get("roads_used", 0)),
				int(row.get("road_cap_remaining", 0)),
				str(row.get("current_goal", "")),
				str(row.get("workshop_road_adjacent", false)),
				str(row.get("workshop_route_from_store_exists", false)),
				str(report.get("classification", "")),
			]
		)
	print("milestones=%s" % JSON.stringify(report.get("milestones", {})))
	print("classification=%s" % str(report.get("classification", "")))
	print("WORKSHOP DEBUG: %s" % out_path)
	print("EXIT: 0 (pass)")
	quit(0)


func _run_profile(profile: String, max_ticks: int, sample_every: int) -> Dictionary:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(profile)
	scenario["max_ticks"] = max_ticks
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.attach_scenario_config(state, scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)

	var dual := DualBotRunner.new(runner)
	dual.bot_p1 = EconomyExpansionBot.new("p1")
	dual.bot_p2 = EconomyExpansionBot.new("p2")
	dual.bot_p1.reset("p1")
	dual.bot_p2.reset("p2")

	var trackers: Dictionary = {
		"p1": _new_player_tracker(),
		"p2": _new_player_tracker(),
	}
	var samples: Array = []
	var accepted_workshop: Dictionary = {"p1": false, "p2": false}
	var recorder_cursor: int = 0

	for _i in range(max_ticks):
		EconomyDiagnosticsSystem.tick(state, "bot")
		var snapshot: Dictionary = dual._snapshot_for_bot(state, dual.bot_p1)
		dual._apply_bot_turn(state, snapshot, dual.bot_p1)
		snapshot = dual._snapshot_for_bot(state, dual.bot_p2)
		dual._apply_bot_turn(state, snapshot, dual.bot_p2)
		state = runner.tick(state)

		var entries: Array = dual.get_recorder().get_command_entries()
		while recorder_cursor < entries.size():
			var entry: Dictionary = entries[recorder_cursor]
			recorder_cursor += 1
			if not bool(entry.get("accepted", false)):
				continue
			var cmd: Dictionary = entry.get("command", {})
			if str(cmd.get("type", "")) != "PlaceBuildingPlanCommand":
				continue
			var payload: Dictionary = cmd.get("payload", cmd.get("params", {}))
			if str(payload.get("building_type", "")) != "workshop":
				continue
			var pid: String = str(payload.get("player_id", ""))
			if pid == "" or not accepted_workshop.has(pid) or bool(accepted_workshop[pid]):
				continue
			accepted_workshop[pid] = true
			_mark_milestone(trackers, pid, "workshop_command_accepted", int(entry.get("tick", state.tick)))

		for pid in ["p1", "p2"]:
			var bot: EconomyExpansionBot = dual.bot_p1 if pid == "p1" else dual.bot_p2
			_update_milestones(state, trackers, pid, bot, sample_every)

		if state.tick % sample_every == 0 or state.tick == max_ticks:
			samples.append(_sample_tick(state, dual, trackers, state.tick, sample_every))

	var players: Array = []
	for pid in ["p1", "p2"]:
		var bot: EconomyExpansionBot = dual.bot_p1 if pid == "p1" else dual.bot_p2
		players.append(_build_final_row(state, dual, pid, bot, trackers[pid]))
		_mark_milestone(trackers, pid, "final_tick", state.tick)

	return {
		"players": players,
		"milestones": trackers,
		"samples": samples,
		"classification": _classify_players(players, trackers, profile, max_ticks),
		"wall_ms": 0,
	}


func _new_player_tracker() -> Dictionary:
	return {
		"goal_selected_workshop": -1,
		"workshop_command_accepted": -1,
		"workshop_site_created": -1,
		"workshop_road_adjacent": -1,
		"first_delivery_each_ware": {},
		"workshop_blocked": -1,
		"workshop_completed": -1,
		"final_tick": -1,
		"road_cap_at_site_created": -1,
		"road_cap_at_blocked": -1,
	}


func _mark_milestone(trackers: Dictionary, player_id: String, key: String, tick: int) -> void:
	if not trackers.has(player_id):
		return
	var tracker: Dictionary = trackers[player_id]
	if int(tracker.get(key, -1)) >= 0:
		return
	tracker[key] = tick


func _update_milestones(
	state: GameState,
	trackers: Dictionary,
	player_id: String,
	bot: EconomyExpansionBot,
	_sample_every: int,
) -> void:
	var tracker: Dictionary = trackers[player_id]
	if bot.get_current_goal() == "workshop" and int(tracker.get("goal_selected_workshop", -1)) < 0:
		tracker["goal_selected_workshop"] = state.tick

	var site: Dictionary = _find_workshop_site(state, player_id)
	if not site.is_empty() and int(tracker.get("workshop_site_created", -1)) < 0:
		tracker["workshop_site_created"] = state.tick
		tracker["road_cap_at_site_created"] = ROAD_CAP - DualBotRunner.count_player_roads_built(state, player_id)

	if not site.is_empty() and _site_road_adjacent(state, site, player_id):
		if int(tracker.get("workshop_road_adjacent", -1)) < 0:
			tracker["workshop_road_adjacent"] = state.tick

	var site_state: String = str(site.get("state", ""))
	if site_state == EconomyConstants.CONS_BLOCKED and int(tracker.get("workshop_blocked", -1)) < 0:
		tracker["workshop_blocked"] = state.tick
		tracker["road_cap_at_blocked"] = ROAD_CAP - DualBotRunner.count_player_roads_built(state, player_id)

	if _has_completed_building(state, player_id, "workshop") and int(tracker.get("workshop_completed", -1)) < 0:
		tracker["workshop_completed"] = state.tick

	if not site.is_empty():
		var delivered: Dictionary = site.get("delivered", {})
		for ware_type in site.get("required", {}):
			if str(ware_type) == "builder_work":
				continue
			var have: int = int(delivered.get(ware_type, 0))
			if have <= 0:
				continue
			var ware_key: String = str(ware_type)
			if not tracker["first_delivery_each_ware"].has(ware_key):
				tracker["first_delivery_each_ware"][ware_key] = state.tick


func _sample_tick(
	state: GameState,
	dual: DualBotRunner,
	_trackers: Dictionary,
	tick: int,
	_sample_every: int,
) -> Dictionary:
	var row := {"tick": tick, "players": {}}
	for pid in ["p1", "p2"]:
		var bot: EconomyExpansionBot = dual.bot_p1 if pid == "p1" else dual.bot_p2
		row["players"][pid] = {
			"buildings_completed": DualBotRunner.count_player_buildings_built(state, pid),
			"roads_used": DualBotRunner.count_player_roads_built(state, pid),
			"road_cap_remaining": ROAD_CAP - DualBotRunner.count_player_roads_built(state, pid),
			"current_goal": bot.get_current_goal(),
			"workshop_site_exists": not _find_workshop_site(state, pid).is_empty(),
		}
	return row


func _build_final_row(
	state: GameState,
	dual: DualBotRunner,
	player_id: String,
	bot: EconomyExpansionBot,
	tracker: Dictionary,
) -> Dictionary:
	var roads_used: int = DualBotRunner.count_player_roads_built(state, player_id)
	var site: Dictionary = _find_workshop_site(state, player_id)
	var site_hex := Vector2i(-1, -1)
	if not site.is_empty():
		site_hex = Vector2i(int(site.get("anchor_q", -1)), int(site.get("anchor_r", -1)))

	var road_adjacent: bool = not site.is_empty() and _site_road_adjacent(state, site, player_id)
	var route_exists: bool = false
	var required: Dictionary = {}
	var delivered: Dictionary = {}
	var missing: Dictionary = {}
	if not site.is_empty():
		required = site.get("required", {}).duplicate(true)
		delivered = site.get("delivered", {}).duplicate(true)
		for ware_type in required:
			if str(ware_type) == "builder_work":
				continue
			var need: int = int(required[ware_type])
			var have: int = int(delivered.get(ware_type, 0))
			if have < need:
				missing[str(ware_type)] = need - have
		route_exists = _workshop_route_from_store_exists(state, player_id, site)

	var worker_counts: Dictionary = _worker_counts(state, player_id)
	var carrier_counts: Dictionary = _carrier_counts(state, player_id)

	return {
		"tick": state.tick,
		"player_id": player_id,
		"buildings_completed": DualBotRunner.count_player_buildings_built(state, player_id),
		"roads_used": roads_used,
		"road_cap_remaining": ROAD_CAP - roads_used,
		"current_goal": bot.get_current_goal(),
		"workshop_site_hex": {"q": site_hex.x, "r": site_hex.y},
		"workshop_road_adjacent": road_adjacent,
		"workshop_route_from_store_exists": route_exists,
		"workshop_required": required,
		"workshop_delivered": delivered,
		"workshop_missing": missing,
		"builder_available": worker_counts,
		"carrier_available": carrier_counts,
		"last_noop_reason": bot.last_noop_reason,
		"last_road_command": _last_road_command(dual.get_recorder(), player_id),
		"missing_ware_sources": _missing_ware_sources(state, player_id, site, missing),
		"upstream_jam_report": _upstream_jam_report(state, player_id),
		"milestones": tracker.duplicate(true),
	}


func _missing_ware_sources(
	state: GameState,
	player_id: String,
	site: Dictionary,
	missing: Dictionary,
) -> Array:
	var rows: Array = []
	if site.is_empty():
		return rows
	var site_id: int = int(site.get("id", 0))
	var dest_node: int = EconomyLogisticsSystem._dest_node_id_for_coords(
		state, site_id, "construction_site", player_id
	)
	for ware_type in missing:
		var ware: String = str(ware_type)
		var owned: int = _total_player_ware(state, player_id, ware)
		var source_info: Dictionary = _nearest_routed_store(state, player_id, ware, dest_node)
		var job_info: Dictionary = _delivery_job_for_site(state, site_id, ware)
		rows.append({
			"ware": ware,
			"total_owned": owned,
			"total_in_any_store": owned,
			"nearest_source_store_hex": source_info.get("hex", {"q": -1, "r": -1}),
			"route_source_to_workshop_exists": bool(source_info.get("route_exists", false)),
			"route_length": int(source_info.get("route_length", 0)),
			"delivery_request_exists": bool(job_info.get("exists", false)),
			"assigned_carrier_exists": bool(job_info.get("assigned_carrier", false)),
		})
	return rows


func _upstream_jam_report(state: GameState, player_id: String) -> Array:
	var rows: Array = []
	for building_type in UPSTREAM_TYPES:
		var building: Dictionary = _find_player_building(state, player_id, building_type)
		if building.is_empty():
			rows.append({
				"building_type": building_type,
				"status": "missing",
				"input_inventory": {},
				"output_inventory": {},
				"pause_reason": "",
				"worker_assigned": 0,
				"road_connected": false,
				"last_produced_tick": -1,
			})
			continue
		var store_id: int = int(building.get("store_id", 0))
		var store: Dictionary = EconomyState.get_ware_store(state, store_id) if store_id > 0 else {}
		rows.append({
			"building_type": building_type,
			"status": str(building.get("state", "")),
			"input_inventory": store.get("wares", {}).duplicate(true),
			"output_inventory": store.get("wares", {}).duplicate(true),
			"pause_reason": str(building.get("paused_reason", "")),
			"worker_assigned": building.get("assigned_workers", []).size(),
			"road_connected": _building_road_connected(state, building, player_id),
			"last_produced_tick": int(building.get("last_production_tick", -1)),
		})
	return rows


func _classify_players(players: Array, trackers: Dictionary, profile: String, max_ticks: int) -> String:
	var classes: Array[String] = []
	for row in players:
		if typeof(row) != TYPE_DICTIONARY:
			continue
		classes.append(_classify_player_row(row, trackers.get(str(row.get("player_id", "")), {}), max_ticks))
	if classes.is_empty():
		return "unknown"
	if classes[0] == classes[1]:
		return classes[0]
	return "%s/%s" % [classes[0], classes[1]]


func _classify_player_row(row: Dictionary, tracker: Dictionary, max_ticks: int) -> String:
	var goal: String = str(row.get("current_goal", ""))
	var site_hex: Dictionary = row.get("workshop_site_hex", {})
	var has_site: bool = int(site_hex.get("q", -1)) >= 0
	var accepted: bool = int(tracker.get("workshop_command_accepted", -1)) >= 0
	var completed: bool = int(tracker.get("workshop_completed", -1)) >= 0
	var missing: Dictionary = row.get("workshop_missing", {})
	var route_exists: bool = bool(row.get("workshop_route_from_store_exists", false))
	var road_adjacent: bool = bool(row.get("workshop_road_adjacent", false))
	var cap_left: int = int(row.get("road_cap_remaining", 0))
	var sources: Array = row.get("missing_ware_sources", [])

	if completed:
		return "workshop_completed"
	if goal == "workshop" and not has_site and not accepted:
		return "workshop_unplaced"
	if has_site and not route_exists and not road_adjacent and cap_left <= 3:
		return "workshop_unconnected"
	if has_site and (route_exists or road_adjacent) and not completed and state_tick_past_budget(tracker, max_ticks):
		if missing.is_empty() and int(row.get("workshop_delivered", {}).get("builder_work", 0)) < int(row.get("workshop_required", {}).get("builder_work", 1)):
			return "workshop_delivered_no_progress"
		for src in sources:
			if typeof(src) != TYPE_DICTIONARY:
				continue
			if bool(src.get("route_source_to_workshop_exists", false)) and int(src.get("total_owned", 0)) > 0 \
					and not bool(src.get("delivery_request_exists", false)):
				return "workshop_connected_undelivered"
		var owned_any: bool = false
		for src2 in sources:
			if int(src2.get("total_owned", 0)) > 0:
				owned_any = true
		if not owned_any or _upstream_jam_blocks(row):
			return "workshop_no_source_wares"
		if int(tracker.get("workshop_road_adjacent", max_ticks + 1)) > maxi(1, max_ticks - 150):
			return "workshop_late_connected"
		return "gate_tick_budget_mismatch"
	if has_site and not route_exists and not road_adjacent:
		return "workshop_unconnected"
	return "gate_tick_budget_mismatch"


func state_tick_past_budget(tracker: Dictionary, max_ticks: int) -> bool:
	return int(tracker.get("final_tick", max_ticks)) >= max_ticks or int(tracker.get("workshop_blocked", -1)) >= 0


func _upstream_jam_blocks(row: Dictionary) -> bool:
	for entry in row.get("upstream_jam_report", []):
		if typeof(entry) != TYPE_DICTIONARY:
			continue
		var reason: String = str(entry.get("pause_reason", ""))
		if reason in ["input_missing", "output_blocked", "missing_worker", "food_missing"]:
			return true
	return false


func _find_workshop_site(state: GameState, player_id: String) -> Dictionary:
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("player_id", "")) != player_id:
			continue
		if str(site.get("building_type", "")) == "workshop":
			return site
	return {}


func _has_completed_building(state: GameState, player_id: String, building_type: String) -> bool:
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) != building_type:
			continue
		if str(building.get("state", "")) in [EconomyConstants.BLD_ACTIVE, EconomyConstants.BLD_COMPLETED]:
			return true
	return false


func _find_player_building(state: GameState, player_id: String, building_type: String) -> Dictionary:
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		if str(building.get("building_type", "")) == building_type:
			return building
	return {}


func _workshop_route_from_store_exists(state: GameState, player_id: String, site: Dictionary) -> bool:
	var site_id: int = int(site.get("id", 0))
	var dest_node: int = EconomyLogisticsSystem._dest_node_id_for_coords(
		state, site_id, "construction_site", player_id
	)
	if dest_node <= 0:
		return false
	for ware_type in site.get("required", {}):
		if str(ware_type) == "builder_work":
			continue
		var source_store: int = EconomyLogisticsSystem._find_source_store_for_ware(
			state, player_id, str(ware_type), dest_node
		)
		if source_store > 0:
			return true
	return false


func _total_player_ware(state: GameState, player_id: String, ware_type: String) -> int:
	var total: int = 0
	var hq_store: int = EconomyState.get_hq_store_id(state, player_id)
	if hq_store > 0:
		total += int(EconomyState.get_ware_store(state, hq_store).get("wares", {}).get(ware_type, 0))
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0 or store_id == hq_store:
			continue
		total += int(EconomyState.get_ware_store(state, store_id).get("wares", {}).get(ware_type, 0))
	return total


func _nearest_routed_store(
	state: GameState,
	player_id: String,
	ware_type: String,
	dest_node: int,
) -> Dictionary:
	var best: Dictionary = {"route_exists": false, "route_length": 0, "hex": {"q": -1, "r": -1}}
	if dest_node <= 0:
		return best
	var candidates: Array = []
	var hq_store: int = EconomyState.get_hq_store_id(state, player_id)
	if hq_store > 0:
		candidates.append(hq_store)
	for building in EconomyState.all_buildings(state):
		if str(building.get("player_id", "")) != player_id:
			continue
		var store_id: int = int(building.get("store_id", 0))
		if store_id <= 0 or store_id == hq_store:
			continue
		candidates.append(store_id)

	var best_len: int = 999999
	for store_id in candidates:
		var store: Dictionary = EconomyState.get_ware_store(state, store_id)
		if not EconomyState.inventory_has(store, ware_type, 1):
			continue
		var source_node: int = EconomyLogisticsSystem._store_node_id(state, store_id, dest_node)
		if source_node <= 0:
			continue
		var route: Array = EconomyPathfinder.find_route(state, source_node, dest_node, player_id)
		if route.is_empty():
			continue
		if route.size() < best_len:
			best_len = route.size()
			best = {
				"route_exists": true,
				"route_length": route.size(),
				"hex": _store_hex(state, store_id),
			}
	return best


func _store_hex(state: GameState, store_id: int) -> Dictionary:
	for building in EconomyState.all_buildings(state):
		if int(building.get("store_id", 0)) == store_id:
			return {
				"q": int(building.get("anchor_q", building.get("q", -1))),
				"r": int(building.get("anchor_r", building.get("r", -1))),
			}
	return {"q": -1, "r": -1}


func _delivery_job_for_site(state: GameState, site_id: int, ware_type: String) -> Dictionary:
	for job in EconomyState.all_delivery_jobs(state):
		if int(job.get("dest_entity_id", 0)) != site_id:
			continue
		if str(job.get("ware_type", "")) != ware_type:
			continue
		var status: String = str(job.get("status", ""))
		if status in [EconomyConstants.JOB_CANCELLED, EconomyConstants.JOB_FAILED, EconomyConstants.JOB_DELIVERED]:
			continue
		var carrier_id: int = int(job.get("assigned_carrier_id", 0))
		return {"exists": true, "assigned_carrier": carrier_id > 0}
	return {"exists": false, "assigned_carrier": false}


func _building_road_connected(state: GameState, building: Dictionary, player_id: String) -> bool:
	var node_id: int = int(building.get("road_node_id", 0))
	if node_id <= 0:
		return false
	var hq: Dictionary = EconomyState.get_building(state, EconomyState.get_hq_building_id(state, player_id))
	var hq_node: int = int(hq.get("road_node_id", 0))
	if hq_node <= 0:
		return false
	return not EconomyPathfinder.find_route(state, hq_node, node_id, player_id).is_empty()


func _worker_counts(state: GameState, player_id: String) -> Dictionary:
	var idle: int = 0
	var total: int = 0
	for worker in EconomyState.all_workers(state):
		if str(worker.get("player_id", "")) != player_id:
			continue
		if str(worker.get("worker_type", "")) != "builder":
			continue
		total += 1
		if str(worker.get("state", "")) == EconomyConstants.WORKER_IDLE:
			idle += 1
	return {"idle": idle, "total": total}


func _carrier_counts(state: GameState, player_id: String) -> Dictionary:
	var idle: int = 0
	var total: int = 0
	for carrier in EconomyState.all_carriers(state):
		if str(carrier.get("player_id", "")) != player_id:
			continue
		total += 1
		if str(carrier.get("state", "")) == EconomyConstants.CARRIER_IDLE:
			idle += 1
	return {"idle": idle, "total": total}


func _last_road_command(recorder: ReplayRecorder, player_id: String) -> Dictionary:
	var last: Dictionary = {}
	for entry in recorder.get_command_entries():
		if not bool(entry.get("accepted", false)):
			continue
		var cmd: Dictionary = entry.get("command", {})
		if str(cmd.get("type", "")) != "PlaceRoadCommand":
			continue
		var payload: Dictionary = cmd.get("payload", cmd.get("params", {}))
		if str(payload.get("player_id", "")) != player_id:
			continue
		last = {
			"tick": int(entry.get("tick", 0)),
			"from_q": int(payload.get("from_q", -1)),
			"from_r": int(payload.get("from_r", -1)),
			"to_q": int(payload.get("to_q", -1)),
			"to_r": int(payload.get("to_r", -1)),
		}
	return last


func _print_profiles() -> void:
	for profile in VALID_PROFILES:
		print(profile)


func _parse_profile(args: PackedStringArray) -> String:
	for arg in args:
		if arg.begins_with("--profile="):
			return arg.substr("--profile=".length())
	return ""


func _parse_int_arg(args: PackedStringArray, prefix: String, default_value: int) -> int:
	for arg in args:
		if arg.begins_with(prefix):
			return int(arg.substr(prefix.length()))
	return default_value


func _has_flag(args: PackedStringArray, flag: String) -> bool:
	return flag in args


func _resolve_output_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("..").path_join(OUTPUT_SUBDIR)


func _site_road_adjacent(state: GameState, site: Dictionary, player_id: String) -> bool:
	var q: int = int(site.get("anchor_q", site.get("q", -1)))
	var r: int = int(site.get("anchor_r", site.get("r", -1)))
	if q < 0:
		return false
	var hex := Vector2i(q, r)
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) != player_id:
			continue
		var a := Vector2i(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99))))
		var b := Vector2i(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99))))
		if a == hex or b == hex:
			return true
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) != player_id:
			continue
		if int(node.get("q", -99)) == q and int(node.get("r", -99)) == r:
			var segment_ids: Array = node.get("segment_ids", [])
			if segment_ids.size() > 0:
				return true
	return false


func _hex_has_player_road(state: GameState, coord: HexCoord, player_id: String) -> bool:
	for road in EconomyState.all_roads(state):
		if str(road.get("player_id", "")) != player_id:
			continue
		var a := HexCoord.new(int(road.get("hex_a_q", road.get("from_q", -99))), int(road.get("hex_a_r", road.get("from_r", -99))))
		var b := HexCoord.new(int(road.get("hex_b_q", road.get("to_q", -99))), int(road.get("hex_b_r", road.get("to_r", -99))))
		if coord.distance(a) == 0 or coord.distance(b) == 0:
			return true
	for node in EconomyState.all_road_nodes(state):
		if str(node.get("player_id", "")) != player_id:
			continue
		if coord.distance(HexCoord.new(int(node.get("q", 0)), int(node.get("r", 0)))) == 0:
			return true
	return false
