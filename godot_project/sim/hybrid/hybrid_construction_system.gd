class_name HybridConstructionSystem
extends RefCounted

## Bridges economy construction sites to military building spawn (HYB-LOGCON-001).


static func spawn_from_completed_site(state: GameState, site: Dictionary) -> int:
	var building_type: String = str(site.get("building_type", ""))
	var anchor_q: int = int(site.get("anchor_q", 0))
	var anchor_r: int = int(site.get("anchor_r", 0))
	var player_id: String = str(site.get("player_id", "p1"))
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	var building_id: int = MilitaryConstructionSystem.spawn_building(
		state, building_type, anchor_q, anchor_r, player_id, cfg,
	)
	_link_economy_ports(state, site, building_id, player_id)
	HybridState.emit_event(state, "military_construction_completed", {
		"site_id": int(site.get("id", 0)),
		"building_id": building_id,
		"building_type": building_type,
		"player_id": player_id,
	})
	return building_id


static func register_hq_as_command_hub(state: GameState, player_id: String) -> int:
	var hq_ids: Dictionary = EconomyState.get_meta_value(state, "hq_building_ids", {})
	var hq_id: int = int(hq_ids.get(player_id, EconomyState.get_meta_value(state, "hq_building_id", 0)))
	var hq: Dictionary = EconomyState.get_building(state, hq_id)
	if hq.is_empty():
		return 0
	var cfg: MilitaryConfig = MilitaryState.get_config(state)
	return MilitaryConstructionSystem.spawn_building(
		state,
		"command_hub",
		int(hq.get("anchor_q", 0)),
		int(hq.get("anchor_r", 0)),
		player_id,
		cfg,
	)


static func _link_economy_ports(state: GameState, site: Dictionary, military_building_id: int, player_id: String) -> void:
	var econ_cfg: EconomyConfig = EconomyState.get_config(state)
	var building_type: String = str(site.get("building_type", ""))
	var port_data: Dictionary = EconomyRoadSystem.create_building_port_nodes(
		state,
		military_building_id,
		int(site.get("anchor_q", 0)),
		int(site.get("anchor_r", 0)),
		player_id,
		econ_cfg,
		building_type,
	)
	var mil_building: Dictionary = MilitaryState.get_building(state, military_building_id)
	mil_building["economy_road_node_id"] = int(port_data.get("road_node_id", 0))
	mil_building["economy_port_node_ids"] = port_data.get("port_node_ids", [])
	MilitaryState.add_building(state, mil_building)
