class_name HybridSpectatorTestHelper
extends RefCounted


static func mount(host: Node) -> Dictionary:
	SimBootstrap.load_all()
	if not host.is_inside_tree():
		Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_2d_spectator")
	var scene: Node = ctx.get("scene")
	if scene != null and scene.has_method("ensure_match_started"):
		scene.call("ensure_match_started")
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES + 2):
		ViewerTestHarness.tick_frames(ctx)
	return ctx
