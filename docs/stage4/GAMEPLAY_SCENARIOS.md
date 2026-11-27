# Gameplay, tutorial and scenarios (stage 4, divine player)

Status: design, 26 Nov 2026. Owner decisions: D5.32 (sandbox first, optional objectives including the existing victory-point mode), D5.27 (unlimited-power sandbox and disabled-intervention mode), D5.33 (no predetermined outcomes), D5.1 (god only). Brief sections 32 (tutorial and scenarios), 44 (checkpoints A to F), 46 (determinism, replay) and 47 (end-to-end scenarios 1 to 18). Package: U6 (tutorial, scenarios and gameplay integration), with Q2 and Q3 for headless experiments and replay. Sister documents: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md), [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md), [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md), [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md), [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md).

## 1. Purpose and scope

What the player does from the first minute to the full game: the twelve-step tutorial, the scenario set (normal sandbox, unlimited Mythic sandbox, autonomous observation, controlled experiments, generated islands, handcrafted scenarios, optional objectives, replay and timeline branching), and what the player sees at each of the six playable checkpoints. It also maps the eighteen end-to-end acceptance scenarios of brief section 47 to the scenario files and the views that demonstrate them. It does not decide balance; numbers are configurable defaults.

## 2. Current state (read from the code, 26 Nov 2026)

| Area | What exists | Where |
|---|---|---|
| Start menu | `GameMenu` pages Start, Main, Save, Load. Start options: Play, Watch (six AIs), Start tutorial, **Living island** (population game with a seed text box and Roll seed), New island, load from the start card | `Game/Menu/GameMenu.cs` |
| Setup | `GameSetup`: seed, human faction or `NoHuman` (watch), faction count, `Population`, `StartNpcsPerFaction` (default 20), `Disease` (On, Mild, Off), `LifeScale`, `TicksPerDay` 1200, tutorial flag, `Victory` list | `Sim/Core/GameSetup.cs` |
| Victory | `VictoryKind`: VictoryPoints (10 VP default), LastStanding, **Sandbox** (never ends; `VictorySpec.SandboxOnly()` is used by the Living island) | `Sim/State/Victory.cs` |
| Tutorial (stage 3) | a stage 3 build tutorial: `TutorialIsland` (a fixed hand-set island), `TutorialSteps` (steps whose goals are pure predicates over `TutorialContext`, read-only), `TutorialCards` (step cards), HUD pointers (gold frame, diamond, map ring) | `Sim/Tutorial/`, `Game/Tutorial/`, `Hud/GodHud.Tutorial.cs` |
| Saving | three slots, `SaveFile` JSON via `SaveStore`; load restores the sim (save version in the header) | `Menu/SaveStore.cs`, `Sim/Save/SaveFile.cs` |
| Scenario files | `Recording/Scenario.cs`: setup, tuning overrides, a command schedule (`ScheduledCommand { Tick, Type, Args }`), recording level, metrics, checkpoints, periodic saves, stop and fail conditions, checks | `Recording/Scenario.cs` |
| Replay | `.godr` files (`ReplayHeader`: format "god-replay", sim version, host, scenario, seed, start kind and hash, checkpoint spacing) written by the runner and by Unity's `-godrecord`; `-godreplay FILE` opens a replay to watch (HEADLESS_SIMULATION 3.4, 456) | `Recording/Replay.cs`, `Boot/SmokeRun.cs` |
| Headless runner | `DotNet/God.Runner`, parallel runs, scenario overrides, `verify --against-unity` | HEADLESS_SIMULATION |
| Speed | 1x, 2x, 4x (watch: up to 16x), debug keys to 64x | `GodHud.cs`, `GodBootstrap.cs` |

