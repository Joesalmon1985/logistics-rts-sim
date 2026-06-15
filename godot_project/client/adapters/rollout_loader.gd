class_name RolloutLoader
extends RefCounted

const SCHEMA_VERSION: int = 1
const SNAPSHOT_DIR: String = "snapshots"
const COMMANDS_FILE: String = "commands.jsonl"
const EVENTS_FILE: String = "events.jsonl"
const METADATA_FILE: String = "metadata.json"
const METRICS_FILE: String = "metrics.json"


static func load_folder(folder_path: String) -> Dictionary:
	var base: String = folder_path
	if base.begins_with("res://"):
		base = ProjectSettings.globalize_path(base)
	if not DirAccess.dir_exists_absolute(base):
		return {"ok": false, "error": "folder not found: %s" % folder_path}

	for required in [METADATA_FILE, METRICS_FILE, COMMANDS_FILE, EVENTS_FILE]:
		if not FileAccess.file_exists(base.path_join(required)):
			return {"ok": false, "error": "missing required file: %s" % required}

	var metadata: Dictionary = _read_json_file(base.path_join(METADATA_FILE))
	if metadata.is_empty():
		return {"ok": false, "error": "invalid metadata.json"}
	if int(metadata.get("schema_version", 0)) != SCHEMA_VERSION:
		return {"ok": false, "error": "unsupported schema_version"}

	var metrics: Dictionary = _read_json_file(base.path_join(METRICS_FILE))
	var commands: Array = _read_jsonl(base.path_join(COMMANDS_FILE))
	var events: Array = _read_jsonl(base.path_join(EVENTS_FILE))
	var snapshot_paths: Array = _list_snapshots(base)
	var alignment: Dictionary = validate_alignment(commands.size(), snapshot_paths.size())
	if not bool(alignment.get("ok", false)):
		return {"ok": false, "error": str(alignment.get("error", "alignment failed"))}

	return {
		"ok": true,
		"folder_path": folder_path,
		"metadata": metadata,
		"metrics": metrics,
		"commands": commands,
		"events": events,
		"snapshot_paths": snapshot_paths,
		"command_count": commands.size(),
		"snapshot_count": snapshot_paths.size(),
	}


static func validate_alignment(command_count: int, snapshot_count: int) -> Dictionary:
	var expected: int = command_count + 1
	if snapshot_count != expected:
		return {
			"ok": false,
			"error": "snapshot/command misalignment: %d snapshots, %d commands (expected %d)" % [
				snapshot_count, command_count, expected,
			],
		}
	return {"ok": true}


static func load_snapshot_at(folder_path: String, step_index: int) -> Dictionary:
	var base: String = folder_path
	if base.begins_with("res://"):
		base = ProjectSettings.globalize_path(base)
	var path: String = base.path_join(SNAPSHOT_DIR).path_join("%06d.json" % step_index)
	return SnapshotLoader.parse_from_path(path)


static func _read_json_file(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {}
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
	return parsed if typeof(parsed) == TYPE_DICTIONARY else {}


static func _read_jsonl(path: String) -> Array:
	var rows: Array = []
	if not FileAccess.file_exists(path):
		return rows
	for line in FileAccess.get_file_as_string(path).split("\n", false):
		line = line.strip_edges()
		if line.is_empty():
			continue
		var parsed: Variant = JSON.parse_string(line)
		if typeof(parsed) == TYPE_DICTIONARY:
			rows.append(parsed)
	return rows


static func _list_snapshots(base: String) -> Array:
	var snap_dir: String = base.path_join(SNAPSHOT_DIR)
	if not DirAccess.dir_exists_absolute(snap_dir):
		return []
	var paths: Array = []
	var dir := DirAccess.open(snap_dir)
	if dir == null:
		return []
	dir.list_dir_begin()
	var name: String = dir.get_next()
	while name != "":
		if not dir.current_is_dir() and name.ends_with(".json"):
			paths.append(snap_dir.path_join(name))
		name = dir.get_next()
	dir.list_dir_end()
	paths.sort()
	return paths
