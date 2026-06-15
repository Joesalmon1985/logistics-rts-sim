class_name MilitaryCommandRegistry
extends RefCounted


static func register_all(registry: CommandRegistry) -> void:
	_reg(registry, "PlaceBuildingCommand", func(p): return PlaceBuildingCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "CancelMilitaryConstructionCommand", func(p): return CancelMilitaryConstructionCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SellBuildingCommand", func(p): return SellBuildingCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "QueueUnitCommand", func(p): return QueueUnitCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "CancelProductionCommand", func(p): return CancelProductionCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "SetRallyPointCommand", func(p): return SetRallyPointCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "MoveUnitCommand", func(p): return MoveUnitCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "AttackUnitCommand", func(p): return AttackUnitCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "AttackMoveCommand", func(p): return AttackMoveCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "PatrolCommand", func(p): return PatrolCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "GuardCommand", func(p): return GuardCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "StopUnitCommand", func(p): return StopUnitCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "StartResearchCommand", func(p): return StartResearchCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "RepairTargetCommand", func(p): return RepairTargetCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "EnableMilitaryBuildingCommand", func(p): return EnableMilitaryBuildingCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "DisableMilitaryBuildingCommand", func(p): return DisableMilitaryBuildingCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))
	_reg(registry, "HarvestResourceCommand", func(p): return HarvestResourceCommand.new(str(p.get("player_id", "p1")), int(p.get("tick", 0)), p))


static func _reg(registry: CommandRegistry, type_name: String, factory: Callable) -> void:
	registry.register(type_name, factory)
