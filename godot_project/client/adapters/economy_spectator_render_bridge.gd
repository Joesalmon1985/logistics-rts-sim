class_name EconomySpectatorRenderBridge
extends RefCounted

## Viewer-only tick deltas and effect queue for spectator sprites.


var last_render_data: Dictionary = {}
var last_diagnostics: Dictionary = {}
var last_event_count: int = 0
var sprite_states: Dictionary = {}
var effect_events: Array = []


func reset() -> void:
	last_render_data = {}
	last_diagnostics = {}
	last_event_count = 0
	sprite_states = {}
	effect_events.clear()


func update(state: GameState, snapshot: Dictionary, layer_visibility: Dictionary) -> Dictionary:
	var render_data: Dictionary = LayerRenderer.build_render_data(snapshot, layer_visibility)
	var new_effects: Array = _collect_effect_events(state)
	effect_events = new_effects
	sprite_states = EconomySpriteStateSelector.select_states(
		render_data,
		snapshot,
		last_diagnostics,
		effect_events,
	)
	var delta: Dictionary = compute_delta(last_render_data, render_data)
	last_render_data = render_data.duplicate(true)
	last_diagnostics = snapshot.get("diagnostics", {}).duplicate(true) if typeof(snapshot.get("diagnostics", {})) == TYPE_DICTIONARY else {}
	if state != null:
		last_event_count = state.event_log.events.size()
	return {
		"render_data": render_data,
		"sprite_states": sprite_states,
		"delta": delta,
		"effect_events": effect_events,
	}


static func compute_delta(before: Dictionary, after: Dictionary) -> Dictionary:
	var counts_before: Dictionary = LayerRenderer.count_entities(before) if not before.is_empty() else {}
	var counts_after: Dictionary = LayerRenderer.count_entities(after)
	var changed: int = 0
	for key in counts_after.keys():
		if int(counts_after.get(key, 0)) != int(counts_before.get(key, 0)):
			changed += 1
	return {
		"counts_before": counts_before,
		"counts_after": counts_after,
		"categories_changed": changed,
		"sprite_state_count": 0,
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
