class_name MobileLayoutProfiles
extends RefCounted

## Mobile screen profiles for hex-view QA (PRD Part C).

const PROFILES: Dictionary = {
	"mobile_portrait_small": Vector2i(360, 800),
	"mobile_portrait_common": Vector2i(390, 844),
	"mobile_portrait_large": Vector2i(412, 915),
	"mobile_landscape_small": Vector2i(800, 360),
	"mobile_landscape_common": Vector2i(844, 390),
	"mobile_landscape_large": Vector2i(915, 412),
	"tablet_portrait": Vector2i(768, 1024),
	"tablet_landscape": Vector2i(1024, 768),
}

const MIN_TOUCH_TARGET_PX: int = 44


static func profile_names() -> Array:
	return PROFILES.keys()


static func get_size(profile_name: String) -> Vector2i:
	return PROFILES.get(profile_name, Vector2i(390, 844))


static func is_portrait(profile_name: String) -> bool:
	var size: Vector2i = get_size(profile_name)
	return size.y >= size.x


static func apply_to_control(control: Control, profile_name: String) -> void:
	if control == null:
		return
	var size: Vector2i = get_size(profile_name)
	control.size = Vector2(size)
	control.custom_minimum_size = Vector2(size)


static func touch_target_ok(button: Control) -> bool:
	if button == null:
		return false
	return button.size.x >= MIN_TOUCH_TARGET_PX and button.size.y >= MIN_TOUCH_TARGET_PX
