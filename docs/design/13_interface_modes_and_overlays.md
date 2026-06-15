# Interface Modes and Overlays (RUN_02.5)

Eight toggleable modes/overlays for the trial viewers and neural rollout observer. v1 implements
**World, Economy, Military, Neural, Debug** presets; others are documented for future runs.

## Mode Template

Each mode defines: purpose, user question, required/optional/hidden information, snapshot fields,
panels, map overlays, legend, screenshot pass criteria.

---

## 1. World View

| Field | Content |
|-------|---------|
| Purpose | Default map reading |
| User question | “What is on the map right now?” |
| Required | Terrain, buildings, roads, units, ownership, selection, tick |
| Optional | Coord grid, forest density |
| Hidden / de-emphasised | Raw JSON, policy metrics |
| Snapshot fields | `display_layers`, `buildings[]`, `roads[]`, `entities[]`, `tick` |
| Panels | Status bar, legend, inspector on click |
| Overlays | Selection ring only |
| Screenshot pass | Map non-blank; tick visible; ≥1 terrain hex |

**v1:** Yes — default preset.

---

## 2. Economy / Logistics View

| Field | Content |
|-------|---------|
| Purpose | Production and ware flow |
| User question | “Is production flowing or stalled?” |
| Required | Wares, production progress, construction progress, stockpiles, carriers/routes |
| Optional | Stalled jobs from `diagnostics`, missing inputs |
| Hidden | Military fog, attack overlays |
| Snapshot fields | `buildings[]`, `carriers[]`, `ware_routes[]`, `diagnostics.economy` |
| Panels | Summary wares, build checklist pattern, logistics event filter |
| Overlays | Route lines, carrier markers, production pulse |
| Screenshot pass | Route or building visible; summary panel shows ware counts if exported |

**v1:** Yes — enables logistics layer + diagnostics.

---

## 3. Construction View

| Field | Content |
|-------|---------|
| Purpose | Build sites and delivery progress |
| User question | “What is being built and where?” |
| Required | Construction sites, progress %, delivered/required |
| Optional | Road placement to site |
| Hidden | Military combat overlays |
| Snapshot fields | `construction_sites[]`, `buildings[].progress` |
| Panels | Build checklist, site inspector |
| Overlays | Dashed site hex, progress ring |
| Screenshot pass | Site hex visible when scenario has construction |

**v1:** Deferred as separate mode — construction visible in **Economy** preset.

---

## 4. Military View

| Field | Content |
|-------|---------|
| Purpose | Combat and force disposition |
| User question | “Who owns what, who is fighting?” |
| Required | Unit ownership, type, HP, military buildings, orders |
| Optional | Fog/shroud/vision, attack targets, patrol/guard if exported |
| Hidden | Economy ware routes |
| Snapshot fields | `entities[]`, `buildings[]`, `intel`, `diagnostics.military` |
| Panels | Credits/power summary, attack target highlight |
| Overlays | HP bars, fog layer, hostile markers |
| Screenshot pass | Unit or military building visible in military scenarios |

**v1:** Yes.

---

## 5. Command / Selection View

| Field | Content |
|-------|---------|
| Purpose | Available actions for actor |
| User question | “What can the actor do next?” |
| Required | `available_commands[]`, selection inspector |
| Optional | Command history |
| Hidden | Policy training metrics |
| Snapshot fields | `available_commands[]` (historical in rollout replay) |
| Panels | Command list (live viewer); action panel (rollout observer) |
| Overlays | Command target highlight |

**v1:** Partial — rollout observer shows recorded commands in action panel, not live command picker.

---

## 6. Neural Rollout Observer View

| Field | Content |
|-------|---------|
| Purpose | Policy behaviour diagnosis |
| User question | “What did the policy choose and did it work?” |
| Required | Policy type, previous command, current/next decision, result, reward/score, no-op/reject counts |
| Optional | Comparison markers, confidence (only if exported) |
| Hidden | Live inference |
| Sidecar fields | `metadata.json`, `commands.jsonl`, `metrics.json` |
| Panels | Action panel (labelled), metrics, timeline |
| Overlays | Command target marker |
| Screenshot pass | Policy type + step + command text legible |

**v1:** Yes.

---

## 7. Debug / Snapshot View

| Field | Content |
|-------|---------|
| Purpose | Raw state inspection |
| User question | “What does the raw snapshot say?” |
| Required | Selected snapshot JSON subset, replay/run id, event details |
| Optional | Available command count |
| Hidden | Pretty map overlays optional toggle off |
| Snapshot fields | Full debug snapshot |
| Panels | Collapsible JSON tree |
| Screenshot pass | JSON panel shows tick + variant keys |

**v1:** Yes — collapsible panel.

---

## 8. Visual Legibility Review View

| Field | Content |
|-------|---------|
| Purpose | Screenshot vs JSON audit |
| User question | “Does the screenshot match the JSON?” |
| Required | Sidecar `{step}_state.json`, review markdown |
| Implementation | Phase C capture tooling + markdown reports, not in-app mode v1 |

**v1:** Documented only; capture via `run_rollout_review_capture.gd`.

---

## v1 Mode Preset Mapping

| Mode | Layer toggles (summary) |
|------|-------------------------|
| World | terrain, buildings, units, roads, ownership |
| Economy | + logistics, forest density, diagnostics economy |
| Military | units, buildings, fog, HP bars, diagnostics military |
| Neural | World + command target overlay + action/metrics panels emphasized |
| Debug | all layers + JSON panel open |

Implementation: `InterfaceModePresets.gd` applies `layer_visibility` dict per mode.
