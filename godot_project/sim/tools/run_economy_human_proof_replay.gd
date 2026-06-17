extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	var replay_path: String = ""
	for arg in args:
		if arg.begins_with("--replay="):
			replay_path = arg.substr("--replay=".length())
	if replay_path == "":
		print("REPLAY FAIL: missing --replay=path")
		print("EXIT: 1 (fail)")
		quit(1)
		return
	var result: Dictionary = EconomyHumanProofReplay.run_replay_file(replay_path)
	if not bool(result.get("ok", false)):
		print("REPLAY FAIL: %s" % str(result.get("error", "unknown")))
		print("EXIT: 1 (fail)")
		quit(1)
		return
	var gates: Dictionary = result.get("gates", {})
	var p1: Dictionary = gates.get("players", {}).get("p1", {})
	print("tick=%d p1 b=%d integrity=ok gates_pass=%s" % [
		int(gates.get("tick", 0)),
		int(p1.get("buildings", 0)),
		str(gates.get("pass", false)),
	])
	print("REPLAY PASS")
	print("EXIT: 0 (pass)")
	quit(0)
