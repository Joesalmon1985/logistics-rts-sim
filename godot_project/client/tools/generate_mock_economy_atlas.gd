extends SceneTree

## Generates placeholder mock economy spritesheet PNG + JSON (RUN_05C.2).

const FRAME_W: int = 32
const FRAME_H: int = 32
const OUTPUT_DIR: String = "res://client/art"

const ANIMATIONS: Dictionary = {
	"carrier_idle": {"count": 2, "base": Color(0.55, 0.35, 0.15), "delta": 0.08, "style": "diamond"},
	"carrier_move_loop": {"count": 4, "base": Color(0.95, 0.65, 0.1), "delta": 0.12, "style": "diamond"},
	"carrier_carrying_loop": {"count": 4, "base": Color(0.85, 0.5, 0.15), "delta": 0.1, "style": "diamond_box"},
	"ware_log_move_loop": {"count": 4, "base": Color(0.45, 0.28, 0.1), "delta": 0.1, "style": "log"},
	"ware_plank_move_loop": {"count": 4, "base": Color(0.72, 0.55, 0.32), "delta": 0.1, "style": "plank"},
	"ware_stone_move_loop": {"count": 4, "base": Color(0.55, 0.55, 0.58), "delta": 0.08, "style": "stone"},
	"ware_generic_move_loop": {"count": 4, "base": Color(0.72, 0.52, 0.28), "delta": 0.1, "style": "box"},
	"road_flow_loop": {"count": 4, "base": Color(0.95, 0.72, 0.1), "delta": 0.15, "style": "stripe_h"},
	"building_idle": {"count": 2, "base": Color(0.45, 0.45, 0.5), "delta": 0.05, "style": "square"},
	"building_working_loop": {"count": 4, "base": Color(0.35, 0.72, 0.42), "delta": 0.12, "style": "square_pulse"},
	"building_blocked_loop": {"count": 4, "base": Color(0.55, 0.25, 0.25), "delta": 0.08, "style": "square_x"},
	"building_activity_pulse": {"count": 3, "base": Color(0.4, 0.85, 0.45), "delta": 0.2, "style": "square_flash"},
	"construction_idle": {"count": 2, "base": Color(0.5, 0.48, 0.42), "delta": 0.06, "style": "scaffold"},
	"construction_progress_25": {"count": 2, "base": Color(0.55, 0.52, 0.4), "delta": 0.08, "style": "scaffold_25"},
	"construction_progress_50": {"count": 2, "base": Color(0.6, 0.56, 0.42), "delta": 0.1, "style": "scaffold_50"},
	"construction_progress_75": {"count": 2, "base": Color(0.65, 0.6, 0.45), "delta": 0.12, "style": "scaffold_75"},
	"construction_progress_loop": {"count": 4, "base": Color(0.62, 0.58, 0.45), "delta": 0.14, "style": "scaffold"},
	"construction_complete_pulse": {"count": 3, "base": Color(0.85, 0.75, 0.2), "delta": 0.18, "style": "flash"},
	"storage_idle": {"count": 2, "base": Color(0.42, 0.38, 0.55), "delta": 0.06, "style": "crate"},
	"storage_activity_pulse": {"count": 3, "base": Color(0.55, 0.45, 0.85), "delta": 0.16, "style": "crate_flash"},
	"warning_blocked_loop": {"count": 4, "base": Color(0.9, 0.35, 0.2), "delta": 0.1, "style": "warning"},
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
		var frame_index: int = int(meta.get("index", 0))
		var delta: float = float(frame_index) * float(spec.get("delta", 0.1))
		var style: String = str(spec.get("style", "square"))
		_paint_frame(image, x, y, base.lightened(delta), style, frame_index)

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


static func _paint_frame(image: Image, x: int, y: int, color: Color, style: String, frame_index: int) -> void:
	for py in range(FRAME_H):
		for px in range(FRAME_W):
			var cx: float = float(px) / float(FRAME_W - 1)
			var cy: float = float(py) / float(FRAME_H - 1)
			var edge: bool = px <= 1 or py <= 1 or px >= FRAME_W - 2 or py >= FRAME_H - 2
			var c: Color = color
			var alpha: float = 1.0
			match style:
				"diamond":
					alpha = 1.0 if absf(cx - 0.5) + absf(cy - 0.5) < 0.42 + float(frame_index) * 0.02 else 0.0
				"diamond_box":
					alpha = 1.0 if absf(cx - 0.5) + absf(cy - 0.5) < 0.38 else 0.0
					if px > 18 and py > 18:
						c = c.darkened(0.2)
				"log":
					alpha = 1.0 if cy > 0.35 and cy < 0.65 and cx > 0.1 + float(frame_index) * 0.05 else 0.0
				"plank":
					alpha = 1.0 if cy > 0.3 and cy < 0.7 and fmod(px + frame_index * 4, 8) < 6 else 0.0
				"stone":
					alpha = 1.0 if (cx - 0.5) * (cx - 0.5) + (cy - 0.5) * (cy - 0.5) < 0.18 else 0.0
				"box":
					alpha = 1.0 if cx > 0.2 and cx < 0.8 and cy > 0.25 and cy < 0.75 else 0.0
				"stripe_h":
					alpha = 1.0 if int(py + frame_index * 3) % 8 < 4 else 0.0
				"square", "square_pulse", "square_x", "square_flash":
					alpha = 1.0 if cx > 0.15 and cx < 0.85 and cy > 0.15 and cy < 0.85 else 0.0
					if style == "square_x" and absf(cx - cy) < 0.08:
						c = Color.WHITE
					if style == "square_flash" and frame_index == 1:
						c = c.lightened(0.35)
				"scaffold", "scaffold_25", "scaffold_50", "scaffold_75":
					alpha = 1.0 if cx > 0.1 and cx < 0.9 else 0.0
					var fill_h: float = 0.25
					if style == "scaffold_25":
						fill_h = 0.25
					elif style == "scaffold_50":
						fill_h = 0.5
					elif style == "scaffold_75":
						fill_h = 0.75
					elif style == "scaffold":
						fill_h = 0.35 + float(frame_index) * 0.1
					if cy > 1.0 - fill_h:
						c = c.lightened(0.1)
				"crate", "crate_flash":
					alpha = 1.0 if cx > 0.2 and cx < 0.8 and cy > 0.3 and cy < 0.8 else 0.0
					if style == "crate_flash" and frame_index == 1:
						c = c.lightened(0.3)
				"flash":
					alpha = 1.0 if (cx - 0.5) * (cx - 0.5) + (cy - 0.5) * (cy - 0.5) < 0.2 + float(frame_index) * 0.05 else 0.0
				"warning":
					alpha = 1.0 if cy < 0.55 - absf(cx - 0.5) * 0.8 or (cx > 0.42 and cx < 0.58 and cy > 0.55) else 0.0
				_:
					alpha = 1.0
			if edge and alpha > 0.0:
				c = c.darkened(0.3)
			c.a = alpha
			if alpha > 0.01:
				image.set_pixel(x + px, y + py, c)
