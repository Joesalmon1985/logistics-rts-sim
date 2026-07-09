extends SceneTree

## Capture hybrid 2D spectator screenshots (PRD B.11 scaffold).

const OUT_DIR := "res://../docs/audit/neural_hybrid_ai/visual/"


func _initialize() -> void:
	var host := Node.new()
	root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_2d_spectator")
	if not bool(ctx.get("ok", false)):
		print("CAPTURE FAIL: could not mount spectator")
		quit(1)
	var scene: Node = ctx.get("scene")
	if scene != null and scene.has_method("ensure_match_started"):
		scene.call("ensure_match_started")
	for _i in range(12):
		ViewerTestHarness.tick_frames(ctx)
	var img: Image = ViewerTestHarness.capture(ctx)
	var path: String = ProjectSettings.globalize_path(OUT_DIR)
	DirAccess.make_dir_recursive_absolute(path)
	var file_path: String = path.path_join("hybrid_spectator_00_match_start.png")
	img.save_png(file_path)
	print("CAPTURE PASS: %s" % file_path)
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
	quit(0)
