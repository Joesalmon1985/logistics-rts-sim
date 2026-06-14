class_name EconomyExporter
extends RefCounted

## CSV/JSON/JSONL exports for economy diagnostics (ECON-DIAG-004 / ECON-EXPORT).


static func export_snapshot_json(state: GameState, path: String) -> bool:
	var snapshot: Dictionary = SnapshotExporter.export_snapshot(state)
	return _write_text(path, JSON.stringify(snapshot, "\t"))


static func export_event_log_jsonl(state: GameState, path: String) -> bool:
	var lines: PackedStringArray = []
	for event in state.event_log.events:
		lines.append(JSON.stringify(event.to_dict()))
	return _write_text(path, "\n".join(lines))


static func export_metrics_csv(state: GameState, path: String) -> bool:
	var diag: Dictionary = state.diagnostics
	var rows: PackedStringArray = []
	rows.append("tick,progress_score,stall_ticks,stall_reason")
	var stall: Dictionary = diag.get("stall", {})
	rows.append(
		"%d,%d,%d,%s"
		% [
			int(diag.get("tick", state.tick)),
			int(diag.get("progress_score", 0)),
			int(stall.get("stall_ticks", 0)),
			str(stall.get("primary_reason", "")),
		]
	)
	return _write_text(path, "\n".join(rows))


static func export_production_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("building_id,building_type,state,paused_reason,workers")
	for record in state.diagnostics.get("production", []):
		rows.append(
			"%d,%s,%s,%s,%d"
			% [
				int(record.get("building_id", 0)),
				str(record.get("building_type", "")),
				str(record.get("state", "")),
				str(record.get("paused_reason", "")),
				int(record.get("workers", 0)),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_construction_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("site_id,building_type,state,missing_wares")
	var cons: Dictionary = state.diagnostics.get("construction", {})
	for site in cons.get("sites", []):
		rows.append(
			"%d,%s,%s,%s"
			% [
				int(site.get("site_id", 0)),
				str(site.get("building_type", "")),
				str(site.get("state", "")),
				JSON.stringify(site.get("missing_wares", {})),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_delivery_jobs_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("job_id,status,ware_type,block_reason")
	var delivery: Dictionary = state.diagnostics.get("delivery", {})
	for job in delivery.get("jobs", []):
		rows.append(
			"%d,%s,%s,%s"
			% [
				int(job.get("job_id", 0)),
				str(job.get("status", "")),
				str(job.get("ware_type", "")),
				str(job.get("block_reason", "")),
			]
		)
	return _write_text(path, "\n".join(rows))


static func export_resource_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("category,key,value")
	var resources: Dictionary = state.diagnostics.get("resources", {})
	for category in resources:
		var data: Variant = resources[category]
		if typeof(data) == TYPE_DICTIONARY:
			for key in data:
				rows.append("%s,%s,%s" % [str(category), str(key), str(data[key])])
	return _write_text(path, "\n".join(rows))


static func export_worker_carrier_status_csv(state: GameState, path: String) -> bool:
	var rows: PackedStringArray = []
	rows.append("kind,total,busy_or_assigned,idle")
	var util: Dictionary = state.diagnostics.get("utilisation", {})
	var carrier: Dictionary = util.get("carrier", {})
	var worker: Dictionary = util.get("worker", {})
	rows.append(
		"carrier,%d,%d,%d"
		% [int(carrier.get("total", 0)), int(carrier.get("busy", 0)), int(carrier.get("idle", 0))]
	)
	rows.append(
		"worker,%d,%d,%d"
		% [int(worker.get("total", 0)), int(worker.get("assigned", 0)), int(worker.get("idle", 0))]
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


static func export_all(state: GameState, output_dir: String, bot_decisions: Array = []) -> Dictionary:
	var results := {}
	results["snapshot"] = export_snapshot_json(state, output_dir + "/snapshot.json")
	results["events"] = export_event_log_jsonl(state, output_dir + "/events.jsonl")
	results["metrics"] = export_metrics_csv(state, output_dir + "/metrics.csv")
	results["production"] = export_production_status_csv(state, output_dir + "/production_status.csv")
	results["construction"] = export_construction_status_csv(state, output_dir + "/construction_status.csv")
	results["delivery"] = export_delivery_jobs_csv(state, output_dir + "/delivery_jobs.csv")
	results["resources"] = export_resource_status_csv(state, output_dir + "/resource_status.csv")
	results["worker_carrier"] = export_worker_carrier_status_csv(state, output_dir + "/worker_carrier_status.csv")
	if not bot_decisions.is_empty():
		results["bot_decision"] = export_bot_decision_csv(bot_decisions, output_dir + "/bot_decision.csv")
	return results


static func _write_text(path: String, text: String) -> bool:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("EconomyExporter: cannot write %s" % path)
		return false
	file.store_string(text)
	return true
