# MRP — Economy Bot, Playable Skirmish MVP, Hybrid Logistics RTS MVP, and 3D Bot Spectator

Status: Planning document for multiple Cursor runs
Project: `logistics-rts-sim`
Primary aim: move from isolated economy/military prototypes to a human-understandable playable game stack with competent bots

---

## 1. Executive summary

The project now has a human-playable economy/settler MVP with tutorial, readable UI, map-driven commands, screenshots, and verification gates. The military side has a strong underlying simulation but is still mainly a developer/debug tool. The hybrid economy-plus-military game is not yet productised.

This MRP defines the next three major product tracks, plus a dedicated 3D spectator product slice:

1. **Economy Tutorial AI**

   * A deterministic bot that can complete the same Settler MVP objective as a human following the tutorial.
   * Purpose: demo player, regression benchmark, future training baseline.

2. **Playable Skirmish MVP**

   * A standalone RTS military mini-game with direct map-driven controls, tutorial, readable sprites, production panel, combat UI, objective/win condition, and human-understandability verification.
   * Purpose: bring military to the same product maturity as Settler MVP.

3. **Playable Hybrid Logistics RTS MVP**

   * A combined game where the Widelands-like economy physically supplies and unlocks RTS military capability.
   * Purpose: prove the actual intended game: logistics economy feeding military strategy.

4. **3D Hybrid Bot-Vs-Bot Spectator**

   * A Godot 3D walkaround spectator mode where two deterministic hybrid bots build settlements, connect roads, supply military production, create soldiers, and fight each other.
   * Purpose: make bot rollouts and hybrid systems understandable, satisfying to watch, and useful for debugging future AI training.

Do not jump directly to neural training or the hybrid game before the economy bot and standalone military product stack are stable.

---

## 2. Current state

### 2.1 Economy / Settler MVP

The economy game now has:

* main menu launch;
* tutorial mode;
* free-play Settler MVP;
* map-driven road/build controls;
* pause/play/speed;
* readable build palette and mode banners;
* plain-English inspector;
* generated sprites/icons;
* carrier ware icons;
* screenshot/visibility verification;
* human-understandability tests;
* launch script.

This is now the product bar for other modes.

### 2.2 Military mini-game

The military side has:

* simulation for RTS-like mechanics;
* harvest loop;
* construction;
* power;
* queues;
* combat;
* fog/intel concepts;
* direct orders;
* skirmish hooks;
* basic debug viewer.

But it lacks:

* human-playable scene;
* main menu entry;
* tutorial;
* map-driven move/attack/build/queue UX;
* readable sprites;
* plain-English military inspector;
* production/build palette;
* power/credits/queue UI;
* screenshot-driven legibility gates;
* human-understandable tests;
* easy launch path.

### 2.3 Hybrid economy + military

The hybrid game is the final target, but should not be treated as just “military plus economy in one scene”.

It needs its own product definition:

* economy roads/carriers/wares matter;
* military capability depends on physical supply;
* the player manages settlement plans, production priorities and military posture;
* bot-vs-bot hybrid play must be observable without reading logs or JSON;
* initial hybrid play should probably be delegated-control, not full RTS micro;
* the UI must explain the connection between economy and military.

---

## 3. Strategic sequencing

Use this sequence:

```text
Track A — Economy Tutorial AI
    ↓
Track B1 — Military Debug/Spectator Readability
    ↓
Track B2 — Playable Skirmish MVP
    ↓
Track B3 — Military Tutorial Legibility
    ↓
Track C1 — Hybrid Observer / Rule Integration
    ↓
Track C2 — Playable Hybrid MVP
    ↓
Track C3 — Hybrid Tutorial + Bot Baselines
    ↓
Track C6 — 3D Hybrid Bot-Vs-Bot Spectator
```

Do not merge the tracks into one massive implementation run.

The immediate next run can be either:

* **Economy Tutorial AI**, if the goal is to stabilise bots first; or
* **Military Debug/Spectator Readability**, if the goal is to productise military first.

Recommended order:

1. Economy Tutorial AI.
2. Military Debug/Spectator Readability.
3. Playable Skirmish MVP.
4. Military Tutorial Legibility.
5. Hybrid Observer.
6. Playable Hybrid MVP.
7. Hybrid Tutorial + Bots.
8. 3D Hybrid Bot-Vs-Bot Spectator.

---

# TRACK A — Economy Tutorial AI

## A1. Product goal

Create a deterministic, explainable AI that can complete the current Settler MVP/tutorial objective at least as well as a human following the tutorial.

The bot should:

* start from the same Settler MVP scenario;
* use the same command API as the human UI;
* build roads;
* build Woodcutter, Forester, Sawmill and Quarry;
* connect buildings to HQ;
* reach 10 planks and 10 stone;
* avoid endless loops;
* export readable decision logs.

## A2. Non-goals

Do not start neural training.

Do not create a Python game.

Do not directly mutate `GameState`.

Do not hide known bot failures.

Do not fix military/hybrid bots in this run unless needed for shared verification hygiene.

## A3. Deliverables

### A3.1 Bot state audit

Create:

```text
docs/audit/RUN_BOT_RECOVERY_TUTORIAL_AI_CURRENT_STATE.md
```

