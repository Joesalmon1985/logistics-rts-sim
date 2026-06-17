class_name TestRunnerSupport
extends RefCounted

## Selection, manifest integrity, and timing helpers for run_tests.gd (RUN_02.8c).

const TESTS_ROOT := "res://sim/tests/"
const EXCLUDE_REL_PATHS: Array[String] = [
	"test_case.gd",
	"integrity/test_run01_integrity_deliberate_fail.gd",
]

const CATEGORY_DIRS: Dictionary = {
	"kernel": ["kernel", "hex", "variants", "determinism"],
	"economy": ["economy"],
	"military": ["military"],
	"bot": ["bot"],
	"viewer": ["viewer"],
	"training": ["training"],
	"replay": ["replay"],
}


static func parse_config(args: PackedStringArray) -> Dictionary:
	var cfg := {
		"list_only": false,
		"manifest_check_only": false,
		"timing_json_path": OS.get_environment("TEST_TIMING_JSON"),
		"filter": OS.get_environment("TEST_FILTER"),
		"filters_csv": OS.get_environment("TEST_FILTERS"),
		"category": OS.get_environment("TEST_CATEGORY"),
		"test_list_file": "",
		"exact_paths_json": "",
		"filter_was_supplied": false,
	}

	for arg in args:
		if arg == "--list":
			cfg["list_only"] = true
		elif arg == "--manifest-check":
			cfg["manifest_check_only"] = true
		elif arg.begins_with("--timing-json="):
			cfg["timing_json_path"] = arg.substr("--timing-json=".length())
		elif arg.begins_with("--filter="):
			cfg["filter"] = arg.substr("--filter=".length())
		elif arg.begins_with("--filters="):
			cfg["filters_csv"] = arg.substr("--filters=".length())
		elif arg.begins_with("--category="):
			cfg["category"] = arg.substr("--category=".length())
		elif arg.begins_with("--test-list-file="):
			cfg["test_list_file"] = arg.substr("--test-list-file=".length())
		elif arg.begins_with("--exact-paths-json="):
			cfg["exact_paths_json"] = arg.substr("--exact-paths-json=".length())

	cfg["filter_was_supplied"] = (
		str(cfg["filter"]) != ""
		or str(cfg["filters_csv"]) != ""
		or str(cfg["category"]) != ""
		or str(cfg["test_list_file"]) != ""
		or str(cfg["exact_paths_json"]) != ""
	)
	return cfg


static func validate_manifest(manifest: Array[String]) -> Dictionary:
	var discovered: Array[String] = _discover_test_scripts()
	var manifest_set: Dictionary = {}
	for path in manifest:
		manifest_set[path] = true

	var orphans: Array[String] = []
	for path in discovered:
		if not manifest_set.has(path):
			orphans.append(path)

	var missing: Array[String] = []
	for path in manifest:
		if path not in discovered:
			missing.append(path)

	return {
		"ok": orphans.is_empty() and missing.is_empty(),
		"discovered_count": discovered.size(),
		"manifest_count": manifest.size(),
		"orphans": orphans,
		"missing": missing,
	}


static func print_manifest_result(result: Dictionary) -> void:
	if result.get("ok", false):
		print(
			"MANIFEST OK: %d manifest entries match %d discovered test scripts"
			% [int(result.get("manifest_count", 0)), int(result.get("discovered_count", 0))]
		)
		return
	print("MANIFEST FAIL:")
	for path in result.get("orphans", []):
		print("  ORPHAN (on disk, not in manifest): %s" % path)
	for path in result.get("missing", []):
		print("  MISSING (in manifest, not on disk): %s" % path)


static func resolve_scripts(manifest: Array[String], cfg: Dictionary) -> Dictionary:
	var manifest_result := validate_manifest(manifest)
	if not manifest_result.get("ok", false):
		return {
			"ok": false,
			"scripts": [],
			"mode": "manifest_fail",
			"manifest_result": manifest_result,
			"filter_was_supplied": bool(cfg.get("filter_was_supplied", false)),
		}

	if bool(cfg.get("list_only", false)) or bool(cfg.get("manifest_check_only", false)):
		return {
			"ok": true,
			"scripts": manifest.duplicate(),
			"mode": "list" if cfg.get("list_only", false) else "manifest_check",
			"manifest_result": manifest_result,
			"filter_was_supplied": bool(cfg.get("filter_was_supplied", false)),
		}

	var exact_paths: Array[String] = _load_exact_paths(cfg)
	if not exact_paths.is_empty():
		var invalid: Array[String] = []
		var manifest_set: Dictionary = {}
		for path in manifest:
			manifest_set[path] = true
		for path in exact_paths:
			if not manifest_set.has(path):
				invalid.append(path)
		if not invalid.is_empty():
			return {
				"ok": false,
				"scripts": [],
				"mode": "exact_paths_invalid",
				"invalid_paths": invalid,
				"manifest_result": manifest_result,
				"filter_was_supplied": true,
			}
		return {
			"ok": true,
			"scripts": exact_paths,
			"mode": "exact_paths",
			"manifest_result": manifest_result,
			"filter_was_supplied": true,
		}

	var selected: Array[String] = []
	var mode: String = "full"
	if str(cfg.get("category", "")) != "":
		mode = "category"
		selected = _select_by_category(manifest, str(cfg["category"]))
	elif str(cfg.get("filters_csv", "")) != "":
		mode = "filters"
		selected = _select_by_substrings(manifest, _split_csv(str(cfg["filters_csv"])))
	elif str(cfg.get("filter", "")) != "":
		mode = "filter"
		selected = _select_by_substrings(manifest, [str(cfg["filter"])])

	if mode != "full":
		return {
			"ok": true,
			"scripts": selected,
			"mode": mode,
			"manifest_result": manifest_result,
			"filter_was_supplied": true,
		}

	return {
		"ok": true,
		"scripts": manifest.duplicate(),
		"mode": "full",
		"manifest_result": manifest_result,
		"filter_was_supplied": false,
	}


