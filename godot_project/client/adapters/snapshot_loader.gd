class_name SnapshotLoader
extends RefCounted

const SUPPORTED_SCHEMA: int = 1


static func parse(snapshot: Dictionary) -> Dictionary:
	if snapshot.is_empty():
		return {"ok": false, "error": "Empty snapshot"}
	var schema: int = int(snapshot.get("schema_version", 0))
	if schema != SUPPORTED_SCHEMA:
		return {"ok": false, "error": "Unsupported schema_version: %d" % schema}
	if not snapshot.has("variant"):
		return {"ok": false, "error": "Missing variant"}
	return {"ok": true, "snapshot": snapshot.duplicate(true)}


static func parse_from_path(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {"ok": false, "error": "File not found: %s" % path}
	var text: String = FileAccess.get_file_as_string(path)
	var parsed: Variant = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return {"ok": false, "error": "Invalid JSON in %s" % path}
	return parse(parsed)
