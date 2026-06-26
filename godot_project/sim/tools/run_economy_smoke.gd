extends SceneTree

## Headless economy smoke runner — ECON-SMOKE-001 through ECON-SMOKE-006.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ExportScript := preload("res://sim/tools/economy_export.gd")

const DEFAULT_SEED: int = 42
const BOT_MAX_TICKS: int = 20000
const REPLAY_TICKS: int = 500
const SMOKE_006_SEED: int = 607
const SMOKE_006_TICKS: int = 500
const SMOKE_006_EXPECTED_HASH: String = "sha256:27d91679d8c0ff82281f5ededde2e2d4db7cc8ccadc74217d2b8c011ddf715ce"
const SMOKE_007_SEED: int = 28001
const SMOKE_007_EXPECTED_HASH: String = ""


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var smoke_id: String = "all"
	var seed_value: int = DEFAULT_SEED
	var scenario_path: String = "scenarios/economy/tiny_success_map.json"
	var check_hash: bool = true
	var expected_hash_override: String = ""
	var export_dir: String = ""

	for arg in args:
		if arg.begins_with("--smoke="):
			smoke_id = arg.substr("--smoke=".length())
		elif arg.begins_with("--seed="):
			seed_value = int(arg.substr("--seed=".length()))
		elif arg.begins_with("--scenario="):
			scenario_path = arg.substr("--scenario=".length())
		elif arg.begins_with("--expected-hash="):
			expected_hash_override = arg.substr("--expected-hash=".length())
			check_hash = true
		elif arg == "--no-hash-check":
			check_hash = false
		elif arg.begins_with("--export-dir="):
			export_dir = arg.substr("--export-dir=".length())

	var valid_smokes: Array[String] = ["all", "001", "002", "003", "004", "005", "006", "007"]
	if smoke_id not in valid_smokes:
		print("ECON SMOKE FAIL: unknown smoke id '%s'" % smoke_id)
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
		return

	var failed: bool = false
	if smoke_id == "all" or smoke_id == "001":
		failed = failed or not _run_smoke_001()
	if smoke_id == "all" or smoke_id == "002":
		failed = failed or not _run_smoke_002(seed_value)
	if smoke_id == "all" or smoke_id == "003":
		failed = failed or not _run_smoke_003(seed_value)
	if smoke_id == "all" or smoke_id == "004":
		failed = failed or not _run_smoke_004(seed_value, check_hash, expected_hash_override)
	if smoke_id == "all" or smoke_id == "005":
		failed = failed or not _run_smoke_005(scenario_path, seed_value, export_dir)
	if smoke_id == "all" or smoke_id == "006":
		failed = failed or not _run_smoke_006(seed_value, check_hash, expected_hash_override)
	if smoke_id == "all" or smoke_id == "007":
		failed = failed or not _run_smoke_007(seed_value, check_hash, expected_hash_override)

	if failed:
		print("ECON SMOKE FAIL")
		print("EXIT: 1 (fail)")
		call_deferred("quit", 1)
	else:
		print("ECON SMOKE PASS")
		print("EXIT: 0 (pass)")
		call_deferred("quit", 0)


func _run_smoke_001() -> bool:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": 7,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 9, 9)
	var validator := CommandValidator.new(CommandRegistry.create_default())
	for q in range(2, 6):
		var cmd := PlaceRoadCommand.new("p1", 0, {"from_q": q, "from_r": 2, "to_q": q + 1, "to_r": 2})
		validator.validate_and_apply(state, cmd)
	state = runner.run_ticks(state, 20)
	var carriers_moved: bool = false
	for carrier in EconomyState.all_carriers(state):
		if int(carrier.get("edge_progress", 0)) > 0 or str(carrier.get("state", "")) != EconomyConstants.CARRIER_IDLE:
			carriers_moved = true
			break
	for job in EconomyState.all_delivery_jobs(state):
		if str(job.get("status", "")) == EconomyConstants.JOB_IN_TRANSIT:
			carriers_moved = true
			break
	if EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).is_empty():
		print("ECON-SMOKE-001 FAIL: no roads placed")
		return false
	print("ECON-SMOKE-001 PASS: road network with carriers path")
	return true


func _run_smoke_002(seed_value: int) -> bool:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": seed_value,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 1, "r": 1},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 9, 9)
	state.map.set_tile(HexCoord.new(5, 2), _forest_tile())
	var woodcutter_id: int = EconomyInit._spawn_completed_building(
		state, "woodcutter", 5, 2, "p1", EconomyState.get_config(state),
	)
	_connect_roads(state, 1, 1, 5, 2)
	var sawmill_id: int = EconomyInit._spawn_completed_building(
		state, "sawmill", 3, 3, "p1", EconomyState.get_config(state),
	)
	_connect_roads(state, 1, 1, 3, 3)
	state = runner.run_ticks(state, 80)
	var hq_id: int = int(EconomyState.get_meta_value(state, "hq_building_id", 0))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	var store: Dictionary = EconomyState.get_ware_store(state, int(hq.get("store_id", 0)))
	var planks: int = int(store.get("wares", {}).get("planks", 0))
	for b in EconomyState.all_buildings(state):
		if str(b.get("building_type", "")) == "sawmill":
			var s: Dictionary = EconomyState.get_ware_store(state, int(b.get("store_id", 0)))
			planks += int(s.get("wares", {}).get("planks", 0))
	if planks < 1:
		print("ECON-SMOKE-002 FAIL: no planks in storage")
		return false
	print("ECON-SMOKE-002 PASS: timber to planks in storage")
	return true


