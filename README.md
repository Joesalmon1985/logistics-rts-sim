# Logistics RTS Sim

Original mobile Godot strategy game combining settlement logistics, RTS military gameplay, and a hybrid mode where military production depends on the economy.

**This is not Widelands, OpenRA, Command & Conquer, or Red Alert.** Use original placeholder names, rules, and assets only.

## Architecture

| Layer | Role |
|-------|------|
| **Python simulation** | Authoritative, headless, deterministic. Commands, events, snapshots, replay. |
| **Godot client** | Mobile 2D viewer, command submission, diagnostics. Non-authoritative. |
| **Bots / agents** | Same command API as humans |

All randomness uses a seeded RNG in simulation state. See `docs/architecture/00_system_architecture.md`.

## Run Sequence

| Run | Document | Focus |
|-----|----------|-------|
| 00 | [RUN_00](docs/runs/RUN_00_CURSOR_BOOTSTRAP.md) | Docs and rules (this bootstrap) |
| 01 | [RUN_01](docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md) | Core sim kernel |
| 02 | [RUN_02](docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md) | Trial economy game |
| 03 | [RUN_03](docs/runs/RUN_03_TRIAL_MILITARY_GAME.md) | Trial military game |
| 04 | [RUN_04](docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md) | Bots, replay, telemetry |
| 05 | [RUN_05](docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md) | Godot trial viewers |
| 06 | [RUN_06](docs/runs/RUN_06_NEURAL_TRAINING_FOR_TRIAL_GAMES.md) | Neural training |
| 07 | [RUN_07](docs/runs/RUN_07_HEADLESS_HYBRID_GAME.md) | Headless hybrid |
| 08 | [RUN_08](docs/runs/RUN_08_HYBRID_BOTS_TELEMETRY_BALANCE.md) | Hybrid bots and balance |
| 09 | [RUN_09](docs/runs/RUN_09_GODOT_2D_HYBRID_OBSERVER.md) | Godot hybrid observer |
| 10 | [RUN_10](docs/runs/RUN_10_HUMAN_PLAYABLE_HYBRID.md) | Human playable hybrid |
| 11 | [RUN_11](docs/runs/RUN_11_EXPANDED_CONTENT_AND_PROGRESSION.md) | Expanded content |
| 12 | [RUN_12](docs/runs/RUN_12_WIFI_MULTIPLAYER_AND_PACKAGING.md) | Wi-Fi multiplayer |

## Documentation

| Area | Path |
|------|------|
| Cursor rules | `.cursor/rules/` |
| Design requirements | `docs/design/` |
| Architecture | `docs/architecture/` |
| Testing | `docs/testing/` |
| Run plans | `docs/runs/` |
| Glossary | `docs/design/10_glossary.md` |

Key requirement contracts:

- [Trial Economy Game](docs/design/02_trial_economy_game_requirements.md)
- [Trial Military Game](docs/design/03_trial_military_game_requirements.md)
- [Hybrid Game](docs/design/04_hybrid_game_requirements.md)

## Development Workflow

1. Read the run document for your milestone.
2. Read prerequisite design/architecture/testing docs.
3. Inspect git state; create/checkout feature branch.
4. Write tests first (reference rule IDs).
5. Implement until acceptance checklist passes.
6. Run smoke tests; `git diff --check`.
7. Commit, push feature branch, deliver final report.

Do not merge to `main` unless explicitly instructed.
