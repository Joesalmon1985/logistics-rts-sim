class_name CommandRegistry
extends RefCounted

var _factories: Dictionary = {}


func register(command_type: String, factory: Callable) -> void:
	_factories[command_type] = factory


func create(command_type: String, payload: Dictionary = {}) -> SimCommand:
	if not _factories.has(command_type):
		return null
	return _factories[command_type].call(payload)


func has_type(command_type: String) -> bool:
	return _factories.has(command_type)


func registered_types() -> Array:
	var keys: Array = _factories.keys()
	keys.sort()
	return keys


static func create_default() -> CommandRegistry:
	var registry := CommandRegistry.new()
	registry.register("NoopCommand", func(payload: Dictionary) -> SimCommand:
		return NoopCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
		)
	)
	registry.register("InvalidTestCommand", func(payload: Dictionary) -> SimCommand:
		return InvalidTestCommand.new(
			str(payload.get("player_id", "p1")),
			int(payload.get("tick", 0)),
		)
	)
	EconomyCommandRegistry.register_all(registry)
	MilitaryCommandRegistry.register_all(registry)
	return registry
