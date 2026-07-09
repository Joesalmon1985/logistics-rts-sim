"""Replay buffer placeholder for hybrid training."""

from __future__ import annotations

from collections import deque


class ReplayBuffer:
    def __init__(self, capacity: int = 10000) -> None:
        self._buf: deque = deque(maxlen=capacity)

    def push(self, transition: dict) -> None:
        self._buf.append(transition)

    def __len__(self) -> int:
        return len(self._buf)
