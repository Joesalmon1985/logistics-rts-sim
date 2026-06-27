class_name ViewerTestHarness
extends RefCounted

## Mounts viewer demo scenes in a SubViewport for pixel/layout observability tests.

const SIZE := Vector2i(1920, 1080)
const DEFAULT_FRAMES := 6

const SCENE_PATHS: Dictionary = {
	"economy": "res://client/scenes/economy_viewer_demo.tscn",
	"military": "res://client/scenes/military_viewer_demo.tscn",
	"spectator": "res://client/scenes/economy_spectator_bot_demo.tscn",
	"military_spectator": "res://client/scenes/military_spectator_demo.tscn",
	"hybrid_observer": "res://client/scenes/hybrid_observer_demo.tscn",
	"hybrid_mvp": "res://client/scenes/hybrid_mvp_game.tscn",
	"hybrid_3d_spectator": "res://client/scenes/hybrid_3d_bot_spectator.tscn",
	"settler_mvp": "res://client/scenes/settler_mvp_game.tscn",
	"military_mvp": "res://client/scenes/military_mvp_game.tscn",
	"main_menu": "res://client/scenes/main_menu.tscn",
}


static func mount(host: Node, variant: String) -> Dictionary:
	if not SCENE_PATHS.has(variant):
		return {"ok": false, "reason": "unknown variant %s" % variant}
	var packed: PackedScene = load(str(SCENE_PATHS[variant]))
	if packed == null:
		return {"ok": false, "reason": "failed to load scene"}
	var scene: Node = packed.instantiate()
	if variant in ["settler_mvp", "military_mvp", "main_menu", "hybrid_mvp"]:
		if scene == null:
			return {"ok": false, "reason": "failed to instantiate playable scene"}
	elif variant in ["spectator", "military_spectator", "hybrid_observer", "hybrid_3d_spectator"]:
		if scene == null:
			return {"ok": false, "reason": "failed to instantiate spectator scene"}
	else:
		var viewer_scene: ViewerRoot = scene as ViewerRoot
		if viewer_scene == null:
			return {"ok": false, "reason": "scene is not ViewerRoot"}
		scene = viewer_scene
	var subvp := SubViewport.new()
	subvp.size = SIZE
	subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	if scene is Control:
		var control_scene: Control = scene as Control
		control_scene.set_anchors_preset(Control.PRESET_TOP_LEFT)
		control_scene.position = Vector2.ZERO
		control_scene.custom_minimum_size = SIZE
		control_scene.size = SIZE
	subvp.add_child(scene)
	host.add_child(subvp)
	var map: MapViewport = scene.get_node_or_null("%MapViewport") as MapViewport
	var controller: Variant = null
	if scene != null and "controller" in scene:
		controller = scene.controller
	return {
		"ok": true,
		"variant": variant,
		"scene": scene,
		"viewport": subvp,
		"map": map,
		"controller": controller,
		"frames_left": DEFAULT_FRAMES,
	}


static func tick_frames(ctx: Dictionary) -> bool:
	var left: int = int(ctx.get("frames_left", 0))
	if left > 0:
		ctx["frames_left"] = left - 1
		return false
	var map: MapViewport = ctx.get("map")
	if map != null and not map.render_data.is_empty():
		map.fit_to_content()
		map.queue_redraw()
	return true


static func capture(ctx: Dictionary) -> Image:
	var subvp: SubViewport = ctx.get("viewport")
	if subvp == null:
		return Image.create(1, 1, false, Image.FORMAT_RGBA8)
	subvp.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	RenderingServer.force_draw(true, 0.0)
	var tex: ViewportTexture = subvp.get_texture()
	if tex == null:
		return Image.create(1, 1, false, Image.FORMAT_RGBA8)
	var img: Image = tex.get_image()
	if img == null or img.is_empty():
		return Image.create(1, 1, false, Image.FORMAT_RGBA8)
	return img


static func teardown(ctx: Dictionary) -> void:
	var scene: Node = ctx.get("scene")
	if scene != null and is_instance_valid(scene):
		var economy_sprite_layer: EconomySpriteLayer = scene.get_node_or_null("%SpriteLayer") as EconomySpriteLayer
		if economy_sprite_layer != null:
			economy_sprite_layer.clear_all()
		var military_sprite_layer: MilitarySpriteLayer = scene.get_node_or_null("%SpriteLayer") as MilitarySpriteLayer
		if military_sprite_layer != null:
			military_sprite_layer.clear_all()
		var hybrid_sprite_layer: HybridSpriteLayer = scene.get_node_or_null("%SpriteLayer") as HybridSpriteLayer
		if hybrid_sprite_layer != null:
			hybrid_sprite_layer.clear_all()
		var hybrid_sprite_layer2: HybridSpriteLayer = scene.find_child("HybridSpriteLayer", true, false) as HybridSpriteLayer
		if hybrid_sprite_layer2 != null:
			hybrid_sprite_layer2.clear_all()
	var subvp: SubViewport = ctx.get("viewport")
	if subvp != null and is_instance_valid(subvp):
		subvp.queue_free()


static func load_production_demo(ctx: Dictionary) -> void:
	var scene: ViewerRoot = ctx.get("scene")
	if scene != null and scene.has_method("reset_demo"):
		scene.reset_demo()


static func load_build_tutorial(ctx: Dictionary) -> void:
	var scene: ViewerRoot = ctx.get("scene")
	if scene != null and scene.has_method("load_build_tutorial"):
		scene.load_build_tutorial()


static func find_command(controller: ViewerController, cmd_type: String, building_type: String = "") -> Dictionary:
	for cmd in controller.get_available_commands():
		if str(cmd.get("type", "")) != cmd_type:
			continue
		if building_type != "" and str(cmd.get("payload", {}).get("building_type", "")) != building_type:
			continue
		return cmd
	return {}
