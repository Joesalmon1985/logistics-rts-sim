class_name HybridBotSpectatorController
extends RefCounted

## 2D four-player hybrid bot spectator transport (PRD Part B).

signal snapshot_refreshed(snapshot: Dictionary)

const SCENARIO_PATH := "scenarios/hybrid/hybrid_4p_training_ffa.json"
const BOT_PLAYERS: Array[String] = ["p1", "p2", "p3", "p4"]
const PLAYER_COLORS: Dictionary = {
	"p1": Color(0.2, 0.45, 0.95),
	"p2": Color(0.9, 0.25, 0.2),
	"p3": Color(0.2, 0.75, 0.35),
	"p4": Color(0.95, 0.75, 0.15),
}

var state: GameState
var runner: SimulationTickRunner
var quad: QuadBotRunner
var scenario: Dictionary = {}
var snapshot: Dictionary = {}
var match_label: String = "Hybrid 4P FFA spectator"
var selected_q: int = -1
var selected_r: int = -1
var autoplay_enabled: bool = true
var paused: bool = false
var speed_multiplier: float = 1.0
var follow_player: String = ""
var bot_status: Dictionary = {}
var ffa_result: Dictionary = {}
var event_log_lines: PackedStringArray = PackedStringArray()


func reset_match() -> void:
	reset_match_trained_neural()


func reset_match_trained_neural() -> void:
	runner = SimulationTickRunner.new()
	quad = QuadBotRunner.new(runner)
	event_log_lines = PackedStringArray()
	bot_status = {}
	ffa_result = {}
	selected_q = -1
	selected_r = -1
	SimBootstrap.load_all()
	scenario = ConfigLoader.load_scenario(SCENARIO_PATH)
	match_label = "Hybrid 4P FFA trained neural (seed %d)" % int(scenario.get("seed", 0))
	state = runner.create_game(scenario)
	_prepare_map()
	for pid in BOT_PLAYERS:
		var bot := ModularNeuralHybridBot.new(pid)
		bot.use_trained_checkpoint = true
		bot.use_heuristic_teacher = false
		quad.set_bot(pid, bot)
		bot_status[pid] = {"intent": "loading checkpoint", "telemetry": {}}
	for _i in range(8):
		if not paused:
			step_tick()
	_refresh("Trained neural match reset")


func _prepare_map() -> void:
	if state == null or state.map == null:
		return
	for spawn in scenario.get("spawns", []):
		var hq_q: int = int(spawn.get("q", 0))
		var hq_r: int = int(spawn.get("r", 0))
		HybridMapSetup.prepare_playable_map(state, 0, 0, state.map.width - 1, state.map.height - 1, hq_q, hq_r)


func step_tick() -> void:
	if state == null or quad == null:
		return
	EconomyDiagnosticsSystem.tick(state, "spectator")
	for pid in BOT_PLAYERS:
		var bot: Bot = quad.bots.get(pid)
		if bot == null:
			continue
		var snap: Dictionary = SnapshotExporter.export_bot_observation_snapshot(state, pid)
		bot.observe(snap, pid)
		var cmd: SimCommand = bot.decide(state.tick)
		if cmd != null and cmd.type != "NoopCommand":
			runner.command_validator.validate_and_apply(state, cmd)
	state = runner.tick(state)
	ffa_result = HybridFfaObjective.evaluate(state)
	for pid in BOT_PLAYERS:
		var mb: ModularNeuralHybridBot = quad.bots.get(pid) as ModularNeuralHybridBot
		if mb != null:
			bot_status[pid] = {
				"intent": mb.get_bot_intent_summary(),
				"telemetry": mb.get_last_telemetry(),
				"inference_mode": str(mb.get_last_telemetry().get("inference_mode", "")),
			}
	_refresh("Tick %d" % state.tick)


func step_ticks(count: int) -> void:
	for _i in range(maxi(0, count)):
		step_tick()


func pan_camera(_delta: Vector2) -> void:
	pass


func set_zoom(_factor: float) -> void:
	pass


func select_hex(q: int, r: int) -> void:
	selected_q = q
	selected_r = r


func get_inspector_text() -> String:
	if selected_q < 0:
		return "Click a building, unit, or hex for details."
	for b in snapshot.get("buildings", []):
		if int(b.get("anchor_q", b.get("q", -1))) == selected_q and int(b.get("anchor_r", b.get("r", -1))) == selected_r:
			return "Player %s %s at (%d,%d)" % [b.get("player_id", "?"), b.get("building_type", "building"), selected_q, selected_r]
	for u in snapshot.get("units", []):
		if int(u.get("q", -1)) == selected_q and int(u.get("r", -1)) == selected_r:
			return "Player %s %s" % [u.get("player_id", "?"), u.get("unit_type", "unit")]
	return "Terrain hex (%d,%d)" % [selected_q, selected_r]


func get_scoreboard_lines() -> PackedStringArray:
	var lines := PackedStringArray()
	lines.append("Match: %s | Tick %d" % [match_label, int(snapshot.get("tick", 0))])
	for pid in BOT_PLAYERS:
		var status: Dictionary = bot_status.get(pid, {})
		var alive: bool = pid in ffa_result.get("alive_players", BOT_PLAYERS)
		lines.append("P%s %s | %s" % [pid.substr(1), "alive" if alive else "out", str(status.get("intent", ""))])
	if bool(ffa_result.get("match_over", false)):
		lines.append("Winner: %s" % str(ffa_result.get("winner_player_id", "none")))
	return lines


func _refresh(_reason: String) -> void:
	if state == null:
		return
	snapshot = HybridSnapshotExporter.export_snapshot(state)
	snapshot_refreshed.emit(snapshot)
