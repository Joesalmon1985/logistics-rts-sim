# RUN_PLAYABLE_SETTLER_MVP — Base Settler Playable MVP PRD

Status: Ready for Cursor implementation planning  
Target branch: `feature/playable-settler-mvp`  
Primary goal: turn the existing economy simulation into a human-playable Godot settler game slice  
Non-goals: bots, neural training, military, hybrid RTS, multiplayer, polished art

---

## 1. Instruction to Cursor

You are working in the `logistics-rts-sim` repository.

Before making changes, read and follow:

- `.cursor/rules/00-project-identity.mdc`
- `.cursor/rules/01-autonomous-run-policy.mdc`
- `.cursor/rules/02-test-first-development.mdc`
- `.cursor/rules/03-godot-sim-authority.mdc`
- `.cursor/rules/04-godot-boundary.mdc`
- `.cursor/rules/05-ip-originality.mdc`
- `.cursor/rules/06-git-branch-and-review-policy.mdc`
- `.cursor/rules/07-final-report-format.mdc`
- `.cursor/rules/10-run-verification-and-logging.mdc`

Then read the existing architecture and relevant audit docs:

- `README.md`
- `docs/architecture/00_system_architecture.md`
- `docs/architecture/01_simulation_core_authoritative.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/architecture/03_godot_adapter_contract.md`
- `docs/architecture/04_bot_and_training_architecture.md`
- `docs/audit/RUN_02_8E_HUMAN_PROOF_REPORT.md`
- `docs/audit/RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md`
- `docs/audit/RUN_02_8E_IDIOTS_GUIDE_AND_DEV_PATH.md`
- `docs/audit/RUN_05C_ECONOMY_SPECTATOR_VISUAL_IDENTITY.md`
- `docs/audit/RUN_05C_RENDER_DATA_INVENTORY.md`
- `docs/audit/RUN_05C_3_VISUAL_REVIEW_REGISTER.md`

This milestone is a corrective product milestone. The project already has substantial simulation, diagnostics, bot, training and viewer infrastructure. Do not expand those areas in this run. The purpose of this run is to make the base economy game playable and understandable for a human in Godot.

---

## 2. Product problem

The repository currently has a strong authoritative Godot/GDScript simulation core and multiple debug/viewer scenes. It also has Python tooling for training and evaluation by driving Godot headlessly.

The missing product is a normal human-playable base settler game.

Current human interaction is too close to a debug harness: selecting from lists of legal commands, stepping ticks manually, watching diagnostics, or observing bot/spectator states. That is useful for development, but it is not yet a playable Widelands-like settlement game.

This PRD asks Cursor to create one focused playable mode:

> A single-player, human-playable, Widelands-inspired settlement logistics MVP on a hex board, using the existing authoritative Godot/GDScript simulation core.

The result should let a human start the project, choose a new game, place roads and buildings through map interaction, watch workers/carriers and goods move, understand why production is blocked, and complete a small economy objective.

---

## 3. Architecture decision for this PRD

The authoritative gameplay rules remain in:

```text
godot_project/sim/
```

The playable UI lives in:

```text
godot_project/client/
```

Python remains tooling/training only:

```text
python_tools/
```

Do not create an independent Python simulation or Python playable game in this milestone.

Correct architecture statement:

> The Godot/GDScript simulation core is authoritative. The Godot client is a non-authoritative human UI. Python is a trainer/tooling layer that drives the authoritative simulation via Godot headless.

All human UI actions must submit commands through the same simulation-core command validation/execution path used by bots and headless tools.

The UI may format, group, filter and render snapshot data. It must not derive independent gameplay rules, maintain a parallel game state, decide command legality locally, calculate production, calculate logistics, mutate authoritative state directly, or generate gameplay randomness.

---

## 4. MVP name and user-facing mode

Create a new playable mode called:

```text
Settler MVP
```

Suggested internal naming:

```text
settler_mvp
```

Suggested scene:

```text
godot_project/client/scenes/settler_mvp_game.tscn
godot_project/client/scenes/settler_mvp_game.gd
```

Suggested controller:

