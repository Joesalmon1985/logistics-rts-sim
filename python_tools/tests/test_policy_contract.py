"""RUN_06b policy contract tests."""

from __future__ import annotations

import numpy as np

from python_tools.training import action_encoder, eval_config, observation_encoder
from python_tools.training.scoring_policy import (
    NeuralRandomWeightsPolicy,
    PassOnlyScoringPolicy,
    RandomLegalScoringPolicy,
)


def test_observation_encoder_fixed_size():
    obs = {
        "schema_version": eval_config.SCHEMA_VERSION,
        "map_width": 16,
        "map_height": 16,
        "board_cells": [
            {
                "q": 0,
                "r": 0,
                "terrain_type": 1,
                "owner_relation": 1,
                "visibility": 1,
                "resource_amount": 0,
                "road_present": 0,
                "has_building": 0,
                "building_type": 0,
                "building_hp_progress": 0,
                "has_unit": 0,
                "unit_type": 0,
                "unit_hp_count": 0,
            }
        ],
        "player_state": [0.0] * eval_config.PLAYER_FEATURE_COUNT,
        "summary": [0.0] * eval_config.SUMMARY_FEATURE_COUNT,
    }
    for r in range(16):
        for q in range(16):
            if q == 0 and r == 0:
                continue
            obs["board_cells"].append(
                {
                    "q": q,
                    "r": r,
                    "terrain_type": 0,
                    "owner_relation": 0,
                    "visibility": 1,
                    "resource_amount": 0,
                    "road_present": 0,
                    "has_building": 0,
                    "building_type": 0,
                    "building_hp_progress": 0,
                    "has_unit": 0,
                    "unit_type": 0,
                    "unit_hp_count": 0,
                }
            )
    encoded = observation_encoder.encode_observation(obs)
    assert encoded.shape == (observation_encoder.observation_dim(),)
    assert encoded.dtype == np.float32


def test_action_encoder_decomposed():
    command = {
        "type": "PlaceBuildingPlanCommand",
        "payload": {"building_type": "woodcutter", "q": 4, "r": 5, "player_id": "p1"},
    }
    encoded = action_encoder.encode_action(command)
    assert encoded.shape == (action_encoder.action_dim(),)


def test_scoring_policy_selects_pass_only():
    policy = PassOnlyScoringPolicy()
    obs = {"schema_version": 1, "map_width": 16, "map_height": 16, "board_cells": [], "player_state": [0.0] * 9, "summary": [0.0] * 6}
    legal = [
        {"type": "PlaceBuildingPlanCommand", "payload": {"building_type": "woodcutter", "q": 1, "r": 1}},
        {"type": "NoopCommand", "payload": {"player_id": "p1"}},
    ]
    chosen = policy.select_command(obs, legal)
    assert chosen["type"] == "NoopCommand"


def test_neural_random_policy_scores_legal_only():
    policy = NeuralRandomWeightsPolicy(seed=3)
    obs = {
        "schema_version": eval_config.SCHEMA_VERSION,
        "map_width": 16,
        "map_height": 16,
        "board_cells": [{"q": q, "r": r, **{name: 0 for name in observation_encoder.BOARD_CHANNEL_NAMES}} for r in range(16) for q in range(16)],
        "player_state": [0.0] * eval_config.PLAYER_FEATURE_COUNT,
        "summary": [0.0] * eval_config.SUMMARY_FEATURE_COUNT,
    }
    legal = [{"type": "NoopCommand", "payload": {"player_id": "p1"}}]
    chosen = policy.select_command(obs, legal)
    assert chosen in legal


def test_random_legal_policy():
    policy = RandomLegalScoringPolicy(seed=5)
    legal = [
        {"type": "NoopCommand", "payload": {}},
        {"type": "MoveUnitCommand", "payload": {"unit_id": 1, "q": 2, "r": 2}},
    ]
    obs = {}
    chosen = policy.select_command(obs, legal)
    assert chosen in legal
