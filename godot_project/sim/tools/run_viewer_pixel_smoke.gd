extends SceneTree

## RUN_05 viewport pixel smoke — GOD-SMOKE-004 (non-headless; requires display).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")

enum Phase {
	INIT,
	WAIT_ECONOMY,
	TEST_ECONOMY,
	WAIT_BUILD,
	TEST_BUILD,
	WAIT_MILITARY,
	TEST_MILITARY,
	DONE,
}

var _phase: Phase = Phase.INIT
var _ctx: Dictionary = {}
var _build_ctx: Dictionary = {}
var _mil_ctx: Dictionary = {}
var _failed: bool = false
var _fail_reasons: PackedStringArray = PackedStringArray()
var _img_a: Image
var _img_b: Image
var _img_c: Image
var _place_dto: Dictionary = {}


func _initialize() -> void:
	BootstrapScript.load_all()
	call_deferred("_begin")


func _begin() -> void:
	_phase = Phase.INIT
	_ctx = ViewerTestHarness.mount(root, "economy")
	if not bool(_ctx.get("ok", false)):
		_fail("mount economy: %s" % str(_ctx.get("reason", "?")))
		_finish()
		return
	_phase = Phase.WAIT_ECONOMY


func _process(_delta: float) -> bool:
	match _phase:
		Phase.WAIT_ECONOMY:
			if ViewerTestHarness.tick_frames(_ctx):
				_phase = Phase.TEST_ECONOMY
				call_deferred("_test_economy")
		Phase.WAIT_BUILD:
			if ViewerTestHarness.tick_frames(_build_ctx):
				_phase = Phase.TEST_BUILD
				call_deferred("_test_build")
		Phase.WAIT_MILITARY:
			if ViewerTestHarness.tick_frames(_mil_ctx):
				_phase = Phase.TEST_MILITARY
				call_deferred("_test_military")
	return false


func _test_economy() -> void:
	_run_vis_layout_001(_ctx)
	_run_god_capture_001(_ctx, "economy")
	await _run_vis_pix_chain_001(_ctx)
	ViewerTestHarness.teardown(_ctx)
	_build_ctx = ViewerTestHarness.mount(root, "economy")
	if not bool(_build_ctx.get("ok", false)):
		_fail("mount build tutorial: %s" % str(_build_ctx.get("reason", "?")))
		_finish()
		return
	ViewerTestHarness.load_build_tutorial(_build_ctx)
	_phase = Phase.WAIT_BUILD


func _test_build() -> void:
	await _run_vis_pix_build_001(_build_ctx)
	ViewerTestHarness.teardown(_build_ctx)
	_mil_ctx = ViewerTestHarness.mount(root, "military")
	if not bool(_mil_ctx.get("ok", false)):
		_fail("mount military: %s" % str(_mil_ctx.get("reason", "?")))
		_finish()
		return
	_phase = Phase.WAIT_MILITARY


func _test_military() -> void:
	_run_god_capture_001(_mil_ctx, "military")
	var img: Image = ViewerTestHarness.capture(_mil_ctx)
	var mil_check: Dictionary = ViewerPixelProbe.assert_military_entities_visible(img)
	if not bool(mil_check.get("ok", false)):
		_fail("military pixels: %s" % str(mil_check.get("reason", "?")))
	ViewerTestHarness.teardown(_mil_ctx)
	_finish()


func _run_vis_layout_001(ctx: Dictionary) -> void:
	var map: MapViewport = ctx.get("map")
	if map == null:
		_fail("VIS-LAYOUT-001: missing MapViewport")
		return
	if map.size.x < 400 or map.size.y < 300:
		_fail("VIS-LAYOUT-001: map size too small (%s)" % str(map.size))
		return
	var render: Dictionary = map.render_data
	var terrain_count: int = int(LayerRenderer.count_entities(render).get("terrain", 0))
	if terrain_count <= 0:
		_fail("VIS-LAYOUT-001: no terrain in render data")
		return
	var img: Image = ViewerTestHarness.capture(ctx)
	var region: Rect2i = ViewerPixelProbe.map_region_rect(img)
	var non_bg: int = ViewerPixelProbe.count_non_background(img, region)
	var total: int = region.size.x * region.size.y
	if non_bg < maxi(100, int(float(total) * 0.05)):
		_fail("VIS-LAYOUT-001: viewport map region mostly blank (%d non-bg)" % non_bg)
		return
	print("VIS-LAYOUT-001 PASS: map %s terrain=%d non_bg=%d" % [str(map.size), terrain_count, non_bg])


func _run_god_capture_001(ctx: Dictionary, variant: String) -> void:
	var img: Image = ViewerTestHarness.capture(ctx)
	if variant == "economy":
		var check: Dictionary = ViewerPixelProbe.assert_economy_terrain_visible(img)
		if not bool(check.get("ok", false)):
			_fail("GOD-CAPTURE-001 economy: %s" % str(check.get("reason", "?")))
			return
		print("GOD-CAPTURE-001 PASS economy: grass=%d forest=%d" % [int(check.get("grass", 0)), int(check.get("forest", 0))])
	else:
		var check2: Dictionary = ViewerPixelProbe.assert_military_entities_visible(img)
		if not bool(check2.get("ok", false)):
			_fail("GOD-CAPTURE-001 military: %s" % str(check2.get("reason", "?")))
			return
		print("GOD-CAPTURE-001 PASS military")


