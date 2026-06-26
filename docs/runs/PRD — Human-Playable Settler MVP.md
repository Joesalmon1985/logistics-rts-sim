# PRD — Human-Playable Settler MVP

Branch suggestion: `feature/human-playable-settler-mvp`
Base branch: current `feature/playable-settler-mvp` if not merged, otherwise `main`
Goal: make the existing Settler MVP understandable, guided, visually readable and easy to launch
Primary player outcome: a human can launch the game, choose tutorial mode, understand what objects are, build roads/buildings, inspect things, and complete the starter economy objective without needing debug knowledge

---

## 1. Context

The Settler MVP now launches and can be played at a basic level. However, manual testing shows it is still not really understandable as a game.

Current human-facing problems:

1. The player can press keys such as `1` or `R`, but it is not clear what mode they have entered or what to do next.
2. Clicking on buildings, tiles or workers does not provide enough useful information.
3. There are no clear submenus, contextual panels or object explanations.
4. The player is not guided through how to build roads, buildings, or a working economy.
5. The game is hard to run because it requires knowing the Godot command line.
6. Existing visuals are too abstract; available downloaded sprites are not yet being used properly, and more original placeholder sprites are needed.

This milestone must turn the project from a debug-playable prototype into a **minimum viable human-playable game**.

The authoritative simulation remains in `godot_project/sim/`. The client remains non-authoritative. Do not expand bots, training, military, hybrid gameplay, multiplayer or economy depth in this run.

---

## 2. Product definition

Create a human-playable Settler MVP with:

* a proper main menu;
* tutorial mode;
* free-play Settler MVP mode;
* debug-viewer access;
* readable sprites/icons for terrain, buildings, workers/carriers, roads, wares and construction sites;
* clear build/road controls;
* contextual selection/inspection panels;
* guided tutorial steps;
* easy launch script and optional desktop launcher;
* screenshot-based visual verification proving the UI is visible and legible.

The result should feel like an early playable prototype, not a developer harness.

---

## 3. Architecture guardrails

* Authoritative game rules stay in `godot_project/sim/`.
* UI/client code stays in `godot_project/client/`.
* Python remains tooling/training only.
* Do not create a Python game.
* Do not invent client-side legality rules.
* Build/road actions must still route through the existing command path:

  * UI/controller action;
  * `CommandSubmitter`;
  * `CommandValidator`;
  * authoritative sim state;
  * refreshed snapshot.
* Tutorial logic may guide the player and read snapshot state, but it must not mutate gameplay state directly except through normal player commands.
* Selection panels may format snapshot data, but must not recalculate hidden sim rules.
* Visual sprites/icons may be placeholders, but they must be original or properly licensed.
* Do not copy Widelands, C&C, OpenRA, Red Alert or other protected assets, names, UI, maps, factions, campaigns, sounds or balance.

---

## 4. Non-goals

Do not work on:

* neural training;
* bot improvement;
* `ECON_BOT_002`;
* military gameplay;
* hybrid RTS gameplay;
* multiplayer;
* procedural map generation unless already trivial;
* save/load unless already trivial;
* final art polish;
* sound/music;
* mobile packaging;
* complex economy expansion beyond the existing starter chain.

This is a human-playability and clarity run.

---

## 5. Phase 0 — inspect current branch and assets

Before editing:

```bash
git status --short
git branch --show-current
git log --oneline -5
```

If there are unexplained uncommitted changes, stop and report.

Inspect the current playable implementation:

```text
godot_project/client/scenes/main_menu.tscn
godot_project/client/scenes/main_menu.gd
godot_project/client/scenes/settler_mvp_game.tscn
godot_project/client/scenes/settler_mvp_game.gd
godot_project/client/core/settler_mvp_controller.gd
godot_project/client/ui/map_viewport.gd
godot_project/client/ui/
godot_project/sim/data/config/buildings.json
godot_project/sim/data/config/wares.json
godot_project/sim/data/config/recipes.json
godot_project/sim/data/scenarios/economy/settler_mvp.json
godot_project/sim/tools/capture_settler_mvp_screenshot.gd
godot_project/sim/tools/capture_settler_mvp_interaction_screenshots.gd
```

