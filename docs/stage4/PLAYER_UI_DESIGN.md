# Player interface design (stage 4, divine player)

Status: design, 26 Nov 2026. Owner decisions: D5.22 (hybrid interface), D5.23 (desktop first, mobile-compatible), D5.25 (decoding off by default), D5.28 (divine and institutional knowledge separate), D5.1 (god only). Brief sections 6, 27, 30, 31 and 36. Package: U1 (controls), U2 (toolbar, contextual actions, Mythic), U5 (reports), with U6 for the tutorial hooks. Sister documents: [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md), [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md), [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md), [GAMEPLAY_SCENARIOS.md](GAMEPLAY_SCENARIOS.md).

## 1. Purpose and scope

This document owns what the god player sees and clicks, outside the world rendering itself: the toolbar, the radial menu, the Mythic meter, selection cards, group commands, the commandment editor, the avatar and possession controls, time controls, alerts, developer overlays, NPC inspection, the family tree and life history, the memorial view, naming and following, and the institutional-report and divine-analysis panels.

It does not own the simulation of any divine power (that is DIVINE_PLAYER_DESIGN, MYTHIC_AND_MIRACLES_DESIGN, POSSESSION_AND_AVATAR_DESIGN, COMMANDMENTS_AND_RELIGION_DESIGN and RESURRECTION_DESIGN), the camera (CAMERA_AND_CONTROLS_DESIGN), or the look of effects (VISUAL_WORLD_DESIGN). It states what each of those must expose so that the interface can be built, and it holds the rule that the Unity layer never decides an outcome.

## 2. Current state (read from the code, 26 Nov 2026)

All paths are under `Assets/God/Game`.

| Area | What exists | Where |
|---|---|---|
| HUD technology | Immediate-mode GUI (`OnGUI`, `GUILayout.Window`), one partial class `GodHud` split by topic, a shared style (`HudStyle`), rectangles registered per frame in `_ui` so the camera ignores clicks over panels (`GodCameraRig.OverUi`) | `Hud/GodHud*.cs`, `Hud/HudStyle.cs` |
| Read and write rules | The HUD reads only through `FactionKnowledgeView` (`GodHud.View`) and writes only through `ICommandGate` (`GodHud.Gate`). A reflection test (`Hud_ReadsOnlyTheView`) enforces this | `Hud/GodHud.cs` header |
| Player model | The HUD acts for one human faction (`Me`); the build bar (Gather, Process, Arms, Army, Land tabs), HQ, road, demolish, trade and VP standings are faction controls. Watch mode has no human | `GodHud.Tabs.cs`, `GodHud.Standings.cs`, `GodHud.Trade.cs` |
| People panel | `GodHud.People.cs`: click a person (`PickNpc`, nearest figure on screen within 2.5 percent of screen height) and a 420 px card opens with tabs Mind, Household, Relations (`PersonTab`). Mind shows age, doing, health, nutrition, injury, five drives, carrying, last decision with the term breakdown (need, instinct, learned, personality, cost, explore), runner-up, "because" habits, options weighed, recent experiences, attending, beliefs, strongest habits. The card is `NpcInspection` from the faction's knowledge view; only the viewer's own people have a card, and in watch mode the person's own faction answers | `GodHud.People*.cs`, `Sim/Information/PopulationKnowledge.cs` |
| Household and relations tabs | Life stage, household members as believed, store, partnership, care, family by generation as believed; relation rows with trust, familiarity, affection, grudge, deals and gifts each way, learned values keyed on the other person, open deals and heard messages | `GodHud.People.Household.cs`, `GodHud.People.Social.cs` |
| "Why" learning view | The Mind tab's "Last decision and why", "Options it weighed" and "Habits learned" sections, and the `LearnedChangedWinner` and `ExploreWon` flags | `GodHud.People.cs` |
| Time controls | Pause (Space), speed buttons 1x, 2x, 4x (`Speeds`; watch mode adds 8x and 16x), `+` and `-` step, calendar label "Day 3, year 1". `GodBootstrap` also has debug keys F3 (run), F4 and F5 (speed to 64x). The sim steps at 10 Hz times `Speed` | `GodHud.cs`, `GodHud.Gui.cs`, `Boot/GodBootstrap.cs` |
| Alerts | `ToastFeed`: one-line toasts for war, peace, HQ capture, elimination, Citadel, fading after 4.5 s, at most 4 shown. A single `ToastText` in the HUD | `Menu/ToastFeed.cs` |
| Developer overlays | True-state panel (F8 or the "True state" button): known / true per faction, only where the game allows it (developer mode, watch mode and other observation games, never a competitive game). Node and build-circle overlay (`TerritoryView.ShowDebugOverlay`, off by default). Dice tick and economy debug text switched off by the HUD | `GodHud.TrueState.cs`, `View/TerritoryView.cs`, `GodHud.cs` |
| Menus | `GameMenu` (pages Start, Main, Save, Load; New island, Watch AIs, Start tutorial, "Living island" start with a seed field and Roll seed) and `SaveStore` (3 slots) | `Menu/GameMenu.cs`, `Menu/SaveStore.cs` |
| Tutorial pointers | Gold pulsing frame, bobbing diamond and map ring (`GodHud.Tutorial.cs`), step cards (`Tutorial/TutorialCards.cs`), step goals as pure predicates in `Sim/Tutorial/TutorialSteps.cs` | listed |
| Gesture text | `GestureLayer` writes the composed message as text above the speaker and draws a line to the addressee, unconditionally. This breaks acceptance row M5.18 (see section 14) | `Models/GestureLayer.cs` |

Nothing divine exists yet: no Mythic, no god identity, no divine commands, no possession, avatar or commandments. The People panel has no name, no family tree, no life history, no memorial and no faith, employment or economy tab.

