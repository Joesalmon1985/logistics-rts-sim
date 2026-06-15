extends RolloutObserverRoot

## RUN_02.5 neural rollout observer entry scene.


func _enter_tree() -> void:
	SimBootstrap.load_all()


func _ready() -> void:
	super._ready()
	var rollout_path: String = _parse_rollout_path()
	if rollout_path != "":
		var result: Dictionary = load_rollout_path(rollout_path)
		if not result.get("ok", false):
			push_error("Failed to load rollout: %s" % str(result.get("error", "?")))
	else:
		var fixture: String = "res://sim/tests/fixtures/visual_rollouts/minimal_economy"
		load_rollout_path(fixture)


func _parse_rollout_path() -> String:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--rollout-path="):
			return arg.substr("--rollout-path=".length())
	return ""
