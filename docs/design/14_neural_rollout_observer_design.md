# Neural Rollout Observer Design (RUN_02.5)

## What Is a Neural Rollout?

A **deterministic episode**: fixed scenario + seed + policy → sequence of authoritative debug
snapshots and command records. Used for human review of agent behaviour — **not** for training input.

Training continues to use `export_training_snapshot()` / `PolicyObservationExporter` — never viewport pixels.

## Artefact Generation

1. Python drives Godot headless via `GodotHeadlessEnv` (file IPC, DDR-033)
2. Each step: policy selects from `available_commands[]` in current snapshot
3. Godot `step` applies command; events recorded
4. `_op_snapshot` returns `SnapshotExporter.export_debug_snapshot()` — **authoritative**
5. Python writes files under `training_runs/visual_rollouts/<run_id>/` (gitignored)

**No live neural inference in Godot v1.** Observer replays saved artefacts.

## Snapshot / Command Alignment Rule

A rollout with **N commands** contains **N+1 snapshots**:

| Index | File / row | Meaning |
|-------|------------|---------|
| 0 | `snapshots/000000.json` | Initial state **before** any command |
| 0 | `commands.jsonl` row 0 | Command **selected from** snapshot 0 |
| 1 | `snapshots/000001.json` | State **after** command row 0 |
| K | `commands.jsonl` row K | Command **selected from** snapshot K |
| K+1 | `snapshots/K+1.json` | State **after** command row K |

**Observer UI labelling:**

- **Previous command** — command that **produced** the currently displayed snapshot
  (`commands[K-1]` when viewing snapshot K, K > 0)
- **Current / next decision** — command **selected from** current snapshot
  (`commands[K]` when viewing snapshot K, if K < N)

Do **not** silently shift rows. Schema tests enforce `len(snapshots) == len(commands) + 1`.

## Stable Artefact Layout

```
training_runs/visual_rollouts/<run_id>/
  metadata.json
  metrics.json
  commands.jsonl
  events.jsonl
  snapshots/000000.json
  snapshots/000001.json
  ...
  screenshots/          # optional Phase C output
```

`schema_version: 1` in metadata.

### metadata.json

```json
{
  "schema_version": 1,
  "scenario_name": "eval_build_obvious",
  "scenario_path": "scenarios/economy/eval_build_obvious.json",
  "game_type": "economy",
  "policy_type": "random",
  "checkpoint_path": null,
  "checkpoint_id": null,
  "trained_available": true,
  "trained_absence_reason": null,
  "seed": 11,
  "start_tick": 0,
  "max_steps": 500,
  "created_timestamp": "2026-06-15T12:00:00Z",
  "code_commit": "ebd4a9e",
  "observation_encoder_version": null
}
```

`game_type`: `economy` | `military`. `policy_type`: `trained` | `untrained` | `random` | `heuristic` | `manual`.

### metrics.json

```json
{
  "total_reward": null,
  "total_score": 0.5,
  "objective_progress": {},
  "steps": 42,
  "valid_commands": 38,
  "no_ops": 4,
  "rejected_commands": 0,
  "final_outcome": "draw",
  "comparison_baseline": null
}
```

### commands.jsonl (one JSON object per line)

```json
{"step_index":0,"tick":0,"actor_id":"p1","command_id":"noop_0","command_label":"No-op","command_type":"NoopCommand","command_target":null,"policy_confidence":null,"was_accepted":true,"result_message":"accepted","reward_delta":null}
```

Do **not** fabricate `policy_confidence` if not exported.

### events.jsonl

```json
{"step_index":0,"tick":1,"event_type":"production_completed","payload":{}}
```

### snapshots/NNNNNN.json

Authoritative `export_debug_snapshot()` output including `display_layers` and `available_commands[]`
(historical context only in observer — not re-validated for training).

## Policy Types

| Type | Source |
|------|--------|
| `random` | `RandomPolicy` / `random_legal` |
| `untrained` | Fresh `TinyPolicy` or random-weight scoring net |
| `trained` | BC checkpoint if present; else metadata flags absence |
| `heuristic` | Godot heuristic bot |
| `manual` | Reserved |

## Replay in Godot

1. `RolloutLoader` parses folder; validates alignment
2. `RolloutController` indexes snapshot step K
3. `LayerRenderer.build_render_data()` — same path as RUN_05
4. Panels read sidecar rows aligned to K
5. Timeline: play/pause/step±/jump/speed — no `GameState` mutation

## Rewards / Scores / Objectives

Display from `metrics.json` and per-step `reward_delta` when present. Do not recompute in client.

## Mistakes / Poor Decisions

Visible via: rejected command count, repeated no-ops in event log, metrics `final_outcome`,
objective progress fields when exported. No ML judgement from pixels.

## Compare Runs

v1: sequential compare via Phase C markdown report — **not** dual viewport.

## Training Isolation

- Observer reads artefact files only
- No feedback to `PolicyObservationExporter` or training loop
- Viewport pixels never enter observation encoder

## Export CLI

```bash
python -m python_tools.training.visual_rollout_export \
  --scenario scenarios/economy/eval_build_obvious.json \
  --seed 11 --policy random --max-steps 50 \
  --output training_runs/visual_rollouts/economy_random_s11
```

## Observer Launch

```bash
$GODOT --path godot_project --resolution 1920x1080 \
  res://client/scenes/rollout_observer_demo.tscn \
  -- --rollout-path=training_runs/visual_rollouts/<run_id>
```

Headless smoke uses committed fixture under `godot_project/sim/tests/fixtures/visual_rollouts/`.
