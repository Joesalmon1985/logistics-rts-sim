class_name GodStrategyMapViability
extends RefCounted

## Map legibility oracle — legal build sites per spawn before bot/eval runs.

const MVP_BUILDINGS: Array[String] = ["woodcutter", "sawmill", "barracks"]


static func find_legal_hex(state: GameState, player_id: String, building_type: String) -> Vector2i:
	return EconomyBvbSetup.find_chain_building_hex(state, player_id, building_type)


static func find_all_legal_hexes(
	state: GameState,
	player_id: String,
	building_type: String,
	limit: int = 8,
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
		var sites: Array = find_all_legal_hexes(state, player_id, btype, 1)
		if sites.is_empty():
			report["ok"] = false
			report["failures"].append({"building_type": btype, "reason": "NO_LEGAL_SITE"})
		else:
			report["sites"][btype] = sites
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
	if phase.begins_with("place_") or phase == "roads":
		if rejection != "":
			return "map"
	if phase in ["worker_policy", "wait_construction", "wait_production"]:
		return "logistics"
	if phase in ["queue_unit", "attack"]:
		return "combat"
	return "unknown"