Inspect existing assets:

```bash
find . \( -iname "*.png" -o -iname "*.jpg" -o -iname "*.jpeg" -o -iname "*.webp" -o -iname "*.svg" -o -iname "*.aseprite" \) -not -path "*/.godot/*"
```

Create a short asset inventory before changing visuals:

```text
docs/audit/RUN_HUMAN_PLAYABLE_SETTLER_ASSET_INVENTORY.md
```

Inventory must say:

* what image/sprite assets already exist;
* where they came from if known;
* whether the licence/source is known;
* which assets are safe to use;
* which are ignored because licence/source is unclear;
* which new placeholder sprites need to be generated.

Do not use any existing downloaded asset unless its licence/source is clear enough for a personal open-source prototype. If licence is unclear, leave it unused and document it.

---

## 6. Phase 1 — visual asset pass

### 6.1 Asset registry

Create or update an asset registry, for example:

```text
godot_project/client/assets/asset_registry.json
```

It should map game concepts to sprite/icon files:

```json
{
  "buildings": {
    "headquarters": "...",
    "woodcutter": "...",
    "forester": "...",
    "sawmill": "...",
    "quarry": "...",
    "construction_site": "..."
  },
  "logistics": {
    "road": "...",
    "road_node": "...",
    "carrier": "...",
    "builder": "..."
  },
  "wares": {
    "timber": "...",
    "planks": "...",
    "stone": "...",
    "builder_work": "..."
  },
  "terrain": {
    "grass": "...",
    "forest": "...",
    "rock": "...",
    "water": "..."
  }
}
```

Use repo conventions if an asset mapping system already exists.

### 6.2 Required readable sprites/icons

By the end of this run, the player should visually distinguish:

* headquarters;
* woodcutter;
* forester;
* sawmill;
* quarry;
* construction site;
* road;
* road node / flag;
* carrier;
* builder/worker;
* timber/logs;
* planks;
* stone;
* grass/buildable tile;
* forest/tree tile;
* rock/quarryable tile;
* selected object highlight;
* invalid placement marker;
* valid placement ghost.

### 6.3 Generate missing placeholder sprites

If safe downloaded sprites are missing or incomplete, generate simple original placeholder spritesheets.

Acceptable generation approach:

* deterministic script;
* simple pixel/vector-style sprites;
* original geometry and colours;
* no copyrighted references;
* no copied Widelands/OpenRA/C&C visual identity.

Suggested script:

```text
tools/generate_settler_placeholder_sprites.py
```

or a Godot tool script if that fits the repo better.

Suggested output:

```text
godot_project/client/assets/settler_mvp/generated/
```

Suggested sizes:

* 64×64 icons for HUD/build buttons;
* 96×96 or 128×128 building sprites;
* simple 4-frame carrier/worker spritesheets if low-cost;
* simple terrain/ware icons.

Generated assets should be committed only if they are small and useful. Add source notes to the asset inventory.

### 6.4 Visual rendering acceptance

The map should no longer look like anonymous debug shapes where avoidable. It must clearly show:

* what is a building;
* what is a construction site;
* what is a road;
* what is a worker/carrier;
* what is forest/rock;
* what the player has selected;
* what mode the player is in.

If an object still uses a debug marker, document it as a known issue.

---

## 7. Phase 2 — contextual selection and inspection panel

The player must be able to click things and understand them.

Implement a permanent context panel or popup inspector. A permanent right-side panel is preferred for MVP because it is easier to test and less likely to hide the map.

Suggested name:

```text
SelectionInspectorPanel
```

or use the existing inspector if present.

### 7.1 Selection behaviour

Clicking on the map should select the most relevant object at that position.

Selection priority:

1. UI buttons if clicked directly.
2. Worker/carrier if visible and clickable.
3. Completed building.
4. Construction site.
5. Road node / flag.
6. Road segment.
7. Terrain hex.

If multiple things are on one hex, show a small selection list or cycle target on repeated click. If that is too large for this run, use the priority order and document cycling as future work.

### 7.2 Inspector content by selection type

#### Terrain hex

Show:

