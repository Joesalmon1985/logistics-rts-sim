"""Frozen paired evaluation configuration (committed)."""

from __future__ import annotations

ECONOMY_SCENARIO = "scenarios/economy/tiny_success_map.json"
MILITARY_SCENARIO = "scenarios/military/skirmish_1v1.json"

ECONOMY_EVAL_SEEDS = [11, 12, 13]
MILITARY_EVAL_SEEDS = [21, 22, 23]

ECONOMY_BC_SEEDS = [11, 12]
MILITARY_BC_SEEDS = [21, 22]

ECONOMY_HOLDOUT_SEEDS = [13]
MILITARY_HOLDOUT_SEEDS = [23]

ECONOMY_TICK_BUDGET = 400
MILITARY_TICK_BUDGET = 600

IMPROVE_MIN_SEED_WIN_RATE = 0.6
IMPROVE_MIN_HOLDOUT_ACCURACY_DEFAULT = 0.40
IMPROVE_MIN_HOLDOUT_ACCURACY = {
    "economy_trial": 0.40,
    "military_trial": 0.25,
}

ECONOMY_PRIMARY_METRIC = "economy_score"
MILITARY_PRIMARY_METRIC = "military_score"

BC_MIN_MATCH_RATE = {
    "economy_trial": 0.90,
    "military_trial": 0.85,
}

TRAINING_RUN_DIR = "training_runs/run06_smoke"
CHECKPOINT_DIR = "checkpoints/run06_smoke"
