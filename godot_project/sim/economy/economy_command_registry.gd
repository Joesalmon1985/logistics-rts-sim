class_name EconomyCommandRegistry
extends RefCounted


static func register_all(registry: CommandRegistry) -> void:
	registry.register("PlaceRoadCommand", func(payload: Dictionary) -> SimCommand:
		return PlaceRoadCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("RemoveRoadCommand", func(payload: Dictionary) -> SimCommand:
		return RemoveRoadCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("PlaceBuildingPlanCommand", func(payload: Dictionary) -> SimCommand:
		return PlaceBuildingPlanCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("CancelConstructionCommand", func(payload: Dictionary) -> SimCommand:
		return CancelConstructionCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("UpgradeBuildingCommand", func(payload: Dictionary) -> SimCommand:
		return UpgradeBuildingCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("EnableBuildingCommand", func(payload: Dictionary) -> SimCommand:
		return EnableBuildingCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("DisableBuildingCommand", func(payload: Dictionary) -> SimCommand:
		return DisableBuildingCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("SetConstructionPriorityCommand", func(payload: Dictionary) -> SimCommand:
		return SetConstructionPriorityCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("SetProductionPriorityCommand", func(payload: Dictionary) -> SimCommand:
		return SetProductionPriorityCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("SetWarePriorityCommand", func(payload: Dictionary) -> SimCommand:
		return SetWarePriorityCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("AssignWorkerCommand", func(payload: Dictionary) -> SimCommand:
		return AssignWorkerCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
	registry.register("SetWorkerPolicyCommand", func(payload: Dictionary) -> SimCommand:
		return SetWorkerPolicyCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
			payload,
		)
	)