* name/type: grass, forest, rock, water, etc.;
* hex coordinates;
* buildable: yes/no;
* terrain/resource notes;
* tree density if present;
* deposit type if present;
* existing road/building/site if present;
* useful action hints, e.g. “Good place for a Woodcutter nearby” if supported by snapshot/authoritative preview.

#### Headquarters

Show:

* name: Headquarters;
* role: stores goods and connects the settlement;
* current stored wares relevant to the MVP;
* connected roads/nodes;
* workers/carriers if available;
* objective relevance.

#### Building

Show:

* building name and type;
* role/explanation in plain English;
* status: working, idle, waiting, blocked, complete, under construction;
* input wares;
* output wares;
* local storage if available;
* production recipe if available;
* road connection status;
* current blocked/waiting reason;
* worker assigned if available.

Example:

```text
Sawmill
Turns timber into planks for construction.

Status: Waiting for timber
Inputs: timber 0/1
Outputs: planks
Road connection: connected to HQ
Tip: Build or connect a Woodcutter to supply timber.
```

#### Construction site

Show:

* planned building;
* construction status;
* required materials;
* delivered materials;
* missing materials;
* builder work remaining if available;
* road connection status;
* reason blocked/waiting;
* action hint.

Example:

```text
Construction Site: Woodcutter
Waiting for materials.

Delivered:
- planks: 1 / 2
- stone: 0 / 1

Road connection: connected
Tip: Press Play to let carriers deliver materials.
```

#### Road / road node

Show:

* road or flag/node;
* connected neighbours;
* whether connected to HQ;
* if road placement mode is active, explain next step.

#### Worker/carrier

Show:

* worker/carrier name/type;
* current task if available;
* carrying ware if available;
* origin/destination if available;
* status: moving, idle, blocked.

If the snapshot does not expose enough worker detail, show what is available and list missing fields in the final report.

### 7.3 Inspector acceptance

* Clicking a building updates the inspector.
* Clicking a construction site updates the inspector.
* Clicking an empty/terrain hex updates the inspector.
* Clicking HQ updates the inspector.
* Clicking road/road node updates the inspector if pickable.
* Inspector text is human-readable, not raw JSON.
* Inspector still works while paused.
* Inspector updates after ticks/commands.

---

## 8. Phase 3 — visible build and road submenus

The player must never be in an invisible mode.

### 8.1 Build menu

Create or improve a build palette with visible buttons and icons:

* Woodcutter;
* Forester;
* Sawmill;
* Quarry;
* Road mode;
* Cancel.

Each button should show:

* icon;
* label;
* short tooltip/help text;
* active/selected visual state;
* disabled state if not currently possible.

When a building is selected:

* show an active mode banner: `Placing Woodcutter`;
* show instructions: `Move over the map, then click a valid hex. Esc cancels.`;
* show ghost preview on the map;
* show valid/invalid reason near cursor or in status panel;
* left click valid hex submits command;
* left click invalid hex shows reason;
* Esc/right-click cancels.

### 8.2 Road mode

Road mode must show:

* active banner: `Road mode`;
* instruction: `Click a start point, then click destination.`;
* after first click: highlight start;
* before second click: show candidate path if available;
* after second click: submit missing road segments;
* if rejected: show first authoritative reason;
* if successful: show message and refreshed road.

### 8.3 Keyboard and mouse parity

Keyboard shortcuts may remain, but the UI must also be usable by mouse.

Required:

* clicking build buttons works;
* clicking road mode works;
* clicking cancel works;
* keyboard 1–4/R/Esc still works;
* active mode displayed consistently whichever input path is used.

---

## 9. Phase 4 — tutorial mode

Add a guided tutorial mode from the main menu.

Main menu should contain:

```text
Tutorial
Free Play / Settler MVP
Debug Viewers
Quit
```

The tutorial may use the same map/scenario as Settler MVP or a smaller tutorial-specific scenario.

Suggested scene/controller:

```text
godot_project/client/scenes/settler_tutorial_game.tscn
godot_project/client/scenes/settler_tutorial_game.gd
godot_project/client/core/settler_tutorial_controller.gd
```

If simpler, reuse `settler_mvp_game.tscn` with `tutorial_enabled = true`.

### 9.1 Tutorial system

