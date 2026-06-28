# PRD — Tutorial Legibility and Watchable Settler MVP

Branch suggestion: `feature/tutorial-legibility-settler-mvp`
Base branch: `feature/human-playable-settler-mvp`
Purpose: make the game understandable from the screen itself, with visible tutorial text, readable menus, meaningful selection panels, clearer sprites, and basic animations.

---

## 1. Problem statement

The game now launches and has visible map activity, but it is still not human-understandable.

Manual screenshot evidence shows:

* the map is visible;
* sprites/markers exist;
* roads/carriers/buildings appear to exist;
* but there is no obvious tutorial text;
* no clear “what should I do next?” panel;
* no readable selected-object panel;
* no visible build menu or road menu in the screenshot;
* no clear current mode;
* no clear explanation of what workers/buildings/resources are doing;
* sprite labels are tiny/unclear;
* the player cannot tell whether the world is working, stuck, waiting or producing.

This run must treat **visual comprehension** as the product, not just technical interactivity.

The run is not complete until screenshot artefacts prove that tutorial text, menus, inspector text, objective text, mode text, and readable sprites are visible on screen.

---

## 2. Product goal

Create a minimum viable human-understandable version of the Settler MVP where a first-time player can:

1. launch the game easily;
2. choose Tutorial from the main menu;
3. see clear tutorial text immediately;
4. understand the objective;
5. see what to press/click next;
6. inspect a tile/building/worker and read what it is;
7. enter build mode and see an obvious active mode panel;
8. enter road mode and see clear road-building instructions;
9. watch workers/carriers moving goods and understand what they carry;
10. see whether buildings are working, waiting, blocked or under construction;
11. visually understand the basic economy chain:

    * forest/tree source;
    * woodcutter;
    * carrier carrying timber;
    * sawmill;
    * planks;
    * construction/stockpile;
    * quarry/stone.

This milestone is successful only if the game screen itself explains what is happening.

---

## 3. Hard rule: no “invisible UI” completion

Cursor must not report success if the screenshot looks like only a map.

The following must be visible in the required tutorial screenshot at 1920×1080:

* tutorial instruction panel;
* objective panel;
* build palette/menu;
* selected-object inspector or “nothing selected” inspector;
* time controls;
* current mode/status banner;
* help/control hint text;
* readable map with buildings/roads/workers;
* at least one selected object with human-readable explanation.

If any of these are missing, off-screen, covered, empty, too small to read, or only present as hidden nodes, the run is not complete.

---

## 4. Architecture guardrails

* Authoritative rules stay in `godot_project/sim/`.
* Client/UI remains non-authoritative.
* UI actions still go through `CommandSubmitter` → `CommandValidator` → sim.
* Tutorial guidance can read snapshot state and UI state.
* Tutorial guidance must not directly mutate sim state except via normal commands.
* Inspector text may format snapshot/config data.
* Inspector must not invent legality, production or logistics rules.
* Python remains tooling only.
* No bot/training/military/hybrid work.
* No `ECON_BOT_002` work.
* No new economy chains unless strictly needed for explanation.
* No final-art rabbit hole.

---

## 5. Design principle

The screen should answer these questions without external explanation:

* What am I looking at?
* What have I selected?
* What does this thing do?
* What does it need?
* What is it producing?
* Is it connected to roads?
* Is it working or blocked?
* What should I do next?
* How do I build a road?
* How do I build a woodcutter/sawmill/quarry?
* What are the moving workers carrying?
* Is the settlement improving?

If the player cannot answer these from the tutorial screen, continue iterating.

---

## 6. Phase 0 — inspect and create visual baseline

Before editing:

```bash
git status --short
git branch --show-current
git log --oneline -5
```

If unexplained uncommitted changes exist, stop and report.

Create:

```text
docs/runs/RUN_TUTORIAL_LEGIBILITY_SETTLER_MVP.md
```

Capture the current broken/insufficient visual state before fixes:

