# Expansion Design

Scope: RUN_11 — expanded content and progression. Not implemented in early runs.

---

## Campaign Structure

- Linear or branching missions using scenario config files
- Each mission specifies map, starting state, victory/defeat rules
- Missions may restrict available buildings or wares for pacing

---

## Progression

| Type | Example |
|------|---------|
| Building unlocks | Research lab unlocks advanced military units |
| Map unlocks | Complete mission 1 → mission 2 available |
| Difficulty tiers | Same map, tighter stall timeout or stronger opponent bot |

Progression state stored outside sim in player profile (RUN_11); sim remains deterministic per scenario.

---

## Content Packs

- Additional maps (hex layout JSON)
- Additional ware chains (new recipe config)
- Additional units/buildings (catalogue IDs + config)
- All content data-driven; no hard-coded sim branches per mission

---

## Rule IDs

| ID | Requirement |
|----|-------------|
| EXP-CAMP-001 | Campaign mission loader from config |
| EXP-PROG-001 | Unlock gating without breaking determinism |
| EXP-MAP-001 | At least 3 additional maps |
| EXP-PACK-001 | Content pack schema versioned |

---

## Non-Goals for RUN_11

- Multiplayer (RUN_12)
- Neural training content (RUN_06 uses trial scenarios)
