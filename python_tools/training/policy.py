"""Policies for rollout, BC training, and evaluation."""

from __future__ import annotations

import random
from typing import Protocol

import numpy as np
import torch
import torch.nn as nn

from python_tools.training import action_space, command_match, observation


class Policy(Protocol):
    def select_action(
        self,
        snapshot: dict,
        available_commands: list,
        *,
        deterministic: bool = False,
    ) -> int: ...


class SeededStubPolicy:
    def __init__(self, policy_seed: int, variant: str) -> None:
        self.rng = random.Random(policy_seed)
        self.variant = variant

    def select_action(
        self,
        snapshot: dict,
        available_commands: list,
        *,
        deterministic: bool = False,
    ) -> int:
        noop = action_space.noop_index(self.variant)
        legal = min(len(available_commands), noop)
        choices = list(range(legal)) + [noop]
        return self.rng.choice(choices)


class RandomPolicy:
    def __init__(self, policy_seed: int, variant: str) -> None:
        self.rng = random.Random(policy_seed)
        self.variant = variant

    def select_action(
        self,
        snapshot: dict,
        available_commands: list,
        *,
        deterministic: bool = False,
    ) -> int:
        noop = action_space.noop_index(self.variant)
        legal = min(len(available_commands), noop)
        choices = list(range(legal)) + [noop]
        return self.rng.choice(choices)


class TinyPolicy(nn.Module):
    def __init__(self, input_dim: int, output_dim: int, hidden: int = 64) -> None:
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(input_dim, hidden),
            nn.ReLU(),
            nn.Linear(hidden, hidden),
            nn.ReLU(),
            nn.Linear(hidden, output_dim),
        )

    def forward(self, obs: torch.Tensor) -> torch.Tensor:
        return self.net(obs)

    def select_action(
        self,
        snapshot: dict,
        available_commands: list,
        *,
        deterministic: bool = False,
    ) -> int:
        obs = observation.encode_snapshot(snapshot)
        logits = self.forward(torch.from_numpy(obs).float().unsqueeze(0)).squeeze(0)
        limit = action_space.max_actions(self.variant)
        noop = action_space.noop_index(self.variant)
        mask = torch.full((limit,), float("-inf"))
        legal = min(len(available_commands), noop)
        for index in range(legal):
            mask[index] = 0.0
        mask[noop] = 0.0
        masked = logits[:limit] + mask
        if deterministic:
            return int(torch.argmax(masked).item())
        probs = torch.softmax(masked, dim=0)
        return int(torch.multinomial(probs, 1).item())

    @property
    def variant(self) -> str:
        return getattr(self, "_variant", "economy_trial")

    def set_variant(self, variant: str) -> None:
        self._variant = variant


def build_policy(variant: str) -> TinyPolicy:
    policy = TinyPolicy(
        observation.feature_dim(variant),
        action_space.max_actions(variant),
    )
    policy.set_variant(variant)
    return policy


def action_from_policy(
    policy: Policy,
    snapshot: dict,
    available_commands: list,
    tick: int,
    *,
    deterministic: bool,
    actor_id: str = "p1",
) -> dict | None:
    index = policy.select_action(snapshot, available_commands, deterministic=deterministic)
    return command_match.index_to_command(index, available_commands, tick, actor_id, variant=getattr(policy, "variant", "economy_trial"))