## 3. Decisions

| Id | Decision | Why |
|---|---|---|
| UI-1 | **World first, panels second.** Everyday divine acts are done on the visible world through a compact toolbar and a contextual radial menu. Panels are for analysis, history, doctrine and records (D5.22) | brief section 30 |
| UI-2 | **Keep IMGUI and the `HudStyle` for Tier 1.** New panels are added as further `GodHud` partials (or a sibling `DivineHud` class that follows the same two rules). A move to UI Toolkit is a Tier 3 option, taken only if touch scaling needs it. Every panel draws through a `HudScale` multiplier (default 1.0, 1.5 on touch) | the existing HUD works, is test-covered, and rewriting it would spend the run on no gameplay |
| UI-3 | **Two read channels, one write channel.** New panels read the world only through a divine observation view (`IDivineView`, section 5) and the event stream, and write only through `ICommandGate`. The old `FactionKnowledgeView` remains for the temporary stage 3 faction mode. The reflection test is extended to cover every new HUD type | keeps M1 intact |
| UI-4 | **Every datum on screen carries a provenance tag** (section 12). A panel cannot show a value without saying whose knowledge it is | brief section 31 and 36, D5.28 |
| UI-5 | **The stage 3 build bar, HQ, road, demolish, trade and VP panels are hidden in the god-only game** and kept behind the temporary "faction control" mode (see UNITY_POC_PLAN, "Owner D5 presentation"). They are removed when stage 3 faction control is retired | D5.1 |
| UI-6 | **Pausing is encouraged.** Commands issued while paused are validated at once with `Gate.Check`, queued, and applied when time next runs or when the player presses Step (one tick; Shift+Step one second). The old "step once when a person command waits" behaviour (`GodBootstrap._personCommandWaiting`) is kept only for the stage 3 mode | brief section 30 |
| UI-7 | **Default knowledge profile for the god is configurable** (`DivineSight`: Omniscient-bodies, Perceptive, Records-only). Sandbox default is Omniscient-bodies: the god sees every body and the minds as the NPC holds them, never the analytics-only truth flags | brief section 36 ("according to configured rules") |
| UI-8 | **Decoded message text has two separate switches, both off by default:** "Captions" (accessibility, plain language from what a watcher could see) and "Decode overlay" (developer, intended versus decoded with the mismatch marker). Neither changes the sim hash | D5.25, M5.18 |
| UI-9 | **Procedural, original icons.** Toolbar and radial icons are drawn from primitives (rings, chevrons, rays, spirals) by a small `IconKit`; no imported art | CLAUDE.md |

## 4. Layout (desktop 16:9; mobile rules in section 16)

```text
+----------------------------------------------------------------------------------+
| [Day 3, yr 1] [pause][1x][2x][5x][10x][>>]        MYTHIC [=======----] 62/100 +20/day | <- top strip
| alerts column (right, under strip)                                                   |
|                                                                   +-------------+ |
|  roster chips (left): favourites and followed, up to 12           | Person card | |
|   (o) Aveth  (o) Kor *                                            | (People     | |
|                                                                   |  panel)     | |
|                    [ world: the island, always the focus ]        +-------------+ |
|                                                                                   |
|        selection chip: "Aveth, 24, gathering"      [Possession pill / Avatar pill] |
| [Inspect][Instruct v][Miracle v][Embody v][Reveal v][Analysis]  <- compact toolbar |
+----------------------------------------------------------------------------------+
```

- The top strip extends the existing cloud strip (`GodHud.Gui.cs`): calendar, time controls, then the Mythic meter.
- The toolbar is bottom centre and at most 6 slots; each slot may open a short tray.
- The person card keeps its place at the right (`PersonPanel`, 420 px wide, scrolled), so existing muscle memory and tests stay valid.
- Panels that are large (Analysis, Commandment editor, Family tree, Memorial) are modal-free windows that can be moved and closed, registered in `_ui` so world clicks pass through the gaps only.

## 5. Sim-interface rules (what each view reads)

### 5.1 Principles
1. The Unity layer reads state, sends commands, animates events. It never mutates the sim and never decides success (CLAUDE.md, brief section 28).
2. Costs, legality, targets and effects shown in the UI come from `Gate.Check` and the sim's own catalogues, never from a Unity-side copy of the rules. This is the pattern `RefreshSpots` already uses for legal building spots.
3. Anything the player types or clicks that matters becomes a `Command` in the replay stream. UI-only state (selection, camera, names the player gives, favourites, panel layout) is not in the stream and not in the state hash (section 15).

### 5.2 Provisional contracts the UI binds to
The names below are the **contracts' names** (SHARED_ARCHITECTURAL_CONTRACTS §9, §10.2, §13 and §19.2; LEAD_RULINGS R13). Where this document's first draft used another name, the row says so. Field lists are the divine packages'.

