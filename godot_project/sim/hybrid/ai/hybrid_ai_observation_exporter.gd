class_name HybridAiObservationExporter
extends RefCounted

## Scalar/vector hybrid policy observation export (PRD A.7).


static func export_from_snapshot(snapshot: Dictionary, player_id: String) -> Dictionary:
	snapshot["player_id"] = player_id
	var vec: Array = []
	vec.append(_stockpile(snapshot, "planks"))
	vec.append(_stockpile(snapshot, "stone"))
	vec.append(_stockpile(snapshot, "tools"))
	vec.append(_stockpile(snapshot, "field_supplies"))
	vec.append(_count_building(snapshot, player_id, "woodcutter"))
	vec.append(_count_building(snapshot, player_id, "sawmill"))
	vec.append(_count_building(snapshot, player_id, "quarry"))
	vec.append(_count_building(snapshot, player_id, "barracks"))
	vec.append(_count_building(snapshot, player_id, "guard_post"))
	vec.append(_count_units(snapshot, player_id, "carrier"))
	vec.append(_count_units(snapshot, player_id, "rifle_squad"))
	vec.append(_visible_enemy_count(snapshot, player_id))
	vec.append(int(snapshot.get("tick", 0)))
	return {
		"schema_version": 1,
		"player_id": player_id,
		"tick": int(snapshot.get("tick", 0)),
		"vector": vec,
		"legal_action_mask": HybridAiActionSpace.legal_mask(null, player_id, snapshot),
	}


static func export_observation(state: GameState, player_id: String) -> Dictionary:
	var snapshot: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, player_id)
	snapshot["player_id"] = player_id
	var vec: Array = []
	vec.append(_stockpile(snapshot, "planks"))
	vec.append(_stockpile(snapshot, "stone"))
	vec.append(_stockpile(snapshot, "tools"))
	vec.append(_stockpile(snapshot, "field_supplies"))
	vec.append(_count_building(snapshot, player_id, "woodcutter"))
	vec.append(_count_building(snapshot, player_id, "sawmill"))
	vec.append(_count_building(snapshot, player_id, "quarry"))
	vec.append(_count_building(snapshot, player_id, "barracks"))
	vec.append(_count_building(snapshot, player_id, "guard_post"))
	vec.append(_count_units(snapshot, player_id, "carrier"))
	vec.append(_count_units(snapshot, player_id, "rifle_squad"))
	vec.append(_visible_enemy_count(snapshot, player_id))
	vec.append(int(snapshot.get("tick", 0)))
	return {
		"schema_version": 1,
		"player_id": player_id,
		"tick": int(snapshot.get("tick", 0)),
		"vector": vec,
		"legal_action_mask": HybridAiActionSpace.legal_mask(state, player_id, snapshot),
		"snapshot_summary": {
			"buildings": snapshot.get("buildings", []).size(),
			"units": snapshot.get("units", []).size(),
			"visible_enemies": _visible_enemy_count(snapshot, player_id),
		},
	}


static func _stockpile(snapshot: Dictionary, ware: String) -> int:
	var stock: Dictionary = snapshot.get("stockpile", {})
	return int(stock.get(ware, 0))


static func _count_building(snapshot: Dictionary, player_id: String, building_type: String) -> int:
	var count: int = 0
	for b in snapshot.get("buildings", []):
		if str(b.get("player_id", "")) == player_id and str(b.get("building_type", "")) == building_type:
			count += 1
	for b in snapshot.get("military_buildings", []):
		if str(b.get("player_id", "")) == player_id and str(b.get("building_type", "")) == building_type:
			count += 1
	return count


static func _count_units(snapshot: Dictionary, player_id: String, unit_type: String) -> int:
	var count: int = 0
	for u in snapshot.get("units", []):
		if str(u.get("player_id", "")) == player_id and str(u.get("unit_type", "")) == unit_type:
			count += 1
	return count


static func _visible_enemy_count(snapshot: Dictionary, player_id: String) -> int:
	var count: int = 0
	for u in snapshot.get("units", []):
		if str(u.get("player_id", "")) != player_id:
			count += 1
	for b in snapshot.get("military_buildings", []):
		if str(b.get("player_id", "")) != player_id:
			count += 1
	return count
