extends TestCase

const ReviewUtil := preload("res://sim/tools/actual_human_launch_review_util.gd")


func get_name() -> String:
	return "test_actual_human_sprite_assets_distinct"


func _run_body() -> void:
	var review: Dictionary = ReviewUtil.probe_sprite_legibility()
	assert_true(bool(review.get("hq_sprite_visible", false)), "hq sprite")
	assert_true(bool(review.get("woodcutter_sprite_distinct", false)), "woodcutter distinct")
	assert_true(bool(review.get("forester_sprite_distinct", false)), "forester distinct")
	assert_true(bool(review.get("sawmill_sprite_distinct", false)), "sawmill distinct")
	assert_true(bool(review.get("quarry_sprite_distinct", false)), "quarry distinct")
	assert_true(bool(review.get("construction_site_distinct", false)), "construction distinct")
	assert_true(bool(review.get("terrain_distinct", false)), "terrain distinct")