```text
godot_project/client/core/settler_mvp_controller.gd
```

Suggested scenario/config files where appropriate:

```text
godot_project/sim/data/maps/settler_mvp_map.json
godot_project/sim/data/config/settler_mvp_objective.json
```

Names may be adapted to existing repo conventions, but the milestone must produce one clearly named playable mode.

---

## 5. Player promise

A player should be able to:

1. Launch the Godot project.
2. Select or enter `Settler MVP`.
3. See a readable hex map with a headquarters, trees, stone/rock and available build space.
4. Pan and zoom the map.
5. Click or hover hexes to inspect terrain, resources, roads, buildings and construction sites.
6. Choose a building from a build menu.
7. Preview legal and illegal building placement on the map.
8. Place a construction site through a normal map click.
9. Place or extend roads/road nodes through map interaction.
10. Watch carriers/workers deliver materials.
11. Watch construction complete.
12. Watch buildings produce wares.
13. See stockpile/objective progress.
14. See clear warnings for blocked or waiting buildings.
15. Complete a simple settlement objective and get a victory message.

The player should not need to understand command JSON, legal-command lists, bot gates, replay tooling, or internal test names.

---

## 6. MVP scenario

Create or reuse a deterministic single-player scenario designed for human play.

Scenario requirements:

- One human player only.
- No opponent.
- No combat.
- Fixed seed.
- Fixed or deterministic small hex map.
- One headquarters already placed.
- Starting roads/road nodes sufficient to begin expansion, but not enough to complete the objective without player action.
- Nearby forest/tree density suitable for woodcutter and forester.
- Nearby rock/quarryable terrain suitable for quarry.
- Enough open buildable terrain for all MVP buildings.
- Starting stock sufficient to build the first few structures if the player connects them correctly.
- The objective is achievable by a typical human without bot-perfect timing.

Recommended objective:

> Build a working starter economy by completing one Woodcutter Lodge, one Forester Camp, one Sawmill and one Quarry, all connected to Headquarters by roads, then store at least 10 planks and 10 stone.

Victory requirements:

- Required buildings exist and are complete.
- Required buildings are connected to the logistics network.
- Headquarters or player stock has at least 10 `planks` and 10 `stone`.
- Victory is detected by simulation/snapshot/objective state, not by UI-only inference.

No failure condition is required for this MVP. A timer may be shown for debugging, but ordinary players should not lose because they are slow.

---

## 7. Economy scope

Use existing economy systems where possible. Do not build a new economy framework.

In scope:

- Hex board rendering and selection.
- Headquarters/storehouse starting stock.
- Road nodes / flags using the existing authoritative logistics graph.
- Road placement / road extension commands.
- Construction sites.
- Delivery of construction materials.
- Abstract builder work if already used by the sim.
- Woodcutter production from trees/forest.
- Forester tree regeneration or tree-density increase.
- Sawmill conversion of timber/logs to planks.
- Quarry production of stone from valid terrain/deposit.
- Carrier/worker movement visualization using existing snapshot/render data.
- Stockpile display.
- Blocked/waiting diagnostics.
- Objective progress.

Keep the MVP ware set narrow:

- `timber` or existing wood/log ware name
- `planks`
- `stone`
- `builder_work` if already required by existing construction rules

Out of scope unless already required by existing data and trivial to preserve:

- bricks
- tools
- water
- grain
- bread
- fish
- meat
- rations
- meals
- coal
- mines
- smelter
- workshop
- military resources
- power/credits/supply/intel/tech

Do not add new economy chains to make the MVP seem richer. Richer chains come later.

---

## 8. UI scope

Build a real playable UI around the existing command/snapshot pipeline.

### 8.1 Main menu

Add a minimal main menu or launch scene with:

- `New Settler MVP Game`
- `Quit`
- Optional: `Open Debug Viewers`

Set a usable main scene in `godot_project/project.godot`, unless there is a strong repo convention preventing this. If changing the project main scene would disrupt tests, document the reason and provide an alternative one-command launch path.

### 8.2 Game HUD

The playable scene must include:

