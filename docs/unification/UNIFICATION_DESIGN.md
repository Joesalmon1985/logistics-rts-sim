# Unification design: one artificial world (owner D8, 28 Nov 2026)

## §D9 Controlling architecture (28 Nov 2026)

**Precedence.** This section is the controlling technical architecture for the 28 November run. It implements [LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md) and owner decisions D9.1–D9.14 ([OWNER_DECISION_REGISTER](../civdiv/OWNER_DECISION_REGISTER.md)); only those two outrank it. It supersedes every conflicting part of §0.2 and §§1–15 below (list in §D9.7). Binding numbers and ownership are in [SHARED_ARCHITECTURAL_CONTRACTS §23](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md); packages, prerequisites and the schedule are in [UNIFICATION_PACKAGE_INDEX.md](UNIFICATION_PACKAGE_INDEX.md) §1 onwards. Every choice marked **(L)** is a lead default: configurable, recorded as such, never an owner quotation. Head at writing: `unity/poc` `fb6ca85` (save version 6, `MaxCand` 36, `PersonalCount` 40, five drives, no `Unification` code).

**In one paragraph.** Unified mode keeps the body (biology, needs, genome, kinship), perception, gestures, the goods physics, divinity, the warehouse and determinism, and replaces every legacy decision and economic path with four new things: one **primitive action executor** for the twelve controls, one **data-defined interaction system** loaded from the workbook manifest, **physical goods with no title**, and a **trained founder brain** that chooses controls from local observation. Construction, collection, exchange and conflict are not subsystems with their own verbs; they are interaction effects reached by `USE`, `TAKE`, `DROP` and `GESTURE`.

### §D9.1 The unified-mode architecture

#### (a) Kept layers (reused as they are, behind the switch where noted)

| Layer | Code | Use in unified mode |
|---|---|---|
| Bodies and biology | `Population/Bodies.cs` (`Damage`, `Heal`, `IsPerson`), `Npc` health, injury, ageing, death (`PopulationModule.Kill`, carry spill `DeathSpill`) | unchanged; strike damage enters through `Bodies.Damage(..., DamageCause.Violence, actor, 0)` |
| Needs | five drives in `Learning/Features.cs` (`Drive.Hunger…Pain`, `Count = 5`, hashed) | kept exactly; seven further channels live in a unified extension (§D9.4 d, contracts §23.6), so the five-drive hash is untouched |
| Genome, temperament, kinship | `Genetics/Genome.cs`, `Npc.Traits`, M4 genealogy, reproductive unions, `LifeCycle` births and ageing | kept as biology; founder families are generated (UN5); conception continues for existing unions (`ConceptionMode 1`); new pair formation is UB (§D9.5) |
| Perception | `Thinker` sight/visibility helpers, `SightDay/SightNight`, `Comms/CommsRange.cs` | reused by the new observation encoder through a spatial index; the legacy candidate builder is not run |
| Gestures and messages | `Comms/Messages.cs` (`Emit`, `Perceive`, noise, `DealId`), `GestureAct` (`Offer 2`, `Promise 8`, `Surrender 11`, `Accuse 13`, `Threaten 26`, …) | GESTURE control emits through `Messages.Emit`; referents and a correlation id go in a unified side record (contracts §23.9) |
| Goods physics | `Population/Goods.cs` (`Move`, `Produce`, `Consume`, `Fits`, `Insert`, `InReach`, `Reach.*`, `MoveResult`), `LotBin`, carry slots and weight | kept as the only way goods exist or move; the title side effects are switched off (§D9.1 d) |
| Divinity | miracles, Mythic, possession and avatar (`Divinity/Control.cs` F1 seam), commandments | kept; possession and avatars submit primitives; suggestions and commandments reach the brain as observation inputs; compulsion drives the body through a labelled goal driver (UN9) |
| Learning core | `Learning/AssociationStore.cs`, `Learner` (TD credit, traces, `Outcome`, `Accrue`), `Imitation.cs` origins | reused as the per-NPC run-time associative overlay (§D9.2 e); the legacy verb table is not used for decisions |
| Warehouse, replay, save, hash | `DotNet/God.Runner/Warehouse/*`, `ReplayWriter`, `SaveFile`, `StateHasher`, `GodSim.StateHash` parts | unified facts and a new `unification` hash part; legacy tables stay legacy-only (QA-02) |
| Exact game-theory lab | `Lab/Experiments/L01HawkDove.cs` and siblings | separate; supplies measurement vocabulary only |

#### (b) Replaced in unified mode by new code

