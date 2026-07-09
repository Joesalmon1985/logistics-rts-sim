class_name GodStrategyMapViability
extends RefCounted

## Map legibility oracle — legal build sites per spawn before bot/eval runs.

const MVP_BUILDINGS: Array[String] = ["woodcutter", "sawmill", "barracks"]
const CHAIN_LAYOUT_ORDER: Array[String] = ["woodcutter", "sawmill", "barracks"]


static func find_legal_hex(state: GameState, player_id: String, building_type: String) -> Vector2i:
	return find_legal_hex_with_road_path(state, player_id, building_type)


static func find_legal_hex_with_road_path(
	state: GameState,
	player_id: String,
	building_type: String,
) -> Vector2i:
	var slot: Vector2i = _spawn_build_slot(state, player_id, building_type)
	if slot.x >= 0:
		return slot
	if building_type == "barracks":
		var pad: Vector2i = GodStrategyTestHelper.find_barracks_pad_site(state, player_id)
		if pad.x >= 0:
			var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
			if GodStrategyRoadPlanner.has_delivery_path(state, player_id, hq.x, hq.y, pad.x, pad.y):
				return pad
	var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
	for site in find_all_legal_hexes(state, player_id, building_type, 32, false):
		var q: int = int(site.get("q", -1))
		var r: int = int(site.get("r", -1))
		if GodStrategyRoadPlanner.has_delivery_path(state, player_id, hq.x, hq.y, q, r):
			return Vector2i(q, r)
	return Vector2i(-1, -1)


static func _spawn_build_slot(state: GameState, player_id: String, building_type: String) -> Vector2i:
	var slots: Dictionary = EconomyState.get_meta_value(state, "god_strategy_build_slots_%s" % player_id, {})
	if typeof(slots) != TYPE_DICTIONARY or not slots.has(building_type):
		return Vector2i(-1, -1)
	var slot: Dictionary = slots[building_type]
	var q: int = int(slot.get("q", -1))
	var r: int = int(slot.get("r", -1))
	if q < 0 or r < 0:
		return Vector2i(-1, -1)
	if not EconomyMapRules.can_place_building(state, building_type, q, r, player_id).get("ok", false):
		return Vector2i(-1, -1)
	var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
	if not GodStrategyRoadPlanner.has_delivery_path(state, player_id, hq.x, hq.y, q, r):
		return Vector2i(-1, -1)
	return Vector2i(q, r)


static func find_all_legal_hexes(
	state: GameState,
	player_id: String,
	building_type: String,
	limit: int = 8,
	owned_only: bool = false,
) -> Array:
	var hq: Vector2i = EconomyState.get_hq_position(state, player_id)
	var build_radius: int = int(EconomyState.get_meta_value(state, "build_radius", 8))
	var found: Array = []
	var candidates: Array[HexCoord] = []
	for dq in range(-build_radius, build_radius + 1):
		for dr in range(-build_radius, build_radius + 1):
			if abs(dq + dr) > build_radius:
				continue
			var coord := HexCoord.new(hq.x + dq, hq.y + dr)
			if state.map.in_bounds(coord):
				candidates.append(coord)
	candidates.sort_custom(func(a: HexCoord, b: HexCoord) -> bool:
		var da: int = a.distance(HexCoord.new(hq.x, hq.y))
		var db: int = b.distance(HexCoord.new(hq.x, hq.y))
		if da != db:
			return da < db
		if a.q != b.q:
			return a.q < b.q
		return a.r < b.r
	)
	for coord in candidates:
		if owned_only:
			var tile: HexTile = state.map.get_tile(coord)
			if tile == null or str(tile.owner_player_id) != player_id:
				continue
		if EconomyMapRules.can_place_building(state, building_type, coord.q, coord.r, player_id).get("ok", false):
			found.append({"q": coord.q, "r": coord.r})
			if found.size() >= limit:
				break
	return found


static func validate_spawn(
	state: GameState,
	player_id: String,
	building_types: Array = MVP_BUILDINGS,
) -> Dictionary:
	var report: Dictionary = {"ok": true, "sites": {}, "failures": []}
	for building_type in building_types:
		var btype: String = str(building_type)
		var placement: Vector2i = find_legal_hex_with_road_path(state, player_id, btype)
		if placement.x < 0:
			report["ok"] = false
			report["failures"].append({"building_type": btype, "reason": "NO_LEGAL_SITE_OR_ROAD_PATH"})
		else:
			report["sites"][btype] = [{"q": placement.x, "r": placement.y}]
	return report


static func validate_all_spawns(state: GameState, scenario: Dictionary) -> Dictionary:
	var all_ok: bool = true
	var players: Dictionary = {}
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		if pid == "":
			continue
		var report: Dictionary = validate_spawn(state, pid)
		players[pid] = report
		if not bool(report.get("ok", false)):
			all_ok = false
	return {"ok": all_ok, "players": players}


static func validate_full_chain_layout(state: GameState, scenario: Dictionary) -> Dictionary:
	var all_ok: bool = true
	var players: Dictionary = {}
	for spawn in scenario.get("spawns", []):
		if typeof(spawn) != TYPE_DICTIONARY:
			continue
		var pid: String = str(spawn.get("player_id", ""))
		if pid == "":
			continue
		var probe: GameState = _layout_probe_for_scenario(scenario)
		var report: Dictionary = _validate_player_chain_layout(probe, pid)
		players[pid] = report
		if not bool(report.get("ok", false)):
			all_ok = false
	return {"ok": all_ok, "players": players, "scenario": str(scenario.get("objective_text", ""))}


