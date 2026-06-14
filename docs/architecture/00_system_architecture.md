# System Architecture

## Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Python Simulation                        │
│  ┌──────────┐  ┌────────────┐  ┌───────────┐  ┌──────────┐ │
│  │ Variant  │  │ Command    │  │ Tick Loop │  │ Event    │ │
│  │ Registry │→ │ Validator  │→ │ + RNG     │→ │ Log      │ │
│  └──────────┘  └────────────┘  └───────────┘  └──────────┘ │
│                       │                │              │      │
│                       ▼                ▼              ▼      │
│              ┌────────────────────────────────────────────┐  │
│              │ Economy │ Military │ Hybrid modules        │  │
│              └────────────────────────────────────────────┘  │
│                       │                                      │
│                       ▼                                      │
│              ┌─────────────────┐  ┌──────────────────────┐   │
│              │ Snapshot Export │  │ Replay Hash / Save   │   │
│              └─────────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
         ▲              ▲              ▲              ▲
         │              │              │              │
    ┌────┴────┐   ┌─────┴─────┐  ┌─────┴─────┐  ┌────┴────┐
    │ Godot   │   │ Bot       │  │ Neural    │  │ Wi-Fi   │
    │ Client  │   │ Client    │  │ Agent     │  │ Client  │
    └─────────┘   └───────────┘  └───────────┘  └─────────┘
```

## Components

| Component | Responsibility |
|-----------|----------------|
| Variant registry | Selects economy_trial, military_trial, hybrid rule sets |
| Command validator | Validates and queues commands per tick |
| Tick loop | Advances sim deterministically |
| Event log | Append-only serializable events |
| Snapshot exporter | JSON state for clients |
| Replay system | Command log + hash verification |
| Godot adapter | Non-authoritative viewer (see `03_godot_adapter_contract.md`) |

## Data Flow

1. Client submits command JSON
2. Validator checks legality → accepts or rejects with reason
3. Tick loop applies accepted commands, runs subsystems, emits events
4. Snapshot exporter produces JSON for clients
5. Replay records commands for hash verification

## Headless Operation

Simulation runs without Godot via CLI or pytest. Godot is optional for all milestones until RUN_05.

## Related Documents

- `01_python_sim_authoritative_core.md`
- `02_command_event_snapshot_contract.md`
- `04_bot_and_training_architecture.md`
- `05_determinism_replay_save_load.md`
