# Visual Identity and UI Language (RUN_02.5)

Extends [`06_mobile_2d_visual_design.md`](06_mobile_2d_visual_design.md). RUN_02.5 targets a
**1920×1080 desktop debug/observer client** — legible placeholder UI, not final production art.

## Overall UI Feel

- **Map-first:** the hex map occupies ~70% width; panels stack on the right
- **Settlement-RTS inspiration (patterns only):** bottom/side command strip, pop-up detail windows,
  economy panels, ware tables, production status, layer toggles, compact status bars, legends,
  selected-object inspector — inspired by genre conventions, **not** copied from Widelands/Settlers
- **Calm debug chrome:** dark panel backgrounds, high-contrast labels, minimal decoration
- **Non-authoritative:** all gameplay meaning comes from snapshot JSON and rollout sidecars

## Visual Hierarchy

1. Map viewport (terrain, entities, overlays)
2. Selection highlight + command target marker (client-only)
3. Top status bar (scenario, policy, seed, tick, step)
4. Timeline transport controls
5. Specialist panels (action, metrics, event log)
6. Mode toggles + legend
7. Collapsible debug snapshot JSON

## Map-First Principle

A reviewer should answer “what is on the map?” within **3 seconds** without opening panels.
Tick, policy type, and step index appear in the status bar. Command context appears in the
action panel without obscuring the map.

## Colour Language

| Element | Colour / style | Meaning |
|---------|----------------|---------|
| Grass terrain | `#4a8c3a` fill | Passable ground |
| Forest / tree density | `#2d5a27` darker green | Harvestable forest |
| Ownership (p1) | Blue tint `#4488cc` @ 25% | Friendly territory |
| Ownership (p2) | Red tint `#cc4444` @ 25% | Hostile territory |
| Roads | Brown `#8b6914` edges | Road network |
| Construction site | Amber dashed hex `#d4a017` | Incomplete building |
| Carrier / route | Amber `#ffaa00` | Ware in transit |
| Friendly unit | Blue rectangle + label | Own military unit |
| Hostile unit | Red rectangle + label | Enemy unit |
| Selection | White/cyan ring | Non-authoritative pick |
| Policy command target | Magenta crosshair | Command target from rollout row |
| Rejected command | Red result text | Validator reject |
| Accepted command | Green result text | Validator accept |
| Diagnostics stall | Orange overlay badge | From `diagnostics` only |

## Icon / Shape / Label Language

- **Buildings:** filled rectangles with 2–4 letter type code (e.g. `LY`, `SM`, `WH`)
- **Units:** smaller rectangles with squad type abbreviation
- **Carriers:** circle on road node + ware letter
- **Workers:** `W#` badge on assigned building (no map movement — sim assigns to buildings)
- **Progress:** arc or bar from snapshot `progress` / `production_progress` fields only

## Text Size Expectations

- Status bar: **16–18 px** minimum
- Panel headers: **14 px** bold
- Panel body / event log: **13 px** minimum
- Legend entries: **12 px** minimum (readable at 1080p in screenshots)
- Debug JSON: **11 px** monospace, collapsible

## Panel / Menu Principles

- Right-side vertical stack; no modal blocking map during rollout review
- Debug snapshot panel **collapsed by default**
- Previous command and current/next decision **explicitly labelled** (see design 14 alignment rule)
- Mode buttons toggle layer presets; they do not change sim state

## Widelands / Settlers Influence (Inspiration Only)

**May inspire interaction patterns:**

- Map-first view, ware/stock tables, production windows, layer toggles, inspector pop-ups

**Must not copy:**

- Assets, icons, exact menus, menu text, code, styling, layout pixel-for-pixel

## Placeholder Art

Coloured rectangles and text badges communicate type and ownership until RUN_11 final art.
Every colour in the legend must appear in at least one demo screenshot.

## Static Screenshot Pass Criteria

From a single 1920×1080 screenshot (World mode, step > 0), a reviewer must read:

- Scenario name or variant
- Policy type
- Step index and tick
- Non-blank map with terrain
- At least one building or unit (when present in snapshot)
- Status bar text legible without zoom

Optional in screenshot: previous command text (if action panel visible in capture crop).

## Rule IDs

| ID | Requirement |
|----|-------------|
| VIS-ID-001 | Map-first layout at 1920×1080 |
| VIS-ID-002 | Colour language documented and used in legend |
| VIS-ID-003 | Placeholder shapes communicate entity type |
| VIS-ID-004 | No Widelands asset/code copying |