static func category_for_path(script_path: String) -> String:
	var rel: String = script_path.replace(TESTS_ROOT, "")
	var first: String = rel.get_slice("/", 0)
	for category in CATEGORY_DIRS.keys():
		var dirs: Array = CATEGORY_DIRS[category]
		if first in dirs:
			return category
	return first


static func print_test_list(manifest: Array[String]) -> void:
	var rows: Array = []
	for script_path in manifest:
		rows.append({
			"category": category_for_path(script_path),
			"path": script_path,
			"file": script_path.get_file(),
		})
	rows.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		if a["category"] != b["category"]:
			return str(a["category"]) < str(b["category"])
		return str(a["path"]) < str(b["path"])
	)
	print("TEST LIST (%d tests):" % rows.size())
	for row in rows:
		print("  %s  %s  %s" % [row["category"], row["path"], row["file"]])


static func write_timing_json(path: String, records: Array, summary: Dictionary) -> bool:
	var payload := {
		"summary": summary,
		"tests": records,
	}
	var text: String = JSON.stringify(payload, "\t")
	if not SimPaths.ensure_parent_dir(path):
		push_error("TestRunnerSupport: could not create parent dir for timing JSON %s" % path)
		return false
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("TestRunnerSupport: could not write timing JSON to %s" % path)
		return false
	file.store_string(text)
	file.close()
	return true


static func print_timing_summary(records: Array, total_us: int) -> void:
	var total_s: float = float(total_us) / 1_000_000.0
	print("TIMING: total=%.2fs" % total_s)
	print("TIMING: slowest 20:")
	var sorted: Array = records.duplicate()
	sorted.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return int(a.get("duration_us", 0)) > int(b.get("duration_us", 0))
	)
	var limit: int = mini(20, sorted.size())
	for i in range(limit):
		var row: Dictionary = sorted[i]
		var dur_s: float = float(int(row.get("duration_us", 0))) / 1_000_000.0
		print("  %.2fs  %s" % [dur_s, str(row.get("name", row.get("path", "")))])


static func _discover_test_scripts() -> Array[String]:
	var result: Array[String] = []
	var exclude_set: Dictionary = {}
	for rel in EXCLUDE_REL_PATHS:
		exclude_set[TESTS_ROOT + rel] = true
	_collect_test_scripts_recursive(TESTS_ROOT, exclude_set, result)
	result.sort()
	return result


static func _collect_test_scripts_recursive(dir_path: String, exclude_set: Dictionary, out: Array[String]) -> void:
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return
	dir.list_dir_begin()
	var entry_name: String = dir.get_next()
	while entry_name != "":
		if entry_name != "." and entry_name != "..":
			var full: String = dir_path.path_join(entry_name)
			if dir.current_is_dir():
				_collect_test_scripts_recursive(full, exclude_set, out)
			elif entry_name.begins_with("test_") and entry_name.ends_with(".gd"):
				if not exclude_set.has(full):
					out.append(full)
		entry_name = dir.get_next()
	dir.list_dir_end()


static func _load_exact_paths(cfg: Dictionary) -> Array[String]:
	var json_path: String = str(cfg.get("exact_paths_json", ""))
	if json_path != "":
		var text: String = FileAccess.get_file_as_string(json_path)
		if text.is_empty():
			push_error("TestRunnerSupport: empty or missing exact paths JSON: %s" % json_path)
			return []
		var parsed: Variant = JSON.parse_string(text)
		if typeof(parsed) != TYPE_ARRAY:
			push_error("TestRunnerSupport: exact paths JSON must be an array")
			return []
		var paths: Array[String] = []
		for item in parsed:
			paths.append(str(item))
		return paths

	var list_path: String = str(cfg.get("test_list_file", ""))
	if list_path == "":
		return []

	var list_text: String = FileAccess.get_file_as_string(list_path)
	if list_text.is_empty():
		push_error("TestRunnerSupport: empty or missing test list file: %s" % list_path)
		return []
	var paths_from_file: Array[String] = []
	for line in list_text.split("\n", false):
		var trimmed: String = line.strip_edges()
		if trimmed != "" and not trimmed.begins_with("#"):
			paths_from_file.append(trimmed)
	return paths_from_file


static func _split_csv(raw: String) -> Array[String]:
	var parts: Array[String] = []
	for piece in raw.split(",", false):
		var trimmed: String = piece.strip_edges()
		if trimmed != "":
			parts.append(trimmed)
	return parts


static func _select_by_substrings(manifest: Array[String], needles: Array[String]) -> Array[String]:
	var selected: Array[String] = []
	for script_path in manifest:
		for needle in needles:
			if needle in script_path:
				selected.append(script_path)
				break
	return selected


static func _select_by_category(manifest: Array[String], category: String) -> Array[String]:
	if not CATEGORY_DIRS.has(category):
		return []
	var allowed_dirs: Array = CATEGORY_DIRS[category]
	var selected: Array[String] = []
	for script_path in manifest:
		var rel: String = script_path.replace(TESTS_ROOT, "")
		var first: String = rel.get_slice("/", 0)
		if first in allowed_dirs:
			selected.append(script_path)
	return selected
