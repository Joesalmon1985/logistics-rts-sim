class_name SimBootstrap
extends RefCounted

## Forces load order so global class_name types are registered before tests run.

const CORE_SCRIPTS: Array[String] = [
	"res://sim/core/fixed_point.gd",
	"res://sim/core/seeded_rng.gd",
	"res://sim/core/entity_id.gd",
	"res://sim/core/hex_coord.gd",
	"res://sim/core/hex_tile.gd",
	"res://sim/core/hex_map.gd",
	"res://sim/core/canonical_json.gd",
	"res://sim/core/replay_hash.gd",
	"res://sim/core/map_generator.gd",
	"res://sim/core/player_state.gd",
	"res://sim/core/event.gd",
	"res://sim/core/event_log.gd",
	"res://sim/core/command_result.gd",
	"res://sim/core/command.gd",
	"res://sim/core/noop_command.gd",
	"res://sim/core/invalid_test_command.gd",
	"res://sim/core/command_registry.gd",
	"res://sim/core/command_validator.gd",
	"res://sim/core/game_state.gd",
	"res://sim/core/snapshot_exporter.gd",
	"res://sim/core/config_loader.gd",
	"res://sim/economy/economy_constants.gd",
	"res://sim/economy/economy_config.gd",
	"res://sim/economy/economy_state.gd",
	"res://sim/economy/economy_map_rules.gd",
	"res://sim/economy/economy_pathfinding.gd",
	"res://sim/economy/economy_road_system.gd",
	"res://sim/economy/economy_logistics_system.gd",
	"res://sim/economy/economy_worker_system.gd",
	"res://sim/economy/economy_production_system.gd",
	"res://sim/economy/economy_construction_system.gd",
	"res://sim/economy/economy_upgrade_system.gd",
	"res://sim/economy/economy_priority_system.gd",
	"res://sim/economy/economy_diagnostics_system.gd",
	"res://sim/economy/economy_init.gd",
	"res://sim/economy/economy_tick.gd",
	"res://sim/economy/economy_command_registry.gd",
	"res://sim/core/variant_module.gd",
	"res://sim/variants/variant_stub_base.gd",
	"res://sim/variants/economy_trial/economy_trial_variant.gd",
	"res://sim/variants/military_trial/military_trial_variant.gd",
	"res://sim/variants/hybrid/hybrid_variant.gd",
	"res://sim/core/variant_registry.gd",
	"res://sim/core/simulation_tick_runner.gd",
]


static func load_all() -> void:
	for path in CORE_SCRIPTS:
		var script: Script = load(path)
		if script == null:
			push_error("SimBootstrap: failed to load %s" % path)
