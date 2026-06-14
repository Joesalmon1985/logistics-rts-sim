# Bot and Training Architecture

## Unified Client Model

Bots, neural agents, and humans are **clients** of the same command API exposed by the authoritative GDScript simulation core.

```
Observation = f(snapshot, player_id)
Action = Command (JSON / dict)
```

No privileged API for bots except scenario fixtures in tests.

## Where bots run

- **In-engine bots (RUN_04, RUN_08):** implemented in GDScript inside the simulation core's tooling, submitting commands through the same validator as humans. Used for smoke tests and skirmish opponents.
- **Python training agents (RUN_06):** Python is the **trainer, not the engine**. Python drives the authoritative core by running **Godot headless** and exchanging snapshots/commands/replays over a development-time transport (CLI + JSON files, pipes, or local socket — exact choice at RUN_06, see DDR-033). There is **no Python simulation package to import**; Python never computes rules.

## Bot Interface (RUN_04, GDScript)

```
class_name Bot
func observe(snapshot: Dictionary, player_id: String) -> void
func decide(tick: int) -> Command   # or null
```

Bot runner:
1. Export snapshot from the core
2. `bot.decide()` → command or none
3. Submit command through the validator
4. Tick the core
5. Repeat

## Observations for Neural Training (RUN_06)

- Encode snapshot fields to a fixed-size tensor or structured dict
- Must be derivable from the **snapshot alone** (no hidden core state, no scene/render state)
- Document the observation schema in RUN_06

## Actions for Neural Training

- Discrete or parameterized command types allowed by variant
- Hybrid variant: exclude forbidden direct unit commands from the action space (delegated only)

## Training Loop (Python trainer driving Godot headless)

```
for episode in episodes:
    env = GodotHeadlessEnv(scenario, seed)   # launches/controls Godot headless core
    obs = encode(env.snapshot())
    while not done:
        cmd = policy(obs)                     # policy RNG must be seeded (determinism)
        result = env.submit(cmd)              # validated by the GDScript core
        env.tick()
        obs = encode(env.snapshot())
        reward = compute_reward(env.events()) # RUN_06 defines reward
    save replay for verification
```

## Determinism Requirements

- Same policy (including seeded policy sampling) + seed → same command log
- Replay hash must match after a training rollout (verified by re-running the core)

## Artifacts

- Model weights: `models/`, `checkpoints/` (gitignored)
- Training logs: `wandb/` (gitignored)
- Python training code lives in `python_tools/training/`

## Gates

See `.cursor/rules/08-neural-training-gates.mdc` — no training before RUN_06 prerequisites.

## Tests

See `docs/testing/04_training_test_plan.md`.
