# Mobile 2D Visual Design

Godot renders simulation state from JSON snapshots. Godot does not compute gameplay.

---

## Rendering Model

1. Python sim exports snapshot each tick (or on demand)
2. Godot adapter loads snapshot, diff-updates scene nodes
3. Player input serializes commands → sent to Python → next snapshot

See `docs/architecture/03_godot_adapter_contract.md`.

---

## Visual Layer Stack (bottom to top)

| Order | Layer | Source field | Notes |
|-------|-------|--------------|-------|
| 0 | Terrain | `map.terrain`, `map.height`, `map.buildability` |
| 0b | Tree density | `map.tree_density` (forest hexes) |
| 1 | Ownership tint | `map.ownership` |
| 2 | Roads | `roads[]` | Edge sprites |
| 3 | Road nodes | `road_nodes[]` | Junction markers |
| 4 | Deposits | `deposits[]` | Overlay icons |
| 5 | Building footprints | `buildings[]` | Plan vs complete art |
| 6 | Construction progress | `buildings[].progress` | Progress bar overlay |
| 7 | Units / carriers / workers | `entities[]` | Sprites by type |
| 8 | In-transit wares | `carriers[].ware` | Small icon on carrier |
| 9 | Intel fog | `intel[player_id]` | Military/hybrid only |
| 10 | Selection / UI | Godot local | Non-authoritative highlight |
| 11 | Diagnostics overlay | `diagnostics` | Optional debug |
| 12 | Hex grid | toggle | Optional; hidden default on mobile |

---

## Hex Grid Overlay

- **VIS-GRID-001:** Toggle via settings; off by default on mobile
- Axial layout; pointy-top or flat-top consistent with sim coordinate system

---

## Touch UX (High Level)

- Pinch/pan camera
- Tap hex to select; long-press for context (RUN_10 detail)
- Command palette submits commands to Python; no local rule execution

---

## Performance Budgets

| Metric | Target |
|--------|--------|
| Snapshot parse + render | < 16 ms per frame at 60 FPS target |
| Max visible entities | 200 without LOD |
| Snapshot size | < 2 MB JSON typical |

See `.cursor/rules/09-mobile-performance.mdc`.

---

## Art Placeholders

RUN_05 uses coloured rectangles / simple icons. Final art is RUN_11 scope.

---

## Rule IDs

| ID | Requirement |
|----|-------------|
| VIS-SNAP-001 | Load and display all snapshot layers |
| VIS-LAYER-001 | Layer order as specified |
| VIS-GRID-001 | Optional hex grid toggle |
| VIS-CMD-001 | Command submission round-trip |
| VIS-DIAG-001 | Diagnostics overlay from snapshot |
