# RUN_05 — Godot 2D Trial Game Debug Viewers

## Purpose

Build **human-readable, step-through Godot 2D visual debugging clients** (`godot_project/client/`)
for the economy and military trial games. The viewer renders authoritative snapshots in-process
and submits commands back to the authoritative GDScript simulation core (`sim/`, from RUN_01–04).
No Python runtime is involved.

RUN_05 is a **debug/review client**, not a finished game UI. The goal is that a human can load a
curated demo, read the game state clearly at 1920x1080, submit legal commands sourced from the
sim, and step the game forward — to inspect and verify behaviour visually.

## Branch Name

`feature/run-05-godot-2d-viewers-trial-games`

## Precondition

RUN_04 must be merged into `main` first (done: `b04a13e`). Branch RUN_05 from the updated `main`
so it includes `SimPaths`, the Windows portability fixes, replay/bot tooling, and RUN_04 docs.

## Prerequisite Documents

- `docs/audit/RUN_05_PRESTART_LESSONS_FROM_RUN_04.md`
- `docs/audit/RUN_04_LESSONS_LEARNED.md`
- `docs/design/06_mobile_2d_visual_design.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/architecture/03_godot_adapter_contract.md`
- `docs/testing/03_godot_viewer_test_plan.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)

## Pre-flight

Before building viewer code, rerun prior verification per `docs/verification/local_verification.md`
(RUN_01–04 tests + smokes + failure-mode integrity), all exit 0, no `SCRIPT ERROR`. Use the Godot
console exe; check `$LASTEXITCODE`; set `GODOT_USER_DATA_DIR`. If anything fails, stop and report.

## In-Scope Work

- `godot_project/client/` economy and military viewers (scenes / renderers / adapters / UI).
- Snapshot loader, layer renderer, selection inspector, command submitter.
- **Sim-side command availability provider** (see contract below) — a small authoritative
  addition under `sim/`, not `client/`.
- Curated economy and military demo scenarios/snapshots.
- Run-forward / step-through controls.
- Placeholder art (coloured rectangles / simple icons).
- Tests: VIS-SNAP-001, VIS-CMD-001, command-availability bridge test; smokes GOD-SMOKE-001/002.
- Documented windowed visual-review path + screenshots.

**RUN_02 carryover:** Economy display layers and per-building diagnostics (`ECON-SNAP-002`,
`ECON-DIAG-001`) render from snapshot JSON only — do not re-derive logistics in `client/`.

**RUN_03 carryover:** Military snapshot layers (fog/shroud/vision, units, buildings, diagnostics)
from `MilitarySnapshotExporter` render from snapshot JSON only — never recompute fog, vision,
combat, or pathfinding in `client/`.

## Command Availability Contract (new)

The viewer must source legal/available commands from authoritative `sim/`, in this order:

1. Snapshot JSON includes an `available_commands` array emitted by the sim/exporter.
2. The viewer adapter calls a sim-owned method such as `list_available_commands(state, actor_id)`.
3. If neither exists yet, RUN_05 adds the **minimal sim-side provider** needed for the economy and
   military demo/play-forward use.

Command availability is **sim-owned**: the provider may inspect authoritative state but lives under
`sim/`, not `client/`. The client may display, group, sort, filter, and count these commands, but
must **not** decide legality.

DTO (snapshot/adapter shape):

```json
{
  "id": "build_lumber_yard_3_4",
  "label": "Build Lumber Yard at 3,4",
  "type": "build",
  "actor_id": "p1",
  "target": {"q": 3, "r": 4},
  "payload": {},
  "enabled": true,
  "disabled_reason": null,
  "source": "sim"
}
```

- Minimum requirement: **legal available commands only**. Returning `enabled: false` +
  `disabled_reason` for explainable-but-invalid commands is optional.
- Available command count = `available_commands.size()`, or `available_commands.filter(c =>
  c.enabled).size()` if disabled entries are included.

### Non-authoritative guardrail (must-have)

`available_commands[]` is a **client/debug/export affordance, not authoritative state.** It must
**not** be included in replay-hashed state unless deliberately and explicitly approved. If
`SnapshotExporter` output is consumed by `ReplayHash`, either exclude `available_commands[]` from
the hashed snapshot or expose it via a separate client/debug export path. **Do not re-pin hashes**
merely because command availability was added to a viewer-facing snapshot.

## Human-Legible Viewer Requirements

Desktop-first: target a readable **1920x1080** debug layout. Mobile design may inform future
scalability but must not compromise desktop readability in this run.

Each viewer (economy and military) must include:

- Large, readable map area.
- Zoom and pan controls if practical.
- Optional coordinate / hex / tile labels (toggle).
- Layer toggle panel.
- Legend explaining every colour, icon, and overlay.
- Click or hover inspector for tile / unit / building.
- Right-side details panel showing the selected object's snapshot data.
- Event/status panel: current turn/tick, active player/faction, phase/priority, last command and
  command result.
- Snapshot debug panel: snapshot source, variant, turn/tick.
- Curated demo snapshot (economy and military).
- Documented screenshot/review process.

## Run-Forward Viewer Requirements

The viewer must let a human advance each trial game as a step-through debugging tool (not polished
tutorial UX). Required controls:

- Load / reset demo snapshot.
- Submit selected legal command (from `available_commands`).
- Pass / advance turn or priority where the sim supports it.
- Step forward one tick / turn / action.
- Refresh snapshot after every command.
- Show command success/failure clearly.
- Show command history / event log.
- Show current available-command count.

Legal/available commands come from the authoritative sim only. The viewer must not invent legality.

## Allowed vs Forbidden Display Logic

The client **may**: format snapshot values for display; group snapshot fields into panels; count
visible snapshot entities for summaries; apply colours/icons based on explicit snapshot fields;
sort/filter entities for display; show warnings already present in snapshot diagnostics.

The client **must not**: recalculate economy logistics; recalculate fog/shroud/vision; recalculate
combat outcomes; recalculate pathfinding; decide command legality except by calling the
authoritative sim path; hold independent authoritative game state.

## Proposed Reusable Components (`godot_project/client/`)

- `ViewerRoot` — main layout container (1920x1080 debug layout).
- `MapViewport` — map canvas (pan/zoom, optional grid/labels).
- `LayerTogglePanel` — per-layer visibility.
- `LegendPanel` — colour/icon/overlay legend.
- `SelectionInspectorPanel` — selected tile/unit/building snapshot data.
- `CommandPanel` — lists `available_commands`, submits selected, shows result.
- `EventLogPanel` — command history + event log + last result.
- `SnapshotDebugPanel` — snapshot source/variant/tick + available-command count.
- Demo/review launcher — boots economy/military demo scenes.

Adapters under `client/adapters/`: `snapshot_loader.gd`, `layer_renderer.gd`,
`command_submitter.gd`, `command_availability.gd` (calls the sim provider; never derives legality).
Separate economy/military viewer scenes may share adapter/renderer code.

## Verification — Two Tracks

### Track 1 — Automated headless verification (CI/smoke confidence)

Covers snapshot loading, renderer data acceptance, command-availability bridge, command
submission, no client-side rule derivation, and basic scene/script load where possible:
VIS-SNAP-001, VIS-CMD-001, command-availability test, GOD-SMOKE-001, GOD-SMOKE-002. This track
does **not** prove visual readability. Per `docs/testing/03_godot_viewer_test_plan.md` and the
RUN_05 row of `docs/testing/02_smoke_test_matrix.md`.

### Track 2 — Windowed visual review (human readability at 1920x1080)

Documented in `docs/verification/local_verification.md` ("RUN_05 windowed visual review"). Launch
each demo scene in a real Godot window at 1920x1080 and confirm: map large/readable; panels do not
overlap the map; text readable at desktop scale; legend explains colours/icons/overlays; layer
toggles visible; selection inspector understandable; command panel shows sim-provided commands;
command result/errors visible; event log shows recent history; turn/tick/player/phase/priority
visible; economy and military demos are immediately understandable.

### Screenshots

Preferred: a lightweight in-viewer screenshot button or debug hotkey saving to
`user://screenshots/run_05_economy_demo.png` and `user://screenshots/run_05_military_demo.png`. If
that is too much for RUN_05, document manual screenshot instructions instead. The final report must
state whether screenshots were captured automatically or manually.