| Need | Provisional contract | Owner package |
|---|---|---|
| God identity, Mythic | `IDivineView` (kept, SAC §9): `GodRecord` fields `Mythic` and `MythicMax` (milli-Mythic), `Mode` (`Standard`, `Unlimited`, `Disabled`), cooldowns; rates are tuning `Divinity.Mythic.RegenPerDay` and `Divinity.Upkeep.<Kind>PerTick`. The first draft's `GodView`, `RegenPerSec` and `UpkeepPerSec` are retired; the UI shows per-day rates and time to full | D1 |
| Power catalogue | **No sim-side `IDivineCatalog`** (R13). U2 builds its catalogue from `Gate.Check` and the tuning costs, upkeep and cooldowns, as rows `{ Power, Kind (Suggestion, Command, Compulsion, Possession, Miracle, Commandment, Resurrection, Avatar), Cost, Upkeep, Cooldown, Valid, Reason }` | U2 (reads D1, D2, D3) |
| Instruction | commands `IssueSuggestion`, `IssueCommand` and `IssueCompulsion { God, Act (the DivineAct vocabulary), Target, Scope, ExpireTicks }` with `DivineScope { Individual, Household, Group, Org, Location }`; record `Instruction` (SAC §10.2). `DivineInstruction`, `Person`, `Institution` and `Area` are retired names | D2 |
| Divine control input | `DivineControlInput { God, Body, Kind (Heading, MoveTo, Stop, Action), Heading, Gait, Node, Act, Target }` (SAC §10.2 and §11.3; see CAMERA_AND_CONTROLS_DESIGN section 8) | F1 (handler), D4 (absorbs D5) |
| Possession and avatar state | `PossessionView` and `AvatarView`: read-only structs of `IDivineView`, filled by `DivineViewFiller` from `ControlTable` (SAC §19.2); fields as the D4 spec (upkeep is per tick; the UI converts to per-day rate and time left) | D4 (absorbs D5) |
| Commandments | the `Commandment` and `CommandmentRule` records of `DivinityState.Doctrine` (SAC §13.2) read through `IDivineView`; commands `RevealCommandment`, `ReviseCommandment`, `RevokeCommandment`. `CommandmentView` (with `KnownByCount`, `FollowedShare`) is a U2/U5 presentation composition, not a contract type; the sentence is rendered by `CommandmentText.Render` in U2 (R6) | D6 |
| NPC divine facts | `DivineMind` (`GodBelief` with the five separate fields, `KnownCommandment`; SAC §13.1) through the inspector view; `NpcFaithCard` is a U2 presentation composition of those fields, the affiliation spells and the citizenship record | D6 |
| Names and life | `Names.Person(seed, npcId)`, `Names.God`, `Names.Org` (F1.18, pure functions; `NpcName` is retired); `LifeRecord(npcId)` and `FamilyGraph(npcId, depth)` are U5 helpers over the archive (`fact_life_spell` lives, SAC §4.4) and the kin views, not contract types | F1, U5 |
| Institutional records | the organisation's own `OrgRecords` as shown by `InstitutionView` (C6, keyed by `OrgId`) and, to the god, through `IDivineView` in `Reported` mode; `InstitutionalRecordView` is a U5 composition | C6, U5 |
| Provenance | every field carries its layer tag (SAC §9: inspector cards with their layer tags); `Sourced<T> { Value, Source (Body, Mind, Record, Truth), AsOfTick }` is the U2 wrapper over those tags | F1, U2 |
| Events | the `SimEvent` stream already raised on `GodBootstrap.SimEventRaised`; divine events listed in VISUAL_WORLD_DESIGN section 11 | D1 to D7 |

### 5.3 What each view reads

| View | Reads | Writes (commands) |
|---|---|---|
| Mythic meter | `IDivineView` god fields, `MythicCredited`, `MythicSpent` and `MythicUpkeepCharged` events (804–806), the cost of the armed power from `Gate.Check` | none |
| Toolbar and radial | the U2 catalogue (`Gate.Check`), selection | `IssueSuggestion`, `IssueCommand`, `IssueCompulsion`, `CastMiracle`, `RevealCommandment`, `Resurrect`, `BeginPossession`, `ManifestAvatar` |
| Selected person card | `NpcInspection`, the `DivineMind` composition (`NpcFaithCard`), `Names.Person`, layer tags | instructions to that person |
| Selected object card | object kind, owner, stock (divine observation), affordances | smite, create or destroy resources, consecrate |
| Group tray | the sim's resolution of scope (`ScopeResolved { Scope, Members }` event or `IDivineView.Resolve(scope)`) | one instruction with a scope, never a list built in Unity |
| Commandment editor | `CommandmentView`, templates, `Check` reasons | `RevealCommandment`, `ReviseCommandment`, `RevokeCommandment` |
| Possession and avatar pills | `PossessionView`, `AvatarView`, needs of the possessed body | `DivineControlInput`, `EndPossession`, `DismissAvatar` |
| Alerts | the event stream through `AlertAggregator` | none |
| Family tree, life, memorial | `FamilyGraph`, `LifeRecord`, `Estate*` events | `Resurrect` variants |
| Reports and analysis | `InstitutionalRecordView`, KPI source (warehouse reader, Mode D only) | none |

## 6. Compact divine toolbar and divine power selection

Six slots, original procedural icons, each with a tooltip, a hotkey and the current cost.

| Slot | Contents | Hotkey (god-only game) |
|---|---|---|
| Inspect | the default pointer: select, hover details | `Q` held or `Esc` returns to it |
| Instruct | tray: Suggest, Command, Compel (three distinct buttons with the three distinct icons and colours of VISUAL_WORLD_DESIGN section 9), then an action picker (section 7) | `1`, `2`, `3` |
| Miracle | tray: Heal injury, Heal disease, Bless, Protect, Smite (person, building, tree or resource), Create resource, Destroy resource, Consecrate, Reveal knowledge, Area miracle, Resurrect; later weather and terrain | `4` |
| Embody | Manifest avatar, Possess selected person, Release (when active) | `5` |
| Reveal | Commandments list and editor, Reveal knowledge or technology | `6` |
| Analysis | opens Mode D: reports, KPIs, history, SQL (advanced) | `7` or `F9` |

**Power selection** works like the existing tool modes (`HudMode`, `BeginPlace`): choosing a power "arms" it (a new `HudMode.DivinePower` with `ArmedPower`). While armed, the cursor carries a ghost: the targeting shape (ring for a person, brush circle for an area), the cost chip, and green or red validity from `Gate.Check` evaluated for the hovered target. Left-click confirms, right-click or `Esc` cancels (`Cancel()` is already bound to right-click without drag). Shift keeps the power armed for repeats (as `keepTool` does today).

