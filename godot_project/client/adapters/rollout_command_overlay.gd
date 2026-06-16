class_name RolloutCommandOverlay
extends RefCounted

## Read-only command overlay descriptors for rollout observer map (non-authoritative).

const COLOR_PREVIOUS := Color(0.35, 0.85, 1.0, 0.95)
const COLOR_CURRENT := Color(1.0, 0.35, 0.85, 0.95)
const COLOR_COMBAT := Color(1.0, 0.45, 0.2, 0.9)
const COLOR_REJECTED := Color(0.95, 0.25, 0.25, 0.95)


static func build_command_overlays(
	prev: Dictionary,
	cur: Dictionary,
	show_previous: bool,
	show_current: bool,
) -> Array:
	var overlays: Array = []
	if show_previous and not prev.is_empty():
		_append_command_overlay(overlays, prev, "previous")
	if show_current and not cur.is_empty():
		_append_command_overlay(overlays, cur, "current")
	return overlays


static func _append_command_overlay(overlays: Array, cmd: Dictionary, role: String) -> void:
	var target: Variant = cmd.get("command_target")
	if typeof(target) != TYPE_DICTIONARY:
		return
	var t: Dictionary = target
	var ctype: String = str(cmd.get("command_type", ""))
	var accepted: bool = bool(cmd.get("was_accepted", true))
	var label: String = str(cmd.get("command_label", ctype))
	var color: Color = COLOR_PREVIOUS if role == "previous" else COLOR_CURRENT
	if not accepted:
		color = COLOR_REJECTED

	if ctype == "PlaceRoadCommand":
		var fq: int = int(t.get("from_q", -1))
		var fr: int = int(t.get("from_r", -1))
		var tq: int = int(t.get("to_q", -1))
		var tr: int = int(t.get("to_r", -1))
		if fq >= 0 and tq >= 0:
			overlays.append(_line_overlay(fq, fr, tq, tr, role, color, label, accepted))
			overlays.append(_ring_overlay(fq, fr, role, color, "SRC", accepted))
			overlays.append(_ring_overlay(tq, tr, role, color, "TGT", accepted))
		return

	if t.has("q") and t.has("r"):
		var q: int = int(t.get("q", -1))
		var r: int = int(t.get("r", -1))
		if q >= 0 and r >= 0:
			var tag: String = "TGT"
			if ctype.find("Building") >= 0:
				tag = str(t.get("building_type", "BLD")).substr(0, 4).to_upper()
			overlays.append(_ring_overlay(q, r, role, color, tag, accepted))
			if t.has("from_q") and t.has("from_r"):
				var sq: int = int(t.get("from_q", -1))
				var sr: int = int(t.get("from_r", -1))
				if sq >= 0:
					overlays.append(_line_overlay(sq, sr, q, r, role, color, label, accepted))
					overlays.append(_ring_overlay(sq, sr, role, color, "SRC", accepted))


static func _ring_overlay(q: int, r: int, role: String, color: Color, tag: String, accepted: bool) -> Dictionary:
	return {
		"kind": "ring",
		"q": q,
		"r": r,
		"role": role,
		"color": color,
		"label": tag,
		"accepted": accepted,
	}


static func _line_overlay(
	from_q: int,
	from_r: int,
	to_q: int,
	to_r: int,
	role: String,
	color: Color,
	label: String,
	accepted: bool,
) -> Dictionary:
	return {
		"kind": "line",
		"from_q": from_q,
		"from_r": from_r,
		"to_q": to_q,
		"to_r": to_r,
		"role": role,
		"color": color,
		"label": label,
		"accepted": accepted,
	}


static func focus_hexes_from_overlays(overlays: Array) -> Array:
	var hexes: Array = []
	for overlay in overlays:
		if str(overlay.get("kind", "")) == "line":
			hexes.append({"q": int(overlay.get("from_q", -1)), "r": int(overlay.get("from_r", -1))})
			hexes.append({"q": int(overlay.get("to_q", -1)), "r": int(overlay.get("to_r", -1))})
		elif str(overlay.get("kind", "")) == "ring":
			hexes.append({"q": int(overlay.get("q", -1)), "r": int(overlay.get("r", -1))})
	return _dedupe_hexes(hexes)


