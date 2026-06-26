# Settler MVP — Play Guide

## Launch

1. Open the Godot project or run:
   ```bash
   export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
   "$GODOT" --path godot_project --resolution 1920x1080
   ```
2. From the main menu choose **New Settler MVP Game**.

Alternative: `res://client/scenes/settler_mvp_game.tscn` directly.

## Objective

Build a working starter economy:

- Complete **Woodcutter Lodge**, **Forester Camp**, **Sawmill**, and **Quarry**
- Connect each to **Headquarters** by roads
- Store at least **10 planks** and **10 stone** in your buildings (HQ counts)

The objective panel tracks each requirement. Victory appears when all are met.

## Placing buildings

1. Click a build button (Woodcutter, Forester, Sawmill, Quarry).
2. Move the cursor over the map — green = legal, red = illegal (reason shown).
3. Click a valid hex to place a construction site.
4. Press **Cancel** or **Esc** to exit build mode.

Terrain hints:

- Woodcutter / Forester: forest hexes with trees
- Sawmill: grass
- Quarry: rock hexes with revealed deposit

## Placing roads

1. Click **Road**.
2. Click a start hex on your existing road network (near HQ).
3. Click a destination hex — the game auto-routes along existing roads and places only missing segments.

## Time controls

- **Play** / **Pause** — run or stop simulation ticks
- **1x / 2x / 4x** — game speed
- **Step** — advance one tick (debug)

## Reading progress

- **Stock** — timber, planks, stone, tools in your economy
- **Warnings** — blocked construction, missing inputs, delivery issues
- **Inspector** — click any hex for details
- **Event log** — recent actions and messages

## Blocked buildings

Common messages:

- `Waiting for planks/stone/tools` — construction materials still delivering
- `No road connection to headquarters` — extend roads to the site
- `Sawmill waiting for timber` — woodcutter not producing or not connected
- `Quarry needs rock or quarryable terrain nearby` — wrong placement

Toggle logistics overlays on the map (roads, carriers, routes) by using the default map layers.

## Victory

When the objective panel shows all checks complete, a **Victory** banner appears. There is no failure timer — take your time.

## Debug viewers

From the main menu, **Open Debug Viewers** launches the economy command-list harness (`economy_viewer_demo.tscn`) for developers.