Document:

* existing bots;
* existing bot tests;
* known hangs;
* `ECON_BOT_002` status;
* fast vs long bot suites;
* which bots are worth keeping;
* which tests need quarantine;
* which command APIs the bot should use.

### A3.2 Fast and long bot suites

Add or update:

```text
godot_project/sim/tools/run_bot_fast.gd
godot_project/sim/tools/run_bot_long.gd
```

Fast suite:

* must complete quickly;
* must not include known hanging dual-bot tests;
* must include tutorial bot smoke once implemented.

Long suite:

* includes quarantined/experimental bot tests;
* has clear timeouts;
* is manually runnable.

### A3.3 TutorialEconomyBot

Add:

```text
godot_project/sim/economy/bot/tutorial_economy_bot.gd
```

or equivalent repo-convention path.

Bot strategy:

1. inspect objective state;
2. inspect existing buildings and roads;
3. build/connect Woodcutter near forest;
4. build/connect Forester near wood economy;
5. build/connect Sawmill;
6. build/connect Quarry near rock;
7. wait/run economy when appropriate;
8. repair missing road connections;
9. avoid repeated invalid commands;
10. finish when objective complete.

### A3.4 Bot runner

Add:

```text
godot_project/sim/tools/run_tutorial_bot_smoke.gd
```

It must:

* load Settler MVP scenario;
* run bot;
* stop on objective complete, max ticks or stall;
* export summary and decision log;
* exit 0 only when objective completes.

### A3.5 Bot artefacts

Create:

```text
docs/audit/bot_runs/tutorial_bot_smoke_summary.json
docs/audit/bot_runs/tutorial_bot_decisions.csv
docs/audit/bot_runs/tutorial_bot_final_snapshot.json
```

Optional:

```text
docs/audit/bot_runs/tutorial_bot_rollout/
```

## A4. Acceptance

Economy bot run is complete only when:

* bot completes Settler MVP objective under seed 42;
* 3/3 fixed seeds pass or documented as stretch if runtime too high;
* bot decision log is readable;
* invalid command attempts are bounded;
* stall detection works;
* fast bot suite exits quickly;
* `ECON_BOT_002` is fixed or explicitly quarantined;
* no default bot verification hangs.

---

# TRACK B — Military Mini-Game to Settler-Level Maturity

## B0. Product target

Bring the standalone RTS military mini-game to the same product maturity as the Settler MVP:

* first-class main menu entry;
* playable scene;
* map-driven commands;
* readable sprites;
* plain-English inspector;
* production/build UI;
* pause/play/speed;
* tutorial;
* screenshot legibility gates;
* easy launch;
* competent baseline bot later.

The standalone military mini-game can use direct RTS controls. The later hybrid game does not have to use direct unit micro.

---

## B1. Milestone — Military Debug/Spectator Readability

### B1.1 Goal

Before building a full playable skirmish UI, make the military state readable in viewer/spectator form.

This closes the gap between:

```text
military_viewer_demo.tscn = developer debug viewer
```

and:

```text
military_spectator_demo.tscn = understandable skirmish watch mode
```

### B1.2 Deliverables

Add or update:

```text
godot_project/client/scenes/military_spectator_demo.tscn
godot_project/client/core/military_spectator_controller.gd
godot_project/client/ui/military_sprite_layer.gd
godot_project/client/adapters/military_catalog.gd
godot_project/client/assets/military/asset_registry.json
tools/generate_military_placeholder_sprites.py
```

Required UI:

* military summary panel;
* credits;
* power supply/demand;
* unit counts;
* production queues;
* selected unit/building inspector;
* recent events / “what changed” panel;
* fog/intel toggle if snapshot supports it;
* attack lines/damage events;
* harvester route overlay if available.

Required visual concepts:

* Command Hub;
* Power Generator;
* Refinery/Supply Depot;
* Barracks;
* Fabricator/Vehicle Plant;
* Turret;
* Harvester;
* Rifle Squad;
* basic vehicle if present;
* projectile/attack marker;
* damage/health state;
* fog/shroud.

### B1.3 Snapshot gaps

Audit and add only safe, tested snapshot fields needed for display:

* production queue blocked reason;
* underpowered reason;
* patrol/guard target;
* attack target;
* harvester route;
* recent combat events;
* recent production events.

Do not invent these client-side if sim state exists.

### B1.4 Verification

Add:

```text
godot_project/sim/tools/verify_military_spectator_screen.gd
godot_project/sim/tools/capture_military_spectator_screenshots.gd
```

Screenshots:

```text
docs/audit/screenshots/military_spectator_00_overview.png
docs/audit/screenshots/military_spectator_01_unit_selected.png
docs/audit/screenshots/military_spectator_02_combat_event.png
docs/audit/screenshots/military_spectator_03_production_queue.png
docs/audit/screenshots/military_spectator_visual_review.md
docs/audit/screenshots/military_spectator_visual_review.json
```

### B1.5 Acceptance

* Reviewer can understand a military skirmish state without reading JSON.
* Distinct military units/buildings are visible.
* Plain-English inspector works for at least one building and one unit.
* Summary panel shows credits/power/queues.
* Screenshot verification fails if the screen is only coloured circles/abbreviations.
* Existing military sim tests still pass.

