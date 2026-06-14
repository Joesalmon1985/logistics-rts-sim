# Project Brief

## Vision

Build an **original mobile Godot strategy game** that combines settlement logistics with RTS military gameplay. The final product is a hybrid where military construction and production depend on the logistics economy, and player military control is delegated through buildings and priorities rather than individual unit micro.

## Three-Phase Roadmap

| Phase | Name | Purpose |
|-------|------|---------|
| 1 | Trial Economy Game | Permanent abstract economy engine on a hex map |
| 2 | Trial Military Game | Standalone RTS trial with direct unit control |
| 3 | Hybrid Game | Composition of economy + military + integration rules |

Each phase is implemented headlessly in the GDScript simulation core first, then visualised in the Godot client.

## Architecture Summary

- **Godot/GDScript simulation core** — authoritative, headless-capable, deterministic, seeded RNG, integer/fixed-point state, events, snapshots, replay. Pure GDScript, separate from UI/rendering.
- **Godot visual client/UI** — mobile 2D viewer, command submission, diagnostics display, replay viewer. **Non-authoritative.**
- **Python** — training, analysis, and tooling only; drives the core via Godot headless. Never the shipped runtime; never embedded in the mobile app.
- **All clients** — humans (via UI), bots, neural agents, future multiplayer peers use the same command validation and execution pipeline.

See `docs/architecture/00_system_architecture.md` and the ratified decisions in `docs/design/11_design_decision_register.md`.

## Originality Statement

This is **not** Widelands, OpenRA, Command & Conquer, or Red Alert. We use original placeholder names, rules, data, balance, and assets. Genre conventions (hex maps, production chains, base building) are fair game; protected identity is not.

## Mechanic Coverage Definition

"All functionality" means: **all major mechanic families represented by simplified, original, testable systems** — for economy, **Widelands-like coverage without Widelands feature parity**. See `docs/design/01_mechanic_coverage_contract.md`.

## Development Model

Thirteen autonomous Cursor runs (`docs/runs/RUN_00` through `RUN_12`) each deliver a major milestone with tests, smoke verification, and a final report. Test-first development is mandatory. Large runs (RUN_02/03/07) aim to complete in one run, with a single controlled A/B continuation allowed only at a coherent boundary (DDR-028).

## Key Documents

| Area | Location |
|------|----------|
| Requirements | `docs/design/02_`, `03_`, `04_` |
| Architecture | `docs/architecture/` |
| Testing | `docs/testing/` |
| Run plans | `docs/runs/` |
| Cursor rules | `.cursor/rules/` |
| Glossary | `docs/design/10_glossary.md` |
| Design decision register | `docs/design/11_design_decision_register.md` |
