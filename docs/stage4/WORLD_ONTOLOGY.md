# World ontology: space, perception, goods and the physical actions

The source of this document is owner decision D3 §9 ([DEV_LOG.md](DEV_LOG.md#owner-decisions-d3-26-nov-2026-after-m3b)). It fixes the vocabulary for **where things are, who can reach them, who can see or hear them, who owns them, who holds them and who may use them**, and the small set of physical actions from which every social and economic act is composed. It follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) and sits under [SHARED_CONTRACTS.md](SHARED_CONTRACTS.md): where the two differ, the contracts win until the amendments in section 9 are applied.

Written against `unity/poc` at `77fe38e` (M3 + M3b). "M3 code" below means that tree. F0 items are as specified in SHARED_CONTRACTS §0, not yet merged.

**The rule in one line.** Seeing or remembering a thing lets an NPC *choose* to go to it; only being within reach of it, holding it, or being handed it lets the NPC *act* on it. Every physical action is checked by the sim against true state (location, reach, possession, capacity); permission is a separate query.

---

## 1. The eight concepts

Information layers are those of [conventions §6](DESIGN_CONVENTIONS.md#6-information-layers-one-vocabulary-for-every-doc): **L1** truth (`GodState`), **L2** observed, **L3** communicated, **L6** belief.

| Concept | Precise meaning | Representation today (or "new") | Written by | Read by (truth / belief) | Example |
|---|---|---|---|---|---|
| **Physical location** | The node where a thing is. A carried or equipped lot is where its carrier is; a stored lot is where its bin is. | `Npc.Node`, `ResourceSource.Node`, `Hut.Node`, `GroundLot.Node` (`Sim/Population/PopulationState.cs`); `Hq.Node`, `NodeBuilding/NodeFlag/NodeRoad` (`Sim/Territory/TerritoryState.cs`). F0: `LotBin.Node`. Carried lots have no node field (derived from `Holder`) | `PopulationSystem.WalkTo` (NPC moves); `PopulationModule` (sources, huts, ground lots); `Goods` (bins, F0) | L1: sim systems. L2: `Sensed.Node/Dist` (`Sim/Cognition/Thinker.cs`). L6: `Belief.Subject` (`Sim/Cognition/Mind.cs`, may be stale). Own node: `SelfView.Node` (exact) | A berry bush at node 4711; an axe in Ana's carry is at Ana's node |
| **Physical accessibility** | Whether, and at what cost in ticks, an NPC can walk from its node to another: a land path exists. | `NodeGrid.IsLand` (`Sim/Territory/NodeGrid.cs`); A* in `NodePather.Find` (`Sim/Population/NodePather.cs`), step cost `StepTicks` 6 / `RoadStepTicks` 4 (`PopulationTuning`). Buildings do not block. Unreachable = `Find` returns null | Immutable grid; roads by Territory | L1: pather only. L6: candidates estimate cost from straight `Grid.Distance` × `WalkCostPerNode` (a belief: water detours are not known in advance) | A fish spot across a bay: seen (10 nodes) but 35 nodes of walking |
| **Visual perception** | What an NPC senses now: things within its sight radius, nearest first, up to a budget; then the top K attended. | `PopulationSystem.Sense` → `ThinkScratch.Seen[16]` (`Sensed`); `Thinker.Attend` → `Attended` (K = `AttendK` 3); `SightDay` 10 / `SightNight` 5, `PerceiveMax` 16. F0.7: `SeenPerson` for people | `Sense` (sim, L1 → L2); `Thinker.UpdateBeliefs` (L2 → L6) | Policies read only `NpcObservation`/`MindView` (L2, L6). Never `GodState` (G4.1) | At night Ana sees the bush 4 nodes away, not the one 7 away |
| **Communication range** | Who can perceive a gesture or display: listeners that see the speaker and attend it. | **New** (M5): `CommsRange.Gesture` = the listener's current sight radius; decoding error rises with distance ([COMMUNICATION §4.9](COMMUNICATION_AND_COGNITION_DESIGN.md)). Displays (M4 `Beg`) use the same range through `SeenPerson` | M5 `Comms.Emit` decides who perceives (truth) | L3: `DecodedMessage` in `ThinkScratch.Heard` (as believed). Speaker's intent: speaker's mind only | Bo signals `NeedFood` 8 nodes from Ana by day: Ana may decode it; at night (sight 5) she cannot |
| **Physical interaction range** | The node distance within which the sim lets an action touch its target. | `PopulationSystem.ReachOf` (private): lot 1, `Gather` 2, else 1; `Sleep`, `Explore`, `Flee` walk to distance 0. F0: `Goods.HandOverReach = 1`. Named constants are **new** (section 2) | Checked only by the sim in `Act` (`WalkTo` returns true within reach) | L1 only. Minds see `Dist` and decide whether to walk | Ana must stand within 1 node of a lot to pick it up |
| **Ownership** | Who is entitled to a lot (or, later, an asset). Independent of who holds it. | F0: `ItemLot.Owner` (`Owner`, `OwnerKind`) in `Sim/Population/Goods.cs`. M3: none (holder = owner). Huts: no owner; `Hut.FoundedBy` is history | Only `Goods` (`Produce`, `Move` with an owner rule, `SetOwner`) | L1: sim, `Goods`, telemetry. L6: own carry and own household's goods are known to the owner (`ObjState.Mine`); others' ownership is a belief from seeing use (ECONOMY "Property", M6/M8) | A household-owned fish carried by Bo: owner `Household(3)`, holder `Carry(Bo)` |
| **Possession** | Which holder physically has a lot now: a carry, the ground, or a bin. | `Npc.Carry` (`ItemLot[CarrySlots]`), `PopulationState.Lots` (`GroundLot`). F0: `Holder` (`Carry`, `Ground`, `Bin`), `LotBin.Lots` | Only `Goods` (F0.1 static test) | L1: sim. L6: own carry exact (`SelfView.Carry*`, F0 `ThinkScratch.Carry`); seen ground lots (`Sensed.Ref`); others' carry: **new** visible mask (amendment A6); store contents as last seen (**new** belief, M4) | Ana holds 2 berries she owns; the estate bin holds Cy's axe, owned by `Estate(Cy)` |
| **Permission** | Whether the owner's or the community's rules allow an individual to perform an action on an object. A **query**, separate from the physical check. | Today: `Hut.Rights` / `Hut.HasRight` (sleeping right), `ResourceSource.CommonsId = 0` (open access), ground lots ownerless. M4: `StoreAccess.MayTake`, `HutRights.Grant/Revoke`. **New:** `Permits` (section 5) | Owners and institutions: `HutRights` (M4), household norms (M4), commons norms (M8), laws (M6/M12) | Two forms: `Permits.Believed(view, …)` for affordances (L6); `Permits.Truth(s, …)` only for telemetry and invariants (`TheftCommitted` at M8). In M4/M5 they agree for every case that exists | Ana may withdraw from her household's store; a guest may sleep in a hut only with a right |

**Consequences stated once:**
- Ownership never moves goods and possession never changes ownership. Each change is its own `Goods` call and its own event (`ItemMoved`, `OwnershipChanged`).
- Theft (M8) is a possession change without an ownership change and without permission. Its consequences come from who saw it (perception), what they believe (ownership beliefs), norms and enforcement. The `Take` itself is the same physical action as a lawful pick-up.
- A use right (sleep, store, later work) is a permission, not ownership ([ECONOMY "Property"](ECONOMY_DESIGN.md#property)).

---

## 2. Spatial layers and ranges

| Layer | Unit | Used for | Not used for |
|---|---|---|---|
| **Hex** (`BoardTopology`, `Board.HexTerrain`) | 60 m edge = 12 nodes; about 21 nodes flat to flat, 24 corner to corner | Geography (terrain, `PlaceKind`), ecology (sources placed per hex, `ResourceSource.Hex`, `Rng` key `ecology(hex)`), dice piles (`TerritoryState.Piles[hex]`, touched at the hex centre node), faction territory (HQ build circles, `NodeGrid.InCircle`, buildings, roads), statistics and later institutions' jurisdiction | Location of any individual thing, reach, perception, personal ownership. NPC claims (M8 plots, commons) attach to **nodes, sources or buildings**, never whole hexes (decision O12) |
| **Node** (`NodeGrid`, triangular `Lattice`, 5 m) | 1 node = 5 m; walking 6 ticks off road, 4 on road; 1 day = 1200 ticks; an NPC thinks every 10 ticks | Location of everything physical, movement (`NodePather`), every range below | Territory |

Distances are `NodeGrid.Distance` (lattice steps). A range "r" means `Distance ≤ r`.

| Named constant (proposed home) | Value | Ticks to cross (off road) | In code today | Status |
|---|---|---|---|---|
| `Reach.Enter` (`Sim/Population/Reach.cs`) | 0 | 0 | literal `0` in `Act` (`Sleep` in a hut, `Explore`, `Flee`) | **new name**, same value |
| `Reach.Touch`: take, drop target, eat or forage at a source, build, inspect | 1 | 6 | `ReachOf` → 1 (`PopulationSystem.cs:814`) | **new name**, same value |
| `Reach.Gather`: logs from a wood stand or a hex pile | 2 | 12 | `ReachOf` → 2 for `Verb.Gather` | **new name**, kept at 2 for hash parity (O1) |
| `Reach.HandOver`: give, feed, exchange step (both NPCs) | 1 | 6 | F0: `Goods.HandOverReach = 1`; ECONOMY 4.5 "adjacent only, 10 ticks" | F0 (alias it) |
| `Reach.Store`: deposit or withdraw at a bin, from the bin's node | 1 | 6 | — | **new** (M4) |
| `SightDay` / `SightNight` (`PopulationTuning`) | 10 / 5 | 60 / 30 | yes | exists. Hidden cap: `Sense` scans 3×3 cells of `CellMm` 60 m, so any sight above 12 nodes is silently truncated (gap G7) |
| `CommsRange.Gesture` (`SimTuning.Comms`) | = current sight (10 / 5) | — | — | **new** (M5) |
| `CommsRange.Hear` (loud events: fights, alarms) | 2 × sight (20 / 10) | — | — | **new** (M7; needs the cell size raised) |
| `WitnessRange` (M8 theft) | = current sight, plus attention | — | — | **new** (M8, ECONOMY D1) |
| `AttendK`, `PerceiveMax` | 3, 16 | — | yes | exists |
| `RecallNodes` (remembered places offered as candidates) | 40 | 240 | yes | exists. A memory produces a **MoveTo-then-act** candidate, never an act |
| `HomeRadius` ("at home" feature) | 3 | 18 | yes | exists (a feature, not a reach) |
| `HutSpacing`, `ExploreNodes`, `EcologyTuning.NeighbourNodes` | 3, 9, 30 | — | yes | exist (placement, search, ecology) |

---

## 3. The physical action set

Preconditions are checked in this order: **perception or belief** (the target is a candidate, M3.12), **location** (walk until within reach), **possession**, **capacity**, **skill or tool**. **Permission** is never checked by `Goods` or by the physical check: in M4/M5 it is a query in the candidate builder, repeated by the actor on arrival (decision O2). Effects go only through the [SHARED_CONTRACTS §2](SHARED_CONTRACTS.md#2-inventories-and-goods-movement) `Goods` API.

| Action | Preconditions | Effect (`Goods` call) | Events | M3 verb today | Lands in |
|---|---|---|---|---|---|
| **MoveTo** (node) | target is land and a path exists; not asleep or busy | `Npc.Node` along `Npc.Path` (no goods) | none per step (`NpcDay.DayWalkTicks`); the purpose action's events at arrival | inside every verb via `WalkTo`; standalone as `Explore`, `Flee` | M3 (exists) |
| **Take** (ground lot) | within `Reach.Touch` of the lot's node; room (`PopulationModule.Room`); permission: ownerless (always so in M4/M5) | `Move(Ground → Carry(a), PickUp, ToReceiver)`; M4 `Claim` for an ownerless estate asset | `ItemMoved(PickUp)`, `OwnershipChanged(FirstArrival)` | `PickUp` (15), `Equip` (19) for an axe | M3 → F0 (rewritten onto `Goods`); M8 owned lots (theft, `OwnerRule.Keep`) |
| **Drop** | holds the lot; none other | `Move(Carry(a) → Ground(node), Drop)`, owner becomes `None` (M4/M5 invariant C4) | `ItemMoved(Drop)`, `OwnershipChanged` if it had an owner | no intent; set-down of tools at sleep (`StartSleep`); `DeathSpill` in `Die` | F0 (sites); standalone intent M6 (deliveries); M8 owner kept (O7) |
| **Give** (to a person) | giver holds the lot; receiver alive, within `Reach.HandOver`, has room; giver perceives receiver; permission: giver owns it, or its household's goods under `StoreAccess` | `Move(Carry(g) → Carry(r), HandOver, ToReceiver)`; household-owned food to a member: `Keep`; loan (M7): `Keep` | `ItemMoved(HandOver)`, `OwnershipChanged(Gift / Exchanged)` if the owner changed, then the purpose event (`ChildFed`, `GiftGiven`, `ExchangeStep`), `SocialHooks.OnHandOver` | none | M4 (`Feed`), M5 (`Give`, exchange) |
| **Store** | holds the lot; within `Reach.Store` of the bin; bin room (`WeightCap`); permission: member of the owning household | `Move(Carry(m) → Bin(store), Deposit, ToBinOwner)` | `ItemMoved(Deposit)`, `OwnershipChanged(Deposited)` | `Verb.Store` (17) reserved, no row | M4 |
| **Withdraw** | within `Reach.Store`; lot in the bin; room; permission: `StoreAccess.MayTake` (query) | `Move(Bin → Carry(m), Withdraw, Keep)` | `ItemMoved(Withdraw)` | `Verb.TakeFromStore` (18) reserved | M4; M8 without permission (theft) |
| **Consume** | holds the lot (eat from carry), or within `Reach.Touch` of a source (eat in place); hunger above `SatedBelow`; for building: holds a log, within `Reach.Touch` of the site | `Consume(Carry(a), lot, n, Eaten / Built / Wear)`; eat in place: `Produced` + `Consumed` with `Lot = 0` (O10) | `Consumed` v2, `Ate`; `Produced`+`Foraged` for eat in place | `Eat` (Lot: busy `EatTicks` 15; Node: busy `ForageTicks`); log into `Build`; axe `Wear` | M3 → F0 |
| **Equip** | holds the tool (or takes it: Take then Equip) | **none in M4/M5**: a tool in the carry is equipped (`AxeSlot`). From M9: `Npc.Equipped` set, no `Goods` move | none (M9: `Equipped`) | `Equip` (19) = pick up an axe | M3 (implicit); explicit M9 (O6) |
| **Use** (an object, not consumed) | within reach of it; permission: right or ownership; tool or skill where the use needs it | no `Goods` call except wear (`Consume(Wear)` at 0 uses) | `Slept`, `Produced(WithTool)`, `HutBuilt` | `Sleep` in a hut (reach 0, `Hut.HasRight`), `Build` (work), axe during `Gather` | M3 (exists); M6 work at a bench under a `Work` right |
| **Gather** (from a source) | within `Reach.Touch` (forage) or `Reach.Gather` (logs); stock > 0; room; tool optional (axe speeds logs) | `Produce(Carry(a), ware, n, owner Npc(a), Foraged / HandGathered)` | `Produced` v2, `Foraged` | `ForageAt` (10), `Gather` (9) | M3 → F0; M8 claimed sources (permission) |
| **Inspect** | within `Reach.Touch`; perceives the target | none: updates the inspector's beliefs (true quality, exact count, owner tag where visible) | `Inspected` (Full only) | none (sensing gives class and stock band at sight range) | M8 (item quality and fakes, ECONOMY 4.1) |

---

## 4. Goods states

| State | Holder (F0) | Location | Owner rule | Merge | Split |
|---|---|---|---|---|---|
| Ground | `Ground(node)` | the node | always `None` in M4/M5 | same node, ware, spoil day (tools never) | on a partial Take: moved part gets a new id |
| Store (bin) | `Bin(id)` | `LotBin.Node` (household store: the hut node; estate bin: the death node) | `HouseholdStore`: household; `Estate`: estate | `HouseholdStore`: same ware, spoil day, owner; `Estate`: never | partial Withdraw |
| Inventory (carry) | `Carry(npc)` | carrier's node | anyone's except estate | same ware and owner (tools never); lower id kept, earlier spoil day | partial Give, Drop, Store |
| Equipped | `Carry(npc)` (+ `Npc.Equipped` from M9) | carrier's node | as carry | never (tools) | never |
| In transit | not a state: a carried lot while its carrier walks (`Carry`). A reserved haul (M6) is a carry plus a reservation | carrier's node | as carry | — | — |

```
                Gather (Produce)                 Consume (Eaten, Built, Wear, Spoiled)
  source ──────────────────────► CARRY ─────────────────────────────────────► gone
                          Take ▲   │ Drop                 ▲ Spoiled
                               │   ▼                      │
                              GROUND ─────────────────────┘
  CARRY(a) ── Give ──► CARRY(b)          CARRY ── Store ──► BIN ── Withdraw ──► CARRY
  CARRY ── Equip/Unequip (M9: flag only) ── EQUIPPED
  CARRY(dead) ── EstateIn ──► BIN(estate) ── EstateOut / SetOwner ──► heir (section 6.5)
```

Split and merge follow SHARED_CONTRACTS §2.2: a partial move splits (new id for the moved part, `ParentLot` set); arrival may merge (`IntoLot` = surviving lower id). A merge never crosses owners, so a theft (M8, owner kept) never merges into the thief's own lot, and `GoodsTrace` can follow it.

---

## 5. Affordances

```csharp
// Sim/Cognition/Affordances.cs (pure; reads only ThinkScratch: never GodState, gate G4.1)
[Flags] public enum Blocked : ushort {
    None = 0, Far = 1, Unreachable = 2, NotHeld = 4, NoRoom = 8, NoPermission = 16,
    NoTool = 32, NoSkill = 64, Empty = 128, Busy = 256, Stage = 512, NotPerceived = 1024 }
public struct Affordance { public byte Verb; public Blocked Why; public int Dist; }
public static int Of(ThinkScratch sc, in ObjectRef o, Span<Affordance> into);  // o = a Sensed, a Belief, a SeenPerson, an own lot or own bin
```

| Rule | |
|---|---|
| Built only from the observer's view | `Self` (own body, own carry, own household, own rights), `Seen`, `People`, `Beliefs`, `Heard`. A stale belief can give a wrong affordance; the act then fails on arrival (frustration, `NoteEmpty`), as M3 does today. |
| `Far` is not a block | It means "MoveTo first". The candidate carries `Dist` and its walk cost, exactly as M3's candidates do. |
| `NoPermission` uses `Permits.Believed` | M4/M5: no candidate is built when it is set (so no theft before M8). M8: the candidate is built, and the policy weighs it (norms, witnesses in sight, learned outcomes). |
| Policy use | A candidate exists for each affordance with `Why ∈ {None, Far}`. The others are kept for the inspector ("why not": M3.28's breakdown) and as features later. |
| Where it sits | Inside `Thinker.BuildCandidates`. M3's `OfferSensed`/`OfferBeliefs` already encode the M3 affordances as inline conditions (`!s.CarryFull`, `s.CanCarryLog`, `!s.CarryTool && s.CanCarryTool`, `State == Mine && CanSleep`). They stay unchanged. M4's and M5's `AddCandidates` hooks use `Affordances.Of` from their first line, so new verbs are affordance-driven from the start. M3's offers move onto it only with an equivalence test (same candidates on the `band_6x20` hash runs), not before M6. |

---

## 6. Composition examples

Each line is one physical action by one actor on its own think tick. "→" lists the events.

### 6.1 Share food with a child (M4, I-BS.1)
| # | Actor | Action | Events |
|---|---|---|---|
| 1 | child C | `Beg` display (perceived by those who see C) | `BeggingMade` |
| 2 | parent P | MoveTo bush; Gather 2 berries | `Produced(Foraged, lot 81, owner P)`, `Foraged` |
| 3 | P | MoveTo C (C in `People`), until `Reach.HandOver` | — |
| 4 | P | Give lot 81 (1 of 2) to C | `ItemMoved(HandOver, lot 82 split from 81)`, `OwnershipChanged(Gift)`, `ChildFed(lot 82)`, `OnHandOver(purpose Care)` |
| 5 | C | Consume lot 82 | `Consumed(Eaten, lot 82, actor C)`, `Ate` |

### 6.2 Barter (M5, I-BS.5)
| # | Actor | Action | Events |
|---|---|---|---|
| 1 | A | sees B (within sight, attends); gesture `Offer {want fish 1, give berries 3}` | `GestureMade(Deal 7)` |
| 2 | B | decodes at its next think; decides `Accept` | `MessageDecoded`, `ResponseChosen`, `GestureMade(Accept)` |
| 3 | A, B | MoveTo each other until `Reach.HandOver` | — |
| 4 | A | Give 3 berries | `ItemMoved(HandOver)`, `OwnershipChanged(Exchanged)`, `ExchangeStep(7, 1)` |
| 5 | B | Give 1 fish (or walks away: `Defected`) | `ItemMoved`, `OwnershipChanged`, `ExchangeStep(7, 2)`, `ExchangeClosed(Completed)` |

### 6.3 A gift (M5)
A sees B; optional `Say(Give)`; MoveTo; Give → `ItemMoved(HandOver)`, `OwnershipChanged(Gift)`, `GiftGiven(DealId)`, `OnHandOver`. B's social memory updates (`TrustChanged`).

### 6.4 Storing in a household store (M4, I-BS.2)
| # | Actor | Action | Events |
|---|---|---|---|
| 1 | M (member) | Gather roots | `Produced(owner M)` |
| 2 | M | MoveTo hut node until `Reach.Store`; Store | `ItemMoved(Deposit)`, `OwnershipChanged(Deposited → Household)` (goods reward 0) |
| 3 | N (another member) | MoveTo; `Permits.Believed(Withdraw)` true; Withdraw | `ItemMoved(Withdraw)`, owner stays `Household` |
| 4 | N | Consume | `Consumed(Eaten, actor N)` |

### 6.5 Estate transfer (M4, I-BS.7)
| # | Actor | Action | Events |
|---|---|---|---|
| 1 | sim | `Kill(D)` at node n | `NpcDied` |
| 2 | sim (`OnDeath`) | `NewBin(Estate, Estate(D), node n)`; each carry lot `Move(Carry(D) → Bin, EstateIn, Set(Estate(D)), Never)` | `EstateOpened`, `ItemMoved(EstateIn)`, `OwnershipChanged(→ Estate)` |
| 3 | sim (`EstateSystem`) | by custom: `SetOwner` in place to the heir | `OwnershipChanged(Inherited)`, `EstateTransfer(lot)` |
| 4 | heir H | MoveTo n until `Reach.Store`; Withdraw own lots | `ItemMoved(Withdraw / EstateOut)` |
| 5 | sim | lots not collected by the deadline: see decision O4 | `EstateSettled` |

### 6.6 Theft (M8)
| # | Actor | Action | Events |
|---|---|---|---|
| 1 | T | sees the store of household H; affordance Withdraw has `NoPermission`, but the candidate exists (M8) and wins | `DecisionMade` |
| 2 | T | MoveTo; Withdraw 2 fish, `OwnerRule.Keep` (owner stays H) | `ItemMoved(Withdraw, OwnerBefore = OwnerAfter = H)` |
| 3 | sim | classifies with `Permits.Truth`: possession moved, no permission | `TheftCommitted(T, H, fish, 2, witnesses)` (truth, analytics) |
| 4 | each W within `WitnessRange` who attended | observation (taker as recognised, ware) | `TheftObserved` (belief) |
| 5 | W, H | may `Accuse`, `Warn`, gossip, contest, or do nothing | `GestureMade`, `TrustChanged`, `ContestStarted` |

### 6.7 Borrowing a tool (M7)
B `Say(Request, axe, later)` → A decides; A Give axe with `OwnerRule.Keep` (possession to B, owner A: no `OwnershipChanged`, `OnHandOver` with `OwnerChanged = false`) → B Use (Gather with the axe; `Uses` fall) → B Give back (`Keep`) → A's promise memory closes (`kept`). Keeping it is a conversion that M8 classifies as theft.

---

## 7. Milestone mapping

| Milestone | Concepts | Actions |
|---|---|---|
| M3 (done) | location, accessibility, sight, attention, beliefs, reach (unnamed), sleeping rights | MoveTo, Take (`PickUp`, `Equip`), Consume (`Eat`, build, wear), Use (sleep, build, axe), Gather |
| F0 | ownership (`ItemLot.Owner`), possession (`Holder`, bins), hand-over reach, seeing people | the M3 goods sites rewritten onto `Goods`; Drop (set-down and death spill) |
| M4 | permission query (`Permits`, `StoreAccess`, `HutRights`), named reaches, `Affordances`, store beliefs, displays within sight | Store, Withdraw, Give (`Feed`), Claim (Take of ownerless estate goods), estate ownership transfer |
| M5 | communication range, recognition, visible carry | Give (gift, exchange steps) |
| M6 | ownership records (state, private, communal), use rights (`Work`), ownership tags | Drop as an intent (delivery), Use of buildings, haul (carry + reservation) |
| M7 | hearing range, promises, observation of others' actions | Give with `Keep` (loans), borrowing |
| M8 | Take without permission, witnesses, line of sight, ownership beliefs, Drop keeps owner | Take/Withdraw against a claim (theft), Inspect |
| M9 | explicit equipped slot (kit) | Equip, Unequip |

### Open decisions (each with its default)

| # | Decision | Default |
|---|---|---|
| O1 | `Gather` reach 2 vs 1 | Keep 2 (F0 hash gate); revisit at M6 |
| O2 | Permission at act time in M4/M5 | The actor repeats `Permits.Believed` on arrival and abandons if it no longer holds. A **temporary mechanism** (to be listed in DEV_LOG), removed at M8 |
| O3 | `Sleep` refuses without `Hut.HasRight` in `Act` (a physical enforcement of a permission) | Keep until M8; then sleeping in another's hut is possible and classified (trespass) |
| O4 | Estate goods at a node far from the heir | Ownership passes in place (`SetOwner`); heirs collect within the 2-day window; uncollected lots go to the heir's store by **executor delivery**, a temporary cross-node move (listed in DEV_LOG, removed at M6 when hauling exists) |
| O5 | Does a Give need the receiver's acceptance? | No (one `Move` by the giver); room is the only physical check. The receiver can give back or drop |
| O6 | Explicit equipped state | Implicit until M9 (a tool in the carry is equipped, as `AxeSlot`) |
| O7 | Ownership of dropped goods | M4/M5: ownerless. M8: Drop keeps the owner; abandoned after a configurable number of days (lost property) |
| O8 | Gesture and hearing range | Gesture = sight; hearing (loud events) = 2 × sight from M7 |
| O9 | Line of sight (forest and hills block, COMMUNICATION 4.2) | Radius only until M8, where witnesses make it matter |
| O10 | Eat in place (no carry) | `Produced` + `Consumed` with `Lot = 0`, no lot id allocated, so `NextLotId` and lot episode keys stay M3-identical (F0 gate) |
| O11 | Moving M3's candidate code onto `Affordances` | Not before M6, and only with an equivalence test |
| O12 | Ownership of land | Nodes, sources and buildings; hexes keep faction territory only |

---

## 8. Gaps found in the M3 code

| # | Where | What | Severity | Handling |
|---|---|---|---|---|
| G1 | `PopulationSystem.ReachOf` (`:814`) | Reaches are private literals (1, 2, 0), inconsistent between verbs | low | name them (`Reach`), same values |
| G2 | `Act` `Sleep` (`:862`) | Permission (`HasRight`) enforced as a physical check | by design for now | O3 |
| G3 | `Act` `PickUp`/`Equip` (`:939`) | Walks to the lot's **true** node by id lookup, not the believed node. Harmless while ground lots never move; a lot merged on `Drop` loses its id, so the intent frustrates | low | M8 (when lots move or are re-dropped): target the believed node and re-sense there |
| G4 | `Sense` pile branch (`:508`) | Visible pile units subtract `Economy.PilesReserved` (a faction's reservations, institutional data) | low | accepted until M6 replaces walkers; note in DEV_LOG |
| G5 | `Equip` verb | Identical to `PickUp`; "equipped" is not a state (`AxeSlot` uses any carried axe) | medium (D3 lists equipped as a state) | O6 |
| G6 | `Eat` (Node), `FinishBusy` | Food is produced and eaten without a holder | low | O10 |
| G7 | `Sense` 3×3 cells, `CellMm` 60 m | Sight above 12 nodes is silently truncated | medium for M7 hearing | raise the cell or scan radius when a range exceeds it; add an assertion |
| G8 | `Thinker` candidate cost | Straight-line distance; the walk may be much longer (water) | none (a belief) | keep |
| G9 | SHARED_CONTRACTS §2.5 estate split | `EstateOut` moves a lot from the death-node bin to a store or carry anywhere: a move at a distance | **contract gap** | O4, amendment A2 |
| G10 | SHARED_CONTRACTS §2.2 social hook | `OnHandOver` fires only when the owner changes, so feeding with household-owned food (`Keep`) is invisible to M5, contradicting §5 | **contract gap** | amendment A3 |

Nothing in M3 acts on an object at a distance because it is seen or remembered: every intent walks first (`WalkTo` within reach) and every remembered target (`OfferBeliefs`, `RecallNodes` 40) becomes a walk-then-act candidate. Stage 3 HQ stocks, walkers and faction trade remain faction-level abstractions until M6 and are outside this document.

---

## 9. Required amendments to SHARED_CONTRACTS.md (to apply after F0 merges)

| # | Section | Amendment |
|---|---|---|
| A1 | §2.2 "Physical checks only" | Add `Sim/Population/Reach.cs` (frozen): `Enter = 0, Touch = 1, Gather = 2, HandOver = 1, Store = 1`; `Goods.HandOverReach` aliases `Reach.HandOver`; `ReachOf` uses them (behaviour-neutral). Add invariant **C6** (test-time, from events): every `ItemMoved` other than `EstateIn`, `EstateOut` and `DeathSpill` has its actor within the reach of both holders' nodes. |
| A2 | §2.5 "Estate split", §2.1 Estate row | Estate ownership passes by `SetOwner` in place; heirs collect by `Withdraw` within `Reach.Store`; a cross-node `EstateOut` is a temporary mechanism (DEV_LOG table, removed at M6). State that an estate bin's node is the death node and a household store's node is its hut's node. |
| A3 | §2.2 "Social hook" | Call `OnHandOver` for **every** `HandOver` between two living NPCs, with `OwnerChanged` in `HandOver`; M5 filters. Needed for feeding from household goods (§5) and loans (M7). |
| A4 | §2.5 "Contribute to / take from the household store", §5 "Shared store" | `StoreAccess.MayTake` is a permission **query** used by the candidate builder (`Affordances`) and repeated by the actor on arrival (O2); never called inside `Goods` or as a sim-side refusal. |
| A5 | §3 last line, §13.8 | M8 theft: an owned lot taken without permission moves with `OwnerRule.Keep`; theft is classified by the sim (`Permits.Truth`) into `TheftCommitted`, never by a `MoveReason`. From M8, C4's "ground lots are ownerless" is relaxed (O7). |
| A6 | §0.1 F0.7 `SeenPerson` | Add visible holdings: `CarryMask` (ware classes carried, as seen) and `HoldsTool`. Needed for "display an object" (D3 §11), offers "while presenting the goods" (ECONOMY 4.5) and the visible-contradiction check (M7). M5 fills it if F0 is already merged. |
| A7 | §7.1 rule 3 | Define the perceiving listener: within `CommsRange.Gesture` (= the listener's current sight radius) and attending the speaker; M4 displays use the same range. |
| A8 | §0.1 (new F0b row) | Freeze `Sim/Cognition/Affordances.cs` (`Blocked`, `Affordance`, `Permits.Believed` signature) as a shared seam; M4 and M5 fill their object kinds through their own hooks. |
| A9 | §2.2 / §2.4 | Eat in place emits `Produced` and `Consumed` with `Lot = 0` and no holder; C2 and C3 exempt lot 0 (O10). |
| A10 | §3 Ownership table | Add a **Permission** row and a **Use right** row pointing to this document (section 1); `Hut.Rights` is a use right, not ownership. |

---

## 10. Civilisation and Divinity extensions (owner D5, 26 Nov 2026)

Source: owner decisions D5.1–D5.33 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). Binding types, numbers and owners are in [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (cited as SAC); this section fixes the vocabulary. The eight concepts of section 1 and the rule in one line are unchanged and apply to gods, avatars, organisations and miracles exactly as to people: **seeing lets a body choose; only reach, holding or being handed something lets it act; permission is a query.**

### 10.1 New concepts

| Concept | Precise meaning | Representation (SAC section) | Relation to the eight concepts |
|---|---|---|---|
| **God** | A persistent divine identity, one per initial faction, that can never be destroyed and owns nothing (D5.3–D5.5). Not a body, not at any node, not an owner, not an authority over any organisation | `GodRecord` in `DivinityState.Gods`, `GodId = faction + 1` (SAC 2) | No location, no possession, no ownership. Acts only through validated commands. Its knowledge is divine observation (layer 8), never an NPC's or an institution's |
| **Avatar** | A god's physical body in the world: a body with a node, a carry, health and reach, but no hunger, fatigue, ageing or ownership (D5.9) | an `Npc` record with `BodyKind.Avatar`, id `0x4000_0000 + GodId`; present when `Alive` (SAC 4.2) | Has physical location, accessibility, interaction range and possession exactly as a person; is seen by others (visual perception) and can be approached, given to and attacked; never an owner (its pick-ups keep the owner). Destroyed, not killed: the god remains (D5.14) |
| **Control authority** | Who chooses the next intent of a body: the body's own mind (Self), a god's compulsion, a god's possession, or (for an avatar) its god | `ControlEntry` in `DivinityState.Control`, one per controlled body, precedence AvatarBody > Possessed > Compelled > Self (SAC 11) | Changes **who decides**, never what is physically possible: the chosen intent is an ordinary action primitive, walked to and checked for reach, room and possession. The body's needs, injuries, perception and learning continue |
| **Divine instruction** | A suggestion or command the god sends to a person; it reaches the mind only by perception and decoding, with possible misreading, and is weighed by the person's own policy (D5.6) | `Instruction` record; `DecodedDivine` in the recipient's `DivineMind` (layer 3) (SAC 10.3) | Communication range does not apply (it is a vision, not a gesture), but perception and decoding do. It never moves goods, creates obligations or forces an action |
| **Miracle** | A validated supernatural change of truth: health, injury, illness, a resource's stock, a building, or goods created or destroyed in place (D5.11) | `CastMiracle`, `Resurrect`; `MiracleEffectApplied`; created and destroyed goods via `Goods.Produce/Consume` with reason `Miracle` and a `ResourceCreated`/`ResourceDestroyed` ledger row (SAC 7.2, 8) | Goods appear or vanish **at the target's holder** (its node, a body's carry, a bin): never a remote transfer of existing goods. C1 holds; ledger invariant C7. A miracle that only plays an effect is forbidden |
| **Consecrated place** | A location a god has marked; a place, not a holder or owner | `ConsecratedPlace` (node, radius), sensed as `ObjClass.Shrine` (SAC 6) | Perceived within sight like any object; its meaning to a person is that person's belief |
| **Organisation** | A persistent entity of NPC members with roles, a charter, governance, records, and **stores and assets it owns** (state, private, cooperative, communal, religious: D5.16, D5.17) | `Org` in `OrgState`, `OrgId`; owns through `OwnerKind.Org`; stores are `LotBin`s of kind `OrgStore` at their node (SAC 1, 7.1) | An owner, never a body: it has no location of its own (its buildings and bins do), perceives nothing and acts only through its members' physical actions. Permission to use its goods is the members' believed role (`Permits.Believed` with an org basis) |
| **Archived person** | A dead person whose mind is kept, dormant, for resurrection | `ArchivedLife` in `DivinityState.Archive` (SAC 4.4) | Not processed per tick. Resurrection restores the same record and NpcId at a node; estates, partnerships, households and contracts are not undone (D5.13) |

### 10.2 Commandment, law and norm (three different records)

| | What it is | Who holds it | How it affects behaviour | Example |
|---|---|---|---|---|
| **Commandment** | A god's structured rule (modality, action, object, applies-to, condition, scope, importance, revisions) | the god's record (`Commandment`, D6); each person holds their **own interpretation** with an adoption level (`KnownCommandment`) | only through the person's own score (the divine term) and learning; **never** an enforced restriction (brief §12) | "Share surplus food with hungry children" |
| **Law** | An organisation's adopted rule with its own enforcement (a court, a fine, detention) | the organisation's records (C9/C10); adopting a commandment as law copies its rules at a revision | through officials' decisions and their physical acts (accuse, detain), which are themselves ordinary actions | a government adopts "do not steal" with a fine |
| **Norm** | A learned or cultural expectation of what people around one do and approve | each person's mind and culture records (C10, M8) | through learning and social consequences | children are fed first in this village |

A theft is the same physical take in all three cases; whether it breaks a commandment, a law or a norm is a classification (by truth for analytics, by belief for minds), not a different action.

### 10.3 Authority stays separate (D5.1, D5.2)

Divine power, religious authority (a religious organisation's offices), political authority (a government organisation's offices), economic ownership and institutional authority are distinct. A god can only perceive (by its observation profile), instruct, compel, possess, manifest, reveal commandments and work miracles. It cannot issue an organisation's decision, move its stores or appoint its officers, except by acting on the individuals who hold those powers.

### 10.4 Visual presentation rule (D5.18–D5.20)

- The hex regions and the node lattice remain the **authoritative** geography (section 2) and are **hidden** in normal play; they are drawn only in developer overlays.
- Rendering may be seamless and organic, but it **must match accessibility and resources**: water, cliffs and anything drawn as impassable correspond to non-land or unreachable nodes; every drawn harvestable thing (bush, tree, fish shoal, store, building, shrine) corresponds to a sim object at that node, and every sim object that a person could perceive is drawn; roads are drawn where `NodeRoad` is set. Decoration never implies an affordance the sim lacks, and never hides one it has.
- Movement, reach and perception are never changed to fit the art: if the drawn world and the lattice disagree, the drawing is wrong.
- What the camera shows, where it is, and whether anything is rendered never influence the sim (SAC 18.1).