Not present: any god, Mythic or divine command; the demographic scenario the brief asks for (brief section 15: the default scenario collapses to extinction, a favourable scenario is F2's job); timeline branching; scenario menus.

## 3. Decisions

| Id | Decision |
|---|---|
| GS-1 | **Sandbox is the default.** Nothing ends the game. Objectives are optional overlays chosen at setup and can be dismissed. The VP mode survives as one objective (D5.32). |
| GS-2 | **Scenarios are data, not code.** A scenario is a `Recording.Scenario` file plus a small `PresentationSpec` (title, text, camera start, tutorial script, objective list). The same file runs in Unity and in `God.Runner` (headless and Unity parity, brief section 46). |
| GS-3 | **A scenario setup never fakes outcomes.** Founders, land, tuning and a command schedule may be set, but results come from the simulation (D5.33). Scripted commands are real commands in the stream. |
| GS-4 | **The tutorial runs on a handcrafted scenario** ("First Light") that gives the lessons the prerequisites (a hungry child, a household, a surplus, a stranger), so it never waits for chance. |
| GS-5 | **Tutorial goals are of two kinds.** Sim goals are pure predicates over events and state (as in `TutorialSteps`): did a suggestion get delivered, did a miracle change state. View goals are probes in the Game layer: did the player select a person, follow a household, open a panel. Tutorial tips and pointers are view only; they change no sim state. |
| GS-6 | **Divine power modes** are per scenario: Normal (maximum, regeneration, costs), Unlimited Mythic (spending logged, balance never falls), Disabled (observation and analysis only). Both AI and human gods follow the same mode (D5.26, D5.27). |
| GS-7 | **Timeline branching is built on saves and the command stream.** A branch is a snapshot plus a new command stream, never an edit of the past. |
| GS-8 | **Difficulty and balance are configurable defaults** held in scenario files, not owner questions. |

## 4. Tutorial: twelve steps (brief section 32)

The tutorial is a guided scenario, "First Light", on a small island (default 2 settlements, 36 people, Disease Mild, Mythic Unlimited for steps 1 to 5 then Normal with a generous maximum), seed fixed and prerequisites staged by the scenario. It reuses the existing mechanisms: a card per step (`TutorialCards`), a pulsing frame and diamond on the toolbar slot or world spot (`GodHud.Tutorial`, `DrawTutorialPointers`), goals evaluated each frame, a Skip and a "Show tips" toggle (`ToggleTutorialTips`). Between steps the game can run or pause; the tutorial never forces speed.

| # | Step | The player learns | The player does | Done when | Sim prerequisites | Tier |
|---|---|---|---|---|---|---|
| 1 | Observing individuals | the world is full of persistent people with names | pan, rotate, zoom to a person; click to select; open the card; star a favourite | view goal: selected a person, opened the card, zoomed to the individual band | F1 names, U1 | 1 |
| 2 | Understanding needs and actions | needs, activity and the reasons for what they do | read the needs strip, the Mind tab "why" and the options weighed for a hungry person; follow them with the camera while they forage | view goal: opened the Mind tab and used Follow for 20 s | M3 learning (exists), U1 | 1 |
| 3 | Following a household | families, partnership, care, the family tree | open the Household tab, the Family tree, select the household, watch the child being fed | view goal: opened Household and Family; sim goal: a `ChildFed` or `CareGiven` event in the household | M4 (exists), U5 | 1 |
| 4 | Issuing a divine suggestion | a suggestion is an offer; the person decides | arm Suggest, pick "gather food" for a hungry person, confirm | sim goal: `DivineSuggestionDelivered`; then the response event is shown on the card whichever way it goes | D1, D2 | 1 |
| 5 | Issuing a command | a command is authoritative but can be refused | arm Command, pick "share food with the hungry child" or "go to the field" for an adult; see the response | sim goal: `DivineCommandIssued` and a `DivineResponse` | D2 | 1 |
| 6 | Performing a miracle | miracles change the physical world and cost Mythic | heal the injured person; create a small pile of food (the ledger note shows) | sim goal: `MiracleWrought` with `Heal` and a created lot | D3 | 1 |
| 7 | Using compulsion | compulsion can overrule priorities but not physics | compel a person to carry food to a place; they must walk, so see the yoke and the tether; try compelling something impossible and read the reason | sim goal: `CompulsionStarted` and `CompulsionEnded(Completed)`; plus one rejected check seen | D2 | 1 |
| 8 | Possessing an individual | direct control, the body's own limits, and the memory | possess a person, walk, gather, give an item, release; open their life history to see the divinely controlled marks | sim goal: `PossessionStarted` then `PossessionEnded(Released)` with at least one action; view goal: opened life history | D4, U1 | 2 |
| 9 | Manifesting an avatar | a physical divine body that can interact and be attacked | manifest, walk to a person, hand over an item, make a gesture, dismiss | sim goal: `AvatarManifested`, one `ExchangeStep` or hand-over with the avatar, `AvatarDismissed` | D5, U1, U4 | 2 |
| 10 | Revealing a commandment | doctrine is learned, spread and followed selectively | use the editor to reveal "Share surplus food with hungry children" to a leader; watch the glyph travel; check known and followed shares | sim goal: `CommandmentRevealed` and at least one `CommandmentLearned` by another person; view goal: opened the spread view | D6 | 1 (builder) |
| 11 | Observing trade and organisations | exchange, work, and institutions that emerge | watch a barter with gestures (no text), enable captions once, open an institution card | sim goal: an `ExchangeClosed` with goods moved; view goal: opened an institution card (if one exists, otherwise a trade marker) | M5 (exists), C3, C6 | 2 |
| 12 | Interpreting institutional reports | records differ from reality | open Analysis, compare an institutional record with the divine observation, read a KPI popover | view goal: opened a report and toggled the difference bar | C6, U5 | 2 |

**Tutorial rules.** The scenario ensures the staged person exists for each step (a hungry child by step 2, an injured worker by step 6), so a delay in the autonomous world never blocks it. Each step can be skipped; the final card names the unlocked scenarios. Because the unlimited Mythic mode switches to normal at step 6 the player learns cost and the meter (`MythicMode` change is a scenario command at that tick). The stage 3 build tutorial stays reachable only in the temporary faction mode.

## 5. Scenario set

All use the same loader. Menu entry: a "Play" page with cards: Tutorial, Sandbox, Unlimited sandbox, Observe, Experiments, Island generator, Scenarios, Replays and branches.

| Scenario type | Purpose | Setup (defaults, all editable) | Divine power | Objectives | Menu cue |
|---|---|---|---|---|---|
| **Normal sandbox** | the intended game | the favourable multigenerational scenario from F2 (viable demography, founders of mixed age, established households), 1 to 6 factions each with a god, one human god | Normal: Mythic maximum 100, regeneration 1 per second (fixed, brief section 7), costs as defaults, upkeep for possession and avatar | none; victory list `Sandbox` | Play |
| **Unlimited Mythic sandbox** | learn and experiment without cost anxiety | as above | Unlimited: spending recorded, balance constant, no cooldowns | none | Play, "Unlimited power" |
| **Autonomous observation** | watch a society develop with no intervention | any seed, `NoHuman` in the faction-policy sense is kept and the divine mode is Disabled, accelerated time, Mode A and D only | Disabled (a switch can enable it later and the run records the first intervention tick) | none; KPI dashboards; stop at N years or extinction | Observe |
| **Controlled experiments** | answer a question with repeatable runs | a scenario file with fixed seeds, a parameter override block and A and B variants (for example a commandment or miracle at day 30 versus none) and a metric list; Unity shows one run live, the runner runs many | any | metric conditions (stop or fail), checks | Experiments; the same file runs in `God.Runner` for batches |
| **Generated islands** | variety | seed text and Roll (existing), size small, medium, large (19 hexes today; larger board later), ecology (rich, normal, harsh), founders, disease, scarcity; every generated island is validated for a viable start by the generator (food within reach, no isolated founders) and the demographic preview is shown | any | any | Island generator |
| **Handcrafted scenarios** | designed situations | data files with fixed land, founders, history and a short brief; initial set: "First Light" (tutorial), "The Hungry Winter" (famine, introduce food-sharing), "Two Villages" (barter and a border), "The Fallen Elder" (resurrection and inheritance), "The Quiet Priest" (a religion forms), "The Failing Clinic" (institutions and care) | any | optional objective set | Scenarios |
| **Optional objectives** | goals for those who want them | an `ObjectiveSpec` list in the scenario: the existing victory-point mode (`VictoryKind.VictoryPoints`, default 10 VP); population thresholds after N generations; survive a famine without a smite; commandment adopted by a share of the people; found an institution through influence only; the objective evaluators read KPIs and events, never force an outcome | any | the list | shown as a corner chip; can be hidden |
| **Replay and branching** | review and what-if | see section 6 | n/a | none | Replays and branches |

**Divine modes in the setup card.** A three-way selector (Normal, Unlimited, Disabled) with a note on what it changes and a single link to the Mythic design; plus "Rival gods: AI policy: default, passive, or none" as a toggle for experiments.

**VP mode.** The old rule is not removed: `VictoryKind.VictoryPoints` remains an objective available to scenarios and uses the existing standings panel (`GodHud.Standings`). It scores factions, not the god, and a god never loses (D5.5): the VP objective ends the scored round, not the existence of the god.

## 6. Replay and sandbox timeline branching

**Recording.** Every game writes the `.godr` command stream as the sim ticks (the existing writer, extended to carry divine commands, control inputs and miracles; PLAYER_UI and CAMERA documents define them). A header holds seed, scenario, sim version, start hash and checkpoint spacing; it already does.

**Playback.** `-godreplay FILE` (existing) or the "Replays" page plays a recording in Unity with the same views; the camera is free and the viewer can switch to Mode A, D or follow a person; divine inputs are shown as marked events on a **timeline scrubber** (interventions, alerts, deaths, commandments, resurrections). Speeds 1x to accelerated; pause at markers; a "jump to event" list. Playback never enters player commands; a "Take control" button branches (below).

**Branching.**
1. The game keeps periodic **snapshots** (full saves in a ring: every sim day for the last 10 days, then keyframes every 10 days, then yearly; sizes and counts configurable; written off the main thread where possible; a snapshot is a normal `SaveFile` with its hash).
2. **Branch here** at any scrubber position finds the nearest earlier snapshot, re-simulates deterministically to the exact tick (fast, headless-style, the command stream applied), and starts a new timeline with a new id and a parent pointer (timeline id, tick).
3. The player's new commands go into the new stream; the old stream is untouched. Branches are listed as a tree (name, parent, tick, divergence summary).
4. **Compare:** two branches can be put side by side by KPI curves (population, deaths, wealth distribution, commandment spread) using the warehouse readers; the sim of each runs separately and deterministically.
5. Constraints: branching needs the save version to match the sim version; a sim upgrade invalidates old branches (shown, not silently broken); possession or avatar states in a snapshot restore as they were (control lease reset: the player must re-enter control); the player's UI annotations are per timeline in the UI save section.

**Determinism rules (brief section 46).** Continuing from a save equals running straight through (save and load parity); replays of the same stream reproduce exact hashes at the checkpoints; headless and Unity agree; branching never changes the past; logging independence holds (recording on or off yields the same hash).

## 7. Checkpoints A to F (brief section 44): what the player sees

Each checkpoint is an independently demonstrable build. A later failure does not invalidate the last working one (brief section 44). "Tier" marks the delivery tier of the contents.

| Checkpoint | Player-facing build | What the player sees and can do | Tutorial steps possible | Tier |
|---|---|---|---|---|
| **A Living world** | M3 to M5 integrated, a viable demographic scenario | the Living island start: a natural-looking island, named people, households and children, gestures as body language with no text, barter between neighbours, the card with Mind, Household, Relations, Family tree; free camera with rotate, zoom to person, follow; selection of a person or household; time controls with 5x and 10x; alerts digest; decode overlay for developers | 1, 2, 3, 11 (partial) | 1 |
| **B Divine sandbox** | D1 to D3, U1, U2 | a Mythic meter; toolbar with Instruct and Miracle; suggestions, commands and compulsion with distinct effects; heal, smite, create resources (ledger shown); the response chips and aggregated alerts; the commandment builder (reveal to one person, watch the glyph); the unlimited and disabled modes; replayable inputs | 1 to 7, 10 (builder) | 1 |
| **C Working economy** | C1 to C3 | people work, trade and own goods; farms, workshops and stores in settlements; employment shown by activity and a possessions list; the Economy tab; fields and depletion in the landscape; production alerts; the divine observation of stocks versus records | 11 | 1 for the physical economy, 2 for employment |
| **D Personal divine interaction** | D4, D5 | possession (rim, glyph, body's needs strip, control and release, life history marks), avatar manifestation (column of light, third-person camera, interaction prompts, gestures, destruction and return), the mode switcher | 8, 9 | 2 |
| **E Institutional society** | C6, C7, U5 | several simple institutions deliver services with NPC labour (clinic, school, communal store, simple religious body); building types with distinct looks; the reports panel with record versus observation; the institutional cards; resurrection and memorial view (D7) | 11, 12 | 2 |
| **F Integrated civilisation and divinity** | all packages, Q3, Q4 | the full play loop: influence the autonomous society through divine actions, observe outcomes in the world and in reports; scenarios and objectives; replay and branching; the polished tutorial; Windows build | 1 to 12 | 3 for polish, with 1 and 2 content |

## 8. Mapping the eighteen end-to-end scenarios (brief section 47)

Each is demonstrable headless first (Q2, Q3) and then shown in Unity. "View" is the view that demonstrates it and what the player sees.

| # | Scenario | Scenario file | What the player sees (view) | Checkpoint | Tier |
|---|---|---|---|---|---|
| 1 | Autonomous population (survive, learn, reproduce, age, form households, no divine act) | Autonomous observation on the favourable island | people live and die over decades with only the alert digest; KPI dashboard with births, deaths, fertility, household counts; family tree grows | A | 1 |
| 2 | Trade: communicate, agree, transfer real goods | Two Villages or any sandbox; synthetic pair in an experiment | gesture sequence request, offer, accept, give, receive, gratitude; the goods visibly pass hand to hand; the trade marker; decode overlay and captions for diagnosis | A | 1 |
| 3 | Employment: operate a productive asset and pay another | The Failing Clinic or a workshop scenario | a worker at a bench, the owner pays goods; Economy tab shows employment history; the record shows wages | C | 2 |
| 4 | Independent institution formed by NPCs | Quiet Priest or Failing Clinic, autonomous observation | a recurring need, then a cooperative or communal store appears with a building; an alert "institution formed"; the report card | E | 2 |
| 5 | Healthcare: an injured person requests help and recovers | Failing Clinic | begging or request gesture, a healer arrives, the treatment pose, bandages disappear, health ring rises; the card shows treatment | E | 2 |
| 6 | Education: one teaches, a capability changes | handcrafted | the teaching animation, the learner's practice, the Mind tab shows the new association and the capability change | E | 2 |
| 7 | Lending: lent, used, repaid or defaulted | handcrafted | a loan handover, a debt shown in the Economy tab for both, repayment gestures, or a default alert and a changed trust | C or E | 2 |
| 8 | Divine command: the NPC perceives it and responds independently | tutorial step 5; Sandbox | gold beam and chevron, the person's response animation (follow, refuse), the card's Divine tab with reasons | B | 1 |
| 9 | Compulsion: spend Mythic to compel a valid action | tutorial step 7 | yoke and tether, the person walks and acts physically, the Mythic spend and end by reason; an impossible request shows its reason | B | 1 |
| 10 | Possession: control, act, return | tutorial step 8 | rim and glyph, third-person camera, the body's needs, release; the life history marks "divinely controlled" | D | 2 |
| 11 | Avatar: manifest, walk, interact, return | tutorial step 9 | the column of light, the distinct figure, interaction prompts, gestures, dismiss | D | 2 |
| 12 | Miracle: heal or smite with genuine state changes | tutorial step 6 | spiral or bolt; the health, building or tree state genuinely changes and shows in the card and the world; ledger entries for created or destroyed goods | B | 1 |
| 13 | Commandment: revealed, learned, communicated, influencing without universal enforcement | tutorial step 10; Quiet Priest | the glyph tablet, the travelling glyph, the known and followed shares, individual non-compliance visible | B for the builder, E for the spread with institutions | 1 and 2 |
| 14 | Resurrection with the same identity and memories; property stays where it is | The Fallen Elder | the memorial, the column and the reborn pose, the Life tab continuing, the estate unchanged and stated in the panel | E or F | 2 |
| 15 | Government autonomy: a government refuses or changes a divine instruction | handcrafted | the institution-scope tray shows "refused" or "modified", the record panel shows its own decision | E | 2 |
| 16 | Religion and politics separate: affiliation changes without citizenship changing | Quiet Priest | the Faith tab shows the new affiliation, the citizenship line is unchanged, the records show both separately | E | 2 |
| 17 | Analytical reconciliation: warehouse records match sim events | experiment run | the Analysis panel shows a reconciliation view (event counts versus table counts) with zero difference; a read-only SQL example | Q1, F | 1 (headless), 3 (console) |
| 18 | Replay: a full scenario with NPC actions and divine inputs reproduces exactly | any, recorded | playback with the timeline scrubber, divine inputs marked, the final hash equals the recorded hash | F, with checks at A and B | 1 |

## 9. Sim-interface rules

1. A scenario or tutorial step may set up state only through the scenario's own setup and scheduled commands (`ScheduledCommand`); it never writes sim state from the Game layer.
2. Objectives and tutorial sim goals read events and KPIs; the evaluators sit in the Game layer or in a pure module, never in policies.
3. Divine mode switches mid-game (a tutorial step, or the player) are commands recorded in the stream.
4. Replay, branching and checkpoints use only saves and commands; the Unity UI state is separate.
5. Handcrafted scenarios and generated islands pass the same invariants and the same validation (viability preview, accessibility checks) as the demographic scenario.
6. The sim hash is independent of the tutorial, objectives panel, camera and annotations.

## 10. Tier and package mapping

| Capability | Package | Tier |
|---|---|---|
| Scenario page with Normal, Unlimited, Disabled selector; sandbox and unlimited sandbox; autonomous observation | U6 with D1 | 1 |
| Scenario loader shared with the runner, controlled-experiment files and batch hook | U6 with Q2 | 1 |
| Generated islands with the viability preview | U6 with F2 | 1 |
| Tutorial steps 1 to 7 and 10 (builder) | U6 | 1 for the functional steps; 3 for the card polish |
| Tutorial steps 8, 9, 11, 12 | U6 | 2 |
| Handcrafted scenario set (First Light plus five) | U6 | 2 |
| Optional objectives (VP mode kept, new objective list) | U6 | 2 |
| Replay playback with scrubber and markers | U6 with Q3 | 2 |
| Timeline branching and compare | U6 with Q3 | 3 |
| Tutorial polish, camera scripts and presentation | U6 | 3 |

## 11. PlayMode-testable acceptance hooks

| Hook | Test |
|---|---|
| H-GS-1 | Sandbox: a 30-year headless run of the normal sandbox with a viable demography has no game-over; the VP objective alone can end a scored round, never the god |
| H-GS-2 | Mythic modes: Normal drains and regenerates on schedule; Unlimited records spending and holds the balance; Disabled rejects any divine command at `Gate.Check` and hides the toolbar slots |
| H-GS-3 | Scenario parity: the same scenario file and seed produce the same hash at ticks 1000, 5000 and 20000 in Unity (batchmode) and in `God.Runner` |
| H-GS-4 | Tutorial: each step's goal is satisfied by a scripted command sequence and not satisfied without it; skipping a step never blocks the next; the stage 3 pointers are reused for the new steps |
| H-GS-5 | Tutorial neutrality: running First Light with the tutorial on and off gives the same sim hash for the same commands |
| H-GS-6 | Replay: record 5 days with divine inputs, play back, compare checkpoint hashes (equal) and markers (one per intervention) |
| H-GS-7 | Branch: snapshot at day 10, branch with a different commandment, run both for 5 days; the shared prefix hashes are equal, the branches differ from the divergence tick, the original timeline is unchanged |
| H-GS-8 | Generated island validity: 100 random seeds produce islands that pass the viability and accessibility validation or are rejected and regenerated; the same seed gives the same island |
| H-GS-9 | Objectives: an objective list evaluates from KPIs; adding or removing a view-only objective panel does not change the hash |
| H-GS-10 | Scenario mapping: each of the 18 end-to-end scenarios has a scenario file, a headless check, and (where marked) a Unity demonstration recipe recorded in the release checklist |

## 12. Risks

| Risk | Mitigation |
|---|---|
| The tutorial depends on divine packages not yet built | steps are ordered by tier; steps 8, 9, 11, 12 are marked 2 and are gated on their packages; the tutorial runs with the steps available |
| Staged prerequisites feel scripted | they only set initial conditions; outcomes come from the sim; the tutorial says so |
| Branching storage and speed | ring of snapshots with keyframes, configurable; branching is Tier 3; fast headless re-simulation |
| Objectives push outcomes (conflict with D5.33) | objectives only read results; none changes rules; they are optional and hideable |
| Generated islands are unviable | the F2 viability preview and reject-and-regenerate loop; the favourable scenario is a fixed handcrafted seed set |
| Scenario files diverge between Unity and runner | one loader, one schema, a parity test (H-GS-3) |
| Unlimited mode hides cost problems | the log still records Mythic spent, so experiments can estimate costs, and Normal remains the default for balance runs |

## 13. Owner D6 (27 Nov 2026): the industrial starting scenario

Owner decision D6.17 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)): an industrial starting scenario with an established population, appropriate technological knowledge, mines, vehicles, industrial organisations and electricity infrastructure, **on the same simulation as the prehistoric Living Island**; industrial integration tests never wait for prehistoric development; autonomous technological progression stays the long-term objective. Sim content is owned by the D6 documents ([INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md), [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md), [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md), [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md), [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md)); knowledge and designs by KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md, money by MONEY_AND_ACCOUNTING_DESIGN.md.

