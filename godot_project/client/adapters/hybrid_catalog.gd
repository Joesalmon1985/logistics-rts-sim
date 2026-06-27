class_name HybridCatalog
extends RefCounted

## Display-only hybrid metadata — economy + military plain English (no sim rules).


static func display_name(sim_type: String) -> String:
	if HybridConfig.load_all().is_military_building(sim_type):
		return MilitaryCatalog.display_name(sim_type)
	return BuildingCatalog.display_name(sim_type)


static func role_text(sim_type: String) -> String:
	if HybridConfig.load_all().is_military_building(sim_type):
		return MilitaryCatalog.role_text(sim_type)
	return BuildingCatalog.role_text(sim_type)


static func format_selection(selection: Dictionary, snapshot: Dictionary) -> String:
	var kind: String = str(selection.get("kind", "empty"))
	var data: Dictionary = selection.get("data", {}) if typeof(selection.get("data", {})) == TYPE_DICTIONARY else {}
	match kind:
		"building":
			var btype: String = str(data.get("building_type", ""))
			if HybridConfig.load_all().is_military_building(btype):
				return MilitaryCatalog.format_selection(selection, snapshot)
			return SelectionFormatter.format(selection, snapshot)
		"construction_site":
			var site_type: String = str(data.get("building_type", ""))
			if HybridConfig.load_all().is_military_building(site_type):
				return _format_military_site(data, snapshot)
			return SelectionFormatter.format(selection, snapshot)
		"unit":
			return MilitaryCatalog.format_selection(selection, snapshot)
		"carrier", "road", "road_node", "terrain":
			return SelectionFormatter.format(selection, snapshot)
		_:
			return "[i]Click a hex to inspect settlement or military objects.[/i]"


static func format_objective(objective: Dictionary) -> String:
	if objective.is_empty():
		return "Hybrid objective: build economy-fed military force and defeat the enemy."
	var lines: PackedStringArray = PackedStringArray(["[b]Hybrid objective[/b]"])
	for req in objective.get("requirements", []):
		if typeof(req) != TYPE_DICTIONARY:
			continue
		var mark: String = "✓" if bool(req.get("done", false)) else "○"
		var detail: String = str(req.get("detail", ""))
		var suffix: String = (" — %s" % detail) if detail != "" else ""
		lines.append("%s %s%s" % [mark, str(req.get("label", "?")), suffix])
	if bool(objective.get("complete", false)):
		lines.append("[color=#55cc66][b]Objective complete![/b][/color]")
	return "\n".join(lines)


static func format_supply_panel(snapshot: Dictionary, player_id: String = "p1") -> String:
	var stock: Dictionary = {}
	for player in snapshot.get("players", []):
		if typeof(player) != TYPE_DICTIONARY:
			continue
		if str(player.get("player_id", "")) != player_id:
			continue
		var wares: Dictionary = player.get("wares", {})
		if typeof(wares) == TYPE_DICTIONARY:
			stock = wares
		break
	var parts: PackedStringArray = PackedStringArray()
	for ware in ["planks", "stone", "tools", "rations", "iron_ingots"]:
		if stock.has(ware):
			parts.append("%s=%d" % [ware, int(stock.get(ware, 0))])
	var supply: int = 0
	var demand: int = 0
	for mp in snapshot.get("military_players", []):
		if str(mp.get("player_id", "")) == player_id:
			supply = int(mp.get("power_supply", 0))
			demand = int(mp.get("power_demand", 0))
			break
	return "Stockpile: %s | Power: %d / %d" % [
		", ".join(parts) if not parts.is_empty() else "(empty)",
		supply,
		demand,
	]


static func format_production_queues(snapshot: Dictionary, player_id: String = "p1") -> String:
	var lines: PackedStringArray = PackedStringArray(["[b]Military production[/b]"])
	var found: bool = false
	for queue in snapshot.get("production_queues", []):
		if typeof(queue) != TYPE_DICTIONARY:
			continue
		if str(queue.get("player_id", "")) != player_id:
			continue
		found = true
		var building_id: int = int(queue.get("building_id", 0))
		var btype: String = "?"
		for building in snapshot.get("buildings", []):
			if int(building.get("id", 0)) == building_id:
				btype = str(building.get("building_type", "?"))
				break
		for item in queue.get("items", []):
			if typeof(item) != TYPE_DICTIONARY:
				continue
			var utype: String = str(item.get("unit_type", "?"))
			var blocked: String = str(item.get("blocked_reason", ""))
			var suffix: String = (" [%s]" % MilitaryCatalog.human_reason(blocked)) if blocked != "" else ""
			lines.append("- %s: %s%s" % [display_name(btype), display_name(utype), suffix])
	if not found:
		lines.append("- (idle — queue units at Barracks or Guard Post)")
	return "\n".join(lines)


static func format_delegated_orders(snapshot: Dictionary, player_id: String = "p1") -> String:
	var orders: Array = snapshot.get("delegated_orders", [])
	if orders.is_empty():
		return "Attack posture: none — click an enemy hex while a unit order panel is active."
	var lines: PackedStringArray = PackedStringArray(["[b]Delegated orders[/b]"])
	for order in orders:
		if typeof(order) != TYPE_DICTIONARY:
			continue
		if str(order.get("player_id", "")) != player_id:
			continue
		if int(order.get("active", 0)) == 0:
			continue
		lines.append("- %s target (%d,%d)" % [
			str(order.get("type", "order")),
			int(order.get("target_q", -1)),
			int(order.get("target_r", -1)),
		])
	return "\n".join(lines)


static func _format_military_site(site: Dictionary, snapshot: Dictionary) -> String:
	var btype: String = str(site.get("building_type", ""))
	var progress: int = int(site.get("progress_percent", 0))
	var missing: String = str(site.get("blocked_reason", site.get("waiting_for", "")))
	var lines: PackedStringArray = PackedStringArray([
		"[b]%s — construction site[/b]" % display_name(btype),
		role_text(btype),
		"Progress: %d%%" % progress,
	])
	if missing != "":
		lines.append("[color=#ffaa55]Waiting: %s[/color]" % missing)
	else:
		lines.append("Carriers deliver planks, stone and tools by road.")
	lines.append("[i]Tip: connect roads from quarry and sawmill to this site.[/i]")
	return "\n".join(lines)
