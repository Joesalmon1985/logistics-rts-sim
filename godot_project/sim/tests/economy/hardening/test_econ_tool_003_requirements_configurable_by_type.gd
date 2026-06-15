extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_TOOL_003_requirements_configurable_by_type"


func _run_body() -> void:
	var cfg: EconomyConfig = EconomyConfig.load_all()
	var woodcutter: Dictionary = cfg.get_tool_requirement("woodcutter")
	var baker: Dictionary = cfg.get_tool_requirement("baker")
	var carrier: Dictionary = cfg.get_tool_requirement("carrier")

	assert_eq(str(woodcutter.get("ware", "")), "tools")
	assert_eq(int(woodcutter.get("amount", 0)), 1)
	assert_eq(int(baker.get("amount", 0)), 0, "baker tool requirement configurable to zero")
	assert_eq(int(carrier.get("amount", 0)), 0, "carrier tool requirement configurable to zero")
	assert_true(int(woodcutter.get("amount", 0)) != int(carrier.get("amount", 0)), "requirements differ per worker type")