**Same simulation (GS-2, GS-3).** "Iron Coast" (working name, original) is a `Recording.Scenario` file plus a `PresentationSpec`, loaded by the same loader in Unity and `God.Runner`. It differs from Living Island only in **initial conditions and switches**: `Industry.Enabled = 1` (and `Civ`, `Orgs`, `Divinity` on), explicit deposits, and a setup block that creates founders, organisations, assets and stocks through the ordinary setup paths. It adds no rule, no industrial-only system and no scripted outcome. Everything after tick 0 is the autonomous simulation plus any divine input. A Living Island game that later develops industry by itself runs exactly the same code.

**What it contains (defaults, all editable in the file):**

| Element | Setup | How it is created honestly |
|---|---|---|
| Island and land | a generated or fixed seed; 2 factions; coast, hills and one mountain region | existing generator |
| Population | about 80 people per faction in established households, mixed ages (founders of the F2 favourable demography), skills seeded for some (mining, mechanics, smelting, driving) | M4 `Founders` mixed-age path; skills as founder attributes |
| Knowledge | specific named NPCs know the designs for mine, smelter, coal station, machine shop, engine works, harvester and hauler; the steel furnace design is open knowledge; one factory owner holds a licence for the harvester design | KNOWLEDGE doc seeding (knowledge held by individuals and records, not by a faction) |
| Deposits | iron ore and coal (Surface and Shallow) in the hills and mountain; one crude oil deposit near the coast (for the oil test); no uranium by default | the scenario `deposits` block (IND §3.3) |
| Organisations | a government per faction (C10 or the AiBrain seat); a private **mining company** (owns the mine, two harvesters, claims on the deposits); a **cooperative smelter**; a **public utility** (one coal station, links); a private **factory** (machine shop, engine works, assembly); a small **transport company** (one hauler); optionally a **military organisation** with a commander (I5 tests) | `Orgs.Found` with origin `Seeded` and their founders as members and office-holders; ownership models set |
| Assets | facilities built (C1 assets with `Embodied` materials recorded as placed), power links station → smelter and station → factory, vehicles parked at the mine and the depot | asset registration with reason `Placed` (`VehicleRegistered` with `Factory = 0`) |
| Stocks | coal in the station fuel bin (2 days), fuel in the depot, a little steel and parts in the factory input bin, food in households | `Goods.Produce(..., ProduceReason.Placed)` (or `Founder`) into the owners' bins: on the ledger from tick 0 |
| Employment | operators, miners, smelter workers, station staff and factory workers hold C3 contracts with their organisations | C3 contracts created in setup |
| Agreements | mining company → smelter (iron ore), mining company → utility (coal), utility → factory (electricity, metered), factory → mining company (one harvester ordered) | I3 agreement records with both office-holders recorded as agreeing |
| Divine | one human god and AI gods (D1), Normal Mythic by default; a Disabled variant for autonomous observation | D1 |

