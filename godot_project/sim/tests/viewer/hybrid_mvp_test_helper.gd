class_name HybridMvpTestHelper
extends RefCounted

const HQ_Q: int = 2
const HQ_R: int = 2


static func settle_frames(ctx: Dictionary, count: int = ViewerTestHarness.DEFAULT_FRAMES) -> void:
	for _i in range(count):
		ViewerTestHarness.tick_frames(ctx)


static func simulate_map_click_at_hex(scene: Node, q: int, r: int) -> void:
	if scene == null:
		return
	if scene.has_method("_on_map_click"):
		scene.call("_on_map_click", q, r)
	elif scene.has_method("select_hex_for_tests"):
		scene.call("select_hex_for_tests", q, r)


static func simulate_map_click_at_hq(scene: Node) -> void:
	simulate_map_click_at_hex(scene, HQ_Q, HQ_R)


static func teardown(ctx: Dictionary, host: Node = null) -> void:
	ViewerTestHarness.teardown(ctx)
	if host != null and is_instance_valid(host):
		host.queue_free()
