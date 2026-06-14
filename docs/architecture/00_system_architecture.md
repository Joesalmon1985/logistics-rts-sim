# System Architecture

## Overview

The authoritative game simulation is a **pure Godot/GDScript simulation core**. The Godot visual client/UI is non-authoritative. Python is for training/tooling only and is never the shipped runtime.

```
┌──────────────────────────────────────────────────────────────┐
│            Godot app (godot_project/)                          │
│                                                                │
│   sim/  — AUTHORITATIVE GDScript simulation core (headless OK) │
│  ┌──────────┐  ┌────────────┐  ┌───────────┐  ┌──────────┐   │
│  │ Variant  │  │ Command    │  │ Tick Loop │  │ Event    │   │
│  │ Registry │→ │ Validator  │→ │ + RNG     │→ │ Log      │   │
│  └──────────┘  └────────────┘  └───────────┘  └──────────┘   │
│                       │                │              │        │
│                       ▼                ▼              ▼        │
│              ┌────────────────────────────────────────────┐   │
│              │ Economy │ Military │ Hybrid modules         │   │
│              └────────────────────────────────────────────┘   │
│                       │                                        │
│                       ▼                                        │
│       ┌─────────────────┐  ┌──────────────────────┐           │
│       │ Snapshot Export │  │ Replay Hash / Save   │           │
│       └─────────────────┘  └──────────────────────┘           │
│                       ▲ commands        │ snapshots/events     │
│                       │                 ▼                      │
│   client/ — NON-AUTHORITATIVE Godot UI (scenes/renderers/UI)  │
└──────────────────────────────────────────────────────────────┘
            ▲                                  ▲
            │ commands / snapshots             │ commands (LAN)
   ┌────────┴────────┐               ┌─────────┴─────────┐
   │ Python tools    │               │ Wi-Fi client      │
   │ (training,      │               │ (host-authoritative│
   │  analysis) via  │               │  RUN_12)          │
   │ Godot headless  │               └───────────────────┘
   └─────────────────┘
```

## Components

| Component | Responsibility |
|-----------|----------------|
| Variant registry | Selects economy_trial, military_trial, hybrid rule sets |
| Command validator | Validates and queues commands per tick |
| Tick loop | Advances sim deterministically (no frame delta / wall-clock) |
| Event log | Append-only serializable events |
| Snapshot exporter | JSON state for clients and tooling |
| Replay system | Command log + hash verification |
| Godot visual client | Non-authoritative viewer/UI (`client/`, see `03_godot_adapter_contract.md`) |
| Python tools | Training/analysis/tournament; drive the core via Godot headless (`python_tools/`) |

## Data Flow

1. Client (UI / bot / agent / peer) submits command
2. Validator checks legality → accepts or rejects with reason
3. Tick loop applies accepted commands, runs subsystems, emits events
4. Snapshot exporter produces JSON for clients/tooling
5. Replay records commands for hash verification

## Headless Operation

The simulation core runs without rendering via Godot headless (CLI tools in `sim/tools/`) and the headless test harness. The visual client is optional for all headless milestones (RUN_01–RUN_04, RUN_07–RUN_08).

## Runtime / Packaging

- The mobile app ships the authoritative GDScript core **in-process** with the visual client. No embedded Python, no Python process dependency for normal gameplay (DDR-026).
- Multiplayer (RUN_12) is **host-authoritative**: the host runs the core; clients submit commands and receive snapshots/events (DDR-030).

## Related Documents

- `01_simulation_core_authoritative.md`
- `02_command_event_snapshot_contract.md`
- `04_bot_and_training_architecture.md`
- `05_determinism_replay_save_load.md`