- Top status bar: tick/time, pause/play status, speed.
- Objective panel: buildings required, stock required, progress, victory state.
- Stockpile panel: current relevant wares.
- Build menu: Road/Flag, Woodcutter, Forester, Sawmill, Quarry.
- Selection inspector: selected hex/building/site/road/node details.
- Message/event log: recent player actions, construction progress, blocked reasons, victory.
- Warning/diagnostics panel: blocked or waiting buildings/sites in plain English.
- Help overlay or compact instructions for first-time play.

### 8.3 Map interaction

The player must be able to interact with the map directly.

Required controls:

- Pan camera.
- Zoom camera.
- Hover or click hex to inspect.
- Select building type from build menu.
- Move cursor over map to see placement preview.
- Green/valid preview for legal placement.
- Red/invalid preview with clear reason for illegal placement.
- Click valid hex to submit the relevant build command.
- Enter road mode.
- Select road start/end or click adjacent hexes to submit road/road-node commands using existing sim commands.
- Cancel current placement mode.

Implementation must use existing simulation command availability/validation wherever possible. The client may use command availability data to guide the UI, but must not invent legality rules that can diverge from the sim.

### 8.4 Time controls

Required:

- Pause/play.
- Speed options: 1x, 2x, 4x.
- Step tick may remain available as a debug control, but normal play should use pause/play.

The authoritative sim must continue to advance through deterministic tick stepping. UI frame rate must not become authoritative.

### 8.5 Visual style

Use simple readable original placeholder art. Do not polish final art in this run.

Required readability:

- The map is readable at 1920x1080.
- Buildings have distinct silhouettes/icons.
- Roads are visible.
- Carriers/workers/ware movement markers are visible.
- Construction sites visibly differ from completed buildings.
- Blocked/waiting buildings are visibly marked.
- Important colours/icons are explained by a small legend or tooltips.

Do not copy Widelands, OpenRA, Command & Conquer, Red Alert, or any other protected art, names, UI, sprites, sounds, maps, missions or balance.

---

## 9. Diagnostics and player feedback

The MVP succeeds only if a human can understand what is happening.

The player must be able to answer these questions from the UI:

- What is selected?
- What can I build?
- Why can I build here?
- Why can I not build there?
- Is this construction site connected to roads?
- What materials does this construction site still need?
- Are carriers delivering materials?
- Is this building working, idle, waiting, or blocked?
- What input ware is missing?
- What output ware is being produced?
- How close am I to victory?

Plain-English diagnostic examples:

- `Waiting for planks: 0/2 delivered.`
- `Waiting for stone: 1/2 delivered.`
- `No road connection to headquarters.`
- `No reachable trees in work area.`
- `Sawmill waiting for timber.`
- `Quarry needs rock or quarryable terrain nearby.`
- `Construction complete: Woodcutter Lodge.`

Prefer using existing snapshot diagnostics. Add missing simulation diagnostics only where necessary and covered by tests. Do not fake diagnostics in the UI if the sim has no underlying state to support them.

---

## 10. Headless/training compatibility

This milestone must preserve the headless training route.

Python remains trainer/tooling only. Do not create a Python game.

Required preservation:

- Existing Python tests should still pass where the environment supports them.
- Existing Godot headless tools should still run.
- Existing command validation and snapshot contracts should not be broken.
- Human UI commands should use the same command path as bots/training agents.
- New scenario/objective state should be exportable or visible in snapshots where practical.

Useful optional addition:

- Add a headless smoke script that starts the Settler MVP scenario, executes a tiny scripted sequence or no-op sequence, exports a snapshot, and exits cleanly.

Do not train a model in this run.

---

## 11. Explicit non-goals

Do not work on these in this milestone:

- Fixing or improving `ECON_BOT_002`.
- Improving expansion bots.
- Improving military bots.
- Neural training quality.
- Behaviour cloning datasets.
- New reward functions.
- Military gameplay.
- Hybrid logistics RTS gameplay.
- Combat.
- Fog of war.
- Multiplayer.
- Mobile packaging.
- Save/load unless already trivial.
- Procedural map generation unless already trivial and not blocking the fixed MVP map.
- Final art polish.
- Sound/music.
- Full tutorial campaign.
- Replacing the existing authoritative simulation architecture.
- Creating a separate Python implementation of gameplay rules.