func _run_smoke_003(seed_value: int) -> bool:
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": seed_value,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 9, 9)
	_connect_roads(state, 2, 2, 5, 3)
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": 5,
		"r": 3,
		"player_id": "p1",
	})
	validator.validate_and_apply(state, cmd)
	state = runner.run_ticks(state, 500)
	var completed: bool = false
	for event in state.event_log.events:
		if event.type == "construction_completed":
			completed = true
			break
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == "storehouse":
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				completed = true
	if not completed:
		print("ECON-SMOKE-003 FAIL: construction not completed")
		return false
	print("ECON-SMOKE-003 PASS: full building construction completed")
	return true


func _run_smoke_004(seed_value: int, check_hash: bool, expected_override: String) -> bool:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": seed_value,
		"map_width": 8,
		"map_height": 8,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
	}
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	state = runner.run_ticks(state, REPLAY_TICKS)
	var hash_a: String = ReplayHash.compute_state_hash(state)
	state = runner.create_game(scenario)
	state = runner.run_ticks(state, REPLAY_TICKS)
	var hash_b: String = ReplayHash.compute_state_hash(state)
	if hash_a != hash_b:
		print("ECON-SMOKE-004 FAIL: replay hash mismatch %s vs %s" % [hash_a, hash_b])
		return false
	print("Replay hash: %s" % hash_a)
	var expected: String = expected_override
	if expected != "" and check_hash:
		if not ReplayHash.matches(expected, hash_a):
			print("ECON-SMOKE-004 FAIL: expected hash mismatch")
			return false
	print("ECON-SMOKE-004 PASS: 500-tick replay hash match")
	return true


func _run_smoke_005(scenario_path: String, seed_value: int, export_dir: String) -> bool:
	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	if scenario.is_empty():
		print("ECON-SMOKE-005 FAIL: could not load scenario")
		return false
	scenario["seed"] = seed_value
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	var bot := HeuristicEconomyBuilderBot.new()
	bot.reset("p1")

	for _i in range(BOT_MAX_TICKS):
		if bot.has_workshop_complete(state):
			break
		bot.tick_act(state, runner)
		state = runner.tick(state)

	if not bot.has_workshop_complete(state):
		print("ECON-SMOKE-005 FAIL: workshop not completed via logistics chain")
		for building in EconomyState.all_buildings(state):
			print("  building: %s state=%s" % [building.get("building_type"), building.get("state")])
		for site in EconomyState.all_construction_sites(state):
			print("  site: %s state=%s delivered=%s required=%s" % [
				site.get("building_type"), site.get("state"), site.get("delivered"), site.get("required"),
			])
		return false

	var has_road: bool = not EconomyState.get_bucket(state, EconomyConstants.BUCKET_ROADS).is_empty()
	var has_delivery: bool = false
	for event in state.event_log.events:
		if event.type in ["ware_delivered", "delivery_job_created", "ware_moved"]:
			has_delivery = true
			break
	if not has_road or not has_delivery:
		print("ECON-SMOKE-005 FAIL: missing road/delivery evidence")
		return false

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if int(snapshot.get("schema_version", 0)) != 1:
		print("ECON-SMOKE-005 FAIL: invalid snapshot")
		return false

	if export_dir != "":
		DirAccess.make_dir_recursive_absolute(export_dir)
		ExportScript.export_all(state, export_dir, bot.get_decisions())

	print("ECON-SMOKE-005 PASS: heuristic bot built workshop via logistics")
	return true


