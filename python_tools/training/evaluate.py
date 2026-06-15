"""Paired fixed-seed evaluation: trained vs random vs untrained."""

from __future__ import annotations

import csv
import json
from pathlib import Path

import torch

from python_tools.training import action_space, eval_config
from python_tools.training.bc_dataset import collect_bc_episode_fast
from python_tools.training.godot_env import GodotHeadlessEnv
from python_tools.training.policy import RandomPolicy, TinyPolicy, build_policy
from python_tools.training.rollout import run_episode


def load_trained_policy(checkpoint_path: Path, variant: str) -> TinyPolicy:
    policy = build_policy(variant)
    payload = torch.load(checkpoint_path, map_location="cpu", weights_only=True)
    policy.load_state_dict(payload["state_dict"])
    policy.set_variant(variant)
    policy.eval()
    return policy


def evaluate_variant(
    variant: str,
    *,
    scenario_path: str,
    eval_seeds: list[int],
    tick_budget: int,
    primary_metric: str,
    checkpoint_path: Path,
    fog_enabled: int | None = None,
) -> list[dict]:
    rows: list[dict] = []
    trained = load_trained_policy(checkpoint_path, variant)
    untrained = build_policy(variant)
    untrained.set_variant(variant)
    untrained.eval()

    for seed in eval_seeds:
        for policy_kind, policy in (
            ("trained", trained),
            ("random", RandomPolicy(seed * 17 + 3, variant)),
            ("untrained", untrained),
        ):
            with GodotHeadlessEnv() as episode_env:
                episode = run_episode(
                    episode_env,
                    policy,
                    scenario_path=scenario_path,
                    seed=seed,
                    variant=variant,
                    max_ticks=tick_budget,
                    deterministic=True,
                    fog_enabled=fog_enabled,
                )
            row = {
                "variant": variant,
                "eval_seed": seed,
                "policy_kind": policy_kind,
                "primary_metric": primary_metric,
                "primary_value": episode["metrics"].get(primary_metric, 0),
            }
            row.update(episode["metrics"])
            rows.append(row)
    return rows