Do not weaken, delete, skip or fake existing tests to make this milestone pass.

---

## 12. Implementation plan

### Phase 0 — Inspect and plan

Before editing:

```bash
git status --short
git branch --show-current
git log --oneline -5
```

If unexplained uncommitted changes exist, stop and report.

Create or checkout:

```bash
git checkout -b feature/playable-settler-mvp
```

Inspect existing relevant files:

- `godot_project/client/scenes/economy_human_proof_demo.*`
- `godot_project/client/scenes/economy_spectator_bot_demo.*`
- `godot_project/client/ui/map_viewport.gd`
- `godot_project/client/ui/economy_sprite_layer.gd`
- `godot_project/client/core/economy_spectator_controller.gd`
- `godot_project/sim/core/command_availability_provider.gd`
- `godot_project/sim/core/command_validator.gd`
- `godot_project/sim/core/snapshot_exporter.gd`
- `godot_project/sim/data/config/buildings.json`
- `godot_project/sim/data/config/wares.json`
- `godot_project/sim/data/config/recipes.json`
- `godot_project/sim/data/config/construction_costs.json`
- existing economy setup/test/harness files

Write a short implementation plan before code. Keep it focused on playability.

### Phase 1 — Product shell

Create the playable scene and controller shell.

Deliverables:

- Settler MVP scene loads without script errors.
- Main menu or launch path exists.
- Map renders.
- HUD shell appears.
- Existing debug scenes remain available.

### Phase 2 — Scenario and objective

Create or reuse a deterministic scenario for one human player.

Deliverables:

- Scenario starts with HQ, starting stock, trees/forest and quarryable terrain.
- Objective state exists.
- Objective progress can be read by UI.
- Victory can be detected when the objective is met.

### Phase 3 — Map-driven building placement

Implement build menu and placement preview for MVP buildings.

Deliverables:

- Build menu lists Woodcutter, Forester, Sawmill and Quarry.
- Placement preview appears on hover/cursor movement.
- Valid placement submits a sim command.
- Invalid placement shows a reason.
- Construction site appears after successful command.

### Phase 4 — Road placement

Implement playable road/road-node placement.

Deliverables:

- Player can enter road mode.
- Player can connect from existing logistics network toward a selected target.
- Legal road commands are submitted to sim.
- Roads render clearly.
- Buildings/sites connected to roads are visibly connected.

### Phase 5 — Time, production and completion loop

Implement normal play controls and ensure the basic economy runs.

Deliverables:

- Pause/play works.
- Speed 1x/2x/4x works.
- Carriers/workers/ware movement appears where snapshot data supports it.
- Materials reach construction sites.
- Construction completes.
- Buildings produce outputs.
- Objective progress updates.

### Phase 6 — Diagnostics and UX pass

Make the game understandable.

Deliverables:

- Selection inspector shows useful details.
- Stock panel shows relevant wares.
- Objective panel is readable.
- Warnings/block reasons are plain English.
- Event log records major events.
- Basic help/instructions are visible.

### Phase 7 — Tests, smoke, documentation and final report

Add tests/smokes and document how to play.

Deliverables:

- New run doc or PRD committed in `docs/runs/` or `docs/product/`.
- Play instructions written in `docs/audit/` or `docs/verification/`.
- Headless smoke added where practical.
- Existing relevant tests still pass.
- Final report follows `.cursor/rules/07-final-report-format.mdc`.

---

## 13. Test requirements

Use existing repo test conventions. Do not invent a new test framework.

Minimum automated tests/smokes, adapted to actual test harness names:

1. Scene load smoke
   - Settler MVP scene loads headlessly or via the existing Godot smoke pattern.
   - No script errors.

2. Scenario smoke
   - Settler MVP scenario initializes.
   - One player exists.
   - HQ exists.
   - Required terrain/resources exist.
   - Objective state exists.

