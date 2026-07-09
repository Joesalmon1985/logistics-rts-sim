extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_GOD_F_001_heuristic_module_proposals"


func _run_body() -> void:
	var bot := HeuristicModularGodBot.new("p1")
	bot.decide(1)
	var proposals: Array = bot.get_module_proposals()
	assert_true(proposals.size() >= 2, "heuristic emits module proposals")
	assert_eq(str(proposals[0].get("module", "")), "EconomyPolicy", "economy module present")