**Variants used by integration tests** (headless, `DotNet/God.Runner/Scenarios/ind_*.scenario.json`, owned by Q3 with the I packages):
- `ind_slice`: the minimal vertical slice island (one faction, about 30 people, one of each organisation above, no military), short horizon (5–10 sim days), checkpoints every sim day. Drives VS-1..VS-10.
- `ind_oil`: adds the well, a tanker and a refinery (`ACC_OilExtractedNoMoney`, `ACC_OilTransportedNoMoney`).
- `ind_grid`: three consumers, two stations, priorities (`ACC_ElectricityNoMoney`, `ACC_FuelShortageCutsGeneration`).
- `ind_waste`: waste on, a recycling plant (`ACC_WasteGeneratedCollectedReused`).
- `ind_military`: the military organisation and a procurement agreement (I5).
- `ind_full`: the full Iron Coast for Unity demonstration and replay (`Replay_IndustrialScenario_Exact`).

Each test scenario uses fixtures (`IndustrialFixtures`, by analogy with `IntegrationFixtures`) only to set initial conditions; outcomes come from the simulation.

**Autonomous technological progression (long-term objective).** The prehistoric Living Island keeps every industrial rule available: deposits exist (generation on when `Industry.Enabled`), manual extraction works without any design, and designs are discovered, taught and licensed through the KNOWLEDGE doc's research mechanics. Whether a Living Island society reaches industry is measured (first extraction, first smelter, first powered factory, by tick and by seed), never forced (D5.33).

**What the player sees (checkpoint presentation).** A busy industrial coast: harvesters crawling between the hill mine and the smelter with visible ore, smoke from the station, lit factory bays, power lines on poles; the facility, vehicle and station cards; the electricity overlay; an outage when a line is cut or the coal runs out; named workers with households, needs and opinions; the god influencing them only by divine means.

**Tier and package mapping:** `ind_slice` and the variants: Q3 with I1–I4 (Tier 2, after the civilisation and divine Tier 1, D6.18); `ind_full` and its Unity presentation: I6 with U3/U2 (Tier 2 end, or 3); military variant: I5 (Tier 2 end, 3). **Acceptance hooks:** **H-GS-11** `ind_slice` reaches VS-1..VS-9 within 10 sim days on seeds 1–3 and VS-10 reconciles (headless); **H-GS-12** the same file in Unity (batch mode) and in the runner gives equal hashes at day 1, 3 and 10 (extends H-GS-3); **H-GS-13** Iron Coast and Living Island run the same system list (a test compares `GodSim` system registrations: identical apart from switches); **H-GS-14** replay of `ind_full` with a possessed harvester driver and a divine compulsion of a lineman reproduces every checkpoint hash.