---

## B2. Milestone — Playable Skirmish MVP

### B2.1 Goal

Create a standalone human-playable RTS military mini-game.

This is the military equivalent of Settler MVP.

A player should be able to:

1. launch Skirmish MVP from the main menu;
2. see a small RTS map;
3. select units/buildings;
4. build or queue basic structures/units;
5. move units;
6. attack enemies;
7. manage credits/power/queues;
8. complete a simple win condition.

### B2.2 New scene/controller

Add:

```text
godot_project/client/scenes/military_mvp_game.tscn
godot_project/client/scenes/military_mvp_game.gd
godot_project/client/core/military_mvp_controller.gd
```

Main menu entry:

```text
Skirmish MVP
```

Optional script:

```text
scripts/run_skirmish.sh
```

### B2.3 Scenario

Add:

```text
godot_project/sim/data/scenarios/military/military_mvp_skirmish.json
```

Scenario requirements:

* one human player;
* one simple enemy/base/target;
* small map;
* starting Command Hub;
* starting harvester/economy if the sim supports it;
* enough credits to build first structure/unit;
* clear win condition.

Suggested win condition:

```text
Destroy the enemy Command Hub.
```

Tutorial version can use a weaker target:

```text
Build a Rifle Squad and destroy the target bunker.
```

### B2.4 Map-driven controls

Required:

* left-click selects unit/building;
* drag-select units if feasible, otherwise single-select is acceptable for MVP;
* right-click or command-click move;
* attack command on enemy unit/building;
* attack-move if sim supports it;
* build palette for base buildings;
* placement ghost with valid/invalid feedback;
* production queue panel for units;
* rally point if low-cost;
* cancel/stop command if supported;
* pause/play/speed controls.

Do not require the player to choose from a raw command list.

### B2.5 Military build/production UI

Panels:

* credits;
* power supply/demand;
* selected building;
* production queue;
* available units/buildings;
* blocked reason:

  * not enough credits;
  * low power;
  * missing prerequisite;
  * queue full;
  * invalid placement.

### B2.6 Inspector

Plain-English inspectors required for:

* terrain;
* Command Hub;
* Power Generator;
* Barracks;
* Fabricator/Factory;
* Turret;
* Harvester;
* Rifle Squad;
* enemy unit/building.

Example:

```text
Rifle Squad

Basic infantry unit.
Good against enemy infantry and unarmoured targets.

Health: 80 / 100
Order: Attacking enemy turret
Range: short
Tip: Select the squad, then right-click an enemy to attack.
```

### B2.7 Time controls

Same as Settler:

* pause/play;
* step;
* 1×/2×/4×;
* tick display;
* keyboard shortcuts.

### B2.8 Verification

Add:

```text
godot_project/sim/tools/verify_military_mvp_screen.gd
godot_project/sim/tools/verify_military_mvp_flow.gd
godot_project/sim/tools/capture_military_mvp_screenshots.gd
```

Screenshots:

```text
docs/audit/screenshots/military_mvp_00_main_menu.png
docs/audit/screenshots/military_mvp_01_skirmish_start.png
docs/audit/screenshots/military_mvp_02_unit_selected.png
docs/audit/screenshots/military_mvp_03_build_palette.png
docs/audit/screenshots/military_mvp_04_production_queue.png
docs/audit/screenshots/military_mvp_05_attack_order.png
docs/audit/screenshots/military_mvp_06_combat.png
docs/audit/screenshots/military_mvp_07_victory.png
```

### B2.9 Tests

Add `TEST_FILTER=military_mvp` tests:

```text
test_military_mvp_scene_loads.gd
test_military_mvp_main_menu_entry.gd
test_military_mvp_selection_inspector_unit.gd
test_military_mvp_selection_inspector_building.gd
test_military_mvp_move_command_map_driven.gd
test_military_mvp_attack_command_map_driven.gd
test_military_mvp_build_palette_visible.gd
test_military_mvp_production_queue_visible.gd
test_military_mvp_invalid_command_feedback.gd
test_military_mvp_time_controls.gd
test_military_mvp_victory_condition.gd
test_military_mvp_screenshot_capture.gd
```

### B2.10 Acceptance

Playable Skirmish MVP is complete when:

* player launches Skirmish MVP from menu;
* no debug command list is needed;
* unit/building selection works;
* move and attack commands work from map interaction;
* build/production palette is visible;
* invalid actions give plain-English reason;
* player can complete simple win condition;
* screenshot verification proves UI is visible;
* tests pass;
* play guide exists.

---

## B3. Milestone — Military Tutorial Legibility

### B3.1 Goal

Add a first-time-player tutorial to Skirmish MVP.

Tutorial should teach:

1. select Command Hub;
2. read credits/power;
3. build Power Generator;
4. build Barracks or Fabricator;
5. queue Rifle Squad/unit;
6. select unit;
7. move unit;
8. attack enemy/target;
9. win.

### B3.2 Tutorial UI

Same standard as Settler:

* visible tutorial panel;
* step number;
* clear instruction;
* highlighted target;
* skip/exit;
* objective panel;
* inspector;
* build/production palette;
* screenshot verification.