### Process safety

Windowed runs can leave stale processes. Use the console Godot exe; check for existing Godot
processes before launching (`Get-Process | Where-Object { $_.ProcessName -like "*Godot*" }`); never
broad-kill unrelated Godot processes without reporting first; document any manual close/cleanup if
a windowed run fails to exit cleanly.

## Acceptance Checklist

- [ ] Economy snapshot renders terrain, resources, buildings, logistics/diagnostics and
      per-building diagnostics from JSON only.
- [ ] Military snapshot renders terrain, units, buildings, fog/shroud/vision/intel and diagnostics
      from JSON only.
- [ ] Viewer is readable at 1920x1080; map, panels and text do not overlap.
- [ ] Every icon, colour and overlay is explained by an on-screen legend.
- [ ] Tile/unit/building selection shows snapshot-derived details.
- [ ] `available_commands` are sourced from the authoritative sim provider (snapshot or API); the
      client does not derive legality; available-command count is shown.
- [ ] `available_commands[]` is non-authoritative and excluded from replay-hashed state; no hashes
      were re-pinned for it.
- [ ] Economy viewer can load a demo, submit at least three legal commands, advance/pass as needed,
      and show updated snapshots after each step.
- [ ] Military viewer can load a demo, submit at least three legal commands, advance/pass as
      needed, and show updated snapshots after each step.
