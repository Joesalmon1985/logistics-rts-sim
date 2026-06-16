class_name InterfaceModePresets
extends RefCounted

const MODES: Array[String] = ["world", "economy", "military", "neural", "debug"]


static func apply_mode(mode: String) -> Dictionary:
	var layers: Dictionary = {
		"terrain": true,
		"buildings": true,
		"units": true,
		"roads": true,
		"resources": true,
		"fog": false,
		"diagnostics": false,
		"logistics": false,
		"forest_density": false,
	}
	match mode:
		"world":
			layers["forest_density"] = true
			layers["fog"] = false
		"economy":
			layers["logistics"] = true
			layers["forest_density"] = true
			layers["diagnostics"] = true
			layers["units"] = false
		"military":
			layers["fog"] = true
			layers["diagnostics"] = true
			layers["logistics"] = false
			layers["forest_density"] = false
		"neural":
			layers["forest_density"] = false
			layers["logistics"] = false
			layers["fog"] = false
		"debug":
			for key in layers.keys():
				layers[key] = true
	return layers


static func apply_ui_flags(mode: String) -> Dictionary:
	var flags: Dictionary = {
		"show_previous_command_overlay": false,
		"show_current_command_overlay": false,
		"show_economy_badges": false,
		"show_military_combat_overlay": false,
		"show_raw_events": false,
		"show_coord_labels": false,
		"show_debug_json_panel": false,
		"mode_unavailable_reason": "",
	}
	match mode:
		"world":
			flags["mode_unavailable_reason"] = "general map view — command overlays hidden"
		"economy":
			flags["show_economy_badges"] = true
			flags["mode_unavailable_reason"] = ""
		"military":
			flags["show_military_combat_overlay"] = true
			flags["mode_unavailable_reason"] = ""
		"neural":
			flags["show_previous_command_overlay"] = true
			flags["show_current_command_overlay"] = true
			flags["mode_unavailable_reason"] = ""
		"debug":
			flags["show_previous_command_overlay"] = true
			flags["show_current_command_overlay"] = true
			flags["show_economy_badges"] = true
			flags["show_military_combat_overlay"] = true
			flags["show_raw_events"] = true
			flags["show_coord_labels"] = true
			flags["show_debug_json_panel"] = true
	return flags