func _run_smoke_006(seed_value: int, check_hash: bool, expected_override: String) -> bool:
	var scenario: Dictionary = {
		"variant": "economy_trial",
		"seed": SMOKE_006_SEED if seed_value == DEFAULT_SEED else seed_value,
		"map_width": 10,
		"map_height": 10,
		"players": ["p1"],
		"hq_position": {"q": 2, "r": 2},
		"build_radius": 12,
	}
	var runner := SimulationTickRunner.new()
	var validator := CommandValidator.new(CommandRegistry.create_default())
	var state: GameState = runner.create_game(scenario)
	_flatten_grass(state, 0, 0, 9, 9)
	_connect_roads(state, 2, 2, 6, 3)
	var cmd := PlaceBuildingPlanCommand.new("p1", 0, {
		"building_type": "storehouse",
		"q": 6,
		"r": 3,
		"player_id": "p1",
	})
	validator.validate_and_apply(state, cmd)

	var site_id: int = 0
	for site in EconomyState.all_construction_sites(state):
		if str(site.get("building_type", "")) == "storehouse":
			site_id = int(site.get("id", 0))
			break
	if site_id <= 0:
		print("ECON-SMOKE-006 FAIL: no storehouse construction site")
		return false

	var saw_job: bool = false
	var saw_carrier: bool = false
	for _pre in range(5):
		state = runner.tick(state)
		for job in EconomyState.all_delivery_jobs(state):
			saw_job = true
		for carrier in EconomyState.all_carriers(state):
			if str(carrier.get("state", "")) != EconomyConstants.CARRIER_IDLE:
				saw_carrier = true

	state = runner.run_ticks(state, SMOKE_006_TICKS)

	var completed: bool = false
	for building in EconomyState.all_buildings(state):
		if str(building.get("building_type", "")) == "storehouse":
			if str(building.get("state", "")) == EconomyConstants.BLD_ACTIVE:
				completed = true
	if not completed:
		print("ECON-SMOKE-006 FAIL: storehouse not completed")
		return false

	var delivered_event: bool = false
	var builder_progress: bool = false
	for event in state.event_log.events:
		if event.type == "ware_delivered":
			delivered_event = true
		if event.type == "construction_progressed":
			builder_progress = true
	if not delivered_event:
		print("ECON-SMOKE-006 FAIL: no ware_delivered event")
		return false
	if not builder_progress:
		print("ECON-SMOKE-006 FAIL: builder_work did not progress")
		return false

	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	if snapshot.get("roads", []).is_empty() or snapshot.get("road_nodes", []).is_empty():
		print("ECON-SMOKE-006 FAIL: snapshot missing roads/nodes")
		return false
	if snapshot.get("delivery_jobs", []).is_empty() and not saw_job:
		print("ECON-SMOKE-006 FAIL: no delivery job evidence")
		return false
	var hq_ports: bool = false
	for building in snapshot.get("buildings", []):
		if str(building.get("building_type", "")) == "headquarters":
			hq_ports = building.get("port_node_ids", []).size() > 0
	if not hq_ports:
		print("ECON-SMOKE-006 FAIL: HQ ports not exported")
		return false

	var hash_value: String = ReplayHash.compute_state_hash(state)
	print("ECON-SMOKE-006 replay hash: %s" % hash_value)
	if check_hash:
		var expected: String = expected_override if expected_override != "" else SMOKE_006_EXPECTED_HASH
		if not ReplayHash.matches(expected, hash_value):
			print("ECON-SMOKE-006 FAIL: replay hash mismatch %s vs %s" % [hash_value, expected])
			return false

	print("ECON-SMOKE-006 PASS: HQ to site logistics with carrier delivery and builder_work")
	return true


func _run_smoke_007(seed_value: int, check_hash: bool, expected_hash_override: String) -> bool:
	var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
	scenario["seed"] = seed_value if seed_value != DEFAULT_SEED else SMOKE_007_SEED
	var runner := SimulationTickRunner.new()
	var state: GameState = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
	var dual := DualBotRunner.new(runner)
	state = dual.run_match(state, scenario)
	var final_hash: String = ReplayHash.compute_state_hash(state)

	for player_id in ["p1", "p2"]:
		if DualBotRunner.count_player_buildings_built(state, player_id) < 5:
			print("ECON-SMOKE-007 FAIL: %s bot-built buildings below minimum" % player_id)
			return false
		if DualBotRunner.count_player_roads_built(state, player_id) < 10:
			print("ECON-SMOKE-007 FAIL: %s bot-built roads below minimum" % player_id)
			return false
		if DualBotRunner.count_player_deliveries_built(state, player_id) < 10:
			print("ECON-SMOKE-007 FAIL: %s bot-built deliveries below minimum" % player_id)
			return false

	if check_hash:
		var expected: String = expected_hash_override if expected_hash_override != "" else SMOKE_007_EXPECTED_HASH
		if expected != "" and final_hash != expected:
			print("ECON-SMOKE-007 FAIL: hash mismatch")
			print("  expected: %s" % expected)
			print("  actual:   %s" % final_hash)
			return false

	print("ECON-SMOKE-007 replay hash: %s" % final_hash)
	print("ECON-SMOKE-007 PASS: bot-vs-bot economy expansion")
	return true


func _flatten_grass(state: GameState, q0: int, r0: int, q1: int, r1: int) -> void:
	for q in range(q0, q1 + 1):
		for r in range(r0, r1 + 1):
			var coord := HexCoord.new(q, r)
			if state.map.in_bounds(coord):
				state.map.set_tile(coord, HexTile.new("grass", 0, 1, 1))


func _forest_tile() -> HexTile:
	var tile := HexTile.new("forest", 0, 1, 1)
	tile.tree_density = 80
	tile.deposit_type = "forest"
	tile.deposit_amount = 50
	tile.deposit_revealed = 1
	return tile


func _connect_roads(state: GameState, from_q: int, from_r: int, to_q: int, to_r: int) -> void:
	var current := HexCoord.new(from_q, from_r)
	var target := HexCoord.new(to_q, to_r)
	while current.q != target.q:
		var next := HexCoord.new(current.q + (1 if target.q > current.q else -1), current.r)
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
	while current.r != target.r:
		var next := HexCoord.new(current.q, current.r + (1 if target.r > current.r else -1))
		EconomyRoadSystem.place_road(state, current, next, "p1")
		current = next