3. Command path smoke
   - A representative legal building placement through the controller submits a sim command.
   - The sim returns success.
   - A construction site appears in the next snapshot.

4. Invalid placement smoke
   - An invalid placement is rejected by the authoritative command validation path.
   - The UI/controller receives and displays a reason.

5. Time/progression smoke
   - Running ticks does not crash.
   - Snapshot tick advances.
   - At least one delivery or construction-progress event can be observed in a scripted/minimal scenario.

6. Objective smoke
   - A test fixture or scripted minimal path can satisfy the objective, or at minimum the objective evaluator returns correct incomplete/complete states for constructed test states.

7. Regression checks
   - Existing relevant economy tests still pass.
   - Existing viewer/smoke tests still pass where not intentionally superseded.
   - Python tool tests are not broken by changed paths/contracts.

Suggested verification commands to adapt after inspection:

```bash
# Godot tests
$GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd

# Specific filter if supported
TEST_FILTER=settler_mvp $GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd

# Existing failure-mode verification
$GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd

# Python tooling tests, if Python env is available
python -m pytest python_tools/tests -q
```

Never claim a test passed unless it was actually run. If Godot or Python is unavailable in the environment, state that explicitly in the final report.

---

## 14. Acceptance checklist

The milestone is complete only when all relevant items are true.

### Product acceptance

- [ ] A normal user can launch a playable Settler MVP mode.
- [ ] The project has a usable main scene or documented one-command launch path.
- [ ] The player can start/reset a deterministic single-player scenario.
- [ ] The hex map is visible and readable.
- [ ] The player can pan and zoom.
- [ ] The player can inspect hexes, roads, buildings and construction sites.
- [ ] The player can select buildings from a build menu.
- [ ] The player can preview legal/illegal placement.
- [ ] The player can place at least Woodcutter, Forester, Sawmill and Quarry through map clicks.
- [ ] The player can place/connect roads through map interaction.
- [ ] Construction sites receive delivered materials.
- [ ] Construction can complete.
- [ ] Buildings can produce relevant wares.
- [ ] Stockpile and objective progress are visible.
- [ ] Victory is detected and displayed.

### UX acceptance

- [ ] The player does not need to select raw command JSON or internal legal-command list entries for normal play.
- [ ] Invalid actions produce readable reasons.
- [ ] Blocked production/construction states produce readable reasons.
- [ ] The objective panel clearly tells the player what to do next.
- [ ] The UI is readable at 1920x1080.
- [ ] Important symbols/colours are explained by labels, tooltips or legend.

### Architecture acceptance

- [ ] Authoritative rules remain in `godot_project/sim/`.
- [ ] UI/client code remains non-authoritative.
- [ ] Human actions use the same command validation/execution path as bots/training.
- [ ] No independent Python gameplay simulation is added.
- [ ] Existing Python tools remain trainer/tooling only.
- [ ] No bot improvement, neural training, military or hybrid gameplay work is included.

### Verification acceptance

- [ ] New Settler MVP tests/smokes are added.
- [ ] Existing relevant Godot tests/smokes pass or failures are documented honestly.
- [ ] Failure-mode verification still passes or failures are documented honestly.
- [ ] Python tooling tests are run where available or explicitly noted as not run.
- [ ] Final report includes launch instructions and tests run.

---

## 15. Documentation requirements

Add or update:

```text
docs/runs/RUN_PLAYABLE_SETTLER_MVP.md
```

or equivalent, containing this PRD or a concise implementation run version of it.

Add a human play guide, for example:

```text
docs/audit/RUN_PLAYABLE_SETTLER_MVP_PLAY_GUIDE.md
```

The play guide must explain:

- how to launch the mode
- what the objective is
- how to place buildings
- how to place roads
- how to read stock/objective progress
- how to understand blocked buildings
- how to know when victory has been achieved

Update `README.md` only if useful, and keep it concise.

---

## 16. Final report format

End the Cursor run with the repo’s standard report format:

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
How to launch Settler MVP:
How to complete the objective:
Human-playability assessment:
Headless/training contract impact:
```

Be honest. Do not report the game as playable unless a human can actually complete the MVP loop through the UI.
