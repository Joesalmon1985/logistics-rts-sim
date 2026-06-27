class_name HybridCommandRegistry
extends RefCounted


static func register_all(registry: CommandRegistry) -> void:
	_reg(registry, "DelegatedAttackCommand", func(p): return DelegatedAttackCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SetBuildingStanceCommand", func(p): return SetBuildingStanceCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SetPatrolZoneCommand", func(p): return SetPatrolZoneCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SetGuardPolicyCommand", func(p): return SetGuardPolicyCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SetRepairPriorityCommand", func(p): return SetRepairPriorityCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "CancelDelegatedOrderCommand", func(p): return CancelDelegatedOrderCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))


static func _reg(registry: CommandRegistry, type_name: String, factory: Callable) -> void:
	registry.register(type_name, factory)