static func _layout_probe_for_scenario(scenario: Dictionary) -> GameState:
	var runner := SimulationTickRunner.new()
	var probe: GameState = runner.create_game(scenario.duplicate(true))
	if bool(scenario.get("god_strategy_regression", false)):
		GodStrategyTestHelper.prep_domination_regression(probe, scenario)
	else:
		GodStrategyTestHelper.prep_all_spawns(probe, scenario)
	return probe


static func _validate_player_chain_layout(probe: GameState, player_id: String) -> Dictionary:
	var report: Dictionary = {
		"ok": true,
		"sites": {},
		"road_paths": {},
		"failures": [],
	}
	var hq: Vector2i = EconomyState.get_hq_position(probe, player_id)
	var placed: Array = []
	for building_type in CHAIN_LAYOUT_ORDER:
		var btype: String = str(building_type)
		var site: Vector2i = Vector2i(-1, -1)
		if btype == "barracks":
			var pad: Vector2i = GodStrategyTestHelper.find_barracks_pad_site(probe, player_id)
			if pad.x >= 0 and not _chain_footprint_conflicts(probe, player_id, btype, pad.x, pad.y, placed):
				if GodStrategyRoadPlanner.has_road_path(probe, player_id, hq.x, hq.y, pad.x, pad.y):
					site = pad
		if site.x < 0:
			for candidate in find_all_legal_hexes(probe, player_id, btype, 32, true):
				var q: int = int(candidate.get("q", -1))
				var r: int = int(candidate.get("r", -1))
				if _chain_footprint_conflicts(probe, player_id, btype, q, r, placed):
					continue
				if not GodStrategyRoadPlanner.has_road_path(probe, player_id, hq.x, hq.y, q, r):
					continue
				site = Vector2i(q, r)
				break
		if site.x >= 0:
			placed.append({"building_type": btype, "q": site.x, "r": site.y})
		if site.x < 0:
			report["ok"] = false
			report["failures"].append({"building_type": btype, "reason": "NO_CHAIN_LAYOUT"})
			continue
		report["sites"][btype] = {"q": site.x, "r": site.y}
		report["road_paths"][btype] = GodStrategyRoadPlanner.plan_road_segments(
			probe, player_id, hq.x, hq.y, site.x, site.y,
		)
	return report


static func _chain_footprint_conflicts(
	state: GameState,
	player_id: String,
	building_type: String,
	q: int,
	r: int,
	placed: Array,
) -> bool:
	var cfg: EconomyConfig = EconomyState.get_config(state)
	var bdef: Dictionary = cfg.get_building_def(building_type)
	var coords: Array[HexCoord] = EconomyMapRules.get_footprint_coords(q, r, bdef.get("footprint", [[0, 0]]))
	for entry in placed:
		var other_type: String = str(entry.get("building_type", ""))
		var other_def: Dictionary = cfg.get_building_def(other_type)
		var other_coords: Array[HexCoord] = EconomyMapRules.get_footprint_coords(
			int(entry.get("q", 0)), int(entry.get("r", 0)), other_def.get("footprint", [[0, 0]]),
		)
		for oc in other_coords:
			for c in coords:
				if oc.q == c.q and oc.r == c.r:
					return true
	var check: Dictionary = EconomyMapRules.can_place_building(state, building_type, q, r, player_id)
	return not check.get("ok", false)


static func export_layout_sweep(scenario_path: String) -> Dictionary:
	SimBootstrap.load_all()
	var runner := SimulationTickRunner.new()
	var scenario: Dictionary = ConfigLoader.load_scenario(scenario_path)
	var state: GameState = runner.create_game(scenario)
	GodStrategyTestHelper.prep_all_spawns(state, scenario)
	var viability: Dictionary = validate_all_spawns(state, scenario)
	var layout: Dictionary = validate_full_chain_layout(state, scenario)
	var resources: Dictionary = GodStrategyResourceAudit.can_afford_chain_start(scenario)
	return {
		"scenario_path": scenario_path,
		"viability": viability,
		"full_chain_layout": layout,
		"resource_audit": resources,
	}


static func format_failure(report: Dictionary) -> String:
	var lines: Array[String] = []
	for pid in report.get("players", {}).keys():
		var player_report: Dictionary = report["players"][pid]
		if bool(player_report.get("ok", false)):
			continue
		for failure in player_report.get("failures", []):
			lines.append("%s missing legal %s (%s)" % [
				pid,
				str(failure.get("building_type", "")),
				str(failure.get("reason", "")),
			])
	return ", ".join(lines)


static func classify_stall(telemetry: Dictionary) -> String:
	var phase: String = str(telemetry.get("phase", ""))
	var rejection: String = str(telemetry.get("last_rejection_reason", ""))
	if phase == "roads" and rejection == "NOT_OWNER":
		return "road_ownership"
	if phase.begins_with("place_") or phase == "roads":
		if rejection != "":
			return "map"
	if phase in ["worker_policy", "wait_construction", "wait_production"]:
		return "logistics"
	if phase.begins_with("place_") and rejection in ["CONS_BLOCKED", "INSUFFICIENT_WARES"]:
		return "production"
	if phase in ["queue_unit", "attack"]:
		return "combat"
	return "unknown"
