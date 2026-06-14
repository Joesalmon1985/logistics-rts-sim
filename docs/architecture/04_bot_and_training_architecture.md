# Bot and Training Architecture

## Unified Client Model

Bots, neural agents, and humans are **clients** of the same command API.

```
Observation = f(snapshot, player_id)
Action = Command JSON
```

No privileged API for bots except scenario fixtures in tests.

## Bot Interface (RUN_04)

```python
class Bot:
    def observe(self, snapshot: dict, player_id: str) -> None: ...
    def decide(self, tick: int) -> Command | None: ...
```

Bot runner:
1. Export snapshot
2. Bot.decide() → command or None
3. Submit command through validator
4. Tick sim
5. Repeat

## Observations for Neural Training (RUN_06)

- Encode snapshot fields to fixed-size tensor or structured dict
- Must be derivable from snapshot alone (no hidden sim state)
- Document observation schema in RUN_06

## Actions for Neural Training

- Discrete or parameterized command types allowed by variant
- Hybrid variant: exclude direct unit commands from action space for human-parity agents

## Training Loop

```
for episode in episodes:
    state = load_scenario(seed)
    while not done:
        obs = encode(snapshot)
        cmd = policy(obs)
        result = submit(cmd)
        sim.tick()
        reward = compute_reward(events)  # RUN_06 defines reward
    log replay for verification
```

## Determinism Requirements

- Same policy + seed → same command log
- Replay hash must match after training rollout

## Artifacts

- Model weights: `models/`, `checkpoints/` (gitignored)
- Training logs: `wandb/` (gitignored)

## Gates

See `.cursor/rules/08-neural-training-gates.mdc` — no training before RUN_06 prerequisites.

## Tests

See `docs/testing/04_training_test_plan.md`.
