# RUN_02.5 — Visual Improvements and Neural Rollout Observer

**Status:** in progress — branch `feature/run-02-5-visual-improvements-neural-observer`

## Purpose

The RUN_05 Godot 2D viewer works as a debug/review client, but visual state is still hard to
understand. RUN_02.5 defines a visual identity and interface language, then implements enough
of that design to let a human **watch neural agents** performing existing economy and military
test scenarios.

The goal is not prettier UI alone. The goal is that a human can watch trained, untrained, random,
and/or heuristic agents play existing deterministic scenarios and **understand what is happening
on screen**.

**Called RUN_02.5 conceptually** (fills a visual-design gap after early foundation runs), but
**technically branches from current `main` after RUN_05 and RUN_06**.

## Branch Name

`feature/run-02-5-visual-improvements-neural-observer`

## Prerequisite Documents

- `docs/design/00_project_brief.md` through `docs/design/06_mobile_2d_visual_design.md`
- `docs/design/10_glossary.md`, `docs/design/12_visual_identity_and_ui_language.md`
- `docs/design/13_interface_modes_and_overlays.md`, `docs/design/14_neural_rollout_observer_design.md`
- `docs/architecture/02_command_event_snapshot_contract.md`, `03_godot_adapter_contract.md`, `04_bot_and_training_architecture.md`
- `docs/testing/03_godot_viewer_test_plan.md`, `docs/testing/04_training_test_plan.md`, `docs/testing/06_visual_legibility_review.md`
- `docs/runs/RUN_02` through `RUN_06`, `docs/audit/RUN_02` through `RUN_06` lessons learned
- `docs/verification/local_verification.md`

## Entry Conditions

- RUN_05 and RUN_06 merged into `main`
- Prior verification green (tier-1 before Phase A; full suite before Phase B and at completion)
- Clean working tree at branch creation

## Scope

### Phase A — Documentation

- Run doc, visual identity, interface modes, neural observer design, legibility review process, assessment report

### Phase B — Implementation

- Visual rollout artefact export from existing neural evaluation scenarios
- Godot rollout observer (load folder, timeline, panels, mode toggles)
- Screenshot capture for legibility review

### Phase C — Visual legibility review

- Generate required rollouts, capture screenshots + sidecar JSON, write review report

## Out-of-Scope

- Final game art or polished consumer UI
- Live neural inference inside Godot (v1 uses saved artefacts)
- Neural training from viewport pixels
- Judging model quality from screenshots alone
- Copying Widelands UI/assets/code
- Hybrid-game implementation
- New gameplay mechanics
- Client-side rule derivation

## Guardrails

- Neural training/evaluation uses authoritative snapshot/state data only
- The 2D viewer is for human observation, review, debugging — **not authoritative**
- Viewer must not invent command legality, economy logic, combat, pathfinding, or diagnostics
- Do not fake policy confidence if not exported
- Do not commit model weights, checkpoints, or generated training artefacts

## V1 Cut-Line

See `docs/audit/RUN_02_5_VISUAL_ASSESSMENT_REPORT.md` and plan v1 section. Minimum:

- Rollout schema + export CLI + minimal fixture
- Godot observer with timeline, status bar, action panels, event log, metrics, five mode toggles
- Screenshot capture for fixed steps

## Implementation Phases

| Phase | Deliverables |
|-------|--------------|
| A | Six docs + assessment report + test plan updates |
| B | Export tooling, observer scene, tests |
| C | Rollouts generated, screenshots, review report |

## Phase A Gate

Do **not** begin Phase B until all six docs exist and the assessment report lists: v1 modes,
rollout schema fields, missing snapshot fields, v1 cut-line, deferred items.

## Expected Artefacts

| Path | Purpose |
|------|---------|
| `docs/runs/RUN_02_5_VISUAL_IMPROVEMENTS_NEURAL_OBSERVER.md` | This run doc |
| `docs/design/12_visual_identity_and_ui_language.md` | Visual language |
| `docs/design/13_interface_modes_and_overlays.md` | Eight interface modes |
| `docs/design/14_neural_rollout_observer_design.md` | Rollout format + observer |
| `docs/testing/06_visual_legibility_review.md` | Phase C process |
| `docs/audit/RUN_02_5_VISUAL_ASSESSMENT_REPORT.md` | Assessment + gaps |
| `python_tools/training/visual_rollout_export.py` | Export CLI |
| `python_tools/training/rollout_artefact.py` | Schema + validation |
| `godot_project/client/adapters/rollout_loader.gd` | Folder loader |
| `godot_project/client/core/rollout_controller.gd` | Timeline controller |
| `godot_project/client/scenes/rollout_observer_demo.tscn` | Observer entry |
| `training_runs/visual_rollouts/<run_id>/` | Gitignored output |

## Test Plan

- VIS-ROLL-001 through VIS-ROLL-005, GOD-ROLL-001 — see `docs/testing/03_godot_viewer_test_plan.md`
- Phase C process — `docs/testing/06_visual_legibility_review.md`
- Existing viewer and training tests must remain green

## Acceptance Checklist

- [ ] Branch created from current main
- [ ] Existing docs reviewed
- [ ] RUN_02.5 run doc created
- [ ] Visual identity document created
- [ ] Interface modes document created
- [ ] Neural rollout observer design document created
- [ ] Visual legibility review document created
- [ ] Widelands-style influence documented as inspiration only
- [ ] Colour language and legend requirements documented
- [ ] Rollout artefact format documented
- [ ] Rollout artefacts generated from existing neural evaluation/test scenarios
- [ ] Godot observer can load a rollout folder
- [ ] Observer can play/pause/step through snapshots
- [ ] Observer shows current policy/scenario/seed/step/tick
- [ ] Observer shows selected action/command/result (previous vs current/next labelled)
- [ ] Observer shows event log and basic metrics
- [ ] Observer supports view modes or overlay toggles
- [ ] Screenshot capture works for rollout review
- [ ] Screenshot/state JSON review report generated
- [ ] Existing viewer tests still pass
- [ ] Existing training tests still pass
- [ ] Generated artefacts, screenshots, checkpoints, and model weights are not committed
- [ ] Final report written

## Rollback / Stop Conditions

- Working tree or branch state unsafe or ambiguous
- Baseline verification fails in a way that invalidates RUN_02.5 work
- Change would invent rules in viewer, make viewer authoritative, or train from pixels
- Would require committing generated rollouts, checkpoints, or weights
- Required file/path missing after search
- Same blocker after multiple documented attempts with no v1 fallback

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.