1. **One primitive action executor** (`Sim/Unified/Motor/`, package UN2). The twelve workbook controls (turn left/right; step forward/back/left/right; TAKE; DROP; USE; GESTURE; SLEEP; WAIT) are the only way any unified body acts. One entry point, `Motor.Submit(s, ev, in PrimitiveControl, ControlOrigin)`, serves the trained policy, possession and avatars (through a new `ControlInputKind.Primitive` on the F1 seam), divine compulsion (through a labelled goal driver), test fixtures and the training teacher. Origin is recorded on events and never changes a physical result (QA-01). Movement is one lattice edge per step (§D9.4 a); there is no `MoveTo` for NPCs and no path solver behind a step.
2. **A data-defined interaction system** (`Sim/Unified/Interactions/`, schema UN0, resolver UN2, effects UN3b/UN4/UN7/UN8a). `USE(source or empty hand, target, slot)` is resolved by matching catalogue rows: each object and building row declares interactions `{target kind, slot, source requirement, reach, duration, tool multiplier, effects}`. Effects are a small fixed set of **physics effect kinds** written in code (produce, consume, need relief, deposit, install, occupy, recipe input/work, place site, contribute, labour, collect raw, forage, drink/wash, hold out, strike, equip, treat). *Which* object offers *which* effect is data, so a new recipe or furnishing never needs a new control or verb. Rows come from the reviewed export of the workbook manifest (UN3a); no Excel at run time.
3. **Physical goods with no title** (UN0 inside `Goods`, UN9 at the callers). The world records holder and location (carry, equipped, ground, building storage, construction site, installed fixture, workstation, container), quantity, condition, uses, provenance and every movement; never a rightful owner, permission or theft. Detail in (d).
4. **Physical construction** (UN4): a known design plus `USE` on a valid site places an unfinished **construction site**; `USE` carried material on the site delivers it; `USE` empty hand or a tool on it adds labour; anyone may contribute; completion needs the required materials and labour; founders and contributors are provenance, not title (§D9.4 c).
5. **Minimal exchange** (UN7): `GESTURE` `Offer`/`Promise`/`Want`/`Accept`/`Refuse` with referents (ware or lot, quantity, person, place) and an optional correlation id; any transfer is an independent hand-over (`USE` lot on person, slot *hold out*, then the receiver's `TAKE`). No atomic trade, lock, automatic second leg, debt or title.
6. **Conflict slice** (UN8a): `USE` empty hand or a strike-capable held object on a reachable person, slot *strike*, resolved with the existing `Military/Duel.cs` maths on NPC stats, damage through `Bodies.Damage`; pain is the existing drive; fear rises in witnesses who actually perceive it; retreat and pursuit are steps; protective equipment is equipped by `USE` on self; remedies by `USE` herbs on the injured. Identity-neutral: same rules within and across tribes; no faction military, no forced retaliation, no theft offence.
7. **Raw extraction gated physically** (UN1 nodes, UN3b gate): dice give a hex an allowance that **materialises gradually** as raw stock on that hex's raw nodes; `USE` (empty hand or tool) on a raw node succeeds only when a **completed collection building of the matching kind** lies within its collection radius and its storage can accept the unit (counting open reservations). The unit goes into the collector's carry and a bounded-expiry capacity reservation is opened at that building; depositing is a separate `USE` on the building (slot *deposit*). On expiry, full store or destruction the unit stays where it physically is. Picking up an already collected unit is ordinary `TAKE`, not extraction. Tools multiply speed only.
8. **Forage and water** (UN1): seven forages are infinite and time-limited at forage places of their terrain, independent of dice (`USE` empty hand → one food lot into the hand after the forage time; `USE` food on self eats). Water is an environmental affordance (§D9.4 d, G3).

#### (c) Removed from unified play (legacy isolation, §D9.3)

Faction AI and every Stage 3 system (dice piles, HQ stock, carriers, builders, harvesters, flags, segment logistics, faction trade, MilUnit military, VP, HQ elimination), the M3 candidate/instinct/verb controller (`Thinker.BuildCandidates`, `InstinctTable.Core`, `AssociativeNpcPolicy` decisions, hut building, `Gather`, `ForageAt`), M4 estates, executors, store permissions and household economic verbs, M5 title-based deal settlement and own-goods filters, C1 assets/work/ownership/theft classification, C3 employment, C6 organisations and rations, C7 services (treatment is now `USE` herbs), and every `Permits.*` candidate filter. They stay compiled and pinned for the legacy scenarios.

#### (d) Physical goods without title (binding detail in contracts §23.4)

- `Goods.OwnerAt` returns `Owner.None` in unified mode whatever the `OwnerRule`; `Produce` ignores its owner argument; `SetOwner` refuses (returns false, no event); `OwnershipChanged` is never emitted; `HandOver.OwnerChanged` is always false; `PayOwnerChange`, `PayProduce` and `PaySpoiled` pay nothing. **No learner reward ever comes from a holder or title change**; reward comes only from experienced needs, pain and health (§D9.2 d).
- **Merge policy (L):** in unified mode two lots merge when ware, spoil day and uses are equal and the ware is not unique; provenance does **not** block merging. Each lot carries one compact **origin** record in a unified side table (producer, produce reason, tick, source kind and id; `Mixed` after a merge of different origins, keeping the earliest tick). The full history is the objective event stream (`ItemMoved`, produce, consume, contribute, hand-over), so analytics can reconstruct any chain without lot-count explosion. Durable objects (tools, furnishings, fixtures, sheep) are unique (count 1) and never merge.
- Containment is physical: carry, equipped, ground, and unified bins of kinds *storage*, *site*, *fixtures*, *workstation input*, *workstation output*, *container* (contracts §23.4). Capacity, reach, occupancy and physical barriers constrain; a belief, claim, group, household or legacy owner field never does (EP-01).
- Death leaves every unit where it is (carry spills to the ground through the existing `DeathSpill`); no estate, executor, heir or escheat. The deceased-owner invariant C4 is replaced, in unified mode only, by location and conservation invariants U-C1…U-C6 (contracts §23.11).

### §D9.2 The brain

#### (a) What the trained founder policy is

A deterministic, small, recurrent integer network, one **shared frozen policy artifact** for the species, evaluated per person at each decision point, plus per-person state:

- **Observation** (schema `Obs v1`, frozen by UN0, filled by UN2's encoder; integers, fixed-point ×1000, fixed order, length fixed by the schema): proprioception (facing one-hot ×6, current action and last result code, asleep, occupying, equipped); body (12 need pressures, health, injury, age band, temperament traits); carried items (up to 4 slots: catalogue class, count, condition); local perception (the 6 neighbour cells: passable, terrain, water, forage; then the **16 nearest perceived entities** within sight, sorted by distance then id: class, relative sector, distance, state bits such as complete/site/has-stock/occupied/holding-out/injured/asleep, and for people kin, familiarity, trust band, recent aggression seen); heard and seen gestures (last 4: speaker slot, act, referent class, quantity); memory (8 remembered places with dead-reckoned sector and distance, 4 remembered people, active claims summary); knowledge (known designs, known recipes and interactions as bits aligned to the action arguments); divine inputs (suggestion or commandment features; zero when divinity is off). No true-state field, no hidden path, no owner field.
- **Action** (schema `Act v1`): a factorised choice: control (1 of 12) → arguments chosen from the observation's slots: target (perceived entity slot, self, facing cell), source (carried slot or empty hand), interaction slot (0–3), design (known design index), gesture token and referents, quantity. Masks are **physical legality only** (exists, perceived, in reach, room, passable, not busy, the design is one the person knows): never a social, moral or "advisable" mask.
- **Network (L):** input encoder → 2 dense layers (128) → integer GRU-style recurrent state (64) → factorised heads; int16 weights, int32 accumulators, saturating arithmetic, piecewise-linear activations, integer log-softmax table. Action sampling uses the person's keyed policy `Rng` stream (temperature from temperament), so replay is exact. **Budget (scale rule):** ≤ 20,000 multiply-accumulates and ≤ 4 KB of state per person per decision, no allocation per decision.
- **Decision points:** a person decides when its current primitive completes, fails or is interrupted (not on a fixed think cadence); asleep or busy persons do not decide. Staggering and level-of-detail depend on sim state only, never on the camera.

#### (b) Training offline against the authoritative simulator (package UN6t)

- **Same world:** the trainer runs the real `GodSim` with `Unification.Enabled = 1`, the same catalogue, needs configuration, executor, encoder and C# inference code; no separate physics, no Python at game run time. Training scenarios are small generated islands (1–12 people) and the full 72-person start for late stages.
- **Tooling (L):** a dependency-free C# trainer, `DotNet/God.Train` (net8, multi-threaded across seeds): behaviour cloning from labelled teacher demonstrations, then evolution-strategies fine-tuning on episode returns, with float master weights quantised to the exact int16 artifact for every evaluation rollout (so training evaluates what the game runs). Any framework is allowed by the owner; Python/PyTorch may be used for analysis or as an alternative optimiser **only** if it consumes rollouts from the C# sim and exports the same integer artifact with a bit-exact parity check.
- **Curriculum** (amendment §6, in order; a stage advances on predeclared stage criteria): S1 motor control and exploration; S2 food, water, sleep; S3 hygiene, comfort, social, fun, bladder; S4 take/store/withdraw and carrying; S5 selecting and completing useful construction (L1 shelters, hearth, well, carpenter); S6 raw collection and deposit, simple recipes (cooked fish, planks, bed, stool); S7 offer/promise/hand-over and non-reciprocation; S8 aggression, avoidance, retaliation, contested goods, childcare and observation. S7–S8 train only once UN7/UN8a effects have merged; artifact v1 may ship S1–S6 with S7–S8 marked not trained.
- **Feedback:** returns are the experienced change of need pressures, pain, health and death, discounted over long horizons (ES optimises whole-episode return, so credit for a 300-tick construction is not lost). No reward for professions, buildings completed, goods held, title, civilisational milestones, morality or winning a dispute.
- **Teacher:** a scripted, labelled teacher (`ControlOrigin.Teacher`) may produce demonstrations and may use a path oracle; it is compiled only into `God.Train` and fixtures, never into a game scenario, and never drives an evaluation population.
- **Artifact:** `Sim/Unified/Brain/Artifacts/FounderPolicyV<n>.g.cs` (generated: base64 weights, header with schema versions, `Obs`/`Act` schema hash, catalogue content hash, tuning hash, curriculum id, training seed range, trainer commit, training metrics and the SHA-256 of the weights). The sim verifies the hash at load; `Unification.Brain.Artifact` names the version and is saved with the game.
- **Seed split:** training and validation seeds `< 900000`; **held-out evaluation seeds 900001–900020** and held-out layouts (mirrored, rotated and regenerated boards, unfamiliar start positions) that the trainer refuses to load.

#### (c) Predeclared thresholds and honest reporting (TR-01)

UN6t commits these thresholds (file and hash in its first commit, before any held-out run; the artifact header records the hash). All runs teacher-free, divinity off, no hidden path oracle, same need mask as training. Comparisons: **untrained** (same network, random initial weights), **frozen** (behaviour-cloned only, no fine-tuning) and **trained**.

| Id | Measure (held-out seeds 900001–900020 unless stated) | Threshold (L) |
|---|---|---|
| T1 survival | 12-person tribe, 10 sim days: share of adults alive | trained ≥ 0.90 and ≥ untrained + 0.40 |
| T2 need control | share of person-ticks in the pain band of hunger or thirst | trained ≤ 0.10 |
| T3 construction | seeds with ≥ 1 level-1 collection shelter completed by day 5 (no fixed order, own choice of design and site) | ≥ 14/20 |
| T4 collection | seeds with ≥ 1 raw unit collected and deposited by day 8 | ≥ 10/20 |
| T5 interruption | interrupted construction tasks resumed and completed (scripted interruption fixture, teacher-free afterwards) | ≥ 0.50 |
| T6 full start | 72-person unified start, held-out seeds 900001–900003, 20 days: adults alive | ≥ 0.75 on each seed |
| T7 interaction | seeds with ≥ 1 hand-over between persons by day 10 (reported for EP-03 learned; not a TR-01 blocker if S7 not trained) | ≥ 10/20 |

Basic survival (T1, T2, T6) and basic construction/collection (T3, T4) are the TR-01 gate. Social outcomes (strategy mix, cooperation rate, Hawk/Dove proportions, population growth) are **measured, never thresholds**. **A failed TR-01 means UA-learned is not green**: the result, metrics and artifact are kept; if a fallback is attached it is labelled `Brain.Kind = Fallback` in the save, the HUD and every report; a scripted controller or the old `Eat/Flee/Build` rules behind primitives is never called trained.

#### (d) Run-time learning and cultural transmission

- **What is inherited (L):** the shared policy artifact is treated as the species' innate cognitive architecture: every person, founder or newborn, runs it. Temperament and biology come from the genome. **Acquired** and never copied: knowledge (designs, recipes, interactions known), skills (practice levels that shorten durations), episodic and place memory, beliefs about people and claims, and the per-person associative overlay. A child starts with only the innate interactions (eat, drink, sleep, approach, gesture, take, drop) and no designs or recipes.
- **Associative overlay:** each person has a bounded `AssociationStore` (reuse) keyed on hashed observation features × action class, adding bounded logit biases to the frozen policy. Credit is the existing `Learner` TD/trace machinery driven by need-pressure change (`DriveReward`) and delayed outcomes; no reward from goods or title.
- **Knowledge acquisition:** by own success (a tried interaction that succeeds becomes known); by **observation** (a person who perceives another complete an interaction, recipe or building may learn it, probability from attention, distance and temperament); by **teaching** (`GESTURE` `Teach`/`Demonstrate` while performing it raises the observer's chance and adds an imitation bias with `Imitation` origin `Vicarious`/`Imitated`). Founders start with varied knowledge sets and skill levels drawn from the start configuration (§D9.5); different adults therefore act differently on the same network.
- **TR-02:** matched pairs of children (same genome class and age), exposed vs not exposed to a demonstrator; measure acquisition, use, retention and transfer to a new target; report failures. The open question in §D9.7 concerns this "innate network" reading.

#### (e) Bounded internal planning: may and may not

**May:** search, inside the brain, over the person's own known interactions and its own remembered or perceived world (not truth), depth ≤ 4, ≤ 64 expansions per decision, to produce an **intention input** (a target slot and a need channel) that the network reads as part of its observation; keep a remembered route made of places the person has itself visited (dead reckoning). **May not:** issue controls, call `NodePather` or any truth query, read another mind, hold hand-written economic goals (no "build shelter" table, no recipe chain script, no profession), or bypass any physical check. `Unification.Brain.Planning` is 0 by default (L); it is switched on only if the training report shows it is needed, and then both training and evaluation use it.

### §D9.3 The unified switch and legacy isolation

- **Switch:** `SimTuning.Unification` (`IOmitWhenDefault`, saved with the game, honoured by both hosts). `Enabled` (0). When 1: `GodState.Unification` exists, the `unification` hash part is added after `divine`, goods lose title (§D9.1 d) and the legacy decision and economy paths below are off. These two consequences have **no sub-switch**: there is never a unified game with title or a second economy.
- **Behaviour sub-switches** (default 1 when `Enabled = 1`, so packages can be tested apart): `World` (terrains, forage, water, materialisation), `Interactions`, `Construction`, `Exchange`, `Conflict`, `Needs.ActiveMask` (12-bit), `Brain.Kind` (0 Untrained, 1 Trained, 2 Fallback; `Teacher` exists only in `God.Train`), `Brain.Artifact`, `Brain.Planning` (0); UB: `Settlements`, `Roads`, `Groups` (0 until UB).
- **Legacy isolation rule:** with `Enabled = 1`, `GodSim.InstallModules` does not install `DiceSystem`, `EconomySystem`, `MilitarySystem`, `TradeSystem`, `FactionSystem` VP/elimination, `EstateSystem`, `CivSystem`, `OrganisationSystem`; the hosts never attach `AiDirector`/`AiBrain`; `PopulationSystem` runs `Body()` and then hands the person to `UnifiedHooks.PersonTick` instead of the legacy think/act; `Ownership.Classify`, lost property, `Property.Claim`, `TheftCommitted`, use-right and estate events are not emitted; every `Owner`/`OwnerRelation`/`Permits.*`/`StoreAccess` read is absent from the unified path (static guard test). Kept on: `DivinitySystem`, `MiraclesSystem`, `LifeCycleSystem` (biology only), `SocialSystem` memory/trust updates fed by unified perception, `GodDirector`.
- **Legacy pins unchanged:** with `Enabled = 0` every part hash of every existing scenario equals the base head (U-P1), including the Stage 3 smoke hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` and P1, P4, P8 (contracts §23.2). The Stage 3 game is kept as an isolated regression scenario.

### §D9.4 Mechanics in brief (numbers are tuning defaults, all (L))

- **(a) Movement and facing:** the movement lattice is the board's fine flag graph (`BoardTopology`); facing is one of 6 world sectors (60°); `Step forward/back/left/right` resolves to the lattice neighbour nearest the facing sector (forward), its opposite (back) or ±90° (left/right), ties by lower node id; no neighbour within ±30° of the wanted sector = `Blocked`. Step duration `StepTicks` 6 (road 4), turn 1 tick, `WAIT` 10 ticks. Facing, action progress and reservations are saved and hashed.
- **(b) Collection buildings:** the six L1 shelters cost labour only (600 ticks), store 10 / 30 / 60 units at L1/L2/L3, give 2 / 4 / 6 basic sleeping spots, collection radius 2 hex rings of nodes; L2 10 wood + 300 ticks, L3 20 wood + 10 stone + 600 ticks; upgrades run as an upgrade site while the building keeps operating at its current level. Reservation expiry `ReserveTicks` 600.
- **(c) Construction sites:** placement on the facing node or an adjacent node: land, no building or site on the footprint or its neighbours, terrain rule from the row (fisher's shelter on a coast node; shepherd's on pasture; and so on), production buildings and L2+ upgrades inside a settlement's development area (UB rule; in UA the six founding settlements' areas exist from the start). Materials are held in the site bin and consumed (`ConsumeReason.Built`) only at completion; labour may run ahead of materials but completion needs both. Many contributors at once; no work for `SiteStaleDays` (0 = never) leaves the site as it is. Destruction or decay drops its contents to the ground.
- **(d) Needs:** 12 channels: Hunger, Energy, Warmth, Safety, Freedom from pain (the five drives, shown as satisfaction), plus Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room. Only Hunger, Thirst, Warmth (exposure) and injury affect health; the others affect comfort, mood and reward only. Every active channel has a UA relief path (manifest §3.3). Default `ActiveMask` = all 12; training and evaluation always use the target scenario's mask.

### §D9.5 Resolved defaults for the manifest corrections and gaps (all (L), configurable)

| Id | Resolution |
|---|---|
| N1 | `USE` requires reach, inputs, capacity, barriers, occupancy and physical validity; "rights" is deleted. |
| A | Any able person may use any physically accessible place, bed, store or container; occupancy is first come first served; founders and contributors are provenance; claims are beliefs; the hall is a shared place. |
| B | No roles, recruitment or staff as engine facts; service is labour plus separate hand-overs. |
| C | "Offer" is a gesture; hand-over is hold-out plus `TAKE`; tally and message tokens are portable symbols; a stall is a surface; a market a place. |
| D | No consent or knowledge flag in an interaction; the treated person may move away; shared play needs a second person's actions. |
| E | Explicit slots: water has *drink* and *wash*; buildings have *deposit*, *install*, *work*, *fuel*, *cook*; tools are speed multipliers (stone axe ×0.6 wood collection, stone hammer ×0.7 construction labour, stone pick ×0.7 stone collection) and strike-capable (attack 2; empty hand 1); habitat, route and woodland huts have no engine effect until a physical effect is defined (Later); raw fish and raw meat give no nutrition. |
| F | Alternative recipe lines are separate recipes; TBD costs stay inactive; L2/L3 values as §D9.4 b; a building produces only what a recipe row states. |
| G | Names normalised by alias to object ids (bedding mat → Sleeping mat, target ball → Hoop, jump platform → Low jump platform, cool storage → Cool storage chest, fire watch → Fire warning station, painting board → easel, sundial → Sun marker, water basin → Water feature); L1 shelters have basic sleeping spots only; listed furnishings are craft options. |
| H | Weapons and armour are held or worn objects for the conflict slice; defensive structures are obstacles or viewpoints; all Later except UA strike by empty hand or stone tool. |
| I | No assigned operators; operating is a person choosing to act. |
| G1 sea ring | The board is the land island plus a sea ring (54 hexes default; radius configurable, §D9.6 scale). Sea hexes are impassable; **coast nodes** (land nodes on a sea hex) carry seaweed forage, salt water (*wash* only) and the fish raw nodes; the fisher's shelter must stand on a coast node. No boats, rafts or swimming before Later. |
| G2 fish | Raw fish nutrition 0. Primitive hearth (1 stone + 120 ticks) cooks: `USE` fish on hearth (input), `USE` wood on hearth (fuel/input), `USE` empty hand slot *cook* (60 ticks) → cooked fish in the hearth output. A lit hearth (1 wood per 1,200 ticks) warms people within 2 nodes. |
| G3 water | **Natural drinking water:** every wetland node, plus one spring node per forest, mountain, pasture and fields hex (placed by the generator), is a fresh-water point; `USE` empty hand slot *drink* or *wash* there (20 ticks), no container, no lot created. Desert has none; the sea is salt. The **Well** (4 wood + 300 ticks) is an improvement that creates a fresh-water point where it stands. Carrying water needs a container (Water jug, UB); `Ware.Water` is reserved for that. |
| G4 weapons | UA strike: empty hand (attack 1) or a held stone axe or stone hammer (attack 2); no UA armour content; the equip effect is generic (defence from data) and its mechanical test uses a test-catalogue row. Stone-point spear and soft armour (Carpenter/Tannery) are UB. |
| G5 herbs | Infinite, time-limited forage-like source on mountain and forest hexes; `USE` herbs on an injured person (or self) slot *treat* gives `Bodies.Heal(Injury)`; the patient may walk away. |
| G6 hall | Each tribe's Communal hall (BLD-001) is a completed founder building at its founding settlement with an **explicit founder endowment ledger** (`ProduceReason.Founder`): 12 sleeping mats installed (12 sleeping spots), 2 benches, 1 storage chest, an integral hearth (cooking and warmth capability), storage 40, plus founder food for `StartFoodDays` 2 (foraged-kind lots). Nothing else is injected. |
| G7 reed, seeds | Reed Yard, Seed plot and everything needing reed or seed are UB. |
| G8 waste | `USE` latrine slot *relieve*, or relieve at any outdoor node (lower hygiene and room for persons nearby). A latrine holds up to 20 Waste units internally (`ProduceReason.Excreted`); when full it works as an outdoor node. Waste handling beyond that is UB. |
| G9 rabbit | Rabbit is forage on pasture: `USE` empty hand takes 3× the mushroom forage time and gives an edible portion (`Rabbit.EdibleRaw` 1, an abstraction of catching and preparing); the UB Hunter's hut consumes a carried rabbit for raw meat; forage is never depleted. |
| G10 mines | Later; the reachability audit marks them as having no bootstrap path. |
| G11 settlement and road costs (UB) | **Settlement:** Catan vertex with the distance rule, 2 wood + 2 clay + 1 sheep + 2 hay + 1,200 ticks of labour, creating a development area of 3 node rings. **Road:** per Catan hex-edge segment 1 wood + 1 clay + 300 ticks; a road site may be placed only on a Catan edge path between **two existing settlements**, adjacent to one of them or to a completed road edge; each completed edge speeds walking at once. A planned route or planned settlement is a gesture or claim, never a site. (This resolves D8.11's "towards a planned settlement site" against the amendment: no roads to nowhere.) |
| G12 missing rows | Aliased as G above; Meal/Ration/Snack Later. |
| G13 tools | Speed multipliers and strike values only; no tool gates a UA interaction. |
| G14 contradictions | §D9.4 capacities win; workbook furnishing lists are craft options; D9 wins over workbook ownership/trade wording; upgrades do not disrupt operation in UA. |
| G15 needs | Defined in `Unification.Needs` (rates, age/temperament variation, relief magnitudes, health coupling as §D9.4 d). |
| Start | Six tribes × (7 adults + 5 children), deterministic varied families (couples, single parents, unpartnered adults, orphans cared for by kin), founding settlement and hall each; founder knowledge: every adult knows the six L1 shelter designs it can place on its terrain and the hearth; 4 of 7 know the carpenter and well; 3 of 7 know each UA recipe; skill levels vary ±30 %. Children know only innate interactions. No pre-formed groups, roles or stores beyond the hall endowment. Standalone hut disabled (R73). |
| Identity | Tribe is an affiliation id (existing faction ids, `TribeOrder` display table, R78); it owns nothing and issues nothing. |

### §D9.6 Long-term scale target (owner decision D10, 28 Nov: recorded target, not a UA requirement)

The owner wants to reach **six fully functioning factions, each with 3–4 settlements made of collections of different buildings and 100–200 people (about 600–1,200 in total), with back-and-forth conflict between them**, on an island enlarged as needed. This is checkpoint **UC "Scale"** after UB (index §7). Conflict at that scale must still emerge from individuals and groups (raids and defence as group behaviour, UB+), never from faction AI. Rules every UA package follows now so UC is not blocked: board radius, hex count, settlement corners per tribe and tribe count are configuration, never constants (generator tested at radius 4 and 6); perception and witnesses use a spatial index (no all-pairs scans); executor, interaction resolution and materialisation cost O(active persons + changed nodes) per tick; brain inference within the §D9.2 a budget with decisions on action completion only; per-person state bounded; no per-tick allocation in hot paths. Benchmarks at 72, 600 and 1,200 people are tracked from UA on (baseline before trained-brain inference: about 1,080 ticks/s at 500 and 384 ticks/s at 1,000 people). **UC performance target (L):** 1,200 people with trained-brain inference headless at ≥ 100 ticks/s (10× real time at 1,200 ticks per day of 2 minutes) and the player at ≥ 20 ticks/s. A performance package (UNP) runs when a slot is free and never delays the UA critical path.

### §D9.7 Owner-level open questions (each has the default used)

1. **Inheritance of the trained network.** Default: the shared founder policy is the species' innate architecture that children also run; only knowledge, skills, memories, beliefs and overlays are acquired. Alternative: a separate child-innate network trained only on stages S1–S3, so construction and exchange competence must be acquired. The default is used unless the owner says otherwise.
2. Standalone family hut (R73) stays disabled until the owner decides.

### §D9.8 Superseded by this section

- §0.1 "the relevant community group owns the communal building" and "subject to access and ownership": superseded (no title; §D9.1 d).
- §0.2.1 workbook pipeline: kept, but the runtime source is the reviewed catalogue export of UN3a; `PersonalCount` 96 and ware numbers of R75 superseded by contracts §23.4.
- §0.2.2: kept in substance; numbering and API are §D9.1 b and contracts §23.5.
- §0.2.3: superseded by §D9.2 (thresholds, artifact, inheritance).
- §0.2.4 "furnishings … owned": superseded; hall capacity is §D9.5 G6.
- §1.1 and §1.2: superseded by §D9.3 for mechanisms; the tribe definition stands.
- §3 forage tables: superseded where they create food without a `USE`; §3.2 rows remain tuning examples.
- §4: kept (allowance, gradual materialisation); collection is §D9.1 b 7.
- §5.1 rights capability, §5.3 builder, §5.4 collection rights, §5.6 the five rights, §5.7 communal ownership, §5.9 sale and inheritance: **superseded** (§D9.1 d, §D9.4 c).
- §6.2 and §7.2: superseded by §D9.5 G11 and UB groups; §7.1 road-as-speed stands.
- §8: rows are examples; recipes are catalogue rows (UN3a) run at workstations by `USE`.
- §9.5 communal building owner and 8 beds: superseded (§D9.5 G6).
- §10 individual planning (`NpcMind.Plan`, goal rules, worked examples): **superseded** by §D9.2 e; no goal tables, no `AdoptGoal` verb.
- §11: kept as measurement; mechanisms are §D9.1 b 5 and §D9.2.
- §12 groups: deferred to UB package UN8b on the D9 rules (no forced organisation, no power over goods).
- §13.2 switch list, §13.4 wares and reasons, §13.5 verbs 128–139, `NpcVerb` 220–229, `MaxCand` 40/44, `SelfFeature`/`SituationKind`/episode keys, §13.6 event codes: superseded by contracts §23 (no new learning verbs; R81).
- §14 acceptance U1–U14/B1–B11: kept as rows but read through EC/EP/CF/TR/QA and the corrected interpretation in §0.2.5; U8 is brain-generated sequences, not a goal object.

---

**Owner of:** the unified geography (seven hex types), forage, raw-material materialisation, the building architecture and the six collection buildings, settlements and development areas, roads, production recipes as data, the tribal start, individual goal planning, specialisation and exchange as emergence, informal groups, the reconciliation with Stage 3 and the acceptance scenarios U1–U14. **Links, does not repeat:** the economy's goods, ownership and barter ([ECONOMY_DESIGN](../stage4/ECONOMY_DESIGN.md), [SHARED_ARCHITECTURAL_CONTRACTS](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) §7, §22.3), the cognition loop ([COMMUNICATION_AND_COGNITION_DESIGN](../stage4/COMMUNICATION_AND_COGNITION_DESIGN.md), [NPC_LEARNING_DESIGN](../stage4/NPC_LEARNING_DESIGN.md)), population and households ([POPULATION_DESIGN](../stage4/POPULATION_DESIGN.md), [HOUSEHOLDS_AND_KINSHIP_DESIGN](../stage4/HOUSEHOLDS_AND_KINSHIP_DESIGN.md)), organisations ([ORGANISATIONS_AND_INSTITUTIONS_DESIGN](../stage4/ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md)), industry ([INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN](../stage4/INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md)).

**Sources.** The owner brief and historical assistant recommendations: [OWNER_BRIEF_2026-11-28_UNIFICATION.md](OWNER_BRIEF_2026-11-28_UNIFICATION.md) (Part B supersedes Part A: raw materials cannot be collected without an operational collection building; level 1 buildings cost only labour and time). Binding decisions D8.1–D8.18 in [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md). House style: [DESIGN_CONVENTIONS §12](../stage4/DESIGN_CONVENTIONS.md#12-how-every-design-doc-is-written). Code audit (written by another agent, align names with it when it lands): `UNIFICATION_BASELINE.md` in this folder.

**Status.** Design only. Written against `unity/poc` (`wt-lrs`). Code names below are real paths unless marked **new**. Every number is a configurable default in a new tuning block (section 13.3), never a literal.

**The principle in one line (owner).** Foraging sustains life. Labour creates initial capital. Capital enables resource collection. Resources enable further investment. Trade enables specialisation. Cooperation enables civilisation. Never present an outcome as designed in: this document names the mechanics that make each outcome *possible* and the measurements that show whether it *happened*.

---

**LATEST CONTROLLING AMENDMENT:** [LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md) supersedes all conflicting provisions below, including §0.2. In particular §§5.3–5.9, 9.5, 10–12 and 13–14 retain old implementation detail for traceability, not a specification of title, permissions, automatic exchange or high-level policy verbs. The lead must reconcile these before coding.

## 0. Decision index

| Id | Where it is worked out |
|---|---|
| D8.1 One simulation | §1 |
| D8.2 Seven hex types | §2 |
| D8.3 Forage | §3 |
| D8.4 Raw materials from rolls | §4 |
| D8.5 Collection needs an operational building | §5.3, §5.4 |
| D8.6 Six collection buildings, level 1 free | §5.2 |
| D8.7 Collection buildings are shelters | §5.5 |
| D8.8 One building architecture; ownership, rights | §5.1, §5.6, §5.7 |
| D8.9 Starting conditions; tribe is affiliation | §1.2, §9 |
| D8.10 Settlements and development areas | §6 |
| D8.11 Roads | §7 |
| D8.12 Recipes | §8 |
| D8.13 Planning | §10 |
| D8.14 Specialisation | §11 |
| D8.15 Informal groups | §12 |
| D8.16 Reconciliation | §13 |
| D8.17 Defaults | §0.1 |
| D8.18 Milestone and demonstration | §14.3 |

### 0.1 Defaults confirmed by D8.17 (restated so the sections can rely on them)

NPCs may walk anywhere accessible without roads. Outside a settlement's development area only basic or temporary buildings may be founded (§6.3). A settlement owns nothing; a tribe owns nothing; the relevant community group owns the communal building. Work across tribes is allowed when relationships and circumstances permit. A successful roll makes physical resources available, not owned. The same raw material may be collected by different tribes subject to access and ownership. Starting knowledge is basic survival and construction with individual variation. Only defined recipes exist at first (no invention). **Exception (Part B):** no raw material is collected by hand; a building is required.

---

### 0.2 Controlling amendment — catalogue-driven world and primitive cognition

**Precedence:** this section supersedes any conflicting implementation detail in §§1–15, especially the prescriptive verb expansion and goal rules in §10, the fixed bed counts in §5, the content/ID limits in §13, and the old acceptance criteria in §14. The existing sections remain useful for physical mechanics and regression context. `God_of_the_Island_Core_Design.xlsx` is the three-tab authoring source. Its 138 object rows and 65 building rows are the target catalogue; not every row is a UA requirement. See the amended package index for scope, owners and tests.

#### 0.2.1 Canonical content pipeline (UN0, UN7)

Keep the workbook under `docs/unification/` with these documents. UN0 owns schema, immutable stable IDs, aliases and neutral migration; UN7 owns content normalisation and validation. Export reviewed rows into deterministic, version-controlled JSON or generated C# tables with a manifest (workbook SHA-256, sheet/row, content ID, version, status, reason, package). No Excel runtime dependency. A controlled build tool validates and regenerates output; CI rejects drift and inconsistent IDs. Do not auto-interpret ambiguous prose as executable recipes. Normalize explicit input/output lines, alternatives, coproducts, quantities, time, tool/knowledge/access constraints, placement, capacity, interaction modes, need effects, durability, and upgrades. Keep missing data marked TBD/deferred; record chosen provisional numbers in the manifest. Do not quietly treat a TBD cost as zero. Each source row maps to implemented, staged, deferred or rejected-with-reason; aliases may map multiple rows to one type (e.g. wood/log and granite/stone). Reconcile duplicate legacy wares and avoid a second economy. Keep existing IDs stable wherever possible; review the proposed 96-slot limit against actual catalogue IDs (138 rows are not necessarily 138 stackable wares). Distinguish stackable wares, individually durable objects, living animals and fixtures/vehicles. Do not overload a byte enum or silently renumber pinned legacy IDs; size storage/ledger arrays and serialization appropriately, with legacy-neutral tests. Content definition IDs are not instance IDs.

Use a single `BuildingDef`/`RecipeDef`/interaction definition registry, with separate object definitions and instance state. Recipe authoring is data-driven. Existing tables in §§5 and 8 are illustrative starter rows, not another source of truth. The lead resolves conflicts against explicit owner decisions first, then workbook rows, then recorded defaults. Include a dependency/reachability audit from founder endowment, labour-only buildings, six dice materials, seven forages and approved environmental sources. Report cycles without a bootstrap path; do not inject free metal tools or hidden goods to make a test pass. Water, reed, seeds, coal/metal deposits, wild cattle and related later chains are proposed secondary sources, not new dice-hex types; adopt only when validated, stage advanced mining/metalworking where needed, and coordinate reserved Industry IDs. Charcoal and mined coal are distinct unless explicitly aliased by a fuel class. No separate currency or automatic resource-to-money conversion.

#### 0.2.2 One body, one controller (UN0, UN6a)

Brain and possessing player submit exactly the same primitive control contract (also used by the headless trainer). Twelve controls from the workbook: turn left/right; step forward/back/left/right; TAKE; DROP; USE; GESTURE; SLEEP; WAIT. A structured control carries actor, stable item/target reference, optional quantity, explicit interaction/target slot or gesture token, and direction when needed. Six-way facing on the triangular lattice is a proposed default: define an unambiguous local side-step mapping, rotation increment, movement duration, blocked-edge behavior and animation correspondence; serialize/hash facing, action progress and reservations. Movement must resolve one local step at a time; no NPC `MoveTo` teleport or omniscient path solver behind a step. Navigation memory and learned sequences may generate local steps; if a conventional path helper is needed for a training teacher or transitional adapter, label it and exclude it from evidence of a trained autonomous brain. Perception and proprioception are bounded and local; blocked moves and failed interactions provide feedback. The world validates reach, occupancy, carry capacity, conditions, time, physical barriers, and object validity; it does not decide the actor's objective. Biology can still generate reflexes such as collapse/wake; these are not extra policy actions.

`USE(source or empty hand, target, interaction slot)` resolves data-defined effects; TAKE/DROP move real goods, not abstract balances. Slots distinguish legitimate alternatives (drink vs wash, deposit vs upgrade) without adding a new policy verb for each object or desired outcome. Empty-hand USE on an accessible forage location collects food; USE food on self consumes it; USE on a natural raw node requires a completed appropriate collection building, physical storage capacity (temporary reservation if needed); carry and deposit remain separate physical steps. A worksite or construction marker is an explicit target, chosen using the NPC's known building design and a valid site, not a hidden automatic build command. USE material on site contributes actual goods; USE empty hand/tool on site advances labour; USE on an occupied/finished building selects its valid facilities. Sleep uses a valid location/occupancy (with or without a bed). Offer and promise are communications; giving is a separate physical handover. Reciprocity is optional; no automatic ownership, consent, settlement or contract enforcement. GESTURE communicates observable tokens and referents; meaning is learned. Existing structured exchanges may be internal protocol state but must be driven by primitive interactions in unified play. Existing high-level verbs remain only legacy adapters, outcome/event classifications and optional brain-internal learned skills; they are not new motor controls. Divine commands/commandments may express goals, but execution uses the same physical interface; miracles remain distinct divine interventions.

#### 0.2.3 Founder training and lifetime learning (UN6t, UN6b)

Bring brain training forward as a first-class deliverable, not an assumption or a renamed heuristic. Build a small deterministic headless training world sharing the exact authoritative simulator and interaction registry. Define observation/action encoding, legal affordance masking (physical legality, not advice about desired behaviour), recurrent or associative state, reward from experienced changes in needs, delayed credit, and bounded memory. Reuse the current learning core where possible. A neural/PyTorch experiment is permitted if feasible, not a prerequisite to calling a learned associative policy trained. No network or trainer may mutate the world outside the primitive executor. Training may use logged demonstrations or a clearly identified scripted teacher, then evaluate with the teacher and divine intervention off. Save versioned founder policy/weights/knowledge, seeds, training config, content hash and training/evaluation metrics; prefer frozen deterministic inference for the shared trained policy plus each NPC's separate lifetime learning. Any imported model must pass C#/trainer action parity and deterministic replay; no dependency on Python at game runtime. If training fails, preserve results and deliver an explicitly labelled fallback/transition, not a claim of learned behaviour.

Train founders on movement, handling goods, survival, social gestures and simple construction/resource use; vary experience rather than make every adult identical. Use held-out maps/seeds, delayed outcomes and interruptions, compare trained vs untrained/frozen and teacher-free controls. Brain-internal learned goals, sequence memory and bounded generic planning may support longer tasks; §10's hand-authored goal candidates, fixed trigger tables, hard-coded `Flee`, `Eat`, `Gather`, `Work`, `Beg` and automatic recipes cannot directly drive the new controller. Reuse the old planner only as a named training baseline or generic search over known interactions, with actual steps and no scripted motivation. Children inherit biological architecture/traits, not parents' acquired associations or trained adult autobiographical memory. They learn from observation, demonstrations, communication and personal experience; no silent weight copying as a proxy for cultural transmission. Test exposure vs no-exposure children, retention and transfer on new targets; measure births/deaths and loss of knowledge across generations.

#### 0.2.4 Needs and physical furnishings (UN1, UN3, UN7)

Five existing drives are Hunger, Fatigue, Cold, Fear and Pain. Expose Hunger, Energy (inverse Fatigue), Warmth (inverse Cold), Safety (inverse Fear) and Freedom from pain (inverse Pain) consistently as satisfaction values in UI; internally preserve pressure semantics. Add independently evolving Social, Fun, Comfort, Hygiene, Bladder, Room/Environment and Thirst (12 total). Health, nutrition, injury, disease, age and reproductive/care motivations remain distinct. Define rates, age/trait variation, actual stimulus/relief, bounded reward scale and decay; avoid double-counting relief across correlated drives. Environment derives from local crowding, cleanliness, noise and furnishings, not an omniscient global score; hygiene/waste have physical effects, fun/social require genuine interaction. Keep thirst and bladder configurable while validating safe accessible water and sanitation; activate them in a coherent scenario only once relief paths exist. Foraging stays infinite, time-limited and independent of dice. Neither thirst nor hygiene should be tuned to guarantee survival or recreate preventable bootstrap extinction. Need tests compare identical bodies with/without appropriate interactions. Train policies against the same active need configuration as the target scenario.

A building is a space with capabilities and finite furnishing slots, not an automatic source of every object in its row. The original six collection shelters are labour-only L1; roof/floor can support basic sleeping spots with poor comfort before furniture is made. Beds, mats, stools, tables, chests, wash basins, hearths, game boards, toys and artwork are individually made, installed, owned, moved/damaged and used via the same action system. Distinguish shell shelter/space from actual bed occupancy, storage and furniture effects: no double-counted capacity or free upgraded beds; founder-provided furnishings are explicit ledgered endowment. Hall accommodates the 12 initial people (at least 9.5 bed-equivalents if child occupancy remains half), with basic comfort. Inn services depend on actual beds, seats, food and workers; no automatic hotel income. Distinguish storage capacity and sleeping rights from ownership; reserve/deposit, expiry, abandonment, destruction and death must preserve physical goods and occupancy. Fix deceased-owner asset-store/estate issue and faction-derived fallback group ownership before advanced content. Preserve existing health/education/care/organisations (including herb access when old ResourceSource is removed), and divine/possession capabilities through migration tests.

#### 0.2.5 Acceptance amendments (UN11)

Add CATALOG-1: all workbook rows mapped with explicit status, deterministic export, schema/referential/cycle checks, stable IDs, no unrecorded differences. MOTOR-1: every unified autonomous/possessed ordinary act is a validated primitive control; step collision, turn, reach, capacity, refusal, duration, physical possession, non-enforcement of claims and save/replay parity; identical action stream from a player or policy gives identical physical consequences given identical initial state (control provenance remains separate). MOTOR-2: use distinct goods/targets to eat, collect/deposit, build and upgrade, produce and install a furnishing, occupy a bed, wash and use sanitation; no direct high-level act bypass. TRAIN-1: reproducible founder training artifact with independent held-out teacher-free performance compared with untrained/frozen baselines; train/eval separation and honest failures. TRAIN-2: children exposed to demonstrators learn more than matched controls in a useful interaction task without inheriting adult memories. NEED-1: all 12 need channels defined, bounded and exercised; enabled channels have reachable relief, with tests of new need effects and reward. ECON-1: full physical vertical slice from labour-only shelter to tool or furniture, exchange, installation and measured need/productivity benefit; no injected goods except declared founder stock. ECON-2: existing asset/store/death-location conservation, health/herbs, child care, trade, education, divine and possession regressions in unified mode. Track mechanical, autonomous and sustainable results separately. The catalogue target is complete mapping; full implementation and learned use of every row are not assumed tomorrow. New recipes and objects should be added without introducing more primitive action IDs.

**Corrected acceptance interpretation:** U-P1 retains exact legacy hashes when off; U-P2 means neutral foundation with every behaviour-changing subsystem off, not active unification. U2 excludes explicitly ledgered Founder food from the assertion about newly produced forage. U8 observes brain-generated action sequences/intentions, not mandatory adoption of a named scripted goal. U13 permits labelled test/teacher origins only outside autonomous demonstration. A favourable viability run needs autonomous survival, production and barter across generations before industry resumes; 20-seed demographic evaluation remains the desired follow-up to the 3-seed smokes. Report viability failure, not just completion of the report. No fabricated pass or forced historical sequence.


## 1. One simulation (D8.1, D8.9)

### 1.1 What "no faction AI plays the economy" means operationally

The test is a list of **who may do what**, enforced by the sim and checked by a static test and a run-time probe (section 14, U13):

| Activity | Stage 3 author today | Unified author |
|---|---|---|
| Choosing to build a building, where, and when | `AiBrain` / `AiDirector` through `EconomyCommands` (`PlaceBuilding`) | an NPC's chosen candidate (section 10), or a group's contributed work (section 12) |
| Building (labour) | anonymous builder walkers of `EconomySystem` | NPCs, through the C1 build act (`Civ/Work.cs`, `Work.VerbBuild`) |
| Harvesting raws | anonymous harvesters at `Harvests` buildings | an NPC collecting through a collection building (section 5.4) |
| Hauling | anonymous carriers on flag/road segments | the NPC that owns the goods or the job (`Verb.Haul`) |
| Storage | HQ and building stocks (`EconomyState`) | owned physical bins (`LotBin`, `ProductiveAsset.Store`) |
| Production | `EconomySystem` processors, faction priorities | an NPC working an asset with a recipe (section 8) |
| Trade | `Ai/AiTrader`, faction trade | NPC barter and gifts (M5 `SocialVerb.HandOver`, Offer/Accept) |
| Territory and roads | `TerritoryCommands` from a faction policy | settlement and road proposals by NPCs and groups (sections 6, 7) |
| Military units | abstract `Military` units | out of the unified milestone (kept in Stage 3 scenarios; C4 recruits come later from NPCs) |

**Rule U-1 (no faction economic writer).** With `Unification.Enabled = 1`, the module list is: Territory (geography, dice, topology), Population, Households, Social, Learning, Civ, Orgs, Divinity (if on), and the new Unification module. **`Economy`, `Ai`, `Factions` command emission, `Trade` (faction trade) and `Military` are not constructed** (`SimBuilder` skips their systems). A static test (`Unified_NoStage3EconomySystem`) scans the built sim's system list. The people are the only producers of intents; there is no policy object that returns a commands list for a faction.

**Rule U-2 (organisations act through members).** An organisation's decision (C6/C10) is permitted only when it is the product of actual members or office-holders' acts, as today (`OrgMind`); the unified start has none.

**Rule U-3 (divine player unchanged).** Suggestions, commands, compulsion, possession and miracles work as in D1–D7. The unified long-run demonstration (section 14.3) runs with `Divinity.Enabled = 0` or an idle god.

### 1.2 What remains of factions (D8.9)

`Faction` (`Sim/Territory/Faction.cs`, `F0`–`F5`) remains the **tribe**: Amber (F0), Crimson (F1), Azure (F2), Jade (F3), Violet (F4), Ivory (F5), in that order, with the existing colours of `FactionInfo`. In the unified game `Npc.Faction` means *initial social affiliation and display colour* and nothing else:

- it grants no property (D8.17); it is not an `OwnerKind` (the list has no tribe kind and none is added);
- it is not an employer, a state or a decision-maker (nothing in the sim reads `Faction` to decide an action);
- it is a **feature the learner may use** (same-tribe familiarity is a social-memory input as today) and a **view colour**;
- an NPC may leave, switch or hold no tribe (`Citizenship`, C5, and tribe change stay as they are; a child of two tribes takes the mother's by default);
- an individual belongs to a tribe, a household, work groups, a religious community and an enterprise at once (D8.9): tribe is one membership among several, and is the only one set at birth.

`FactionCount` in `GameSetup` stays 6 and `StartNpcsPerFaction` is replaced by the tribal generator (section 9). The Stage 3 human-faction seat (`PlayerControl.Stage3Faction`) is not used; the unified game is `PlayerControl.GodOnly`.

---

## 2. Geography (D8.2)

### 2.1 The seven hex types and what each gives

| `HexType` (**new** byte enum, `Sim/Board/HexType.cs`) | Raw material | Raw `Ware` | Forageable food | Forage `Ware` |
|---|---|---|---|---|
| `Forest` = 1 | wood | `Log` (existing 1) | mushrooms | `Mushroom` |
| `Mountain` = 2 | stone | `Stone` (existing 5) | roots | `Roots` (existing 29) |
| `Wetland` = 3 | clay | `Clay` (existing 2) | wild eggs | `WildEgg` |
| `Pastures` = 4 | sheep (living) | `Sheep` (animal, §4.5) | rabbit | `Rabbit` |
| `Fields` = 5 | hay | `Hay` | wild vegetables | `WildVeg` |
| `Desert` = 6 | none | — | cactus fruit | `CactusFruit` |
| `Sea` = 7 | fish | `Fish` (existing 30; raw, uncooked) | seaweed | `Seaweed` |

All new wares take ids from the extended personal-ware range (section 13.4); existing ids are never renumbered.

### 2.2 Mapping from the current terrain kinds (the migration)

Today `Terrain` (`Sim/Board/HexCoord.cs`) has seven values that are **not** these seven: `Meadow` (rim, yields nothing), `Forest`, `Hills` (clay), `Fields` (grain), `Mountain`, `Pasture` (not dealt on the island), `Desert` (one tile, no number). `RawKinds.OfTerrain` maps terrain to the five hauled raws.

| Current `Terrain` | → `HexType` | Note |
|---|---|---|
| `Forest` | `Forest` | unchanged |
| `Mountain` | `Mountain` | stone only; ore leaves the unified start (industrial milestone I1 re-adds ore as a deposit, not a hex raw) |
| `Hills` | `Wetland` | clay; the name changes only |
| `Fields` | `Fields` | yield changes from grain to hay; grain stays a Stage 3 ware |
| `Pasture` | `Pastures` | now **dealt** on the island (it was kept for dressing only) |
| `Desert` | `Desert` | the single tile stays; with more than one desert allowed by configuration |
| `Meadow` | `Sea` | the rim becomes sea; a rim hex next to land is a **coastal** hex |

**The migration** is a pure function `HexTypes.OfTerrain(Terrain)` (**new**) plus a new island deal `BoardGenerator.UnifiedPool` (30 land hexes: 5 Forest, 5 Mountain, 5 Wetland, 6 Pastures, 6 Fields, 3 Desert by default; configurable; the pool constant lives in `UnifiedTuning`) beside the unchanged `IslandTerrainPool`. `Board.HexTerrain[]` keeps its Stage 3 `Terrain` values; the unified game adds `Board.HexType[]` (**new**, not hashed when `Unification.Enabled = 0`). The number tokens (2–12) are dealt as today to every hex that has a raw material (Desert has none; Sea receives fish production numbers). The existing `TerrainInfo.IsResource` stays for Stage 3.

The sea ring: the unified board is the existing radius-3 six-player island plus one ring of `Sea` hexes (radius 4), so every outer land hex has a coast. Sea hexes are not walkable; the **shore nodes** (land nodes adjacent to a sea hex) are the access points for seaweed and fish (sections 3.5, 5.2). A land hex counts as coastal when any of its nodes is a shore node.

### 2.3 Continuous presentation

The hex grid and the node lattice stay internal (`BoardTopology`, `NodeGrid`, `Lattice`). `Assets/God/Game/Island` (the organic island look, P4a) already hides hexes; for seven types it needs one more ground blend per type and a sea shader boundary, driven by `Board.HexType`. The view never draws hex edges or node markers in normal play (the dev overlay stays). View work is outside the sim milestone and does not affect any hash.

---

## 3. Forage (D8.3)

### 3.1 Principle

Forage is **not a resource stock**. It is a *capability of a place*: standing at an accessible node of a hex of type T, an NPC may spend time and receive one item of T's food. There is no stock to deplete, no regrowth curve, no dice. It is the only way to eat that needs no building or tool.

### 3.2 Data: `ForageTable` (**new** data rows in `Sim/Population/ForageTable.cs`)

One row per hex type (all values configurable; defaults chosen so one adult needs about 100 ticks, under a tenth of the day, to feed itself on a middling food, compared with `HungerPerDay` 500 and `ForageTicks` 30 in `PopulationTuning`):

| HexType | Ware | Nutrition (hunger removed) | Collect ticks | Spoils after (days) | Access rule |
|---|---|---|---|---|---|
| Forest | `Mushroom` | 120 | 30 | 2 | any node of the hex |
| Mountain | `Roots` | 200 | 45 | 6 | any node; slope cost via normal walking |
| Wetland | `WildEgg` | 180 | 40 | 4 | any node |
| Pastures | `Rabbit` | 350 | 90 | 1 | any node (a hunt: needs a free hand, no tool) |
| Fields | `WildVeg` | 160 | 30 | 3 | any node |
| Desert | `CactusFruit` | 100 | 40 | 3 | any node (water drive pressure: sensed, not simulated beyond a thirst feature if M3 has none) |
| Sea | `Seaweed` | 90 | 35 | 2 | **shore nodes only** (§3.5) |

A row also carries `PoisonPermille` (default 0; the nightshade-style learning test uses it only in its scenario), and `Skill` (`SkillClass.Foraging`, optional: a skill shortens collection time by up to `ForageSkillMaxPermille` default 300).

### 3.3 Mechanics

1. **Perception.** Forage is perceived as a *place affordance*: `Sense` already reports the terrain under and around the NPC; the unified `Sense` adds one `Sensed` row of class `ObjClass.ForagePlace` (**new**, in the free range 130–139 proposed in §13.5) for the nearest accessible node of each forage type within sight, plus remembered places through `RecallNodes` as today. A forage place has no `Ref` stock; its "state" is its food kind.
2. **Act.** The M3 `ForageAt` verb (`Verb.ForageAt` = 10, `NpcVerb.ForageAt`) is reused. The act walks to `Reach.Touch`, then works for `CollectTicks` (adjusted by skill); on completion `Goods.Produce(..., ProduceReason.Foraged)` places one lot in the carry (or eats it at once if the NPC chooses to eat: the existing eat-from-carry act). Foraging never touches `ResourceSource` in unified mode.
3. **Per-individual rate limit.** The rate limit is the collection time itself, plus a carry limit (`CarrySlots`) and spoilage. No other cap is needed; an individual cannot forage faster than `1 / CollectTicks`. A configurable `ForageDailyItemCap` (default 0 = off) exists only as a safety valve.
4. **Shared-location throughput (optional, default off).** `Unification.Forage.SharedThroughput.Enabled = 0`. When on, each *forage cell* (a node and its neighbours within `CellRadius`, default 2) has `Tokens` that refill by `RefillPerDay` per day up to `Cap`; a forager needs a token; with none left it waits or moves (the learner sees a failed act and learns the cell is busy). Tokens refill: the food never runs out permanently (D8.3). Off, many NPCs can forage the same node at once.
5. **Coastal access.** Seaweed (and, via a fisher's shelter, fish) are available only from **shore nodes** of a Sea hex. A shore node is a land node with a Sea hex adjacent. Boats arrive later with the industrial vehicles package; until then deep-sea fish is out of reach (a configurable `FishAccessRadius` of 1 node of shore).
6. **Food and decisions.** Mushroom, Roots, WildEgg, Rabbit, WildVeg, CactusFruit and Seaweed are `Wares.IsFood`. Nutrition comes from the table (replacing `BerryNutrition` etc. in unified mode). Spoilage uses `ItemLot.SpoilDay` as today. Food can be eaten, carried, stored (in any bin that accepts food), gifted or traded with no change to `Goods`.

### 3.4 How this replaces the current finite regrowing wild-food sources

`ResourceSource` (kinds Berries, Roots, Fish, Nightshade, Wood; `Stock`, `Capacity`, `RegrowPermille`, `EmptyDays`) and `EcologyTuning` placement (`BerriesPerForest` and so on, regrowth from neighbours) are the M3 food model: finite, local, and the cause of the "universal population collapse" the owner refers to. Under `Unification.Enabled = 1`:

- no `ResourceSource` objects are placed (`Ecology.Place` skips); `PopulationState.Sources` is empty;
- `Wood` sources (hand-gathered logs) are **removed**; wood comes only through a woodcutter's shelter (§5);
- forage is the table above.

### 3.5 Implications for the M3 learning tests

The M3 learning scenarios (NPC_LEARNING L1–L4, Nightshade, the `learning_*` fixtures in `Population/LearningFixtures.cs`, and the pins P1–P9) depend on the finite, depletable, regrowing model: *learn that a bare source is empty and move on*, *learn what poisons*. They **keep their food model under the switch**: `Unification.Enabled = 0` is the default and every existing scenario sets it 0; `Unification.Forage.Enabled` is a sub-switch (`Unification.Enabled = 1` implies it; the sub-switch alone is allowed for experiments with M3 population and no collection buildings). New learning behaviour specific to the unified model (learn which food is best per tick; learn that the seashore is far) gets its own tests (U2 and §14.2). **No M3 hash changes with the switch off** (neutrality pin U-P1).

### 3.6 Tests (forage)

`Forage_NoBuildingNeeded`, `Forage_TakesTime` (ticks between start and lot equal `CollectTicks` ±`1`), `Forage_NeverDepletes` (10 000 acts at one node: no change to anything but the forager's lots and the ledger), `Forage_SeaweedOnlyAtShore`, `Forage_SharedThroughput_RefillsAndNeverZeroForever`, `Forage_LedgerBalances` (every food lot comes from reason `Foraged`).

---

## 4. Raw materials (D8.4)

### 4.1 From roll to physical lot: the pipeline

```
Dice roll (DiceSystem, 2d6, every DiceIntervalTicks = 60)
   hex number matches, hex has a raw material
        ↓ success
PendingAllowance[hex] += AllowancePerRoll   (default 6, brief example)   [cap AllowanceCap per hex; excess counted in WastedAllowance]
        ↓ every MaterialiseIntervalTicks (default 30), per hex
Materialise: move MaterialiseUnits (default 1) from PendingAllowance to a RawNode of that hex
        ↓
RawNode.Count  (physical, perceivable, visible to memory; owned by no one)
        ↓ only through a collection building (§5.4)
NPC collects → carry lot (ProduceReason.Collected) → carried → deposited into the building's bin
```

`DiceSystem` (`Sim/Territory/TerritorySystems.cs`) keeps its rolls, its stream `Rng.Fork("dice", roll)` and its `DiceRolled` event unchanged. The change is only what a matching hex receives: under unification `PileAdded`/`Piles[]` (stage 3 piles with `PileCap` 3) are replaced by an allowance. The Stage 3 pile path stays for Stage 3 scenarios (`Unification.Enabled = 0`).

### 4.2 Data (**new**, in `GodState.Unification.Geo`)

```
HexSupply { int Hex; HexType Type; int Pending; int Wasted; long IssuedEver, MaterialisedEver, CollectedEver; }   // one per hex, ascending hex id
RawNode   { int Id; int Hex; int Node; Ware Ware; int Count; int Cap; long ArrivedEver, TakenEver; }          // sorted by Id
```

`RawNode` placement is deterministic: at start, for each non-Desert hex of a raw-producing type, choose `NodesPerHex` (default 4) land nodes from the hex's interior (at least `RawNodeSpacing` = 4 nodes apart, not within 2 nodes of an HQ or building), using `Rng.Fork("rawnodes", hex)`. For Sea hexes the nodes are **shore nodes** adjacent to the hex. Materialisation goes to the node of the hex with the lowest `Count` (ties: lowest id), so a roll spreads rather than piling in one place; a full node (`Count == Cap`, default 12) is skipped; if all are full the unit stays pending (and the cap on `Pending` wastes excess). A `RawNode` is **not** an `ItemLot` and not a `GroundLot`: nobody can pick it up by hand (D8.5). It is perceivable (`ObjClass.RawNode`, §13.5) and rememberable.

### 4.3 Rates (configurable defaults, to be tuned by the viability experiments)

A 2d6 roll matches a given number with probability 1/36 (2, 12), 1/18 (3, 11), 1/12 (4, 10), 1/9 (5, 9), 5/36 (6, 8). At 60 ticks per roll, a day (1200 ticks) is 20 rolls. A hex numbered 6 or 8 therefore gets about 2.8 successes a day, 17 raw units a day at 6 per success; a 2 or 12 hex gets about 0.55 a day, 3.3 units. Over the 30-hex island (about 24 raw hexes) the expected supply is about 190 units a day, which is more than a primitive 72-person society can collect with level-1 buildings. The defaults are deliberately generous so the **capital bottleneck is labour and shelters, not the dice**; `AllowancePerRoll` and `MaterialiseIntervalTicks` are the scaling knobs. The materialisation rate (1 unit per 30 ticks per hex, so at most 40 units a day per hex) is the cap on how quickly a rich hex can feed collectors.

### 4.4 Perception and memory

- `Sense` reports a `RawNode` within sight as a `Sensed` of class `ObjClass.RawNode` whose visible state is a quantised count (none / few / many; the exact count is read only when within `Reach.Touch`).
- Memory uses the existing place memory: the NPC remembers "wood at node N, last seen many", the belief ages (`RecallNodes` 40). A remembered raw node produces a MoveTo-then-act candidate, never a direct act (the ontology rule).
- A rumour (M5 communication `TopicKind`) can say where materials are; that is ordinary social knowledge, not a new system.

### 4.5 Desert and sheep

- **Desert:** no raw material; no building collects from it; foraging cactus fruit only.
- **Sheep are a living resource.** A roll on Pastures adds allowance in **head** (`AllowancePerRoll` for sheep default 2). Materialised sheep appear as a `RawNode` of ware `Sheep` (a flock at the node). They are *not* a hand-carried lot. A **shepherd's shelter** enclosure (§5.2) collects them: an NPC works the building (herds the sheep to it); on arrival the head count moves into the enclosure's animal register (`Enclosure { int Sheep; int Capacity; long LastShearDay }` on the asset, **new**). Sheep in an enclosure: can be sheared (1 wool per `ShearIntervalDays` default 3, wool is a lot, sheep stay), slaughtered (meat and hide lots, sheep count falls), or left. No breeding in the first milestone (`SheepBreedPermille` default 0, configurable). They need no food in the first milestone (hay feeding is a later row: `SheepFeedHay` default off). Sheep count is conserved in the ledger as a living stock (§4.6).

### 4.6 Invariants and tests

| Id | Statement |
|---|---|
| `Geo-1` per hex: `IssuedEver = Pending + Wasted + MaterialisedEver` exactly |
| `Geo-2` per raw ware: `ΣMaterialised = ΣRawNode.Count + ΣTaken` |
| `Geo-3` `Σ Taken = Σ lots produced with reason Collected` (the ledger of §13.4); sheep: `Taken = Σ enclosure intake` |
| `Geo-4` no `Ware` of a raw kind is ever produced by any reason other than `Collected` (or `Crafted`/`Miracle`/`Founder`) in unified mode |
| `Geo-5` deterministic: the same seed gives the same `RawNode` set and the same roll-to-lot timeline |

Tests: `Allowance_FromRollThenGradual` (a roll at tick t adds `Pending`; units appear at `t+30k`), `Allowance_Cap`, `RawNode_NotPickable` (a hand `PickUp` of a raw node is refused with reason `NoBuilding`), `Sheep_LiveInEnclosure_ShearKeepsHead`, `Sea_FishAtShoreOnly`, `Desert_NoRaw`.

---

## 5. Collection buildings and the building architecture (D8.5–D8.8)

### 5.1 One building architecture: the capability model

A building is a **C1 productive asset** (`ProductiveAsset`, `Sim/Civ/Assets.cs`, R20: its `AssetId` comes from `Assets.Create`) with a data row. The existing `AssetDef` (kind, `BuildCost`, `BuildTicks`, `WorkTicks`, `Recipe`, `StoreCapacity`, `StaffSlots`) grows into `BuildingDef` (**new**, `Sim/Civ/BuildingDefs.cs`; the three C1 rows Field, Workshop, Store keep their kind ids 1–3 and become rows of the same table). A `BuildingDef` is data, never an enum switch.

```
BuildingDef {
  int Kind;  string Name;  int ObjClass;
  CapabilityFlags Caps;      // Shelter, Storage, Collection, Production, Employment (flags; Ownership, Access, Condition, Upgrade apply to every building)
  BuildingLevel[] Levels;    // 1..MaxLevel (default 3)
  HexType[] CollectsFrom;    // Collection capability: which hex type(s)
  Ware CollectWare;          // the raw (Sheep: the animal register)
  int CollectRadiusNodes;    // default 18 (about one hex): the building's collection zone
  Recipe[] Recipes;          // Production capability (§8)
  KnowledgeId Knowledge;     // the construction knowledge needed (§9.3)
  bool SettlementOnly;       // §6.3: false for basic/temporary types
}
BuildingLevel {
  WareCount[] UpgradeCost;   // cost of reaching this level from the one below (level 1: empty = labour only)
  int BuildTicks;            // work ticks of this step
  int StorageCapacity;       // units of the collected ware (and of other wares up to GeneralStorage)
  int SleepCapacity;         // beds (child = half a bed, existing rule)
  byte ShelterQuality;       // 1 Basic, 2 Good, 3 Excellent (comfort per night, §5.5)
  int Workers;               // concurrent workers (WorkerCapacity)
  int CollectPermille;       // collection productivity: 1000 = 1x
  int ProductionSlots;       // concurrent recipe runs (Production capability)
}
```

The nine capabilities of the brief map to data as follows.

| Capability (brief) | Where it lives |
|---|---|
| Shelter | `Caps & Shelter`, `SleepCapacity`, `ShelterQuality`; sleeping rights are `UseRight` kind `Sleep` (§5.5) |
| Storage | `Caps & Storage`, `StorageCapacity`; the building's bin (`ProductiveAsset.Store`, `BinKind.AssetStore`, `Reach.Store`) |
| Collection | `Caps & Collection`, `CollectsFrom`, `CollectWare`, `CollectRadiusNodes`, `CollectPermille` |
| Production | `Caps & Production`, `Recipes`, `ProductionSlots` (section 8) |
| Ownership | `ProductiveAsset.Owner`, `Basis`, shares: C1's unchanged ownership record |
| Access | `ProductiveAsset.Rights` (`UseRight`, `UseKind` extended: §5.6) |
| Employment / WorkerCapacity | `Workers`; C3 `Employment` supplies the contract; the building lists who works there |
| Condition | `ProductiveAsset.Condition` (permille, 1000 new); decays by `ConditionLossPerDay`; below `ConditionNoUse` it gives no shelter quality bonus and below `ConditionRuin` it cannot be used; repair is a work act with wood (`Verb.Build` on an existing asset with `Detail = Repair`) |
| Upgrade | `ProductiveAsset.Level`, `UpgradeMaterialsIn`, `UpgradeWork` (**new** fields, §5.8) |

Later buildings (hospital, refinery, barracks, factory, power station) are rows combining the same flags; industry appends rows and `ObjClass` ids (contracts §22.5) and never allocates ids itself (R20).

### 5.2 The six collection buildings and the three levels

All six are the **same code path**: rows differ in `CollectsFrom`, `CollectWare` and names.

| Kind (**new** ids 10–15) | Name | Collects | Where it may stand |
|---|---|---|---|
| 10 | Woodcutter's shelter | `Log` | a node within `PlacementRadius` of a Forest hex (collection zone covers Forest raw nodes) |
| 11 | Stonecutter's shelter | `Stone` | near a Mountain hex |
| 12 | Clay gatherer's shelter | `Clay` | near a Wetland hex |
| 13 | Shepherd's shelter | `Sheep` (animal) | near a Pastures hex; has an `Enclosure` |
| 14 | Hay gatherer's shelter | `Hay` | near a Fields hex |
| 15 | Fisher's shelter | `Fish` (raw) | a **shore node** of a Sea hex |

There is no kind for Desert. `PlacementRadius` (default 12 nodes) is the largest distance from the building to the nearest of its hex's raw nodes; a building placed where nothing is in its zone is refused (reason `NoResourceInReach`). The **collection zone** is every `RawNode` of the right hex within `CollectRadiusNodes` of the building (default 18); a raw node may be in several zones (D-brief: several buildings may collect from the same hex, competing).

Default levels (the brief's illustrative table, all configurable in `UnifiedTuning.Buildings`):

| Property | Level 1 primitive shelter | Level 2 established hut | Level 3 productive workshop |
|---|---|---|---|
| Upgrade cost (materials) | none | 10 `Log` | 20 `Log`, 10 `Stone` |
| Labour (`BuildTicks`) | 400 | 800 | 1500 |
| Storage capacity | 10 | 30 | 60 |
| Sleeping capacity | 2 | 4 | 6 |
| Shelter quality | 1 Basic | 2 Good | 3 Excellent |
| Concurrent workers | 1 | 2 | 3 |
| Collection productivity (`CollectPermille`) | 1000 | 1500 | 2000 |
| Production slots (§8) | 0 | 0 | 1 (a level 3 shelter may host one recipe, e.g. the stonecutter's shelter hosts "building blocks") |

Labour defaults are chosen so one adult can finish level 1 in about a third of a day of work (`1200 × 0.33`), a real but affordable first investment. A level-1 building uses no material **by decision D8.6**: its cost is recorded only as labour (`WorkTicks`) and time (the ticks the work takes); the construction does not call `Goods.Consume`.

### 5.3 Construction (level 1: labour and time only)

`Work.FoundSite` (`Sim/Civ/Work.cs`) already founds an unfinished asset where the NPC stands, with `CarriesMaterial` checking the build cost. Unified changes:

1. `CarriesMaterial` is trivially true for a cost line list that is empty; level 1 therefore needs no material. (No change to the function; the row has no cost.)
2. A site is **founded** at a valid node (§6.3 for where), by an NPC with the building's knowledge (§9.3). The founder becomes the founder of record; the owner is `Work.OwnerOf` (household or person) by default, or a group when founded by a group (§12).
3. **Labour:** the C1 `Work` act adds work ticks to `BuildWork` at the NPC's `Work.Speed` (skill and tool aware); several NPCs may work the same site **jointly** (a `UseRight` of kind `Work` is auto-granted to contributors; the founder's owner may stay sole owner or declare shares, §5.7). The building finishes when `BuildWork ≥ BuildTicks` (level-1 step). Elapsed time is therefore real: with one worker at speed 1000 it takes `BuildTicks` ticks; with three, a third.
4. When finished it emits C1's `AssetBuilt` (270) and the unification event `BuildingCompleted` (§13.6); it has a bin with `StorageCapacity` and its beds.
5. A **Condition** starts at 1000.

Level 2 and 3 are **upgrades** (§5.8). An NPC with knowledge of the level may go straight to building level 2 only through level 1 (levels are sequential: no skipping).

### 5.4 The collection process (D8.5, Part B §4)

Collection is an act of an NPC performed *at* a raw node *through* a building. The sim checks every condition:

| Check | Refusal reason (`Reasons`, **new** rows) |
|---|---|
| a finished collection building of the right kind exists | `NoBuilding` |
| the NPC holds a `UseRight` of kind `Gather` (collect) on it (owner, grant, employment, or the building's open communal setting) | `NoCollectRight` |
| the raw node is inside the building's zone | `OutOfZone` |
| the NPC is at `Reach.Gather` (2) of the raw node | (walks there first) |
| **storage:** the building's bin has `free capacity − reserved ≥ the load` | `StorageFull` |
| worker slots: fewer than `Workers` concurrent workers | `NoWorkerSlot` |
| building condition above `ConditionNoUse` | `BuildingRuined` |

Steps (every one physical, by a persistent NPC, no step by a system):
1. The NPC **chooses** (a candidate `Collect` whose target is a building; the candidate exists only when the NPC has a collect right and believes the building has room).
2. It **reserves** the load against the bin (`Reservation` list on the asset; expires after `ReserveTicks` default 600, so an abandoned reservation frees up).
3. It walks to the raw node, spends `CollectTicksPerUnit × 1000 / CollectPermille × 1000 / Work.Speed` ticks per unit (default `CollectTicksPerUnit` 60), takes units (up to `CarrySlots × StackSize` and the node's `Count`) from the `RawNode` (`Geo-3`), producing carry lots with `ProduceReason.Collected` and the **collector as lot owner**.
4. It walks back to the building (`Reach.Store` 1) and **deposits** into the bin (`Goods.Move` into the `AssetStore` bin). The bin never receives goods from the environment directly.
5. The building's role is only to **enable** and **hold**; an empty building does nothing; nobody works it unless an NPC decides to.

Productivity: `CollectPermille` scales the *per-unit time*, never the supply. A level-3 building and a level-1 building face the same `RawNode` counts.

**Storage rule (Part B §7).** A collector may not start collection when the designated bin has no free capacity (counting reservations). It may redirect the deposit to another building it holds a store right on (the candidate list offers each building it may deposit in), but never to the ground.

**Deposit target.** The deposit goes to the building through which the collection was made. A carried load may also be hauled to any bin the NPC may store in (its household's store, another building); that is the existing `Store` verb.

### 5.5 Sleeping (D8.7) and migration of M4 huts

- Every `BuildingDef` with `Shelter` has `SleepCapacity` beds and a `ShelterQuality`. A **sleeping right** is a `UseRight` of kind `Sleep` (new `UseKind.Sleep = 5`, appended; existing kinds untouched). Grant sources reuse `HutRights` (Built, Household, Invited) as the `Terms` field of the right.
- **Who may sleep:** the owner and the owner's household members and dependants (children included, existing "child = half a bed" rule), anyone granted a right, and anyone **without** a right as *unauthorised* (physically possible when a bed is free; the access check is a permission, not a physical check, as in the ontology; the classification is analytics-only `UseTheft`).
- **Comfort:** sleeping applies fatigue recovery as today plus a **comfort** effect by `ShelterQuality`: Basic 1×, Good 1.25×, Excellent 1.5× recovery (`ShelterRecoveryPermille` table) and exposure risk removed (`Exposure` death cause only outdoors; this reuses the M3 exposure model). Outdoors the NPC sleeps on the ground, the existing `GroundSleep` behaviour.
- **No separate housing system.** The Stage 3-era `Hut` object (`Population/PopulationState.cs`: `Built`, `LogsIn`, `LogsNeeded`, `Beds`, `Rights`, `Contributors`, `Lodgers`, `RightSource`) remains **only** for scenarios with `Unification.Enabled = 0`. Under the switch:
  - the M3/M4 hut-founding act (`Verb.Build` = 12 with log cost) is replaced by founding a building row **`Hut`** (`BuildingDef` kind 20: Shelter + small Storage, no Collection; level 1 labour only, 1 bed per adult, quality Basic, 4 beds); this is the answer to "I need shelter" (§10.4). It is needed because a family can sleep in a collection shelter only when one exists near their foraging; the plain hut is the same architecture, not a parallel system;
  - `Hut.Rights[]` becomes `UseRight(Sleep)` rows; `Hut.Contributors` becomes the right-granting rule (a contributor of labour gets a `Sleep` right as today, in order of contribution, while beds are free);
  - `Hut.Lodgers` becomes the child rule on the same asset;
  - `PopulationSystem`'s sleep seam (which finds "a hut with my right") is routed through a **`ShelterIndex`** (**new** derived, `[JsonIgnore]`, rebuilt in `FromState`) that answers "beds I may use and their quality" from assets in unified mode and from `Huts` otherwise. One query, two back ends: no behaviour change for Stage 3 scenarios.
  - `ObjClass.Hut` perception stays for the legacy; the assets perceive as `ObjClass` 130–139 (§13.5).

### 5.6 The rights model: five distinct things (D8.8)

Building ownership, collection rights, sleeping rights, storage access and ownership of collected goods **stay distinguishable**; nothing is inferred from any other.

| Thing | Representation | Default | Changed by |
|---|---|---|---|
| Ownership of the building | `ProductiveAsset.Owner` + `Basis` (`OwnerKind`: Npc, Household, Group, Enterprise, Org, Estate), shares (C1: one owner = 1000 permille; joint owners are a `Group` owner, §5.7) | the founder's household or person; a group when a group builds | sale, gift, inheritance (`MaterialInheritance`, `Estate`), abandonment, destruction (C1) |
| Collection right | `UseRight { Kind = Gather }` | the owner (and household); others by grant, employment or terms | `Ownership.Grant` (C1): the owner decides, may set `UntilDay` and `Terms` |
| Sleeping right | `UseRight { Kind = Sleep }` | the owner's household; contributors of labour | as above; the owner can revoke |
| Storage access | `UseRight { Kind = Store }` (deposit and withdraw each separate bits in `Terms`) | owner; collectors may *deposit* but only the owner (or a withdraw grant) may *withdraw* what is not theirs | grant |
| Ownership of collected goods | `ItemLot.Owner` of the lot in the bin | **the collector** keeps ownership of what it collected (it is their labour); it may be set by the collection `Terms` to the building owner (employment/wage-in-kind), or split (`SharePermille` collector/owner, a `Terms` field) | a collection grant's `Terms`; any later `Goods` ownership change (gift, sale) |

Consequences, deliberately not collapsed: a collector can deposit goods it owns in someone else's building (a storage right needed, not ownership); an owner can let others sleep but not collect; a stored lot is its collector's even though the bin is the owner's; theft remains possession without permission (the classification of `Ownership.Classify` is unchanged). C1's `UseKind` is extended by `Sleep` only. The `Gather` kind (existing 3) is the collect right. The C1-T11 invariant (nothing branches on the owner's kind) holds.

### 5.7 Joint construction and communal ownership

- Several NPCs working one site is the normal case: each `Work` act adds its ticks and records a **contributor** row (`Contribution { Npc, WorkTicks, Materials[] }`, bounded 8 per asset, **new** list on `ProductiveAsset`). On completion the building's owner is the founder's `Owner` unless the founder created a **group** and declared it the owner at founding (§12): a group owner makes the building *jointly or communally owned*; `Owner.Group(id)` already exists in `OwnerKind` and `Owner`.
- An NPC may build more than one collection building (no limit but their time and settlement rules).
- Communal and cooperative ownership are `Group` owners with membership rules (§12); a cooperative later is a C6 organisation owner (`OwnerKind.Org`) after conversion (§12.6). Nothing here changes C1's no-branch-on-owner-kind rule.
- An NPC may work at another's building through permission (`Gather` right), cooperation (the owner is a group the NPC belongs to) or employment (C3 contract, whose `Wage` is paid by `Pay`), exactly as C3 does for any asset.

### 5.8 Upgrades while operating

An upgrade to level L+1 is a **work project on the existing asset** (the same C1 build path):

1. An NPC (the owner or anyone the owner's terms allow) starts an upgrade by choosing the `Build` candidate with the building as target (`Detail = Upgrade`). The asset gets `UpgradeTarget = L+1` and a `MaterialsIn[]` array for the cost lines.
2. The NPC (or any contributor) **hauls materials** to the building (`Verb.Haul`) and puts them in (`Goods.Consume` with `ConsumeReason.Built`, taking them from the NPC's carry or from the building's own bin if the owner allows).
3. Once every line is in, work ticks add to `UpgradeWork` until `BuildTicks` of the new level.
4. **While operating:** during the upgrade the building keeps the *old* level's properties but **capacity reduced** by `UpgradeDisruptionPermille` (default 500, applies to `Workers` and `SleepCapacity`, rounded down, minimum 1) until finished; storage is unchanged. A level change is atomic at completion (`UpgradeCompleted`).
5. Level 2's cost default is 10 wood, level 3's 20 wood + 10 stone (from the brief); costs are data and may add other processed wares (planks, building blocks) in later rows: the cost lines are `WareCount`, so "wood, stone and processed materials" needs no code.
6. Upgrades never touch `RawNode`, `HexSupply` or the dice (`Geo`-invariants; the `Upgrade_DoesNotChangeSupply` test, §14).
7. **Failure and stuck states:** a stalled upgrade (no work or no material for `UpgradeStallDays`, default 5) keeps the partial progress; the building's disruption penalty is lifted when it stalls (the building returns to normal) so a stalled upgrade never ruins it.

Measurable upgrade benefit (a Part B acceptance test): `Upgrade_RaisesStorageSleepersCollectionRate` compares the same NPC in the same node setup at level 1 and level 2: units per 1000 ticks rises by the ratio `CollectPermille` (within the per-unit rounding) and storage and bed capacities change as tabled.

### 5.9 Abandonment, destruction, sale, inheritance

All by C1's existing mechanisms: `Condition` decay with no use, an owner's explicit abandonment (`Assets.Abandon`, ownerless and open), `Destroyed` flag (fire, miracle), `Property` transfers (gift, sale), `Estate` inheritance on death (`MaterialInheritance`). A building's bin contents follow their lot owners, not the building.

---

## 6. Settlements and development areas (D8.10)

### 6.1 What a settlement is

A settlement is a **vertex of the hex lattice** (a hex corner, `Corner`) with a **development area**; it is a geographical and infrastructural fact, not a government, owner or economic agent. It replaces the Stage 3 `Hq` as a *place*. In unified mode there is no HQ stock, no HQ build circle, no HQ-held goods; the Stage 3 `Hq` type remains for Stage 3 scenarios only.

```
Settlement { int Id; int Corner; int Node; long FoundedTick; int FoundedByGroup; int Founder; int Area (radius in nodes) }   // GodState.Unification.Settlements, ascending id
```

- **Distance rule (Catan):** two settlements may not stand on corners that share an edge (`Topology.CornersAdjacent`); existing `t.HqNearCorner(corner)` implements this for HQs and is reused (the `Reasons.HqTooClose` row is reused with an alias `SettlementTooClose`).
- **Development area:** the nodes within `AreaRadiusNodes` (default 24, about one hex) of the settlement node, minus nodes inside another settlement's area for the *first* hex ring (areas may overlap at the margins: a building belongs to the nearest settlement's area by node distance, ties to the older settlement).
- **A settlement owns nothing** (D8.10): no stock, no land, no resource. Being in an area only permits founding non-basic buildings (§6.3).

### 6.2 Founding

- **The six founding settlements** are placed by the setup: one per tribe, on the six standard six-player start corners (`BoardGenerator`'s existing six-player starts), each at a coast-near land corner so seaweed and fish are reachable. Each is created at tick 0 with its tribe's communal building (§9.5).
- **New settlements** are founded by NPCs: a **`ProposeSettlement`** act (Verb in §13.5) by an NPC or group names a candidate corner; contributors do real work and materials (a "founding work" project of `SettlementFoundWork` default 600 ticks and `SettlementFoundWood` default 4 `Log` for a marker/communal shelter; configurable; labour-only default is allowed by setting wood 0); on completion the settlement exists and its area opens. A settlement can only be founded on a corner with the distance rule satisfied, on land, with at least `MinLandHexes` (default 2) land hexes touching, and within `MaxSiteDistance` of an existing settlement's area (default 3 hexes: no isolated flags across the sea).
- The proposer's reason is an NPC's goal (section 10: "I want a better collection site", "my group wants a place near forest"); nothing triggers it automatically.

### 6.3 What may be built where

| Building class | Inside a development area | Outside any area |
|---|---|---|
| Basic or temporary (`BuildingDef.SettlementOnly = false`): Hut level 1, collection shelter level 1, camp | yes | yes, **level 1 only**; no upgrades beyond level 1 outside an area |
| Everything else (level 2+, workshops, stores, production, later hospitals etc.) | yes | no (refused with `OutsideArea`) |

A level-1 collection shelter outside an area exists so a lone forager can reach a distant forest; it can be upgraded only when a settlement's area grows to cover it (a new settlement founded nearby). Roads and settlements follow section 7.

### 6.4 Tests

`Settlement_DistanceRule`, `Settlement_OwnsNothing` (no `Owner` of kind settlement; the settlement's id never appears in any `Owner`), `Settlement_NewOnlyByNpcWork`, `Outside_OnlyBasic`.

---

## 7. Roads (D8.11)

### 7.1 What a road is now

A road is a **chain of Catan edges between two settlement sites** (or from a settlement to a *planned* settlement site). It changes only the **walking cost** of the nodes along it (the existing `NodeRoad`, `NodePather` `RoadStepTicks` 4 vs `StepTicks` 6). It has no flags, no carriers, no handover points; no building requires a road; goods never move along it by themselves.

```
Road { int Id; int FromCorner, ToCorner; int[] Nodes (the lattice path, ≤ 64); int Progress (work ticks in); int Needed; byte State (Planned, Building, Done, Abandoned); int Proposer; int Group; ... }
```

Rules: edges are valid hex edges (`Topology` edge adjacency); the from-end is an existing settlement or a corner already a **planned site** (`SettlementSite` proposal); the to-end is another settlement or a planned site (D8.11). A road needs no building at either end. Overlapping roads share nodes; road nodes are marked in `NodeRoad` as today; **flags are not placed** and `FlagTooClose` logic is not consulted in unified mode.

### 7.2 Built by informal groups: the collective-action loop (brief §10)

1. **Experience.** NPCs walk between two places repeatedly (a `TravelLog` keeps their recent journeys: node pairs and tick costs, 8 entries, already implied by place memory).
2. **Proposal.** An NPC whose recent journeys on the same corridor exceed `RoadProposeJourneys` (default 6 in 3 days) and whose learned association "walking this corridor costs time" is strong, may choose the **`ProposeRoad`** candidate (§10.3): it creates a **group** (§12) with objective `Road(from, to)`, a **contribution plan** (labour ticks `Needed` default 40 per node; optionally materials: planks later) and invites others (a Say/Offer gesture reused from M5 or a direct message).
3. **Decision.** Each invitee independently decides to join and then whether to contribute each day (§12.3): they weigh expected benefit (how often *they* walk there), their drives, trust in the proposer, and what they see others contributing. Some will not contribute though they will use the road (**free-riding**, measurable).
4. **Work.** Contributors perform the **`RoadWork`** act at the next unfinished road node: `Progress` increases at their `Work.Speed`; materials, if any, go in as real lots (`Consume` reason `Built`).
5. **Completion.** When `Progress ≥ Needed` the road is `Done`, nodes get `NodeRoad`, the walking cost drops, `RoadCompleted` is emitted. If contributions stall for `RoadStallDays` (default 6), the group may dissolve; partial roads stay partial (the built prefix is usable).
6. **Ownership and disputes.** A road is **public by default** (no owner; anyone walks). A group may declare itself the road's owner for later maintenance and a toll norm (C10 norms can form; not modelled here). Disputes are the existing C1 dispute and C9 complaint machinery when an NPC claims a right to block; the first milestone does not add tolls.

### 7.3 Tests

`Road_NoFlagsNoCarriers`, `Road_OnlySpeedsWalking` (path cost drops by exactly the on-road factor; no goods moved), `Road_PlannedSiteAllowed`, `Road_NeedsContributions` (with `Contributors = 0` no progress), `Road_FreeRiding_Measurable` (users who did not contribute > 0 in a scenario with `ContributeBase` low).

---

## 8. Production recipes (D8.12)

### 8.1 Recipes are data rows

The C1 `Recipe` (`ProduceReason`, `Inputs[]`, `Outputs[]` of `WareCount`) is kept and extended into `RecipeDef` (**new** data table `RecipeTable`, `Sim/Civ/Recipes.cs`; append-only; industry adds rows):

```
RecipeDef { int Id; string Name; WareCount[] Inputs; WareCount[] Outputs; int LabourTicks; KnowledgeId Knowledge; SkillClass Skill; ToolReq Tool (ware + min uses, none allowed); CapabilityReq Needs (a BuildingDef flag + min level); ProduceReason Reason; }
```

An NPC works a recipe by the C1 `Work` act at an asset whose `Caps & Production` and `Recipes` contain it and whose level meets `Needs`; inputs are consumed from the asset's store or the worker's carry; outputs go to the store, owned by the asset's owner (C1 rule, unchanged). The act needs the **knowledge** (§9.3), the **tool** equipped (`Verb.Equip`, tool factor in `Work.Speed`), the labour ticks, and the materials: "each transformation requires actual materials, knowledge, labour and appropriate equipment" (brief §6).

### 8.2 The first rows (brief §6; all configurable)

| Id | Process (building capability needed) | Inputs | Output | Labour ticks | Tool |
|---|---|---|---|---|---|
| R1 | Sawing (a level-2+ woodcutter's shelter, or a standalone `Carpentry` row, **new** kind 30) | 1 `Log` | 1 `Plank` | 80 | saw? (none in the first set: axe optional, +25 % speed) |
| R2 | Stonecutting (level-2+ stonecutter's shelter, or kind 31) | 1 `Stone` | 1 `BuildingBlock` | 100 | hammer optional |
| R3 | Potting (clay gatherer's level 3, or `Pottery` kind 32) | 2 `Clay` | 1 `Pottery` | 90 | — |
| R4 | Firing (kind 33 `Kiln`) | 2 `Clay` + 1 `Log` | 2 `Brick` | 120 | — |
| R5 | Shearing (shepherd's shelter enclosure) | 1 sheep (kept alive) | 1 `Wool` | 40 | — (output only per `ShearIntervalDays`) |
| R5b | Slaughter | 1 sheep (consumed) | 3 `Meat`, 1 `Hide` | 60 | — |
| R6 | Weaving (kind 34 `Weaver`) | 2 `Wool` | 1 `Cloth` | 100 | — |
| R7 | Bedding (any level-2+ shelter) | 2 `Hay` | 1 `Bedding` | 40 | — |
| R8 | Cooking (kind 35 `Hearth`, or any Shelter level 2+ with Storage) | 1 `Fish` | 2 `PreparedFood` (better nutrition and spoilage) | 40 | — |
| R9 | Toolmaking (kind 36 `Toolmaker`) | 1 `Log` + 1 `Stone` | 1 `Axe` or `Hammer` (`Detail` chooses; tool with `Uses`) | 120 | — |

Meat and hides come from sheep; Prepared food and cooked fish are new wares (food, with nutrition and spoilage rows). The recipe quantities are defaults. Rows R1–R4, R6 and R9 are the Widelands chains reduced to the brief's list; later rows (coal, oil, metal, electricity, vehicles) are the **industrial package's rows** on the same table (§13.7).

### 8.3 Reuse and replace of the stage-3 processor data

`Economy/BuildingDefs.cs` (Stage 3: `Harvests`, processors with inputs of two raws each and ten processed wares) stays untouched for Stage 3 scenarios. In unified mode it is **not read**. Its *ideas* (which processed ware is made from which pair) are retained only where the brief's list names them; the ten processed wares of Stage 3 (`Mortar`, `Bronze`, `Iron`, `Steel`, `Bread`, `Leather`) are not in the unified start and appear later with the industrial recipe rows. The wares `Plank`, `Brick`, `Pottery`, `Cloth` already exist (ids 7, 9, 14, 17) and are reused; `BuildingBlock`, `Bedding`, `PreparedFood`, `Wool`, `Meat`, `Hide`, `Hay`, `Axe`, `Hammer` are new ids (section 13.4). Stage 3 `Axe` (20) is kept; the unified tool uses it so the existing "Equip" learning (M3b: `Verb.Equip`) is reused.

### 8.4 Tests

`Recipe_NoGoodsFromNothing` (every output ware's ledger balances against consumed inputs and reasons), `Recipe_NeedsKnowledge`, `Recipe_NeedsTool` (if the row requires one), `Recipe_NeedsCapability`, `Recipe_DeterministicLabour`.

---

## 9. Starting population (D8.9)

### 9.1 Composition

Six tribes in the order Amber, Crimson, Azure, Jade, Violet, Ivory = `Faction.F0`..`F5`. Each: **7 adults and 5 children** (12), so 42 adults, 30 children, 72 in all. Exact counts are asserted (U1). Tribe sizes are configurable by `UnifiedTuning.Start` (`AdultsPerTribe` 7, `ChildrenPerTribe` 5) but U1 uses the defaults.

### 9.2 Deterministic family generator (**new** `Founders` generator 2, "Tribal")

`Founders.StartGame` has `ft.Generator` 0 (scenario mix) and 1 (`Established`); generator 2 is **new** and the only one the unified start uses. For each tribe `t` the generator draws from `Rng.Fork("tribe-family", t)` and produces:

- **Households (default pattern, configurable list):** three couples (6 adults) plus one adult **sibling** of one partner (the seventh adult), who lives in that sibling's household (an aunt or uncle for the children, a second caregiver).
- **Children:** five biological children distributed as 2, 2, 1 over the three couples (a pattern table; other patterns allowed).
- **Ages (milli-years as today):** adults 18–50 with the partner age gap within `PartnerAgeGapMax` (default 8 y); the sibling within 15 y of the partner; children 0–14, at least one under 4 in each tribe where the pattern allows; each parent is at least `MinParentAgeAtBirth` (default 18) older than each of their children.
- **Genetics:** each adult founder's genome from `Rng.Fork("genome-founder", npcId)` (`GenomeOps.Founder`); the **sibling shares ancestry** with their partner-sibling by being generated from a common (unnamed, archived-as-unknown) parent pair via `GenomeOps` inheritance so the two are genetically related (the Pedigree records them as full siblings with unknown parents); a **child's genome** is `GenomeOps` inheritance of its parents' genomes (as at conception today), with the usual mutation; sexes are drawn per npc; couples are of opposite sex and **not** related (`Relatedness == 0` checked).
- **Cross-tribe:** adults are *unrelated across tribes* (all founders). Cross-tribe partnering is allowed later by M4 partner preference.
- **Personalities:** trait vectors from the genome and `Rng.Fork("personality", npcId)` as today, varied across the tribe (a check requires the spread between the highest and lowest adult in each trait of at least `PersonalitySpreadMin`).
- **Experience:** each adult gets a modest, varied `SkillStore` entry (0–200 in one or two of foraging, building, hauling; no profession, no title); children have none.
- **Caregiving:** every child has at least two caregivers among their household's adults (parents; plus the aunt/uncle); infants are assigned a primary caregiver for feeding; the M4 caregiving logic then applies (`HVerb.Tend`, `Feed`).
- **Names:** from the existing naming table by tribe culture; deterministic.

Generator output is a pure function of `(seed, tribe)`: re-running yields the same 72 people (U14).

### 9.3 Initial knowledge (D8.17)

`KnowledgeId` (K1's knowledge store, `KNOWLEDGE_AND_TECHNOLOGY_DESIGN`; until K1 lands the minimal set is a bitset on `NpcMind.Unif.Known`, **new**) starts with these items per NPC, with individual variation:

| Knowledge | Default holders |
|---|---|
| Basic survival: foraging all seven foods, eating, sleeping, fire-less warmth | everyone |
| Construction of level-1 shelter (`Hut`, collection shelter 10–15) | all adults (95 % deterministic by personality seed); each child learns by imitation/teaching later (C7 `Teach`, M3 `Imitation`) |
| Construction of level-2 (upgrade) | 40 % of adults at start (varies) |
| Construction of level-3 | 10 % |
| Sawing, stonecutting, weaving, potting, cooking | each adult knows 0–2 of them (varies); no one knows toolmaking at the start except by chance (≤ 10 %) |
| Road building | 50 % of adults; settlement founding | 25 % of adults |

Knowledge is **not** a profession: it unlocks candidates; it is learned, forgotten, taught and discovered (`KnowledgeAcquired` etc.). Skill (speed) and knowledge (permission to try) and equipment stay separate fields (owner: "keep learning, physical skills, knowledge and capability distinct").

### 9.4 Not at the start (D8.9)

No government, no professional military, no currency, no employers, no formal institutions, no professions, no production chain. Specifically: `Orgs.Enabled = 1` but `Emergence` governs; none is seeded; the founding community group (below) is an **informal group**, not an organisation.

### 9.5 The communal building and its owner

Each tribe starts with **one communal building** at its founding settlement: a `BuildingDef` row `CommunalHall` (kind 21: Shelter + Storage, no Collection; level 1; `SleepCapacity` 8 beds of Basic quality, `StorageCapacity` 30 general; built at tick 0 and complete).

**Who owns it.** The tribe never owns it. At start the generator creates one **founding community group** per tribe (`OwnerKind.Group`, `Group` record with kind `Community`, members = the seven adults, no officers, no rules) and registers the hall to `Owner.Group(g)` with `OwnershipBasis.Founded`. Every adult is a member, with a `Sleep` and `Store` right through membership; children sleep by their household's right. The group's membership can change (join, leave, expel through ordinary acts; death releases the share to the remaining members, C1 Estate for group shares), and its members may dissolve it, sell the hall or give it away (D8.17). Because it is a group, not an organisation, no authority exists and none is added by this decision; a later step (a community decision to formalise) can convert it into an org (§12.6), never by threshold.

### 9.6 Start sequence (the setup)

1. Create the six tribe settlements at the six standard start corners (`Settlements`).
2. For each tribe: run the family generator; place the communal hall at the settlement node; place the NPCs around it (deterministic ring spacing, `HutSpacing`); create the founding group; grant rights.
3. Give each household a **small starting food** (`StartFoodDays` default 1 day of the household's forage food in the hall's bin, owned by the group) so the first day is not decided by luck; this is a `Founder` produce reason, one entry in the ledger, recorded (configurable 0).
4. Start the dice, materialisation and everything else. **No** collection buildings, no huts, no tools exist at tick 0 (no production chains).

---

## 10. Individual planning (D8.13)

### 10.1 Aim and constraint

Add **bounded, interruptible multi-step goals** beside the associative learner (`Learning/Learner.cs`, `Thinker`, `OpportunitySlot`). The planner **never executes**; it only proposes the next concrete act as an ordinary candidate that the learner scores against every other option under the NPC's drives, with a bounded **Plan term** added to the score (like the `Social` and `Divine` terms of `OpportunitySlot`). Every executed act is an existing verb with its existing sim checks and its existing reward signal. There is no faction strategy, no script of eight instructions, and nothing is hard-coded per activity: a goal is data (a *target*), an expansion is derived from data (building defs, recipe rows, known places, social options), and a step is a candidate.

### 10.2 Data model (**new**, `NpcMind.Plan`, null unless `Unification.Enabled`; hashed only when present)

```
Plan {
  byte Count;                 // 0..MaxGoals (default 2): goals pursued at once; goal 0 is the active one
  Goal[] Goals;               // bounded, ≤ 2
}
Goal {
  GoalKind Kind;              // Shelter, Capital (first collection building), Upgrade, Stock (get a ware), Sell, Craft, Road, Settlement, Group, Care...
  int Target;                 // a BuildingDef kind / ware / place / group id
  int Param;                  // a count or level
  Step[] Steps;               // ≤ MaxSteps (default 6)
  byte Cursor;
  long AdoptedTick;
  int Patience;               // decisions the plan may lose before abandonment (default 8)
  int Replans;                // ≤ MaxReplans (default 3)
  int Weight;                 // the current Plan term, permille of a drive's worth; decays with Patience
}
Step {
  StepKind Kind;              // Obtain(ware, n), GoTo(place), Collect(building), Haul, Build(building/site), Work(asset, recipe), Trade(ware, n), Ask(npc), Join(group), Use(building)
  Ware Ware; int Count; int TargetId; int Node;
  byte State;                 // Pending, Active, Done, Failed
}
```

All sizes are fixed so the mind's hash is bounded. The goal list persists in saves (`NpcMind.Plan` in `NpcMind`'s JSON like `Kin`, `Social`).

### 10.3 Where goals come from

A **goal candidate** is generated by `Thinker` (through a `UnifMindHooks` partial in the Civ-then-Orgs-then-Divinity-then-Unification dispatch order, §13.5) whenever a *felt state* and the NPC's *knowledge and beliefs* make an outcome reachable:

| Goal kind | Trigger (a feature the learner already sees) | Needed knowledge |
|---|---|---|
| `Shelter` | the NPC has no `Sleep` right and slept outdoors (the `Exposure`/comfort self-feature `SelfFeature.Unsheltered`, **new** in the free `SelfFeature` range, §13.5) or sleeps in poor quality | a shelter building row |
| `Capital` (first collection building) | the NPC *perceives or remembers* raw nodes of a kind (`RawNode` memory) and holds no collection right on a building of that kind; and the learned association "stored goods are valuable" or an observed neighbour with a stock | construction knowledge of that row |
| `Upgrade` | an owned building at level L; a want for storage, beds or speed (e.g. storage full recently, or sleeping full) | knowledge of level L+1 |
| `Stock` | a recipe's input or a build cost is short | the recipe or building row |
| `Craft` / `Sell` / `Road` / `Settlement` / `Group` | as described in sections 8, 11, 7, 6, 12 | their rows |

The goal candidate becomes **an ordinary candidate** with `Class = ObjClass.Goal` (**new**, §13.5) and a learned feature `FeatureKind.Goal`-less encoding by `EpisodeKey` kind (the `Episode-key kind` 48–55 range, §13.5), so the learner's associations learn *which goals have paid off for this individual*: "adopting Capital(wood) led to comfort and surplus last time". Goal candidates compete with eating, sleeping, foraging, fleeing and everything else in the same softmax. Adoption: when a goal candidate is **chosen**, the planner expands it.

### 10.4 Expansion (the planner)

A deterministic backward chain over data, bounded by `MaxSteps` and `MaxExpandNodes` (default 24 evaluations):

1. **Goal → required capability or ware.** E.g. `Shelter` needs a `Sleep` right on a building with `SleepCapacity ≥ 1`. Known options: use an existing building the NPC may sleep in (cheapest); ask for a right (`Ask` step); build one (`Build(Hut)`).
2. **Build → needs** (from the row): level-1 needs construction knowledge (a missing knowledge makes the option unavailable, with a `Learn` step if a known teacher exists), a **site** (a node valid by §6.3 and near a raw node for collection kinds, found from *the NPC's own memory of places*, not by a global search), and **labour**. Level 2+ needs `UpgradeCost` wares: for each missing ware an `Obtain` step.
3. **Obtain(ware, n)** expands by *options available to this NPC*, in the order of its own believed cost in ticks (never a global optimum): (a) it holds or has stored goods, (b) a known place where the ware is collectable by a building it may use (`Collect` + `Haul`), (c) a known NPC who has surplus and would trade (`Trade`, based on the NPC's social memory and goods it holds to give), (d) a gift request (`Ask`), (e) a group (`Join`). If none is known: an **explore** step (existing `Verb.Explore`) with a bounded search budget.
4. The expansion yields ≤ `MaxSteps` steps (default 6). If longer, the planner stores only the **next sub-goal** (a short prefix) and re-expands on arrival. A plan is a *plan sketch*, always reconstructible.

**The step is a candidate.** On each decision tick, the planner emits at most **`PlanCandidates` (default 2)** concrete candidates derived from the active step: e.g. `Collect` at a named raw node through a named building, `Haul` to a named bin, `Build` on a named site, `HandOver`/`Say(Offer)` to a named NPC. They go into the normal candidate set with `OpportunitySlot.Plan` = `Goal.Weight` (a bounded permille added to `Total`). The learner scores them with learned associations like any other. Candidate quota for Unification: `MaxCand` rises by 4 (§13.5).

### 10.5 Interruption, replanning and abandonment

| Event | Behaviour |
|---|---|
| A drive crisis (hunger, fatigue, fear, pain) | the Plan term is a small bounded add-on; any stronger drive candidate simply wins the softmax. The plan is **not cancelled**; it is merely not progressed. Each lost decision costs `Patience` 1 only if the winning candidate was *not* a survival verb (survival interruptions are free). |
| Step precondition changes (a bin full, a node empty, a site taken, a building ruined) | the step is marked `Failed`; the goal is **replanned** (`Replans++`, ≤ 3), skipping the failed option; a `GoalReplanned` event |
| A better alternative appears (another building with a free bed, a trade offer) | the planner re-evaluates the remaining steps at every step boundary and takes the cheaper option the NPC believes (so "I found a free bed" completes Shelter goal) |
| Danger | `Flee` wins; the plan is paused, resumes later or abandons by patience |
| Patience exhausted, `Replans` exhausted or `AbandonDays` (default 6) with no progress | `GoalAbandoned(reason)`; the goal is removed, and a **negative episode** is recorded for the goal's key (so the learner gives it a lower prior) |
| Goal done (the building stands and the right is held) | `GoalCompleted`; the outcome reward (comfort, stored goods, income) trains the goal's key as an episode (the existing deferred-outcome mechanism, M3 episodes, `Verb.Build` open episode) |

### 10.6 Learning from outcomes

Nothing new learns: the existing associative store gets (a) the per-act reward of each step as always, (b) the **goal-level episode** on completion or abandonment, keyed by `(goal kind, target kind)`, so a successful *build a woodcutter's shelter* raises the NPC's later tendency to adopt it, and a failure lowers it. Others learn by imitation (M3 `Imitation`) and by social transmission (M5), so an NPC who has seen a neighbour sleep better after building is more likely to adopt the goal. Knowledge that the building improves collection comes from the NPC's own experience (the measured speed up at the building) and from what it hears.

### 10.7 Worked example 1: the shelter (brief §7)

Eight steps of the brief, produced by the NPC and not a script:

1. *Felt state:* the NPC slept outdoors three nights: `SelfFeature.Unsheltered` is on, comfort is low.
2. *Goal candidate* `Shelter` appears among its candidates; its learned prior for "build a shelter" is positive (an adult knows the construction); it is chosen over foraging this decision because its drive-weighted score exceeds the alternatives.
3. *Expansion:* it checks the shelters it may use (the communal hall is full; none other known), decides to build; **the hut is a level-1 `Hut` row: labour only**, so there is no wood or stone `Obtain` step in the first game (the brief's wood and stone are the later *level 2* hut); the planner finds a site: a free node within its place memory inside the settlement area.
4. *Steps:* [GoTo site, Build(Hut)].
5. The NPC walks to the site (candidate `MoveTo` that wins), then the `Build` candidate; it works `BuildTicks`; hunger interrupts at tick 200: it foraged (survival verb: free interruption), returns, resumes (the step survives; patience unchanged).
6. The hut is finished; `Sleep` right granted to the NPC and its household; the NPC sleeps in it, gets the Good/Basic comfort; the reward trains the `Build` and the `Shelter` goal association.
7. Another person with stone or wood is irrelevant for level 1; the *level 2* upgrade later generates a `Stock` goal: `Obtain(10 Log)` that the NPC can do by collecting at its own woodcutter's shelter or trading with someone who holds wood (the brief's "another person has stone" case arises for the upgrade).

### 10.8 Worked example 2: the first collection building

1. A forager passes a Forest hex and *perceives* raw nodes with `many` wood (or hears of it). It remembers them.
2. It has the knowledge to build a woodcutter's shelter. The learned association "storing a good gave me something to trade or eat later" (or the sight of a neighbour with a bin of wood) makes `Capital(wood)` a candidate. It is chosen on a day it is well fed.
3. Expansion: `Build(Woodcutter L1)` at a site near its remembered raw nodes within `PlacementRadius`, inside a development area or as a basic level-1 outside one (§6.3); labour only; steps [GoTo(site), Build(10)].
4. After the building stands: the NPC holds a `Gather`, `Store` and `Sleep` right through ownership; `Collect` candidates (target: building and raw node) appear when raw nodes are in its zone and the bin has room. It chooses collection when its drives allow; each trip is an ordinary act; the learner reinforces it if the stock is useful.
5. With 10 or more logs stored, the NPC may adopt `Upgrade(Woodcutter L2)`; no one told it to.

### 10.9 Invariants and tests

| Id | Statement |
|---|---|
| `Plan-1` | a plan never acts: every act performed by a plan-origin candidate passes the same sim checks as a non-plan act (static test: no write to sim state in the planner assembly path) |
| `Plan-2` | bounded: goals ≤ `MaxGoals`, steps ≤ `MaxSteps`, expansions ≤ `MaxExpandNodes` per goal; the mind's hash size is constant |
| `Plan-3` | the Plan term is bounded: `OpportunitySlot.Plan ≤ PlanTermMax` (default 400 permille of the strongest drive), so a crisis always outranks a plan |
| `Plan-4` | deterministic: expansion order is by ids; tie-breaks by `Rng.Fork("plan", npcId, day)`; no unordered enumeration |
| `Plan-5` | no faction strategy: no code path reads `Faction` in goal generation or expansion (static test) |
| `Plan-6` | no hidden global knowledge: expansion reads only the NPC's `Beliefs`, its place memory, its `SocialMind`, its own carry and bins it may use; never `GodState` truth (a static analyser test on the namespace's reads, like the M3 "no truth in the policy" test) |

Tests: `Plan_ShelterExample` (headless: a sheltered-less NPC builds a hut within a day without any command), `Plan_FirstBuilding_FromPerception`, `Plan_InterruptedByHunger_Resumes`, `Plan_AbandonsAfterPatience_AndRecordsNegativeEpisode`, `Plan_Replans_WhenBinFull`, `Plan_ToldNothingByFaction` (the static test).

---

## 11. Specialisation and exchange (D8.14)

### 11.1 No professions

There is no profession field. The word "woodcutter" is the *building* (a data row) or an analytics label (`DayRole` of the KPI: "an NPC who spent ≥ 60 % of work ticks on one recipe/collection in 5 days"). Nothing in cognition reads that label.

### 11.2 The emergence path (mechanisms that make it possible)

| Step | Mechanism that makes it possible | How we measure whether it happened |
|---|---|---|
| Different people do different things | different knowledge and skills (§9.3), different tools (an axe raises wood speed through `Work.Speed`), different locations (a shelter near wood), learned associations (rewarded repetition), personalities | Herfindahl index of each NPC's work ticks over activity kinds, over 10 days (`KPI Specialisation`, §14) |
| Surplus appears | storage (a bin) plus a collection building lets a worker collect more than they use; spoilage penalises hoarding food | `SurplusHeld` per ware per NPC |
| Others want it | needs of others (building upgrades need wood or stone; a household without a woodcutter needs wood for upgrades; an NPC with no collection right wants stone) | `Demand` as unmet `Stock` goals per ware |
| Exchange | M5 barter and gifts (`SocialVerb.Offer/Accept/HandOver`, `Valuation`): a holder offers surplus for what they want; subjective values differ by need and holdings | `ExchangesPerDay`, `ExchangeRatios`, `GiftsVsBarter` |
| Specialisation pays | the specialist's tools and building level make a unit of their work cheaper than others'; trade lets the others do what *they* do best | `ProductivityGain` (units per work-hour of the specialist vs the generalist), `NetBenefit` of exchange |
| Persistence of specialisation | the learner reinforces what worked; cost of switching (travel, tools) | transitions between activity kinds per NPC per month |
| Change | circumstances (a building ruined, a famine, a new neighbour) change rewards; the learner re-weighs | `SwitchRate` after shocks (the shock tests of §14) |

**Exchange reuses M5.** `Goods` hand-over, the offer/accept dialogue, subjective `Valuation`, trust updates: unchanged. The unification adds only *what is offered*: raw lots from a building's bin, deposited goods, services (access to a bed, access to a building: grant a right as a *good* in a trade, represented as a `UseRight` grant with `Terms`).

**Employment reuses C3** (`Civ/Employment.cs`: `Hire`, `WorkFor`, `Pay`, wage as a good). A building owner may hire a worker to collect or produce; because **everyone can build a level-1 shelter for free** (Part B §5), an employer must offer an advantage (a better building, tools, a better bed, a better site); the C3 contract negotiation is unchanged and its price is whatever the two agree in goods. No wage is set by anything but the parties.

**Money.** None at start (D8.9). Barter and gifts only; K2 (money) later builds on the same exchange acts.

### 11.3 Tests (observational: never "designed in")

`Spec_DifferentActivities_Measured`, `Exchange_Physical_HandOver` (U7), `Employment_C3Unchanged`, `NoProfessionField` (static: no `Profession`, `Job` field on `Npc`), `Spec_TwoRuns_SameHash`.

---

## 12. Informal groups (D8.15)

### 12.1 What a group is

An **informal group** is a persistent social record created by an NPC for a **common objective**, with members who each decide to take part. It sits between the household and the organisation (brief §9: individual, household, informal group, enterprise or community organisation, institution, state) and it is **not** C6's `Organisation` (no offices, funds, records, charter or services).

```
Group { int Id; byte Kind (Community, Build, Road, Settlement, Haul, SharedAsset, Defence, Care, Trade, Other);
        int Founder; Member[] Members (≤ MaxMembers default 24); Objective Objective; GroupState State;
        long FoundedTick; Owner AssetOwner (the group as OwnerKind.Group); ... }
Member { int Npc; long Joined; int ContributedWork; Ware/Count contributions (bounded 4 kinds); byte Status (Invited, Active, Withdrawn, Expelled) }
Objective { ObjectiveKind Kind; int Target (building site, road id, settlement corner, ...); WareCount[] Needs; int WorkNeeded; long Deadline }
```

Stored in `GodState.Unification.Groups` (ascending id). `OwnerKind.Group` exists and is used for ownership of group assets; `Group` ids are allocated by the unification module only.

### 12.2 Propose, invite, join

- **Propose:** an NPC chooses the `ProposeGroup` candidate when a goal's expansion contains a step that it cannot do alone (a road, a settlement, a large upgrade, a shared workshop) or when it believes sharing is cheaper (a shared bin). Creating it costs only the NPC's action time; it names the objective.
- **Invite:** by `Say(Invite)` (a new `GestureAct` slot in the free range or the existing Offer; `Detail` = group id) to NPCs it can speak to (M5 comms); and the group is **perceivable** (`ObjClass.GroupSite`, a flag at the work site): others who see or hear can ask to join.
- **Join:** each invitee evaluates a `JoinGroup` candidate: expected benefit of the objective to *them* (use of the road, a bed in the building), cost (time and goods), the proposer's trust, the number already in, and their drives. Joining is **voluntary** and costs nothing by itself.

### 12.3 Contribute, withdraw, free-ride

Contributions are only **real**: work ticks at the site (the same C1/`RoadWork` acts, with the group recorded as contribution beneficiary) and wares put in (`Goods.Consume` into the project). A member who joins and never contributes is a **free rider**; nothing forces a contribution. The group never forces and never owns more than what is contributed. A member may **withdraw** at any decision tick: their contributions already in stay in the project (no refund unless the group agrees or the objective is dissolved: refund policy is data, default `NoRefund`; a withdrawing member's *future* share of the benefit is determined by the objective: a built road is public, a building owned by the group leaves the withdrawing member without a right).

### 12.4 Objectives and completion

| Objective | What completion produces |
|---|---|
| Building (a building row, site) | the building, owner `Owner.Group(g)`, with rights for each member by contribution share (a `Terms` field: `SharePermille` by contributed work) |
| Road | a public road (§7) |
| Settlement | a settlement (§6) |
| Gather and haul | the hauled goods are delivered to the group's bin, owned by `Owner.Group(g)` |
| Shared asset | a workshop or store jointly owned |
| Defence | a **non-economic** objective in the first milestone: no military; recorded and inert (C4 later) |
| Care | shared caregiving for dependants (reuses M4 `Tend`/`Feed` with a group household flag) |
| Trade | a joint expedition to another tribe's settlement (a plan whose steps are `GoTo`, `Trade`); proceeds belong to the individuals who gave the goods |

When the objective is complete, the group either **dissolves** (default, `GroupObjectiveDone` then `GroupDissolved` after `AfterglowDays`), or **persists as the owner** of its asset. Contributors keep their individual rights; the group's later decisions (maintenance, who gets use) are the members' acts: a group asset's grants are issued by a member in accordance with the group's `GrantRule` (default: any active member may grant `Sleep`/`Gather` rights to a member's household; any grant to an outsider needs `Majority` of active members' explicit accept acts; configurable).

### 12.5 Disputes

Disputes use C1's `Disputes` list on the asset and C9 complaint machinery when present. The group has no internal court; a quarrel is a social-memory event with trust effects.

### 12.6 Relation to C6 organisations (never by threshold)

A group **may become** a C6 organisation (enterprise, cooperative, religious community, institution) **only by an act of its members**: an active member chooses the `FoundOrg` candidate (`OrgCommands`, the existing C6 founding path) and the rest accept in the same way they join. There is **no automatic conversion** at any number of members, any size of assets or age of the group (D8.15, owner: "do not automatically create formal organisations merely because a threshold is reached"). A test asserts that a scenario with 24 members and a large asset never creates an `Organisation` unless an NPC acts. On conversion the group's asset ownership is transferred (`Property.Transfer`) to the organisation and the group record ends with `GroupBecameOrg`.

### 12.7 Tests

`Group_ProposeJoinContribute`, `Group_FreeRider_DoesNotContribute_StillMember`, `Group_Withdraw_KeepsContributions`, `Group_NoAutoOrg_AtAnyThreshold`, `Group_RoadBuilt_OnlyWithEnoughContributors`.

---

## 13. Reconciliation and migration (D8.16)

### 13.1 Table of retained, replaced and removed systems

| System (current code) | Decision (D8.16) | In the unified game |
|---|---|---|
| Catan geography: `Board`, `BoardTopology`, `BoardGenerator`, hex numbers | **Retain**; add `HexType` and the unified deal (§2) | as is, with seven types |
| Dice: `DiceSystem` | **Retain; gradual materialisation** | rolls the same; effect is an allowance (§4) |
| Settlements: HQ corner rules (`HqNearCorner`, `Hq`, `TerritoryRules.PlaceHq`) | **Retain and refine** | settlement corners, distance rule; no HQ stock/circle (§6) |
| Roads: `TerritoryRules` road placement, `NodeRoad` | **Retain and refine** | Catan edges, walking-speed effect only (§7) |
| Fine movement lattice (`Lattice`, `NodeGrid`, `NodePather`) | **Retain internally** | unchanged |
| Widelands recipes (`Economy/BuildingDefs.cs` processors) | **Retain** as data | recipe rows (§8) replace the processor table for the unified start |
| Widelands flags (`FlagPlaced`, `NodeFlag`) | **Remove from normal gameplay** | none placed; no `FlagTooClose` check |
| Road-segment carrier system (`EconomySystem` carriers) | **Remove** | none; NPCs carry |
| Anonymous builders (`EconomySystem` builder walkers) | **Replace** with NPCs | C1 build act |
| Anonymous harvesters | **Replace** with NPCs | collection (§5.4) |
| Faction stockpiles (`EconomyState` HQ stocks) | **Replace** with owned physical inventories | bins (`LotBin`, `Store`) |
| Faction economic AI (`Ai/AiBrain`, `AiDirector`, `AiTrader`, `EconomyBot`) | **Remove from normal gameplay** | not constructed |
| Automatic faction construction | **Replace** | individual/group decisions |
| Stage 3 military units (`Military`) | **Retain isolated** (regression); the unified start has none | C4 later recruits NPCs |
| NPC cognition (`Cognition`, `Learning`, `Policy`) | **Retain and extend** | + planning (§10) |
| NPC families and learning (M3, M4, genetics) | **Retain** | + tribal founder generator (§9) |
| NPC barter and employment (M5, C3) | **Retain** | unchanged |
| Organisations and institutions (C6–C10) | **Retain** | start off, may emerge (§12.6) |
| Divine player (D1–D7) | **Retain** | unchanged |
| `Hut` and `ResourceSource` (M3/M4) | **Retain for M3/M4 scenarios**; replaced in unified | `Hut` → building row (§5.5); sources → forage (§3.4) |
| `GodBootstrap` faction-AI start | **Retain for Stage 3**; unified start uses `UnifiedBootstrap` (**new** in `Game/Boot`) | God-only seat |

The Stage 3 game remains reachable and bit-identical (§13.2): "we should not delete working functionality before the replacement passes its tests".

### 13.2 The switch plan and neutrality

New tuning block `SimTuning.Unification` (`IOmitWhenDefault`, written only when changed, like `Divinity`, `Orgs` and `Civ` in `SimTuning.cs`): `Enabled` (0 default), `Forage.Enabled`, `Forage.SharedThroughput.Enabled` (0), `Materialise.Enabled`, `Buildings.Enabled`, `Planning.Enabled`, `Groups.Enabled`, `Roads.Enabled`, `Settlements.Enabled`. Convention: `Enabled = 1` sets the others to 1 unless a scenario overrides; the sub-switches exist so packages are testable apart.

| Switch (`SimTuning.Unification`) | Default | Off means |
|---|---|---|
| `Enabled` | 0 | no `GodState.Unification`; the Stage 3 and M3–M5 game is bit-identical |
| `Forage.Enabled` | 0 | M3 `ResourceSource` food as today |
| `Materialise.Enabled` | 0 | `DiceSystem` makes stage-3 piles |
| `Buildings.Enabled` | 0 | Stage 3 economy buildings and M3/M4 huts |
| `Planning.Enabled` | 0 | no `NpcMind.Plan`, no goal candidates |
| `Groups.Enabled` | 0 | no groups; no `ProposeGroup` |
| `Roads.Enabled` / `Settlements.Enabled` | 0 | Stage 3 territory commands |

**Neutrality pins** (contract §16 style; "switch **all** new modules off except the one under test"; scenarios `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, seeds 1–3, every checkpoint and every part):

| Pin | Statement |
|---|---|
| `U-P1 Unification_Off_Reproduces_Base` | all unification switches 0: every part hash equals the base head's |
| `U-P2 Unification_Idle_Neutral` | foundation allocation only, all behaviour-changing switches off, legacy start: every legacy part equals U-P1 (active unification intentionally differs) |
| `U-P3 Forage_SubSwitch_Isolated` | only `Forage.Enabled = 1` in a Stage 3 faction game: Stage 3 parts unchanged (population parts differ by design) |
| `U-P4 Planning_Off_Neutral` | `Planning.Enabled = 0` inside a unified run reproduces the same run with plan terms zero |
| `U-P5 Recording_Neutral` | Off/Summary/Full recording of the new domains gives one hash |
| `U-P6 Switch_Matrix` | pairwise and all-on |

New state lives only in `GodState.Unification` and its sub-objects, `NpcMind.Plan` and the `Npc`/`ProductiveAsset` additive fields (hashed only when present), consistent with the save rules of §17 of the contracts. Save version bump by the integration lead only.

### 13.3 Tuning block map (all defaults of sections 3–12)

`UnifiedTuning`: `Forage` (rows, `SharedThroughput`), `Geo` (`AllowancePerRoll`, `AllowanceCap`, `MaterialiseIntervalTicks`, `MaterialiseUnits`, `NodesPerHex`, `RawNodeSpacing`, `RawNodeCap`), `Buildings` (rows, levels, `PlacementRadius`, `CollectRadiusNodes`, `CollectTicksPerUnit`, `ReserveTicks`, `UpgradeDisruptionPermille`, `UpgradeStallDays`, `ConditionLossPerDay`), `Settlements` (`AreaRadiusNodes`, founding costs, site rules), `Roads` (`RoadPerNodeWork`, `RoadProposeJourneys`, `RoadStallDays`), `Recipes` (rows), `Start` (family patterns, ages, knowledge shares, `StartFoodDays`), `Planning` (`MaxGoals`, `MaxSteps`, `MaxExpandNodes`, `Patience`, `MaxReplans`, `PlanTermMax`, `AbandonDays`), `Groups` (`MaxMembers`, `GrantRule`, `AfterglowDays`).

### 13.4 Goods and ledger (an extension to confirm)

- **Wares.** `Wares.PersonalCount` is 40 today (33–39 free): the unified game needs more than seven new wares. Propose **raising `PersonalCount` to 96** with the same rule as F1.17 (the population hash keeps the old 40-ware order and adds the new wares only when non-zero; the ledger arrays widen; stage 3 per-ware arrays use `Wares.Count = 28` unchanged). New ids: 40 `Mushroom`, 41 `WildEgg`, 42 `Rabbit`, 43 `WildVeg`, 44 `CactusFruit`, 45 `Seaweed`, 46 `Hay`, 47 `Sheep` (animal register only; never a lot), 48 `Wool`, 49 `Meat`, 50 `Hide`, 51 `BuildingBlock`, 52 `Bedding`, 53 `PreparedFood`, 54 `Hammer`; 55–95 reserved for industry and later. The existing `Log`, `Clay`, `Stone`, `Plank`, `Brick`, `Pottery`, `Cloth`, `Axe`, `Roots`, `Fish` are reused (so the ledger spans Stage 3 and unified wares with one identity). **This is a contracts amendment (the goods surface of §22.3) the lead must confirm.**
- **Reasons.** `ProduceReason` has 8 slots with 6 and 7 reserved: **6 = `Collected`** (raw material taken from a `RawNode` through a building), 7 reserved. `ConsumeReason` unchanged; `Built` covers building and upgrade materials. The conservation invariant `Σ Produced − Σ Consumed = Σ (carry + ground + bins)` remains exact (C1 of the ledger). **Environment books** are separate: `ΣHexSupply issued = pending + wasted + materialised` and `ΣRawNode.Count + ΣTaken = ΣMaterialised`; together they give the full chain from roll to lot.

### 13.5 Contract extensions to propose (the contracts document must confirm)

These are **proposals within free ranges**; nothing here is allocated until `SHARED_ARCHITECTURAL_CONTRACTS` accepts it (the same owner process as §22).

| Space | Proposed | Notes |
|---|---|---|
| Learning `Verb` (byte, `Max = 128`; free: 114–115, 119–123 are other packages' spares; 127 and 21–31 taken) | **raise `Max` to 160; 128–139 for Unification**: 128 `Collect`, 129 `ProposeGoal`/`AdoptGoal` (internal), 130 `ProposeGroup`, 131 `JoinGroup`, 132 `ContributeGroup`, 133 `LeaveGroup`, 134 `ProposeRoad`, 135 `RoadWork`, 136 `ProposeSettlement`, 137 `Upgrade` (Build with `Detail`), 138 `Repair`, 139 spare | the verb table's `Max` is a hashed array bound: the raise is a foundation change |
| Intent `NpcVerb` | 220–229: 220 `Collect`, 221 `RoadWork`, 222 `Propose` (`Detail` = group/road/settlement), 223 `JoinGroup`, 224 `Contribute`, sentinel `UnifReserved = 229` | after the Knowledge/money block 210–219 |
| `ObjClass` | 130–139: 130 `RawNode`, 131 `ForagePlace`, 132 `Settlement`, 133 `Road`, 134 `GroupSite`, 135 `Goal` (candidate class), 136–139 buildings of unified kinds (collection, shelter, communal, production) as a **range per asset row**, not one number: assets take `ObjClass` from the row, 140–149 reserved for rows | contracts list 123–129 spare and 110+ reserved: ask for 130–149 |
| `SelfFeature` | 88–95: 88 `Unsheltered`, 89 `StoreFull`, 90 `HasCollectRight`, 91 `InGroup`, 92–95 spare | |
| `SituationKind` | 152–159 | `NoShelter`, `BuildingNear`, `GroupInvite` |
| Episode-key kind | 48–55: 48 goal, 49 building investment, 50 collection route, 51 group contribution, 52–55 spare | free range |
| Candidate quota | Unification 4 (`MaxCand` 40 → 44) | |
| `UseKind` (Civ's enum, appended) | `Sleep = 5` | |
| Telemetry domains | `Unification = 80`, `Buildings = 81`, `Planning = 82`, `Groups = 83` → `RangeOf` **1200–1299** | after 1100–1199 (K1/K2) |

### 13.6 Events (a proposed code block 1200–1299, to be confirmed by the contracts)

| Codes | Domain | Events |
|---|---|---|
| 1200–1219 | Unification (geography, forage) | 1200 `ForageCollected` (HV: ware, hex), 1201 `AllowanceGranted` (hex, units, die total), 1202 `AllowanceWasted`, 1203 `ResourceMaterialised` (HV: node, ware, count), 1204 `ForageThroughputWaited` (HV), 1205 `UnifiedDay` (daily snapshot: stocks, pending, nodes, foraged, collected), 1206 `TribeStarted` (tribe, adults, children, households), 1207–1219 spare |
| 1220–1249 | Buildings | 1220 `BuildingFounded`, 1221 `BuildingWorked` (HV), 1222 `BuildingCompleted` (carries level; C1's `AssetBuilt` 270 is also emitted), 1223 `CollectionStarted`, 1224 `CollectionDeposited` (HV: units, building, collector), 1225 `CollectionRefused` (reason: `NoBuilding`, `NoCollectRight`, `StorageFull`, `OutOfZone`, `NoWorkerSlot`), 1226 `UpgradeStarted`, 1227 `UpgradeMaterialAdded`, 1228 `UpgradeCompleted`, 1229 `UpgradeStalled`, 1230 `BuildingSlept` (HV), 1231 `BuildingRuined`, 1232 `BuildingAbandoned`, 1233 `RecipeRun` (HV), 1234 `SheepSheared`, 1235 `SheepSlaughtered`, 1236–1249 spare. C1's `UseRightGranted` (272), `UseRightEnded` (273) are reused for rights |
| 1250–1269 | Settlements, roads | 1250 `SettlementProposed`, 1251 `SettlementFounded`, 1252 `SettlementFailed`, 1253 `RoadProposed`, 1254 `RoadWorked` (HV), 1255 `RoadCompleted`, 1256 `RoadStalled`, 1257 `RoadAbandoned`, 1258–1269 spare |
| 1270–1289 | Planning | 1270 `GoalAdopted`, 1271 `GoalStepAdvanced` (HV), 1272 `GoalReplanned`, 1273 `GoalAbandoned` (carries reason), 1274 `GoalCompleted`, 1275–1289 spare |
| 1290–1299 | Groups | 1290 `GroupProposed`, 1291 `GroupJoined`, 1292 `GroupContributed` (HV), 1293 `GroupWithdrew`, 1294 `GroupObjectiveDone`, 1295 `GroupFailed`, 1296 `GroupDissolved`, 1297 `GroupBecameOrg`, 1298–1299 spare |

Each event carries ids only (not references), `Control` (0 own, 1 compelled, 2 possessed, 3 avatar) when started under divine control; the SAC §8 rules apply (own partial catalogue array, own mapper, codes never reused, `Version` bump for an added field). Warehouse migrations: one new migration owned by the unification package (tables `buildings`, `building_events`, `goals`, `groups`, `hex_supply`); the integration lead allocates the migration number.

### 13.7 Packages for the next run (independent, in this order)

U0 foundation (switches, tuning, state shells, ware/ledger widening, id ranges, `Founders` generator 2 stub) → U1 geography and forage (§2, §3, §4) → U2 buildings and collection (§5) → U3 population start (§9) → U4 settlements and roads (§6, §7) → U5 planning (§10) → U6 recipes and production (§8) → U7 groups (§12) → U8 exchange and employment integration (§11) → U9 regression and neutrality (§13.2) → U10 analytical reconciliation and the long-run demonstration (§14.3). Industry (INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN, D6) is sequenced **after** U10 and uses `RecipeTable`, `BuildingDef` and the `Unification` ids rather than a parallel economy.

### 13.8 Retiring temporary mechanisms

Stage 3 economy systems are *kept, off*; they are removed from the code only after: U-P1 holds, the Stage 3 regression scenario passes on its own tuning, and the owner agrees. Until then `Unification.Enabled` selects the world; no shared mutable state is written by both.

---

## 14. Acceptance (U1–U14, Part B, long-run demonstration)

Every scenario is a **headless** run (`HEADLESS_SIMULATION`): `dotnet test DotNet` and the headless runner; seeds 1–3 unless said; the measurement is read from the event log and state at the stated tick. Numbers marked (default) change with tuning; the *test* is the relation.

### 14.1 The brief's fourteen scenarios

| Id | Scenario | Measurable criterion | Headless demonstration |
|---|---|---|---|
| U1 | Six tribes, exactly 7 adults and 5 children each | tick 0: 6 tribes (F0..F5) × (7 adults, 5 children); total 72; ages: adults ≥ 18, children < 15; each child has ≥ 2 adult caregivers of their household; no formal office or building except six communal halls | `Unified_Start_Counts`; asserts counts, ages and household structure |
| U2 | Survive by forage independent of dice | with `Materialise.Enabled = 0`, dice never rolling (a scenario with no number tokens): all 72 survive 20 days; every food lot has reason `Foraged`; no `ResourceSource` exists | `Forage_Survive_NoDice` |
| U3 | Rolls make gradual physical materials | after a success at tick t, `Pending` rises by `AllowancePerRoll` at t; the `RawNode` counts rise by one unit each `MaterialiseIntervalTicks` until the allowance is spent; the unit total conserves (`Geo-1`, `Geo-2`) | `Geo_RollToLot` prints the timeline |
| U4 | An individual collects raw materials and builds a productive building | an NPC (no command) completes a level-1 collection shelter and then ≥ 10 units are deposited into it by an NPC within 10 days in ≥ 2 of 3 seeds | `Capital_FirstBuilding` |
| U5 | Another individual specialises in a different resource | by day 30, ≥ 2 distinct collection kinds each have ≥ 1 NPC for whom ≥ 60 % of work ticks over 5 days were that kind; the NPCs differ | `Spec_TwoResources` |
| U6 | Production through actual recipes | an NPC runs R1 or R2 (needs knowledge, labour, materials, the right level): the output ledger matches consumed inputs; a recipe is refused without any of them | `Recipe_Run`, `Recipe_RefusedWithout` |
| U7 | Physical exchange of goods between two individuals | a barter hand-over event pair within `Reach.HandOver`, both lots change possession and ownership, neither NPC receives goods otherwise | `Exchange_Physical` |
| U8 | An individual independently identifies and pursues a multi-step construction objective | a goal `Capital` or `Upgrade` with ≥ 3 steps adopted without any command, completed or abandoned with an event; the plan passes `Plan-6` | `Plan_MultiStepConstruction` |
| U9 | Cooperation to build a road between settlements | a road group with ≥ 3 contributors, a completed road between two settlement sites (or a planned site), path cost on it lower; ≥ 1 free-rider recorded in a variant | `Road_Cooperative` |
| U10 | Founding a new settlement | an NPC or group founds a settlement satisfying the distance rule; its development area opens; first non-basic building inside it | `Settlement_Founded` |
| U11 | Goods carried directly by NPCs, no flags or anonymous carriers | zero flags, zero road-segment carriers exist; every goods movement event has an `Npc` mover; goods reach a distant building by carry | `NoFlags_NoCarriers` |
| U12 | An informal group emerges from individual participation | a group record is created by an NPC's `ProposeGroup` and has ≥ 2 members who each joined by their own act | `Group_Emerges` |
| U13 | The whole process without a faction-level RTS economic AI or divine intervention | the sim's system list has no Stage 3 economy, AI, faction trade; `Divinity.Enabled = 0` or idle; a static test and a runtime probe that no intent has an origin other than an NPC mind | `NoFactionAi_NoDivine` |
| U14 | Deterministic, replayable, analytically reconcilable | same seed, same hash at every checkpoint; a replay equals the original; the ledger and environment books balance to zero error | `Unified_Determinism_Replay_Reconcile` |

### 14.2 Part B acceptance tests

| Id | Statement | Measurable criterion | Demonstration |
|---|---|---|---|
| B1 | forage without any building | an NPC with no building anywhere forages and eats | `Forage_NoBuildingNeeded` |
| B2 | no raw collection without an appropriate operational building | collection refused with `NoBuilding` (none), `NoCollectRight` (wrong person), or `BuildingRuined`; a hand pick-up of a raw node refused | `Collect_RefusedWithout` |
| B3 | autonomous first collection building without material inputs | build completes with zero `ConsumeReason.Built` units; materials ledger unchanged | `Level1_NoMaterials` |
| B4 | construction consumes labour and time | `BuildWork` equals `BuildTicks` at completion; elapsed ticks ≥ `BuildTicks / workers` | `Level1_LabourAndTime` |
| B5 | physical collect, carry, deposit | the event chain `CollectionStarted → CollectionDeposited` spans ≥ a walking time proportional to the distance; the lots pass carry then bin | `Collect_CarryDeposit` |
| B6 | collection respects finite storage | when bin free capacity is 0, collection is refused (`StorageFull`); with a reservation, a second collector is refused until capacity is released; collected units never exceed capacity | `Collect_StorageCap` |
| B7 | buildings give sleeping accommodation | an NPC with a right sleeps in the building, beds counted; capacity limits sleepers; children use half beds | `Shelter_Sleeping` |
| B8 | upgrade consumes materials and labour | `ConsumeReason.Built` units equal the upgrade cost; `UpgradeWork` equals `BuildTicks(L+1)` | `Upgrade_Costs` |
| B9 | upgrades give a measurable benefit | storage, beds, shelter quality, collection rate change per the table; supply unchanged | `Upgrade_Benefit`, `Upgrade_DoesNotChangeSupply` |
| B10 | individuals cooperate, trade and specialise around buildings | scenario "three tribesmen": by day 40 an exchange between a wood collector and a stone collector occurs, one upgrades using exchanged wood | `Economy_ThreeIndividuals` |
| B11 | no faction-level economic AI | as U13 | as U13 |

Plus per-section tests listed in sections 3–12; neutrality pins U-P1…U-P6 (§13.2).

### 14.3 The long-run demonstration (D8.18) and viability experiments

**The demonstration:** the six-tribe start run for `LongRunYears` (default 30 years, 300 days) with `Divinity.Enabled = 0`, no commands, recording on. The owner's criterion: *leave the 72 people running and find workshops, trade, roads, new settlements and communal organisations emerging, deterministic and reconcilable*. The pass condition is a **report**, not a pre-set outcome: it states for each seed which of the following happened and when (the sim may legitimately fail to produce some, and that is a finding): first collection building; first upgrade; first exchange; first recipe run; first group; first road; first new settlement; first organisation (by a member act); counts of buildings by level; population by age; deaths by cause; Gini of holdings; the specialisation index; the share of NPC work ticks spent foraging versus building versus collecting versus producing. The run is replayed from its seed: same hash.

**Viability experiments (the new forage model):** the first objective is a **coherent, self-sustaining primitive society** before anything advanced.

| Experiment | Question | Measure | Done when |
|---|---|---|---|
| V1 Baseline | does a 72-person society survive 5, 20, 50 years with level-1 buildings only and the default forage? | population curve, causes of death, births, mean nutrition; extinction by tribe | ≥ 5 of 6 tribes alive at 50 years in ≥ 2 of 3 seeds, or the report records why not |
| V2 Throughput | what does turning on shared throughput do? | the same measures with `SharedThroughput` on at `Cap` 6/12/24 | the tipping point (population stable vs collapse) located; default stays off |
| V3 Forage richness | sensitivity to nutrition and collect time per food | stable population versus time spent foraging | a plot of forage time share against population stability |
| V4 Supply | sensitivity to `AllowancePerRoll`, `MaterialiseIntervalTicks` | collected units/day, upgrades by day 100 | the capital bottleneck is labour, not supply, at the default |
| V5 Generations | do 3+ generations (genetics, M4) keep skills and knowledge? | inherited knowledge counts, teaching events | knowledge not extinct across generations in ≥ 2 seeds |
| V6 Shocks | a bad-luck dice streak (no rolls on a hex for 10 days), a famine of one food, a ruined building | recovery time, switching | societies recover or the report states the failure mode |
| V7 Planning ablation | with `Planning.Enabled = 0`, does anything but forage and sleep happen? | which U-scenarios fail | shows what planning contributes (the value of §10) |

These are run in the analytical runner (`HEADLESS_SIMULATION`, the bench harness), results written under `docs/runs/`; no experiment changes a default silently: a change to a default is a recorded decision.

### 14.4 Invariants (whole-module list)

| Id | Statement |
|---|---|
| `Unif-1` | with the switch off, no unified type is constructed and every hash is the base's |
| `Unif-2` | goods: the ledger identity holds with the new wares and reason `Collected` |
| `Unif-3` | environment books (section 4.6) balance exactly |
| `Unif-4` | no ware of a raw kind enters any inventory except through `Collected` or `Crafted` or `Founder` or `Miracle` |
| `Unif-5` | every building's bin content ≤ its storage capacity (counting reservations) at all times |
| `Unif-6` | sleepers in a building ≤ beds at all times |
| `Unif-7` | for every asset: rights, owner and goods ownership are independent (a mutation test changes one and checks the others unchanged) |
| `Unif-8` | no code in the unified path reads `Faction` to decide an economic act (static) |
| `Unif-9` | every economic act has an NPC actor (the `Control` field and an actor id on every event) |
| `Unif-10` | determinism: same seed, same hash at every checkpoint; save/load parity at a mid-state (open build, open collection, open plan, open group, open road): `*_SaveLoadParity` |

---

## 15. Open questions (minimal; each has a configurable default)

1. **Verb space (consequential).** The learning `Verb` table is a byte with `Max = 128` and nearly full; section 13.5 proposes raising `Max` to 160. If the lead refuses, the unified verbs must reuse existing rows with `Detail` (collect = `Gather` with a building target, upgrade = `Build` with `Detail`), and `ProposeGroup`, `JoinGroup`, `ProposeRoad`, `ProposeSettlement` need either spare rows (114–115, 119–123 belong to Orgs and Divinity) or a generalised `Propose` row with `Detail`. Default: raise `Max`.
2. **Ware widening (consequential).** `PersonalCount` 40 → 96 changes the ledger layout (F1.17 pattern; hash keeps old order). Default: yes; confirm at the foundation commit.
3. **Sea hexes.** The default is a radius-4 ring of sea around the existing 30-hex island and shore nodes for fishing and seaweed. Alternatives (interior lakes, a larger board) are a deal-table change only (`UnifiedPool`).
4. **Level-1 hut as a separate row.** §5.5 keeps a plain `Hut` row (shelter only) beside the six collection shelters because a family needs a place to sleep near a settlement not always next to a resource. If the owner prefers that *only* collection buildings give shelter, the `Hut` row is removed and the planner's `Shelter` goal always resolves to a collection building or the communal hall. Default: disable standalone `Hut` in normal play; retain legacy row for regression.
5. **Ownership of collected goods.** Default: the collector owns what it collects; the building owner owns nothing in the bin unless terms say so. This favours the individual and makes employment a negotiation. Configurable per right (`Terms`).
6. **Forage on desert water.** Default: define thirst now; activate only with accessible potable water and validated relief; cactus fruit remains food unless a hydration effect is explicitly defined.
7. **Initial tribe geography.** The six start corners are the standard six-player corners; the tribes therefore start with different neighbouring hex mixes (fairness is the dealt board's, not tuned). Default: one deal, seeds vary it; a fairness check (every tribe within 2 hexes of at least four of the seven hex types) is a setup test with a re-deal if it fails.
