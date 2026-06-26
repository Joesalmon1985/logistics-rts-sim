extends SceneTree

## Verify settler MVP sprite legibility for actual human launch acceptance.

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")

var _failures: Array[String] = []


func _initialize() -> void:
	BootstrapScript.load_all()
	call_deferred("_run")


func _run() -> void:
	SettlerAssetRegistry.load_registry()
	var review: Dictionary = ReviewUtil.probe_sprite_legibility()
	var building_types: Array[String] = [
		"headquarters", "woodcutter", "forester", "sawmill", "quarry", "construction_site",
	]
	var hashes: Array = []
	var paths: Array = []
	for btype in building_types:
		var tex: Texture2D = SettlerAssetRegistry.get_building_texture(btype)
		if tex == null:
			_failures.append("missing_building:%s" % btype)
			continue
		var path: String = SettlerAssetRegistry.path_for("buildings", btype)
		paths.append(path)
		var img: Image = tex.get_image()
		if img == null or img.is_empty():
			_failures.append("empty_image:%s" % btype)
			continue
		hashes.append(img.get_data().hex_encode())
	for i in range(hashes.size()):
		for j in range(i + 1, hashes.size()):
			if hashes[i] == hashes[j]:
				_failures.append("identical_sprite_hashes:%s_%s" % [building_types[i], building_types[j]])
	for i in range(paths.size()):
		for j in range(i + 1, paths.size()):
			if paths[i] == paths[j]:
				_failures.append("identical_sprite_paths:%s_%s" % [building_types[i], building_types[j]])
	if not bool(review.get("hq_sprite_visible", false)):
		_failures.append("hq_sprite_missing")
	if not bool(review.get("carrier_sprite_visible", false)):
		_failures.append("carrier_sprite_missing")
	if not bool(review.get("carried_ware_icon_visible", false)):
		_failures.append("carried_ware_icons_missing")
	if not bool(review.get("terrain_distinct", false)):
		_failures.append("terrain_not_distinct")

	var dir: String = ReviewUtil.ensure_output_dir()
	var json_path: String = dir.path_join("actual_human_sprite_legibility_review.json")
	review["verify_failures"] = _failures
	SimPaths.write_text_file(json_path, JSON.stringify(review, "\t"))

	if _failures.is_empty():
		print("VERIFY ACTUAL HUMAN SPRITE LEGIBILITY PASS")
		print("EXIT: 0 (pass)")
		quit(0)
	else:
		for f in _failures:
			print("FAIL: %s" % f)
		print("VERIFY ACTUAL HUMAN SPRITE LEGIBILITY FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
