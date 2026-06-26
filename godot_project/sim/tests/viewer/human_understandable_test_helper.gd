class_name HumanUnderstandableTestHelper
extends RefCounted

const SettlerHelper := preload("res://sim/tests/viewer/settler_mvp_test_helper.gd")
const ReviewUtil := preload("res://sim/tools/human_understandable_review_util.gd")
const HQ_Q: int = 6
const HQ_R: int = 6


static func mount_tutorial_scene(host: Node) -> Dictionary:
	var ctx: Dictionary = SettlerHelper.mount_settler(host)
	var scene: Node = ctx.get("scene")
	if scene.has_method("enable_tutorial_for_tests"):
		scene.call("enable_tutorial_for_tests")
	if scene.has_method("_refresh_tutorial_ui"):
		scene.call("_refresh_tutorial_ui")
	SettlerHelper.settle_frames(ctx)
	return ctx


static func probe_context(scene: Node, context: String) -> Dictionary:
	return ReviewUtil.probe_game_context(scene, context)