The armed power's colour and silhouette match its effect (VISUAL_WORLD_DESIGN section 9), so the player learns one visual language for the toolbar, the ghost, the in-world effect and the alert.

## 7. Contextual radial menu

- **Open:** right-click without drag on a person, building, object or ground point; touch: long-press (350 ms default). Right-drag still pans (`GodCameraRig.Dragged` already tells click from pan).
- **Shape:** an inner ring of up to 6 intervention types applicable to the hit target, an outer ring of up to 8 specific actions for the highlighted type. Items are rendered from `IDivineCatalog.Available`, so a disabled item shows its reason (`Reasons`) and cost, never hides silently.
- **Contents by target:** person (Inspect, Follow, Favourite, Suggest, Command, Compel, Possess, Bless, Heal, Protect, Smite, Resurrect if dead), building (Inspect, Smite, Consecrate, Command its operators), resource source (Smite, Create, Destroy), ground (Move avatar here, Consecrate, Create resource, Area miracle, Place sacred marker), group (Instruct group, Heal all).
- **Instruction builder:** choosing Suggest, Command or Compel opens a small popover: Action (the 21 actions of brief section 9, filtered to those valid for the target), Target (defaulting to the click, changeable by picking in the world), Scope, Expiry (defaults: Suggest 1 day, Command 2 days, Compel until done or 1 day; all configurable). The popover is a form over `DivineInstruction`. There is no free text.
- **Keyboard and gamepad:** number keys choose ring items; mobile uses the same data in a bottom sheet (section 16).

## 8. Mythic meter

- A bar with current over maximum, numbers shown, a fixed regeneration tick mark (per sim day, "+20/day" default, MYTHIC §4.2; the meter also shows the time to full), and a draining segment for upkeep while possession or an avatar is active, with the time left (LEAD_RULINGS R12).
- **Cost preview:** hovering an item or arming a power shows a ghost segment of the cost; red if it exceeds the balance.
- **Modes:** Normal (bar), Unlimited (infinity glyph, spending still logged and shown as the ghost), Disabled (greyed meter, power slots hidden; observation and analysis only).
- **Warnings** (all configurable): below 20 percent the bar pulses amber; 10 s before possession or avatar upkeep exhausts it, the pill flashes; at zero possession ends (sim) and the meter shows "Exhausted".
- The same meter component can show a rival god's Mythic in the Analysis panel when the observation rules allow it (D5.26: same rules for AI gods).

## 9. Selected-person and selected-object controls

**Person chip and card.** Selecting a person shows a floating chip over the figure (name, age, activity icon, health ring) and the card on the right. The card's header holds: name (editable inline, section 13), the favourite star, Follow, close; then the divine verbs for that person. Tabs: Overview, Mind, Household, Relations, Family, Life, Economy, Faith, Divine (section 11). The existing tabs Mind, Household and Relations keep their places and code.

**Object card.** For a building, resource source, store, tree, shrine or lot: kind, owner and permission (divine observation), stock and affordances, then Smite, Create resources (kind and amount picker, shows the ledger effect), Destroy resources, Consecrate or Deconsecrate, Inspect operators. Resource creation shows the cost preview and a note "creates N units in the ledger".

**Mini needs strip.** Hunger, tiredness, cold, fear and pain as small bars (reusing `DriveBar`), always visible in the chip when the person is followed, possessed or compelled.

## 10. Group commands and selection scopes

Scopes (matching brief sections 9 and 26): Person, Group (a selection set), Household, Institution, Area, Location. The tray under the toolbar shows the selection as chips with a count and a scope label.

- Unity sends one instruction naming the scope (a household id, an institution id, an area as centre node and radius in lattice steps, or an explicit person list for a hand-picked group). Membership of an area or household is resolved by the sim at the application tick, not by Unity (CAMERA_AND_CONTROLS_DESIGN section 6).
- **Responses are aggregated.** After issuing, the tray shows "7 delivered, 5 followed, 1 refused, 1 not reached" built from the response events, and a click expands the list. Institution-level instructions show "delivered to the relevant officials (3)" and a link to the institution's report; governments may refuse, and the tray shows that as an ordinary refusal, never as an error.
- A group is never ordered "to obey"; the instruction kind (suggestion, command, compulsion) is fixed per issue and cost scales with the number reached by the sim's own rule.

## 11. NPC inspection (brief section 31)

The People panel is retained and extended. Tabs and where every required field lives:

| Field (brief section 31) | Tab | Source channel | State today |
|---|---|---|---|
| Name | header | Body (public name), alias is player-only | needs F1 `NpcName` (procedural names) |
| Age | Overview | Body | exists (`HudAge`, `Founder`, `Stage`) |
| Health | Overview | Body | exists (`Health`, `Nutrition`, `Injury`); illness and fatigue to add |
| Needs | Overview | Body (drives are what the person feels) | exists (`Drives` hunger, tiredness, cold, fear, pain) |
| Current activity | Overview | Body | exists (`Doing`) |
| Intentions | Mind | Mind | exists (`LastDecision`, `Opportunities`) plus open divine instructions |
| Possessions | Economy | Body (carried) and Truth (owned vs held) | carried exists (`Carry`); owned goods need C1 |
| Equipment | Economy | Body | tools exist in the figure (axe); equipment list needs C1 |
| Household | Household | Mind (as believed) | exists (`Household` card) |
| Family tree | Family | Body (true genealogy) with a toggle to Mind (as believed) | believed family exists (`Household.Family`); true graph needs `FamilyGraph` |
| Relationships | Relations | Mind | exists (`Relations` card) |
| Trust | Relations and Faith | Mind | exists towards persons (`Trust`); towards the god needs D6 |
| Fear | Overview (drive) and Faith | Mind | drive exists; fear of the god needs D6 |
| Beliefs | Mind | Mind | exists (`Beliefs`); religious beliefs need D6 |
| Faith | Faith | Mind | needs D6 |
| Learned associations | Mind | Mind | exists (`HabitsPositive`, `HabitsNegative`, `Why`, entry counts) |
| Recent experiences | Mind | Mind | exists (`Experiences`); divine events as experiences need D2 |
| Employment history | Economy | Record (if employed by an institution) and Mind | needs C3 |
| Economic relationships | Economy | Mind and Record | deals exist (`Relations`); employment, debt, ownership need C1 to C3 |
| Religious affiliation | Faith | Mind (self-declared) and Record (religious institution) | needs D6; kept separate from citizenship (D5.15) |
| Known commandments | Faith | Mind | needs D6 |
| Divine commands | Divine | Mind (perceived) with the true issue beside it in Truth mode | needs D2 |
| Reasons for decisions | Mind | Mind | exists (`LastDecision`, `Why`, term breakdown); divine instruction weight added as a term by D2 |