Create a client-side tutorial step controller. It can read snapshot/objective state and UI state, but must not directly mutate sim state except via normal commands.

Each tutorial step should have:

* title;
* instruction text;
* completion condition;
* optional highlighted UI element;
* optional highlighted map hex/object;
* optional “Next” button only where appropriate;
* “Skip Tutorial” or “Exit to Menu”.

Tutorial state may be client-side. It is guidance, not authoritative gameplay.

### 9.2 Minimum tutorial steps

The tutorial must guide a new player through:

1. Welcome and objective

   * Explain that this is a road-and-carrier settlement game.
   * Objective: build Woodcutter, Forester, Sawmill and Quarry, connect them to HQ, store 10 planks and 10 stone.

2. Camera controls

   * Ask player to pan and zoom.
   * Detect camera moved/zoomed.

3. Inspect Headquarters

   * Ask player to click HQ.
   * Detect HQ selected.
   * Explain the inspector panel.

4. Start time

   * Explain pause/play/step.
   * Ask player to press Play or Space.
   * Detect tick advances.
   * Pause again or allow continuing.

5. Road mode basics

   * Ask player to enter road mode.
   * Ask player to connect from HQ toward a target area.
   * Detect at least one new road/road node placed.

6. Build Woodcutter

   * Ask player to select Woodcutter.
   * Highlight suitable forest-adjacent area if possible.
   * Detect construction site or completed woodcutter.

7. Build Forester

   * Explain forests can be renewed.
   * Detect Forester construction site or completion.

8. Build Sawmill

   * Explain timber becomes planks.
   * Detect Sawmill construction site or completion.

9. Build Quarry

   * Explain rock/stone.
   * Detect Quarry construction site or completion.

10. Watch economy run

* Explain carriers, construction, production and waiting reasons.
* Ask player to press Play and watch stock/objective progress.

11. Complete objective

* Detect objective complete.
* Show tutorial victory/completion message.

### 9.3 Tutorial acceptance

* Tutorial mode can be started from main menu.
* Tutorial instruction panel is visible.
* Current step is clear.
* Player can progress through first three steps without guessing.
* Tutorial detects at least camera movement, HQ selection and play/tick advance.
* Build/road steps have visible guidance.
* Tutorial can be exited.
* Tutorial completion or objective victory is displayed.

If full end-to-end tutorial completion is too large, the first run must at least complete steps 1–6 and document remaining tutorial steps. Do not pretend incomplete steps are done.

---

## 10. Phase 5 — easy launch

The game should not require the player to remember a long Godot command.

### 10.1 Run script

Add a root-level script:

```text
scripts/run_game.sh
```

Requirements:

* executable;
* detects or uses `$GODOT`;
* if `$GODOT` is not set, tries the known local path:

```text
/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
```

* sets `GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"` unless already set;
* launches the project:

```bash
"$GODOT" --path godot_project
```

* prints a clear error if Godot is not found.

Also add:

```text
scripts/run_tutorial.sh
```

if Godot supports passing a scene/mode argument cleanly. If not, the game should open to main menu and Tutorial should be one click away.

### 10.2 Desktop launcher

Add an optional helper:

```text
scripts/create_desktop_launcher.sh
```

It should create a local desktop launcher such as:

```text
~/.local/share/applications/logistics-rts-sim.desktop
```

or optionally `~/Desktop/logistics-rts-sim.desktop`.

The launcher should run `scripts/run_game.sh`.

Do not hard-code the repo path unless the script generates it from the current working directory. If a `.desktop` template is committed, use placeholders or generate it.

### 10.3 Optional exported executable

If Godot export templates are available, optionally add/export a Linux build. Do not block this milestone on exports.

Primary acceptance is the run script and optional desktop launcher, not a packaged release.

### 10.4 Launch acceptance

* From repo root, `./scripts/run_game.sh` opens the main menu.
* If possible, double-click desktop launcher opens the main menu.
* Main menu offers Tutorial, Free Play/Settler MVP, Debug Viewers, Quit.
* Documentation explains both command-line and launcher options.

---

## 11. Phase 6 — visual screenshot verification

Cursor must visually test what it builds.

Use the existing screenshot capture approach and extend it for this run.

### 11.1 Required screenshots