func _run_vis_pix_chain_001(ctx: Dictionary) -> void:
	var controller: ViewerController = ctx.get("controller")
	var map: MapViewport = ctx.get("map")
	if controller == null or map == null:
		_fail("VIS-PIX-CHAIN-001: missing controller/map")
		return
	_img_a = ViewerTestHarness.capture(ctx)
	controller.step_ticks(30)
	map.fit_to_content()
	map.queue_redraw()
	for _i in range(3):
		await process_frame
	_img_b = ViewerTestHarness.capture(ctx)
	var region: Rect2i = ViewerPixelProbe.map_region_rect(_img_a)
	var diff: int = ViewerPixelProbe.image_diff_score(_img_a, _img_b, region)
	var forest_region: Rect2i = ViewerPixelProbe.hex_region_in_viewport_image(map, _img_b, 8, 5, 32.0)
	var forest_diff: int = ViewerPixelProbe.image_diff_score(_img_a, _img_b, forest_region)
	var amber: int = ViewerPixelProbe.count_color_near(_img_b, Color(0.95, 0.65, 0.15), region, 0.15)
	var signals: int = 0
	if diff > 80:
		signals += 1
	if forest_diff > 10:
		signals += 1
	if amber > 5:
		signals += 1
	if signals < 1:
		_fail("VIS-PIX-CHAIN-001: no visible viewport delta after 30 ticks (diff=%d forest_diff=%d amber=%d)" % [diff, forest_diff, amber])
		return
	print("VIS-PIX-CHAIN-001 PASS: diff=%d forest_diff=%d amber=%d" % [diff, forest_diff, amber])


func _run_vis_pix_build_001(ctx: Dictionary) -> void:
	var controller: ViewerController = ctx.get("controller")
	var map: MapViewport = ctx.get("map")
	if controller == null or map == null:
		_fail("VIS-PIX-BUILD-001: missing controller/map")
		return
	_img_a = ViewerTestHarness.capture(ctx)
	_place_dto = ViewerTestHarness.find_command(controller, "PlaceBuildingPlanCommand", "storehouse")
	if _place_dto.is_empty():
		_fail("VIS-PIX-BUILD-001: no storehouse placement command")
		return
	if not controller.submit_command_dto(_place_dto).success:
		_fail("VIS-PIX-BUILD-001: placement rejected")
		return
	map.fit_to_content()
	map.queue_redraw()
	for _i in range(3):
		await process_frame
	_img_b = ViewerTestHarness.capture(ctx)
	var sq: int = int(_place_dto.get("payload", {}).get("q", 0))
	var sr: int = int(_place_dto.get("payload", {}).get("r", 0))
	var site_region: Rect2i = ViewerPixelProbe.hex_region_in_viewport_image(map, _img_b, sq, sr, 36.0)
	var site_diff: int = ViewerPixelProbe.image_diff_score(_img_a, _img_b, site_region)
	var site_green: int = ViewerPixelProbe.count_color_near(_img_b, Color(0.35, 0.95, 0.45), site_region, 0.2)
	if site_diff < 5 and site_green < 3:
		_fail("VIS-PIX-BUILD-001: construction site not visible at (%d,%d) diff=%d green=%d" % [sq, sr, site_diff, site_green])
		return
	for _i in range(30):
		controller.step_tick()
	map.fit_to_content()
	map.queue_redraw()
	for _i in range(3):
		await process_frame
	_img_c = ViewerTestHarness.capture(ctx)
	var progress_diff: int = ViewerPixelProbe.image_diff_score(_img_b, _img_c, site_region)
	var full_diff: int = ViewerPixelProbe.image_diff_score(_img_b, _img_c, ViewerPixelProbe.map_region_rect(_img_c))
	if progress_diff < 3 and full_diff < 40:
		_fail("VIS-PIX-BUILD-001: no visible progress after 30 ticks (site_diff=%d full_diff=%d)" % [progress_diff, full_diff])
		return
	print("VIS-PIX-BUILD-001 PASS: site_diff=%d site_green=%d progress_diff=%d" % [site_diff, site_green, progress_diff])


func _fail(reason: String) -> void:
	_failed = true
	_fail_reasons.append(reason)
	print("FAIL: %s" % reason)


func _finish() -> void:
	if _failed:
		print("GOD-SMOKE-004 FAIL")
		for r in _fail_reasons:
			print("  - %s" % r)
		print("VIEWER PIXEL SMOKE FAIL")
		print("EXIT: 1 (fail)")
		quit(1)
	else:
		print("GOD-SMOKE-004 PASS: viewport observability")
		print("VIEWER PIXEL SMOKE PASS")
		print("EXIT: 0 (pass)")
		quit(0)
