extends SceneTree

## RUN_06 persistent headless training env — JSON-lines on stdin/stdout or file session (DDR-033).

const BootstrapScript := preload("res://sim/core/sim_bootstrap.gd")
const TrainingEnvServerScript := preload("res://sim/tools/training_env_server.gd")

var _server: RefCounted
var _running: bool = true
var _transport: String = "stdio"
var _session_dir: String = ""
var _stdin_thread: Thread
var _current_response_id: int = 0


func _initialize() -> void:
	BootstrapScript.load_all()
	_server = TrainingEnvServerScript.new()
	_parse_args()
	var args: PackedStringArray = OS.get_cmdline_user_args()
	if args.has("--self-test"):
		_run_self_test()
		return
	if _transport == "file":
		DirAccess.make_dir_recursive_absolute(_session_dir)
		call_deferred("_poll_file_transport")
	else:
		_stdin_thread = Thread.new()
		_stdin_thread.start(_stdio_loop)


func _parse_args() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--transport="):
			_transport = arg.substr("--transport=".length())
		elif arg.begins_with("--session-dir="):
			_session_dir = arg.substr("--session-dir=".length())


func _stdio_loop() -> void:
	while _running:
		var line: String = OS.read_string_from_stdin().strip_edges()
		if line == "":
			if not _running:
				break
			continue
		call_deferred("_handle_line", line)


func _poll_file_transport() -> void:
	while _running:
		var dir := DirAccess.open(_session_dir)
		if dir != null:
			dir.list_dir_begin()
			var file_name := dir.get_next()
			while file_name != "":
				if file_name.begins_with("request_") and file_name.ends_with(".json"):
					var request_path: String = _session_dir.path_join(file_name)
					var file := FileAccess.open(request_path, FileAccess.READ)
					if file != null:
						var line: String = file.get_as_text().strip_edges()
						file.close()
						if line != "":
							_handle_line(line)
						DirAccess.remove_absolute(request_path)
				file_name = dir.get_next()
			dir.list_dir_end()
		await create_timer(0.01).timeout


func _handle_line(line: String) -> void:
	var parsed: Variant = JSON.parse_string(line)
	if typeof(parsed) != TYPE_DICTIONARY:
		_emit_response({"ok": false, "error": "invalid_json"})
		return

	var req: Dictionary = parsed as Dictionary
	_current_response_id = int(req.get("response_id", 0))
	var op: String = str(req.get("op", ""))
	var response: Dictionary = _server.handle_request(req)
	_emit_response(response)
	if op == "shutdown":
		_running = false
		if _stdin_thread != null and _stdin_thread.is_started():
			_stdin_thread.wait_to_finish()
		call_deferred("quit", 0)


func _emit_response(payload: Dictionary) -> void:
	var formatted: String = TrainingEnvServerScript.format_response(payload)
	if _transport == "file":
		var suffix: String = "response.json"
		if _current_response_id > 0:
			suffix = "response_%d.json" % _current_response_id
		var response_path: String = _session_dir.path_join(suffix)
		SimPaths.write_text_file(response_path, formatted + "\n")
	else:
		print(formatted)


func _run_self_test() -> void:
	var server := TrainingEnvServerScript.new()
	var reset: Dictionary = server.handle_request({
		"op": "reset",
		"scenario_path": "scenarios/economy/tiny_success_map.json",
		"seed": 42,
		"actor_id": "p1",
	})
	if not reset.get("ok", false):
		print("TRAINING ENV SELF-TEST FAIL: reset")
		call_deferred("quit", 1)
		return
	var snap: Dictionary = server.handle_request({"op": "snapshot"})
	if not snap.get("ok", false):
		print("TRAINING ENV SELF-TEST FAIL: snapshot")
		call_deferred("quit", 1)
		return
	var formatted: String = TrainingEnvServerScript.format_response({"ok": true})
	if not formatted.begins_with("@@RUN06_JSON@@"):
		print("TRAINING ENV SELF-TEST FAIL: sentinel")
		call_deferred("quit", 1)
		return
	print("TRAINING ENV SELF-TEST PASS")
	print("EXIT: 0 (pass)")
	call_deferred("quit", 0)


func _finalize() -> void:
	_running = false
	if _stdin_thread != null and _stdin_thread.is_started():
		_stdin_thread.wait_to_finish()
