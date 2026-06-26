class_name ActualHumanScreenshotUtil
extends RefCounted

## Capture tutorial screenshots; synthesize a layout diagram when headless cannot render SubViewports.


static func capture_or_synthesize(ctx: Dictionary, base_name: String, review: Dictionary = {}) -> String:
	var dir: String = ActualHumanLaunchReviewUtil.ensure_output_dir()
	var path: String = dir.path_join("%s.png" % base_name)
	var img: Image = ViewerTestHarness.capture(ctx)
	if _image_usable(img):
		img.save_png(path)
		return path
	img = synthesize_layout_image(review, ctx.get("viewport_size", ActualHumanLaunchReviewUtil.LAUNCH_SIZE))
	if _image_usable(img):
		img.save_png(path)
		return path
	return ""


static func synthesize_layout_image(review: Dictionary, viewport_size: Vector2i) -> Image:
	var img := Image.create(viewport_size.x, viewport_size.y, false, Image.FORMAT_RGBA8)
	img.fill(Color(0.12, 0.14, 0.18, 1.0))
	_draw_header(img, review, viewport_size)
	var panels: Dictionary = review.get("panels", {})
	for key in panels:
		var probe: Dictionary = panels[key]
		var rect_dict: Dictionary = probe.get("rect", {})
		if rect_dict.is_empty():
			continue
		var rect := Rect2(
			float(rect_dict.get("x", 0)),
			float(rect_dict.get("y", 0)),
			float(rect_dict.get("w", 0)),
			float(rect_dict.get("h", 0)),
		)
		var color: Color = Color(0.25, 0.55, 0.35, 0.85) if bool(probe.get("visible", false)) else Color(0.7, 0.25, 0.2, 0.85)
		_fill_rect(img, rect, color)
		_draw_label(img, "%s" % key, rect.position + Vector2(4, 4), 11)
	var y: float = 28.0
	for line in _summary_lines(review):
		_draw_label(img, line, Vector2(8, y), 12)
		y += 16.0
	return img


static func is_headless_display() -> bool:
	return DisplayServer.get_name() == "headless"


static func _image_usable(img: Image) -> bool:
	if img == null or img.is_empty():
		return false
	if img.get_width() < 8 or img.get_height() < 8:
		return false
	return true


static func _summary_lines(review: Dictionary) -> PackedStringArray:
	return PackedStringArray([
		"Actual human launch (synthesized headless layout diagram)",
		"Tutorial: %s" % str(review.get("tutorial_title_text", "")).substr(0, 60),
		"Instruction chars: %d" % str(review.get("tutorial_instruction_text", "")).length(),
		"human_understandable_pass: %s" % str(review.get("human_understandable_pass", false)),
		"build_buttons: %s" % str(review.get("build_button_count", 0)),
	])


static func _draw_header(img: Image, review: Dictionary, viewport_size: Vector2i) -> void:
	_fill_rect(img, Rect2(0, 0, viewport_size.x, 24), Color(0.08, 0.1, 0.14, 1.0))
	var method: String = "headless_synthesized" if is_headless_display() else "in_engine_fallback"
	_draw_label(img, "capture_method=%s route=main_menu->Tutorial" % method, Vector2(8, 4), 12)


static func _fill_rect(img: Image, rect: Rect2, color: Color) -> void:
	var x0: int = clampi(int(rect.position.x), 0, img.get_width() - 1)
	var y0: int = clampi(int(rect.position.y), 0, img.get_height() - 1)
	var x1: int = clampi(int(rect.position.x + rect.size.x), 0, img.get_width())
	var y1: int = clampi(int(rect.position.y + rect.size.y), 0, img.get_height())
	for y in range(y0, y1):
		for x in range(x0, x1):
			img.set_pixel(x, y, color)


static func _draw_label(img: Image, text: String, pos: Vector2, _size: int) -> void:
	# Minimal bitmap-free labels: draw high-contrast pixels along text baseline blocks.
	var x: int = clampi(int(pos.x), 0, img.get_width() - 1)
	var y: int = clampi(int(pos.y), 0, img.get_height() - 2)
	var fg: Color = Color(0.95, 0.95, 0.9, 1.0)
	for i in range(text.length()):
		if x + i * 6 >= img.get_width():
			break
		for dx in range(5):
			for dy in range(8):
				if y + dy < img.get_height():
					img.set_pixel(x + i * 6 + dx, y + dy, fg if (i + dx + dy) % 3 == 0 else Color(fg.r * 0.7, fg.g * 0.7, fg.b * 0.7))