def write_evaluation_csv(path: Path, rows: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not rows:
        path.write_text("", encoding="utf-8")
        return
    fieldnames = list(rows[0].keys())
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def mean_primary(rows: list[dict], policy_kind: str, primary_metric: str) -> float:
    values = [
        float(row["primary_value"])
        for row in rows
        if row["policy_kind"] == policy_kind
    ]
    if not values:
        return 0.0
    return sum(values) / len(values)


def per_seed_wins(rows: list[dict], primary_metric: str) -> float:
    seeds = sorted({row["eval_seed"] for row in rows})
    wins = 0
    for seed in seeds:
        seed_rows = [row for row in rows if row["eval_seed"] == seed]
        trained = next(row for row in seed_rows if row["policy_kind"] == "trained")
        random_row = next(row for row in seed_rows if row["policy_kind"] == "random")
        untrained_row = next(row for row in seed_rows if row["policy_kind"] == "untrained")
        if (
            float(trained["primary_value"]) > float(random_row["primary_value"])
            and float(trained["primary_value"]) > float(untrained_row["primary_value"])
        ):
            wins += 1
    return wins / max(1, len(seeds))


def policy_accuracy_on_dataset(policy, dataset_path: Path, variant: str) -> float:
    raw = json.loads(dataset_path.read_text(encoding="utf-8"))
    steps = raw.get("steps", [])
    if not steps:
        return 0.0
    correct = 0
    for step in steps:
        snapshot = step.get("snapshot", {})
        label = int(step.get("label", -1))
        available = snapshot.get("available_commands", [])
        predicted = policy.select_action(snapshot, available, deterministic=True)
        if predicted == label:
            correct += 1
    return correct / len(steps)


def evaluate_bc_holdout(
    variant: str,
    *,
    scenario_path: str,
    bot_kind: str,
    holdout_seeds: list[int],
    max_ticks: int,
    checkpoint_path: Path,
    dataset_dir: Path,
    fog_enabled: int | None = None,
) -> list[dict]:
    rows: list[dict] = []
    trained = load_trained_policy(checkpoint_path, variant)
    untrained = build_policy(variant)
    untrained.set_variant(variant)
    untrained.eval()
    dataset_dir.mkdir(parents=True, exist_ok=True)

    with GodotHeadlessEnv() as env:
        for seed in holdout_seeds:
            dataset_path = dataset_dir / f"bc_holdout_{variant}_{seed}.json"
            collect_bc_episode_fast(
                env,
                scenario_path=scenario_path,
                seed=seed,
                bot_kind=bot_kind,
                variant=variant,
                max_ticks=max_ticks,
                dataset_path=dataset_path,
                fog_enabled=fog_enabled,
            )
            for policy_kind, policy in (
                ("trained", trained),
                ("random", RandomPolicy(seed * 17 + 3, variant)),
                ("untrained", untrained),
            ):
                accuracy = policy_accuracy_on_dataset(policy, dataset_path, variant)
                rows.append(
                    {
                        "variant": variant,
                        "eval_seed": seed,
                        "policy_kind": policy_kind,
                        "primary_metric": "holdout_accuracy",
                        "primary_value": accuracy,
                        "holdout_accuracy": accuracy,
                    }
                )
    return rows


def assert_holdout_improvement(rows: list[dict], variant: str) -> dict:
    minimum = eval_config.IMPROVE_MIN_HOLDOUT_ACCURACY.get(
        variant, eval_config.IMPROVE_MIN_HOLDOUT_ACCURACY_DEFAULT
    )
    trained_mean = mean_primary(rows, "trained", "holdout_accuracy")
    random_mean = mean_primary(rows, "random", "holdout_accuracy")
    untrained_mean = mean_primary(rows, "untrained", "holdout_accuracy")
    seed_win_rate = per_seed_wins(rows, "holdout_accuracy")
    if trained_mean < minimum:
        raise AssertionError(
            f"trained holdout accuracy {trained_mean:.3f} below minimum {minimum}"
        )
    if trained_mean <= random_mean or trained_mean <= untrained_mean:
        raise AssertionError(
            f"trained accuracy {trained_mean:.3f} must exceed random {random_mean:.3f} and untrained {untrained_mean:.3f}"
        )
    if seed_win_rate < eval_config.IMPROVE_MIN_SEED_WIN_RATE:
        raise AssertionError(
            f"trained wins on only {seed_win_rate:.2f} holdout seeds; need >= {eval_config.IMPROVE_MIN_SEED_WIN_RATE}"
        )
    return {
        "trained_mean": trained_mean,
        "random_mean": random_mean,
        "untrained_mean": untrained_mean,
        "seed_win_rate": seed_win_rate,
    }


def assert_improvement(rows: list[dict], primary_metric: str) -> dict:
    trained_mean = mean_primary(rows, "trained", primary_metric)
    random_mean = mean_primary(rows, "random", primary_metric)
    untrained_mean = mean_primary(rows, "untrained", primary_metric)
    seed_win_rate = per_seed_wins(rows, primary_metric)
    if trained_mean <= random_mean or trained_mean <= untrained_mean:
        raise AssertionError(
            f"trained mean {trained_mean} must exceed random {random_mean} and untrained {untrained_mean}"
        )
    if seed_win_rate < eval_config.IMPROVE_MIN_SEED_WIN_RATE:
        raise AssertionError(
            f"trained wins on only {seed_win_rate:.2f} seeds; need >= {eval_config.IMPROVE_MIN_SEED_WIN_RATE}"
        )
    return {
        "trained_mean": trained_mean,
        "random_mean": random_mean,
        "untrained_mean": untrained_mean,
        "seed_win_rate": seed_win_rate,
    }


def run_evaluation(
    run_dir: Path | None = None,
    checkpoint_dir: Path | None = None,
) -> dict:
    root = Path(__file__).resolve().parents[2]
    run_dir = run_dir or (root / eval_config.TRAINING_RUN_DIR)
    checkpoint_dir = checkpoint_dir or (root / eval_config.CHECKPOINT_DIR)

    economy_rows = evaluate_bc_holdout(
        "economy_trial",
        scenario_path=eval_config.ECONOMY_SCENARIO,
        bot_kind="economy",
        holdout_seeds=eval_config.ECONOMY_HOLDOUT_SEEDS,
        max_ticks=eval_config.ECONOMY_TICK_BUDGET,
        checkpoint_path=checkpoint_dir / "economy_policy.pt",
        dataset_dir=run_dir / "bc_holdout",
    )
    military_rows = evaluate_bc_holdout(
        "military_trial",
        scenario_path=eval_config.MILITARY_SCENARIO,
        bot_kind="military",
        holdout_seeds=eval_config.MILITARY_HOLDOUT_SEEDS,
        max_ticks=eval_config.MILITARY_TICK_BUDGET,
        checkpoint_path=checkpoint_dir / "military_policy.pt",
        dataset_dir=run_dir / "bc_holdout",
        fog_enabled=0,
    )

    rows = economy_rows + military_rows
    write_evaluation_csv(run_dir / "evaluation.csv", rows)
    summary = {
        "economy": assert_holdout_improvement(economy_rows, "economy_trial"),
        "military": assert_holdout_improvement(military_rows, "military_trial"),
    }
    metrics_path = run_dir / "metrics.json"
    if metrics_path.exists():
        existing = json.loads(metrics_path.read_text(encoding="utf-8"))
    else:
        existing = {}
    existing["evaluation"] = summary
    metrics_path.write_text(json.dumps(existing, indent=2), encoding="utf-8")
    return summary


if __name__ == "__main__":
    print(json.dumps(run_evaluation(), indent=2))
