class_name TutorialNavigationUI
extends RefCounted

## Applies the four-always-visible tutorial button rules to any MVP game scene.


static func refresh(
	tutorial: BaseTutorialController,
	panel: PanelContainer,
	title: Label,
	progress: Label,
	instruction: RichTextLabel,
	back_btn: Button,
	next_btn: Button,
	next_reason: Label,
	skip_btn: Button,
	exit_btn: Button,
	step_kind_label_node: Label = null
) -> void:
	if not tutorial.enabled or tutorial.exited or panel == null:
		if panel != null:
			panel.visible = false
		return
	var step: Dictionary = tutorial.current_step()
	panel.visible = not step.is_empty()
	if step.is_empty():
		return
	if title != null:
		title.text = str(step.get("title", "Tutorial"))
	if progress != null:
		progress.text = "Step %d of %d" % [tutorial.step_index + 1, tutorial.steps.size()]
	if instruction != null:
		instruction.clear()
		instruction.append_text(str(step.get("instruction", "")))
	if step_kind_label_node != null:
		step_kind_label_node.text = tutorial.step_kind_label(step)
		step_kind_label_node.visible = true
	_apply_nav_buttons(tutorial, step, back_btn, next_btn, next_reason, skip_btn, exit_btn)


static func _apply_nav_buttons(
	tutorial: BaseTutorialController,
	step: Dictionary,
	back_btn: Button,
	next_btn: Button,
	next_reason: Label,
	skip_btn: Button,
	exit_btn: Button
) -> void:
	for btn in [back_btn, next_btn, skip_btn, exit_btn]:
		if btn != null:
			btn.visible = true
	if back_btn != null:
		back_btn.disabled = not tutorial.back_button_enabled()
	if next_btn != null:
		var enabled: bool = tutorial.next_button_enabled(step)
		next_btn.disabled = not enabled
		next_btn.text = "Next"
	if skip_btn != null:
		skip_btn.disabled = false
		if skip_btn.text.is_empty():
			skip_btn.text = "Skip Tutorial"
	if exit_btn != null:
		exit_btn.disabled = false
		if exit_btn.text.is_empty():
			exit_btn.text = "Back to Main Menu"
	if next_reason != null:
		var reason: String = tutorial.next_disabled_reason(step)
		next_reason.text = reason
		next_reason.visible = not reason.is_empty()


static func probe_button_texts(scene: Node) -> Dictionary:
	return {
		"tutorial_title_text": _label_text(scene, "TutorialTitle"),
		"tutorial_instruction_text": _rt_text(scene, "TutorialInstruction"),
		"tutorial_back_button_text": _btn_text(scene, "TutorialBackBtn"),
		"tutorial_next_button_text": _btn_text(scene, "TutorialNextBtn"),
		"tutorial_skip_button_text": _btn_text(scene, "TutorialSkipBtn"),
		"tutorial_exit_button_text": _btn_text(scene, "TutorialExitBtn"),
		"tutorial_step_number_text": _label_text(scene, "TutorialProgress"),
		"tutorial_next_disabled_reason": _label_text(scene, "TutorialNextReasonLabel"),
	}


static func _label_text(scene: Node, name: String) -> String:
	var node: Label = scene.find_child(name, true, false) as Label
	return node.text if node != null else ""


static func _rt_text(scene: Node, name: String) -> String:
	var node: RichTextLabel = scene.find_child(name, true, false) as RichTextLabel
	return node.get_parsed_text() if node != null else ""


static func _btn_text(scene: Node, name: String) -> String:
	var node: Button = scene.find_child(name, true, false) as Button
	return node.text if node != null else ""
