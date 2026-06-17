extends SceneTree

## Generates placeholder mock economy spritesheet PNG + JSON (RUN_05C).

const FRAME_W: int = 16
const FRAME_H: int = 16
const OUTPUT_DIR: String = "res://client/art"

const ANIMATIONS: Dictionary = {
	"carrier_idle": {"count": 2, "base": Color(0.55, 0.35, 0.15), "delta": 0.08},
	"carrier_move_loop": {"count": 4, "base": Color(0.95, 0.65, 0.1), "delta": 0.12},
	"ware_log_move_loop": {"count": 4, "base": Color(0.55, 0.32, 0.12), "delta": 0.1},
	"ware_generic_move_loop": {"count": 4, "base": Color(0.72, 0.52, 0.28), "delta": 0.1},
	"road_flow_loop": {"count": 4, "base": Color(0.95, 0.72, 0.1), "delta": 0.15},
	"building_idle": {"count": 2, "base": Color(0.45, 0.45, 0.5), "delta": 0.05},
	"building_working_loop": {"count": 4, "base": Color(0.35, 0.72, 0.42), "delta": 0.12},
	"building_blocked_loop": {"count": 4, "base": Color(0.55, 0.25, 0.25), "delta": 0.08},
	"building_activity_pulse": {"count": 3, "base": Color(0.4, 0.85, 0.45), "delta": 0.2},
	"construction_idle": {"count": 2, "base": Color(0.5, 0.48, 0.42), "delta": 0.06},
	"construction_progress_loop": {"count": 4, "base": Color(0.62, 0.58, 0.45), "delta": 0.14},
	"construction_complete_pulse": {"count": 3, "base": Color(0.85, 0.75, 0.2), "delta": 0.18},
	"storage_idle": {"count": 2, "base": Color(0.42, 0.38, 0.55), "delta": 0.06},
	"storage_activity_pulse": {"count": 3, "base": Color(0.55, 0.45, 0.85), "delta": 0.16},
	"warning_blocked_loop": {"count": 4, "base": Color(0.9, 0.35, 0.2), "delta": 0.1},
}


func _initialize() -> void:
	var result: Dictionary = _generate()
	if not bool(result.get("ok", false)):
		print("MOCK ATLAS GENERATE FAIL: %s" % str(result.get("reason", "?")))
		print("EXIT: 1 (fail)")
		quit(1)
		return
	print("MOCK ATLAS GENERATE PASS: %s" % str(result.get("path", "")))
	print("EXIT: 0 (pass)")
	quit(0)


static func ensure_generated() -> bool:
	var png_path: String = ProjectSettings.globalize_path(OUTPUT_DIR.path_join("mock_economy_sprites.png"))
	var json_path: String = ProjectSettings.globalize_path(OUTPUT_DIR.path_join("mock_economy_sprites.json"))
	if FileAccess.file_exists(png_path) and FileAccess.file_exists(json_path):
		return true
	return bool(_generate().get("ok", false))


static func _generate() -> Dictionary:
	var frames_meta: Dictionary = {}
	var animations_meta: Dictionary = {}
	var frame_names: PackedStringArray = PackedStringArray()
	for anim_name in ANIMATIONS.keys():
		var spec: Dictionary = ANIMATIONS[anim_name]
		var count: int = int(spec.get("count", 2))
		var frame_ids: PackedStringArray = PackedStringArray()
		for i in range(count):
			var frame_id: String = "%s_%d" % [anim_name, i]
			frame_ids.append(frame_id)
			frame_names.append(frame_id)
			frames_meta[frame_id] = {"anim": anim_name, "index": i, "spec": spec}
		animations_meta[anim_name] = {
			"frames": frame_ids,
			"fps": 8,
			"loop": not anim_name.ends_with("_pulse"),
		}

	var cols: int = 8
	var rows: int = int(ceil(float(frame_names.size()) / float(cols)))
	var atlas_w: int = cols * FRAME_W
	var atlas_h: int = rows * FRAME_H
	var image := Image.create(atlas_w, atlas_h, false, Image.FORMAT_RGBA8)
	image.fill(Color(0, 0, 0, 0))

	var frame_positions: Dictionary = {}
	for idx in range(frame_names.size()):
		var frame_id: String = frame_names[idx]
		var col: int = idx % cols
		var row: int = idx / cols
		var x: int = col * FRAME_W
		var y: int = row * FRAME_H
		frame_positions[frame_id] = {"x": x, "y": y, "w": FRAME_W, "h": FRAME_H}
		var meta: Dictionary = frames_meta[frame_id]
		var spec: Dictionary = meta.get("spec", {})
		var base: Color = spec.get("base", Color.GRAY)
		var delta: float = float(spec.get("index", meta.get("index", 0))) * float(spec.get("delta", 0.1))
		_paint_frame(image, x, y, base.lightened(delta))

	var global_dir: String = ProjectSettings.globalize_path(OUTPUT_DIR)
	DirAccess.make_dir_recursive_absolute(global_dir)
	var png_global: String = global_dir.path_join("mock_economy_sprites.png")
	var json_global: String = global_dir.path_join("mock_economy_sprites.json")
	if image.save_png(png_global) != OK:
		return {"ok": false, "reason": "png save failed"}

	var json_frames: Dictionary = {}
	for frame_id in frame_positions.keys():
		json_frames[frame_id] = frame_positions[frame_id]

	var json_doc: Dictionary = {
		"texture": "mock_economy_sprites.png",
		"frame_size": [FRAME_W, FRAME_H],
		"frames": json_frames,
		"animations": animations_meta,
	}
	if not SimPaths.write_text_file(json_global, JSON.stringify(json_doc, "\t")):
		return {"ok": false, "reason": "json save failed"}
	return {"ok": true, "path": png_global}


static func _paint_frame(image: Image, x: int, y: int, color: Color) -> void:
	for py in range(FRAME_H):
		for px in range(FRAME_W):
			var edge: bool = px == 0 or py == 0 or px == FRAME_W - 1 or py == FRAME_H - 1
			var c: Color = color
			if edge:
				c = c.darkened(0.25)
			image.set_pixel(x + px, y + py, c)