```text
docs/audit/screenshots/legibility_before_tutorial_start.png
docs/audit/screenshots/legibility_before_review.md
docs/audit/screenshots/legibility_before_review.json
```

The review must explicitly record:

* whether tutorial text is visible;
* whether build menu text is visible;
* whether selected-object inspector text is visible;
* whether objective text is visible;
* whether mode/status text is visible;
* whether workers/carriers have readable identity;
* whether carried goods are visible;
* whether the player can tell what to do next.

This “before” screenshot should be used as the failure case this run is fixing.

---

## 7. Phase 1 — force a readable game layout

The current map consumes the screen while explanatory UI is not visible enough. Create a fixed, readable MVP layout.

At 1920×1080, the game must show:

### Left / centre

* map viewport;
* selected object highlight;
* visible workers/carriers;
* visible road network;
* visible placement ghost when in build mode;
* visible road preview when in road mode.

### Right panel or bottom/right panel

A permanent information panel with:

* tutorial step title and instructions;
* selected-object inspector;
* objective progress;
* stockpile summary;
* current action hint.

### Top or bottom HUD

* Play/Pause;
* Step;
* speed;
* tick;
* current mode;
* help toggle.

### Build palette

A visible panel with icon+text buttons:

* Road;
* Woodcutter;
* Forester;
* Sawmill;
* Quarry;
* Cancel.

The build palette must not be hidden behind keyboard-only shortcuts. Keyboard shortcuts may exist, but visible buttons are required.

### Acceptance

* The map is not allowed to cover or push the explanatory panels off-screen.
* The right/bottom explanatory panel must be visible at 1920×1080.
* Text must be large enough to read in the screenshot.
* Controls must use plain English labels, not internal IDs.
* The layout must be verified by screenshot review.

---

## 8. Phase 2 — tutorial mode must open with visible text

The main menu must include:

```text
Tutorial
Free Play
Debug Viewers
Quit
```

Clicking Tutorial must open the game with tutorial mode enabled.

The first tutorial screen must visibly show:

```text
Welcome to Settler MVP

This is a road-and-carrier settlement game.
Your job is to build a small working economy.

Goal:
1. Connect buildings to Headquarters with roads.
2. Build a Woodcutter, Forester, Sawmill and Quarry.
3. Store 10 planks and 10 stone.

Start by clicking your Headquarters.
```

The exact wording can be improved, but it must include:

* what kind of game it is;
* what the goal is;
* what the first action is;
* how to continue.

### Tutorial panel requirements

The tutorial panel must have:

* title;
* instruction text;
* progress indicator, e.g. `Step 1 of 11`;
* “Next” where appropriate;
* “Skip Tutorial”;
* “Back to Menu” or “Exit Tutorial”;
* highlighted target if applicable.

### Acceptance

* In screenshot `human_understandable_01_tutorial_start.png`, the tutorial text must be clearly visible.
* The visual review JSON must include the actual visible tutorial title and first 80+ characters of instruction text.
* A test must fail if tutorial text is empty, hidden, off-screen, or shorter than a useful minimum.

---

## 9. Phase 3 — object inspector must be human-readable

Clicking something must update a visible inspector panel.

The inspector must always show one of:

* selected terrain;
* selected Headquarters;
* selected building;
* selected construction site;
* selected road/road node;
* selected worker/carrier;
* no selection, with instruction text.

The inspector must not show only internal labels like:

```text
BUILDING
Type: woodcutter
ID: b_123
```

It should show plain English.

### 9.1 Headquarters inspector

Example text:

```text
Headquarters

This is the centre of your settlement.
Carriers collect and deliver goods from here.

Stored goods:
- Timber: 4
- Planks: 6
- Stone: 3

Road connection: connected
Tip: Build roads from here to new buildings.
```

### 9.2 Woodcutter inspector

