# Godot Viewer Test Plan

Scope: RUN_05 (trial viewers), RUN_02.7 (economy logistics viewer observability), RUN_02.8 (bot-vs-bot economy viewer), RUN_09 (hybrid observer), RUN_10 (human play polish).

> **RUN_02.7:** Functional viewer observability of logistics mechanics (road nodes, ports, carriers, jobs, diagnostics) is in scope — driven by snapshot/export only, not polish. See [`docs/runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md`](../runs/RUN_02_7_ECONOMY_LOGISTICS_FOUNDATION.md) Phase 9.

> **RUN_02.8:** Bot-vs-bot economy match observability — ownership overlay, dual scoreboard, both players' logistics entities visible, deterministic contact sheet. See [`docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md`](../runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md) Phase 7.

## Three-Track Verification (RUN_05)

RUN_05 verification runs on **three tracks**; all are required and must not be collapsed:

- **Track 1 — Automated headless tests/smokes.** Prove the snapshot/command/availability
  *contracts*: snapshot loads, renderer accepts snapshot data, sim-provided `available_commands`
  flow to the viewer, commands round-trip through the validator, and the client derives no rules.
  These do **not** prove the viewport shows a map.
- **Track 1.5 — Viewport pixel observability (non-headless).** Proves the SubViewport renders
  hex terrain and visible changes on step/command via pixel probes and capture assert
  (GOD-SMOKE-004, GOD-CAPTURE-001). Requires display; do not pass `--headless`.
