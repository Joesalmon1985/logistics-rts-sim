class_name SnapshotExporter
extends RefCounted

const SCHEMA_VERSION: int = 1


static func export_snapshot(state: GameState) -> Dictionary:
	var player_data: Array = []
	for player in players_to_array(state):
		player_data.append(player.to_dict())

	return {
		"schema_version": SCHEMA_VERSION,
		"variant": state.variant_id,
		"tick": state.tick,
		"seed": state.seed,
		"rng_state": state.rng_state,
		"map": state.map.to_dict() if state.map != null else {},
		"roads": [],
		"road_nodes": [],
		"buildings": [],
		"entities": state.entities.duplicate(true),
		"diagnostics": state.diagnostics.duplicate(true),
		"players": player_data,
	}


static func players_to_array(state: GameState) -> Array[PlayerState]:
	return state.players