```text
Woodcutter Lodge

Cuts nearby trees to make timber.
Timber is carried along roads to the Sawmill or construction sites.

Status: Working / Waiting / Blocked
Needs: nearby trees, road connection
Produces: timber
Road connection: connected / not connected
Tip: Build near forest and connect it to Headquarters.
```

### 9.3 Forester inspector

```text
Forester Camp

Plants new trees so the forest can regrow.

Status: Working / Waiting / Blocked
Needs: open forest/grass nearby
Produces: more tree growth
Tip: Build near the Woodcutter to keep timber flowing.
```

### 9.4 Sawmill inspector

```text
Sawmill

Turns timber into planks.
Planks are needed for new buildings.

Status: Waiting for timber
Input: timber
Output: planks
Road connection: connected
Tip: Make sure a Woodcutter can deliver timber here.
```

### 9.5 Quarry inspector

```text
Quarry

Cuts stone from nearby rock.
Stone is used for construction.

Status: Working / Waiting / Blocked
Needs: nearby rock
Produces: stone
Road connection: connected
Tip: Build near grey rock tiles.
```

### 9.6 Construction site inspector

```text
Construction Site: Woodcutter Lodge

Workers are building this.

Materials:
- Planks: 1 / 2
- Stone: 0 / 1

Status: Waiting for materials
Road connection: connected
Tip: Press Play and make sure roads connect to Headquarters.
```

### 9.7 Worker/carrier inspector

```text
Carrier

Carries goods along roads.

Current task: delivering timber
Carrying: timber
From: Woodcutter Lodge
To: Sawmill
Tip: Carriers move automatically when roads connect buildings.
```

If origin/destination are not available in snapshot, show what is available and add missing snapshot fields to a known-issues section.

### Acceptance

* Inspector updates when clicking HQ.
* Inspector updates when clicking terrain.
* Inspector updates when clicking building.
* Inspector updates when clicking construction site.
* Inspector updates when clicking visible worker/carrier if pickable.
* Inspector contains plain-English role text.
* Inspector contains status.
* Inspector contains a tip.
* Inspector text is visible in screenshot.
* Tests fail if the inspector is empty, off-screen, or contains only raw IDs/internal fields.

---

## 10. Phase 4 — make build and road modes obvious

Pressing `1`, `R`, or clicking a build button must produce obvious visible feedback.

### Build mode

When selecting Woodcutter / Forester / Sawmill / Quarry:

Visible mode banner:

```text
Placing Woodcutter Lodge
Click a green hex to place it. Red means it cannot be built there.
Esc cancels.
```

Visible side panel:

```text
Woodcutter Lodge
Role: cuts trees to make timber.
Best place: next to forest.
Needs: road connection to Headquarters.
```

Visible map feedback:

* large ghost building sprite;
* green valid marker;
* red invalid marker;
* invalid reason text:

  * `Too far from Headquarters`;
  * `Not buildable terrain`;
  * `Needs nearby trees`;
  * `Needs nearby rock`;
  * `Already occupied`;
  * or real authoritative reason.

### Road mode

When selecting Road mode:

Visible mode banner:

```text
Road mode
Click a start point, then click the destination.
Roads let carriers move goods between buildings.
```

After first click:

```text
Road start selected
Now click where the road should go.
```

After second click:

* show road preview;
* submit road segments;
* show success or first rejection reason.

### Acceptance

* Mode banner visible in screenshots.
* Build palette active button visibly highlighted.
* Road mode banner visible in screenshot.
* Invalid reason visible after invalid hover/click.
* User does not need to infer mode from keypress alone.

---

## 11. Phase 5 — sprite readability pass

Current sprites are not readable enough. Make the map visually clearer without trying to create final art.

### 11.1 Building silhouettes

Use generated original placeholder sprites with strong silhouettes and labels/icons:

* Headquarters: large central hall/storehouse with flag/crates.
* Woodcutter: small log cabin with axe/log pile.
* Forester: hut with sapling/tree icon.
* Sawmill: building with saw blade/log/plank stack.
* Quarry: stone shed/crane/pickaxe/rock pile.
* Construction site: scaffolding/foundation with hammer icon.

