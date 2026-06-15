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

## Policy Observation (RUN_06b)

RUN_06b adds a compact **policy observation** export separate from the full debug snapshot used in BC training.

- **GDScript:** `PolicyObservationExporter.export_policy_observation(state, actor_id)` — training-env op `"policy_observation"`.
- **Python:** `python_tools/training/observation_encoder.py` encodes the export to a fixed-size float32 vector for scoring/eval (legacy flat `observation.py` remains for BC only).
- **Schema version:** `schema_version` in the export (currently `1`); increment on breaking layout changes.

### Board tensor (16×16 economy/military trial maps)

Per-hex channels (actor-relative): `terrain_type`, `owner_relation`, `visibility`, `resource_amount`, `road_present`, `has_building`, `building_type`, `building_hp_progress`, `has_unit`, `unit_type`, `unit_hp_count`.

### Player / summary vectors

- **Player state (9):** wood, stone, food, population, idle_workers, building_count, unit_count, victory_points, known_enemy_strength (acting-player perspective, normalised).
- **Summary (6):** tick, phase, own/enemy unit counts, own/enemy building counts, objective-progress flags.

Unsupported map sizes are rejected with a clear error; eval scenarios use `flat_16` maps.

## Legal commands (RUN_06b)

- **GDScript:** `CommandAvailabilityProvider.enumerate_legal_commands(state, actor_id)` — training-env op `"legal_commands"`.
- Wraps candidate generation and validation; **always includes an engine-owned `NoopCommand` (pass)** even when no other actions exist.
- Policies must choose only from this list; the scoring wrapper never invents commands.

## Actions for Neural Training

- Discrete or parameterized command types allowed by variant
- Hybrid variant: exclude forbidden direct unit commands from the action space (delegated only)

## Scoring policy architecture (RUN_06b)

Eval and future evolution use a **decomposed action encoding** plus a scoring wrapper — not fixed action indices.

- **Python:** `action_encoder.encode_action(command)` — `action_type`, coords, unit/building types, costs, args (no hash buckets).
- **Python:** `scoring_policy.py` — `score(obs, action) -> float`, `select_command(obs, legal_commands)` picks the highest-scoring **legal** command only.
- Candidate paging: max 64 legal commands, page size 8, deterministic sort order.
- **Baselines (eval):** pass-only, random-legal (seeded), heuristic (`bot_decide`), scripted-good (per-scenario `eval_script`), neural-random-weights (`TinyScoringPolicy`).
- **Episode fitness:** win=1.0, draw=0.5, loss=0.0 from `PolicyEpisodeResult` (win/loss/draw only — proxy metrics are debug-only).
- **Index-based `TinyPolicy`** remains for legacy BC warm-start; **not** the RUN_06b eval path.

Batched eval IPC: training-env op `"play_episode"` runs a full baseline episode in one Godot request; neural scoring uses reset/step with compact policy observations.

Eval artefacts: `training_runs/run_06b/policy_eval/` (`report.csv`, `summary.json`, `command_logs/`).

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
