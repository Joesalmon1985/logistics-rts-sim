extends SceneTree

## Mobile hex-view QA runner (PRD C.11).


func _initialize() -> void:
	SimBootstrap.load_all()
	var failures: Array[String] = []
	if not _check_profiles():
		failures.append("mobile_layout_profiles")
	if not _check_menu_route():
		failures.append("main_menu_mobile")
	if failures.is_empty():
		print("MOBILE HEX QA PASS")
		quit(0)
	print("MOBILE HEX QA FAIL: %s" % ", ".join(failures))
	quit(1)


func _check_profiles() -> bool:
	return MobileLayoutProfiles.profile_names().size() >= 8


func _check_menu_route() -> bool:
	var host := Node.new()
	root.add_child(host)
	var ctx: Dictionary = ViewerTestHarness.mount(host, "main_menu")
	var ok: bool = bool(ctx.get("ok", false))
	ViewerTestHarness.teardown(ctx)
	host.queue_free()
	return ok