Each building must be distinguishable at normal zoom.

### 11.2 Terrain/resource readability

Improve:

* forest tiles: obvious trees, not only dark green blobs;
* rock tiles: grey rocks with clear quarryable look;
* grass/buildable tiles: simple clear base;
* road: visible path/track, not just thin blue lines;
* road node/flag: small flag or post.

### 11.3 Worker/carrier readability

Carriers/workers must be more readable:

* carrier sprite should look like a person/cart/worker, not a dot;
* carrier should show carried ware icon above or beside it;
* timber carrier shows log icon;
* plank carrier shows plank icon;
* stone carrier shows stone icon;
* idle carrier has no ware icon or an idle marker;
* worker/carrier should have a tiny label or tooltip when selected.

If carrier carried-ware is not exposed in snapshot, add minimal snapshot exposure if safe and tested. Otherwise show carrier status and document missing carried-ware field.

### 11.4 Construction/progress readability

Add simple visual state feedback:

* construction sites show progress ring/bar if progress is available;
* buildings waiting for input show small warning icon;
* working buildings show subtle pulse/smoke/animation;
* completed building event can briefly glow;
* delivered ware can create a small icon pop if low-cost.

Do not overbuild effects. Minimum viable animations are enough.

### 11.5 Acceptance

* In screenshot, HQ, Woodcutter, Forester, Sawmill and Quarry are visually distinguishable.
* At least one carrier/worker is visually identifiable.
* If a carrier is carrying timber/planks/stone, the carried ware is visually shown.
* Roads look like roads.
* Forest and rock are obvious.
* Construction site is visually different from completed building.
* Visual review must fail if sprites are all generic blobs or tiny unreadable labels.

---

## 12. Phase 6 — basic animation and “watchability”

Add lightweight animation to make the economy feel alive.

This is not final polish. It is minimum readable motion.

### Required minimum animations

* carrier/worker has simple movement animation or bobbing while moving;
* carried ware icon moves with carrier;
* working building has subtle state animation:

  * woodcutter: tiny chopping/log pulse;
  * sawmill: small saw/wheel pulse;
  * quarry: small stone/dust pulse;
  * forester: tiny sapling/tree pulse;
* waiting/blocked building has warning pulse;
* construction site has progress/hammer pulse when receiving work/materials;
* production/delivery event can show small floating ware icon or glow if already feasible.

### Animation rules

* Animation must not affect authoritative simulation.
* Animation is client-only visual feedback.
* Animation must be readable but not visually noisy.
* It should be possible to disable or reduce animation later.

### Acceptance

* Screenshot/short captured sequence after ticks shows at least one visible animated state or changed frame.
* The review JSON/MD records which animations were observed or structurally enabled.
* If full animation capture is hard headlessly, tests must at least verify animated nodes/state flags exist and change with snapshot state.

---

## 13. Phase 7 — tutorial steps tied to visible UI

The tutorial must now verify visible instructions and UI state, not just internal step state.

Minimum tutorial steps:

1. Welcome/objective visible.
2. Click Headquarters.
3. Explain inspector.
4. Pan/zoom.
5. Press Play.
6. Enter Road mode.
7. Build/connect a road.
8. Enter Woodcutter build mode.
9. Place Woodcutter.
10. Enter Sawmill build mode.
11. Explain watching carriers/goods.

Stretch steps:

12. Build Forester.
13. Build Quarry.
14. Complete objective.

For this run, hard acceptance is steps 1–11 visible and testable. Steps 12–14 may exist as future/optional if time is tight.

Each step must have:

* visible title;
* visible instruction text;
* visible target highlight or UI highlight where applicable;
* completion condition;
* failure/help text if the player gets stuck.

### Tutorial screenshot acceptance

Capture screenshots for:

```text
human_understandable_00_main_menu.png
human_understandable_01_tutorial_start.png
human_understandable_02_hq_selected_inspector.png
human_understandable_03_build_palette_visible.png
human_understandable_04_road_mode_instruction.png
human_understandable_05_woodcutter_build_mode.png
human_understandable_06_construction_site_inspector.png
human_understandable_07_carrier_with_ware.png
human_understandable_08_watch_economy_running.png
```

The visual review must record actual extracted node text for:

* tutorial title;
* tutorial instruction;
* inspector heading;
* inspector body;
* mode banner;
* objective text;
* build palette labels.

Do not rely on screenshots alone. Use Godot node probes to confirm text nodes exist, have non-empty text, and visible rects are inside the viewport.

---

## 14. Phase 8 — screenshot-driven iterative loop

Cursor must use the screenshot tools as part of development, not just as a final box-tick.

Implement this loop:

1. Launch tutorial scene headlessly.
2. Capture screenshot.
3. Probe visible UI nodes and text.
4. Fail if required visible text is absent/off-screen/too short.
5. Make layout/UI changes.
6. Repeat until review passes.
7. Save final screenshot artefacts.
8. Include the before/after comparison in final report.

Add a tool:

```text
godot_project/sim/tools/verify_human_understandable_screen.gd
```

It should fail non-zero unless the current tutorial screen has:

* tutorial panel visible;
* tutorial title length > 5;
* tutorial instruction length > 40;
* objective text length > 20;
* inspector panel visible;
* build palette visible;
* time controls visible;
* mode/status visible;
* map visible;
* at least 3 labelled build buttons visible;
* text rects inside viewport;
* no major panel off-screen;
* screenshot non-blank.

Add a second tool or mode for build/road screens:

```text
godot_project/sim/tools/verify_human_understandable_flow.gd
```

It should step through tutorial/menu/build/road/selection states and verify each required screen.

### Acceptance

The run is not complete until:

```bash
$GODOT --headless --path godot_project --script res://sim/tools/verify_human_understandable_screen.gd
$GODOT --headless --path godot_project --script res://sim/tools/verify_human_understandable_flow.gd
```

both exit 0.

---

## 15. Phase 9 — tests

Add or update tests using existing Godot test style.

### 15.1 Human-understandable screen tests

```text
test_human_understandable_tutorial_screen_visible.gd
test_human_understandable_required_text_present.gd
test_human_understandable_text_inside_viewport.gd
test_human_understandable_build_palette_visible.gd
test_human_understandable_mode_banner_visible.gd
```

### 15.2 Inspector tests

```text
test_human_understandable_inspector_hq_plain_english.gd
test_human_understandable_inspector_building_plain_english.gd
test_human_understandable_inspector_construction_site_plain_english.gd
test_human_understandable_inspector_worker_or_carrier.gd
```

### 15.3 Sprite/readability tests

```text
test_human_understandable_sprite_registry_complete.gd
test_human_understandable_building_sprites_distinct.gd
test_human_understandable_carrier_ware_icons.gd
test_human_understandable_terrain_sprites_distinct.gd
```

### 15.4 Tutorial flow tests

```text
test_human_understandable_tutorial_steps_1_to_5.gd
test_human_understandable_tutorial_road_step_visible.gd
test_human_understandable_tutorial_woodcutter_step_visible.gd
test_human_understandable_tutorial_can_exit.gd
```

### 15.5 Animation/watchability tests

```text
test_human_understandable_carrier_animation_state.gd
test_human_understandable_building_status_animation_state.gd
test_human_understandable_delivery_feedback_state.gd
```

Tests should not demand perfect art. They should demand that the game exposes enough visible, labelled, distinct, human-readable UI and visual state.

---

## 16. Phase 10 — easy launch remains required

Ensure this still works:

```bash
./scripts/run_game.sh
```

Main menu must open.

If tutorial can be launched directly, add:

```bash
./scripts/run_tutorial.sh
```

If direct tutorial launch is not feasible, `run_tutorial.sh` may open the main menu and print:

```text
Choose Tutorial from the main menu.
```

