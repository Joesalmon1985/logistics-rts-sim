class_name PassiveBot
extends Bot


func _init(p_player_id: String = "p1") -> void:
	player_id = p_player_id


func decide(tick: int) -> SimCommand:
	return NoopCommand.new(player_id, tick)