**Behaviour.** The card is for any NPC the god can observe under `DivineSight`, not only the viewer's own faction (a change to `InspectionOf`). Each tab section has a header chip with its channel (section 12).

## 12. Provenance: authoritative truth versus what the NPC or government knows

Four channels plus one external:

| Channel | Meaning | Chip colour and shape | Where it appears |
|---|---|---|---|
| **Body** | what the god observes of a body and place (health, injury, position, carried items) under `DivineSight` | gold, sun glyph | Overview, Economy (carried) |
| **Mind** | what the NPC itself holds (beliefs, memories, relations, associations, its reading of messages) | blue, thought-bubble glyph | Mind, Household, Relations, Faith |
| **Record** | what an institution holds, with the institution, as-of tick and coverage | green, ledger glyph | Economy, Reports |
| **Truth** | authoritative debug state, including analytics-only truth flags | magenta hatched panel, "DEBUG TRUTH" watermark, warning glyph | only with the developer truth toggle |
| **Warehouse** | external analytical recording (Mode D only) | grey, database glyph | Analysis |

Rules:
1. A chip is colour, glyph and word, so it works without colour vision.
2. Truth never replaces what the NPC believes. With Truth on, a field shows both values side by side with a "differs" mark: "believes 3 berries, truth 5". The same applies to household members, kin, trust, and debts.
3. The Truth channel is available only where the game allows it today (developer mode, observation and sandbox games, never a competitive objective game), switched by F8 or the existing "True state" button, which is extended to the card. A scenario can lock it off.
4. No institution, AI god or policy ever reads Body, Truth or Warehouse data; only the human UI does. This keeps D5.28 and the reflection tests.
5. The Mind channel is exactly the knowledge view's card as today (`NpcInspection`), now fetched for any NPC under divine observation.

## 13. Names, favourites, following

- **Procedural name** (given name, family name from the lineage) is a sim fact (F1), stable from birth, used by events, records and the warehouse.
- **Player alias, favourite star, follow, note** are annotations in a `PlayerAnnotations` store keyed by persistent NPC id. They live in the save file's UI section, are never hashed, never reach the sim, and the NPC never knows its alias. The card shows the alias with the true name beneath.
- **Roster chips** along the left edge list favourites and followed people (default maximum 12), each with a health ring, an alert dot, and a click to jump (`GodCameraRig.FocusOn`). Following is the camera mode of CAMERA_AND_CONTROLS_DESIGN section 5 and also raises the alert priority of that person.
- A followed or favourite person's death is always an individual alert (section 14) and opens the memorial view on demand.

## 14. Alerts with aggregation

