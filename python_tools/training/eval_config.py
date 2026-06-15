"""Frozen RUN_06b evaluation configuration."""

from __future__ import annotations

RUN_06B_EVAL_DIR = "training_runs/run_06b/policy_eval"

ECONOMY_EVAL_SCENARIOS = [
    "scenarios/economy/eval_build_obvious.json",
    "scenarios/economy/eval_outproduce.json",
]
MILITARY_EVAL_SCENARIOS = [
    "scenarios/military/eval_focus_fire_hub.json",
    "scenarios/military/eval_survive_rush.json",
]

ECONOMY_EVAL_SEEDS = [11, 12]
MILITARY_EVAL_SEEDS = [21, 22]

POLICY_KINDS = [
    "pass_only",
    "random_legal",
    "heuristic",
    "scripted_good",
    "neural_random",
]

SCHEMA_VERSION = 1
FIXED_MAP_WIDTH = 16
FIXED_MAP_HEIGHT = 16

BOARD_CHANNEL_COUNT = 11
PLAYER_FEATURE_COUNT = 9
SUMMARY_FEATURE_COUNT = 6

ACTION_TYPE_COUNT = 12
ACTION_ARG_COUNT = 8

MAX_CANDIDATE_ACTIONS = 64
CANDIDATE_PAGE_SIZE = 8

# Legacy RUN_06 BC paths (unchanged)
ECONOMY_SCENARIO = "scenarios/economy/tiny_success_map.json"
MILITARY_SCENARIO = "scenarios/military/skirmish_1v1.json"
ECONOMY_EVAL_SEEDS_LEGACY = [11, 12, 13]
MILITARY_EVAL_SEEDS_LEGACY = [21, 22, 23]
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

FITNESS_WIN = 1.0
FITNESS_DRAW = 0.5
FITNESS_LOSS = 0.0
