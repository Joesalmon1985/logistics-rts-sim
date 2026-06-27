class_name Hybrid3dSpectatorHud
extends RefCounted

## HUD text builders for hybrid 3D spectator panels.


static func bind_labels(root: Node, controller: Hybrid3dSpectatorController) -> void:
	if root == null or controller == null:
		return
	root.set_meta("hybrid_3d_controller", controller)


static func refresh(root: Node, controller: Hybrid3dSpectatorController) -> void:
	if root == null or controller == null:
		return
	_set_label(root, "%StatusLabel", controller.build_transport_bar_text())
	_set_label(root, "%BotASummary", controller.build_bot_summary("p1"))
	_set_label(root, "%BotBSummary", controller.build_bot_summary("p2"))
	_set_label(root, "%ObjectiveLabel", controller.build_objective_text())
	_set_label(root, "%CameraModeLabel", "Camera: %s | Follow: %s" % [controller.camera_mode, controller.follow_target])
	_set_richtext(root, "%Inspector", Hybrid3dInspectorPanel.format_selection(controller.get_selection(), controller.snapshot))
	_set_richtext(root, "%RecentEvents", controller.build_recent_events_text())
	_set_richtext(root, "%EventLog", controller.build_event_log_text())
	_set_label(root, "%MatchResultLabel", str(controller.match_result.get("summary", "Match in progress")))


static func _set_label(root: Node, path: String, text: String) -> void:
	var node: Label = root.get_node_or_null(path) as Label
	if node != null:
		node.text = text


static func _set_richtext(root: Node, path: String, text: String) -> void:
	var node: RichTextLabel = root.get_node_or_null(path) as RichTextLabel
	if node != null:
		node.text = text
