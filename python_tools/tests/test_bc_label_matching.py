from python_tools.training import command_match


def test_command_round_trip_index():
    available = [
        {
            "type": "place_road",
            "actor_id": "p1",
            "payload": {"player_id": "p1", "from_q": 1, "from_r": 2, "to_q": 2, "to_r": 2, "tick": 99},
            "target": {"from_q": 1, "from_r": 2, "to_q": 2, "to_r": 2},
        }
    ]
    command = {
        "type": "place_road",
        "player_id": "p1",
        "tick": 0,
        "payload": {"player_id": "p1", "from_q": 1, "from_r": 2, "to_q": 2, "to_r": 2},
    }
    index = command_match.find_index(command, available)
    assert index == 0
    rebuilt = command_match.index_to_command(index, available, tick=5)
    assert rebuilt is not None
    assert command_match.find_index(rebuilt, available) == 0


def test_noop_label_when_command_null():
    available = [{"type": "noop", "actor_id": "p1", "payload": {"player_id": "p1"}}]
    assert command_match.find_index(None, available, "economy_trial") == 63


def test_unmatched_command_returns_none():
    available = [{"type": "a", "actor_id": "p1", "payload": {"player_id": "p1", "x": 1}}]
    command = {"type": "b", "player_id": "p1", "payload": {"x": 2}}
    assert command_match.find_index(command, available) is None


def test_tick_mismatch_does_not_break_match():
    available = [
        {
            "type": "queue_building",
            "actor_id": "p1",
            "payload": {"player_id": "p1", "building_type": "lumberjack", "q": 3, "r": 4, "tick": 50},
        }
    ]
    command = {
        "type": "queue_building",
        "player_id": "p1",
        "tick": 0,
        "payload": {"player_id": "p1", "building_type": "lumberjack", "q": 3, "r": 4},
    }
    assert command_match.find_index(command, available) == 0
