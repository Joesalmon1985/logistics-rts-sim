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
		"fog": true,
		"diagnostics": false,
		"logistics": false,
		"forest_density": false,
	}
	match mode:
		"world":
			layers["forest_density"] = true
		"economy":
			layers["logistics"] = true
			layers["forest_density"] = true
			layers["diagnostics"] = true
		"military":
			layers["fog"] = true
			layers["diagnostics"] = true
		"neural":
			layers["forest_density"] = true
			layers["logistics"] = true
		"debug":
			for key in layers.keys():
				layers[key] = true
	return layers
