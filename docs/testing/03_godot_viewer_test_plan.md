# Godot Viewer Test Plan

Scope: RUN_05 (trial viewers), RUN_09 (hybrid observer), RUN_10 (human play polish).

## Two-Track Verification (RUN_05)

RUN_05 verification runs on two tracks; both are required and must not be collapsed:

- **Track 1 — Automated headless tests/smokes.** Prove the snapshot/command/availability
  *contracts*: snapshot loads, renderer accepts snapshot data, sim-provided `available_commands`
  flow to the viewer, commands round-trip through the validator, and the client derives no rules.
  These do **not** prove visual readability.
- **Track 2 — Windowed visual review.** A human launches each demo at 1920x1080 and confirms
  readability/legibility per `docs/verification/local_verification.md` ("RUN_05 windowed visual
  review"). Screenshots are captured (in-viewer button/hotkey or manual).

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

### Layer Visibility (VIS-LAYER-001)

- Toggle terrain, roads, buildings independently
- Construction progress bar matches snapshot progress field
- Carrier sprite position matches snapshot coordinates

### Hex Grid (VIS-GRID-001)

- Grid off by default on mobile export settings
- Toggle on shows axial grid aligned with terrain

### Command Round-Trip (VIS-CMD-001)

- Select a sim-provided available command → submit through `CommandValidator` → new snapshot →
  visual update; snapshot refreshes after each command
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
- [ ] Economy viewer loads curated demo snapshot (VIS-SNAP-001)
- [ ] Military viewer loads curated demo snapshot (VIS-SNAP-001)
- [ ] Command availability bridge sources commands from the sim provider (VIS-AVAIL-001)
- [ ] Command round-trip for at least one command type each, with snapshot refresh (VIS-CMD-001)
- [ ] `available_commands[]` excluded from replay-hashed state; no hashes re-pinned for it
- [ ] GOD-SMOKE-001, GOD-SMOKE-002 pass

Track 2 (windowed visual review, per `docs/verification/local_verification.md`):
- [ ] Economy and military demos are readable at 1920x1080 (map/panels/text do not overlap)
- [ ] Legend explains every colour/icon/overlay; selection inspector understandable
- [ ] Screenshots captured (in-viewer or manual) and referenced in the final report
