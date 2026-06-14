# Project Brief

## Vision

Build an **original mobile Godot strategy game** that combines settlement logistics with RTS military gameplay. The final product is a hybrid where military construction and production depend on the logistics economy, and player military control is delegated through buildings and priorities rather than individual unit micro.

## Three-Phase Roadmap

| Phase | Name | Purpose |
|-------|------|---------|
| 1 | Trial Economy Game | Permanent abstract economy engine on a hex map |
| 2 | Trial Military Game | Standalone RTS trial with direct unit control |
| 3 | Hybrid Game | Composition of economy + military + integration rules |

Each phase is implemented headlessly in Python first, then visualised in Godot.

## Architecture Summary

- **Python simulation** — authoritative, headless, deterministic, seeded RNG, events, snapshots, replay.
- **Godot client** — mobile 2D viewer, command submission, diagnostics display, replay viewer. Non-authoritative.
- **All clients** — humans, bots, neural agents, future multiplayer peers use the same command validation and execution pipeline.

See `docs/architecture/00_system_architecture.md`.

## Originality Statement

This is **not** Widelands, OpenRA, Command & Conquer, or Red Alert. We use original placeholder names, rules, data, balance, and assets. Genre conventions (hex maps, production chains, base building) are fair game; protected identity is not.

## Mechanic Coverage Definition

"All functionality" means: **all major mechanic families represented by simplified, original, testable systems.** See `docs/design/01_mechanic_coverage_contract.md`.

## Development Model

Thirteen autonomous Cursor runs (`docs/runs/RUN_00` through `RUN_12`) each deliver a major milestone with tests, smoke verification, and a final report. Test-first development is mandatory.

## Key Documents

| Area | Location |
|------|----------|
| Requirements | `docs/design/02_`, `03_`, `04_` |
| Architecture | `docs/architecture/` |
| Testing | `docs/testing/` |
| Run plans | `docs/runs/` |
| Cursor rules | `.cursor/rules/` |
| Glossary | `docs/design/10_glossary.md` |