- **Track 2 — Windowed visual review.** A human launches each demo at 1920x1080 and confirms
  readability/legibility per `docs/verification/local_verification.md` ("RUN_05 windowed visual
  review"). Screenshots are captured (capture runner with `--assert` or manual).

## Test Categories

### Snapshot Loading (VIS-SNAP-001)

- Load valid economy snapshot JSON → all layers instantiate from snapshot fields only
- Load military snapshot → units, buildings, and fog/shroud/vision/intel layer present
- Load hybrid snapshot → combined layers without error (RUN_09)
- Invalid schema_version → clear error message, no crash
- Renderer reads real exported field names (e.g. `anchor_q`/`anchor_r`), tolerates
  missing/empty layers without crashing, and treats the loaded snapshot as read-only

### Command Availability Bridge (VIS-AVAIL-001)

- The viewer obtains `available_commands` from the authoritative sim (snapshot
  `available_commands[]` or the sim-owned `list_available_commands(state, actor_id)` provider)
- The available-command count matches the sim provider output (size, or enabled-filtered size)
- The client does **not** synthesize or filter legality on its own (only display grouping/sorting)
- `available_commands[]` is non-authoritative: confirm it is excluded from replay-hashed state (or
  uses a separate client/debug export path) and that no replay hash changed because of it

### Viewport Pixel Observability (VIS-LAYOUT-001, VIS-PIX-*, GOD-CAPTURE-001)

Non-headless SubViewport tests prove the **2D view** shows terrain and changes on step/command — not only render dictionaries.

| ID | Scenario | Pass criteria |
|----|----------|---------------|
| VIS-LAYOUT-001 | Economy scene boot in SubViewport | MapViewport size ≥ 400×300; terrain count > 0; ≥5% map-panel pixels non-background |
| GOD-CAPTURE-001 | Static screenshot assert | Grass + forest (economy) or unit/building colors (military) in left map region |
| VIS-PIX-CHAIN-001 | Production demo step 30 | Viewport image diff, forest hex diff, or amber route pixels |
| VIS-PIX-BUILD-001 | Build tutorial place site | Construction site pixels at placed hex; progress diff after 30 ticks |
| GOD-SMOKE-004 | Combined pixel runner | All above pass via `run_viewer_pixel_smoke.gd` |

Runner: `sim/tools/run_viewer_pixel_smoke.gd` (no `--headless`). Capture assert: `run_viewer_capture.gd -- --variant=economy --assert`.

### Layer Visibility (VIS-LAYER-001)

- Toggle terrain, roads, buildings independently
- Construction progress bar matches snapshot progress field
- Carrier sprite position matches snapshot coordinates

### Hex Grid (VIS-GRID-001)

- Grid off by default on mobile export settings
- Toggle on shows axial grid aligned with terrain

### Construction / Carrier Render Extraction (VIS-RENDER-001)

- Economy demo exposes `construction_sites` and `carriers` in render data (carriers resolved via `road_nodes` → q/r)
- Site progress percent computed from delivered/required (economy) or progress/required_ticks (military)
- Military place-building increases visible construction-site render count

### Command Round-Trip (VIS-CMD-001)

- Select a sim-provided available command → submit through `CommandValidator` → new snapshot →
  visual update; snapshot refreshes after each command
- **Before/after delta:** accepted road/build command increases render counts (roads or construction_sites); rejected command leaves render counts unchanged
- Accepted command appears in the command history / event log with its result
- Rejected/invalid command shows the readable `CommandResult` reason in UI
- No visual change on rejected command except the error/result display
- Each viewer can submit at least three legal commands and advance/pass where the sim supports it

### Diagnostics Overlay (VIS-DIAG-001)

- Stall reasons from snapshot diagnostics displayed
- Power unpowered buildings highlighted (hybrid)

### Replay Timeline (VIS-REP-001)

- Step forward/back through replay snapshots
- Hash at end matches the simulation core's replay runner

## Test Methods

| Method | When |
|--------|------|
| Godot headless unit tests | Snapshot parser, layer counts (visual client) |
| Integration | Visual client + in-process simulation core (Godot headless) |
| Manual | Touch UX RUN_10 |

## Non-Goals

- Authoritative gameplay-rule tests here (those belong in the simulation-core test harness, `sim/tests/`); this plan covers the **visual client** only

## Pass Criteria for RUN_05

Track 1 (headless):
- [x] Economy viewer loads curated demo snapshot with carriers, tree density, and logistics layers (VIS-SNAP-001, VIS-RENDER-001, VIS-ECON-CHAIN-001)
- [x] Build tutorial flow: place site → road → deliver → complete (VIS-ECON-BUILD-001)
- [x] Military viewer loads curated demo snapshot (VIS-SNAP-001)
- [x] Command availability bridge sources commands from the sim provider (VIS-AVAIL-001)
- [x] Command round-trip with before/after render delta (VIS-CMD-001)
- [x] `available_commands[]` excluded from replay-hashed state; no hashes re-pinned for it
- [x] GOD-SMOKE-001 (progression within 50 ticks), GOD-SMOKE-002 (move + attack combat delta), GOD-SMOKE-003 (economy logistics visibility) pass

Track 1.5 (viewport pixels — non-headless, mandatory for RUN_05 closure):
- [x] VIS-LAYOUT-001: hex map visible on first open (MapViewport sized, non-blank pixels)
- [x] GOD-CAPTURE-001: capture asserts grass/forest (economy) or entity colors (military)
- [x] VIS-PIX-CHAIN-001: stepping production demo changes viewport pixels
- [x] VIS-PIX-BUILD-001: placing storehouse shows construction site in viewport
- [x] GOD-SMOKE-004 passes (`run_viewer_pixel_smoke.gd`, exit 0)

Track 2 (windowed visual review, per `docs/verification/local_verification.md`):
- [x] Economy and military demos are readable at 1920x1080 (map/panels/text do not overlap)
- [x] Legend explains every colour/icon/overlay; selection inspector understandable
- [x] Screenshots captured (capture runner `--assert`) and referenced in [`RUN_05_FINAL_REPORT.md`](../audit/RUN_05_FINAL_REPORT.md)

---

## RUN_02.5 Rollout Observer Tests (VIS-ROLL-*)

Scope: neural rollout artefact loading, timeline playback, alignment rule. See
`docs/design/14_neural_rollout_observer_design.md` and `docs/testing/06_visual_legibility_review.md`.

| ID | Type | Pass criteria |
|----|------|---------------|
| VIS-ROLL-001 | Unit | Rollout schema validation (Python + GDScript) |
| VIS-ROLL-001b | Unit | `len(snapshots) == len(commands) + 1`; export/loader indexing |
| VIS-ROLL-002 | Unit | `RolloutLoader` loads metadata + snapshot count |
| VIS-ROLL-003 | Unit | Step forward/back updates snapshot + both action panels (no off-by-one) |
| VIS-ROLL-004 | Unit | Missing optional fields tolerated without crash |
| VIS-ROLL-005 | Smoke | Headless observer loads committed fixture rollout |
| GOD-ROLL-001 | Capture | Non-headless screenshot non-blank + step diff |

Fixture: `godot_project/sim/tests/fixtures/visual_rollouts/minimal_economy/` (2–3 commands → 3–4 snapshots).

Runners:

```bash
# Headless rollout smoke
$GODOT --headless --path godot_project --script res://sim/tools/run_rollout_observer_smoke.gd

# Capture (non-headless)
$GODOT --path godot_project --script res://sim/tools/run_rollout_review_capture.gd \
  -- --rollout-path=... --steps=0,10
```

---

## RUN_02.8 Bot-vs-Bot Economy Viewer Review (planned)

Scope: deterministic two-player economy expansion match — functional observability only, not polish.
Authority: [`docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md`](../runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md).

### Required viewer capabilities

| Capability | Pass criteria |
|------------|---------------|
| Ownership overlay | Human can tell p1 vs p2 for buildings, roads, road nodes, carriers, construction sites without reading JSON |
| Dual scoreboard | Side panel shows `diagnostics.economy_scores` for p1 and p2 from snapshot/export |
| Both economies visible | Both players' buildings, roads, nodes, carriers, construction sites render with distinct ownership |
| Logistics overlay | RUN_02.7 baseline: nodes, ports, jobs, carriers, blocked jobs (logistics layer) |
| Contact sheet | Deterministic panels: **start / early / mid / late / final** + logistics overlay; `contact_sheet.html` + PNG manifest |

**Viewer anti-cheat:** Ownership overlay and scoreboard must use snapshot/export fields only — no client-fabricated player state.

### Planned tests

| ID | Type | Pass criteria |
|----|------|---------------|
| VIS-BVB-001 | Unit / render-data | Ownership overlay layer includes p1/p2 tint on roads, nodes, carriers, sites |
| VIS-BVB-002 | Unit / render-data | Scoreboard render-data or summary text includes both `economy_scores` |
| GOD-SMOKE-005 | Smoke (planned) | Headless render-data smoke for ownership + scoreboard fields from bot-vs-bot snapshot |
| GOD-BVB-CAPTURE-001 | Capture (non-headless) | Contact sheet panels generated; manifest JSON lists expected panel ids |

### Visual review runner (planned)

```bash
$GODOT --path godot_project --script res://sim/tools/run_economy_bvb_visual_review.gd
```

Expect PNG panels + `capture_manifest.json` + `contact_sheet.html` under project-local export path (see `local_verification.md` §RUN_02.8).

### Pass criteria for RUN_02.8 viewer closure

- [ ] Ownership overlay acceptance met
- [ ] Dual scoreboard from `diagnostics.economy_scores`
- [ ] Contact sheet with all required panels + logistics overlay
- [ ] GOD-SMOKE-005 or equivalent headless smoke passes (when implemented)
- [ ] Visual review bundle referenced in RUN_02.8 final report

## RUN_05C — Economy Spectator Visual Groundwork (VIS-SPECTATOR-*)

Scope: placeholder sprites, render-data bridge, activity panels, start→end delta verification. **Production-chain demo only** for automated gates; dual-bot is manual diagnostic.

| ID | Type | Pass criteria |
|----|------|---------------|
| VIS-SPECTATOR-001 | Unit | Production chain, 30 ticks, `meaningful_count >= 2` start→end deltas (not static sprites) |
| VIS-SPECTATOR-002 | Unit | No carrier/construction sprites without matching render markers; stepping changes sprite set or chain fingerprint |
| VIS-SPECTATOR-003 | Unit | Mock atlas loads ≥15 named animations |
| VIS-SPECTATOR-004 | Unit | `measure_activity_after_steps(30)` passes smoke gate with non-sprite delta |
| GOD-SMOKE-ECON-SPECTATOR-001 | Smoke | `run_economy_spectator_smoke.gd` — same gate as 001 |
| GOD-CAPTURE-ECON-SPECTATOR | Capture (non-headless) | `run_economy_spectator_capture.gd -- --assert` — tick 0/30 PNGs, `image_pixel_delta > 5` |

Runner filter:

```bash
TEST_FILTER=vis_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

**Not in scope:** `ECON_BOT_002`, `BOT-SMOKE-004`, `ECON-SMOKE-007`. See `docs/audit/RUN_05C_VISUAL_GROUNDWORK_CONTINUATION.md`.
