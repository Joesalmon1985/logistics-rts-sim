# Godot Viewer Test Plan

Scope: RUN_05 (trial viewers), RUN_09 (hybrid observer), RUN_10 (human play polish).

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
