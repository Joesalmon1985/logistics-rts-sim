extends "res://sim/tests/test_case.gd"

## MIL-SCAFFOLD-002: all military_*.json config files load with integer-only,
## non-empty data and are reachable through MilitaryConfig accessors.


func get_name() -> String:
	return "test_MIL_SCAFFOLD_002_config_loads"


func _run_body() -> void:
	var cfg: MilitaryConfig = MilitaryConfig.load_all()
	assert_true(cfg.buildings.size() >= 5, "buildings config loaded")
	assert_true(cfg.units.size() >= 5, "units config loaded")
	assert_true(cfg.weapons.size() >= 3, "weapons config loaded")
	assert_true(cfg.armour_types.size() >= 3, "armour types loaded")
	assert_true(cfg.tech.size() >= 1, "tech tree loaded")

	# Config numbers come from JSON (parsed as float in Godot) but must be
	# whole numbers — the sim casts them via int() into integer authoritative
	# state. Verify there are no fractional config values.
	var hub: Dictionary = cfg.get_building_def("command_hub")
	assert_false(hub.is_empty(), "command_hub def present")
	assert_true(_is_whole(hub.get("hp", 0)), "building hp is a whole number")
	assert_true(_is_whole(hub.get("credit_cost", 0)), "building cost is a whole number")
	assert_eq(int(hub.get("hp", 0)), 1500, "command hub hp")

	var rifle: Dictionary = cfg.get_unit_def("rifle_squad")
	assert_false(rifle.is_empty(), "rifle_squad def present")
	assert_true(_is_whole(rifle.get("speed", 0)), "unit speed is a whole number")

	# Damage modifier matrix returns integer percentages.
	var modifier: int = cfg.damage_modifier("small_arms", "infantry")
	assert_eq(typeof(modifier), TYPE_INT, "damage modifier accessor returns int")
	assert_true(modifier > 0, "small_arms vs infantry modifier positive")


func _is_whole(value: Variant) -> bool:
	return float(value) == float(int(value))
