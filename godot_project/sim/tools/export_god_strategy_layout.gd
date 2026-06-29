extends SceneTree

func _initialize() -> void:
	SimBootstrap.load_all()
	var path: String = GodStrategyTestHelper.write_layout_sweep_artefact()
	print("LAYOUT_SWEEP: %s" % path)
	var sweep: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(path))
	for key in ["mvp_a", "regression"]:
		if not sweep.has(key):
			continue
		var layout: Dictionary = sweep[key].get("full_chain_layout", {})
		print("%s ok=%s" % [key, str(layout.get("ok", false))])
		for pid in layout.get("players", {}).keys():
			print("  %s sites=%s" % [pid, JSON.stringify(layout["players"][pid].get("sites", {}))])
	quit(0)