`AlertAggregator` (pure C#, no Unity types, EditMode-testable) turns the event stream into cards. It replaces `ToastFeed` for the divine game and keeps its wording style (a faction-colour swatch, one line).

| Class | Examples | Rule |
|---|---|---|
| Critical, individual | followed or favourite person in danger or dead; possession ended; avatar destroyed; Mythic exhausted; commandment broken by a priest (if tracked) | one card each, persists until dismissed, optional jump |
| Important, aggregated | famine in an area, epidemic, birth wave, institution formed, commandment spread milestone, war, resurrection result | one card per (class, cause, area) per window |
| Info, digest | trades, ordinary births, ordinary deaths, tools made | one digest card per sim day: "34 trades, 5 births, 3 deaths" |

- **Window and thresholds** are configurable (`AlertTuning`): window 1 sim day, individual cap 3 cards per 10 s, digest at day end.
- **Rules from the brief:** no card for every transaction or death; deaths of people the player does not know are counted ("3 died of hunger near the north forest").
- **Card content:** icon (the intervention or event glyph), one line, place, a jump button (`FocusOn`), a snooze for the category, a pin. A log panel keeps the history with filters.
- **Speed interaction:** an accelerated run auto-drops to a configurable speed (default 1x) on a Critical alert if "Pause on critical" is set; default is to slow to 2x and flash, not pause.

## 15. Time controls

Extends `GodHud.SetSpeed` and `Speeds`.

| Control | Behaviour |
|---|---|
| Pause (Space) | stops stepping; the world, camera and UI stay live; commands are queued (UI-6) |
| 1x, 2x, 5x, 10x | `Boot.Speed` 1, 2, 5, 10 (default set `{1, 2, 5, 10}`, configurable; the stage 3 set 1, 2, 4 and 8, 16 is kept for the faction mode) |
| Accelerated (`>>`) | steps as fast as a per-frame budget allows (default 8 ms of sim per frame, cap 50x, configurable), for autonomous observation; drops to the cap when the budget is exceeded, shows the achieved rate |
| Step (`.`) | one tick; Shift+`.` one second (10 ticks) |
| Limits while embodied | possession and avatar limit speed to a configurable cap (default 2x), because control input is sampled once per tick |

Rules: the speed change is a UI input and not a sim command (the sim does not know the speed); pause, speed and step are recorded in the replay only as the ticks at which commands applied, which already makes a replay exact at any speed. The calendar label stays (`CalendarText`).

## 16. Mobile-compatible architecture (D5.23)

- All gameplay input goes through `PlayerIntent` objects (CAMERA_AND_CONTROLS_DESIGN section 3); no logic reads `Input.*` outside the input adapters.
- No panel requires hover (tooltips become tap-and-hold), no action requires a right button, minimum touch target 44 dp at `HudScale`, the radial menu and toolbar trays have a bottom-sheet presentation selected by `Screen.dpi` and touch support, and modal text entry is limited to the rename field.
- Desktop is implemented and tested first; the touch adapters and bottom sheets are Tier 3, but the data they present is already the same.

## 17. Commandment editor (structured builder)

A window with four columns; there is no free-text rule entry and the sim never parses the preview sentence (brief section 12).

1. **Template picker:** the twelve templates of the brief (share surplus food with hungry children, do not steal, honour agreements, protect forests, teach children, assist strangers, care for elderly people, forgive those who repent, protect communal property, contribute to public services, defend your community, treat injured individuals). Choosing one fills the fields; "Blank" starts empty.
2. **Fields (structured):** Modality (Required, Prohibited, Encouraged); Action (from the brief section 9 vocabulary); Object class (food, trees, goods, buildings, persons by class); Applies to (everyone, a group by role, age class, household, institution); Conditions (a list from a fixed catalogue: the other is hungry, the other is a child, the other is a stranger, goods are communal, in a place, in a season); Geographical scope (island, region, radius round a sacred place); Importance (1 to 5); Expiry or permanent.
3. **Preview and checks:** a generated sentence ("Required: share surplus food with hungry children, everywhere, importance 4"), the Mythic cost, `Check` reasons (conflicts, invalid combinations), a note for the sim's conflict resolution ("individuals may weigh this against other commandments").
4. **Delivery:** how it is revealed (to one person, a priest or leader, everyone in an area, a dream to many), shown with the cost; then Reveal. Later: Revise (keeps history), Revoke.

The list view shows every commandment with status, issue date, revision count, how many know it, how many follow it (divine observation, with the Mind or Record caveat chip), and a link to the spread map overlay (people who know it as tinted dots, a diagnostic overlay).

## 18. Avatar and possession controls

**Mode switcher** (top left of the toolbar region): A Overhead, B Avatar, C Possession, as three pills with a shared camera (CAMERA_AND_CONTROLS_DESIGN section 5).

**Possession pill** (while active): the person's name and portrait; Mythic per sim day (upkeep is charged per tick) and the time left; a Release button (hotkey `Backspace`, also ends on `Esc` after a confirm if the NPC is in danger); the body's needs strip (hunger, tiredness, fear, pain, health) in prominent bars, because the body keeps suffering; a context action bar listing only the actions that NPC can do now (`IDivineView.AvailableActions(npc)`, taken from the existing NPC action primitives, never a second list); a "divinely controlled" tag that appears on the card and in life history afterwards.

**Avatar pill** (while manifested): the avatar's divine name, health, Mythic upkeep, carried object slot, the nearest affordance prompt (key `E`), a gesture wheel listing the vocabulary of NPC_ANIMATION_DESIGN (the player selects a composed gesture; it is a sim command), Dismiss. If the avatar is destroyed the event drives a return to overhead and a Critical alert with the cooldown.

**Movement** is described only in CAMERA_AND_CONTROLS_DESIGN section 8; the UI never moves a body.

## 19. Developer overlays

All off by default in the player; a `DevOverlays` flags set persisted in `UserSettings`-style local preferences (never in the save, never hashed). Available where the truth channel is available (rule 3 of section 12).

| Overlay | Shows | Exists |
|---|---|---|
| Node lattice | fine movement nodes, build circles, legal spots | yes (`TerritoryView.ShowDebugOverlay`) |
| Hex regions | the 19 large hexes as outlines with numbers and ids; regions by terrain and resource | needs a small `HexRegionOverlay` (reads `Board`) |
| Accessibility | passable, blocked, cost per node; why a node is unreachable | needs a view over the lattice and `Pathing` |
| True state | known versus true panel and card Truth channel | yes (F8, `GodHud.TrueState.cs`), to be extended |
| **Decode overlay (M5.18)** | intended message and each listener's decoded message, with the mismatch marker and the `Honest` flag, above the speaker and the listeners | the current unconditional text in `GestureLayer` becomes this overlay and defaults to off |
| Intent lines | each NPC's current intent target as a line | needs a view over `Doing` |
| Perception | sight radius and the nearest 8 attended people | needs `CommsRange` values |
| Ownership and permission | goods and land tinted by owner or permission | needs C1 |
| Command and event log | the tail of the command and event stream with ids and ticks | partly (`SimEventRaised`) |
| Performance | tick time, NPC count, stepped ticks per second, hash | partly (`SmokeRun`) |

Hotkeys use a chord (`Ctrl+Shift+D`, then the letter) so no normal-play key is lost. Mobile builds hide the whole set.

## 20. Divine analysis (Mode D) and institutional reports

**Separation.** The panel has two columns on every report and KPI:

- **Institutional record** (green): compiled by a named institution, as-of tick, coverage percentage, with its mistakes and delays. Mythic cannot fix it; the player can change it only by acting on the people who keep records.
- **Divine observation** (gold): what the god sees right now under `DivineSight`, always current, with the exact count where `DivineSight` allows it.
- A **difference bar** shows the gap: "Records: 41 adults. Observed: 47." Never a single merged number.
- **Warehouse analysis** (grey, advanced): the `fact_*` tables through a read-only query console and canned charts. It is an external instrument, not knowledge inside the world; it is labelled as such and no institution can read it.

**Views.** Population and households (counts, pyramid, births, deaths, childhood mortality), economy (stocks, wealth distribution, employment, ownership concentration), institutions (list, members, services, unmet demand), health, education, justice, military, religion (affiliation, known commandments, compliance), trust and fear, divine (interventions, Mythic spent, responses). Each KPI has an info popover showing its definition, period, denominator, source and limitations (brief section 35, KPI_CATALOGUE); there is no universal wellbeing score. Charts are simple line, bar and stacked-area drawings in IMGUI with explicit axis labels.

**History.** A timeline of epochs (settlement founded, first institution, famine, war, commandment issued, resurrection), each linking to the camera location and the alert log; and the family tree and memorial views (section 21).

## 21. Family tree, life history, memorial

- **Family tree.** Rows by generation, nodes are persons with a small portrait glyph, name, years; links are drawn differently for **partnership** (a horizontal bar), **biological parenthood** (solid), **adoption or fostering** (dashed) and **caregiving** (dotted), because the brief separates them. The dead are shown dimmed with a cross, the resurrected with a ring. Click a node to recentre; scroll zooms. A toggle chooses "true genealogy" (Body channel) or "as this person believes" (Mind channel, from `KinBeliefFormed` data); differences are marked.
- **Life history.** A scrolling timeline from birth: named, matured, partnerships, children, households, jobs, injuries and illnesses, divine suggestions received, commands, compulsions, possession periods (marked divinely controlled), miracles, resurrection, death. It reads `LifeRecord(npcId)`, a bounded sim-side record, with the oldest entries compressed (sim decision, F1/D7).
- **Memorial view.** For a dead person: a frozen figure in a procedural memorial pose, name, birth and death dates, cause, household and family links, notable deeds taken from `LifeRecord`, what happened to their estate (`Estate*` events), and a resurrection panel (eligibility, the four variants of brief section 11 with cost previews, and the consequences stated: estates stay, partnerships are not restored). A "Remembered" list holds everyone, filterable by household, generation, cause. In the world, a small cairn marker stands where a person died or at the settlement's memorial ground (VISUAL_WORLD_DESIGN section 12).

## 22. Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.

| Capability | Package | Tier |
|---|---|---|
| Time controls (pause, 1x, 2x, 5x, 10x), queued commands while paused | U1 | 1 |
| Selection chip, person card for any NPC, instruct popover, Mythic meter, toolbar Instruct and Miracle | U2 | 1 |
| Alerts with aggregation, group tray with aggregated responses, divine power arming and ghost | U2 | 1 |
| Commandment editor and list | U2 (with D6) | 1 (U2a) for the template picker (choose a template, fill its slots, reveal; LEAD_RULINGS R11), 2 (U2b) for the free structured builder and the spread view |
| Names, favourites, follow, roster chips | U1 | 1 |
| Developer overlays (decode overlay, hex regions, truth channel on the card) | U1/U2 | 1 for decode overlay and truth, 2 for the rest |
| Possession pill, avatar pill, mode switcher, gesture wheel | U2 (with D4a, D4b) | 2 |
| Family tree, life history, memorial and resurrection panel | U5 (with D7) | 2 |
| Institutional reports with record versus observation columns, KPI popovers | U5 | 2 |
| Warehouse read-only query console | U5 (with Q1) | 3 |
| Accelerated speed with budget, accessibility captions, mobile bottom sheets and touch scaling | U1/U2 | 3 |
| Tutorial pointers on the new UI | U6 (deferred, index §0) | 3 |

## 23. PlayMode-testable acceptance hooks

Following the existing pattern (public methods on the HUD, `DebugCursor`, `InputEnabled = false`).

| Hook | Test |
|---|---|
| H-UI-1 | With a divine scenario loaded, `Hud.SelectNpc(id)` opens a card for an NPC of another faction; `PersonCard` is non-null and carries a `Mind` source tag |
| H-UI-2 | `Hud.ArmPower(power)`, hovering a target sets `Ghost.Valid` equal to `Gate.Check(...)`.Ok for the same command, in 50 random targets |
| H-UI-3 | Issuing a suggestion while paused queues it: `Boot.Sim.Tick` unchanged, a pending command exists; after `Step()` the matching event is raised |
| H-UI-4 | `Hud_ReadsOnlyThroughViews`: reflection test that no new HUD type references `GodState` or `GodSim` |
| H-UI-5 | `AlertAggregator` over a recorded 3-day event log yields at most 1 digest card per day and no card per `Ate` or per trade; a followed person's death yields one individual card |
| H-UI-6 | Decode overlay off: no `TextMesh` named `GestureLabel` exists in any figure in a 5-day Living island run (M5.18); on: labels exist and the state hash is identical |
| H-UI-7 | Mythic mode Unlimited: meter shows the infinity glyph and the balance does not fall; Disabled: power slots hidden and `Gate.Check` of a divine command is rejected |
| H-UI-8 | Truth channel: with `ToggleTrueState()` on, a field the NPC misbelieves shows two values and a "differs" marker; off, no Truth text is drawn |
| H-UI-9 | The family-tree builder draws partnership, parenthood, fostering and caregiving links as four distinct link kinds for a seeded household with an adoption |
| H-UI-10 | Renaming, favouriting and following change `PlayerAnnotations` and the state hash is unchanged |
| H-UI-11 | Time: `SetSpeed(5)` then 100 frames steps within 5 percent of 5x ticks at fixed `Time.deltaTime`; accelerated never exceeds the frame budget |
| H-UI-12 | Possession pill shows the body's drives, and `Release` sends exactly one `EndPossession` command |

## 24. Risks

| Risk | Mitigation |
|---|---|
| IMGUI is slow to lay out for dense panels (family tree, charts) and poor for touch | draw big graphs with `GL` or a mesh, cap panel detail by LOD; UI Toolkit migration is a Tier 3 option behind `HudScale` |
| The UI invents rules (cost, validity) | everything comes from `Gate.Check` and the catalogue; a test compares them |
| Provenance tags are forgotten | the `Sourced<T>` type and the card builder fail to compile or assert in tests without a source |
| Alert flood at high speed | aggregation by window, caps, digest; thresholds configurable |
| Panels cover the world | docking rules, collapsible cards, all large panels are optional windows |
| The divine contracts change names | the UI binds to kinds through thin adapters (`DivineViewAdapter`), one file to update |
| Old stage 3 UI conflicts with the god-only game | a single `PlayerModel` switch (`GodOnly` or `FactionControl`) chosen by the scenario; stage 3 stays testable |

## 25. Owner D6 (27 Nov 2026): industrial interface

Owner decision D6.19 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). The world look is [VISUAL_WORLD_DESIGN.md §18](VISUAL_WORLD_DESIGN.md#18-owner-d6-27-nov-2026-industrial-presentation); the sim is owned by the D6 documents ([INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md), [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md), [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md), [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md), [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md)). Decisions UI-1 to UI-9 hold: world first, panels second; reads only through `IDivineView` and events; writes only divine commands through `ICommandGate`; every datum carries its provenance tag (§12). **The god never places a production order, sets a routine, a dispatch rank, a tariff or a budget** (D5.1): industrial panels are inspection and analysis; the only actions offered are the existing divine ones aimed at the people involved (instruct, compel, possess, miracles).

**Inspection cards** (selected-object card, §9; each shows divine observation and, beside it, the owning organisation's own record where one exists, with the difference bar of §20):

| Card | Shows | Divine actions offered (existing ones) |
|---|---|---|
| **Deposit** | ware, depth, remaining (as observed: exact under `Omniscient`, a band otherwise), claimant and use rights, who knows of it (count of persons, records) | reveal (Tier 3 miracle), smite source |
| **Facility** (mine, well, smelter, refinery, factory, recycling plant) | owner organisation and ownership model, design, status and stall reason, **production queue** (orders with recipe, quantity, done, buyer, military or civilian tag, progress bar of the running cycle), inputs and outputs by ware and owner, workers present and their employment, power share this interval, condition, emissions | instruct or compel the manager or a worker; possess a worker; smite |
| **Vehicle** (vehicle inspection) | owner, operator and crew (names, roles, whether following the routine), routine and current step, cargo by ware and owner, tank level and range, condition and last maintenance, odometer, trips today; military: ammunition and readiness | instruct, compel or **possess the operator** (the possession pill of §18 then shows the vehicle's tank and cargo beside the body's needs) |
| **Power station** | owner organisation, design and technology, on or off, output and availability, staff on duty vs minimum, **fuel bin by ware and days of fuel left**, ash bin, dispatch rank, customers and meters, bills open and paid (as observed vs the utility's records) | instruct or compel an operator; possess; smite (a nuclear station warns of contamination) |
| **Industrial organisation** (the C6 card, §20, extended) | facilities and fleets owned, agreements (supply, procurement, electricity, haulage) with deliveries and open obligations, workers and wages, actual payments received and made (never "revenue" from deliveries), insolvency state | the existing organisation-scope instructions (relayed to office-holders, ORGANISATIONS §4.12) |
| **Waste site, regulator** | stock by ware, capacity, leak class; inspection records | as for facilities |

**Overlays** (toolbar Analysis tray or hotkeys; off by default; none reveals the hex grid):

| Overlay | Content |
|---|---|
| **Electricity network** | links coloured by component; energised vs dark; stations with output and fuel bars; consumer rings (full, partial, out); outage and shortage spells highlighted |
| **Industrial logistics** | routine paths of vehicles as flow ribbons (thickness by units moved today, colour by ware); open supply agreements as dashed links between organisations' facilities; stockpile bars at bins |
| **Resources** | deposits as observed, with remaining bands and claimants |
| **Pollution** | the pollution band per region as a soft heat map (contamination separately) |

**Shortage indicators and alerts** (§14 aggregation applies): world glyphs per VISUAL_WORLD §18; alert digest rows for outages beginning and ending, a factory stalled more than a day, a station with under a day of fuel, a deposit depleted, a vehicle broken down far from a depot, a strategic device built or used (always an individual card). At most one digest card per category per day.

**Analysis (Mode D, §20):** an Industry view (output by recipe, capacity utilisation, stall reasons, deposit depletion), an Energy view (generation mix, unserved energy, outage hours, fuel per kEU), a Logistics view (units moved, fuel per unit, routine adherence), a Military production view (military share of factories, fuel and power) and an Environment view (waste flows, pollution bands, illness by band): the I6 KPIs, each with its definition popover.

**Tier and package mapping:** package **I6** (with U2/U5 conventions): facility card with production queue, vehicle inspection, power station card, electricity overlay, shortage indicators and alerts: Tier 2 (D6.18 slice); logistics and pollution overlays, organisation agreements view, Analysis views: Tier 3.

**PlayMode-testable hooks:** **H-UI-13** `Hud.SelectAsset(vehicle)` opens a vehicle card whose cargo, tank and operator equal the observed state, each with a provenance tag; **H-UI-14** a facility card's queue rows equal the facility's orders (count, order, progress ±1%); **H-UI-15** no industrial panel offers a command that is not a divine command (reflection test over the new HUD types; extends H-UI-4); **H-UI-16** toggling every industrial overlay leaves the state hash unchanged; **H-UI-17** cutting a link in a fixture raises exactly one outage alert for the component and clears it on restoration.
