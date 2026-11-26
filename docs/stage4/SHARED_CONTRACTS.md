# Shared contracts for M4 and M5 (binding)

M4 (life cycle, households, kinship, `Genome`, basic estates, care) and M5 (social memory, gestures, honest barter, gifts) are built in parallel from `unity/poc` at `d3aa9fb` (M3 merged), in separate worktrees. This document fixes every interface the two touch, so that both merge mechanically and the I-BS gate ([ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md#i-bs-integration-of-biology-and-society-owner-d2-4-gate-after-m4-and-m5-are-merged)) runs on the merged tree without rework.

**Sequencing (lead's amendment, 26 Nov 2026).** This draft was written against `d3aa9fb`. Two branches were still open on top of it: `unity/m3b`, which finishes M3, and `unity/m3-wh`, which adds the M3 warehouse tables. Both land before F0. F0 is then rebuilt on their result:
- **Warehouse:** F0.14 extends the tables that `unity/m3-wh` created (`dim_person`, `fact_death`, `fact_person_day`, `fact_production`, `fact_learning`) instead of creating them again. Its migration number follows that branch's.
- **Episode kinds:** M3b's credit mechanism for food spoilage takes an episode-key kind from the M3 block. If it is keyed by lot, kind 0 stays "hut" and the lot gets kind 16, the first free kind after M5's block. The M4 and M5 blocks don't change.
- **Events:** M3b's `DecisionMade` event and its `fact_decision` table stay in the M3 code range.
- **Gate:** the F0 gate's reference hashes are taken at the post-M3b head, not at `d3aa9fb`.

**F0 as built (branch `unity/f0`, on `cde7e10`).** The details F0 fixed are written into the sections below; departures from this draft are rows 12.15 to 12.30. F0 also folds in the owner's D3 decisions (the decision trace equals the intents started; reach, possession, ownership and permission kept apart) and the accepted WORLD_ONTOLOGY amendments A1 to A10 (marked "A*n*" where they apply).

**Precedence.** For anything that crosses the M4/M5 boundary, this document wins over the design documents. Where it departs from a design document it says so (section 12), and the milestone that implements the item updates that owning document in the same commit ([DEV_LOG.md](DEV_LOG.md) rule). Inside its own boundary each milestone follows its own design documents.

**Words.** *Must* and *never* are binding. "M3 code" means the merged tree at `d3aa9fb`. "Truth" is layer 1 (`GodState`); "belief" is what a mind holds ([conventions §6](DESIGN_CONVENTIONS.md#6-information-layers-one-vocabulary-for-every-doc)).

---

## 0. The F0 scaffolding commit (lead, before either branch starts)

Both milestones need the same seams in M3 files (goods movement, lot owners, seeing people, candidate and score hooks, catalogue and hash plumbing). If each added them, every seam would conflict. So the lead lands **one behaviour-neutral commit, F0**, on `unity/poc`; `unity/m4` and `unity/m5` branch from it. After F0, each stub file below has exactly **one owner**, and the frozen files change only by a joint amendment to this document.

### 0.1 What F0 contains

| # | Item | Files (new unless marked "edit") | Owner after F0 |
|---|---|---|---|
| F0.1 | `Owner`, `OwnerKind`, `ItemLot.Owner`; `Holder`, `LotBin`, `PopulationState.Bins`/`NextBinId`; the goods API `Goods` (section 2) and `GoodsWorth.Of` (moved unchanged from `PopulationSystem.Worth`) | `Sim/Population/Goods.cs`, `Sim/Population/LotBin.cs`; edit `PopulationState.cs` (`ItemLot`, bins, counters) | frozen |
| F0.2 | M3 goods sites rewritten onto the API with identical effects: forage, eat, gather, pick up, build, wear, spoil, sleep drop, death drop, `PlaceLot` | edit `PopulationSystem.cs`, `PopulationModule.cs` | M3 code (see 10.2) |
| F0.3 | `Produced`, `Consumed` v2 (add `Lot`, `OwnerKind`, `OwnerId`); new `ItemMoved` (205), `OwnershipChanged` (206); `ProduceReason`/`ConsumeReason` widened to 8 slots each | edit `PopulationEvents.cs`, `PopulationState.cs` | frozen |
| F0.4 | `LifeStage` enum (`Adult = 0`, `Child = 1`, `Elder = 2`), `Npc.Stage`, `Npc.Household` (int, 0 = none). M3 NPCs stay `Adult`, household 0 | edit `PopulationState.cs` | written only by M4 |
| F0.5 | Kin **data**: `KinRole`, `KinSource`, `KinBelief`, `KinMind` (cap 32), `KinBand`, `KinData.RoleOf/RHat/BandOf` (pure); `NpcMind.Kin` | `Sim/Population/KinData.cs`; edit `Mind.cs` | data frozen; written only by M4 |
| F0.6 | Social **slot**: `NpcMind.Social` (type `Social.SocialMind`, stub class) | edit `Mind.cs`; `Sim/Social/SocialMind.cs` (stub) | M5 |
| F0.7 | Seeing people: `SeenPerson`, `ThinkScratch.People[8]`/`PeopleCount`, a per-tick position snapshot and spatial index of living NPCs, `NpcObservation.PeopleCount/Person(i)`; `ThinkScratch.Carry` (copy of the carry), `SelfView.Stage`, `SelfView.Household` | `Sim/Cognition/People.cs`; edit `Thinker.cs`, `MindView.cs`, `PopulationSystem.cs` | frozen (fields); filling of M4/M5 fields by their owners via hooks |
| F0.8 | Messages **data**: `GestureAct`, `TopicKind`, `MsgTime`, `DecodedMessage`; `ThinkScratch.Heard[4]`/`HeardCount`; `NpcObservation.HeardCount/Heard(i)` | `Sim/Comms/Message.cs` | layout frozen; filled only by M5 |
| F0.9 | Extension dispatch (section 0.2) and the stub hook classes. Each owner's file holds two classes (12.16): the sim-side `HouseholdHooks` / `SocialHooks` (may read `GodState`) and the mind-side `HouseholdMindHooks` / `SocialMindHooks` (scratch only, G4.1) | `Sim/Cognition/Extensions.cs` (`CognitionExtensions`, `Relations`, `VerbRanges`; frozen); `Sim/Population/PopulationExtensions.cs` (sim-side dispatch; frozen); `Sim/Population/Households/HouseholdHooks.cs` (M4); `Sim/Social/SocialHooks.cs` (M5) | as listed |
| F0.10 | Id-range plumbing: `ThinkScratch.MaxCand` 16 → 24 with M3 capped at 16 (M3 unchanged); `Verb.Max` 64 → 128; `NpcVerb` generic intents and reserved blocks; `IntentTarget.Bin = 9`; `IntentRules.IsCivilian` covers 100–199; `Activity.Care = 11`, `Activity.Social = 12`, `Count = 13` (`NpcDay` v2) | edit `Thinker.cs`, `Verbs.cs`, `PolicyTypes.cs`, `PopulationState.cs`, `PopulationEvents.cs` | frozen |
| F0.11 | Catalogue, tables and verbs as concatenations: `EventCatalog.All = Core + HouseholdEvents.Catalog + SocialEvents.Catalog`; `VerbTable.Rows = Core + HouseholdHooks.Verbs + SocialHooks.Verbs`; `InstinctTable.Default = Core + HouseholdHooks.Instincts + SocialHooks.Instincts` | edit `DomainEvent.cs`, `Verbs.cs`, `Instincts.cs`; `Sim/Population/Households/HouseholdEvents.cs` (M4), `Sim/Social/SocialEvents.cs` (M5) | as listed |
| F0.12 | State, tuning and hash plumbing: `GodState.Households` and `GodState.Social` (partial files, stub classes); `SimTuning.Households/Genetics/Estate/Social/Comms` (stub classes, all `IOmitWhenDefault`); `AllHashPartNames` gains `"household"`, `"social"`, each appended only when its state is non-null; `NpcMind.AddTo` hashes `Kin` and `Social` only when non-null; `SaveFile.CurrentVersion = 4` | `Sim/Core/GodState.Households.cs` (M4), `Sim/Core/GodState.Social.cs` (M5); edit `SimTuning.cs`, `GodSim.cs`, `Mind.cs`, `SaveFile.cs` | stubs to their owners; the edits frozen |
| F0.13 | Module entry points called from `GodSim.Create`/`StartGame`/`Install`, `PopulationModule.AddNpc`, `Kill` (extracted from `Die`), `SimInvariants.Check`: `HouseholdHooks.StartGame/Install/NewMind/OnDeath/Check`, `SocialHooks.StartGame/Install/NewMind/OnDeath/Check` (all no-ops) | edit `GodSim.cs`, `PopulationModule.cs`, `PopulationSystem.cs`, `SimInvariants.cs` | frozen call sites; bodies to owners |
| F0.14 | Warehouse: migration `0004_people_goods.sql` (after M3's `0002_population` and M3b's `0003_decision`): it **extends** `dim_person` (every M4 column, nullable), `fact_person_day` (`ticks_care`, `ticks_social`, `life_stage_code`, `household_id`) and `fact_production` (lot and owner columns), and creates `dim_party`, `fact_ownership` (one row per `OwnershipChanged`) and `fact_social_day` (empty until M4/M5). The `NpcSpawned`, `NpcDied` and `NpcDay` mappers exist since M3 (NpcDay v2 writes the two new tick columns); F0 adds the `OwnershipChanged` mapper and `MapContext.Party`. `EventMapRegistry.CreateDefault` calls `HouseholdMappings.Register(r)` and `SocialMappings.Register(r)` (stubs, each with its `Grains`); `Tables` is `partial` | `DotNet/God.Runner/Warehouse/...` | stubs to owners |
| F0.15 | People panel seams: `NpcInspection.Household` (`HouseholdCard`), `NpcInspection.Social` (`SocialCard`) filled by `HouseholdCards.Fill`/`SocialCards.Fill` stubs; `GodHud.People.cs` draws two tabs that call `DrawHouseholdTab()`/`DrawRelationsTab()` from `GodHud.People.Household.cs` (M4) and `GodHud.People.Social.cs` (M5); `NpcLayer` calls `LifeStageLayer` (M4) and `GestureLayer` (M5) stubs | `Sim/Information/...`, `Game/Hud/...`, `Game/Models/...` | stubs to owners |
| F0.16 | Test helpers: `GoodsTrace` (follow a lot id through `Produced`, `ItemMoved` splits and merges, to `Consumed`; `CheckC3`, `CheckC6`, `IdMultiset`, `Follow`) and `EventProbe` (collect every event at Full); `GoodsTests` (API, atomicity, reach, bins, permission, the static write rule) and the runner's `F0GoodsConservationTests` (C1-C6 for 30 days on `band_6x20`, with a save and load) | `Assets/God/Tests/EditMode/Support/GoodsTrace.cs`, `EventProbe.cs` | frozen |
| F0b (A1, A7, A8) | `Reach` (interaction ranges), `CommsRange.Gesture`, and the affordance seam `Affordances` / `Blocked` / `Affordance` / `Permits.Believed` (stubbed `Affordances.Of`; `Permits.Believed` is the one permission query, D3) | `Sim/Population/Reach.cs`, `Sim/Comms/CommsRange.cs`, `Sim/Cognition/Affordances.cs` | frozen |

### 0.2 The extension dispatch (frozen order: M3, then M4, then M5)

| Call site (M3 code) | Dispatch in `CognitionExtensions` / `PopulationExtensions` | M4 stub (`HouseholdHooks`) | M5 stub (`SocialHooks`) |
|---|---|---|---|
| `Thinker.Think` after `Attend` | people filled by the sim before Think; then `Recognise` each person | — | `int Recognise(ThinkScratch sc, int trueId)` → default `trueId` |
| `Thinker.Think` before `BuildCandidates` | messages | — | `void FillHeard(ThinkScratch sc)` → default none |
| `Thinker.BuildBase` end (before sort) | base features, only while `BaseCount < 12` | `void AddBase(ThinkScratch sc)` (at most 1 key) | none (M5 adds no base keys) |
| `Thinker.BuildCandidates` before costs | candidates, quota 4 each after M3's 16 | `void AddCandidates(ThinkScratch sc)` | `void AddCandidates(ThinkScratch sc)` |
| `MindView.Terms` | `Social = clamp(KinTerm, ±2000) + clamp(SocialTerm, ±2000)` | `int KinTerm(ThinkScratch sc, int cand)` → 0 | `int SocialTerm(ThinkScratch sc, int cand)` → 0 |
| `MindView.Terms` | instinct gain | `int InstinctGain(ThinkScratch sc, byte verb, int prior)` → `prior` | — |
| `MindView.Personality` default branch | by verb range | `int Personality(byte verb, int[] traits)` → 0 | same |
| `Feature.Describe` default branch | words for new values | `string Describe(uint f)` → null | same |
| relationship class of a seen person | `Relations.ClassOf(sc, person)`: M5's answer if non-zero, else the kin fallback (5.4) | — | `byte RelationClass(ThinkScratch sc, in SeenPerson p)` → 0 |
| `PopulationSystem.Think`, trace opening | episode key | `int EpisodeKey(Npc npc, in Candidate c)` → 0 | same |
| `PopulationSystem.Start` and `Act` default branches | first one that returns true handles it | `bool TryStart(...)`, `bool TryAct(GodState s, EventBuffer ev, Npc npc, int day)` → false | same |
| `Activity.Of` default branch | time-use category | `int ActivityOf(byte verb)` → `Other` | same |
| `Goods.Move` after a person-to-person ownership transfer | hand-over observed by both parties | — | `void OnHandOver(GodState s, EventBuffer ev, in HandOver h)` |
| `PopulationModule.Kill` | after `NpcDied` is emitted | `void OnDeath(GodState s, EventBuffer ev, Npc npc)` | same, after M4 |
| two claimants of one ownerless asset in one tick (M4 calls it) | contest | — | `int Contest(GodState s, EventBuffer ev, int a, int b, byte assetKind, int assetId)` → winner; stub: lower id (13.2) |

A hook in `Sim/Cognition` or `Sim/Policy` receives only `ThinkScratch` (no `GodState`, gate G4.1). `TryStart`, `TryAct`, `OnDeath`, `OnHandOver` and the module entry points are sim code and may read `GodState`.

As built (12.16): the scratch-only hooks (`Recognise`, `FillHeard`, `AddBase`, `AddCandidates`, `KinTerm`, `SocialTerm`, `InstinctGain`, `Personality`, `Describe`, `RelationClass`, and the `Verbs` / `Instincts` rows) are on `HouseholdMindHooks` / `SocialMindHooks`, dispatched by `CognitionExtensions`; the sim-side ones (`StartGame`, `Install`, `NewMind`, `OnDeath`, `Check`, `FillPerson`, `EpisodeKey`, `TryStart`, `TryAct`, `ActivityOf`, `OnHandOver`, `Contest`) are on `HouseholdHooks` / `SocialHooks`, dispatched by `Population.PopulationExtensions`. F0 adds one sim-side hook pair, `FillPerson(GodState, Npc observer, Npc seen, ref SeenPerson)`, by which each owner fills its own `SeenPerson` fields. Candidates are added with `Thinker.AddCandidate` and given target features with `Thinker.TargetFeatures` (any keys, including `Feature.NpcRel/NpcGroup/NpcRef/MsgAct`); the quota is enforced by `ThinkScratch.CandLimit`. `PopulationSystem.Started` (a read-only host hook, like `Inspect`) reports each intent as it starts, so tests can check the decision trace against the actions (D3).

### 0.3 F0 gate
- `dotnet test DotNet -c Release` and Unity EditMode green; the stage 3 pinned hashes (`PolicyRunTests`) unchanged.
- On `band_6x20`, seeds 1–3, 30 days: the `cognition` and `learning` hash parts are identical to `d3aa9fb` at every day boundary, and so are decisions, meals, deaths and the `NpcDay` drive and learning fields. Only the `population` part may change (new fields).

---

## 1. Persistent identity

All ids are `int`, start at 1, are allocated from a counter that is a public field of the owning state (so saved and hashed), are never reused (also after death, save and load), and are allocated in the deterministic processing order (ascending NPC id inside a tick). 0 always means "none".

| Id | Allocator (only path) | Counter | When | Notes |
|---|---|---|---|---|
| `NpcId` | `PopulationModule.AddNpc` (M3; M4 calls it for births and immigrants) | `PopulationState.NextNpcId` | founder at start; birth in `LifeCycleSystem` (mothers in ascending id within a day); immigrant per arrival | Warehouse natural key of `dim_person`; `person_key` is the recorder's surrogate. Each person has exactly one origin event: `NpcSpawned` (founder), `NpcBorn` or `NpcImmigrated` |
| `ItemLotId` | `PopulationModule.NewLot`, called only by `Goods.Produce` and the split in `Goods.Move` | `PopulationState.NextLotId` | produce; partial move (the moved part gets the new id, the rest keeps its id) | Ids are destroyed only by `Consume` (to 0 count) or by a merge (`ItemMoved.IntoLot`). Nothing else creates or destroys an id |
| `BinId` | `Goods.NewBin` (F0) | `PopulationState.NextBinId` | M4 creates household stores and estate bins | |
| `HouseholdId` | M4 `HouseholdModule.Form` | `HouseholdState.NextHouseholdId` | household formed | `dim_household` key |
| `PartnershipId` | M4 `Partnerships.Form` | `HouseholdState.NextPartnershipId` | partnership formed | Not the pair key of the design doc (12.3) |
| `CareArrangementId` | M4 | `HouseholdState.NextCareId` | arrangement made | |
| `EstateId` | none: the deceased's `NpcId` | — | death | One death, one estate ([MATERIAL §3](MATERIAL_INHERITANCE_DESIGN.md#3-data-structures-simestate)) |
| `MessageId` | M5 `Comms.Emit` | `SocialState.NextMessageId` | each `GestureMade` | Shared by every listener's `MessageDecoded` |
| `DealId` (exchange id) | M5, allocated by the **speaker's first `Offer`** of a new negotiation | `SocialState.NextDealId` | first offer | Carried in the message; each side remembers it in its own mind; `DealTrace` (telemetry only) is keyed by it. Gifts get a `DealId` too (kind Gift) |
| `ContestId` | M5 `Contests.Start` | `SocialState.NextContestId` | contest raised | |
| Episode key | `EpisodeKey.Make(kind, id) = (kind << 24) | (id & 0xFFFFFF)` (F0) | — | trace opening | Kind 0 = M3 hut (the raw hut id, unchanged); 1–7 M4 (1 partnership, 2 care arrangement, 3 child, 4–7 spare); 8–15 M5 (8 deal, 9 reciprocity with a person: id = that NpcId, 10 message, 11–15 spare); **64 = M3b's lot episode** (`EpisodeKey.Lot`; its existing key `1 << 30 | lotId`, 12.15); 16–63 and 65+ later. `Sim/Learning/EpisodeKey.cs` (frozen). An id ≥ 2^24 throws |

---

## 2. Inventories and goods movement

### 2.1 Holders and lots (F0, `Sim/Population/Goods.cs`)

```csharp
public enum OwnerKind : byte { None = 0, Npc = 1, Household = 2, Group = 3, Enterprise = 4, State = 5, Estate = 6 }
public readonly struct Owner { public readonly OwnerKind Kind; public readonly int Id; }   // Estate: Id = deceased NpcId; State: FactionId

public struct ItemLot { int Id; Ware Ware; int Count; int SpoilDay; int Uses; Owner Owner; }   // M3 fields + Owner

public enum HolderKind : byte { None = 0, Carry = 1, Ground = 2, Bin = 3 }
public readonly struct Holder { public readonly HolderKind Kind; public readonly int Id; }   // Carry: NpcId; Ground: node; Bin: BinId

public enum BinKind : byte { HouseholdStore = 1, Estate = 2 }      // 3+ reserved (site, granary, workshop: later)
public sealed class LotBin { public int Id; public BinKind Kind; public Owner Owner; public int Node; public int WeightCap; public List<ItemLot> Lots; } // Lots sorted by Id; WeightCap 0 = none
```

| Holder | Where it lives | Merge on arrival | Owner rule |
|---|---|---|---|
| Person carry | `Npc.Carry` (M3 slots and weight) | same ware and same owner merge (M3 rule: lower id kept, earlier spoil day); tools never | lots carried are owned by anyone (own, household's, estate's never) |
| Ground | `PopulationState.Lots` | same node, ware, spoil day (M3 rule) | **always ownerless** in M4/M5 (invariant). Dropping makes a lot ownerless |
| Household store | `LotBin` kind `HouseholdStore`, at the hut node, owner `Household(id)` | same ware, spoil day and owner | created and removed only by M4 |
| Estate | `LotBin` kind `Estate`, owner `Estate(deceased)` | **never** | created at death and emptied within 2 days by M4 |

As built: `Owner` is a plain struct with public fields (it is saved and hashed with each lot; 12.17), compared with `Same`; `Holder` is a readonly struct. A bin's node is its hut's node (household store) or the death node (estate) (A2). **Equipped** (D3) is not a holder: a tool lot in a person's carry is equipped (`Goods.IsEquipped`: an axe or gear in a carry). M3 has no flag; an explicit `Npc.Equipped` comes with M9.

### 2.6 Spatial and entitlement terms (owner D3; WORLD_ONTOLOGY 1)

Eight things are kept apart in every type and document:

| Term | In the code | Never stands in for |
|---|---|---|
| Physical location | `Npc.Node`, `GroundLot.Node`, `LotBin.Node`, `ResourceSource.Node`, `Hut.Node` (`Goods.NodeOf`) | sight, reach |
| Accessibility | a walkable path (`NodePather`); `Blocked.Unreachable` | reach |
| Visual perception | `SightDay` / `SightNight`; `Sense`, `SeePeople`; `ThinkScratch.Seen` / `People` | reach: nothing is acted on because it is seen |
| Communication range | `CommsRange.Gesture` = the listener's current sight (A7) | reach |
| Physical interaction range | `Reach` (Enter 0, Touch 1, Gather 2, HandOver 1, Store 1), enforced by `Goods` for every person-initiated `Move` and `Consume` (`MoveStatus.OutOfReach`), checked from events by C6 | permission |
| Ownership | `ItemLot.Owner`, `LotBin.Owner`; changed only by `Goods` (`OwnershipChanged`) | possession |
| Possession | the holder: `Carry`, `Ground`, `Bin` (and the equipped state of a carried tool) | ownership |
| Permission to use | `Permits.Believed(sc, owner)`: allowed with basis Own, Household or Ownerless, else not (D3, A8). A query only: `Goods` never checks it, and M3–M5 candidates never offer another party's goods | ownership, reach |

A remembered or seen target always becomes a walk-then-act candidate (`RecallNodes`, `OfferBeliefs`); the act happens only within `Reach` of it.

### 2.2 The one sanctioned API

No code outside `Goods` may add, remove, split, merge or re-own a lot, or change `NextLotId`. (Static test, added by F0: only `Goods` and `PopulationModule.NewLot` write `Npc.Carry`, `PopulationState.Lots`, `LotBin.Lots`, `ItemLot.Owner`.)

```csharp
public static MoveResult Move(GodState s, EventBuffer ev, Holder from, int lotId, int count, Holder to,
    MoveReason why, int actor, OwnerRule rule = OwnerRule.Keep, Owner set = default,
    MergeRule merge = MergeRule.Merge, int purposeRef = 0);
public static int  Produce(GodState s, EventBuffer ev, Holder to, Ware w, int count, int spoilDay, int uses,
    Owner owner, ProduceReason why, int actor, int source);          // returns the lot id; 0 = no room, nothing produced
public static bool Consume(GodState s, EventBuffer ev, Holder from, int lotId, int count, ConsumeReason why, int actor, int refId = 0);
public static bool SetOwner(GodState s, EventBuffer ev, Holder at, int lotId, Owner to, OwnershipBasis basis, int actor); // no movement
public static int  NewBin(GodState s, BinKind kind, Owner owner, int node, int weightCap);
public static void RemoveBin(GodState s, int binId);                 // throws unless empty

public enum MoveReason : byte { PickUp = 1, Drop = 2, Deposit = 3, Withdraw = 4, HandOver = 5, EstateIn = 6, EstateOut = 7, Claim = 8, DeathSpill = 9 } // 10–31 reserved
public enum OwnerRule  : byte { Keep = 0, ToReceiver = 1, ToBinOwner = 2, Set = 3 }
public enum MergeRule  : byte { Merge = 0, Never = 1 }
public enum MoveStatus : byte { Ok = 0, NoSuchLot = 1, NotEnough = 2, NoRoom = 3, BadHolder = 4, SameHolder = 5, OutOfReach = 6 }
public struct MoveResult { public MoveStatus Status; public int Lot, IntoLot, EndedLot, ParentLot; public Owner Before, After; }
```

As built (F0), additions to the signatures above (12.18): `Produce(..., bool withTool = false)` (M3's `Produced.WithTool`); `Move(..., OwnershipBasis basis = Auto)` (Auto derives the basis from the reason; M5 passes `Exchanged`); `MoveResult.EndedLot` (the id a merge ended, which may be the older lot when the moved id is lower, M3's rule); `Goods.Wear(s, ev, at, lot, actor)` (a tool's use; consumes it at 0 uses); `Goods.ProduceInHand` / `ConsumeInHand` (eating at a source: `Produced` and `Consumed` with `Lot = 0` and no holder, A9); `Goods.InReach`, `Goods.NodeOf`, `Goods.IsEquipped`, `Goods.BinById`, `Goods.CreditedLater`. `GoodsWorth.Of` is in `Goods.cs`. The founder packs and `PopulationModule.PlaceLot` are produced before the first tick with no event buffer (`ev = null`): the ledger books them, no `Produced` is emitted, and `GoodsTrace.Start` takes them from the state.

| Rule | |
|---|---|
| Atomic | On any status but `Ok` nothing changes and nothing is emitted. |
| Immediate | Executes inside the acting NPC's `Act`, in tick order. Never queued, never two-sided: an exchange is two separate `Move`s by two actors on their own think ticks ([ECONOMY 4.5](ECONOMY_DESIGN.md#45-exchange)). |
| Physical checks only | Lot present in `from`, `count ≤ Count`, room in `to` (carry: weight and a free or mergeable slot; bin: `WeightCap`), and **reach** (A1, D3): for every person-initiated move the actor must be within `Reach.Of(reason)` of both holders' nodes (its own carry is always in reach), else `OutOfReach`; `Consume` and `Wear` need `Reach.Touch` of the holder. `EstateIn`/`EstateOut` have no person reach. Consent, perception and rights (permission) are the **caller's** checks. `Goods.HandOverReach` aliases `Reach.HandOver` = 1. |
| Split | `count < Count`: the moved part gets a new id, `ParentLot` = the original. |
| Merge | If merged on arrival, `IntoLot` is the surviving (lower) id and the moved id ends. `MergeRule.Never` is required for `EstateIn` and `EstateOut`. |
| Events | `Ok` emits exactly one `ItemMoved` (with the actor's and both holders' nodes before the move, for C6); if the owner changed, also one `OwnershipChanged`. `Produce`/`Consume` emit `Produced`/`Consumed` v2 and update `GoodsLedger`. Purpose events (`ChildFed`, `GiftGiven`, `ExchangeStep`, `EstateTransfer`) are emitted by the caller **after** `Ok`, carrying `MoveResult.Lot`. |
| Social hook | Every `HandOver` from one living person's carry to another's calls `SocialHooks.OnHandOver` once (A3), with giver, receiver, ware, count, lot, `purposeRef`, the reason, both owners and `OwnerChanged`; M5 filters. This is how M5's social memory sees M4's feeding (also from household goods, owner kept) and later loans without M4 calling M5. |
| Static rule | `GoodsTests.Static_OnlyGoodsWritesLotsCarriesAndOwners` scans `Assets/God/{Sim,Game,Recording}` for writes to a carry slot or array, the ground list, a bin's lots, a ground lot, an owner, `Bins` and `NextLotId`. Allowed: `Goods.cs`; in `PopulationModule.cs` only `NewLot`'s id allocation and `AddNpc`'s empty carry; the counter's declaration; `ThinkScratch.Carry` (a copy, not a holder). |

### 2.3 The goods reward (the only goods channel into learning)

`Goods` pays it; **no other code adds a goods reward** (M3 sites move into `Goods` in F0 with identical values).

| Event | Reward to | Value |
|---|---|---|
| `Produce` with owner `Npc(a)` | a | `+GoodsWorth.Of(a, ware) × count` |
| `Consume` reason `Spoiled` of a lot owned by `Npc(a)` | a | `−worth × count` (other consume reasons pay nothing: their effect comes through drives and episodes, as in M3) |
| Owner changes from X to Y (`Move` or `SetOwner`) | `Npc(a)` = X: a loses; `Npc(b)` = Y: b gains | `∓worth × count`, **except** between an NPC and its own household (deposit, withdraw, feeding from household goods): 0 |
| `Drop`, `DeathSpill` | nobody | 0 (M3 parity: setting down tools to sleep costs nothing) |

As built (12.19): M3b changed the goods channel after this draft (realised value, `SimTuning.Learning.Goods`), so `Goods` pays exactly M3b's rules: production pays only for `Foraged` and `HandGathered` (founder packs and placed goods pay nothing), and nothing for goods `CreditedLater` (food and tools under `Realised`, which reach the deciding NPC through M3b's lot episodes: `AwaitLot`, the meal's `Accrue`, `ToolGone` stay in `PopulationSystem`); owner changes pay likewise except for goods credited later; spoilage of an NPC's lot is booked against the lot's episodes, else as a plain loss. Eating at a source pays nothing.

`GoodsWorth.Of` stays the M3 rule in both milestones. M5's subjective valuation ([ECONOMY 4.5](ECONOMY_DESIGN.md#45-exchange)) is a separate function used for exchange decisions and telemetry, never for this reward (open decision 13.6).

### 2.4 Conservation invariants (F0 extends `PopulationInvariants`)

| # | Invariant |
|---|---|
| C1 | Per ware: `Σ Produced − Σ Consumed = Σ (carry + ground + bins)`, exactly, every day boundary and after save/load. |
| C2 | Every live lot id is held by exactly one holder; `0 < Id < NextLotId`; `Count > 0`. |
| C3 | Every id that ever existed is live, consumed (`Consumed.Lot`), or merged (`ItemMoved.IntoLot ≠ Lot`). `GoodsTrace` reconstructs this from events. |
| C4 | Ground lots are ownerless; no lot is owned by `Npc(x)` with x dead for more than 3 days, nor by `Estate(x)` after its estate settled (G3.2). |
| C5 | No bin over its cap; no carry over its weight; an `Estate` bin is empty and removed by `EstateSettled`. |
| C6 | (A1) Every `ItemMoved` other than `EstateIn`, `EstateOut` and `DeathSpill` had its actor within `Reach.Of(reason)` of both holders' nodes (from the event's `ActorNode`, `FromNode`, `ToNode`). |

As built: C1, C2, C4 (ground ownerless; nothing owned by a person dead over 3 days) and C5 (bins, carries) are state checks in `PopulationInvariants` (every `SimInvariants.Check`); C3 and C6 need events and are `GoodsTrace.CheckC3` / `CheckC6`. Lot 0 (eaten in place) is exempt from C2 and C3 (A9). C4's estate clause and C5's estate clause need M4's estates and are M4's (`HouseholdHooks.Check`).

### 2.5 How each milestone uses it

| Action | Milestone | Calls |
|---|---|---|
| Parent hands food to a child or patient (`Feed`) | M4 | `Move(Carry(p), lot, n, Carry(c), HandOver, p, Keep if household-owned else ToReceiver)`, then `ChildFed`/`CareGiven` with the lot |
| Contribute to / take from the household store | M4 | `Move(Carry(m) → Bin(store), Deposit, ToBinOwner)`; `Move(Bin(store) → Carry(m), Withdraw, Keep)`, both within `Reach.Store`. `StoreAccess.MayTake` is a permission **query** used when building candidates and repeated by the actor on arrival; never a refusal inside `Goods` (A4) |
| Death | M4 (`OnDeath`) | each carry lot `Move(Carry(d) → Bin(estate), EstateIn, Set(Estate(d)), Never)`; with M4 disabled, M3's `DeathSpill` to the ground |
| Estate split | M4 | (A2) ownership passes **in place**: `SetOwner(Bin(estate), lot, heir party, Inherited or Escheat)`; heirs then collect by `Move(Bin(estate) → Carry(heir), Withdraw, Keep)` within `Reach.Store`; `Consume(Buried)` for `BuriedWithDead`; then `EstateTransfer` with `AssetId = lot`. A cross-node `EstateOut` is not part of the contract (WORLD_ONTOLOGY G9) |
| Claim an ownerless lot | M3 `PickUp`, M4 `Claim` | `Move(Ground → Carry, PickUp/Claim, ToReceiver)` |
| Gift | M5 | `Move(Carry(g) → Carry(r), HandOver, ToReceiver, purposeRef = DealId)`, then `GiftGiven` |
| Exchange hand-over | M5 | the same, then `ExchangeStep(DealId, step)` |
| Gesture | M5 | **never** calls `Goods` (section 7) |

---

## 3. Ownership

| Term | Meaning |
|---|---|
| Possession | The holder of a lot (carry, ground, bin). Physical; the sim's validations use it. |
| Ownership | `ItemLot.Owner`. Who is entitled to it. In M4/M5 it differs from possession for household goods carried by a member, goods in an estate, and a minor heir's goods held in a household store. |

| Form | `OwnerKind` | In M4/M5 | Who may change it |
|---|---|---|---|
| Personal | `Npc` | yes | the owner's own action (give, hand over, deposit); the sim at its death (to `Estate`) |
| Household | `Household` | yes (M4 store; carried by members) | any member with store access under the household's norms (M4); M4 at dissolution (`SetOwner`) |
| Estate | `Estate` | yes, ≤ 2 days | only `EstateSystem` (M4) |
| Communal group | `Group` | reserved (no M4/M5 writer; `BackToCommunity` uses `Group(faction community id = FactionId)`) | M4 estates only |
| Ownerless | `None` | every ground lot | becomes owned by the first `PickUp`/`Claim` |
| State, Enterprise | `State`, `Enterprise` | not used before M6, M8 | — |
| **Permission** (A10) | — | the query `Permits.Believed(sc, owner)`: Own, Household, Ownerless allowed; anything else not (D3) | nobody: it is derived from ownership and household membership; WORLD_ONTOLOGY 1 |
| **Use right** (A10) | — | `Hut.Rights` (sleeping) is a use right, not ownership; M6 work rights later | M3 (built huts), M4 (`HutRights.Grant/Revoke`); WORLD_ONTOLOGY 1 |

| Change | Basis (`OwnershipBasis`) | Event |
|---|---|---|
| produced | `Produced = 8` | in `Produced` (no `OwnershipChanged`) |
| gift, feeding to own | `Gift = 3` | `OwnershipChanged` + purpose event |
| exchange step | `Exchanged = 2` | `OwnershipChanged` + `ExchangeStep` |
| deposit to household | `Deposited = 9` | `OwnershipChanged` |
| death, inheritance | `Inherited = 4` (to heir), `Escheat = 12` (to none/group) | `OwnershipChanged` + `EstateTransfer` |
| first pick-up | `FirstArrival = 0` | `OwnershipChanged` |
| dissolution split | `Divided = 13` | `OwnershipChanged` |
| dropped to the ground | `Abandoned = 10` (F0) | `OwnershipChanged` |
| spilled at death (M3, no estates) | `Spilled = 11` (F0) | `OwnershipChanged` |

`OwnershipBasis` values 0–7 are the design doc's `ClaimBasis`; 8–13 are added by F0; 14–31 reserved. Theft does not exist in M4/M5: no candidate takes a lot owned by another party (M8). (A5) M8 theft moves an owned lot with `OwnerRule.Keep`; the sim classifies it (`TheftCommitted`, by its own truth-side permission check), never a `MoveReason`; from M8, C4's "ground lots are ownerless" is relaxed.

---

## 4. Relationships

### 4.1 Who owns and writes what

| Record | Owner (writes) | Lives in | Readers | Truth or belief |
|---|---|---|---|---|
| `Partnership` (id, A, B, stage, start) | M4 | `HouseholdState.Partnerships` | sim systems; minds only through their own `KinBelief(Partner)` | truth |
| Parent/child (`Mother`, `BioFather`), `PedigreeRecord` | M4 | `Npc`, `PopulationState.Pedigree` | **never** a mind, policy, `Social` or `Comms` type (G4.2) | truth |
| `KinBelief { Other, Role, Confidence, Source, Tick }` | M4 (cues: birth, raising, co-residence, told, resemblance) | `NpcMind.Kin` (F0 type) | anyone, via `KinData` | belief |
| `Household` membership | M4 | `HouseholdState`, `Npc.Household` | sim; a mind knows **its own** household and whether a seen person shares it (`SeenPerson.SameHousehold`) | truth, known to members |
| `SocialMind` (relations: familiarity, trust, affection, fear, grudge, ledger, flags, `KinCue`; obligations; log) | M5 | `NpcMind.Social` | anyone, via `SocialView` | belief |
| `CareArrangement` | M4 | `HouseholdState` | M4 | truth (terms as each side recorded them in its own copy) |

### 4.2 Reading across without owning

| Reader | Read API | Defined in | Default when the other is absent or disabled |
|---|---|---|---|
| M5 reads kin | `KinData.RoleOf(NpcMind me, int other) → KinRole`, `RHat(me, other) → 0..500`, `BandOf(me, other) → KinBand` | F0 (pure, over `me.Kin`) | `None`, 0 (empty `KinMind`) |
| M5 reads household | `ThinkScratch.Self.Household`, `SeenPerson.SameHousehold`; sim side `Npc.Household` | F0 fields | 0 / false |
| M4 reads social memory | `SocialView.Trust(me, other) → −1000..1000`, `Familiarity(me, other) → 0..1000`, `ClassOf(me, other) → NpcRelClass` | `Sim/Social/SocialView.cs` (M5 body; signatures frozen by F0) | 0, 0, `Unknown` |

Rules:
- M5 **refreshes** `Relation.KinCue = RHat/2` and the `Partner`/`HouseholdMate` flags once a day from these reads (SOCIAL 3), and never writes kin, households or partnerships.
- M4 never writes `SocialMind`. M4's decisions may use `SocialView` and must work with its defaults.
- Kin enters the decision score only through M4's `KinTerm` (Hamilton: `KinWeight trait × r̂`); trust, obligations and affection only through M5's `SocialTerm`. Neither term reads the other's data to avoid double counting the same cue.

### 4.3 `KinRole`, `KinSource`, `KinBand` (F0 values)

| Enum | Values |
|---|---|
| `KinRole` | None 0, Mother 1, Father 2, Daughter 3, Son 4, FullSibling 5, HalfSibling 6, Grandparent 7, Grandchild 8, Uncle 9, Aunt 10, Nephew 11, Niece 12, Cousin 13, Partner 14, InLaw 15, StepParent 16, StepChild 17, FosterParent 18, FosterChild 19 |
| `KinSource` | Born 1, Raised 2, Told 3, Seen 4, Resemblance 5, Partnered 6 |
| `KinBand` (warehouse `dim_relationship`) | None 0, ParentChild 1, Sibling 2, OtherKin 3, Partner 4 |
| `r̂` (`KinData.RHat`) | HOUSEHOLDS table: parent, child, full sibling 500; half sibling, grandparent, uncle, aunt, nephew, niece 250; cousin 125; partner, step, foster-known 0; × `Confidence / 1000` |

### 4.4 Relationship class (`NpcRelClass`, the `NpcRel` feature value, F0)

Unknown 0, Stranger 1, Acquaintance 2, Friend 3, Kin 4, Partner 5, HouseholdMate 6, Enemy 7, Rival 8, Debtor 9, Creditor 10, Employer 11 (M6), Employee 12 (M6), Cheat 13 (M7).

`Relations.ClassOf` (F0): M5's classifier when it returns non-zero (SOCIAL 4.3 priority, which puts Partner and HouseholdMate first and Kin fifth); otherwise the kin fallback: Partner if `RoleOf = Partner`; HouseholdMate if `SameHousehold`; Kin if `RHat ≥ 120`; else Stranger. So M4's classes survive the merge unchanged.

---

## 5. Households

| Topic | Contract |
|---|---|
| Membership | M4 only. `Npc.Household` and `Household.Members` (sorted, ≤ 12) always agree; at most one household per NPC; `HouseholdJoined`/`HouseholdLeft` pair exactly (M4.14). |
| Sleeping rights | `Hut.Rights` (M3) stays the single source of truth for who may sleep in a hut. M4 grants and revokes through `HutRights.Grant/Revoke(s, ev, hut, npc, source)` (M4) and adds `Hut.RightSource[]` (Built, Household, Invited). M5 reads rights (resident/intruder) and never writes them. |
| Shared store | One `LotBin` (`HouseholdStore`, owner `Household(id)`) per household with a hut, created and removed by M4. Access: `StoreAccess.MayTake(s, npc, bin)` (M4; norms `StoreAccess`, `ChildrenFirst`). The verbs `Store` (17) and `TakeFromStore` (18) and their candidates belong to M4. |
| Within-household transfers (M5 rules) | **Store deposit/withdraw is not an exchange or a gift**: no `DealId`, no `ExchangeStep`/`GiftGiven`, no social-ledger change. A **carry-to-carry hand-over between household mates is a gift** if made by M5's `Give` (`GiftGiven.SameHousehold = true`) and updates social memory like any gift; if made by M4's `Feed`, it is `ChildFed`/`CareGiven` and social memory sees it through `OnHandOver` (`purpose = Care`). Barter between household mates is allowed (nothing forbids it) and recorded with `SameHousehold = true`. |
| Household goods given away | A member may hand household-owned goods to an outsider only if `StoreAccess` allows it to take them; the event names the giver and `FromOwner = Household`. |
| M5 alone | No households exist (`Npc.Household = 0`); "hut-mate" means sharing a hut's sleeping rights (M3). |

---

## 6. Learning integration

The M3 core is closed: neither milestone edits `Learner`, `AssociationStore`, `LearningTuning`, `AssociativeNpcPolicy` or the trace and TD code. All integration goes through these points.

| Point | How |
|---|---|
| Rewards and punishments | `Learner.AddReward(npc.Mind.Store, vp)` at the moment the consequence happens. Gains live in each milestone's own tuning block (`SimTuning.Households.*`, `SimTuning.Social.*`), using the channel values of [NPC_LEARNING 4.4](NPC_LEARNING_DESIGN.md#44-reward-and-punishment-signals) (household accepted +500 / expelled −600, promise kept +400, ...). Goods: only through `Goods` (2.3). |
| Delayed outcomes | Mark the verb `Deferred` in its `VerbInfo`; return a namespaced key from your `EpisodeKey` hook (section 1); call `Learner.Outcome(store, tuning, traits, key, reward, day, sink)` when the outcome happens (M5: reciprocity, key `Make(9, giverId)` closed on the receiving side's later gift back; M4: partnership or child outcomes). |
| Contexts | Candidate **target** features through the M3 `Target`-style helper F0 extends with `NpcRel`, `NpcGroup`, `NpcRef`, `MsgAct`; base features only through `AddBase` (M4 ≤ 1 key, M5 none). |
| Instincts, personality | Rows in your own `Instincts` and `Personality` (dispatched by verb range). Genome instinct gains (M4) through `InstinctGain`. |
| Learning rate by age | M4 edits `PopulationSystem.TraitsOf` (`AlphaShift`: child 1, adult 2, elder 3). Newborns get an empty store (`AddNpc`); nothing learned is ever copied at birth. |
| Drives | Neither milestone adds a drive (open decision 13.4); `Drive.Count` stays 5. |

### 6.1 Id ranges

| Space | Type | M3 / core (taken) | M4 | M5 | Reserved |
|---|---|---|---|---|---|
| Learning verb `Verb` | byte (code has no 64 limit; F0 sets `Max = 128`) | 0–20, 60–62 | 32–47, plus rows for 17 `Store`, 18 `TakeFromStore` | 48–59 (non-gesture: `Give` 48, `HandOver` 49, `Withhold` 50, `Help` 51, `Greet` 52, `Ignore` 53; 54–59 M5's choice), 64–95 (`Say(act)` = 64 + `GestureAct`) | 21–31 core, 63, 96–127 M6+ |
| Intent `NpcVerb` | int | 1–6, 20–30, 40–48 | 100 `HandOver`* , 101 `Interact`*, 102 `UseStore`*, 103 `Claim`, 104 `Leave`, 105 `Reproduce`; own block 106–139 | `Gesture` (6) for every `Say`; `HandOver`*, `Interact`*; own block 140–169 | 170–199 |
| `SelfFeature` values | int | 1–8 | 16–31 | 32–47 | 48+ |
| `ObjClass` | int | 1–14, 30 | 40–59 | 60–79 | 80+ |
| `ObjCategory` | int | 1–9 | 16–23 | 24–31 | 32+ |
| `ObjState` | int | 1–7 | 16–23 | 24–31 | 32+ |
| `SituationKind` | int | 1–4 | 16–47 | 48–79 | 80+ |
| `NpcRel` | int | — | — | — | values fixed by 4.4 (F0) |
| `NpcGroup` | int | — | Elder 6, Child 7 | OwnFaction 1, OtherFaction 2, EnemyFaction 3 | 4 StateStaff, 5 Soldier (later) |
| `Role` | int | — | Child 4, Elder 5 | — | Worker 1, Soldier 2, Envoy 3 |
| `MsgAct` | int | — | — | = `GestureAct` value | |
| `PlaceKind`, `TimeOfDay` | int | as M3 | none | none | |

\* Shared generic intents defined by F0: `Detail` carries the learning verb, so a new learning verb needs no new `NpcVerb`. An own-block `NpcVerb` member is added only inside your block in `PolicyTypes.cs`, above the sentinel member `M4Reserved = 139` / `M5Reserved = 169`.

---

## 7. Communication

### 7.1 The boundary

| M5 owns (`Sim/Comms`, `Sim/Social`) | Everyone else |
|---|---|
| `GestureAct` semantics, composition, encoding, `GestureMade`, perception of gestures, decoding with misunderstanding, `MessageDecoded`, the inbox in `SocialMind`, `FillHeard`, the `Say(act)` verbs and their candidates, response candidates (accept, refuse, counter, ignore), negotiation memory per mind, `DealTrace` (telemetry only) | May **read** `sc.Heard` (decoded messages, as believed) and must work when `HeardCount == 0`. Never emits a gesture, never writes a message, never reads `GestureMade.Honest` or `MessageDecoded.Mismatch` |

**Rules (static tests added by M5 to G4):**
1. **A gesture never moves goods; only an action does.** No type in `God.Sim.Comms` references `Goods`; no message creates an obligation, a transfer or a partnership by itself.
2. **Three records stay separate:** the speaker's intention (its own mind, plus the analytics-only `Honest` flag), the receiver's belief (its `DecodedMessage`, obligations, relation updates), and the action (`ItemMoved` and its purpose event).
3. **Delivery:** a message emitted at tick t enters each perceiving listener's inbox at once, but a listener reads it only at its first think tick > t, so the result never depends on id order. A perceiving listener is within `CommsRange.Gesture` of the speaker (its own current sight radius, day or night) and attends it; M4's displays use the same range (A7).
4. **The receiver decides:** a response is a candidate scored by the receiver's own policy on its own decoded message and beliefs.

### 7.2 M4 with and without M5

| M4 behaviour | Always (M4 alone) | When M5 is present and `Social.Gestures = 1` |
|---|---|---|
| Child's hunger | `Beg` (verb, M4): a visible **display** with intensity 0–3 in `SeenPerson.Display/DisplayLevel/DisplayTarget`; not a message | The child may also `Say(NeedFood)`; M4's `Feed` candidate adds `MsgAct(NeedFood)` to its target features when a heard message from that child exists |
| Courtship, partnership | `Court` (accompany, gift via `HandOver`), `ProposePartner` display, `AcceptPartner` as the target's own decision on a perceived proposal | M5's gifts and `Cooperate` are ordinary candidates and may happen too; partnership still forms only by M4's propose/accept |
| Care requests, shared childcare | M4 displays (`AskCare`, accept) form a `CareArrangement` | unchanged in M4/M5; `TopicKind.Task` for "mind child" offers is reserved for later (open decision 13.10) |
| Food sharing in a household | `Feed`, store deposit/withdraw | unchanged; M5 `Give` between mates is a separate, additional route |

Displays are M4 state, perceived by anyone (social memory may use them as visible cues); they are never decoded, never misread and never in `GestureMade`.

### 7.3 Switches for testing each milestone alone, before and after merge

| Switch (tuning path) | Default | Effect when 0 |
|---|---|---|
| `Households.Enabled` | 1 | no ageing, births, households, kin cues, care or estates (M3 deaths spill to the ground); M4 hooks return defaults |
| `Social.Enabled` | 1 | no social memory, recognition (perfect), gestures, barter or gifts; M5 hooks return defaults |
| `Social.Gestures` | 1 | no `Say` candidates and no decoding (M5.15: barter falls to zero) |

After merge, M4's suite runs with `Social.Enabled = 0` and M5's with `Households.Enabled = 0`, and each must give the **same final hash as on its own branch tip** for its own scenarios. Then both run with both on.

---

## 8. Domain events

### 8.1 Code ranges (inside DATA_WAREHOUSE §7's module ranges; `EventCatalog.RangeOf` unchanged)

| Codes | Owner | Domain |
|---|---|---|
| 100–104 | M3 (taken) | Population |
| 105–124 | M4 life cycle | Population |
| 125–159 | M4 households, kin, care | Household |
| 160–199 | reserved (M10 migration, later) | |
| 200–204 | M3 (taken) | Production, Economy |
| 205–209 | F0 goods | Economy, Ownership |
| 210–229 | M5 exchange and gifts | Economy |
| 230–249 | M4 estates | Estate |
| 250–299 | reserved (M6 state, M8) | |
| 300–319 | M5 social memory and contests | Social, Contest |
| 320–329 | reserved (M7 promises, gossip) | |
| 330–339 | Learning (330–333 taken; 334–339 reserved) | Learning |
| 340–354 | M5 comms | Comms |
| 355–399 | reserved (Culture, M11) | |

### 8.2 Events (code, table, recording). **HV** = high volume (Summary keeps a daily aggregate plus a 1-in-`sampleEvery` sample). **Full** = written only at Full. Analytics-only truth fields are Full only. Codes in a block are allocated in the order listed; spare codes stay unused until a milestone appends.

| Code | Event | Owner | Table | Rec. |
|---|---|---|---|---|
| 205 | `ItemMoved` (Lot, IntoLot, EndedLot, ParentLot, Ware, Count, From/To kind and id, Reason, Actor, ActorNode, FromNode, ToNode, OwnerBefore/After, PurposeRef) | F0 | `fact_event` | HV |
| 206 | `OwnershipChanged` (asset kind Lot, Lot, Ware, Count, From, To, Basis, Actor) | F0 | `fact_ownership` | Summary |
| 200, 201 v2 | `Produced` (+ Lot, IntoLot, OwnerKind, OwnerId), `Consumed` (+ Lot, OwnerKind, OwnerId, RefId) | F0 | `fact_production` (+ lot and owner columns) | HV |
| 104 v2 | `NpcDay` (`Ticks` has 13 entries: + Care, Social) | F0 | `fact_person_day` (+ `ticks_care`, `ticks_social`) | Summary |
| 105 | `NpcBorn` (child, mother, believed father, bio father†, household, faction, node, genome hash) | M4 | `fact_birth`, `dim_person` | Summary |
| 106 | `NpcMatured` / stage change (npc, from, to) | M4 | `dim_person_history` | Summary |
| 107, 108 | `PregnancyStarted`, `PregnancyEnded` | M4 | `fact_event` | Summary |
| 109, 110 | `NpcImmigrated`, `NpcEmigrated` | M4 | `fact_migration`, `dim_person` | Summary |
| 111–113 | `IllnessStarted`, `IllnessEnded`, `InjuryTaken` | M4 | `fact_event` | Summary |
| 114 | `GeneMutated` | M4 | `fact_event` | Full |
| 115 | `GeneticsYearly` (snapshot) | M4 | `fact_event` | Summary |
| 125–128 | `HouseholdFormed`, `HouseholdJoined`, `HouseholdLeft`, `HouseholdDissolved` | M4 | `dim_household`, `fact_household_membership` | Summary |
| 129 | `CourtshipAct` | M4 | `fact_event` | HV |
| 130, 131 | `PartnershipFormed`, `PartnershipEnded` (PartnershipId, A, B, reason, initiator) | M4 | `fact_event` | Summary |
| 132, 133 | `TryForChildChosen`, `TryForChildStopped` | M4 | `fact_event` | Summary |
| 134 | `ChildFed` (feeder, child, lot, ware, count, source holder, feeder's `KinBand`, `TrueR`†) | M4 | `fact_social_day` (aggregate) + `fact_event` | HV |
| 135 | `BeggingMade` (child, intensity, true hunger bucket†) | M4 | `fact_event` | HV |
| 136 | `CareGiven` (carer, patient, kind, ticks, carer's `KinBand`, `TrueR`†) | M4 | `fact_social_day` (aggregate) + `fact_event` | HV |
| 137–139 | `ChildOrphaned`, `ChildFostered`, `ChildUnattendedHazard` | M4 | `fact_event` | Summary |
| 140 | `KinBeliefFormed` | M4 | `fact_event` | Full |
| 141 | `ActivityObservable` | M4 | `fact_event` | HV |
| 142, 143 | `CareArrangementMade`, `CareArrangementEnded` | M4 | `fact_event` | Summary |
| 144, 145 | `HutRightGranted`, `HutRightLost` | M4 | `fact_event` | Summary |
| 146 | `HouseholdDay` (snapshot) | M4 | `fact_household_day` | Summary |
| 147 | `PersonDayLife` (stage, household, partner, care ticks given and received, pregnant, ill) | M4 | `fact_person_day` (M4 columns) | Summary |
| 230–232 | `EstateOpened`, `EstateTransfer` (estate, asset kind, **asset id = lot id**, ware, qty, from, to, reason), `EstateSettled` | M4 | `fact_event`, `fact_inheritance` (+ `fact_ownership` via 206), `fact_event` | Summary |
| 233–238 | `EstateAssetClaimed`, `EstateDisputed`, `EstateDisputeResolved`, `WillMade`, `WillHonoured`, `WillIgnored` | M4 | `fact_event` | Summary |
| 210 | `ExchangeOpened` (DealId, A, B, kind, offered as meant†) | M5 | `fact_transaction` | Summary |
| 211 | `ExchangeStep` (DealId, step, giver, receiver, lot, ware, count, node) | M5 | `fact_transaction_line` | HV |
| 212 | `ExchangeClosed` (DealId, outcome, given each way, **each side's believed worth gain**) | M5 | `fact_transaction` | Summary |
| 213 | `GiftGiven` (DealId, giver, receiver, lot, ware, count, giver's `KinBand`, `SameHousehold`, `TrueR`†) | M5 | `fact_transaction` + `fact_transaction_line` | HV |
| 214 | `LabourHelped` | M5 | `fact_event` | HV |
| 300 | `TrustChanged` (a, b, before, after, familiarity before/after, cause code, cause ref = event lot/deal/message id) | M5 | `fact_social_day` (aggregate) + sample | HV (always kept when \|Δ\| ≥ 100) |
| 301 | `RelationEvicted` | M5 | `fact_social_day` | HV |
| 302 | `RecognitionError` | M5 | `fact_social_interaction` | HV |
| 303 | `BetrayalRecorded` | M5 | `fact_social_interaction` | Summary |
| 304 | `HelpGiven` (helper, helped, kind, value, helper's `KinBand`, `TrueR`†) | M5 | `fact_social_interaction` | HV |
| 305, 306 | `Apologised`, `Forgave` | M5 | `fact_social_interaction` | Summary |
| 307–309 | `ContestStarted`, `ContestMove`, `ContestEnded` | M5 | `fact_contest` | Summary (moves HV) |
| 310 | `PersonDaySocial` (RelCount, MeanTrust, friends, enemies, grudge sum, messages sent/received, misread) | M5 | `fact_person_day` (M5 columns) | Summary |
| 340 | `GestureMade` (MessageId, speaker, act, topic, quantity, counter, target, time, DealId, `Honest`†) | M5 | `fact_social_interaction` | HV |
| 341 | `MessageDecoded` (MessageId, listener, decoded fields, `Mismatch`†) | M5 | `fact_social_interaction` | HV |
| 342 | `ResponseChosen` (MessageId, listener, chosen learning verb, ignored flag) | M5 | `fact_social_interaction` | HV |

† analytics-only truth field: never read by sim rules or policies, recorded at Full only.

**Catalogue rules.** Each milestone adds its rows only in its own catalogue file (F0.11), its warehouse mappers only in `HouseholdMappings`/`SocialMappings`, its tables only in `Tables.Households.cs`/`Tables.Social.cs`, and its DDL only in migration `0005_households.sql` (M4) or `0006_social.sql` (M5) (renumbered: M3b took `0003_decision`, F0 `0004_people_goods`; the writer applies whatever numbers exist in order, so a branch with only one of them is fine). Columns that either adds to an F0 table go in its own migration as `ALTER TABLE ... ADD COLUMN` (distinct column names; M4 prefix none, M5 prefix `soc_` on `fact_person_day`). Codes are never reused; a changed field meaning takes a new code; an added field bumps `Version`.

---

## 9. Determinism

### 9.1 Random streams

No M4 or M5 code draws from an NPC's saved streams (`RngPolicy`, `RngWork`, `RngLearn`): these belong to M3 (exploration, work, consolidation), and sharing them would let each milestone shift the other's draws. All new draws use stateless keyed forks of the root, `s.Rng.Fork(label, a, b)` (or `SetToFork` into a scratch). A label is unique per call site; two call sites never share one.

| Owner | Label (a, b) | Purpose |
|---|---|---|
| M3 | `npc` (id), `founder` (id), `ecology` (hex), `dice` (roll), `duel` (unit, tick) | taken |
| M4 | `genome-founder` (npcId) | founder alleles (not `founder`: M3 uses it for placement, open 12.7) |
| M4 | `conceive` (childId), `develop` (npcId) | gametes; phenotype noise |
| M4 | `fertility` (motherId, day), `mortality` (npcId, day), `illness` (npcId, day), `hazard` (npcId, day) | daily life-cycle draws |
| M4 | `immigration` (faction, day), `household` (householdId, day), `kin-cue` (npcId, tick), `estate` (deceasedId, 0) | |
| M5 | `recognise` (observerId, tick), `decode` (listenerId, tick), `social-day` (npcId, day), `contest` (contestId, turn), `exchange` (npcId, tick) | |

### 9.2 Order within a tick

| Order | System | Owner | Work |
|---|---|---|---|
| 10 | Dice | stage 3 | |
| 200 | Economy (stage 3) | stage 3 | |
| **240** | `LifeCycleSystem` | M4 | day start: ageing and stage changes, natural death (`PopulationModule.Kill`), conception and births (`AddNpc`, mothers ascending), illness, immigration, household daily work; per tick: pregnancy and hazards |
| **245** | `SocialSystem` | M5 | day start: decay, familiarity loss, cache refresh from `KinData`/households, obligation and deal expiry, `PersonDaySocial`; per tick: message expiry |
| 250 | `PopulationSystem` | M3 + hooks | per NPC ascending id: body, reflexes, think (sense, people, `Recognise`, `FillHeard`, beliefs, base, candidates M3 → M4 → M5, decide, trace with `EpisodeKey`), act (M3 cases, then `HouseholdHooks.TryAct`, then `SocialHooks.TryAct`) |
| **255** | `EstateSystem` | M4 | settle estates due this tick (estates are opened in `OnDeath`, the same tick as `NpcDied`) |
| 300, 350, 400, 900 | Military, Trade, Factions, AI | existing | |

`Kill` order: mark dead → emit `NpcDied` → `HouseholdHooks.OnDeath` (carry into the estate, leave household) → `SocialHooks.OnDeath` → M3 cleanup (spill what is left, clear hut rights, release the mind). The people snapshot used for perception is taken once at the start of the population tick.

### 9.3 Saves and hashes

| Item | Rule |
|---|---|
| Save version | F0 sets `CurrentVersion = 4`. **Neither M4 nor M5 changes it.** The lead sets 5 in the commit that merges the second of the two (a single-milestone save lacks the other's state). Older versions are refused, as today. |
| State | New state lives only in `GodState.Households` (M4: households, partnerships, care, estates) and `GodState.Social` (M5: counters, deal traces, contests), plus per-NPC fields M4 adds to `Npc` and the minds (`Kin`, `Social`). All public fields, ascending-id lists, no dictionaries in saved state. |
| Tuning | Only in the five F0 blocks; each implements `IOmitWhenDefault`, so stage 3 hashes stay. No new `GameSetup` field (it is hashed in `core`); scenario options are tuning paths (`tuning.Households.Immigration`, ...). |
| Hash parts | Fixed order: `territory, economy, military, factions, trade, core, pending, population, cognition, learning, household, social`; the last two present only when their state is non-null. M4 also extends `HashPopulation` for its `Npc` fields; M5 adds nothing to M3 parts beyond `NpcMind.Social.AddTo`. |
| Recording | No sim rule reads the event buffer. Every scenario of each milestone gives the same hash at Off, Summary and Full. |

---

## 10. Merge plan

### 10.1 Owned files (only the owner creates or edits them)

| M4 | M5 |
|---|---|
| `Sim/Population/Households/**`, `Sim/Population/Genetics/**`, `Sim/Estate/**`, `Sim/Core/GodState.Households.cs` | `Sim/Social/**`, `Sim/Comms/**` (except the frozen `Message.cs` layout), `Sim/Economy/Exchange/**`, `Sim/Core/GodState.Social.cs` |
| `Sim/Information/PeopleCards.Household.cs`, `Game/Hud/GodHud.People.Household.cs`, `Game/Models/LifeStageLayer.cs` and household/family-tree views | `Sim/Information/PeopleCards.Social.cs`, `Game/Hud/GodHud.People.Social.cs`, `Game/Models/GestureLayer.cs` and relationship views |
| `DotNet/God.Runner/Warehouse/HouseholdMappings.cs`, `Tables.Households.cs`, `Migrations/0005_households.sql` | `SocialMappings.cs`, `Tables.Social.cs`, `Migrations/0006_social.sql` |
| Tests `Assets/God/Tests/EditMode/Household*`, `Genetics*`, `Estate*`, `HouseholdFixtures.cs`; runner tests `M4*`; scenarios `m4_*.scenario.json` | Tests `Social*`, `Comms*`, `Exchange*`, `SocialFixtures.cs`; runner tests `M5*`; scenarios `m5_*.scenario.json` |
| Docs: POPULATION, HOUSEHOLDS_AND_KINSHIP, BIOLOGICAL_INHERITANCE, MATERIAL_INHERITANCE | Docs: SOCIAL_MEMORY, COMMUNICATION_AND_COGNITION, ECONOMY §4.5 (exchange), GAME_THEORY island contests |

### 10.2 M3 files that one milestone may edit (the other never)

| File | M4 may edit | M5 may edit |
|---|---|---|
| `PopulationState.cs` | `Npc` (sex, parents, genome, phenotype, pregnancy, illness), `Hut.RightSource`, pedigree list, `DeathCause` use | — |
| `PopulationModule.cs` | `AddNpc` (genome, phenotype, stage), `StartGame` founder families, `HashPopulation` | — |
| `PopulationSystem.cs` | `TraitsOf`, `Body` (growth, illness, child limits), `Kill` M4 branch | — |
| `Thinker.cs` `FillSelf` | the `SelfView` fields F0 added (`Stage`, `Household`) | — |
| `StandardPolicies.cs`, `PolicyRegistry.cs` | — | benchmark civilian policies (`benchmark.TitForTat`, ...) |
| `PolicyTypes.cs` | inside the M4 intent block only | inside the M5 intent block only |

Everything else in M3 and F0 is frozen for both. A needed change goes to the lead as an amendment to this document first.

### 10.3 Shared files and the rule for each

| Shared thing | Rule |
|---|---|
| `EventCatalog` | untouched after F0; rows in the owners' catalogue files |
| Save format | only F0 (v4) and the lead's second-merge bump (v5) |
| `SimBuilder` | untouched by both (module wiring is in F0's entry points) |
| Scenario keys (`Recording/Scenario.cs`) | no new setup keys; options via `tuning.*`; new scenario files only, with the prefixes above |
| People panel | only the owner's partial file and card class |
| `DEV_LOG.md` | each milestone writes only its own `## M4 record` / `## M5 record` section (as M3 did); the lead folds rows into the shared tables at merge |
| `ACCEPTANCE_TESTS.md` | each edits only its own milestone table; I-BS is the lead's |
| Any other unavoidable shared edit | a marked block with a sentinel line, separated from the other milestone's block by at least two unchanged lines, so git merges the hunks without conflict |

Merge order does not matter. The lead merges M4 and M5 into `unity/poc`, bumps the save version, runs both suites with the other milestone switched off (7.3), then both on, then I-BS.

---

## 11. Integration test fixtures (I-BS)

The lead writes `IntegrationFixtures` and the I-BS tests after the merge. Each milestone must deliver the pieces below, usable from EditMode and runner tests, deterministic, and built only from real NPCs, real lots and the sanctioned APIs (no direct state writes except where marked "test-only").

| Fixture | Provided by | Signature (indicative; the names are binding) |
|---|---|---|
| World | M3 (exists) | `LearningFixtures.Empty`, `Glade`, `Npc`, `Bush` |
| Family | M4 | `HouseholdFixtures.Family(sim, node, adults, childAgesMilli[], hut: bool, store: (Ware, int)[]) → FamilyIds { Household, Hut, Bin, Adults[], Children[] }` (real partnership, household, kin beliefs from real cues) |
| Shared hut | M4 | `HouseholdFixtures.SharedHut(sim, node, memberCount, store)` |
| Kin belief | M4 | `HouseholdFixtures.SetKinBelief(sim, a, b, role, conf, source)` (test-only; source `Told`) |
| Death now | M4 | `HouseholdFixtures.Kill(sim, npc, cause)` (through `PopulationModule.Kill`) and `SetCustom(faction, custom)` |
| Meet | M5 | `SocialFixtures.Meet(sim, a, b, interactions)` (real gifts), `SetRelation(sim, a, b, trust, familiarity)` (test-only) |
| Trader pair | M5 | `SocialFixtures.TraderPair(sim, node, aGoods, bGoods) → (a, b)` with complementary needs |
| Trace | F0 | `GoodsTrace.Follow(events, lotId)`, `GoodsTrace.IdMultiset(holders)`, `EventProbe` |

| Scenario | What each milestone must make observable |
|---|---|
| I-BS.1 parent feeds child | M4: `ChildFed.Lot`; `Consumed` v2 with `Lot` and actor = child. Assert `Produced(lot, by parent) → ItemMoved* → ChildFed → Consumed(Eaten, child)` by `GoodsTrace` |
| I-BS.2 shared shelter and store | M4: both `Slept.Hut = h`; `ItemMoved(Withdraw)` from the same bin by ≥ 2 actors |
| I-BS.3 kin and non-kin help | M4 and M5: every help-like event (`ChildFed`, `CareGiven`, `GiftGiven`, `HelpGiven`, `LabourHelped`) carries the actor's believed `KinBand` (and `TrueR` at Full). Report counts by band |
| I-BS.4 relationship grows | M5: `TrustChanged` with familiarity and a cause ref to the gift/exchange/care event; ≥ 3 increasing steps |
| I-BS.5 mutual exchange | M5: two `ExchangeStep`s (with lots), `ExchangeClosed(Completed)` with both believed worth gains > 0 |
| I-BS.6 behaviour changes | M5 (and M4 for care): a twin design as M3.25; the learned term carries the difference; `Frozen` control |
| I-BS.7 estate without duplication | M4: `EstateTransfer.AssetId = lot id`, `MergeRule.Never`. Assert the id multiset of the dead person's carry and personal share equals the multiset of transferred, buried or ownerless ids; `NextLotId` unchanged by the settlement; C1–C5 hold |

All seven also run at Off, Summary and Full with the same hash.

---

## 12. Conflicts between the documents and the code (resolved here)

| # | Documents say | Code (M3) does | Chosen | Why |
|---|---|---|---|---|
| 12.1 | `ItemLot` has `short Count`, qualities, `VisibleWare`; owner explicit only from M6 (ECONOMY 3.1) | `int Count`, `Uses`, no owner | Code fields + `Owner` now (F0); qualities stay for M8 | M4 estates and household goods need owner ≠ holder now (M4.19 "each `ItemLotId` has one owner") |
| 12.2 | Household store is `ItemStack[8]` (HOUSEHOLDS 3); carry is `ItemStack[]` (POPULATION 3) | `ItemLot[]` carry, no stacks | `LotBin` of `ItemLot` | Lot ids must survive storage for I-BS.1 and I-BS.7 |
| 12.3 | `Partnership` keyed by the pair (A < B) | — | `PartnershipId` | The same pair may partner twice; concurrent partnerships (Q5); warehouse spells need a key |
| 12.4 | Verbs are a byte, "at most 64" (NPC_LEARNING 3.2) | byte; `Verb.Max = 64` is never enforced | `Max = 128` | One verb per gesture act plus later domains exceed 64 |
| 12.5 | `NpcObservation` has `Things`, `Npcs`, `Messages` (COMMUNICATION 3) | `SelfView`, `Sensed`, candidates | Code shape + `People` and `Heard` (F0) | Keeps M3 policies and the reflection tests valid |
| 12.6 | Social and comms draws use `Rng.Fork("npc", id)` (SOCIAL 2, COMMUNICATION 2) | that stream is the NPC's saved exploration stream | keyed forks per purpose (9.1) | Otherwise M4 and M5 would shift each other's and M3's draws |
| 12.7 | Founder genomes use `Fork("founder", npcId)` (BIOLOGICAL 4.2) | M3 already uses that key for founder placement and pack | `genome-founder` | Identical streams would correlate genomes with placement |
| 12.8 | Begging is a gesture (HOUSEHOLDS child-rearing); care arrangements agreed by `SayOffer` | — | Begging and care proposals are M4 displays (7.2) | M4 must work without M5 |
| 12.9 | Island contests start at M5 (GAME_THEORY); M4.21 needs a contest for simultaneous claimants | — | open decision 13.2 | |
| 12.10 | MATERIAL 9: "save version 3 includes open estates" | v3 since M0 | v4 at F0, v5 at the second merge | |
| 12.11 | SOCIAL 4.2: a betrayal raises the Anger drive | 5 drives | no new drive in M4/M5 (13.4) | Drive arrays are in M3's events and both milestones would change them |
| 12.12 | DATA_WAREHOUSE puts `ChildFed`/`CareGiven` aggregates in `fact_social_day`, a social table | no warehouse NPC tables yet | `fact_social_day` and `dim_person` created by F0 | Both milestones write them |
| 12.13 | POPULATION: immigration is a scenario key | `GameSetup` is hashed | tuning path `Households.Immigration` (default 0) | Keeps stage 3 hashes |
| 12.14 | `OwnerKind` has no estate (ECONOMY 3.1) | — | `OwnerKind.Estate = 6` | An estate holds goods for up to 2 days (MATERIAL 4.1) |
| 12.15 | Sequencing amendment: the lot episode takes episode-key kind 16 | M3b keys lot episodes `1 << 30 \| lotId`, which is kind 64 in `EpisodeKey.Make` | kind 64 (`EpisodeKey.Lot`) | Episode keys are in the `learning` hash part; re-keying to 16 changes every learning hash with no behavioural difference, against the F0 gate. Kind 64 is outside the M4 (1–7) and M5 (8–15) blocks, so the amendment's purpose holds |
| 12.16 | One hook class per milestone (`HouseholdHooks`, `SocialHooks`) | a class that takes `GodState` anywhere is a frontier type for G4.1 (`PolicyTests`) | two classes per owner file: sim side `XHooks`, mind side `XMindHooks` | Keeps G4.1's reflection test strict without growing its allow-list |
| 12.17 | `Owner` and `Holder` are `readonly struct`s | the save writes public fields only | `Owner` is a mutable struct (public fields); `Holder` readonly | Lots are saved; readonly fields do not round-trip reliably |
| 12.18 | §2.2 signatures | M3 needs `WithTool`, eating at a source, tool wear | additions listed under §2.2 "As built" | Behaviour-neutral rewrite of M3's sites |
| 12.19 | §2.3 reward table (M3's immediate goods reward) | M3b's realised-value credit | `Goods` pays M3b's rules (§2.3 "As built") | Identical effects (gate) |
| 12.20 | `LifeStage` added by F0 with `Adult = 0` | `God.Sim.LifeStage { Child = 0, Adult = 1, Elder = 2 }` already existed (calendar) | the one enum, renumbered `Adult = 0, Child = 1, Elder = 2` (byte) | One enum; a zeroed record is an adult. Nothing stored or hashed the old numbers |
| 12.21 | `NpcInspection.Social` is the social card | `NpcInspection.Social` is already the score term | `NpcInspection.Household` (`HouseholdCard`) and `NpcInspection.Relations` (`SocialCard`), plus `NpcInspection.Stage` | Name clash |
| 12.22 | `ItemMoved.IntoLot ≠ Lot` marks a merge (C3) | M3's merge keeps the **lower** id, which may be the moved one | `ItemMoved.EndedLot` names the id that ended | Without it the older lot's end is invisible to C3 |
| 12.23 | `fact_ownership` holds spells | — | one row per `OwnershipChanged` (spells derive from it) | Exact reconciliation with the event; M13a may add a spell view |
| 12.24 | Migration numbers 0002 / 0003 / 0004 | M3's warehouse took 0002, M3b's 0003 | F0 0004, M4 0005, M5 0006 | Sequencing |
| 12.25 | `OwnershipBasis` 8–13 named | drops and death spills change owners too | `Abandoned = 10`, `Spilled = 11` | Every `OwnershipChanged` has a basis |
| 12.26 | The founder packs emit `Produced` | they are made in `StartGame`, before any event buffer | no event; ledger booked; `GoodsTrace.Start` reads them from the state | No change to M3's event stream at tick 1 |
| 12.27 | `SeenPerson` fields unspecified | — | `Id, As, Faction, SameFaction, Node, Dist, Stage, SameHousehold, Doing, Asleep, CarryMask, HoldsTool, Display, DisplayLevel, DisplayTarget, Gesture`; filled from a per-tick `PeopleIndex` snapshot (nearest 8 within sight, ties by id); M4 and M5 fill theirs through `FillPerson` | A6 fields filled in F0 (nothing reads them: neutral) |
| 12.28 | `SocialHooks.OnHandOver` only on owner change | A3 | every hand-over between two living people, with `OwnerChanged` | A3 |
| 12.29 | Separate `Permission` seam (D3) | A8 | one seam: `Permits.Believed` in `Affordances.cs` | A8 supersedes it |
| 12.30 | `SaveFile` v4 refuses older | v3 saves (M0–M3b) exist | refused with the same "older version" message | 9.3 |
| 12.31 | `Thinker.Known` knows no people (frozen M3.12 check) | M4 aimed person candidates at the actor's own node with the handle as id; M5 used `IntentTarget.None` | **I-BS amendment:** person targets are `IntentTarget.Npc` (handle + the person's node); `Known` accepts a person in `sc.People` or remembered (`CognitionExtensions.Remembers`: believed kin, social memory). Both workarounds removed | M4 and M5 asked for it; I-BS.15 checks every person-directed choice |
| 12.32 | A bin is never perceived: store use offered blind, heir collection a reflex, O4 delivers estate goods across nodes | - | **I-BS amendment:** the Sense seam `PopulationExtensions.SenseBins` (M4 body): within sight a person senses its household's store (`HClass.Store`) and estate goods (`HClass.Estate`) with the band of what is its own or its household's; `BeliefKind.Store` remembers them; store and estate candidates are `IntentTarget.Bin` from perception or memory, then walk and `Withdraw` within `Reach.Store`, permission checked lot by lot. `Estates.Collect` and O4's delivery removed: what heirs leave goes ownerless at the death node (first arrival may claim it) | A2, D3 9: nothing used at a distance |
| 12.33 | Genome instinct gains reach the mind | `Npc.Traits[12..15]` (M4 workaround) | **I-BS amendment:** `SelfView.HungerGain/FearGain/SocialGain/NurtureGain` (0 = none); traits 12-15 no longer written but kept (zero) so later trait slots (demography 16-18) keep their indices. Gains still apply only to M4's verbs | M4 asked for it |
| 12.34 | `OpportunitySlot` has no Social column | M5 kept `Social.Term = 0` so M3.28's sum held | **I-BS amendment:** `OpportunitySlot.Social`, `Opportunity.Social` (hashed only for minds with an M4/M5 part). `Social.Term` stays 0: with it on, L14 moves only from 32% to 29% (20 seeds; target at most 20%) | M5 asked for it; evidence in DEV_LOG I-BS record |
| 12.35 | F0's `Produced`/`Consumed` mappers make a `dim_person` row for npc 0 | M3 tests counted `npc_id > 0` | rows with no actor have `person_key` NULL; no npc 0 person; tests tell personal rows by `lot_id IS NOT NULL` | M4 amendment 4 |
| 12.36 | SOCIAL 4.1: familiarity +100 per interaction; warehouse names | M5 built 180, `fact_transaction_line`, `soc_` columns | 180 in SOCIAL 4.1 (doc updated); the table names in 8.2 are as built | M5.2 evidence (SOCIAL 8.1) |

---

## 13. Open decisions (each with the default taken)

| # | Decision | Default |
|---|---|---|
| 13.1 | Who lands the seams | The lead lands F0 before branching (section 0). Fallback if refused: M4 lands F0 first on its branch and M5 rebases onto it before writing code |
| 13.2 | M4.21 "simultaneous claimants raise one contest" | `SocialHooks.Contest(s, ev, a, b, assetKind, assetId) → winner`, M5 body. On M4 alone the stub returns the lower NpcId and emits nothing; M4 reports that clause as checked at I-BS |
| 13.3 | M5.12 S5 "shared store on" arm | Needs M4's store: run at I-BS. M5 alone runs S2 and S5 with the store off (gifts only) |
| 13.4 | New drives (Anger, Loneliness, Boredom) | None in M4/M5; betrayal uses grudge and trust only |
| 13.5 | Goods reward rule | Ownership-based, neutral between an NPC and its own household (2.3) |
| 13.6 | M5's subjective valuation as the learning goods channel | No: `GoodsWorth.Of` stays; valuation only for exchange decisions and telemetry |
| 13.7 | Who may barter or give | Adults and elders get exchange and gift candidates; children only `Say(NeedFood)`, `Say(NeedHelp)`, `Say(Follow)`, plus M4's `Beg` |
| 13.8 | Ground ownership in M4/M5 | Always ownerless (no theft before M8) |
| 13.9 | `ItemMoved` warehouse table | `fact_event` (HV) now; a dedicated `fact_item_move` is left to M13a |
| 13.10 | Care arrangements by gesture (`TopicKind.Task`) | After I-BS (M7); M4 uses its own displays |
| 13.11 | Recognition when `Social.Enabled = 0` or on the M4 branch | Perfect (`As = Id`) |
| 13.12 | `SeenPerson.Id` exposed to policies | Kept as an opaque handle for intents; features and memory use `As` only (M5 adds a review check) |
| 13.13 | Branch save compatibility | None between branch builds; v5 after the second merge refuses both v4 variants |