### B3.3 Verification

Add:

```text
verify_human_understandable_military_screen.gd
verify_human_understandable_military_flow.gd
capture_human_understandable_military_screenshots.gd
```

Screenshots:

```text
human_understandable_military_00_main_menu.png
human_understandable_military_01_tutorial_start.png
human_understandable_military_02_command_hub_selected.png
human_understandable_military_03_build_power.png
human_understandable_military_04_queue_unit.png
human_understandable_military_05_unit_selected.png
human_understandable_military_06_attack_order.png
human_understandable_military_07_victory.png
```

### B3.4 Acceptance

* tutorial starts from menu;
* first screen visibly explains what to do;
* a first-time player can follow tutorial without command list;
* screenshots prove text/panels/sprites are visible;
* verification fails on map-only UI;
* existing military MVP tests still pass.

---

## B4. Milestone — Military Tutorial Bot

### B4.1 Goal

Create a deterministic military bot that can complete the military tutorial/skirmish objective.

Suggested bot:

```text
TutorialMilitaryBot
```

It should:

* build required base structure;
* queue basic unit;
* gather/harvest if needed;
* attack target;
* win scenario.

### B4.2 Acceptance

* bot completes skirmish tutorial objective under seed 42;
* no infinite stall;
* decision log explains choices;
* fast bot smoke passes;
* rollout artefacts generated.

---

# TRACK C — Combined Economy + Military Hybrid MVP

## C0. Product target

Create the actual hybrid game where the settlement economy physically enables military capability.

This should not simply paste the Skirmish MVP controls into Settler MVP.

Hybrid design principle:

> The player commands settlement priorities and military intent. The economy delivers materials, workers and supplies. Military buildings produce and control units through logistics, not instant sidebar magic.

The first hybrid MVP should be **delegated-control**, not full unit micro, unless explicitly changed later.

---

## C1. Milestone — Hybrid Rule Integration Audit

### C1.1 Goal

Take stock of what already exists for hybrid rules and what remains missing.

Create:

```text
docs/audit/RUN_HYBRID_MVP_CURRENT_STATE.md
```

Audit:

* economy wares that military uses;
* military buildings currently defined;
* production queues;
* supply/power/credits;
* how construction works for military structures;
* how units are produced;
* whether units require wares;
* whether military orders are direct or delegated;
* snapshot coverage;
* UI coverage;
* bot coverage.

### C1.2 Acceptance

* gap register exists;
* hybrid scope is confirmed before implementation;
* no coding until audit is written.

---

## C2. Milestone — Hybrid Observer MVP

### C2.1 Goal

Before human play, create a watchable hybrid observer.

It should show:

* economy road/carrier network;
* military buildings;
* material deliveries to military construction/production;
* unit production;
* enemy/target state;
* objective progress.

Add:

```text
godot_project/client/scenes/hybrid_observer_demo.tscn
godot_project/client/core/hybrid_observer_controller.gd
godot_project/client/ui/hybrid_sprite_layer.gd
```

### C2.2 Scenario

Add:

```text
godot_project/sim/data/scenarios/hybrid/hybrid_mvp_observer.json
```

Scenario should prove:

* HQ produces/supports logistics;
* player can build/supply Barracks or Fabricator;
* military unit is produced;
* unit/force can attack target via delegated order or scripted order.

### C2.3 Visual acceptance

Observer must make the chain visible:

```text
timber/stone/planks/tools/supplies
→ military construction site
→ completed military building
→ queued unit
→ unit produced
→ attack/defend objective
```

### C2.4 Verification

Add:

```text
verify_hybrid_observer_screen.gd
capture_hybrid_observer_screenshots.gd
```

Screenshots:

```text
hybrid_observer_00_start.png
hybrid_observer_01_military_site_supplied.png
hybrid_observer_02_unit_queue.png
hybrid_observer_03_unit_ready.png
hybrid_observer_04_attack_order.png
hybrid_observer_05_objective_result.png
```

---

## C3. Milestone — Playable Hybrid MVP

### C3.1 Goal

Create a human-playable hybrid game where the player builds a small economy, supplies military production, and uses military force to complete an objective.

### C3.2 Scene/controller

Add:

```text
godot_project/client/scenes/hybrid_mvp_game.tscn
godot_project/client/core/hybrid_mvp_controller.gd
```

Main menu entry:

```text
Hybrid MVP
```

Optional script:

```text
scripts/run_hybrid.sh
```

### C3.3 MVP objective

Suggested first objective:

```text
Build a supplied Barracks, produce a Rifle Squad, and destroy the enemy outpost.
```

or delegated version:

```text
Build a supplied Guard Post, assign Attack Posture, and let the force destroy the enemy outpost.
```

Pick one and document the control model.

### C3.4 Hybrid economy requirements

Military construction should consume physical wares, for example:

* planks;
* stone;
* tools;
* field supplies;
* machine parts later.

For MVP, keep it simple:

```text
Barracks requires planks + stone + builder_work.
Rifle Squad requires field_supplies or training_time.
```

If `field_supplies` chain is not ready, start with simpler wares and document future expansion.

### C3.5 Hybrid UI

Required panels:

