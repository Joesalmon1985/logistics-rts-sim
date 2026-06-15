import numpy as np

from python_tools.training import observation


def test_economy_encoder_stable():
    snapshot = {
        "variant": "economy_trial",
        "tick": 3,
        "seed": 42,
        "buildings": [{"state": "active", "building_type": "hq"}],
        "construction_sites": [],
        "carrier_markers": [{}],
        "blocked_job_markers": [],
        "roads": [{}, {}],
        "display_layers": {"tree_density": [1], "resources": [1, 2]},
        "diagnostics": {"buildings": [{"id": "b1"}]},
        "players": [{"stores": {"timber": 4}}],
    }
    first = observation.encode_economy(snapshot)
    second = observation.encode_economy(snapshot)
    assert np.array_equal(first, second)
    assert first.shape == (len(observation.ECONOMY_FEATURE_NAMES),)


def test_military_encoder_stable():
    snapshot = {
        "variant": "military_trial",
        "tick": 8,
        "seed": 7,
        "units": [{"owner": "p2", "visibility": "visible"}],
        "buildings": [{}],
        "combat_log": [{"a": 1}],
        "construction_sites": [],
        "diagnostics": {"military": {"credits": 120, "power_state": "ok", "queued_production": 2, "victory_decided": 0}},
    }
    vec = observation.encode_military(snapshot)
    assert vec.shape == (len(observation.MILITARY_FEATURE_NAMES),)
