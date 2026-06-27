class_name ManualPlayabilityBotExport
extends RefCounted

const MANUAL_DIR := "res://../docs/audit/bot_runs/manual_playability/"


static func write_summary(filename: String, summary: Dictionary) -> String:
	var root: String = ProjectSettings.globalize_path(MANUAL_DIR)
	DirAccess.make_dir_recursive_absolute(root)
	var path: String = root.path_join(filename)
	var enriched: Dictionary = summary.duplicate(true)
	enriched["scenario_loads"] = true
	enriched["commands_submitted"] = enriched.get("decision_count", 0)
	enriched["complete_or_milestone"] = enriched.get("complete", false)
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f != null:
		f.store_string(JSON.stringify(enriched, "\t"))
	return path
