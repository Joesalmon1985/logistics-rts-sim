# RUN_00 — Cursor Bootstrap

## Purpose

Create project rules, design contracts, architecture documents, testing documents, and run plans for autonomous Cursor development. No gameplay implementation.

## Branch Name

`feature/run-00-cursor-bootstrap`

## Prerequisite Documents

None (first run).

## In-Scope Work

- 10 `.cursor/rules/*.mdc` files
- 11 `docs/design/*.md` files (including gate requirements 02, 03, 04)
- 7 `docs/architecture/*.md` files
- 6 `docs/testing/*.md` files
- 13 `docs/runs/*.md` files
- `README.md`, `.gitignore`
- Git init, feature branch, commit, push

## Out-of-Scope Work

- Python simulation code, pytest, Godot project
- Trial economy, military, hybrid gameplay
- Neural training, model weights
- Any reference-game code or assets

## Test-First Requirements

Documentation-only run. No tests invented or executed.

## Implementation Requirements

- Gate files meet minimum depth: 02 ≥150 lines, 03/04 ≥120 lines
- Rule IDs in gate docs and test matrix
- Glossary authored before other design docs
- No TODO-only documents

## Smoke-Test Requirements

None for RUN_00.

## Acceptance Checklist

- [ ] All 49 bootstrap files exist with usable content
- [ ] `02_trial_economy_game_requirements.md` ≥150 lines with rule IDs, commands, events, DoD
- [ ] `03_trial_military_game_requirements.md` ≥120 lines
- [ ] `04_hybrid_game_requirements.md` ≥120 lines
- [ ] Mechanic families traceable: design → test matrix → run checklist
- [ ] `git diff --check` clean
- [ ] Committed and pushed to feature branch
- [ ] Final report delivered

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