Update README and play guide.

---

## 17. Visual verification commands

Use:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

Required verification:

```bash
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_settler_mvp_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_human_understandable_screenshots.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_human_understandable_screen.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_human_understandable_flow.gd
```

Do not block on quarantined bot tests.

---

## 18. Manual acceptance checklist

After implementation, manually run:

```bash
./scripts/run_game.sh
```

Then verify:

```text
Main menu:
- Tutorial button visible.
- Free Play button visible.
- Debug Viewers visible.
- Quit visible.

Tutorial:
- Tutorial starts with readable text.
- It tells me what the game is.
- It tells me the objective.
- It tells me what to click first.
- Text is visible without needing to guess.

Layout:
- Map visible.
- Tutorial/objective/inspector panels visible.
- Build palette visible.
- Time controls visible.
- No important panel off-screen.

Selection:
- Clicking HQ explains HQ.
- Clicking terrain explains terrain.
- Clicking building explains building.
- Clicking construction site explains progress/materials.
- Clicking worker/carrier explains task or status.

Build/road:
- Pressing/clicking Road mode shows road instructions.
- Pressing/clicking Woodcutter shows build instructions.
- Placement ghost is obvious.
- Invalid reason is visible.
- Active mode is obvious.

Sprites:
- Woodcutter looks like a woodcutter/log building.
- Sawmill looks like a sawmill/plank building.
- Quarry looks like a stone/quarry building.
- Forester looks like a tree/forester building.
- HQ looks like a central storehouse.
- Carrier/worker looks like a carrier/worker.
- Carried goods are visible where available.
- Roads look like roads.

Watching:
- Workers/carriers visibly move.
- Buildings show working/waiting/blocked state.
- Something about the economy visibly changes while it runs.
- I can understand at least one cause/effect chain.

Launch:
- ./scripts/run_game.sh works.
- ./scripts/run_tutorial.sh works or clearly opens the menu with instructions.
```

If any item fails, do not call the run complete. Fix or document as explicitly out of scope only if agreed by user.

---

## 19. Documentation

Create/update:

```text
docs/runs/RUN_TUTORIAL_LEGIBILITY_SETTLER_MVP.md
docs/audit/RUN_TUTORIAL_LEGIBILITY_PLAY_GUIDE.md
docs/audit/RUN_TUTORIAL_LEGIBILITY_VISUAL_REVIEW.md
docs/audit/screenshots/human_understandable_*.png
docs/audit/screenshots/human_understandable_visual_review.json
```

Update README:

```text
How to play:
./scripts/run_game.sh

For tutorial:
./scripts/run_tutorial.sh
or choose Tutorial from the main menu.
```

---

## 20. Definition of done

This run is complete only when:

* Tutorial mode opens with readable text visible.
* The player can see what to do first.
* The player can inspect HQ/building/tile/site/worker and read plain-English information.
* Build and road modes show visible instructions and active state.
* Sprites are distinct enough to identify the core buildings/resources/workers.
* Carriers/workers show what they are carrying where snapshot data supports it.
* Basic working/waiting/blocked/construction states are visually represented.
* Required screenshots show menu, tutorial, inspector, build mode, road mode, construction, carrier/ware and running economy.
* Screenshot verification fails if text panels are missing/off-screen/empty.
* `verify_human_understandable_screen.gd` passes.
* `verify_human_understandable_flow.gd` passes.
* Existing relevant tests and smokes pass.
* `./scripts/run_game.sh` works.
* Final report includes before/after screenshots and an honest manual playability judgement.

The run must not be marked complete if the final screenshot still looks like only a map with no clear text or menus.

---

## 21. Final report format

Use the standard repo format:

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
Before screenshot:
After screenshots:
Human-understandability verification:
Tutorial text visibility:
Inspector visibility:
Build/road mode visibility:
Sprite readability:
Animation/watchability:
Manual playability result:
Remaining usability gaps:
```

Commit and push only after verification passes.

