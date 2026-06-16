extends "res://sim/tests/test_case.gd"


func get_name() -> String:
	return "test_ECON_BOT_003_dual_replay_hash"


func _run_body() -> void:
	var run_once := func() -> Dictionary:
		var runner := SimulationTickRunner.new()
		var scenario: Dictionary = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
		var state: GameState = runner.create_game(scenario)
		EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
		var dual := DualBotRunner.new(runner)
		state = dual.run_match(state, scenario)
		return {
			"hash": ReplayHash.compute_state_hash(state),
			"commands": dual.get_recorder().get_command_entries(),
		}

	var first: Dictionary = run_once.call()
	var second: Dictionary = run_once.call()
	assert_eq(first["hash"], second["hash"], "dual-bot replay hash stable")
	assert_eq(JSON.stringify(first["commands"]), JSON.stringify(second["commands"]), "dual-bot command log stable")

	for i in range(mini(first["commands"].size(), 40)):
		pass

	var by_tick: Dictionary = {}
	for entry in first["commands"]:
		var tick_key: int = int(entry.get("tick", -1))
		if not by_tick.has(tick_key):
			by_tick[tick_key] = []
		by_tick[tick_key].append(entry)
	for tick_key in by_tick:
		var group: Array = by_tick[tick_key]
		if group.size() < 2:
			continue
		var p1_cmd: Dictionary = group[0].get("command", {})
		var p2_cmd: Dictionary = group[1].get("command", {})
		var p1_id: String = str(p1_cmd.get("player_id", p1_cmd.get("payload", {}).get("player_id", "")))
		var p2_id: String = str(p2_cmd.get("player_id", p2_cmd.get("payload", {}).get("player_id", "")))
		assert_eq(p1_id, "p1", "p1 command before p2 at tick %d" % tick_key)
		assert_eq(p2_id, "p2", "p2 command after p1 at tick %d" % tick_key)
