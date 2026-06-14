# Godot Viewer Test Plan

Scope: RUN_05 (trial viewers), RUN_09 (hybrid observer), RUN_10 (human play polish).

## Test Categories

### Snapshot Loading (VIS-SNAP-001)

- Load valid economy snapshot JSON → all layers instantiate
- Load military snapshot → units and intel layer present
- Load hybrid snapshot → combined layers without error
- Invalid schema_version → clear error message, no crash

### Layer Visibility (VIS-LAYER-001)

- Toggle terrain, roads, buildings independently
- Construction progress bar matches snapshot progress field
- Carrier sprite position matches snapshot coordinates

### Hex Grid (VIS-GRID-001)

- Grid off by default on mobile export settings
- Toggle on shows axial grid aligned with terrain

### Command Round-Trip (VIS-CMD-001)

- UI action → command JSON → Python → new snapshot → visual update
- Rejected command shows reason in UI
- No visual change on rejected command except error toast

### Diagnostics Overlay (VIS-DIAG-001)

- Stall reasons from snapshot diagnostics displayed
- Power unpowered buildings highlighted (hybrid)

### Replay Timeline (VIS-REP-001)

- Step forward/back through replay snapshots
- Hash at end matches Python replay runner

## Test Methods

| Method | When |
|--------|------|
| GUT/unit tests in Godot | Snapshot parser, layer counts |
| Integration | Godot headless + Python subprocess |
| Manual | Touch UX RUN_10 |

## Non-Goals

- Gameplay rule tests in GDScript (those belong in Python pytest)

## Pass Criteria for RUN_05

- [ ] Economy viewer loads tutorial snapshot
- [ ] Military viewer loads skirmish snapshot
- [ ] Command round-trip for one command type each
- [ ] GOD-SMOKE-001, GOD-SMOKE-002 pass