Create screenshot tools or extend existing ones to capture:

```text
docs/audit/screenshots/human_mvp_00_main_menu.png
docs/audit/screenshots/human_mvp_01_tutorial_start.png
docs/audit/screenshots/human_mvp_02_hq_selected_inspector.png
docs/audit/screenshots/human_mvp_03_build_menu_open.png
docs/audit/screenshots/human_mvp_04_woodcutter_placement_preview.png
docs/audit/screenshots/human_mvp_05_road_mode_active.png
docs/audit/screenshots/human_mvp_06_building_or_site_selected.png
docs/audit/screenshots/human_mvp_07_free_play_hud.png
```

Create JSON/MD review summaries alongside them:

```text
docs/audit/screenshots/human_mvp_visual_review.json
docs/audit/screenshots/human_mvp_visual_review.md
```

### 11.2 Visual review checks

The review summary should confirm:

* screenshot file exists;
* screenshot is not blank;
* main menu visible;
* Tutorial button visible;
* Free Play button visible;
* Debug Viewers button visible;
* tutorial instruction panel visible;
* map visible;
* HUD visible;
* build palette visible;
* inspector visible;
* objective panel visible;
* time controls visible;
* active mode banner visible when in build/road mode;
* selection highlight visible;
* selected-object inspector text changes after selection;
* building/terrain sprites are visible;
* no major right-side panel is off-screen at 1920×1080;
* no full-screen HUD element blocks map input.

### 11.3 Screenshot acceptance

The run is not complete unless screenshot artefacts prove the menu, tutorial, inspector and build/road modes are visible.

If live GUI testing is unavailable, report that honestly and rely on in-engine screenshots. If a live display is available, also launch the game window manually and capture/report the result.

---

## 12. Phase 7 — tests

Add tests to the existing Godot test manifest. Use repo conventions.

### 12.1 Visual asset tests

Add tests proving:

* asset registry loads;
* required concepts have sprite/icon paths;
* files exist;
* missing licence/source notes are documented;
* map renderer uses sprite/icon mapping where appropriate;
* fallback debug shapes are used only when asset mapping is missing and logged.

Suggested test:

```text
test_human_mvp_asset_registry.gd
```

### 12.2 Selection/inspector tests

Add tests proving:

* click/select HQ updates inspector;
* click/select terrain updates inspector;
* click/select construction site updates inspector;
* click/select completed building updates inspector;
* inspector shows human-readable labels, not raw JSON;
* inspector works while paused;
* inspector refreshes after command/tick.

Suggested tests:

```text
test_human_mvp_selection_inspector_hq.gd
test_human_mvp_selection_inspector_building.gd
test_human_mvp_selection_inspector_terrain.gd
test_human_mvp_selection_inspector_construction_site.gd
```

### 12.3 Build/road UI tests

Add tests proving:

* build buttons are visible and clickable;
* selected build mode changes banner/status;
* valid placement preview appears;
* invalid placement reason appears;
* road mode banner appears;
* first road click records start;
* second road click submits route or shows reason;
* cancel exits mode.

Suggested tests:

```text
test_human_mvp_build_menu_visible.gd
test_human_mvp_build_mode_feedback.gd
test_human_mvp_road_mode_feedback.gd
```

### 12.4 Tutorial tests

Add tests proving:

* tutorial mode launches from main menu;
* tutorial instruction panel visible;
* first tutorial step visible;
* camera step can complete;
* HQ selection step can complete;
* play/tick step can complete;
* road/build guidance step appears;
* tutorial can be exited.

Suggested tests:

```text
test_human_mvp_tutorial_launch.gd
test_human_mvp_tutorial_first_steps.gd
test_human_mvp_tutorial_exit.gd
```

### 12.5 Launch script tests

Add a lightweight shell/test check:

* `scripts/run_game.sh` exists;
* is executable;
* prints helpful error if Godot missing;
* can launch Godot project if `$GODOT` is set;
* desktop launcher script exists and generates a valid `.desktop` file.

Do not require actual GUI launch in a headless CI-style test, but document live/manual result.

### 12.6 Screenshot tests

Add tests or smoke scripts proving:

