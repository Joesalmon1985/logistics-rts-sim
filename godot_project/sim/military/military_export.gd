class_name MilitaryExporter
extends RefCounted

## CSV/JSON/JSONL exports for the trial military game (MIL-DIAG / MIL-EXPORT).
## All exports are deterministic and integer-only, derived from snapshot/diagnostics.


static func export_snapshot_json(state: GameState, path: String) -> bool:
	return _write_text(path, JSON.stringify(SnapshotExporter.export_snapshot(state), "\t"))


static func export_event_log_jsonl(state: GameState, path: String) -> bool:
	var lines: PackedStringArray = []
	for event in state.event_log.events:
		lines.append(JSON.stringify(event.to_dict()))
	return _write_text(path, "\n".join(lines))


static func export_player_metrics_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("player_id,credits,power_supply,power_demand,powered,hub_count,defeated,unit_count,building_count,idle_units")
	var military: Dictionary = state.diagnostics.get("military", {})
	var players: Dictionary = military.get("players", {})
	for pid in players:
		var d: Dictionary = players[pid]
		rows.append(
			"%s,%d,%d,%d,%d,%d,%d,%d,%d,%d"
			% [
				str(pid),
				int(d.get("credits", 0)),
				int(d.get("power_supply", 0)),
				int(d.get("power_demand", 0)),
				int(d.get("powered", 0)),
				int(d.get("hub_count", 0)),
				int(d.get("defeated", 0)),
				int(d.get("unit_count", 0)),
				int(d.get("building_count", 0)),
				int(d.get("idle_units", 0)),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_unit_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("unit_id,player_id,unit_type,q,r,hp,max_hp,state,order")
	for unit in MilitaryState.all_units(state):
		rows.append(
			"%d,%s,%s,%d,%d,%d,%d,%s,%s"
			% [
				int(unit.get("id", 0)),
				str(unit.get("player_id", "")),
				str(unit.get("unit_type", "")),
				int(unit.get("q", 0)),
				int(unit.get("r", 0)),
				int(unit.get("hp", 0)),
				int(unit.get("max_hp", 0)),
				str(unit.get("state", "")),
				str(unit.get("order", {}).get("type", "")),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_building_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("building_id,player_id,building_type,anchor_q,anchor_r,hp,max_hp,state,powered")
	for building in MilitaryState.all_buildings(state):
		rows.append(
			"%d,%s,%s,%d,%d,%d,%d,%s,%d"
			% [
				int(building.get("id", 0)),
				str(building.get("player_id", "")),
				str(building.get("building_type", "")),
				int(building.get("anchor_q", 0)),
				int(building.get("anchor_r", 0)),
				int(building.get("hp", 0)),
				int(building.get("max_hp", 0)),
				str(building.get("state", "")),
				int(building.get("powered", 0)),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_metrics_csv(state: GameState, path: String) -> bool:
	var military: Dictionary = state.diagnostics.get("military", {})
	var rows: PackedStringArray = []
	rows.append("tick,active_projectiles,construction_sites,repair_jobs,victory_state,stall_ticks")
	rows.append(
		"%d,%d,%d,%d,%s,%d"
		% [
			int(state.tick),
			int(military.get("active_projectiles", 0)),
			int(military.get("construction_sites", 0)),
			int(military.get("repair_jobs", 0)),
			str(military.get("victory_state", "none")),
			int(MilitaryState.get_meta_value(state, "stall_ticks", 0)),
		]
	)
	return _write_text(path, "\n".join(rows))


static func export_bot_decision_csv(decisions: Array, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("tick,actor_id,legal_actions_count,chosen_command,reason,expected_effect")
	for row in decisions:
		rows.append(
			"%d,%s,%d,%s,%s,%s"
			% [
				int(row.get("tick", 0)),
				str(row.get("actor_id", "")),
				int(row.get("legal_actions_count", 0)),
				str(row.get("chosen_command", "")),
				str(row.get("reason", "")).replace(",", ";"),
				str(row.get("expected_effect", "")).replace(",", ";"),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_replay_json(replay_data: Dictionary, path: String) -> bool:
	return _write_text(path, JSON.stringify(replay_data, "\t"))


static func export_all(state: GameState, output_dir: String, bot_decisions: Array = []) -> Dictionary:
	var results := {}
	results["snapshot"] = export_snapshot_json(state, output_dir + "/military_snapshot.json")
	results["events"] = export_event_log_jsonl(state, output_dir + "/military_events.jsonl")
	results["player_metrics"] = export_player_metrics_csv(state, output_dir + "/military_player_metrics.csv")
	results["units"] = export_unit_status_csv(state, output_dir + "/military_unit_status.csv")
	results["buildings"] = export_building_status_csv(state, output_dir + "/military_building_status.csv")
	results["metrics"] = export_metrics_csv(state, output_dir + "/military_metrics.csv")
	if not bot_decisions.is_empty():
		results["bot_decision"] = export_bot_decision_csv(bot_decisions, output_dir + "/military_bot_decision.csv")
	return results


static func _write_text(path: String, text: String) -> bool:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("MilitaryExporter: cannot write %s" % path)
		return false
	file.store_string(text)
	return true
