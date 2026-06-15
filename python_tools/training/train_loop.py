"""BC training loop for economy and military TinyPolicy."""

from __future__ import annotations

import json
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn

from python_tools.training import action_space, eval_config
from python_tools.training.bc_dataset import (
    assert_label_quality,
    collect_bc_dataset,
    load_bc_samples_from_dir,
    save_label_stats,
)
from python_tools.training.policy import TinyPolicy, build_policy


def _train_on_samples(
    policy: TinyPolicy,
    samples,
    *,
    variant: str,
    epochs: int = 12,
    learning_rate: float = 1e-3,
) -> list[float]:
    if not samples:
        raise RuntimeError("no BC samples to train on")
    policy.set_variant(variant)
    noop = action_space.noop_index(variant)
    action_samples = [sample for sample in samples if sample.label != noop]
    noop_samples = [sample for sample in samples if sample.label == noop]
    if action_samples:
        balanced = action_samples * 8 + noop_samples
    else:
        balanced = samples
    optimizer = torch.optim.Adam(policy.parameters(), lr=learning_rate)

    obs = torch.from_numpy(np.stack([sample.observation for sample in balanced])).float()
    labels = torch.tensor([sample.label for sample in balanced], dtype=torch.long)
    output_dim = action_space.max_actions(variant)
    label_counts = torch.bincount(labels, minlength=output_dim).float()
    class_weights = label_counts.sum() / (label_counts + 1.0)
    class_weights = class_weights / class_weights.mean()
    loss_fn = nn.CrossEntropyLoss(weight=class_weights)
    losses: list[float] = []

    for _ in range(epochs):
        optimizer.zero_grad()
        logits = policy(obs)[:, :output_dim]
        loss = loss_fn(logits, labels)
        loss.backward()
        optimizer.step()
        losses.append(float(loss.item()))
    return losses


def train_variant(
    variant: str,
    *,
    scenario_path: str,
    bot_kind: str,
    bc_seeds: list[int],
    max_ticks: int,
    fog_enabled: int | None = None,
    run_dir: Path,
    checkpoint_dir: Path,
) -> dict:
    dataset_dir = run_dir / "bc_datasets"
    dataset_dir.mkdir(parents=True, exist_ok=True)
    existing = sorted(dataset_dir.glob(f"bc_{variant}_*.json"))
    if existing and variant not in ("economy_trial", "military_trial"):
        samples = load_bc_samples_from_dir(dataset_dir, variant)
        stats_path = run_dir / f"bc_label_stats_{variant}.json"
        if stats_path.exists():
            stats = json.loads(stats_path.read_text(encoding="utf-8"))
            stats.pop("variant", None)
        else:
            stats = {"matched": len(samples), "unmatched": 0, "match_rate": 1.0}
    else:
        samples, stats = collect_bc_dataset(
            variant,
            bc_seeds,
            scenario_path=scenario_path,
            bot_kind=bot_kind,
            max_ticks=max_ticks,
            dataset_dir=dataset_dir,
            fog_enabled=fog_enabled,
        )
        save_label_stats(run_dir / f"bc_label_stats_{variant}.json", {"variant": variant, **stats})
    assert_label_quality(variant, stats)

    policy = build_policy(variant)
    train_epochs = 80 if variant in ("economy_trial", "military_trial") else 12
    losses = _train_on_samples(policy, samples, variant=variant, epochs=train_epochs)
    checkpoint_dir.mkdir(parents=True, exist_ok=True)
    checkpoint_name = "economy_policy.pt" if variant == "economy_trial" else "military_policy.pt"
    checkpoint_path = checkpoint_dir / checkpoint_name
    torch.save({"state_dict": policy.state_dict(), "variant": variant}, checkpoint_path)
    return {
        "variant": variant,
        "samples": len(samples),
        "losses": losses,
        "checkpoint": str(checkpoint_path),
        "bc_stats": stats,
    }


def run_training(run_dir: Path | None = None, checkpoint_dir: Path | None = None) -> dict:
    root = Path(__file__).resolve().parents[2]
    run_dir = run_dir or (root / eval_config.TRAINING_RUN_DIR)
    checkpoint_dir = checkpoint_dir or (root / eval_config.CHECKPOINT_DIR)
    run_dir.mkdir(parents=True, exist_ok=True)

    economy = train_variant(
        "economy_trial",
        scenario_path=eval_config.ECONOMY_SCENARIO,
        bot_kind="economy",
        bc_seeds=eval_config.ECONOMY_BC_SEEDS,
        max_ticks=eval_config.ECONOMY_TICK_BUDGET,
        run_dir=run_dir,
        checkpoint_dir=checkpoint_dir,
    )
    military = train_variant(
        "military_trial",
        scenario_path=eval_config.MILITARY_SCENARIO,
        bot_kind="military",
        bc_seeds=eval_config.MILITARY_BC_SEEDS,
        max_ticks=eval_config.MILITARY_TICK_BUDGET,
        fog_enabled=0,
        run_dir=run_dir,
        checkpoint_dir=checkpoint_dir,
    )
    payload = {"economy": economy, "military": military}
    (run_dir / "metrics.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return payload


if __name__ == "__main__":
    result = run_training()
    print(json.dumps(result, indent=2))