* economy objective;
* military objective;
* stockpile;
* military supply panel;
* build palette:

  * economy buildings;
  * military buildings;
* selected-object inspector:

  * economy buildings;
  * military buildings;
  * units;
  * roads;
  * construction sites;
* production queue panel;
* order/posture panel:

  * defend;
  * attack target;
  * patrol if supported;
* warnings:

  * missing wares;
  * no road connection;
  * underpowered;
  * no supplies;
  * enemy nearby.

### C3.6 Control model

Choose one for MVP:

#### Option A — delegated military control, recommended

Player chooses:

* build military structure;
* queue/enable unit production;
* set posture:

  * defend base;
  * attack marked target;
  * patrol route;
* assign rally/target area.

Units execute automatically.

This matches the project’s original logistics/delegated-control identity.

#### Option B — direct RTS control

Player can select units and give move/attack orders.

This may be used only if the explicit goal is to combine the standalone Skirmish MVP UX into hybrid mode.

Default for this MRP: **Option A delegated control**.

### C3.7 Acceptance

Playable Hybrid MVP is complete when:

* player launches Hybrid MVP from menu;
* player builds/uses economy;
* player builds a military building supplied by economy;
* physical wares are delivered to military construction/production;
* military unit/capability appears;
* player issues delegated military intent or direct order as defined;
* enemy/target is defeated;
* objective complete is shown;
* screenshots prove chain is visible;
* tests pass.

---

## C4. Milestone — Hybrid Tutorial Legibility

### C4.1 Goal

Add a first-time-player hybrid tutorial.

Tutorial teaches:

1. inspect HQ;
2. build/connect wood/stone economy if not already present;
3. build supplied military structure;
4. inspect missing materials/supplies;
5. wait/watch carriers deliver materials;
6. queue/enable unit production;
7. set attack/defence posture;
8. watch force attack target;
9. complete objective.

### C4.2 Verification

Add:

```text
verify_human_understandable_hybrid_screen.gd
verify_human_understandable_hybrid_flow.gd
capture_human_understandable_hybrid_screenshots.gd
```

Screenshots:

```text
human_understandable_hybrid_00_main_menu.png
human_understandable_hybrid_01_tutorial_start.png
human_understandable_hybrid_02_economy_building_selected.png
human_understandable_hybrid_03_military_site_missing_materials.png
human_understandable_hybrid_04_carrier_delivering_to_barracks.png
human_understandable_hybrid_05_unit_queue.png
human_understandable_hybrid_06_attack_posture.png
human_understandable_hybrid_07_combat.png
human_understandable_hybrid_08_victory.png
```

### C4.3 Acceptance

* tutorial text visible;
* hybrid chain visible;
* inspector explains both economy and military objects;
* player can understand why military production is blocked;
* screenshots prove explanatory UI is visible;
* verification fails if only the map is visible.

---

## C5. Milestone — Hybrid Bot Baseline

### C5.1 Goal

Create a deterministic hybrid bot that can complete the hybrid MVP objective.

Suggested bot:

```text
TutorialHybridBot
```

It should:

* build starter economy if needed;
* connect roads;
* build military structure;
* wait for materials;
* produce unit/capability;
* set attack posture/target;
* complete objective.

### C5.2 Acceptance

* bot completes hybrid objective under seed 42;
* bot logs economy and military decisions;
* no infinite stall;
* fast bot smoke passes;
* rollout artefacts are readable.

---

## C6. Milestone — 3D Hybrid Bot-Vs-Bot Walkaround Spectator

### C6.1 Goal

Create a Godot 3D spectator mode where the user can walk around a live hybrid match between two deterministic bots.

The mode should make the hybrid game enjoyable and understandable to watch. A viewer should be able to see two bot settlements grow from starting hubs into supplied military bases, then watch soldiers move out and fight for an objective.

This is not the main playable control mode. It is a visualisation, QA, demo, and future training-rollout review mode.

### C6.2 Product experience

From the main menu, add:

```text
3D Bot Spectator
```

When launched, the mode starts a small bot-vs-bot hybrid scenario.

The spectator should be able to:

* walk around the world in 3D;
* pan, rotate, zoom, and change camera height;
* switch between free camera, orbit camera, and follow camera;
* follow either bot player;
* follow a selected carrier, builder, soldier, or military squad;
* pause, play, step, and change speed;
* toggle overlays for roads, supply flow, military orders, combat events, and bot intent;
* open plain-English inspectors for buildings, roads, stockpiles, carriers, construction sites, units, and squads;
* watch a complete match from base-building through military production to combat and victory.

The viewer should understand the broad story without reading debug logs:

```text
Bot A expands economy
→ Bot B expands economy
→ both build military infrastructure
→ wares are delivered to barracks / guard posts
→ soldiers are trained
→ forces move toward the contested area
→ combat happens
→ one side wins or the match times out with a result
```

### C6.3 Scene/controller

Add:

```text
godot_project/client/scenes/hybrid_3d_bot_spectator.tscn
godot_project/client/scenes/hybrid_3d_bot_spectator.gd
godot_project/client/core/hybrid_3d_spectator_controller.gd
godot_project/client/ui/hybrid_3d_spectator_hud.gd
godot_project/client/ui/hybrid_3d_inspector_panel.gd
godot_project/client/rendering/hybrid_3d_world_renderer.gd
godot_project/client/rendering/hybrid_3d_camera_controller.gd
```

