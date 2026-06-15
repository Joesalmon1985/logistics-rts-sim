extends SceneTree

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")


func _initialize() -> void:
	BootstrapScript.load_all()
	var replay_path: String = ""
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--replay="):
			replay_path = arg.substr("--replay=".length())
	if replay_path == "":
		print("VERIFY DEBUG FAIL: missing --replay=")
		call_deferred("quit", 1)
		return
	var server := TrainingEnvServerScript.new()
	var started: int = Time.get_ticks_msec()
	var verify: Dictionary = server.handle_request({"op": "verify_replay", "path": replay_path})
	var elapsed: int = Time.get_ticks_msec() - started
	print("VERIFY DEBUG elapsed_ms=%d ok=%s" % [elapsed, str(verify.get("ok", false))])
	print("EXIT: 0 (pass)" if verify.get("ok", false) else "EXIT: 1 (fail)")
	call_deferred("quit", 0 if verify.get("ok", false) else 1)
