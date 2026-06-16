class_name EconomyBvbReview
extends RefCounted

## Demo state builders for RUN_02.8 bot-vs-bot visual review.


const CHECKPOINT_SUBDIR: String = "training_runs/visual_review/run_02_8_economy_bvb/checkpoints"


static func build_at_ticks(ticks: int) -> Dictionary:
	return load_checkpoint(ticks)


static func load_checkpoint(tick: int) -> Dictionary:
	var path: String = _checkpoint_path(tick)
	if not FileAccess.file_exists(path):
		push_error("Missing checkpoint: %s" % path)
		return {}
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		return {}
	var parsed: Variant = JSON.parse_string(f.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}
	return parsed


static func load_final_checkpoint() -> Dictionary:
	var manifest_path: String = _checkpoint_dir().path_join("checkpoint_manifest.json")
	if not FileAccess.file_exists(manifest_path):
		return load_checkpoint(3000)
	var f := FileAccess.open(manifest_path, FileAccess.READ)
	if f == null:
		return load_checkpoint(3000)
	var manifest: Variant = JSON.parse_string(f.get_as_text())
	if typeof(manifest) != TYPE_DICTIONARY:
		return load_checkpoint(3000)
	var checkpoints: Array = manifest.get("checkpoints", [])
	if checkpoints.is_empty():
		return load_checkpoint(3000)
	var last: Dictionary = checkpoints[-1]
	return load_checkpoint(int(last.get("tick", 3000)))


static func checkpoint_dir() -> String:
	return _checkpoint_dir()


static func _checkpoint_path(tick: int) -> String:
	return _checkpoint_dir().path_join("tick_%05d.json" % tick)


static func _checkpoint_dir() -> String:
	return ProjectSettings.globalize_path("res://").path_join("..").path_join(CHECKPOINT_SUBDIR)