- [ ] Invalid command attempts show readable validation errors (from `CommandResult`).
- [ ] Event log records each submitted command and its result.
- [ ] VIS-SNAP-001 and VIS-CMD-001 pass; command-availability bridge test passes.
- [ ] GOD-SMOKE-001 and GOD-SMOKE-002 pass.
- [ ] No gameplay logic in the visual client beyond display (rules stay in `sim/`).
- [ ] Final report includes headless test commands + results, windowed-review launch commands,
      whether visual review was performed, screenshot paths/instructions, known readability issues,
      and any stale Godot process/cleanup notes.

## Out-of-Scope Work

- Authoritative GDScript gameplay rules (stay in `sim/`).
- Client-side command-legality derivation (sim-owned only).
- Making the viewer feel like a finished game UI — RUN_05 is a debug/review client, not the final
  player UX (no animation polish, transitions, responsive mobile UI, or art polish).
- Hybrid viewer (RUN_09).
- Human tutorial polish (RUN_10).
- Final art (RUN_11).
- Mobile export/packaging (RUN_12).
- Neural training (RUN_06).
- Mobile-layout work that compromises desktop readability in this run.

## Recommended Implementation Sequence

1. Sim-side command availability provider + DTO (under `sim/`).
2. Viewer-facing snapshot export path (keep `available_commands[]` out of hashed state).
3. Curated economy/military demo scenarios.
4. Static map render from snapshot.
5. Layer toggles, legend, selection inspector.
6. Command panel using `available_commands[]`.
7. Submit command through `CommandValidator`.
8. Refresh snapshot after command.
9. Event/status/history panels.
10. Headless viewer tests/smokes (Track 1).
11. Windowed 1920x1080 review and screenshots (Track 2).

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`, plus the RUN_05 verification additions above
(headless results, windowed-review commands, screenshot paths/instructions, readability issues,
process-safety notes).
