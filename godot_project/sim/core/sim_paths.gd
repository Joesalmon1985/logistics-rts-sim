class_name SimPaths
extends RefCounted

const EXPORT_CACHE_SUBDIR: String = "wide_rts_export"


static func default_export_dir() -> String:
	var base: String = OS.get_cache_dir().path_join(EXPORT_CACHE_SUBDIR)
	DirAccess.make_dir_recursive_absolute(base)
	return base


static func default_export_file(filename: String) -> String:
	return default_export_dir().path_join(filename)


static func ensure_parent_dir(path: String) -> bool:
	var parent: String = path.get_base_dir()
	if parent.is_empty():
		return true
	return DirAccess.make_dir_recursive_absolute(parent) == OK


static func write_text_file(path: String, text: String) -> bool:
	if not ensure_parent_dir(path):
		push_error("SimPaths: cannot create parent dir for %s" % path)
		return false
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		push_error("SimPaths: cannot write %s" % path)
		return false
	file.store_string(text)
	file.close()
	return true
