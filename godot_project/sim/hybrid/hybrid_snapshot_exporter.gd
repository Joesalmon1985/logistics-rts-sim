class_name HybridSnapshotExporter
extends RefCounted

const SCHEMA_VERSION: int = 1


static func export_snapshot(state: GameState) -> Dictionary:
	var econ: Dictionary = SnapshotExporter.export_economy_snapshot(state)
	var mil_layers: Dictionary = MilitarySnapshotExporter.export_snapshot(state)
	econ["variant"] = state.variant_id
	econ["military_buildings"] = mil_layers.get("buildings", [])
	econ["units"] = mil_layers.get("units", [])
	econ["production_queues"] = mil_layers.get("production_queues", [])
	econ["projectiles"] = mil_layers.get("projectiles", [])
	econ["repair_jobs"] = mil_layers.get("repair_jobs", [])
	econ["resource_fields"] = mil_layers.get("resource_fields", [])
	econ["military_players"] = mil_layers.get("military_players", [])
	econ["combat_log"] = mil_layers.get("combat_log", [])
	econ["vision"] = _merge_vision(state, mil_layers.get("vision", {}))
	econ["power"] = mil_layers.get("power", {})
	econ["tech"] = mil_layers.get("tech", {})
	econ["delegated_orders"] = HybridState.all_delegated_orders(state)
	econ["hybrid_diagnostics"] = state.diagnostics.get("hybrid", {})
	econ["god_strategy"] = GodStrategyTick.export_diagnostics(state)
	return econ


static func _merge_vision(state: GameState, military_vision: Dictionary) -> Dictionary:
	var merged: Dictionary = military_vision.duplicate(true)
	merged["economy_explored"] = _economy_explored_hexes(state)
	return merged


static func _economy_explored_hexes(state: GameState) -> Array:
	var result: Array = []
	if state.map == null:
		return result
	for q in range(state.map.width):
		for r in range(state.map.height):
			var tile: HexTile = state.map.get_tile(HexCoord.new(q, r))
			if tile != null and tile.deposit_revealed > 0:
				result.append({"q": q, "r": r})
	return result
