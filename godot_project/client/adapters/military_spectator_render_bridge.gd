class_name MilitarySpectatorRenderBridge
extends RefCounted

## Viewer-only render bridge for military spectator sprites and effect markers.


var last_render_data: Dictionary = {}
var sprite_states: Dictionary = {}
var effect_events: Array = []
var last_event_count: int = 0


func reset() -> void:
	last_render_data = {}
	sprite_states = {}
	effect_events.clear()
	last_event_count = 0


func update(state: GameState, snapshot: Dictionary, layer_visibility: Dictionary) -> Dictionary:
	var render_data: Dictionary = LayerRenderer.build_render_data(snapshot, layer_visibility)
	effect_events = _collect_effect_events(state)
	sprite_states = MilitarySpriteStateSelector.cap_states(
		MilitarySpriteStateSelector.select_states(render_data, snapshot, effect_events)
	)
	last_render_data = render_data.duplicate(true)
	if state != null:
		last_event_count = state.event_log.events.size()
	return {
		"render_data": render_data,
		"sprite_states": sprite_states,
		"effect_events": effect_events,
	}


func _collect_effect_events(state: GameState) -> Array:
	if state == null:
		return []
	var collected: Array = []
	for i in range(last_event_count, state.event_log.events.size()):
		var evt: SimEvent = state.event_log.events[i]
		collected.append({
			"type": str(evt.type),
			"tick": state.tick,
			"payload": evt.payload if typeof(evt.payload) == TYPE_DICTIONARY else {},
		})
	return collected