Optional helper assets:

```text
godot_project/client/assets/hybrid_3d/
godot_project/client/assets/hybrid_3d/asset_registry.json
tools/generate_hybrid_3d_placeholder_assets.py
scripts/run_hybrid_3d_spectator.sh
```

The 3D scene must consume authoritative sim snapshots. It must not become a second simulation.

### C6.4 Scenario

Add:

```text
godot_project/sim/data/scenarios/hybrid/hybrid_bot_vs_bot_3d_spectator.json
```

Scenario requirements:

* two bot-controlled players;
* small symmetrical or near-symmetrical map;
* each bot starts with a Headquarters / Command Hub;
* each bot can build roads and core economy buildings;
* each bot can build a supplied military building;
* each bot can produce at least one basic soldier / squad type;
* forces have a reason to meet, such as a central outpost, enemy base, or destroy-command-hub objective;
* match has a win condition and timeout condition;
* seed is fixed by default for reproducible visual review.

Suggested first objective:

```text
Destroy the enemy Command Hub, or control the central shrine/outpost for long enough to win.
```

For the first implementation, prefer a constrained “demo lane” map over a large open-ended map. The point is to prove readability, not strategic depth.

### C6.5 Bot requirements

Use deterministic bots that issue legal commands through the same command API as humans and tutorial bots.

Suggested bot names:

```text
HybridSpectatorBotA
HybridSpectatorBotB
```

or parameterised instances of:

```text
TutorialHybridBot
```

Each bot should:

* build a minimal wood/stone economy;
* connect roads to resource and production buildings;
* build a supplied Barracks, Guard Post, or equivalent military building;
* wait for physical materials to arrive;
* produce soldiers/squads;
* set military posture or target area;
* attack the opponent or contested objective;
* recover from simple blocked states;
* export readable decision logs.

Bot-vs-bot mode must not directly mutate `GameState`.

### C6.6 3D visual language

The first version may use simple meshes, billboards, and placeholder animations. It does not need final art.

Required 3D readable concepts:

* hex terrain with visible height/biome differences;
* Headquarters / Command Hub;
* road nodes and road segments;
* carriers walking along roads;
* construction sites with scaffold/progress state;
* economy buildings such as Woodcutter, Forester, Sawmill and Quarry if present;
* military buildings such as Barracks, Guard Post, Turret or Fabricator if present;
* wares moving or being carried;
* soldiers/squads;
* projectiles, melee markers, or attack beams;
* health/damage state;
* defeated/destroyed building state;
* player colour/team identity;
* objective marker.

Use intentionally simple readable shapes:

* buildings should have different silhouettes, not just different colours;
* soldiers should be visibly different from carriers;
* construction sites should be visibly incomplete;
* damage should be visible at a glance;
* roads and supply routes should remain readable from the camera height used by the spectator.

### C6.7 Camera and walkaround controls

Desktop controls:

* WASD or arrow keys move camera/player spectator;
* mouse look or right-drag rotate;
* scroll wheel zooms or changes height;
* Q/E or equivalent raises/lowers camera;
* number keys jump to Bot A base, Bot B base, battle area, or objective;
* space pauses/plays;
* `[` and `]` adjust speed;
* F follows selected object;
* Escape opens menu.

Mobile controls, if feasible in this run:

* one-finger drag rotates/pans;
* pinch zooms;
* two-finger drag changes height;
* tap selects object;
* follow buttons for Bot A, Bot B, objective, and battle.

If mobile controls are too much for the first run, desktop controls are acceptable, but the limitation must be documented.

### C6.8 HUD and overlays

Required HUD:

* match timer and tick;
* speed/pause controls;
* Bot A summary;
* Bot B summary;
* objective status;
* current camera mode;
* selected object inspector;
* recent events panel;
* bot intent panel.

Bot summaries should show:

* stockpile headline;
* building counts;
* soldier/squad counts;
* military production status;
* current high-level bot plan;
* blocked reason if the bot is stuck.

Overlay toggles:

* roads;
* supply deliveries;
* building construction progress;
* military production queues;
* attack targets;
* unit orders;
* combat events;
* territory/control if available;
* fog/intel mode if the sim supports it.

Spectator mode may include a full-map omniscient view by default, but it must be clearly labelled as spectator-only. If fog/intel matters to the sim, provide toggles for:

```text
Spectator full reveal
Bot A knowledge
Bot B knowledge
```

### C6.9 Plain-English 3D inspector

The inspector must explain what the selected object is and why it matters.

Required inspected objects:

* terrain hex;
* road node/road segment;
* carrier;
* ware delivery;
* HQ / Command Hub;
* economy building;
* military building;
* construction site;
* soldier/squad;
* enemy target/objective.

Example:

```text
Barracks — Bot A

This military building trains basic soldiers.
It needs planks, stone and training supplies delivered by road.

Status: waiting for stone
Connected to road: yes
Queue: 1 Rifle Squad
Blocked reason: needs 2 more stone
Tip: Watch the road from the quarry to see whether stone is arriving.
```