* human MVP screenshot sequence can be generated;
* PNGs exist and are non-blank;
* review JSON/MD exists;
* required UI sections are reported present.

---

## 13. Verification commands

Use the local Godot path unless overridden:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

Suggested fast verification:

```bash
"$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
"$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
"$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
"$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
"$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

Add or update screenshot verification commands:

```bash
"$GODOT" --headless --path godot_project --script res://sim/tools/capture_human_mvp_screenshots.gd
```

Manual launch:

```bash
./scripts/run_game.sh
```

Optional desktop launcher:

```bash
./scripts/create_desktop_launcher.sh
```

Do not run or block on known hanging bot tests unless explicitly requested. Keep the known `ECON_BOT_002` issue quarantined and documented.

---

## 14. Manual acceptance checklist

After implementation, manually verify:

```text
Launch:
- ./scripts/run_game.sh opens the game.
- Main menu appears.
- Tutorial button works.
- Free Play/Settler MVP button works.
- Debug Viewers button works or is clearly marked.

Visual clarity:
- Buildings have recognisable sprites/icons.
- Roads are visible.
- Construction sites are distinguishable.
- Workers/carriers are visible where snapshot supports them.
- Terrain/resources are distinguishable.
- Selected object is visibly highlighted.

Controls:
- Help panel explains controls.
- Play/Pause/Step/Speed work.
- Pan/zoom work.
- Mouse build buttons work.
- Keyboard shortcuts still work.
- Road mode is understandable.

Inspector:
- Clicking HQ shows useful information.
- Clicking terrain shows useful information.
- Clicking a building/site shows useful information.
- Inspector explains status, inputs, outputs, missing materials and road connection where available.

Tutorial:
- Tutorial tells the player what to do.
- Tutorial can guide camera movement.
- Tutorial can guide HQ inspection.
- Tutorial can guide starting simulation time.
- Tutorial can guide building/road basics.
- Tutorial can be exited.

Screenshots:
- Main menu screenshot is correct.
- Tutorial screenshot is correct.
- Inspector screenshot is correct.
- Build mode screenshot is correct.
- Road mode screenshot is correct.
- Visual review MD/JSON exists.

Known issues:
- List honestly.
```

Do not mark the run complete unless a human can understand the basic loop without being told out-of-band what keys to press.

---

## 15. Documentation

Update or create:

```text
docs/runs/RUN_HUMAN_PLAYABLE_SETTLER_MVP.md
docs/audit/RUN_HUMAN_PLAYABLE_SETTLER_ASSET_INVENTORY.md
docs/audit/RUN_HUMAN_PLAYABLE_SETTLER_PLAY_GUIDE.md
docs/audit/screenshots/human_mvp_visual_review.md
docs/audit/screenshots/human_mvp_visual_review.json
```

Update README with a short “How to play” section:

```text
To launch the game:
./scripts/run_game.sh

Then choose:
- Tutorial, or
- Free Play / Settler MVP
```

Keep README concise. Put detailed instructions in the play guide.

---

## 16. Definition of done

This run is complete when:

* main menu is usable;
* tutorial mode exists;
* free-play Settler MVP still works;
* debug viewers remain accessible;
* sprites/icons make buildings, roads, terrain and key objects visually distinct;
* clicking objects updates a human-readable inspector;
* build and road modes have clear submenus/status/preview;
* tutorial guides a new player through the first playable actions;
* `./scripts/run_game.sh` launches the game;
* optional desktop launcher can be created;
* screenshot artefacts prove menu/tutorial/inspector/build/road UI are visible;
* tests cover assets, inspector, build/road UI, tutorial launch/steps, screenshot capture and launcher script;
* existing relevant sim/viewer/smoke tests still pass;
* known hanging bot tests remain quarantined and documented;
* final report is honest about any remaining gaps.

---

## 17. Final report format

Use the repo standard final report:

```text
Branch:
Summary:
Files changed:
Tests run:
Results:
Known issues:
Unimplemented requirements:
Uncommitted changes:
Suggested next run:
```

Also include:

```text
Manual playability result:
Screenshot artefacts:
Visual clarity result:
Inspector result:
Tutorial result:
Launch script / desktop launcher result:
Known remaining usability issues:
```

Commit and push when complete.