static func military_combat_overlays(snapshot: Dictionary) -> Array:
	var overlays: Array = []
	var by_id: Dictionary = {}
	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		by_id[int(unit.get("id", 0))] = unit
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		by_id[int(building.get("id", 0))] = building

	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		var target_id: int = int(unit.get("engage_target_id", 0))
		if target_id <= 0:
			continue
		var target: Dictionary = by_id.get(target_id, {})
		if target.is_empty():
			continue
		var uq: int = int(unit.get("q", -1))
		var ur: int = int(unit.get("r", -1))
		var tq: int = int(target.get("q", target.get("anchor_q", -1)))
		var tr: int = int(target.get("r", target.get("anchor_r", -1)))
		if uq < 0 or tq < 0:
			continue
		var unit_type: String = str(unit.get("unit_type", "unit")).substr(0, 4).to_upper()
		overlays.append(_line_overlay(uq, ur, tq, tr, "combat", COLOR_COMBAT, unit_type, true))
		overlays.append(_ring_overlay(tq, tr, "combat", COLOR_COMBAT, "ATK", true))
	return overlays


static func military_focus_hexes(snapshot: Dictionary) -> Array:
	var hexes: Array = []
	for building in snapshot.get("buildings", []):
		if typeof(building) != TYPE_DICTIONARY:
			continue
		if str(building.get("player_id", "")) == "p2":
			hexes.append({
				"q": int(building.get("anchor_q", building.get("q", -1))),
				"r": int(building.get("anchor_r", building.get("r", -1))),
			})
	for unit in snapshot.get("units", []):
		if typeof(unit) != TYPE_DICTIONARY:
			continue
		hexes.append({"q": int(unit.get("q", -1)), "r": int(unit.get("r", -1))})
	hexes.append_array(focus_hexes_from_overlays(military_combat_overlays(snapshot)))
	return _dedupe_hexes(hexes)


static func economy_badges(snapshot: Dictionary) -> Array:
	var badges: Array = []
	for site in snapshot.get("construction_sites", []):
		if typeof(site) != TYPE_DICTIONARY:
			continue
		var state: String = str(site.get("state", ""))
		var q: int = int(site.get("anchor_q", site.get("q", -1)))
		var r: int = int(site.get("anchor_r", site.get("r", -1)))
		if q < 0:
			continue
		if state == "blocked":
			badges.append({"q": q, "r": r, "text": "BLOCKED", "color": Color(0.95, 0.3, 0.3)})
		elif state == "active":
			var progress: int = LayerRenderer.site_progress_percent(site)
			badges.append({"q": q, "r": r, "text": "%d%%" % progress, "color": Color(0.45, 0.95, 0.45)})
	for job in snapshot.get("blocked_job_markers", []):
		if typeof(job) != TYPE_DICTIONARY:
			continue
		var jq: int = int(job.get("from_q", job.get("q", -1)))
		var jr: int = int(job.get("from_r", job.get("r", -1)))
		if jq < 0:
			continue
		var status: String = str(job.get("status", ""))
		var tag: String = "NO RT" if status == "waiting_for_route" else "BLOCK"
		badges.append({"q": jq, "r": jr, "text": tag, "color": Color(0.95, 0.65, 0.2)})
	return badges


static func overlay_manifest_entry(prev: Dictionary, cur: Dictionary, overlays: Array) -> Dictionary:
	return {
		"previous_command": prev.get("command_label", null),
		"current_command": cur.get("command_label", null),
		"previous_target": prev.get("command_target"),
		"current_target": cur.get("command_target"),
		"overlay_count": overlays.size(),
		"overlay_available": not overlays.is_empty(),
	}


static func _dedupe_hexes(hexes: Array) -> Array:
	var seen: Dictionary = {}
	var out: Array = []
	for hex in hexes:
		var q: int = int(hex.get("q", -1))
		var r: int = int(hex.get("r", -1))
		if q < 0:
			continue
		var key: String = "%d,%d" % [q, r]
		if seen.has(key):
			continue
		seen[key] = true
		out.append({"q": q, "r": r})
	return out