### C6.10 Snapshot and event requirements

Audit and add safe snapshot fields needed for 3D display.

Required snapshot/event concepts:

* player ownership for all visible objects;
* object world/grid position;
* construction progress;
* carried ware type;
* delivery source and target if available;
* unit/squad order;
* attack target;
* damage event;
* death/destruction event;
* production started/completed event;
* bot high-level intent;
* bot blocked reason;
* objective progress;
* match result.

The 3D client must not invent logistics, combat, or bot-intent facts. If the sim does not expose a value, either add it to the authoritative snapshot with tests or omit it from the display.

### C6.11 Verification

Add:

```text
godot_project/sim/tools/verify_hybrid_3d_spectator_screen.gd
godot_project/sim/tools/verify_hybrid_3d_spectator_flow.gd
godot_project/sim/tools/capture_hybrid_3d_spectator_screenshots.gd
godot_project/sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd
```

Screenshots:

```text
docs/audit/screenshots/hybrid_3d_spectator_00_main_menu.png
docs/audit/screenshots/hybrid_3d_spectator_01_match_start.png
docs/audit/screenshots/hybrid_3d_spectator_02_bot_a_base_growing.png
docs/audit/screenshots/hybrid_3d_spectator_03_bot_b_base_growing.png
docs/audit/screenshots/hybrid_3d_spectator_04_supply_to_military_site.png
docs/audit/screenshots/hybrid_3d_spectator_05_soldiers_produced.png
docs/audit/screenshots/hybrid_3d_spectator_06_forces_moving.png
docs/audit/screenshots/hybrid_3d_spectator_07_combat.png
docs/audit/screenshots/hybrid_3d_spectator_08_victory_or_timeout.png
docs/audit/screenshots/hybrid_3d_spectator_visual_review.md
docs/audit/screenshots/hybrid_3d_spectator_visual_review.json
```

Verification must fail if:

* the scene loads but no 3D world is visible;
* both bots are not represented;
* buildings are not distinguishable;
* roads/supply movement are not visible;
* soldiers/combat are not visible by the combat stage;
* HUD/inspector text is missing or unreadable;
* the mode only shows debug circles, IDs, or raw JSON.

### C6.12 Tests

Add `TEST_FILTER=hybrid_3d_spectator` tests:

```text
test_hybrid_3d_spectator_scene_loads.gd
test_hybrid_3d_spectator_main_menu_entry.gd
test_hybrid_3d_spectator_uses_authoritative_snapshots.gd
test_hybrid_3d_spectator_two_bots_spawn.gd
test_hybrid_3d_spectator_camera_controls_exist.gd
test_hybrid_3d_spectator_follow_bot_a.gd
test_hybrid_3d_spectator_follow_bot_b.gd
test_hybrid_3d_spectator_inspector_building.gd
test_hybrid_3d_spectator_inspector_unit.gd
test_hybrid_3d_spectator_supply_overlay_visible.gd
test_hybrid_3d_spectator_combat_overlay_visible.gd
test_hybrid_3d_spectator_flow_reaches_combat_or_timeout.gd
test_hybrid_3d_spectator_screenshot_capture.gd
```

### C6.13 Acceptance

3D Hybrid Bot Spectator is complete when:

* user can launch `3D Bot Spectator` from the main menu;
* a deterministic two-bot hybrid match starts;
* both bots build visible settlement/economy infrastructure;
* at least one military building is physically supplied by economy wares;
* soldiers/squads are produced;
* forces move and fight;
* match reaches victory or a clearly explained timeout;
* user can walk/fly around the scene;
* user can follow Bot A, Bot B, selected units, and the main battle;
* HUD explains what each bot is doing;
* inspector explains selected objects in plain English;
* screenshots prove the whole story is visible;
* 3D viewer consumes authoritative snapshots and does not run its own game logic;
* tests pass without adding long hanging bot runs to the default fast suite.

### C6.14 Non-goals

Do not:

* replace the 2D playable hybrid MVP;
* create a separate 3D simulation;
* require final art assets;
* implement online multiplayer;
* start neural training;
* add full human RTS micro to hybrid mode;
* make the spectator HUD depend on hidden debug-only state not exposed through tested snapshots;
* treat a pretty camera as success if the logistics-to-military chain is not understandable.


---

# 4. Cross-cutting requirements

## 4.1 Screenshot verification standard

For every playable mode, add:

```text
verify_<mode>_screen.gd
verify_<mode>_flow.gd
capture_<mode>_screenshots.gd
```

Screenshots must prove:

* menu entry;
* tutorial start;
* selection inspector;
* build/production palette;
* active mode;
* objective panel;
* victory/result.

A node existing is not enough. Verification must check:

* visible;
* inside viewport;
* non-zero size;
* readable text;
* not covered by the map;
* screenshot non-blank.

## 4.2 Plain-English inspectors

Each mode needs catalog-backed display text:

```text
BuildingCatalog
UnitCatalog
MilitaryCatalog
HybridCatalog
```

or equivalent.

Inspectors must explain:

* what the thing is;
* what it does;
* what it needs;
* what it produces or commands;
* current status;
* blocked reason;
* useful tip.

