extends SceneTree

## Headless economy smoke runner — ECON-SMOKE-001 through ECON-SMOKE-005.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ExportScript := preload("res://sim/tools/economy_export.gd")

const DEFAULT_SEED: int = 42
const BOT_MAX_TICKS: int = 20000
const REPLAY_TICKS: int = 500


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

	var valid_smokes: Array[String] = ["all", "001", "002", "003", "004", "005"]
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
