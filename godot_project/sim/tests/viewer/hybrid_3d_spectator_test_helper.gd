class_name Hybrid3dSpectatorTestHelper
extends RefCounted

## Shared mount/layout helpers for hybrid 3D spectator viewer tests.


static func mount(host: Node) -> Dictionary:
	SimBootstrap.load_all()
	if not host.is_inside_tree():
		if host.get_parent() == null:
			Engine.get_main_loop().root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "hybrid_3d_spectator")
	var scene: Node = ctx.get("scene")
	if scene != null and scene.has_method("ensure_match_started"):
		scene.call("ensure_match_started")
	force_layout(ctx)
	return ctx


static func force_layout(ctx: Dictionary) -> void:
	var scene: Control = ctx.get("scene") as Control
	if scene != null:
		scene.set_anchors_preset(Control.PRESET_TOP_LEFT)
		scene.size = ViewerTestHarness.SIZE
		scene.custom_minimum_size = ViewerTestHarness.SIZE
	var subvp: SubViewport = ctx.get("viewport")
	if subvp != null:
		subvp.size = ViewerTestHarness.SIZE
	var world_vp: SubViewport = scene.get_node_or_null("%WorldViewport") as SubViewport if scene != null else null
	if world_vp != null:
		world_vp.size = Vector2i(1280, 720)
	for _i in range(ViewerTestHarness.DEFAULT_FRAMES + 2):
		ViewerTestHarness.tick_frames(ctx)


static func settle_frames(ctx: Dictionary, extra: int = 4) -> void:
	ctx["frames_left"] = int(ctx.get("frames_left", 0)) + extra
	while not ViewerTestHarness.tick_frames(ctx):
		pass


static func teardown(ctx: Dictionary, host: Node) -> void:
	ViewerTestHarness.teardown(ctx)
	if host != null and is_instance_valid(host):
		if host.is_inside_tree():
			host.get_parent().remove_child(host)
		host.free()
