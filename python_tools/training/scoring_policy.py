"""Scoring-based policy wrapper — selects highest-scoring legal command only."""

from __future__ import annotations

import random
from typing import Any, Protocol

import numpy as np
import torch
import torch.nn as nn

from python_tools.training import action_encoder, eval_config, observation_encoder


class ScoringPolicy(Protocol):
    def score(self, observation: dict[str, Any], command: dict[str, Any]) -> float: ...

    def select_command(
        self,
        observation: dict[str, Any],
        legal_commands: list[dict[str, Any]],
    ) -> dict[str, Any]: ...


class TinyScoringPolicy(nn.Module):
    def __init__(self, obs_dim: int, action_dim: int, hidden: int = 64, seed: int = 0) -> None:
        super().__init__()
        self.obs_dim = obs_dim
        self.action_dim = action_dim
        self.net = nn.Sequential(
            nn.Linear(obs_dim + action_dim, hidden),
            nn.ReLU(),
            nn.Linear(hidden, hidden),
            nn.ReLU(),
            nn.Linear(hidden, 1),
        )
        torch.manual_seed(seed)
        for param in self.parameters():
            nn.init.normal_(param, mean=0.0, std=0.05)

    def forward(self, obs: torch.Tensor, action: torch.Tensor) -> torch.Tensor:
        return self.net(torch.cat([obs, action], dim=-1)).squeeze(-1)

    def score_tensor(self, obs_tensor: torch.Tensor, action_tensor: torch.Tensor) -> float:
        with torch.no_grad():
            value = self.forward(obs_tensor, action_tensor)
            return float(value.item())

    def score(self, observation: dict[str, Any], command: dict[str, Any]) -> float:
        obs_tensor = torch.from_numpy(observation_encoder.encode_observation(observation)).float()
        action_tensor = torch.from_numpy(action_encoder.encode_action(command)).float()
        return self.score_tensor(obs_tensor, action_tensor)

    def select_command(
        self,
        observation: dict[str, Any],
        legal_commands: list[dict[str, Any]],
    ) -> dict[str, Any]:
        if not legal_commands:
            raise ValueError("legal_commands must be non-empty")
        best_command = legal_commands[0]
        best_score = float("-inf")
        limit = min(len(legal_commands), eval_config.MAX_CANDIDATE_ACTIONS)
        page_size = eval_config.CANDIDATE_PAGE_SIZE
        obs_tensor = torch.from_numpy(observation_encoder.encode_observation(observation)).float()
        for start in range(0, limit, page_size):
            end = min(start + page_size, limit)
            for command in legal_commands[start:end]:
                action_tensor = torch.from_numpy(action_encoder.encode_action(command)).float()
                score = self.score_tensor(obs_tensor, action_tensor)
                if score > best_score:
                    best_score = score
                    best_command = command
        return best_command


class PassOnlyScoringPolicy:
    def score(self, observation: dict[str, Any], command: dict[str, Any]) -> float:
        _ = observation
        return 1.0 if str(command.get("type", "")) == "NoopCommand" else 0.0

    def select_command(
        self,
        observation: dict[str, Any],
        legal_commands: list[dict[str, Any]],
    ) -> dict[str, Any]:
        _ = observation
        for command in legal_commands:
            if str(command.get("type", "")) == "NoopCommand":
                return command
        return legal_commands[0]


class RandomLegalScoringPolicy:
    def __init__(self, seed: int) -> None:
        self.rng = random.Random(seed)

    def score(self, observation: dict[str, Any], command: dict[str, Any]) -> float:
        _ = observation, command
        return self.rng.random()

    def select_command(
        self,
        observation: dict[str, Any],
        legal_commands: list[dict[str, Any]],
    ) -> dict[str, Any]:
        _ = observation
        return self.rng.choice(legal_commands)


class NeuralRandomWeightsPolicy(TinyScoringPolicy):
    def __init__(self, seed: int = 7, variant: str = "economy_trial") -> None:
        _ = variant
        super().__init__(
            observation_encoder.observation_dim(),
            action_encoder.action_dim(),
            hidden=32,
            seed=seed,
        )


def build_scoring_policy(kind: str, *, seed: int = 7, variant: str = "economy_trial") -> ScoringPolicy:
    if kind == "pass_only":
        return PassOnlyScoringPolicy()
    if kind == "random_legal":
        return RandomLegalScoringPolicy(seed)
    if kind == "neural_random":
        return NeuralRandomWeightsPolicy(seed=seed, variant=variant)
    raise ValueError(f"unsupported scoring policy kind for Python wrapper: {kind}")