## 4.3 Easy launch

Maintain:

```text
./scripts/run_game.sh
```

Add where useful:

```text
./scripts/run_tutorial.sh
./scripts/run_skirmish.sh
./scripts/run_hybrid.sh
```

Each should either directly launch the mode or open the menu with clear printed instructions.


## 4.4 3D spectator rendering standard

3D spectator modes are client-only visualisers of the authoritative simulation.

They must:

* consume snapshots/events from the sim;
* keep the sim as the source of truth;
* avoid duplicating gameplay rules in rendering code;
* make ownership, construction, supply, production, movement and combat legible;
* provide camera controls suitable for visual review;
* support screenshot capture;
* expose enough UI text that a reviewer can understand what happened.

A visually impressive 3D scene is not sufficient. The mode must prove the hybrid chain:

```text
economy → logistics → military production → movement → combat → result
```

## 4.5 Bots

For each playable product mode, eventually provide:

```text
TutorialEconomyBot
TutorialMilitaryBot
TutorialHybridBot
```

Bots must:

* use command API;
* be deterministic;
* avoid direct state mutation;
* export decision logs;
* have max ticks and stall detection;
* complete the tutorial objective.

## 4.6 Verification hygiene

Do not allow known hanging long tests to block fast development.

Required structure:

* fast suite;
* long/quarantined suite;
* explicit test filters;
* no silent deletion;
* no indefinite hangs.

---

# 5. Recommended run list

## Run 1 — Bot Recovery and Tutorial Economy AI

Branch:

```text
feature/bot-recovery-tutorial-ai
```

Outcome:

* economy tutorial bot completes Settler MVP objective;
* fast/long bot suites cleaned up.

## Run 2 — Military Debug/Spectator Readability

Branch:

```text
feature/military-spectator-readability
```

Outcome:

* military state readable;
* sprites/catalog/inspector;
* spectator screenshots.

## Run 3 — Playable Skirmish MVP

Branch:

```text
feature/playable-skirmish-mvp
```

Outcome:

* main menu Skirmish MVP;
* map-driven move/attack/build/queue;
* win condition;
* tests/screenshots.

## Run 4 — Military Tutorial Legibility

Branch:

```text
feature/military-tutorial-legibility
```

Outcome:

* military tutorial;
* human-understandable screen/flow verification.

## Run 5 — Military Tutorial Bot

Branch:

```text
feature/military-tutorial-bot
```

Outcome:

* bot completes military tutorial objective.

## Run 6 — Hybrid Current-State Audit and Observer

Branch:

```text
feature/hybrid-observer-mvp
```

Outcome:

* hybrid rules/gaps documented;
* observer shows economy feeding military.

## Run 7 — Playable Hybrid MVP

Branch:

```text
feature/playable-hybrid-mvp
```

Outcome:

* player builds economy and supplied military capability;
* completes hybrid objective.

## Run 8 — Hybrid Tutorial Legibility

Branch:

```text
feature/hybrid-tutorial-legibility
```

Outcome:

* hybrid tutorial;
* hybrid human-understandability gates.

## Run 9 — Hybrid Tutorial Bot

Branch:

```text
feature/hybrid-tutorial-bot
```

Outcome:

* bot completes hybrid tutorial objective.

## Run 10 — 3D Hybrid Bot-Vs-Bot Spectator

Branch:

```text
feature/hybrid-3d-bot-spectator
```

Outcome:

* main menu `3D Bot Spectator` mode;
* two deterministic hybrid bots play a complete watchable match;
* 3D walkaround/follow camera;
* visible economy, roads, supply deliveries, military buildings, soldiers and combat;
* HUD, overlays, inspectors, screenshots and tests.

---

# 6. What not to do

Do not:

* start neural training before deterministic tutorial bots exist;
* build hybrid before military UI is readable;
* add final art before controls/tutorials work;
* merge direct military micro into hybrid by default;
* let screenshot tools pass map-only screens;
* let long bot tests hang default verification;
* create separate Python simulations;
* create a separate 3D gameplay simulation instead of rendering authoritative snapshots;
* count a 3D scene as complete if bot-vs-bot logistics, production and combat are not visible;
* use copyrighted RTS assets/names;
* claim product readiness from scene-load tests only.

---

# 7. Final target state

The project reaches the next maturity level when all three product modes exist:

## Economy / Settler

* human tutorial;
* free play;
* readable UI;
* deterministic tutorial bot.

## Military / Skirmish

* human tutorial;
* free play/skirmish;
* readable UI;
* deterministic tutorial bot.

## Hybrid Logistics RTS

* human tutorial;
* playable economy-fed military objective;
* readable UI;
* deterministic tutorial bot.

## 3D Hybrid Bot Spectator

* two-bot hybrid match;
* walkaround/follow camera;
* visible settlement growth, logistics, military production and combat;
* plain-English HUD and inspectors;
* screenshot and flow verification.

At that point, neural training becomes much more meaningful because the project will have:

* stable scenarios;
* stable legal command APIs;
* baseline scripted-good bots;
* random/pass baselines;
* clear rewards/objectives;
* visual rollout tools;
* failure diagnostics;
* human-understandable game modes.
