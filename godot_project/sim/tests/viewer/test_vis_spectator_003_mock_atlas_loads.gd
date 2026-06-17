extends TestCase

## VIS-SPECTATOR-003 — mock economy atlas loads named animations.


func get_name() -> String:
	return "test_VIS_SPECTATOR_003_mock_atlas_loads"


func _run_body() -> void:
	var names: PackedStringArray = MockEconomyAtlas.animation_names()
	assert_true(names.size() >= 15, "expected at least 15 named animations (got %d)" % names.size())
	var required: Array[String] = [
		"carrier_idle",
		"carrier_move_loop",
		"ware_log_move_loop",
		"ware_generic_move_loop",
		"road_flow_loop",
		"building_idle",
		"building_working_loop",
		"building_blocked_loop",
		"construction_idle",
		"construction_progress_loop",
		"construction_complete_pulse",
		"storage_idle",
		"storage_activity_pulse",
		"warning_blocked_loop",
	]
	for anim_name in required:
		assert_true(MockEconomyAtlas.has_animation(anim_name), "missing animation %s" % anim_name)
	var sprite_frames: SpriteFrames = MockEconomyAtlas.get_sprite_frames()
	assert_true(sprite_frames != null, "sprite frames null")
	for anim_name in required:
		var anim_id: StringName = StringName(anim_name)
		assert_true(sprite_frames.has_animation(anim_id), "sprite_frames missing %s" % anim_name)
		assert_true(sprite_frames.get_frame_count(anim_id) >= 1, "animation %s has no frames" % anim_name)
	var sprite := AnimatedSprite2D.new()
	sprite.sprite_frames = sprite_frames
	sprite.animation = StringName("carrier_move_loop")
	sprite.frame = 0
	assert_eq(str(sprite.animation), "carrier_move_loop", "animated sprite animation name")
