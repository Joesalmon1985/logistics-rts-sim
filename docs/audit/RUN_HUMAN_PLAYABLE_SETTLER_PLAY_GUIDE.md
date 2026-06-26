# Human-Playable Settler MVP — Play Guide

## Launch

From the repository root:

```bash
./scripts/run_game.sh
```

Tutorial (same binary; pick **Tutorial** on the menu, or):

```bash
./scripts/run_tutorial.sh
```

Optional desktop entry:

```bash
./scripts/create_desktop_launcher.sh
```

Requires Godot 4.5.x. Set `GODOT` if the binary is not at the default Linux path.

## Main menu

| Button | Action |
|--------|--------|
| **Tutorial** | Guided 11-step settlement walkthrough |
| **Free Play / Settler MVP** | Full scenario without tutorial overlay |
| **Debug Viewers** | Economy/military debug demos |
| **Quit** | Exit |

## Core loop

1. **Pause** on start — read the objective panel (planks + stone at HQ).
2. **Click the map** to inspect terrain, Headquarters, roads, carriers, or construction sites. The right-hand **inspector** uses plain English (no raw type codes).
3. **Build menu** — icon buttons for Road, Woodcutter, Forester, Sawmill, Quarry, Cancel. Active mode is highlighted; a **mode banner** explains what to do next.
4. **Road mode** — click a networked hex, then an adjacent destination.
5. **Build mode** — move over valid terrain; ghost shows valid/invalid placement; click to place.
6. **Play / Pause / Step** — time controls in the top bar. Space toggles play; `.` steps one tick while paused.

## Keyboard shortcuts

| Key | Action |
|-----|--------|
| Space | Play / Pause |
| `.` or Enter | Step one tick (paused) |
| Esc | Cancel build / road mode |
| 1–4 | Woodcutter, Forester, Sawmill, Quarry |
| R | Road mode |
| WASD | Pan map |
| +/- | Zoom |

## Tutorial (11 steps)

1. Welcome — read instructions, press **Next**
2. Pan/zoom the map
3. Click Headquarters — inspector shows stored goods
4. Press **Play** — watch ticks advance
5. Road mode — extend network toward forest
6. Place Woodcutter on forest near roads
7. Forester (optional renewal)
8. Sawmill on grass near roads
9. Quarry on rock
10. Deliver materials to complete sites
11. Victory when objective met (10 planks + 10 stone at HQ)

**Skip / Exit to Menu** ends the tutorial safely.

## Objective

Connect production buildings to Headquarters by roads and store **10 planks** and **10 stone** at HQ.

## Sprites

Map and HUD use **original generated placeholders** only (`client/assets/settler_mvp/generated/`). Kenney reference assets are not used in-game.
