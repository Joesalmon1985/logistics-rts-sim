# Shared architectural contracts for Civilisation and Divinity (owner D5, binding)

This document fixes every interface that the parallel D5 packages (brief §39: F1, F2, C1–C10, D1–D7, U1–U6, Q1–Q4) share. It **extends** [docs/stage4/SHARED_CONTRACTS.md](../stage4/SHARED_CONTRACTS.md) (the M4/M5 contracts, F0, rows 12.1–12.36): every rule there still holds unless a row of section 20 below changes it. Owner decisions are cited by id ([OWNER_DECISION_REGISTER.md](OWNER_DECISION_REGISTER.md), D5.1–D5.33 = the bullets of brief §51 in order); the brief is [OWNER_BRIEF_2026-11-26.md](OWNER_BRIEF_2026-11-26.md).

**Authority.** For anything that crosses a package boundary, this document is the single authority. It **overrides any conflicting proposal** in DATA_WAREHOUSE_DESIGN §12, HEADLESS_SIMULATION §12, INFORMATION_AND_MEDIA_DESIGN §12, AI_TRAINING_DESIGN §14, CAMERA_AND_CONTROLS_DESIGN, PLAYER_UI_DESIGN, NPC_ANIMATION_DESIGN, VISUAL_WORLD_DESIGN, GAMEPLAY_SCENARIOS, ORGANISATIONS_AND_INSTITUTIONS_DESIGN, INSTITUTIONAL_SERVICES_DESIGN and the other D5 design documents (DIVINE_PLAYER, MYTHIC_AND_MIRACLES, POSSESSION_AND_AVATAR, COMMANDMENTS_AND_RELIGION, RESURRECTION). It also settles the eleven blockers of [CURRENT_IMPLEMENTATION_BASELINE.md §13](CURRENT_IMPLEMENTATION_BASELINE.md) (section 0.3). Where it confirms their names (most of them) it says so; where it renumbers or renames, section 19 lists the change. A package that finds a contradiction stops and asks the lead for an amendment row in section 20; it never works around it.

**Grounding.** Written against `unity/poc` at `8617eaa` (M4 + M5 merged, save v4, warehouse schema v6) and the two branches that merge first on 27 Nov: `unity/ibs` (`81e4f47`, SHARED_CONTRACTS 12.31–12.36: person targets in `Thinker.Known`, `PopulationExtensions.SenseBins`, `SelfView` instinct gains, `OpportunitySlot.Social`, no npc-0 person row) and `unity/demog` (`5aaa255`: household verbs 43–45, events 148–150, `Npc.Traits` 16–18, `ReproductiveUnion`, `EconomicPartnership` stub). **F1 is cut from `unity/poc` after both have merged**, and its neutrality pins are taken at that merge head ("the base"). Paths below are relative to `Assets/God/Sim/` unless they start with `DotNet/` or `docs/`.

**Words.** *Must*, *never*, *only* are binding. "Truth" is layer 1 (`GodState`); "belief" is what a mind holds. "Body" is an `Npc` record (a person or an avatar, section 4). "Intervention" is one accepted divine action.

---

## 0. The F1 foundation commit (lead, before any D or C package starts)

As F0 did for M4/M5, the lead lands **one behaviour-neutral foundation, F1**, on `unity/poc`. Every D, C and U package branches from F1. After F1 each stub file has exactly one owner (column 4), and the files marked *frozen* change only by an amendment row in section 20. F1 is small in behaviour (none) and large in surface; it is the only package that edits shared enums, `GodState`, `GodSim`, `SaveFile`, `EventCatalog`, `PolicyTypes`, `Thinker`, `MindView` and `PopulationSystem` for D5.

### 0.1 What F1 must contain

| # | Item | Files (new unless "edit") | Owner after F1 |
|---|---|---|---|
| F1.1 | **Ids and registries** (section 1): `GodId` helpers, `InterventionId`, `OrgId`, `CommandmentId`, `ReligionId`, `LedgerId`, `RequestId`, `ObligationId`, `AvatarBodyId` rule; counters as public fields of their owning states | `Divinity/DivinityIds.cs`, `Orgs/OrgIds.cs` | frozen |
| F1.2 | **State slots**: `GodState.Divinity` (`DivinityState`, partial, sub-objects `Gods` D1, `Interventions` D1, `Control` F1/D1, `Messages` D2, `Miracles` D3, `Avatars` D5, `Doctrine` D6, `Archive` D7), `GodState.Organisations` (`OrgState`, C6 plus sub-objects C7–C10), `GodState.Civ` (`CivState`, sub-objects C1, C2, C3, C5); each null unless its switch is on; each sub-object null until its package fills it; `AddTo(ref StateHasher)` hashes sub-objects in the fixed order above, each only when non-null | `Core/GodState.Divinity.cs`, `Core/GodState.Organisations.cs`, `Core/GodState.Civ.cs`, `Divinity/DivinityState.cs` (+ one partial file per package: `DivinityState.D1.cs` … `D7.cs`), `Orgs/OrgState.cs` (+ `OrgState.C6.cs` … `C10.cs`), `Civ/CivState.cs` (+ `C1`, `C2`, `C3`, `C5`) | each partial file to its package; the container files frozen |
| F1.3 | **Tuning blocks** `SimTuning.Divinity`, `SimTuning.Orgs`, `SimTuning.Civ`, each `IOmitWhenDefault`; `Divinity.Enabled = 0`, `Divinity.Mode = Standard`, `Orgs.Enabled = 0`, `Orgs.Emergence = 1`, `Civ.Enabled = 0`; one nested class per package (`Divinity.Mythic` D1, `.Instruction` D2, `.Miracles` D3, `.Possession` D4, `.Avatar` D5, `.Doctrine` D6, `.Resurrection` D7; `Orgs.Health` …) | edit `Core/SimTuning.cs`; `Divinity/DivinityTuning.cs`, `Orgs/OrgTuning.cs`, `Civ/CivTuning.cs` | nested classes to their packages; the block roots frozen |
| F1.4 | **Hash parts** appended to `GodSim.AllHashPartNames` in this order: `…, "household", "social", "civ", "organisations", "divine"`, each present only when `Population != null` and its state is non-null; `PartNamesOf`/`PartsOf` extended the same way | edit `Core/GodSim.cs` | frozen |
| F1.5 | **Module entry points** called from `GodSim.Create` (after `PopulationExtensions.StartGame`), `InstallModules`, `PopulationModule.AddNpc`, `Kill`, `SimInvariants.Check`: `CivModule`, `OrgModule`, `DivinityModule` with `StartGame/Install/NewMind/OnDeath/Check` (no-ops; `StartGame` creates the state only when the switch is on and the game has a population) | edit `GodSim.cs`, `PopulationModule.cs`, `SimInvariants.cs`; `Civ/CivModule.cs`, `Orgs/OrgModule.cs`, `Divinity/DivinityModule.cs` | call sites frozen; bodies: Civ C1, Orgs C6, Divinity D1 |
| F1.6 | **Extension dispatch** extended (section 13): new hook classes `CivHooks/CivMindHooks`, `OrgHooks/OrgMindHooks`, `DivinityHooks/DivinityMindHooks`, called after M4 and M5 at every existing site, plus the new sites `OverrideIntent`, `ForceThink`, `ActionEnded`, `SenseExtra`, `DivineTerm`, `ArchiveMind`, `Heal`, `Damage` | edit `Cognition/Extensions.cs`, `Population/PopulationExtensions.cs`, `Thinker.cs`, `PopulationSystem.cs`; stubs `Civ/CivHooks.cs`, `Orgs/OrgHooks.cs`, `Divinity/DivinityHooks.cs` | dispatch frozen; hook bodies to C1 / C6 / D1 (D1 sub-delegates by partial files, 13.3) |
| F1.7 | **Id-range plumbing** (section 14): `ThinkScratch.MaxCand` 24 → 36 (quotas 4 each for Civ, Orgs, Divinity); `NpcVerb` blocks 170–179 Civ, 180–189 Orgs, 190–199 Divinity with sentinels; learning verb rows reserved 96–127; `GestureAct` 18–23 and `TopicKind` 7–10 reserved; `FeatureKind.Divine = 17`, `Org = 18`; `SelfFeature`, `SituationKind`, `ObjClass`, `NpcGroup`, `Role` blocks; episode-key kinds 16–39 | edit `Thinker.cs`, `PolicyTypes.cs`, `Learning/Verbs.cs`, `Learning/Features.cs`, `Comms/Message.cs`, `Learning/EpisodeKey.cs` | frozen |
| F1.8 | **Goods and body enums widened** (sections 6, 8): `OwnerKind.Org = 7`; `BinKind` 3–6; `MoveReason` 10–12; `OwnershipBasis` 14–23; `ProduceReason.Miracle = 4`, `Crafted = 5`; `ConsumeReason.Miracle = 5`, `Treatment = 6`, `Offering = 7` (ledger arrays stay 8 slots: no hash change); `BodyKind` and `Npc.Kind`; `Npc.BioAgeOffset`; `Bodies.IsPerson`, `Bodies.AgeTicks`; every person-semantics loop in M3/M4/M5 code switched to `Bodies.IsPerson` (section 4.3); `SeenPerson.Body` | edit `Population/LotBin.cs`, `Goods.cs`, `PopulationState.cs`, `Cognition/People.cs`, and the loop sites listed in 4.3; `Population/Bodies.cs` | frozen |
| F1.9 | **Control authority and the decision-override seam** complete (section 11): `ControlKind`, `ControlEntry`, `Control.Of/TryAcquire/Release/Sweep`, the precedence table, `Control.OverrideIntent` and `Control.ForceThink` (the bodies behind the `PopulationExtensions` sites) and the lease/heading resolution of `DivineControlInput`; no caller opens an entry in F1 | `Divinity/Control.cs` | frozen (the **single owner** of the seam; D2, D4, D5 change it only by amendment) |
| F1.9b | **Mind slots**: `NpcMind.Divine` (`DivineMind`, D2 inbox / D6 beliefs and commandments), `NpcMind.Orgs` (`OrgMind`: at most 6 `OrgBelief`, about 144 B, C6), `NpcMind.Civ` (`CivMind`: believed obligations and ownership, C2/C1); each null unless its switch is on, hashed only when present (the `Kin`/`Social` pattern) | edit `Cognition/Mind.cs`; stubs `Divinity/DivineMind.cs`, `Orgs/OrgMind.cs`, `Civ/CivMind.cs` | as listed |
| F1.10 | **Commands, base types and refusal codes** (section 10): `DivineCommand` base, every concrete command class of section 10.2 as **plain data with no handler** (so every one is refused with `Reasons.Disabled` until its package registers a handler); `DivineAct`, `DivineTarget`, `DivineScope`, `InterventionKind`, `MiracleKind`, `ResurrectionVariant`; `DivineReasons`; `GameSetup.Control` (`PlayerControl`) and `GameSetup.HumanGod`; the god-only gate in `GodSim.Submit` | `Divinity/DivineCommands.cs`, `Divinity/DivineTypes.cs`, `Divinity/DivineReasons.cs`; edit `Core/GameSetup.cs`, `Core/GodSim.cs`, `Core/Reasons.cs` | types frozen; handlers to their packages |
| F1.11 | **Policy level `God`** (section 12): `PolicyLevel.God = 4`; `IGodPolicy`; `PolicyAssignment` arrays sized `PolicyLevelCount = 5` (its `ToString` omits the god level while it is `none`, so manifests are unchanged); `TryParseLevel("god")`; `IDivineView`, `DivineObservationProfile` (types only), `IdleGod` registered | edit `Policy/PolicyInterfaces.cs`, `PolicyRegistry.cs`; `Information/DivineView.cs` (stub); `Policy/GodPolicies.cs` | D1 |
| F1.12 | **Events**: `TelemetryDomain` values of section 15.1; `EventCatalog.RangeOf` for them; `EventCatalog.All = Core + Household + Social + CivEvents.Catalog + OrgEvents.Catalog + DivinityEvents.Catalog`; each catalogue file is a concatenation of per-package partial arrays (`DivinityEvents.D1` … `D7`), empty in F1 | edit `Telemetry/DomainEvent.cs`; `Civ/CivEvents.cs`, `Orgs/OrgEvents.cs`, `Divinity/DivinityEvents.cs` | per-package arrays to their packages |
| F1.13 | **Save**: `SaveFile.CurrentVersion = 5`; `FromJson` accepts null `Divinity`, `Organisations`, `Civ` (section 17) | edit `Save/SaveFile.cs` | lead only |
| F1.14 | **Knowledge layer tag** `KnowledgeLayer` (section 9) and the inspection cards `NpcInspection.Divine` (`DivineCard`, D2/D6), `.Orgs` (`OrgCard`, C6), `.Economy` (`CivCard`, C1), filled by stub `Fill` methods | edit `Information/PopulationKnowledge.cs`; `Information/PeopleCards.Divine.cs`, `PeopleCards.Orgs.cs`, `PeopleCards.Civ.cs` | as listed |
| F1.15 | **Warehouse**: migration `0007_divine_core.sql` (section 18); `EventMapRegistry.CreateDefault` calls `DivinityMappings.Register(r)`, `OrgMappings.Register(r)`, `CivMappings.Register(r)`, each of which calls per-package stubs (`D1Mappings.Register` …); `Tables.Divinity.cs`, `Tables.Orgs.cs`, `Tables.Civ.cs` partial stubs; `MapContext.God`, `MapContext.Org` | `DotNet/God.Runner/Warehouse/...` | stubs to their packages |
| F1.16 | **Tests and helpers**: the neutrality pins P1, P4, P8 (section 16) at the base hashes; the reflection and static tests of sections 4.3, 9.3, 11.5; `DivineProbe` (collect divine events) and `MythicTrace` (balance reconstruction from events) helpers | `Assets/God/Tests/EditMode/Support/...`, `DotNet/God.Runner.Tests/F1*` | frozen |

| F1.17 | **One-time extensions of frozen enums** (blocker 9), all neutral: `BeliefKind` 7 `Shrine` (D3/D6), 8 `OrgPlace` (C6), 9 `Asset` (C1); `GestureAct` reservations (section 14); `Ware.Herbs = 32` with `Wares.PersonalCount` 32 → 40, and `HashPopulation` hashing the ledger in the old 32-ware order plus the extra wares only when any is non-zero (so the `population` part is unchanged); `GestureMade` v2 with `DurationTicks` (from `CommsTuning`; events are not hashed); `DeathCause` deliberately **not** extended (20.4) | edit `Cognition/Mind.cs`, `Economy/Wares.cs`, `PopulationModule.cs`, `Social/SocialEvents.cs`, `Comms/Messages.cs` | frozen; `Herbs` sources and use to C7 |
| F1.18 | **Procedural names**: `Names.Person(seed, npcId)`, `Names.God(seed, godId)`, `Names.Org(seed, orgId, kind)`: pure functions of their arguments (no state, not hashed, not saved); player aliases, favourites, follows, selection and camera live in a non-hashed UI save section (`ISaveExtra` key `ui`), never in `GodState` | `Information/Names.cs`; `Game/...` UI extra | U1 (UI extra); names table to U4 |

### 0.2 F1 gate
- `dotnet test DotNet -c Release` and Unity EditMode green; stage 3 `PolicyRunTests` hashes unchanged; Unity smoke hashes unchanged (stage 3 seed 2 and population seed 2).
- **Unity compile check** (blocker 11): `dotnet build` does not compile `Assets/God/Game`, so F1 and **every** package that changes a type `God.Game` uses opens the editor in batch mode (`unity command test run` after a clean console read) and reports 0 compile errors before its Unity tests; a package report without it is not accepted.
- P1, P4, P8 (section 16): with all three switches at 0, every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6` and the `demog_*` scenarios, seeds 1–3, every checkpoint, equals the base.
- The static person-loop test (4.3) and the "no handler, refused" test (every F1 command type is refused with `disabled`) pass.

### 0.3 The baseline blockers (CURRENT_IMPLEMENTATION_BASELINE §13), resolved

| # | Blocker | Resolution | Owner of the change | Neutrality |
|---|---|---|---|---|
| 1 | I-BS and demography branches open | F1 is cut after both merge; pins taken at that head | lead | — |
| 2 | One owner of the decision seam in `PopulationSystem.Think` | **F1 owns it entirely**: the call sites (`ForceThink`, `OverrideIntent`, `ActionEnded`, `DivineTerm`) and the override logic itself, which lives in `Control.cs` and reads only `ControlTable`. D2, D4, D5 never edit `Think`, `Start`, `Match` or `DecisionRecord`: they open and close `ControlEntry`s and add candidates through `DivinityMindHooks.AddCandidates`; D2/D6 supply only the `DivineTerm` value (sections 10.3, 11.3) | F1 | no entries, no candidates, term 0: identical |
| 3 | `Kill` discards the mind (`PopulationModule.cs:106`) | `ArchiveMind` hook before `Mind = null`; archive in `DivinityState.Archive` (section 4.4) | call site F1, body D7 | archive empty when off or `Archive = Off`; cognition part unchanged either way |
| 4 | God/org identity kinds, domains, codes, migrations | sections 1, 15, 18.2 | F1 | catalogue rows only |
| 5 | Divine command base not a `FactionCommand`; AI-god level | `DivineCommand : Command` (never `FactionCommand`, so `FactionCommandSet` and the HUD's S-CMD-1 set are unchanged); `PolicyLevel.God`; `IntentRules.Allowed(God, …)` is false for every intent (gods act through commands) | F1 | — |
| 6 | Human as a god; hide faction commands | `PlayerControl.GodOnly` gate in `GodSim.Submit` + HUD hides the 18 faction paths when `Control == GodOnly` (U2); stage 3 mode and its tests stay (temporary mechanism, DEV_LOG) | F1 (gate), U2 (HUD) | default `Stage3Faction` |
| 7 | Avatar entity kind | `Npc` body with `BodyKind.Avatar` (4.2) | F1 types, D5 behaviour | no avatar exists until manifested |
| 8 | Divine observation view | `IDivineView` filled by `DivineViewFiller`, separate from `FactionKnowledgeView`; in `Omniscient`/`Reported` it wraps the same `ViewFiller` output, so the HUD reflection gate still holds (section 9) | D1 | — |
| 9 | Frozen enums to extend once | F1.8, F1.17, section 14 | F1 | each listed as neutral |
| 10 | Population not yet viable | divine and resurrection tests use fixtures (`IntegrationFixtures`) and short horizons; generation-scale resurrection is tested on an archived record, not a natural lineage; F2 owns viability | F2 | — |
| 11 | Unity compile gap | gate 0.2 | every package | — |

---

## 1. Persistent identity

All ids follow SHARED_CONTRACTS §1: `int`, start at 1, allocated only by the listed path from a public counter in the owning state (saved and hashed), never reused (also after death, save, load or dissolution), allocated in deterministic processing order. 0 means none.

| Id | Allocator (only path) | Counter | Notes |
|---|---|---|---|
| `NpcId` | unchanged: `PopulationModule.AddNpc` | `PopulationState.NextNpcId` | **A person keeps one NpcId for every life** (resurrection restores the same record, section 4.4). Avatar bodies never take an NpcId (4.2) |
| `GodId` | `DivinityModule.StartGame` only | none: `GodId = (int)faction + 1` for each faction of the setup | Gods are created once, before the first tick, one per initial faction (D5.3); 1..`FactionCount`. **No code removes, merges or re-ids a god** (D5.4, D5.5). Never equal to a `FactionId` by convention (offset 1) |
| `AvatarBodyId` | `DivinityModule` at a god's first manifestation | none: `AvatarBodyId = 0x4000_0000 + GodId` | One body record per god for the whole game, reused by every manifestation (persistent divine identity, D5.9). Above every NpcId, so `PopulationState.Npcs` stays sorted and `Find` works |
| `InterventionId` | `Interventions.Open(...)` (D1), called only from a divine command's `Apply` | `DivinityState.NextInterventionId` | One per accepted divine action of any kind. The warehouse's `command_id`, `compulsion_id`, `possession_id`, `avatar_id` (a manifestation spell), `miracle_id` are this id. Carried by every effect event |
| `LedgerId` | `DivineLedger.Book(...)` (D3), called with every miracle `Produce`/`Consume` | `DivinityState.NextLedgerId` | One per creation or destruction row (`ResourceCreated`/`ResourceDestroyed`) |
| `CommandmentId` | `Doctrine.Issue` (D6) | `DivinityState.Doctrine.NextCommandmentId` | Revisions keep the id and bump `Revision` |
| `ReligionId` | `Doctrine.FoundReligion` (D6) | `DivinityState.Doctrine.NextReligionId` | A religion is a tradition, not a god and not an organisation (13, section 7.3) |
| `OrgId` | `Orgs.Found(...)` (C6) only | `OrgState.NextOrgId` | Every organisation of every kind (firm, cooperative, commune, government, temple, school, clinic, bank, court, army unit). Dissolved organisations keep their record |
| `RequestId` (service request) | `Services.Open(...)` (C6) | `OrgState.NextRequestId` | name confirms INSTITUTIONAL_SERVICES; warehouse `request_id` |
| `ObligationId` (`LoanId` is an alias) | C2 `Obligations.Open` | `CivState.NextObligationId` | One id space for formal loans, promises and deferred obligations. **`FormalObligation`** (debtor party, creditor party, principal ware and count, terms, collateral lot, status) is a **double-entry registry owned by C2** in `CivState`: every open obligation is one claim and one liability of equal terms, created and closed only by C2's API, and it moves no goods (C1–C8 unaffected). C6/C8 organisations create obligations only through that API. Informal promises stay in M5/M7 social memory |
| `CaseId` | C9 `Cases.Open` | `OrgState.Justice.NextCaseId` | |
| `ContractId` (employment) | C3 `Employment.Hire` | `CivState.NextContractId` | |
| `FactionId` | unchanged (`Faction` enum 0–5) | — | **No new faction ever.** A faction is a community and stage-3 polity marker; it may be eliminated, its god is not |

**Episode-key kinds** (`EpisodeKey.Make(kind, id)`, id < 2^24): 16–23 Civ (16 loan, 17 employment contract, 18 productive asset), 24–31 Orgs (24 service request, 25 membership), 32–39 Divinity (32 divine instruction: id = InterventionId, 33 commandment: id = CommandmentId, 34 compulsion, 35 possession). 40–63 and 65+ stay free.

---

## 2. God identity and the god registry (owner D1; F1 lands the types)

```csharp
// Divinity/DivinityState.D1.cs (D1)
public enum GodSeat : byte { None = 0, Human = 1, Ai = 2 }
public sealed class GodRecord {
    public int Id;                    // GodId
    public string Name;               // deterministic from the setup (procedural list keyed by GodId), never player text in the sim
    public Faction OriginFaction;     // history only: the faction it was created for (D5.3). Never an authority
    public GodSeat Seat; public string PolicyName;     // who drives it (section 12); changed only by SwitchGod / SetGodPolicy
    public long Mythic, MythicMax;    // milli-Mythic (section 5)
    public long[] CooldownUntil;      // per InterventionKind, tick
    public int AvatarBody;            // 0 until the first manifestation, then AvatarBodyId
    public DivineObservationProfile Profile;           // section 9
    public long CreatedTick;
}
public sealed class GodRegistry { public List<GodRecord> Gods; }   // sorted by Id; Count fixed at StartGame
```

| Invariant | |
|---|---|
| G1 | `Gods.Count == Setup.FactionCount` from the start; ids 1..N contiguous; the list never shrinks (D5.4, D5.5). |
| G2 | Nothing about a god depends on its faction, state, religion, population or territory existing: faction elimination, extinction of followers or a dissolved religion change no `GodRecord` field except through an explicit command. A god with no followers can still act (on anyone), recover by conversion, migration or resurrection (D5.5); **the sim never regenerates a population** for it. |
| G3 | A god owns nothing: no `OwnerKind.God` exists. Offerings belong to the receiving organisation or are consumed (`ConsumeReason.Offering`). Religious authority, political authority and ownership stay apart (brief §4). |
| G4 | `GodCreated` is emitted for every god on the first divinity tick (the founders' `NpcSpawned` pattern; StartGame has no event buffer). |

### 2.1 Player seat → GodId (the human controls a god, never a faction)

| Setup field (F1, `JsonProperty(DefaultValueHandling.Ignore)`, so stage 3 core hashes are unchanged) | Meaning |
|---|---|
| `GameSetup.Control = PlayerControl.Stage3Faction` (0, default) | **Stage 3 mode** (temporary, kept for every existing scenario and test): `Setup.Human` is a faction the person plays with the stage 3 HUD. If `Divinity.Enabled = 1` the HUD may also send divine commands for `HumanGod` (developer and test use). |
| `GameSetup.Control = PlayerControl.GodOnly` (1) | **God-only mode** (D5.1, D5.29): `Setup.Human` must be `NoHuman` (`Create` throws otherwise), so every faction is seated by a faction policy; `Divinity.Enabled` must be 1. `GodSim.Submit` refuses an **external** (`!InStep`) `FactionCommand` with `DivineReasons.GodOnly` ("Governments decide for themselves."). Faction policies submit inside `Step` and are unaffected. |
| `GameSetup.HumanGod` (int, 0 = none / watch) | The god whose seat is `Human` at the start. All other gods start `Ai` with the `policies.god` assignment, default `IdleGod` (section 12). |

The seat is **sim state** (`GodRecord.Seat`), not a host flag, because AI-god commands are recomputed in replay and depend on which gods an AI drives. Sandbox switching is the logged command `SwitchGod { From, To }` (moves the `Human` seat; refused outside sandbox purpose by the host and with `Divinity.Mode` not affecting it). The host maps its single input seat to `HumanGod`'s current holder; the HUD stamps `DivineCommand.God` with it. The stage-3 faction AI (`AiBrain`) remains the temporary government of every faction until C10's government organisations take its decisions over (listed as technical debt; D5.6 forbids treating it as the final design).

---

## 3. Faction identity

Unchanged ids and meaning (`Faction` enum, `FactionInfo.Max = 6`). `Npc.Faction` keeps its M3 meaning (the community a person was born or founded into). D5 adds, and keeps separate (D5.15):

| Concept | Where | Owner | Never stands in for |
|---|---|---|---|
| Community of origin | `Npc.Faction` (existing) | M3/M4 | citizenship, religion |
| Political citizenship | `CivState.Citizenship` spells per NpcId (state organisation `OrgId` of kind Government, or the `FactionId` until C10) | C5 | faction of birth, religion |
| Religious affiliation | `DivinityState.Doctrine.Affiliations` spells (NpcId, ReligionId, optional religious `OrgId`), several per person allowed | D6 | citizenship, belief |
| Cultural membership | C10 culture records (later) | C10 | any of the above |
| God association | `fact_god_faction` spells (warehouse) and `GodRecord.OriginFaction` | D1 | authority over the faction |

A change of one never changes another automatically (scenario 16, test `Affiliation_Change_Keeps_Citizenship`).

---

## 4. Persistent NPC identity, bodies, avatars and the dead

### 4.1 Bodies

```csharp
// Population/Bodies.cs (F1, frozen)
public enum BodyKind : byte { Person = 0, Avatar = 1 }            // 2+ reserved (animals, later: brief §6 Mode C)
// Npc gains (F1): public BodyKind Kind;  public int God;  public long BioAgeOffset;   all JSON-ignored at default, hashed only when non-default
public static class Bodies {
    public static bool IsPerson(Npc n) => n.Kind == BodyKind.Person;
    public static bool IsLivingPerson(Npc n) => n.Alive && n.Kind == BodyKind.Person;
    public static long AgeTicks(Npc n, long tick) => tick - n.BornTick - n.BioAgeOffset;   // biological age (resurrection, rejuvenation)
    public static void Damage(GodState s, EventBuffer ev, Npc body, int injuryMicro, DamageCause cause, int actor, int intervention); // dispatch: person → injury, Kill at 0; avatar → D5
    public static void Heal(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention);                     // dispatch: injury (M3 fields), illness (M4 hook), avatar (D5)
}
```

### 4.2 The avatar is a body, not an NPC (decision)

- An avatar is an `Npc` record with `Kind = Avatar`, `God = GodId`, id `AvatarBodyId`, in `PopulationState.Npcs`. It therefore uses, unchanged, the existing movement (`WalkTo`, `NodePather`, step costs), `Goods` (`Holder.Carry(AvatarBodyId)`, reach, room, weight), `Start`/`Act` for every action primitive, and the people snapshot (`PeopleIndex`), so NPCs see, approach, receive from, give to and (once an attack verb exists, C4) attack it. **No second implementation of any action** (brief §9).
- **Manifested** means `Alive = true` and a node; **not manifested** means `Alive = false`, `Node` the last node, empty carry. Every `if (!n.Alive) continue` loop therefore skips an unmanifested avatar for free. The record is created at the god's first `ManifestAvatar` (so a game where no god ever manifests has a byte-identical population part, pin P2).
- It has an `NpcMind` with `LearningMode.Off` (so per-mind day work runs), no M4 `Life`, no household, no hut rights, `Policy = PolicyRegistry.None`. It **never thinks autonomously**: it acts only on control inputs (section 11, `ControlKind.AvatarBody`).
- Its body: no hunger, fatigue, cold, ageing or illness (D5.9): `Body()` skips the drive updates for `Kind != Person` through the F1 site. It has `Health` and `Injury`; damage goes through `Bodies.Damage` → D5: at `Health <= 0` the avatar is **destroyed, not killed**: no `NpcDied`, no estate, no `Kill`; carried lots go to the ground with `MoveReason.AvatarDrop` (ownership unchanged), `AvatarDestroyed`, the god returns to divine existence (D5.14). `PopulationModule.Kill` throws on a non-person body.
- Ownership: an avatar never becomes an owner. Its pick-ups use `OwnerRule.Keep` (ownerless stays ownerless, an owned lot stays owned: taking someone's goods is the same physical take as an NPC's and is classified by C1's theft rule); its gives use `ToReceiver` with basis `Gift`.
- Perception: `SeenPerson.Body = Avatar` (F1), so cognition can tell; whether a person *recognises* it as their god is D6 belief (it may not). `Faction` of the record = `OriginFaction` (cosmetic for perception; `SameFaction` is computed but D6 may override recognition).
- Warehouse: never a `dim_person` row; `fact_avatar_manifestation` spells (D5).

### 4.3 Person-semantics loops (F1 sweep; static test)

Every loop over `PopulationState.Npcs` whose meaning is "people" uses `Bodies.IsLivingPerson`. F1 switches at least: `PopulationState.AliveCount/AliveOf`; `PopulationSystem.Tick` (`Body` drives part only; `Think` is replaced by the control path for avatars), `DayStart` person-day rows (`NpcDay`), `PersonDay`; `LifeCycleSystem` (ageing, fertility, mortality, illness, immigration, `PersonDayLife`); household formation, partnership, care and estate candidates (M4 `AddCandidates` and `FillPerson` skip `People[i].Body != Person` as targets); M5 exchange, gift, recognition and `PersonDaySocial`; `PopulationInvariants` population counts; warehouse person mappers; `FactionKnowledgeView` population figures; victory inputs. Test `Static_PersonLoopsUseIsPerson` scans `Sim/**` for `foreach (var * in *.Npcs)` / `for (... Npcs.Count ...)` and requires `IsPerson`/`IsLivingPerson` or an allow-list entry with a reason (hashing, `Find`, save). Neutral while no avatar exists.

### 4.4 Death, archive and resurrection (D7; F1 lands the hook)

- `PopulationModule.Kill` is unchanged except that, **just before** `npc.Mind = null`, it calls `PopulationExtensions.ArchiveMind(s, npc)` → `DivinityHooks.ArchiveMind` (D7). With `Divinity.Enabled = 0` or `Divinity.Resurrection.Archive = Off` nothing happens, so the cognition part keeps its `-1` for the dead (neutral).
- **Archive** (`DivinityState.Archive`, D7): `ArchivedLife { int Npc; int LifeNo; long DiedTick; DeathCause Cause; int Node; long AgeTicksAtDeath; AssociationStore Store; KinMind Kin; SocialMind Social; DivineMind Divine; BeliefStore Beliefs (optional by tuning) }`, sorted by NpcId, one current entry per person. **Dormant**: no system iterates it per tick; it is saved and hashed (in `divine`). Retention `Divinity.Resurrection.Archive = Off | All | RecentDays` with `ArchiveMax` and deterministic eviction (oldest `DiedTick`, ties by id), so generations-old resurrection is possible when configured and costs memory only.
- **Resurrection** (`Resurrect { God, Npc, Variant, Node }`, D7): validates the record is dead, a person, archived; restores the **same `Npc` record and NpcId** (`Alive = true`, mind from the archive, `Health`/`Nutrition`/`Injury` by `ResurrectionVariant` Lesser / Full / Restoration), `DiedTick = 0`, `DeathCause = None`, `BioAgeOffset += Tick − DiedTick` (biological age = age at death; `Rejuvenate` is a separate miracle that adds to `BioAgeOffset`), `Household = 0`, `HomeHut = 0`, `Stage` from biological age; new keyed streams are **not** made (the record's saved streams continue). Emits `NpcResurrected` (with `LifeNo`).
- **Not reversed** (D5.13): estates stay with heirs; partnerships, care arrangements, employment contracts, household membership and hut rights are not restored; other people's memories of the dead are theirs (M5 may have evicted them); `NpcDied` and the warehouse death stay. **Warehouse:** `dim_person.death_*` means the **first** death; every death is a `fact_death` row with `life_no`; `fact_life_spell` holds lives (DATA_WAREHOUSE §12.3, confirmed).
- M4's ageing must use `Bodies.AgeTicks` (F1 edits `LifeCycle.cs`, `HouseholdHooks.cs`, `HouseholdActs.cs`, `PopulationKnowledge.cs`, `PopulationSystem.PersonDay` from `cal.AgeMilli(npc.BornTick, …)` to the offset form; neutral at offset 0). `NpcDied.AgeTicks` stays calendar age.
- Population conservation (invariant P-C): `living persons = founders + births + immigrants + resurrections − deaths − emigrants`, with resurrections counted in `DivinityState` (not `PopulationStats`, which is hashed in `population`).

**Required tests (D7):** `Resurrect_SameIdAndMind` (store, kin and social memory byte-equal to the archive), `Resurrect_EstateStaysWithHeirs` (I-BS.7 id multiset unchanged; C1–C5 hold), `Resurrect_NoHouseholdOrPartnerRestored`, `Archive_Dormant` (archive size does not change ticks/second beyond a bound), save/load in the middle of an archive and after a resurrection, `DeathTwice_LifeNo` warehouse reconciliation.

---

## 5. Divine power: Mythic (D1)

| Item | Contract |
|---|---|
| Unit | integer **milli-Mythic** (`long`) in `GodRecord.Mythic`, `MythicMax`. Costs and rates in tuning are whole Mythic × 1000. |
| Modes (`Divinity.Mode`, D5.26, D5.27) | `Standard` (max, regeneration, costs, upkeep, cooldowns), `Unlimited` (nominal costs recorded on every spend, nothing charged, balance held at max), `Disabled` (gods and records exist; every divine command refused with `divinity_disabled`, state untouched: pin P3). Per-god override `GodRecord`-level is reserved (scenario option later). |
| Regeneration | fixed (brief §7), credited at **day start** by `DivinitySystem` (`MythicCredited`), capped at max. No worship feedback in this run (later D6/C10 may add a channel through an amendment). |
| Spending | only `Mythic.TrySpend(s, ev, god, kind, costMilli, intervention)` (D1): checks mode, balance and cooldown; debits; emits `MythicSpent`; sets `CooldownUntil[kind]`. A command's `Validate` calls `Mythic.CanSpend` (no mutation); `Apply` calls `TrySpend` first and applies nothing if it fails. |
| Upkeep | continuous costs (compulsion if configured, possession, avatar) charged **per tick** by `DivinitySystem` (`Mythic.ChargeUpkeep`, aggregated `MythicUpkeepCharged` per god per day, HV); when a god cannot pay, the dependent control ends **that tick** with reason `MythicExhausted`, in ascending InterventionId order. |
| Invariant M1 | `0 ≤ Mythic ≤ MythicMax` always; per god `initial + Σ credited − Σ charged = balance` exactly (from events: `MythicTrace`, F1). |
| Same rules (D5.26) | Human and AI gods use the same commands, costs, cooldowns and validation; no code reads `Seat` when charging. |

Tuning: `Divinity.Mythic.{Start, Max, RegenPerDay}`, `Divinity.Costs.<CommandName>` and `Divinity.Upkeep.<Kind>PerTick`, `Divinity.Cooldown.<Kind>Ticks`. Balancing is out of scope (brief §7).

---

## 6. Physical locations, object affordances, action primitives

Unchanged from WORLD_ONTOLOGY §§1–5 and SHARED_CONTRACTS §2.6: hexes are geography and jurisdiction, nodes are where things are; seeing lets a body *choose* to go, only reach lets it *act*; permission is a query, never a `Goods` check.

D5 adds:

| Item | Contract | Owner |
|---|---|---|
| **Divine targets** | `DivineTarget { DivineTargetKind Kind; int Id; int Node; int Radius }` with kinds `Npc, Household, Org, Location, Lot, Bin, Source, Hut, Building, Commandment, Self` (Self = the god's own avatar). Group kinds expand at `Apply` to recipients in ascending NpcId, capped by `Divinity.Instruction.MaxRecipients` | F1 |
| **Consecrated places** | `ConsecratedPlace { Id (= InterventionId), God, Node, Radius, SinceTick }` in `DivinityState.Miracles`; perceived through the F1 site `SenseExtra` as `ObjClass.Shrine` (80); a place, never an owner | D3 (create), D6 (meaning to minds) |
| **Sense seam** | `PopulationExtensions.SenseExtra(s, npc, sight, g, near, n)` generalises I-BS's `SenseBins`: M4 bins, then Civ (assets, C1), Orgs (org buildings and stores, C6), Divinity (shrines, D3; manifested avatars are people, not sensed things) | F1 |
| **Affordances** | new object kinds give their affordances only through their owner's hook into `Affordances.Of`; `Permits.Believed` remains the one permission query; C1 extends it with ownership beliefs, C9/C10 with laws, D6 with adopted commandments as *believed norms* (never a block) | as listed |
| **Action primitives** | every divine, compelled, possessed or avatar action is one of the existing primitives (MoveTo, Take, Drop, Give, Store, Withdraw, Consume, Use, Gather, Inspect, plus the new verbs other packages add in section 14) started through `PopulationSystem.Start` and performed by `Act`; miracles are the only divine effects that are not primitives (section 8) | all |

### 6.1 The divine action vocabulary (`DivineAct`, brief §9; table owned by D2)

`MoveTo 1, Approach 2, Inspect 3, Gather 4, Take 5, Drop 6, Give 7, Eat 8, UseTool 9, Sleep 10, Work 11, Construct 12, Trade 13, Cooperate 14, Communicate 15, Protect 16, Retreat 17, Attack 18, Teach 19, Treat 20, InstitutionalAction 21` (22–31 reserved).

`DivineActs.Map(act, target) → (learning verb, NpcVerb, IntentTarget)` maps each to an existing candidate shape (e.g. MoveTo → `Verb.Approach` 7 on `IntentTarget.Node`; Gather → `Gather`/`ForageAt`; Take → `PickUp`/`TakeFromStore`; Give → M5 `Give` 48; Eat → `Eat`; Construct → `Build`; Retreat → `Flee`; Trade → `SayOffer`; Teach/Treat → C7 verbs 108/107; InstitutionalAction → C6 verbs). An act whose verb is not yet implemented on the branch is **refused at Validate** with `act_unavailable` (never faked). `Verb.Approach` gets its `Act` case from D2 through `TryAct` if M3 has none.

---

## 7. Ownership, possession, inventories and resource conservation

### 7.1 Ownership and possession (extends SHARED_CONTRACTS §3)

| Change (F1 values) | Use |
|---|---|
| `OwnerKind.Org = 7` (Id = OrgId) | **every** organisation owns through this kind (state, private, cooperative, communal, religious: the model is the org's attribute `OwnershipModel`, D5.17; confirms ORGANISATIONS). `OwnerKind.Enterprise` (4) is never written (deprecated). `State` (5, Id = FactionId) is a **temporary alias** resolved by `OrgState.GovernmentOf(faction)` to the faction's government org once C10 creates it (until then it means the stage 3 polity); new code writes `Org(id)`; listed in the DEV_LOG temporary-mechanisms table, removed by C10. `Group` (3) keeps the community escheat. Governance (`DecisionRule`: OwnerDecides, HeadDecides, MajorityPresent, Consensus, Council) is C6's internal type, not a shared contract |
| `BinKind.OrgStore = 3` (C6), `AssetStore = 4` (C1: a workshop's or productive asset's store), `CommunalStore = 5` (C8), `Shrine = 6` (D6: offerings at a consecrated place) | created only through `Goods.NewBin` by their owner package |
| `MoveReason.Deliver = 10` (C1 haul), `AvatarDrop = 11` (D5), `Confiscate = 12` (C9); 13–31 reserved | |
| `OwnershipBasis.Forfeited = 14` (C2: collateral taken on default; confirms INSTITUTIONAL_SERVICES), `Miracle = 15` (D3), `Wage = 16` (C3), `Loan = 17` (C2), `Repayment = 18` (C2), `Levy = 19` (C10), `Restitution = 20` (C9), `Offering = 21` (D6), `OrgContribution = 22` (C6), `Purchase = 23` (C8), `Confiscated = 24` (C9); 25–31 reserved | |
| `Ware.Herbs = 32` (F1.17; sources and remedy use C7) | the one new personal ware of this run; `PersonalCount` 40 leaves 33–39 for later appends without another layout change |

All of these values are landed **once, by F1**, behind the neutrality pins: no lot, bin or event uses them until a switch is on and a package writes them, and the ledger hash keeps its old layout (F1.17).

Rules: only `Goods` writes lots, holders and owners (the F0 static test is extended to `Sim/Civ`, `Sim/Orgs`, `Sim/Divinity`). Theft (C1) is a `PickUp`/`Withdraw` of an owned lot with `OwnerRule.Keep`, classified by the sim with `Permits.Truth` into `TheftCommitted` (code 274); from C1, invariant C4's "ground lots are ownerless" is relaxed (O7). **No remote transfer exists:** no divine, organisational or financial operation moves an existing lot between holders except a `Goods.Move` by a body within reach (brief §16). Financial claims are obligations (C2) and create no goods (brief §21).

### 7.2 Resource conservation with the miracle ledger (D3)

- A miracle that **creates** goods calls `Goods.Produce(..., ProduceReason.Miracle, actor = 0, source = InterventionId)` into a holder at the target: the ground at the target node (ownerless), a target body's carry (owner `Npc(target)`, basis `Miracle`, no goods reward: §2.3 pays only foraging), or a bin (owner the bin's). It never creates goods in a place nobody can reach.
- A miracle that **destroys** goods calls `Goods.Consume(..., ConsumeReason.Miracle, actor = 0, refId = InterventionId)` on lots at the target. Smiting a **resource source** lowers `ResourceSource.Stock` (sources are not lots; the change is `MiracleEffectApplied` with before/after stock) through a D3 function in `PopulationModule`'s source API; smiting a **building or hut** uses the owning module's existing destruction path (territory `BuildingDestroyed`, a hut-removal function D3 adds to `PopulationModule` that first empties its bins by `Goods`).
- Every such call is paired with exactly one `ResourceCreated` or `ResourceDestroyed` (`LedgerId`, ware, count, lot, holder, InterventionId) and booked in `DivinityState.Miracles.Ledger` (per god per ware created/destroyed totals).
- **C1 holds unchanged** (the ledger reason slots exist): per ware `Σ Produced − Σ Consumed = Σ holdings`. **C7 (new):** `Ledger.ProducedBy(Miracle, w) = Σ ResourceCreated(w)` and `ConsumedBy(Miracle, w) = Σ ResourceDestroyed(w)`, at every day boundary and after save/load. **C8 (new):** no lot ever changes holder without an `ItemMoved` by an actor within reach (C6), divine control included.

---

## 8. Miracles (D3; D7 for resurrection)

A miracle is a `CastMiracle { God, Kind, Target, Magnitude }` (or `Resurrect`) command whose `Apply` changes truth through the owning module's functions and then emits `MiracleCast` (one) and `MiracleEffectApplied` (one per affected thing, with before/after values). A visual effect without a state change is forbidden (brief §10); U packages animate only these events.

| `MiracleKind` | Effect (only through) | Learning and witnesses |
|---|---|---|
| `HealInjury 1`, `HealDisease 2` | `Bodies.Heal` (M3 injury fields; M4 illness through its hook) | the patient: drive relief through the normal reward channel; `SelfFeature.Healed` (49) for `Miracles.MemoryDays` |
| `Bless 3`, `Protect 4` | a timed modifier record `Blessing { Npc, Kind, Until }` read by `Bodies.Damage` (protect) or by M4 health rolls (bless) through their hooks | `SelfFeature.Blessed` (50) |
| `SmitePerson 5` | `Bodies.Damage(..., DamageCause.Divine)`; at 0 health `Kill(DeathCause.Violence)` (no new death cause: `DeathsByCause` length is hashed) with the attribution in `MiracleEffectApplied` | Pain/Fear through the normal channel (`Learning.Injury`), `SelfFeature.Smitten` (48); every person within sight who attends gets `SituationKind.SmiteWitnessed` (83) and a D6 belief update. **The sim never tells the victim why**: the existing trace credit assigns the punishment to whatever its recent decisions were (brief §14) |
| `SmiteBuilding 6`, `SmiteSource 7` | owning module destruction / stock change (7.2) | witnesses as above |
| `CreateResource 8`, `DestroyResource 9` | 7.2 | — |
| `RevealKnowledge 10` | `BeliefStore.Upsert` into the target's own mind (layer 6) with the true node of a place or thing; never into an institution's records (D5.28) | the belief may later be wrong (stale), like any belief |
| `RevealTechnology 11` | reserved (needs a skills store, C7) — refused `act_unavailable` | |
| `Consecrate 12` | `ConsecratedPlace` (section 6) | perceived as `ObjClass.Shrine` |
| `Rejuvenate 13` | `BioAgeOffset` (D7) | |
| `Area 64 + kind` | the single-target effect over every eligible thing within `Target.Radius`, ascending id; draws `miracle-effect(intervention, step)` only | |
| `Weather`, `Terrain` | reserved (need ecology/terrain support) | |

`Resurrect` is its own command type (section 4.4) of `InterventionKind.Miracle`.

---

## 9. Knowledge views: who may read what

The nine layers of INFORMATION_AND_MEDIA_DESIGN §12.1 are **confirmed**. F1 adds `enum KnowledgeLayer : byte { Truth = 1, Observed = 2, Communicated = 3, InstitutionRecord = 4, Published = 5, Belief = 6, FactionKnowledge = 7, DivineObservation = 8, Analytics = 9 }`; every inspector row and divine-view item carries one.

| Code | May read | Never reads | Enforced by |
|---|---|---|---|
| `INpcPolicy`, mind hooks (`*MindHooks`) | `NpcObservation`, `MindView`, `ThinkScratch` (layers 2, 3, 6) | `GodState`, any module state, any view of 4, 7, 8, 9 | G4.1 reflection test (exists), extended to `CivMindHooks`, `OrgMindHooks`, `DivinityMindHooks` |
| `IInstitutionPolicy` (C6) | `InstitutionView` built **only** from the org's own records (`OrgRecords`, layer 4) and its member-manager's own mind | `GodState`, other orgs' records, 8, 9, `ITrueStateOverlay` | I-D5-1 |
| `IFactionPolicy` | `FactionKnowledgeView` (unchanged) | 1, 8, 9 | I1, I-D5-1 |
| `IGodPolicy` (human seat's AI helpers too) | `IDivineView` filled by `DivineViewFiller` from the god's `DivineObservationProfile` (`Omniscient`, `Perceived`, `Reported`, `Blind` per domain, channels `Avatar, Possessed, Prayer, ConsecratedSite, Followers, Records, Sight`) | `GodState`, 4 and 7 directly, 9 | I-D5-1, I-D5-2 |
| Human divine UI (`God.Game`) | `IDivineView`; inspector cards with their layer tags; `ITrueStateOverlay` only where `TrueStateOverlayRules` allows; read-only SQL (layer 9) in sandbox/developer purpose | writing state (it sends commands) | CLAUDE.md rule; existing view tests |
| Sim systems, `*Hooks` (sim side), view fillers, recorders | layer 1 | the event buffer as input to a rule; the warehouse | existing "no sim rule reads the event buffer"; asmdef (Sim does not reference Recording/Runner) |

Rules: divine knowledge never becomes NPC or institutional knowledge except through a physical in-sim step (a miracle writing a belief, a message perceived, a record written by a person); the record names the person as source (D5.28). Institutional knowledge reaches a god only through a channel or `Reported` mode. `IDivineView` is the interface of INFORMATION §12.3 (confirmed; D1 owns it; F1 lands it with `God`, `Profile`, `AsOfTick` and `TryPerson` only, other members added by D1/U2 through amendment-free additive edits in `DivineView.cs`).

---

## 10. Command validation and the divine intervention taxonomy

### 10.1 Base rules (all packages)

- Every divine and player input is a `Command` through `GodSim.Submit` (validated now, applied at the next tick boundary in submission order, re-validated in `Apply`, `CommandApplied`/`CommandRejected` with a reason code). No back door (CLAUDE.md "bots and people use the same commands").
- `abstract class DivineCommand : Command { public int God; }` (`PlayerId` unused, kept 0). Every concrete class is plain data with public fields (they are saved in `Pending`, hashed in `pending`, logged in the replay as JSON). `Validate` never mutates; `Apply` re-checks, spends Mythic first (`TrySpend`), opens the intervention (`InterventionId`), then applies.
- Common refusals (`DivineReasons`, F1): `divinity_off` (no state), `divinity_disabled` (mode), `no_such_god`, `mythic_short`, `cooldown`, `bad_target`, `target_dead`, `not_a_person`, `act_unavailable`, `already_controlled`, `control_limit`, `god_only` (faction command in god-only mode), `not_sandbox` (`SetMythicMode`, `SwitchGod`).
- **Authorship of a GodId** (blocker 5). `DivineCommand` derives from `Command`, never from `FactionCommand`, so `FactionCommandSet`, `AiDirector` admissibility and the HUD's faction set are unchanged. `GodSim.Submit` (F1 gate, next to the god-only gate) refuses an **external** divine command whose `God` does not hold the `Human` seat (`not_your_god`), and `GodDirector` drops an in-step command whose `God` is not the policy's own `Ai`-seated god (`DivineCommandSet.Admissible(c, god)`). Configuration commands (`SwitchGod`, `SetMythicMode`, `SetGodPolicy`, `SetDivineProfile`) are external only. A scripted test god is therefore given the `Human` seat (origin `schedule`).

### 10.2 The six intervention types are six families of distinct command and record types (never one generic type, brief §8)

| `InterventionKind` | Commands (names confirm HEADLESS_SIMULATION §12.2) | Record | Owner | Effect path |
|---|---|---|---|---|
| `Suggestion = 1` | `IssueSuggestion { God, Act, Target, Scope, ExpireTicks }` | `Instruction` (Kind Suggestion) | D2 | a **perceived divine message** (10.3); the NPC decides |
| `Command = 2` | `IssueCommand { … same fields }` | `Instruction` (Kind Command, higher authority weight and cost) | D2 | same path, more weight; still refusable |
| `Compulsion = 3` | `IssueCompulsion { God, Act, Target, ExpireTicks }`, `CancelCompulsion { God, Intervention }` | `ControlEntry` (`Compelled`) + `Instruction` | D2 | intent override (section 11), existing primitives and reach |
| `Possession = 4` | `BeginPossession { God, Npc }`, `EndPossession { God }`; inputs by `DivineControlInput` (11.3) | `ControlEntry` (`Possessed`) | D4 | control swap (section 11) |
| `Commandment = 5` | `RevealCommandment { God, Template, Rules[], Importance, Recipients (DivineTarget) }`, `ReviseCommandment { God, Commandment, Rules[], Importance }`, `RevokeCommandment { God, Commandment }` | `Commandment` (section 13) | D6 | revealed to recipients as layer 3/6 knowledge; never enforced |
| `Miracle = 6` | `CastMiracle { God, Kind, Target, Magnitude }`, `Resurrect { God, Npc, Variant, Node }` | `MiracleRecord` | D3, D7 | truth changes (section 8) |
| `Avatar = 7` (manifestation, not one of the brief's six but its own family) | `ManifestAvatar { God, Node }`, `DismissAvatar { God }`; inputs by `DivineControlInput` | `AvatarSpell` | D5 | body control (section 11) |
| (control input, not an intervention) | `DivineControlInput { God, Body, Kind (Heading, MoveTo, Stop, Action), Heading, Gait, Node, Act, Target }` | updates the `ControlEntry` | F1 (handler in `Control.cs`; registered by D4/D5 when their spells exist) | 11.3 |
| configuration | `SwitchGod { From, To }`, `SetGodPolicy { God, Policy }`, `SetMythicMode { Mode }`, `SetDivineProfile { God, Profile }` | `GodRecord` fields | D1 | logged commands (sandbox) |

Instruction scope: `DivineScope { Individual, Household, Group (list of ids), Org, Location }`. **An institution-level instruction is relayed** (`InstructionRelayed`, one per recipient; this name replaces `CommandFanOut`) to the office-holders whose powers cover it in ascending NpcId, plus members present at the meeting place for a meeting rule (C6 supplies `Orgs.RelayRecipients(org, act)`; D2 emits the event and delivers), each receiving an individual message through its own perception; the institution "decides" only through those individuals and its own policy (D5.2; scenario 15).

### 10.3 How a suggestion or command enters NPC cognition (D2, reusing M5 comms)

1. `Apply` (tick t) opens the `Instruction` and, for each recipient in ascending NpcId, decides **perception**: the recipient must be a living person (asleep recipients perceive it on waking: dream/vision, tuning) with draw `divine-perceive(npc, intervention)` against `Instruction.PerceiveChance`; emits `CommandPerceived` (or nothing).
2. A perceived instruction is **decoded** into the recipient's `DivineMind.Inbox` (cap 4, oldest evicted) as `DecodedDivine { int Intervention; int God; InterventionKind Kind; DivineAct Act; MsgTopic Topic; MsgTime Time; long Expires; byte Clarity }`, using M5's decode-noise model with label `divine-decode(npc, intervention)` (a misread act or topic is possible; the `Mismatch` truth flag is analytics only). It is **read at the recipient's first think after t** (M5 rule 3), through `DivinityMindHooks.FillDivine` into `ThinkScratch.Divine[2]` (F1; separate from M5's frozen `Heard`).
3. `DivinityMindHooks.AddCandidates` (quota 4, after Orgs) adds the walk-then-act candidate the decoded instruction names, via `DivineActs.Map`; the decoded topic counts as **communicated** knowledge, so `Thinker.Known` accepts it (F1 extends `Known`: a node, person or object named by a `ThinkScratch.Divine` entry). Its target features include `Feature.Divine(Suggested 1 | Commanded 2)`.
4. The **divine term** `DivinityMindHooks.DivineTerm(sc, cand)` scores every candidate that matches an inbox entry: `clamp(authority(kind) × belief × max(trust, fear) / 1e6, ±2000)` (D2) `+ clamp(commandment term, ±2000)` (D6, section 13). It is a new score term: F1 adds `CandidateScore.Divine`, `OpportunitySlot.Divine`, `DecisionRecord.Divine`, `Opportunity.Divine`, adds it to `AssociativeNpcPolicy`'s total (0 by default: identical choices), and hashes it only for minds with a `Divine` part (the I-BS 12.34 pattern). `MindView.Layout` stays 1 (`CandidateTerms` unchanged) until a package exports it.
5. The NPC **decides with its own policy**. D2 emits `CommandResponded { Intervention, Npc, Response: Followed | Ignored | Refused (chose another candidate while the instruction was its own candidate) | Lapsed (expired unread or unfollowed) | Misread }` at the think when it starts a matching intent, or at expiry.
6. **Learning**: the candidate's `Divine` feature is in the context, so the ordinary TD credit learns whether "doing what the god said" pays (brief §14). D2 adds no reward of its own except the configured instinct prior for `Commanded` (tuning, may be 0).

---

## 11. Temporary control authority (F1 implements; D2, D4, D5 use)

```csharp
// Divinity/Control.cs (F1, frozen)
public enum ControlKind : byte { Self = 0, Compelled = 1, Possessed = 2, AvatarBody = 3 }
public struct ControlEntry {
    public int Body; public ControlKind Kind; public int God; public int Intervention;
    public long SinceTick, ExpiresTick;                 // 0 = no expiry
    public DivineAct Act; public DivineTarget Target;   // compulsion's action; possession/avatar: the last discrete action input
    public byte Heading, Gait;                          // the held movement intent (possession, avatar); Heading 64 = stop
    public byte Status;                                 // Pending, Running, Blocked, Done
    public bool Started;                                // the override has started an intent at least once
}
public sealed class ControlTable { public List<ControlEntry> Entries; }   // DivinityState.Control; sorted by Body; only non-Self entries stored
public static class Control {
    public static ControlKind Of(GodState s, int body);
    public static bool TryAcquire(GodState s, EventBuffer ev, in ControlEntry e, out string reason);
    public static void Release(GodState s, EventBuffer ev, int body, ControlEndReason why);
    public static void Sweep(GodState s, EventBuffer ev);   // expiry, death, exhaustion: called by DivinitySystem each tick
}
```

### 11.1 Who may drive a body, and precedence

| Kind | Who | On which bodies | Precedence | Limit |
|---|---|---|---|---|
| `Self` | the body's own `INpcPolicy` | persons | lowest (default) | — |
| `Compelled` | one god | living persons not possessed | over Self | one compulsion per body; a second god's compulsion is refused `already_controlled`; the same god's replaces its own (`Superseded`) |
| `Possessed` | one god | living persons (foreign citizens allowed; animals reserved) | over Compelled: beginning a possession **ends** a compulsion on that body (`Superseded`) | `Divinity.Possession.MaxPerGod = 1`; another god's possession refused |
| `AvatarBody` | its own god | only its avatar body | the only kind an avatar has | one avatar per god |
| Embodiment | — | — | a god has at most one embodied focus: `ManifestAvatar` ends its possession and `BeginPossession` dismisses its avatar (`Superseded`) | tuning may relax later |

### 11.2 End conditions (`ControlEndReason`)
`Completed` (compulsion: the compelled intent started and its action ended `Done`, through `ActionEnded`), `Timeout` (`ExpiresTick`), `Cancelled` (`CancelCompulsion`, `EndPossession`, `DismissAvatar`), `Death` (`Kill` → `DivinityHooks.OnDeath` releases first), `MythicExhausted` (upkeep, section 5), `Superseded`, `Destroyed` (avatar). Each emits its family's end event (`CompulsionEnded`, `PossessionEnded`, `AvatarDismissed`/`AvatarDestroyed`) exactly once; a spell's start and end pair exactly (invariant CT1).

### 11.3 How control reaches the body (the F1 sites in `PopulationSystem`)
- **Think cadence:** `if ((Tick + Id) % ThinkEvery == 0 || PopulationExtensions.ForceThink(s, npc))`; `DivinityHooks.ForceThink` is true on the tick after a new control input or a new control spell (default false: neutral).
- **The mind still runs:** `Thinker.Think` runs for a possessed or compelled person (perception, beliefs, attention: the NPC "remains conscious", brief §6), candidates are built, and `DivinityMindHooks.AddCandidates` places the controlled intent as a candidate (first divine slot, always present when physically meaningful).
- **Override:** after the policy step, `PopulationExtensions.OverrideIntent(s, npc, ref intent, ref why)`: `Compelled` replaces the policy's choice with the compelled candidate while it exists (it may override hunger and fatigue, D5.7, but not asleep-and-collapsed, busy or physical failure); `Possessed` and `AvatarBody` **skip the policy entirely** (`RngPolicy` not advanced: no exploration draw, HEADLESS §12.2 item 6) and take the input candidate, or continue the running action, or idle. `why.Control = ControlKind` (new `DecisionRecord.Control` field, hashed only when non-zero) and the decision trace carries it.
- **Same primitives:** the chosen candidate goes through `Match` → `Start` → `Act` as any intent; reach, room, possession and paths are checked as for anyone. A compelled action that is physically blocked sets `Status = Blocked` (`CompulsionBlocked`), retries at each think, never teleports, never creates goods, and runs to expiry or cancellation.
- **Body continues:** `Body()` (needs, injury, illness, ageing) runs unchanged for persons in every control state; a possessed or compelled person can starve, be hurt or die (D5.8).
- **Learning:** traces open as usual with the `Feature.Divine(Compelled 3 | Possessed 4)` key in the context; rewards scale by `Divinity.Possession.LearnPermille` / `Instruction.CompelLearnPermille` (default 1000). Afterwards the mind keeps `SelfFeature.Possessed` (51) / `Compelled` (52) for `MemoryDays` (the NPC remembers the experience).
- **Movement inputs: one model, `DivineControlInput`** (this overrides both HEADLESS §12.2's 1/64-turn heading and CAMERA_AND_CONTROLS' 16-sector proposal where they differ; it is the lead's preferred "sim chooses the next node, short lease" model):
  - `Kind = Heading`: `Heading` is an integer sector `0 … HeadingSectors − 1` in the world frame (north = board +Z, clockwise), `HeadingSectors = Divinity.Control.HeadingSectors` (default **16**; must be 4, 8, 16, 32 or 64), `Gait` 1 walk / 2 run. `Kind = MoveTo` carries a `Node` (click-to-move: the sim paths there with `NodePather`). `Kind = Stop`. `Kind = Action` carries `Act` and `Target` (pick up, drop, give, use, gesture…, validated by reach as for anyone).
  - **Lease**: a `Heading` input is valid for `Divinity.Control.LeaseTicks` (default **3**) ticks from the tick it applies; `MoveTo` and `Action` hold until done or replaced. With `Stop` or an expired lease the body finishes its current edge (bodies never stop mid-edge) and halts.
  - **The sim chooses the node**: each tick while the lease holds, `Control` (run by `DivinitySystem` before the population tick) picks, among the passable lattice neighbours of the body's node, the one whose direction is nearest the sector's centre angle (integer angle table, ties by lower node id), and makes it the body's `MoveTo` candidate; the step itself is `WalkTo` at the body's speed. Gait 2 equals walk until a run speed exists (21.6).
  - **Submission (Game, U1)**: the camera yaw plus the move axis give a world direction, quantised (with hysteresis) to a sector; the host submits one `DivineControlInput` only when the kind, sector or gait changed, or when the lease would expire next tick; at most one per body per tick; last input wins within a tick. The camera, frame rate and time scale never reach the sim. Commands sent while paused apply at the next tick that runs.

### 11.4 Order within a tick (confirms HEADLESS §12.2 and DATA_WAREHOUSE §12)

| Order | System | Owner | Work |
|---|---|---|---|
| (tick head) | commands | all | divine commands apply here: miracles change truth, instructions are delivered, control spells open |
| **230** | `DivinitySystem` | D1 (+ D2, D4, D5, D7 parts by partial methods in fixed order) | day start: `GodCreated` once, `MythicCredited`, `MythicBalanceDay`, instruction expiry; per tick: upkeep, `Control.Sweep`, movement-intent resolution for possessed and avatar bodies |
| 240 | `LifeCycleSystem` | M4 | unchanged |
| 245 | `SocialSystem` | M5 | unchanged |
| 250 | `PopulationSystem` | M3 + hooks | unchanged order; control sites of 11.3 |
| 255 | `EstateSystem` | M4 | unchanged |
| **257** | `CivSystem` | C1 (+ C2, C3, C5 partials) | day start: obligations due, contracts, citizenship; per tick: none by default |
| **260** | `OrganisationSystem` | C6 (+ C7–C10 partials) | day start: org day, request expiry, records; per tick: service queue upkeep |
| 300–400 | military, trade, factions | existing | |
| 900 | `AiDirector` (faction policies) | existing | |
| **905** | `InstitutionDirector` (`IInstitutionPolicy` seats, host-attached like `AiDirector`) | C6 | |
| **910** | `GodDirector` (`IGodPolicy` for `Seat == Ai` gods, host-attached, reads seats from state each think) | D1 | |

`Kill` order (extends SHARED_CONTRACTS 9.2): mark dead → `NpcDied` → `HouseholdHooks.OnDeath` → `SocialHooks.OnDeath` → `CivHooks.OnDeath` → `OrgHooks.OnDeath` → `DivinityHooks.OnDeath` (release control, close instructions) → M3 cleanup → `ArchiveMind` → `Mind = null`.

### 11.5 Required tests
`Control_Precedence_Table` (every pair of 11.1), `Compulsion_UsesReachAndPrimitives` (a compelled take of a lot 10 nodes away walks first; `ItemMoved` reach-checked by C6), `Compulsion_BlockedNeverTeleports`, `Compulsion_OverridesHunger`, `Possession_BodyContinues` (hunger rises, death ends possession), `Possession_NoPolicyDraws` (`RngPolicy` state unchanged during possession), `Possession_ReturnsSameIdentity`, `Possession_LearnsWithControlTag`, `ControlInput_LeaseAndReplay` (a recorded stick sequence replays exactly; a lapsed lease halts at the edge end), `Exhaustion_EndsSameTick`, `Control_SaveLoadMidSpell`.

---

## 12. Gods as policies (D1)

```csharp
public interface IGodPolicy : IPolicy { void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands); }   // PolicyLevel.God = 4
```
Confirms AI_TRAINING §14: `IdleGod` (never acts; control for P2) in F1, `StewardGod` (scripted) in D1 Tier 2. `GodDirector` (910) seats every god whose `Seat == Ai` with `policies.god` (`PolicyAssignment` scope = GodId), thinks every `Divinity.AiThinkTicks` staggered by god id, draws from `god-policy(god, tick)`, submits inside `Step` (so its commands are recomputed in replay, not logged; the replay header carries the policy names). A dropped inadmissible command is counted, never applied.

---

## 13. Beliefs and commandments (D6)

### 13.1 Per-NPC divine mind (separate fields, D5.15, brief §13)

```csharp
// Divinity/DivineMind.cs (F1 stub class; D6 fields, D2 inbox) — NpcMind.Divine, null unless Divinity.Enabled; hashed only when present
public sealed class DivineMind {
    public DecodedDivine[] Inbox;  public int InboxCount;          // D2, cap 4
    public GodBelief[] Gods;       public int GodCount;            // D6, cap = god count
    public KnownCommandment[] Known; public int KnownCount;        // D6, cap 8, eviction: lowest salience, ties by id
}
public struct GodBelief { public int God; public short Belief, Faith, Trust, Fear, Loyalty; public long LastEvidenceTick; } // each −1000..1000 or 0..1000, independent
public struct KnownCommandment { public int Commandment, Revision; public CommandmentSource Source; public int FromNpc;
                                 public short Adoption /* −1000 rejected … 1000 adopted */, Salience; public CommandmentRule Interpretation; }
```
- **Belief** (the god exists), **faith** (confidence in its goodness/promises), **trust**, **fear**, **loyalty** are five fields; none is derived from another; there is no obedience score. Updates come only from the NPC's own perceptions (a perceived instruction, a witnessed miracle, a miracle on itself, prayers answered or not, others' communication) with cause refs in `BeliefChanged`.
- **Religious affiliation** is a separate spell record (section 3) and may be several; it neither requires nor implies belief. **Citizenship** (C5) and **culture** (C10) are elsewhere. M5's `SocialMind` trust/fear are about **people** and stay M5's (D6 never writes `SocialMind`).
- A worship or prayer is an ordinary verb (`Pray` 116, `Worship` 117, `MakeOffering` 118; gestures `Pray` 19, `Worship` 20) with its own affordances (shrine, alone, with others).

### 13.2 Commandments (structured, never free text in the sim)

```csharp
public enum Modality : byte { Required = 1, Prohibited = 2, Encouraged = 3 }
public struct CommandmentRule { public Modality Modality; public byte LearningVerb; public byte ObjClass; public Ware Ware;
    public ApplySelector Applies; public ConditionKind Condition; public int ConditionParam; public DivineTarget Scope; }
public sealed class Commandment { public int Id, God, Revision; public int Template; public CommandmentRule[] Rules; public int Importance;
    public long IntroducedTick; public bool Revoked; public List<CommandmentRevision> History; }   // DivinityState.Doctrine, sorted by Id
```
- Templates (brief §12 examples) are a static table `CommandmentTemplates` (D6) that fills `Rules`; the builder UI edits typed fields only. Player-facing text is generated from the rules (U2).
- **Never enforced:** no code refuses, blocks or forces an action because of a commandment. A commandment affects behaviour only through (a) the D6 part of the divine term for candidates matching an *interpretation* the NPC holds with positive adoption (Required/Encouraged +, Prohibited −, weighted by adoption × importance × condition match), (b) the `Feature.Divine(CommandmentRequired 5 | Prohibited 6 | Encouraged 7)` key so learning can confirm or erode it, and (c) social consequences other NPCs choose.
- Learning, misunderstanding (an interpretation may drop a condition or shift the verb; draw `commandment-interpret`), adoption, rejection, forgetting (salience decay), transmission (M5 `Say(Teach)` / `TopicKind.Commandment` with the interpretation the speaker holds, not the original), conflicts (two adopted rules on one candidate simply add terms).
- **Commandment vs law vs norm:** a commandment is the god's record; a *law* is an organisation's record (C9/C10 `Law { OrgId, SourceCommandment?, Rules, Enforcement }`) adopted through the org's own decision; a *norm* is a person's or culture's learned expectation (C10). Adopting a commandment as law copies the rules at a revision; later revisions do not change the law.

---

## 14. Id ranges reserved by F1

| Space | Taken (base) | Civ (C1–C5) | Orgs (C6–C10) | Divinity (D1–D7) | Reserved |
|---|---|---|---|---|---|
| Learning `Verb` (byte, `Max = 128`) | 0–20, 32–62, 64–95 | 96–103 (Work 96, Lend 97, Repay 98, Hire 99, WorkFor 100, Pay 101; 102–103 spare), 124–126 (C4 Enlist 124, Desert 125; C5 Migrate 126) | 104–115 (Join 104, LeaveOrg 105, RequestService 106, Treat 107, Teach 108, Attend 109, Report 110, Complain 111, Accuse 112, Arbitrate 113; 114–115 spare) | 116–123 (Pray 116, Worship 117, MakeOffering 118; 119–123 spare) | 21–31, 63, 127 |
| Intent `NpcVerb` | 1–6, 20–30, 40–48, 100–169 | 170–178, sentinel `CivReserved = 179` | 180–188, `OrgsReserved = 189` | 190–198, `DivinityReserved = 199` | — |
| `GestureAct` (M5 semantics) | 0–17 | 24 Thank, 25 Greet, 26 Threaten, 27 Alarm (C2: signalling acts NPC_ANIMATION asks for; `Say(act)` verbs 88–91) | 18 Teach (C7), 21 Demonstrate (C7, also avatar), 22 RequestService (C6), 23 Report (C9) | 19 Pray, 20 Worship (D6) | 28–31 |
| `TopicKind` | 0–6 | — | 9 Org, 10 Service (C6) | 7 Commandment, 8 God (D6) | 11+ |
| `FeatureKind` | 1–16, 32 | — | 18 Org (C6) | 17 Divine (D2/D6) | 19–31 |
| `SelfFeature` | 1–8, 16–47 | 64–71 | 56–63 | 48–55 (48 Smitten, 49 Healed, 50 Blessed, 51 Possessed, 52 Compelled) | 72+ |
| `SituationKind` | 1–4, 16–79 | 112–127 | 96–111 | 80–95 (80 DivineSign, 81 AvatarInSight, 82 ConsecratedPlace, 83 SmiteWitnessed) | 128+ |
| `ObjClass` | 1–14, 30, 40–79 | 90–99 (C1 assets) | 100–109 (org buildings, stores) | 80–89 (80 Shrine) | 110+ |
| `NpcGroup` | 1–3, 6–7 (4, 5 reserved) | — | 9 OrgStaff | 8 Clergy, 10 Avatar | 11+ |
| `Role` | 4, 5 (1–3 reserved) | 10 Employer | 6 Priest, 7 Healer, 8 Teacher, 9 Official | — | 11+ |
| Episode-key kind | 0–15, 64 | 16–23 | 24–31 | 32–39 | 40–63, 65+ |
| Candidate quota | M3 16, M4 4, M5 4 | 4 | 4 | 4 | `MaxCand = 36` |

Dispatch order everywhere: **M3 → M4 → M5 → Civ → Orgs → Divinity**. Each D package adds to `DivinityMindHooks` only through its partial file (`DivinityMindHooks.D2.cs`, `.D6.cs`), and F1's dispatcher calls the partial methods in package order; the same for `DivinityHooks` (`OnDeath`: D2 → D4 → D5 → D7; `TryAct`: D2 → D4 → D5).

---

## 15. Domain events

### 15.1 Domains and code ranges (F1; codes allocated in the order listed, spares unused until appended)

`TelemetryDomain` (F1 appends): `Lending = 36`, `Organisation = 45`, `Service = 46`, `Divine = 60`, `Possession = 61`, `Avatar = 62`, `Miracle = 63`, `Ledger = 64`, `Commandment = 65`, `Religion = 66` (the names of DATA_WAREHOUSE §12.2, confirmed). `RangeOf`: 36, 45, 46 → 500–599; 60–66 → **800–899** (new block). Existing ranges unchanged.

| Codes | Owner | Domain | Events (names confirm DATA_WAREHOUSE §12.5) |
|---|---|---|---|
| 250–269 | C3 | Employment | 250 `EmploymentStarted`, 251 `WagePaid`, 252 `EmploymentEnded`, 253 `JobDay` |
| 270–289 | C1 | Ownership, Production | 270 `AssetBuilt`, 271 `AssetUsed`, 272 `UseRightGranted`, 273 `UseRightEnded`, 274 `TheftCommitted`† , 275 `SourceClaimed` |
| 290–299 | C5 | Citizenship | 290 `CitizenshipChanged`, 291 `MigrationDecided` |
| 320–329 | C2 | Social | 320 `PromiseMade`, 321 `PromiseKept`, 322 `PromiseBroken` (gossip codes of M7 move to 323–329) |
| 355–374 | C10 | Culture | 355 `TraditionLearned`, 356 `NormFormed` |
| 400–429 | C4 | Military | 400 `Recruited`, 401 `Discharged`, 402 `Deserted`, 403 `Defected`, 404 `OrderGiven` |
| 500–519 | C6 | Organisation, Service | 500 `OrgFounded`, 501 `OrgDissolved`, 502 `OrgHeadChanged`, 503 `MemberJoined`, 504 `MemberLeft`, 505 `OrgSpent`, 506 `ServiceRequested`, 507 `ServiceAccepted`, 508 `ServiceDelivered`, 509 `ServiceOutcomeRecorded`, 510 `ServiceRefused`, 511 `OrgOpportunityAvailable`, 512 `OrgDay` |
| 520–539 | C7 | Health, Education | 520 `TreatmentStarted`, 521 `TreatmentEnded`, 522 `TeachingSession`, 523 `KnowledgeTransferred` |
| 540–549 | C2 | Lending | 540 `LoanMade`, 541 `LoanRepaid`, 542 `LoanDefaulted`, 543 `LoanWrittenOff`, 544 `CollateralSeized` |
| 550–559 | C8 | Finance | 550 `DepositMade`, 551 `WelfareGranted`, 552 `InstitutionLoanMade` |
| 560–589 | C9 | Justice, Crime | 560 `ComplaintMade`, 561 `InvestigationOpened`, 562 `InvestigationClosed`, 563 `AccusationMade`, 564 `CaseDecided`, 565 `RestitutionMade`, 566 `FineImposed`, 567 `Detained`, 568 `Released` |
| 590–599 | C10 | Politics | 590 `LawAdopted`, 591 `LawRepealed`, 592 `GovernmentDecision` |
| 700–719 | Q1 | Kpi | KPI day rows if any |
| 800–809 | D1 | Divine | 800 `GodCreated`, 801 `GodAssociated`, 802 `GodPolicyChanged`, 803 `MythicConfigChanged`, 804 `MythicCredited`, 805 `MythicSpent`, 806 `MythicUpkeepCharged` (HV), 807 `DivineActionAccepted` (one per intervention), 808 `DivineObservationAnswered` (Full), 809 `MythicBalanceDay` |
| 810–824 | D2 | Divine | 810 `DivineSuggestionIssued`, 811 `DivineCommandIssued`, 812 `InstructionRelayed`, 813 `CommandPerceived` (HV), 814 `CommandResponded`, 815 `CompulsionStarted`, 816 `CompulsionEnded`, 817 `CompulsionBlocked` |
| 825–844 | D3 | Miracle, Ledger | 825 `MiracleCast`, 826 `MiracleEffectApplied`, 827 `ResourceCreated`, 828 `ResourceDestroyed`, 829 `LocationConsecrated` |
| 845–849 | D4 | Possession | 845 `PossessionStarted`, 846 `PossessionEnded`, 847 `PossessedActionStarted` (HV) |
| 850–859 | D5 | Avatar | 850 `AvatarManifested`, 851 `AvatarDismissed`, 852 `AvatarDestroyed`, 853 `AvatarDamaged`, 854 `AvatarActionStarted` (HV) |
| 860–879 | D6 | Commandment, Religion | 860 `CommandmentIssued`, 861 `CommandmentRevised`, 862 `CommandmentRevoked`, 863 `CommandmentLearned`, 864 `CommandmentStanceChanged`, 865 `CommandmentForgotten`, 866 `CommandmentChoiceMade` (HV), 867 `ReligionFounded`, 868 `AffiliationChanged`, 869 `BeliefChanged` (HV, always kept when \|Δ\| ≥ 100), 870 `PrayerMade` (HV), 871 `WorshipHeld` |
| 880–889 | D7 | Divine | 880 `NpcResurrected`, 881 `NpcArchived` (Full), 882 `NpcRestoredFromArchive`, 883 `NpcRejuvenated` |
| 890–899 | reserved | Divine | AI gods, divine KPIs |

† analytics-only truth field or event (recorded at Full; never read by a rule or policy). Rules of SHARED_CONTRACTS §8 apply: each package adds rows only in its own partial catalogue array, mappers only in its own mapping file, codes never reused, a changed field meaning takes a new code, an added field bumps `Version`. Every effect event of an intervention carries `Intervention`; every action event started under control carries `Control` (0 own, 1 compelled, 2 possessed, 3 avatar) so the warehouse's `control_code` columns fill from the event, not by inference.

---

## 16. Compatibility switches and neutrality pins

| Switch | Default during the run | Off means |
|---|---|---|
| `Divinity.Enabled` | 0 (every existing scenario); 1 in new divine scenarios and in god-only mode | no `GodState.Divinity`, no handlers (every divine command refused `disabled`), every Divinity hook returns its default |
| `Divinity.Mode` | `Standard` | section 5 |
| `Orgs.Enabled` | 0 | no `GodState.Organisations`, no founding, no services, no institution seats |
| `Orgs.Emergence` | 1 | with 0, organisations only when a scenario seeds them |
| `Civ.Enabled` (added by this document) | 0 | no `GodState.Civ`: no assets, obligations, contracts or citizenship records; C1–C5 hooks return defaults |

Pins (confirm HEADLESS §12.4 P1–P7, add P8–P9). Base = the `unity/poc` head after `unity/ibs` and `unity/demog` merge. Scenarios: `band_6x20`, `m4_families`, `ibs_families`, `watch6`, the `demog_*` set; seeds 1–3; every checkpoint and every part.

| Pin | Statement | Landed by |
|---|---|---|
| P1 `Divinity_Off_Reproduces_Base` | all three switches 0: every part hash equals the base | F1 |
| P2 `Divinity_Idle_Is_Neutral` | `Divinity.Enabled = 1`, `IdleGod` for every god, no external command: every part except `divine` equals P1 | D1 (needs gods and Mythic) |
| P3 `Divinity_Disabled_Mode_Neutral` | `Mode = Disabled`, a scripted god commanding every day: all refused; every part except `divine` and `pending` equals P1 | D1 |
| P4 `Orgs_Off_Reproduces_Base` | as P1 | F1 |
| P5 `Orgs_NoEmergence_Neutral` | `Orgs.Enabled = 1`, `Emergence = 0`, no seeds: every part except `organisations` equals P4 | C6 |
| P6 `Switch_Matrix` | each package's suite with the other switches 0 equals its branch tip; pairwise; all on | Q3 after every merge |
| P7 `Recording_Neutral_On_New_Domains` | Off/Summary/Full and each new domain alone give one hash | Q1 |
| P8 `Civ_Off_Reproduces_Base` | as P1 | F1 |
| P9 `Archive_Neutral` | `Divinity.Enabled = 1`, `Resurrection.Archive = All`, idle: every part except `divine` equals P1 | D7 |

As with M4/M5 (DEV_LOG merge record), once a switch has stub state its single-switch pin can no longer pin the base; pins therefore always switch **all** new modules off except the one under test.

---

## 17. Save state

| Item | Rule |
|---|---|
| Owner of the save schema | **the integration lead only.** No package edits `SaveFile.cs`. |
| Version | Still 4 on the base (the planned v5 at the M4+M5 merge was not taken; section 20 row 20.1). **F1 sets `CurrentVersion = 5`** (Npc `Kind`/`God`/`BioAgeOffset`, `NpcMind.Divine`, the three state slots, setup fields). The lead bumps once at each playable checkpoint whose merges changed saved state (B → 6, C → 7, D → 8, E → 9, F → 10), in the merge commit, with a DEV_LOG row. Older versions are refused, as today. No compatibility between branch builds (13.13). |
| State shape | New state only in `GodState.Divinity`, `.Organisations`, `.Civ` and their per-package sub-objects, `NpcMind.Divine`, and the F1 `Npc` fields. Public fields, ascending-id `List`s, no dictionaries, no references between objects (ids only), `[JsonIgnore]` for caches (indexes rebuilt in `FromState`). The archive is saved in full. |
| Tuning | only in the three F1 blocks (`IOmitWhenDefault`); scenario options are tuning paths; the two `GameSetup` fields of 2.1 are the only setup additions. |
| Required tests (each package) | save at a mid-state of its feature (open compulsion, possession, manifested avatar, open service request, open loan, archived mind) → load → continue = uninterrupted run hash at every later checkpoint (`*_SaveLoadParity`). |

---

## 18. Replay and analytical recording

### 18.1 Replay
- Every human divine input is an external command and is logged automatically (`ReplayWriter.Command`, origin tag `human`; scripted gods `schedule`). AI gods' and institution policies' commands are submitted inside `Step` and recomputed (`aigod:<policy>` in the header only). Movement is the `DivineControlInput` stream with its lease (11.3); the camera, frame rate and time scale never reach the sim; commands issued while paused are stamped with the next tick.
- Required: `Replay_DivineScenario_Exact` (scenario 18: ordinary NPC actions, a suggestion, a compulsion, a possession with movement, an avatar walk and give, a heal, a smite, a resource creation, a commandment and a resurrection; replay equal at every checkpoint), `Replay_SwitchGod`, `Unity_Agrees_With_Runner` on that replay.

### 18.2 Warehouse migrations (one owner each; confirms DATA_WAREHOUSE §12.4 with two splits)

A migration may reference only lower numbers; a merged file is never edited; numbers are fixed by this table (a package that is late keeps its number; one that needs a table from an unmerged lower number waits for it). Gaps are allowed (the writer applies what exists).

| Migration | Owner | Contents |
|---|---|---|
| `0007_divine_core.sql` | F1 | `dim_god`, `dim_god_history`, `fact_god_faction`, `fact_resource_ledger`, `dim_code` sets (`control_source`, `cause_kind`, mythic category), `control_code` and `intervention_id` columns on the existing person-action tables |
| `0008_mythic.sql` | D1 | `fact_mythic_balance_day`, `fact_mythic_ledger`, `fact_divine_action`, `fact_divine_observation` |
| `0009_commands.sql` | D2 | `fact_divine_command`, `fact_divine_command_target`, `fact_command_perception`, `fact_command_response`, `fact_compulsion` |
| `0010_possession.sql` | D4 | `fact_possession` |
| `0011_avatar.sql` | D5 | `fact_avatar_manifestation` |
| `0012_miracles.sql` | D3 | `fact_miracle`, `fact_miracle_effect` |
| `0013_resurrection.sql` | D7 | `fact_resurrection`, `fact_life_spell`, `fact_npc_archive`, `life_no` on `fact_death` |
| `0014_commandments_religion.sql` | D6 | commandment, revision, knowledge, religion, affiliation, belief, prayer, worship tables |
| `0015_ownership_work.sql` | **C1** | `fact_ownership_spell`, `fact_use_right`, `fact_economic_relationship` |
| `0016_employment.sql` | C3 | `fact_employment`, `fact_job_day` (enterprise columns on `dim_org` go in C3's later file only after 0018 exists, see below) |
| `0017_lending.sql` | **C2** | `fact_loan`, `fact_loan_repayment`, `fact_loan_default`, `fact_promise` |
| `0018_organisations.sql` | C6 | `dim_org`, `dim_org_history`, `fact_org_membership`, `fact_org_expenditure`, `fact_org_day`, `fact_service_request`, `_delivery`, `_outcome` |
| `0019_health_education.sql` | C7 | `fact_health_encounter`, `fact_education_enrolment`, `fact_education_assessment` |
| `0020_banking_welfare.sql` | **C8** (was free) | institutional lending and welfare columns, `fact_welfare` |
| `0021_justice.sql` | C9 | `fact_investigation`, `fact_court_decision`, `fact_detention` |
| `0022_military.sql` | C4 | `fact_military_service`, `fact_service_day` |
| `0023_citizenship_politics.sql` | C5 | citizenship spells, migration extensions |
| `0024_culture_religion_orgs.sql` | C10 | religious-institution and government columns on `dim_org`, laws, traditions |
| `0025_enterprise_columns.sql` | C3 | `dim_org` enterprise view and `fact_org_day` columns (needs 0018) |
| `0026`–`0029` | free (lead allocates) | |
| `0030_kpi_views.sql` | Q1 | `dim_kpi` rows, `v_kpi_*`, `v_person_life`, `v_god_day`, `v_intervention_chain`, `v_org_history` |

Mapping registration: `DivinityMappings.Register` → `D1Mappings` … `D7Mappings`; `OrgMappings` → `C6Mappings` … `C10Mappings`; `CivMappings` → `C1`, `C2`, `C3`, `C5` (`C4Mappings` under `CivMappings`). Each package writes only its mapping file, its `Tables.<Pkg>.cs` and its migration. Recording never changes a hash (P7); **reconciliation test per package** (scenario 17): every row count of its facts equals the count of its events, and its spells close and open exactly at the events.

---

## 19. Contract → owning package, and what this document changed in the D5 proposals

### 19.1 Contract → owner

| Contract | Owner (implements) | May modify (by amendment only, except owner) | Consumers |
|---|---|---|---|
| Persistent NPC identity, `Bodies`, person loops | F1 | D5 (avatar body behaviour), D7 (resurrection) | all |
| God identity, registry, seats | D1 (F1 types) | — | D2–D7, U1–U6, Q1 |
| Faction identity | existing (frozen) | — | C5, C10, D1 |
| Organisation identity, `OrgState`, memberships | C6 | C3, C7–C10 (own sub-objects) | C2, C8, D2 (fan-out), D6, U5, Q1 |
| Physical locations, sense seam, consecrated places | F1 / D3 | C1, C6 (own object kinds) | all |
| Object affordances, `Permits` | existing + C1 (ownership beliefs) | C9, C10 (laws), D6 (believed norms) | all minds |
| Action primitives, `DivineActs` map | existing / D2 | C1, C3, C7 (new verbs in their ranges) | D2, D4, D5 |
| Ownership and possession, Goods enums | F1 (values), `Goods` frozen | C1 (theft classification) | all |
| Inventories, bins | existing | C1, C6, C8, D6 (own bin kinds) | all |
| Resource conservation, miracle ledger (C1–C8) | F1 (C1–C6 existing) / D3 (C7) | — | Q3 |
| Command validation, taxonomy, refusal codes | F1 / D1 | each D package (its handlers) | U2, Q3 |
| Temporary control authority | F1 | D1 (fields by amendment) | D2, D4, D5 |
| Possession | D4 | — | U1, U4 |
| Avatar state | D5 | — | U1, U4 |
| Divine power (Mythic) | D1 | — | all D, U2 |
| Knowledge views, `IDivineView`, layer tags | D1 (divine), C6 (institution view), existing (faction) | U2, U5 (presentation only) | all policies, U |
| Beliefs (`DivineMind.Gods`, affiliation) | D6 | D2 (inbox only) | D2, C10, U |
| Commandments | D6 | C10 (law adoption copies) | C9, C10, U2 |
| Institutional service requests and outcomes | C6 | C7, C8, C9 (service kinds) | U5, Q1 |
| Domain events (ranges, catalogue) | F1 | each package in its range | Q1, U |
| Save state | lead | — | Q3 |
| Replay | existing / Q3 | D4, D5 (input commands) | Q3 |
| Analytical recording, migrations | F1 (0007) / each package / Q1 | — | Q1, Q2 |

### 19.2 Changes to the companion proposals (this document wins)

| Proposal | Decision here |
|---|---|
| DATA_WAREHOUSE §12.4: `0015` shared by C1 and C3, `0017` by C2 and C8, `0020` free | one owner per migration: `0015` C1, `0016` + `0025` C3, `0017` C2, `0020` C8 |
| DATA_WAREHOUSE §12.4: a late package "takes the next free number at merge" | numbers are fixed by 18.2; dependency order is enforced by waiting, not renumbering |
| DATA_WAREHOUSE §12.1: `intervention_id` and the separate `compulsion_id`, `possession_id`, `avatar_id`, `miracle_id`, `command_id` | all equal the one `InterventionId` (section 1); `ledger_id` is its own counter |
| DATA_WAREHOUSE §12.5, HEADLESS §12.2 event and command names | confirmed; codes in 15.1 |
| HEADLESS §12.4 hash part `divine`, states `GodState.Divinity`, `GodState.Organisations` | confirmed; plus `GodState.Civ` / `civ` and `Civ.Enabled`, pins P8, P9 |
| HEADLESS §12.2 "a slot before 240" / "after 255" | 230 `DivinitySystem`, 257 `CivSystem`, 260 `OrganisationSystem`, 905, 910 (11.4) |
| HEADLESS §12.2 item 7 compulsion "candidate set replaced" | the compelled candidate is added and the policy's choice overridden (11.3); same observable effect, keeps perception and the M3.12 check |
| HEADLESS §12.3 random-stream labels | confirmed, plus `divine-decode (npc, intervention)`, `god-policy (god, tick)`, `miracle-effect (intervention, step)`; one label per call site |
| INFORMATION §12.3 `IDivineView` signatures | confirmed as the target; F1 lands the minimal subset (section 9) |
| AI_TRAINING §14 `IGodPolicy`, `IdleGod`, `StewardGod` | confirmed (section 12) |
| HEADLESS §12.2 `PossessedMoveIntent`, `PossessedAction`, `AvatarMoveIntent`, `AvatarAction`, 1/64-turn heading; CAMERA_AND_CONTROLS `Heading16` | one command `DivineControlInput` with a configurable sector count (default 16), a 3-tick lease and the sim choosing the next node (11.3) |
| CAMERA_AND_CONTROLS / PLAYER_UI provisional names | canonical: `IDivineView` (kept); `PossessionView`, `AvatarView` are read-only structs of `IDivineView` filled by `DivineViewFiller` from `ControlTable` (D4, D5); `MiracleWrought` → **`MiracleCast`** (825) + `MiracleEffectApplied` (826); `DivineResponse` → **`CommandResponded`** (814); `DivineControlInput` (kept) |
| NPC_ANIMATION gesture requests `Thank`, `Threaten`, `Greet`, `Alarm`; `GestureMade.DurationTicks`; prayer and teaching events | `GestureAct` 24–27 (C2); `GestureMade` v2 with `DurationTicks` (F1.17); prayer `PrayerMade` (870), worship `WorshipHeld` (871), teaching `TeachingSession` (522) and `KnowledgeTransferred` (523) |
| Procedural names, aliases, favourites, selection, camera | names are pure functions (F1.18); everything the player sets is in the non-hashed `ui` save extra, never in `GodState` |
| ORGANISATIONS / INSTITUTIONAL_SERVICES: `OrgId`, `OwnerKind.Org = 7`, `BinKind.OrgStore = 3`, `TopicKind.Org`, `RequestId`, `ObligationId`, `FormalObligation`, `DecisionRule`, `InstructionRelayed`, `OrgBelief`, `State(f)` alias, `Herbs` | confirmed (`TopicKind.Org = 9`, `InstructionRelayed` = 812, episode kinds 24–31 for org roles, `OrgBelief` in `NpcMind.Orgs`, `Herbs = 32`); **renumbered**: `OwnershipBasis.Forfeited = 14` taken as proposed, so the D5 bases move to 15–24 (7.1) |

---

## 20. Conflicts between documents and code, resolved here

| # | Documents say | Code does | Chosen | Why |
|---|---|---|---|---|
| 20.1 | SHARED_CONTRACTS 9.3: save v5 at the second M4/M5 merge | `CurrentVersion = 4` after the merge | v5 at F1; then one bump per checkpoint by the lead | one owner, one place |
| 20.2 | Brief §11: resurrection keeps memories and learned associations | `Kill` sets `npc.Mind = null` | archive before release, dormant (4.4) | hash-neutral while off; restores the same mind |
| 20.3 | Brief §11: resurrect someone dead for generations | ageing is `cal.AgeMilli(BornTick, Tick)` everywhere | `Npc.BioAgeOffset` and `Bodies.AgeTicks` | otherwise a resurrected elder dies of age at once |
| 20.4 | "Smite" suggests a divine death cause | `PopulationStats.DeathsByCause` is a hashed `int[9]` | `DeathCause.Violence` + attribution in `MiracleEffectApplied` | a new cause changes every population hash |
| 20.5 | SHARED_CONTRACTS 9.3: no new `GameSetup` field | the player seat must be in setup | two fields, default-ignored in JSON | core hash unchanged at default |
| 20.6 | Brief §6 Mode B: avatar uses existing movement and interaction | movement, `Goods` and `Act` resolve carriers through `PopulationState.Find` | avatar = `Npc` body with `BodyKind.Avatar`, id `0x4000_0000 + GodId` (4.2) | one implementation of every action; no NpcId consumed |
| 20.7 | F0.8: `ThinkScratch.Heard` filled only by M5 | divine messages must reach the mind | separate `ThinkScratch.Divine[2]`, decoded with M5's noise model | keeps the frozen M5 layout |
| 20.8 | F0.10: `MaxCand = 24` | full (16 + 4 + 4) | 36 | quotas for three new modules |
| 20.9 | `PolicyLevel` has four levels; `PolicyAssignment` arrays fixed at 4 | — | fifth level `God`, `PolicyLevelCount` | AI gods (D5.30) |
| 20.10 | ECONOMY: owners `Enterprise`, `State` | never written | one `OwnerKind.Org` for every organisation | D5.17 multiple models on one framework |
| 20.11 | `InstitutionView` keyed by `Faction` (placeholder) | unused | C6 rewrites it around `OrgId` (not frozen) | institutions do not originate only in government (D5.16) |
| 20.12 | HEADLESS §12.2: commands logged only when external | AI gods would be in-step | gods' seats are sim state; `SwitchGod` is a logged command | replay recomputes AI gods only if seats are in state |
| 20.13 | WORLD_ONTOLOGY C4: ground lots ownerless | — | relaxed from C1 (theft); avatar pick-ups keep owners | brief §16 |

**F1 implementation deviations (branch `unity/f1`, for the lead's review; each is a proposed amendment row).**

| # | Contract says | F1 does | Why |
|---|---|---|---|
| 20.F1.1 | F1.3: tuning roots frozen, nested classes partial | the roots `DivinityTuning`, `OrgTuning`, `CivTuning` are **not** partial; each nested block is partial and has fields in one file only (its package's); `Divinity.Control` (`ControlTuning`: `HeadingSectors`, `LeaseTicks`) is not partial | the tuning JSON is part of the `core` hash and reflection field order across partial files depends on the compiler's file order (Unity vs dotnet); D1's costs, upkeep and cooldowns go inside `Divinity.Mythic` |
| 20.F1.2 | 10.1 last sentence (scripted test god takes the `Human` seat) | LEAD_RULINGS R1: a scripted test god is an `Ai`-seated god inside `GodDirector`; the `Submit` gate checks only external commands | R1 |
| 20.F1.3 | 10.1 authorship: the seat is `GodRecord.Seat` (D1) | `DivinityModule.HoldsHumanSeat` asks D1 through the partial slot `HumanSeatD1`; until D1 answers, the seat is `GameSetup.HumanGod`. Gate order in `GodSim.Check`: game over, then `divinity_off`, then `not_your_god` (external only), then `god_only` for external `FactionCommand`s in god-only mode, then the handler lookup (`disabled`) | testable before D1 (F1-T3) |
| 20.F1.4 | 11: `void Release(...)` | `bool Release(...)` (true when a spell ended), so CT1 is testable without events; end events are D2/D4/D5's through the partial slots `Control.EndedD2/D4/D5` | F1 has no event rows |
| 20.F1.5 | 11: `ControlEntry` fields | adds `Input`, `InputTick`, `LeaseUntil`, `MoveNode` (lease bookkeeping of 11.3) and the `ControlStatus` enum; `ThinkScratch.Control` (the entry, copied before Think) and `ThinkScratch.ControlCand` (index of the controlled candidate a divinity mind hook adds); sites `PopulationExtensions.SkipsPolicy`/`ControlIntent` beside `OverrideIntent`/`ForceThink` | mind hooks cannot read `GodState` (9), so the control candidate needs scratch data |
| 20.F1.6 | F1.17: `GestureMade.DurationTicks` from `CommsTuning` | from `Social.GestureTicks` (the existing gesture duration, default 3) | no second number for one duration |
| 20.F1.7 | F1.12: per-package partial **arrays** | per-package static **properties** (`private static ReadOnlyArray<EventInfo> D1 => ...`) | static field initialisers across partial files run in an undefined order |
| 20.F1.8 | F1.6 slot lists | the listed slots exist; every other hook also has slots for every package of its domain (Civ C1-C5, Orgs C6-C10, Divinity as listed or D2-D7) | supersets cost nothing and save later amendments |
| 20.F1.9 | 13.2 `CommandmentRule` and its enums | in `Divinity/CommandmentTypes.cs`, OWNER D6: the struct shape is frozen (a field of `RevealCommandment`), D6 appends `ApplySelector`, `ConditionKind`, `CommandmentSource` values | the commands are F1's, the vocabulary D6's |
| 20.F1.10 | F1.16 `MythicTrace` | reads D1's events by name (`MythicCredited`, `MythicSpent`, `MythicUpkeepCharged`) with fields `int God`, `long Amount` | the events do not exist yet |
| 20.F1.11 | F1.15 `MapContext.God`, `.Org` | the dimension key is the GodId / OrgId itself | one run per database; ids never reused |
| 20.F1.12 | R27 `Orgs.C10.SeedGovernments` | `SimTuning.TrySet` refuses every path containing "seed", so that switch cannot be set by `--set` or a scenario | **needs a rename** (for example `C10.Governments`) or a narrower guard; not changed by F1 |

## 21. Open decisions (each with the default taken)

| # | Decision | Default |
|---|---|---|
| 21.1 | Default of `Divinity.Enabled` for new games after Checkpoint B | stays 0 until the lead flips it at Checkpoint B with re-pinned hashes; god-only scenarios set 1 |
| 21.2 | Several possessions or avatar + possession at once | no (one embodied focus per god); tuning later |
| 21.3 | Whether NPCs recognise an avatar as their god | belief-based (D6), no automatic recognition |
| 21.4 | Divine-observation profile ownership (scenario, player, per god) | per scenario and per god (INFORMATION §12.3) |
| 21.5 | Worship-based Mythic | none in this run (brief §7) |
| 21.6 | Gait "run" | equals walk until a running speed exists |

---

## 22. Industry addendum (owner D6, 27 Nov 2026)

**Status: binding.** This section settles, as contracts, every shared number and seam that the owner D6 design documents left provisional: [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](../stage4/INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md) ("IND") §9 and §12, [VEHICLES_AND_HARVESTING_DESIGN.md](../stage4/VEHICLES_AND_HARVESTING_DESIGN.md) ("VEH"), [ELECTRICITY_DESIGN.md](../stage4/ELECTRICITY_DESIGN.md) ("ELE"), [INDUSTRIAL_MILITARY_DESIGN.md](../stage4/INDUSTRIAL_MILITARY_DESIGN.md) ("IMIL"), [WASTE_AND_CIRCULAR_DESIGN.md](../stage4/WASTE_AND_CIRCULAR_DESIGN.md) ("WST"), [KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md](../stage4/KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md) ("KNOW") §14, [MONEY_AND_ACCOUNTING_DESIGN.md](../stage4/MONEY_AND_ACCOUNTING_DESIGN.md) ("MONEY") §12, WORLD_ONTOLOGY §11 and DATA_WAREHOUSE_DESIGN §13.4. It closes lead rulings **R15**, **R16** (this half), **R32**, **R33** and **R34** ([LEAD_RULINGS.md](LEAD_RULINGS.md)). Sections 0–21 still hold for everything not listed here. Where a design document differs from this section, this section wins and the document is corrected in review (the R7/R24 pattern). Owner decisions are written **D6.n**; the commandments package is "package D6" (R35). Paths are relative to `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`.

### 22.0 Why a second foundation commit, and when it lands

Separate worktrees do **not** make shared-contract changes safe. Two branches that each edit `Wares.cs`, `Goods.cs`, `GodState`, `EventCatalog`, `Thinker` or `Mind.cs` both pass their own tests and then break each other, or move every hash, at merge. So, exactly as F1 did for D5, **one behaviour-neutral foundation commit, I1a**, lands the whole industrial and knowledge shared surface, and every I and K package branches from `unity/poc` after it.

| | Rule |
|---|---|
| Cut from | `unity/poc` after **F1b** and **C1** have merged (I1a needs F1's slot, dispatch and catalogue pattern, and C1's `AssetId` allocator, asset record and Civ dispatcher bodies) |
| Merged | only **after the Tier 1 set has merged** (D6.18: F0, F1, F2 experiments, C1, D1, D2, D3, package D6 commandments, U1a, U2a, U3a, Q1 Tier 1, Q3 harness), and **before** any other I or K package starts |
| Who | one agent (Opus) run for the lead, branch `unity/i1a` in `wt-lrs-i1a`; the lead reviews every frozen-file diff, as for F1 |
| After it | each stub file has exactly one owner (22.1, column 4); I1–I6, K1 and K2 change only their own files plus the flagged additive rows of 22.9. Any further shared change needs an amendment row in section 20 from the lead; a package never works around a missing seam |

### 22.1 What I1a must contain (all behaviour-neutral)

| # | Item | Files (new unless "edit") | Owner after I1a |
|---|---|---|---|
| I1a.1 | **Wares** 40–71 (22.3), `Wares.PersonalCount` 40 → **72**; helpers `IsIndustrial`, `IsWaste`, `IsLiquid`, `IsFuel`, `IsUnique`; the weight column; `WareRefValue` rows (analytics only); population hashing extended by the F1.17 rule (22.3) | edit `Economy/Wares.cs`, `Population/PopulationModule.cs` (`Weight`, `HashPopulation`) | frozen; content (recipes, deposits) I1, `Document`/`Paper` K1, `Coin` K2a |
| I1a.2 | **Goods ledger** widened to 16 produce and 16 consume reason slots; `ProduceReason` 6–9, `ConsumeReason` 8–13 (22.3); slots 8–15 hashed only when non-zero | edit `Population/PopulationState.cs` | frozen |
| I1a.3 | **Goods API**: `MoveReason.BinTransfer = 13` (reach `Reach.Store` at both bins); `OwnershipBasis.SupplyAgreement = 25`, `Paid = 26`; `Goods.MoveBin` (22.3); `Wares.IsUnique(w)` replaces `w != Ware.Axe` in `Goods.Mergeable` and the ground merge | edit `Population/Goods.cs`, `Population/Reach.cs` | frozen (amendment row 22.11 #10–13) |
| I1a.4 | **State slot** `GodState.Industry` (`IndustryState`, partial; sub-objects in 22.4), null unless `Industry.Enabled`; each sub-object null until its package fills it | `Core/GodState.Industry.cs`, `Industry/IndustryState.cs` + partials `IndustryState.I1.cs` … `I5.cs`, `IndustryState.Waste.cs` | container frozen; partials to I1–I5 |
| I1a.5 | **Civ sub-objects** `CivState.K1`, `CivState.K2` (partial stubs), hashed in the `civ` part after C5 | edit `Civ/CivState.cs`; `Civ/CivState.K1.cs`, `Civ/CivState.K2.cs` | container frozen; partials K1, K2 |
| I1a.6 | **Hash part** `"industry"` appended to `GodSim.AllHashPartNames` after `"divine"`, present only when `Population != null` and `GodState.Industry != null`; `PartNamesOf`/`PartsOf` extended | edit `Core/GodSim.cs` | frozen |
| I1a.7 | **Tuning**: root `SimTuning.Industry` (`IOmitWhenDefault`, `Enabled = 0`) with nested classes `Geology`, `Production`, `Waste` (I1), `Vehicles` (I2), `Trade` (I3), `Grid` (I4), `Military` (I5); nested `SimTuning.Civ.Knowledge` (`Enabled = 0`, empty `Designs`) and `SimTuning.Civ.Money` (`Enabled = 0`, `RecordPersonSales = 0`, `SliceCommodity = Bronze`) | edit `Core/SimTuning.cs`; `Industry/IndustryTuning.cs`, `Civ/CivTuning.K1.cs`, `Civ/CivTuning.K2.cs` | root frozen; nested classes to their packages |
| I1a.8 | **Module entry points** `IndustryModule.StartGame/Install/NewMind/OnDeath/Check`, called after `DivinityModule` at every F1.5 site (no-ops; `StartGame` creates `GodState.Industry` only when the switch is on and the game has a population) | edit `Core/GodSim.cs`, `PopulationModule.cs`, `SimInvariants.cs`; `Industry/IndustryModule.cs` | call sites frozen; bodies I1 |
| I1a.9 | **Extension dispatch**: `IndustryHooks`/`IndustryMindHooks` called **after Divinity** at every existing site (including the F1.6 sites and `SenseExtra`), and in the `Kill` order (22.4); three new movement sites `PopulationExtensions.StepTicks`, `PathCosts`, `Moved` (22.4); the K1/K2 partial-method slots in C1's Civ dispatchers and in `CivSystem` (flagged additive edits to C1's files) | edit `Cognition/Extensions.cs`, `Population/PopulationExtensions.cs`, `PopulationSystem.cs`, `Thinker.cs`, `Civ/CivHooks.cs`, `Civ/CivMindHooks.cs`, `Civ/CivSystem.cs`; `Industry/IndustryHooks.cs`, `Industry/IndustryMindHooks.cs` | dispatch frozen; Industry bodies I1 (I2–I5 by partial files, fixed order I1 → I2 → I3 → I4 → I5); K1/K2 partials |
| I1a.10 | **Id ranges** of 22.5; `ThinkScratch.MaxCand` 36 → **40** (Industry quota 4); mind slots `NpcMind.Industry` (`IndustryMind`) and `NpcMind.Know` (`KnowledgeMind`), created lazily (22.4) | edit `Thinker.cs`, `Policy/PolicyTypes.cs`, `Learning/Verbs.cs`, `Learning/EpisodeKey.cs`, `Comms/Message.cs`, `Cognition/Mind.cs`, the files of `ObjClass`, `SituationKind`, `SelfFeature`, `Role`, `NpcGroup`, `Divinity/DivineTypes.cs` (`DivineAct` 22, 23); stubs `Industry/IndustryMind.cs`, `Civ/KnowledgeMind.cs` | enums frozen; mind fields I1/I2 and K1 |
| I1a.11 | **`DesignId` on C1 asset records** (`ushort`, 0 = common), exposed to inspection by the sense seam (K1-R10) | edit `Civ/Assets.cs` (C1's file, flagged additive) | C1 (field value written by I1–I5) |
| I1a.12 | **Systems registered as empty shells**: `ElectricitySystem` (262) and `IndustrySystem` (264), each returning at once while `GodState.Industry == null` | edit `Core/GodSim.cs` (registration); `Industry/IndustrySystem.cs`, `Industry/Grid/ElectricitySystem.cs` | bodies I1, I4 |
| I1a.13 | **Events**: `TelemetryDomain` 70–79 and `RangeOf` (22.6); `IndustryEvents.Catalog` = per-package partial arrays `I1` … `I6` (empty except `BinMoved` 1014); `CivEvents.K1`, `CivEvents.K2` partial arrays (empty); `EventCatalog.All` += `IndustryEvents.Catalog` | edit `Telemetry/DomainEvent.cs`, `Civ/CivEvents.cs`; `Industry/IndustryEvents.cs` (+ `IndustryEvents.I1.cs` … `I6.cs`) | per-package arrays to their packages |
| I1a.14 | **Warehouse stubs**: `EventMapRegistry.CreateDefault` calls `IndustryMappings.Register(r)` (→ `I1Mappings` … `I6Mappings` stubs); `CivMappings` also calls `K1Mappings`, `K2Mappings` stubs; `Tables.Industry.cs` stub. **No migration** (I1a writes no table) | `DotNet/God.Runner/Warehouse/...` | stubs to their packages |
| I1a.15 | **Save**: `FromJson` accepts null `Industry`, null `CivState.K1/K2`, null `NpcMind.Industry/Know` (written on the lead's instruction; the lead owns `SaveFile.cs` and the version, 22.8) | edit `Save/SaveFile.cs` (lead) | lead |
| I1a.16 | **Tests and helpers**: pins P10–P13 (22.8); `Wares_LayoutNeutral`, `Ledger_WideningNeutral`; the static tests of 22.10 in their guarding form (they pass vacuously until code exists, then guard); `IndustrialProbe` (collect industry events) | `DotNet/God.Runner.Tests/I1a*`, `Assets/God/Tests/EditMode/Support/...` | frozen |

**I1a gate** (as 0.2): `dotnet test DotNet -c Release` and Unity EditMode green; `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` shows 0 errors (the `Ware` enum and `GodState` are used by `God.Game`); stage 3 `PolicyRunTests` hash `4bd54b271857a803` unchanged; P1, P4, P8 and every pin already landed (P2, P3, P5, P9 when present) unchanged; P10–P13 pass; the "no handler, refused" and person-loop static tests still pass.

### 22.2 Binding rules (D6.2, D6.9–D6.12, R33)

| # | Rule |
|---|---|
| IA-1 | **One economy.** Industrial goods are ordinary `ItemLot`s moved only by `Goods`; facilities, stations, links and vehicles are C1 productive assets; owners are the existing `Owner` kinds (`Npc`, `Household`, `Org`). There is no industrial inventory, no faction stockpile, no credit balance and no industrial owner type (D6.1) |
| IA-2 | **One money system; no external buyer.** Money is commodity-money lots (category 1, C1) or institutional money (K2b), created and destroyed **only** by MONEY §4.2 (E1–E4, D1–D5). Production, extraction, refining, delivery, unloading, electricity generation or supply, construction, research, a completed order, a miracle and the passage of time never change a money balance and never create a claim. Revenue is only an actual payment by an NPC, household or organisation in the world. There is no external market, buyer of last resort, sink or converter of goods into money, and no type named or acting as one (static `NoExternalBuyer`, MON-I5). The first brief's simplified external buyer has no successor (D6.2) |
| IA-3 | **Knowledge gates production; rights are owner policy (R33).** Every industrial runner (recipe cycle, construction, station operation, vehicle drive) checks only physical and knowledge conditions: inputs present and owned, workers or operators present, power, and the knowledge-and-skill rule (a present worker or operator meets the design's knowledge and skill minima, or follows a usable document at the site; `DesignId = 0`, or K1 absent or off, means no knowledge check). Stall reason `NoDesign` means exactly "no present worker knows it and no usable document is at the site". A patent, licence or other right is **never** a truth gate. `Rights.Permits` is called only to set the analytics flag `Licensed` † on `ProductionCycleStarted` and on the asset at `AssetBuilt`, from which K1 classifies `UnauthorisedDesignUse` † (KNOW §9). Respecting rights is the owner's own decision: the organisation policy `RespectRights` (default on), followed by its office-holders by their own scores from believed rights (`Permits.Believed`); an NPC owner weighs it in its own score. Static test `Rights_NeverGate` |
| IA-4 | **A vehicle is a C1 productive asset, not an `Npc`** (VEH-1). Its `AssetId` comes from C1's allocator (R20); it never enters `PopulationState.Npcs`, never takes an NpcId and never decides. It moves only by its living driver's own step through the I1a movement sites; occupants' `Npc.Node` equals the vehicle node; no `Npc` field is added (occupancy is a derived index) |
| IA-5 | **Energy is not a good and not money** (ELE-1, ELE-2): never a lot, never owned, never in a bin or the goods ledger; recorded only in the energy ledger. A claim arises only from a person's billing act (a C2 obligation), revenue only from a payment |
| IA-6 | **Every lot move is a `Goods` call with a living actor within reach.** `Sim/Industry`, `Sim/Civ/Knowledge*` and `Sim/Civ/Money*` never pass `actor <= 0` (which bypasses reach) |
| IA-7 | **The god acts on industry only through existing divine mechanics**: instruction, compulsion, possession (with `DivineAct` 22 Board and 23 Alight), and D3 miracles with their ledger rows (never `ProduceReason.Extracted`). The god never places an order, sets a routine, dispatch rank, port priority or price, owns nothing and holds no right (SAC G3, RT-I5) |
| IA-8 | **No ownership-model branch.** ORG-INV-5's static test is extended to `Sim/Industry`, `Sim/Civ/Knowledge*`, `Sim/Civ/Money*` |
| IA-9 | **No new death cause, faction or person creation.** Soldiers, crews and operators are existing NPCs (IMIL-I1, P-C); strategic deaths use `DeathCause.Violence` with attribution (20.4) |

### 22.3 Goods surface (frozen after I1a)

**Wares** (block 40–71; stage 3 wares 0–27 and `Wares.Count = 28` unchanged; personal 28–39 unchanged; stage 3 books never hold 40–71):

| No. | Ware | Class | Weight | Content owner |
|---|---|---|---|---|
| 40 | `IronOre` | geological raw | 4 | I1 |
| 41 | `CopperOre` | geological raw | 4 | I1 |
| 42 | `Coal` | geological raw, fuel (new code: stage 3 has no coal; `Ware.Ore` stays separate) | 3 | I1 |
| 43 | `CrudeOil` | geological raw, liquid | 3 | I1 |
| 44 | `UraniumOre` | abstract fictional strategic mineral (D6.14) | 5 | I1 |
| 45 | `Fuel` | refined fuel, liquid | 2 | I1 |
| 46 | `FuelOil` | refined fuel, liquid | 3 | I1 |
| 47 | `Copper` | industrial metal | 2 | I1 |
| 48 | `Wire` | component | 1 | I1 |
| 49 | `MachineParts` | component | 2 | I1 |
| 50 | `Engine` | component | 4 | I1 |
| 51 | `Ammunition` | military consumable (abstract) | 1 | I5 |
| 52 | `NuclearFuel` | abstract nuclear material | 2 | I1 (Tier 3 recipe) |
| 53 | `Firearm` | abstract infantry gear | 2 | I5 |
| 54–55 | spare (industrial) | | | |
| 56–62 | `Refuse`, `Scrap`, `Recyclables`, `Ash`, `Slag`, `Sludge`, `SpentFuel` | waste | WST | I1 (waste part) |
| 63 | `Document` | knowledge artefact; **unique** (count 1, never merges) | 1 | K1 |
| 64 | `Paper` | writing material | 1 | K1 |
| 65 | `Coin` | commodity-money candidate (optional: no recipe required; acceptance is belief, MONEY §3.1) | 1 | K2a |
| 66–71 | spare | | | later chains |

Helpers: `IsIndustrial(w)` = 40–55; `IsWaste(w)` = 56–62; `IsLiquid(w)` = {43, 45, 46}; `IsFuel(w)` = {42, 45, 46, 52, 56}; `IsUnique(w)` = {`Axe`, `Document`}. Existing `Iron` (10) and `Steel` (15) are reused; industry adds further recipes for them.

**Hashing (F1.17 rule, extended).** `HashPopulation` hashes the per-ware ledger and holdings in the base order (wares 0–31, then 32–39 only when any is non-zero, as F1.17), then wares 40–71 only when any of their ledger entries or holdings is non-zero. Ledger reason slots 0–7 are hashed exactly as on the base; slots 8–15 only when any is non-zero. The internal array layout may change; the hasher iterates the base order explicitly. With no industrial or knowledge lot, every `population` part equals the base (`Wares_LayoutNeutral`, `Ledger_WideningNeutral`, P10).

**Ledger reasons** (`GoodsLedger.ProduceReasons = ConsumeReasons = 16`):

| Produce | No. | Owner | | Consume | No. | Owner |
|---|---|---|---|---|---|---|
| (base) `Founder`, `Foraged`, `HandGathered`, `Placed` | 0–3 | | | (base) `Eaten`, `Spoiled`, `Built`, `Wear`, `Buried` | 0–4 | |
| (F1) `Miracle`, `Crafted` | 4, 5 | | | (F1) `Miracle`, `Treatment`, `Offering` | 5–7 | |
| `Extracted` (deposit → lots: mine, well, harvester, manual) | 6 | I1, I2 | | `ProcessInput` (recipe input at cycle start; writing and copying materials) | 8 | I1, K1 |
| `Processed` (recipe outputs, waste included; ash and spent fuel at stations) | 7 | I1, I4 | | `Combusted` (fuel burned: station, vehicle tank, process heat) | 9 | I2, I4 |
| `Discarded` (waste arising from consumption) | 8 | I1 waste | | `Embodied` (built into a persistent asset; recorded per asset) | 10 | I1–I5 |
| `Salvaged` (released by dismantling or destruction, ≤ embodied) | 9 | I1 waste, I2 | | `Disposed` (explicit disposal recipe; goods in bins destroyed by a strategic strike) | 11 | I1 waste, I5 |
| reserved | 10–15 | | | `Expended` (ammunition fired) | 12 | I5 |
| | | | | `Research` (experiment materials) | 13 | K1 (Tier 3) |
| | | | | reserved | 14–15 | |

`ConsumeReason.Built` (2) keeps its meaning for population huts and sites; industrial assets use `Embodied`. Vehicle maintenance consumes `ServiceWares` with the existing `Wear` (3).

**Moves and owners.** `MoveReason.BinTransfer = 13` (a crew member or worker within `Reach.Store` of both bins; loads, unloads, refuelling, station fuel); 14–31 reserved. `OwnershipBasis.SupplyAgreement = 25` (I3: ownership passes at the unload under an agreement; also procurement deliveries, I5), `Paid = 26` (K2a: the generic payment basis); `Purchase 23`, `Wage 16`, `Levy 19`, `Repayment 18`, `Loan 17` keep their meanings; 27–31 reserved.

**`Goods.MoveBin(s, ev, bin, toNode, actor)`** (frozen goods API amendment): relocates one `LotBin` to a lattice neighbour of its node; the lots inside keep holder `Bin(id)` and their owners (nothing is transferred, remotely or otherwise); `actor` is the living driver inside the vehicle; emits `BinMoved { Bin, From, To, Actor }` (1014). **Only caller: `IndustryHooks.Moved`** (static test `MoveBin_OnlyFromIndustryHooks`). `Goods.RemoveBin` still throws on a non-empty bin.

### 22.4 State, switches, minds, systems and dispatch

**`GodState.Industry`** (`IndustryState`) sub-objects, hashed in this fixed order inside the `industry` part, each only when non-null: `Geology` (I1), `Production` (I1: facilities, orders, recipe cycles), `Vehicles` (I2), `Trade` (I3: supply, electricity, procurement and haulage agreements), `Grid` (I4: stations, links, ports, meters, energy ledger), `Military` (I5), `Waste` (I1 waste part: sites, pollution field). Hash part order: `…, "household", "social", "civ", "organisations", "divine", "industry"`.

**`CivState.K1`** (K1: `DesignRegistry`, `Artefacts`, `Rights`, `Projects`, `Auctions`) and **`CivState.K2`** (K2: payments, `OrgBook`s, units of account; K2b monies and accounts), hashed in the `civ` part in the order `C1, C2, C3, C5, K1, K2`, each only when non-null. `CivState` itself exists only when `Civ.Enabled = 1`, so K1 and K2 require `Civ.Enabled = 1`.

**Identity** (section 1 rules: int, from 1, one allocator, public counter in the owning state, never reused):

| Id | Allocator (only path) | Counter |
|---|---|---|
| `AssetId` (R20) | C1 `Assets.Register(...)` — **every** productive asset: workshops, mines, wells, smelters, refineries, factories, power stations, power links, depots, laboratories, vehicles, strategic devices. Industry never allocates an asset id | `CivState.C1.NextAssetId` |
| `DepositId` | I1 `Geology.Add` (generation or scenario block) | `IndustryState.Geology.NextDepositId` |
| `OrderId` | I1 `Orders.Place` | `IndustryState.Production.NextOrderId` |
| `AgreementId` | I3 `Agreements.Make` (supply; electricity agreements of I4; procurement of I5; haulage of I2) | `IndustryState.Trade.NextAgreementId` |
| `LinkId`, `PortId`, `MeterId` | I4 `Grid.AddLink/AddPort/AddMeter` (a link is also a C1 asset) | `IndustryState.Grid.Next*` |
| `DesignId` | content id (`ushort`; 0 = common knowledge; 1–0x7FFF content, stable across versions; 0x8000+ invented variants, Tier 3, allocated only by `Designs.Formalise`) | none in this run |
| `RightId`, `ResearchProjectId`, `AuctionId` | K1 `Rights.Grant`, `Research.Start`, `Auctions.Announce` | `CivState.K1.Next*` |
| `PaymentId` | K2a `Payments.Begin` | `CivState.K2.NextPaymentId` |
| `MoneyId`, `AccountId` | K2b `Money.Found`, `Accounts.Open` | `CivState.K2.Next*` |
| `RecipeDef.Id`, `VehicleDef.Id`, `GeneratorDesign.Id` | frozen content tables; appended only (`RecipeBook` rows 1–20 are the stage 3 workshop defs, 21+ industrial) | — |

**Switches** (each default 0 in every existing scenario; the pins of 22.8 hold with them off):

| Switch | Default | Off means | On in |
|---|---|---|---|
| `Industry.Enabled` | 0 | no `GodState.Industry`; systems 262 and 264 return at once; every Industry hook returns its default | `ind_*` scenarios ("Iron Coast"), industrial tests |
| `Industry.Geology.Generate` | 1 (effective only when `Industry.Enabled`) | no generated deposits (a scenario `deposits` block always suppresses generation) | — (P11 sets 0) |
| `Industry.Waste.Enabled` | 0 | no `Discarded` production, no pollution update (WST-I5) | `ind_waste`, `ind_full` |
| `Civ.Knowledge.Enabled` | 0 | no `CivState.K1`, no `NpcMind.Know`; every `DesignId` is treated as common (no knowledge check) | `ind_*`; needs `Civ.Enabled = 1` (no effect without it) |
| `Civ.Money.Enabled` | 0 | no `CivState.K2`, no `Payment` records, no books; M5 barter and C3 wages as before | `ind_*`; needs `Civ.Enabled = 1` |
| `Civ.Money.RecordPersonSales` | 0 | person-to-person M5 deals get no `Payment` record | `ind_*` |
| `Civ.Money.SliceCommodity` | `Bronze` | — | `Coin` in `ind_*` (seeded holdings and price beliefs, MONEY §3.2) |

Iron Coast also sets `Civ.Enabled`, `Orgs.Enabled`, `Divinity.Enabled` to 1 and `Orgs.C10.SeedGovernments = 1` (R27).

**Mind slots** (F1.9b pattern): `NpcMind.Industry` (`IndustryMind`: at most 8 deposit beliefs, I1; the operator role `{ Vehicle, RoutineSeen, Since }`, I2; later fields by partial file) and `NpcMind.Know` (`KnowledgeMind`: at most 24 `KnownDesign`, K1; skill slots only if K1 owns the skill store, below). Both are **created lazily** on the first write and are otherwise null, so switching a module on without seeded content changes no mind hash; hashed only when present, after the F1 mind slots, in the order `Industry`, `Know`. `ArchivedLife.Know` (D7's archive) is added by K1 as a flagged additive edit after D7 merges (null-safe, hashed only when non-null); `IndustryMind` is not archived (roles end at death).

**One skill store.** The first of C7 and K1 to merge owns the person skill store; the other reuses it through one accessor (`Skills.Of(s, npc, SkillClass)`, `Skills.Gain(...)`). `SkillClass` keeps the owner's values first and appends the other's (K1: `Construction, Mechanics, Electrical, Driving, Mining, Refining, Chemistry, Metalwork, Reading, Research`; C7: healing as `Medicine`). In the planned order C7 merges first, so the store is C7's and K1 extends it; the lead records the outcome in DEV_LOG. I packages read skills only through the accessor. With K1 absent or off there are no design minima; skill only scales work speed (C1).

**Order within a tick** (extends 11.4):

| Order | System | Owner | Work |
|---|---|---|---|
| 250 | `PopulationSystem` | M3 + hooks | unchanged; vehicles move inside it through the movement sites (drivers in ascending NpcId) |
| 257 | `CivSystem` | C1 (+ C2, C3, C5, **K1, K2** partials, in that order) | K1 day start: forgetting decay (ascending NpcId), right expiry and lapse; K2 day start: `OrgBookDay` snapshots (ascending OrgId). **No separate K system** |
| 260 | `OrganisationSystem` | C6 | unchanged |
| **262** | `ElectricitySystem` | I4 | at each interval start: settle the finished interval (energy, fuel burn, meters), recompute components if dirty, demand, dispatch, allocation, `PowerPermille` for the interval (ELE §4.2) |
| **264** | `IndustrySystem` | I1 (+ I2, I3, I5, waste partials, in that order) | per tick: the recipe runner, facilities in ascending asset id; day start: `FacilityDay`, order expiry, vehicle days (I2), agreement schedules (I3), pollution field (waste) |

**Dispatch order everywhere:** M3 → M4 → M5 → Civ (C1, C2, C3, C5, K1, K2) → Orgs → Divinity → **Industry**. `SenseExtra` adds, after Divinity: Surface deposits, facilities, stations, ports and links, vehicles (`ObjClass` 110–129) with their inspectable `DesignId`. **`Kill` order** (extends 11.4): … → `DivinityHooks.OnDeath` → **`IndustryHooks.OnDeath`** (vacate seat, end operator role) → M3 cleanup → `ArchiveMind` → `Mind = null`.

**Movement sites** (I1a.9; default = base, so neutral without vehicles): `PopulationExtensions.StepTicks(s, npc, from, to, base)` (ticks for one step; a driver gets the vehicle's road/off-road cost and a refused step for impassable nodes or an empty tank); `PopulationExtensions.PathCosts(s, npc, ref profile)` (the cost profile handed to the unchanged `NodePather`); `PopulationExtensions.Moved(s, ev, npc, from, to)` (after a step: moves the vehicle, its bins by `Goods.MoveBin`, the other occupants, burns fuel, adds wear, emits `VehicleMoved`).

### 22.5 Id ranges reserved by I1a (extends section 14)

| Space | Industry (I1–I6) | Knowledge and money (K1, K2) | Notes |
|---|---|---|---|
| Learning `Verb` (byte, `Max = 128`) | 21 Board (Alight in its family), 22 Drive, 23 LoadCargo, 24 UnloadCargo, 25 Refuel, 26 Maintain, 27 Survey, 28 Extract, 29 PlaceOrder (also setting a vehicle routine), 30 OperateSwitch (station on/off, link open/close), 31 ReadMeter | 102 Study (read a document), 103 WriteDocument (write and copy), 63 Research (K1); 127 IssueBill (K2a: energy bills, royalties, fees) | reused, no new row: applying for a right = `RequestService` 106 (service kind 20); bidding = M5 `Say(Offer)`; paying = C3 `Pay` 101 (the general payment act, K2-R6); building a link, station or facility = `Build` 12; firing = the military rows. After this only the owned spares 114–115 (Orgs) and 119–123 (Divinity) remain free; any further row needs `Verb.Max` widened (a layout change that moves hashes; not in this run) |
| Intent `NpcVerb` | 200 Board, 201 Alight, 202 Drive, 203 LoadCargo, 204 UnloadCargo, 205 Refuel, 206 Extract, 207 PlaceOrder (`Detail`: order or routine), 208 OperateAsset (`Detail` = learning verb: Maintain, Survey, OperateSwitch, ReadMeter), sentinel `IndustryReserved = 209` | 210 Study, 211 WriteDocument (`Detail`: write or copy), 212 Research, 213 IssueBill, 214–218 spare (K2b account acts), sentinel `KnowMoneyReserved = 219` | other acts use the F0 generic intents (`Interact`, `UseStore`, `HandOver`) with the learning verb in `Detail` |
| `ObjClass` | 110 Deposit, 111 Mine, 112 Well, 113 Smelter/Refinery, 114 Factory (industrial workshop), 115 PowerStation, 116 PowerLink (pole, port, line), 117 Vehicle, 118 Depot, 119 WasteSite, 120 RecyclingPlant, 121 StrategicSite | 122 Laboratory | 123–129 spare; C1 keeps 90–99 for its own assets |
| `TopicKind` | 11 Deposit, 12 Order (jobs and orders at a facility) | 13 Design, 14 Infringement | 15+ free |
| `SituationKind` | 128 PowerOut, 129 FuelShort, 130 Polluted, 131 VehicleNear, 132 StrategicStrike; 133–143 spare | 144–151 reserved | 152+ free |
| `SelfFeature` | 72 Driving, 73 InVehicle, 74 IndustrialWork; 75–79 spare | 80–87 reserved | 88+ free |
| `Role` | 11 Operator, 12 Crew, 13 Engineer | 14–15 reserved | K1's Researcher, Examiner, Clerk, Librarian are C6 **office kinds**, not roles |
| `NpcGroup` | 11 VehicleCrew | — | 12+ free |
| `DivineAct` | 22 Board, 23 Alight (VEH §4.7; R15 closed) | — | 24–31 reserved; map rows by I2 (22.9) |
| `FeatureKind` | none | none | 19–31 stay reserved; a package that needs one asks for an amendment |
| Episode-key kind | 40 vehicle routine, 41 production order, 42 supply agreement, 43 meter bill; 44–47 spare | 19 right, 20 auction, 21 research project (K1); 22 payment (K2a); 23 spare | inside Civ's 16–23 for K; 48–63, 65+ free |
| `ServiceKind` (C6's enum) | — | 20 RegisterRight, 21 ContractResearch, 22 CommercialDispute, 23 LibraryRead (R34) | rows added by K1 (22.9) |
| Candidate quota | Industry 4 → `MaxCand = 40` | K1 and K2 use the **Civ** quota (`CivMindHooks.K1/.K2` partials); no new quota | |

### 22.6 Domain events (extends 15.1)

`TelemetryDomain` (I1a appends): `Industry = 70`, `Vehicle = 71`, `Energy = 72`, `Waste = 73`, `Environment = 74`, `IndustrialMilitary = 75` → `RangeOf` **1000–1099**; `Knowledge = 76`, `Rights = 77`, `Auction = 78`, `Money = 79` → `RangeOf` **1100–1199**. Existing ranges unchanged. The SAC §8 rules apply (own partial catalogue array, own mapper, codes never reused, a changed meaning takes a new code, an added field bumps `Version`); every industrial action event started under control carries `Control`, every divine effect carries `Intervention`.

| Codes | Owner | Domain | Events (names as in the owning document) |
|---|---|---|---|
| 1000–1019 | I1 | Industry | 1000 `DepositGenerated` (Full), 1001 `DepositDiscovered`, 1002 `DepositSurveyed`, 1003 `DepositDepleted`, 1004 `ResourceExtracted` (HV), 1005 `ProductionOrderPlaced`, 1006 `ProductionOrderCompleted`, 1007 `ProductionOrderCancelled`, 1008 `ProductionCycleStarted` (HV; carries `Licensed` †), 1009 `ProductionCycleCompleted` (HV), 1010 `ProductionCycleAborted`, 1011 `ProductionStalled`, 1012 `FacilityStatusChanged`, 1013 `FacilityDay`, **1014 `BinMoved` (landed by I1a)**; 1015–1019 spare |
| 1020–1039 | I2 | Vehicle | 1020 `VehicleRegistered` … 1037 `VehicleDay` as VEH §7; 1038–1039 spare |
| 1040–1049 | I3 | Industry | 1040 `SupplyAgreementMade`, 1041 `SupplyDelivered`, 1042 `SupplyAgreementBreached`, 1043 `SupplyAgreementEnded`, 1044 `IndustrialSaleClosed`; 1045–1049 spare |
| 1050–1069 | I4 | Energy | 1050 `GridChanged` … 1065 `PortPriorityChanged` as ELE §7; 1066–1069 spare |
| 1070–1079 | I5 | IndustrialMilitary | 1070 `ProcurementAgreed` … 1078 `StrategicEffectApplied` as IMIL §7; 1079 spare |
| 1080–1094 | I1 (waste part) | Waste (1080–1084), Environment (1085–1089) | 1080 `WasteGeneratedDay`, 1081 `WasteCollected`, 1082 `WasteDeposited`, 1083 `WasteIncinerated`, 1084 `SalvageReleased`, 1085 `PollutionDay`, 1086 `PollutionThresholdCrossed`, 1087 `ContaminationDay`, 1088 `EnvironmentalHarmApplied`, 1089 `InspectionRecorded`; 1090–1094 spare |
| 1095–1099 | I6 | Industry | industrial KPI day rows, if any |
| 1100–1129 | K1 | Knowledge | 1100 `DesignDiscovered`, 1101 `KnowledgeAcquired`, 1102 `KnowledgeForgotten`, 1103 `DesignFormalised`, 1104 `ArtefactCopied` (`Authorised` †), 1105 `ArtefactClosed`, 1106 `DesignLost`, 1107 `SkillChanged` (only if K1 owns the skill store; otherwise C7's 524), 1110 `ResearchProjectStarted`, 1111 `ResearchProgress` (HV), 1112 `ResearchProjectEnded`; others spare |
| 1130–1159 | K1 | Rights | 1130 `RightApplied`, 1131 `RightGranted`, 1132 `RightRefused`, 1133 `RightAssigned`, 1134 `LicenceGranted`, 1135 `LicenceEnded`, 1136 `RightRenewed`, 1137 `RightLapsed`, 1138 `RightExpired`, 1139 `RightChallenged`, 1140 `RightRevoked`, 1141 `RightSurrendered`, 1142 `UnauthorisedDesignUse` †, 1143 `InfringementPerceived`, 1144 `CompulsoryLicenceGranted`, 1145 `InjunctionOrdered`; 1146–1159 spare |
| 1160–1169 | K1 | Auction | 1160 `AuctionAnnounced`, 1161 `BidMade` (HV), 1162 `AuctionClosed`, 1163 `AuctionSettled`, 1164 `AuctionVoided`; 1165–1169 spare |
| 1170–1179 | K2a | Money | 1170 `PaymentMade`, 1171 `PaymentStep` (HV), 1172 `PaymentFailed`, 1173 `PaymentAbandoned`, 1174 `OrgBookDay`, 1175 `UnitOfAccountChanged`, 1176 `ObligationAccrued` (Tier 3), 1177 `MediumOfExchangeObserved`; 1178–1179 spare |
| 1180–1189 | K2b | Money | 1180 `MoneyFounded`, 1181 `MoneyIssued`, 1182 `MoneyDestroyed`, 1183 `AccountOpened`, 1184 `AccountClosed`, 1185 `AccountTransferred`, 1186 `DepositAccepted`, 1187 `WithdrawalPaid`, 1188 `NotesIssued`, 1189 `NotesRedeemed` |
| 1190–1199 | K2a (catalogue), emitted by C2's registry API | Money | **non-loan obligations** (sources `Supply`, `Bill`, `LicenceFee`, `Royalty`, `AuctionPrice`, `Damages`, `Deposit`, `Tax`, `Fee`): 1190 `ObligationOpened`, 1191 `ObligationSettled`, 1192 `ObligationDefaulted`, 1193 `ObligationWrittenOff`, 1194 `ObligationAdjusted` (K2b); 1195–1199 spare. Loans keep C2's 540–544 |

### 22.7 Warehouse migrations (extends 18.2)

| Migration | Owner | Contents |
|---|---|---|
| `0031_industry_production.sql` | I1 | `dim_deposit`, `fact_deposit_knowledge`, `fact_extraction`, `dim_facility`, `fact_production_order`, `fact_production_cycle`, `fact_facility_day`, `fact_embodied_matter`, ledger reasons 6–13 in `fact_goods_ledger_day` |
| `0032_vehicles.sql` | I2 | VEH §7 tables |
| `0033_industrial_trade.sql` | I3 | `fact_supply_agreement`, `fact_supply_delivery`, `fact_industrial_sale` |
| `0034_energy.sql` | I4 | ELE §7 tables |
| `0035_military_industry.sql` | I5 | IMIL §7 tables |
| `0036_waste_environment.sql` | I1 (waste part) | WST §7 tables |
| `0037_industry_kpi_views.sql` | I6 (with Q1) | `v_kpi_ind_*`, `v_kpi_veh_*`, `v_kpi_ele_*`, `v_kpi_wst_*`, `v_kpi_imil_*` over 0031–0036 only |
| `0038_money_claims.sql` | K2a | `fact_payment`, `fact_payment_lot`, `fact_journal_line`, `fact_org_book_day`, `fact_unit_of_account_spell`, `fact_obligation`, `fact_obligation_event`, `fact_claim_balance_day`; `dim_code` sets |
| `0039_knowledge_designs.sql` | K1 | `dim_design`, `fact_knowledge_spell`, `fact_knowledge_transfer`, `fact_design_discovery`, `dim_artefact`, `fact_artefact_copy`, `fact_skill_spell` (if K1 owns skills), research tables (empty until Tier 3) |
| `0040_rights_licences.sql` | K1 | `dim_right`, `fact_right_holder_spell`, `fact_licence`, `fact_right_event`, `fact_patent_law_spell`, `fact_infringement` |
| `0041_knowledge_money_kpi_views.sql` | Q1 (with K1, K2) | `v_kpi_knw_*`, `v_kpi_mon_*`, `v_ledger_nine_categories` and **every view that joins an industrial table to a money, claim, knowledge or rights table** (for example ELE-05 utility cash flow) |
| `0042_monetary_institutions.sql` | K2b | `dim_money`, `fact_money_issuance`, `fact_account_spell`, `fact_account_balance_day` |
| `0043_auctions.sql` | K1 (Tier 3) | `fact_auction`, `fact_auction_bid` |
| `0044`–`0049` | free (lead allocates) | |

**Cross-reference rule.** A migration references only lower numbers (18.2). Where an industrial table needs an id from a higher-numbered table (`0033`'s `payment_id`, `0034`'s `obligation_id` on `fact_meter_bill`, `0031`'s `design_id`), it is a **plain id column with no foreign key and no view**; every join across that boundary lives in `0041` or later. So `0031`–`0037` apply on a run without K1 or K2, and `W-D6-7 Migration_0031_To_0043` checks each intermediate version. R18 is unchanged (`0026` early views, `0030` late views).

### 22.8 Neutrality pins and save

| Pin | Statement | Landed by (re-run by) |
|---|---|---|
| **P10** `Industry_Off_Reproduces_Base` | every D5 switch, `Industry.Enabled`, `Civ.Knowledge.Enabled` and `Civ.Money.Enabled` at 0: every part hash of the P1 scenario set (seeds 1–3, every checkpoint) equals the base, i.e. the ware, ledger, enum, `MaxCand` and dispatch widening of I1a is neutral | I1a (every later I/K merge) |
| **P11** `Industry_Idle_Is_Neutral` | `Industry.Enabled = 1`, no `deposits` block, `Geology.Generate = 0`, `Waste.Enabled = 0`, no seeded assets: every part except `industry` equals P10 | I1a as a stub (I1, I2, I4 re-run it with their systems) |
| **P12** `Knowledge_Idle_Is_Neutral` | `Civ.Enabled = 1`, `Civ.Knowledge.Enabled = 1`, empty `Designs`, no seeded knowledge: every part except `civ` equals the same run with `Civ.Knowledge.Enabled = 0` | I1a as a stub (K1 re-runs) |
| **P13** `Money_Idle_Is_Neutral` | `Civ.Enabled = 1`, `Civ.Money.Enabled = 1`, `RecordPersonSales = 0`, no seeded money: every part except `civ` equals the same run with `Civ.Money.Enabled = 0` | I1a as a stub (K2a re-runs) |
| P6 `Switch_Matrix` | extended with `Industry.Enabled`, `Civ.Knowledge.Enabled`, `Civ.Money.Enabled` (each alone, pairwise with `Civ`, all on) | Q3 after every I/K merge |
| P7 `Recording_Neutral_On_New_Domains` | extended to domains 70–79 (W-D6-5) | Q1 |

The stage 3 hash `4bd54b271857a803` must hold at every I/K merge. **Save:** I1a changes the saved shape (new optional slots), so the lead bumps `SaveFile.CurrentVersion` in the merge commit of the next checkpoint after it (section 17: **F → 10** if I1a merges before the freeze; otherwise **G → 11**, which then also covers I1–I6, K1 and K2). Each package's `*_SaveLoadParity` saves at its own mid-state: a running cycle with three queued orders (I1); a loaded vehicle mid-trip with its driver (I2); an open supply agreement with an unpaid delivery (I3); mid-interval during an outage (I4); an open procurement (I5); mid-teaching with a live document (K1); a two-step payment in progress and an open bill (K2a).

### 22.9 Additive rows in other packages' owned (not frozen) tables

These are not frozen files, so I1a does not land them. The **requesting** package adds them as a flagged, additive, lead-reviewed edit after the owning package has merged (the R28 pattern); until then the stated fallback holds.

| Row | Table owner | Added by | Fallback until then |
|---|---|---|---|
| `JobKind.Operate` (operate vehicle V) | C3 | I2 | owner-operators and cooperative members only |
| `DivineActs.Map` rows for 22 Board, 23 Alight | D2 | I2 | refused `act_unavailable` |
| Purposes `SupplyPower` (I4), `Transport` (I2), `WasteService`/`Recycle`/`Regulate` (I1 waste); policies `PortPriority` (I4), `RespectRights` (K1, default on) | C6 | as listed | NPC-owned facilities; rights respected by the owner's own score term |
| Purposes `Research`, `RegisterRights`, `Adjudicate`; offices `Researcher`, `Examiner`, `Clerk`, `Librarian`; powers `Examine`, `Register`; the four templates (KNOW §13); `ServiceKind` 20–23 | C6 | K1 | no patent office: patents unavailable, contract licences only |
| `Purpose.Issue`, power `Issue` (K2b); policies `UnitOfAccount`, `AcceptsInPayment`, `PaysWagesIn`, `TariffPer`, `PriceList`; charter term `MayIssueBySpending` (K2b) | C6 | K2a / K2b | commodity money only |
| Obligation sources `Supply` (I3), `Bill` (I4), `LicenceFee`, `Royalty`, `AuctionPrice`, `Damages` (K1), `Deposit` (K2b), `Tax`, `Fee`; emission of 1190–1194 for non-loan sources; `Books.Post` calls on open, adjust, close; `Accrue`, `Adjust` (Tier 3) | C2 | K2a (the source values requested by I3, I4, K1) | spot payments only; no bills or deliveries on credit |
| `Pay` (101) as the general payment act creating the `Payment` record | C3 | K2a | wages as today |
| `Teach` topic extended to `DesignId` | C7 | K1 | knowledge only by reading, practice and seeding |
| complaint kind `Infringement` | C9 (deferred) | K1 | K1 records a complaint stub at the court organisation (the Tier 2 end point of D6.8) |
| `Law.Kind = PatentLaw` and its rules | C10 | K1 | a scenario-seeded patent law on the government alias (DEV_LOG temporary mechanism) |
| `Permits.Believed` extended with rights beliefs | C1 | K1 | no rights beliefs |
| `ArchivedLife.Know` | D7 | K1 | knowledge not archived |
| `RevealTechnology` handler body (MiracleKind 11) | D3 (calls K1) | K1 | refused (R4), Tier 3 |
| `AttackResolved` asset fields (version bump) | Military (400 block) | I5 | no vehicle combat (Tier 3) |
| KPI rows IND, VEH, ELE, WST, ENV, IMIL; KNW, MON | Q1 (`dim_kpi`, KPI_CATALOGUE) | I6; K1/K2 with Q1 | — |
| People-panel and inspection tabs for facilities, vehicles, stations | U1 (`GodHud` registration hook, R9) | I6 | — |

### 22.10 Invariants and required tests

| Category | Invariants (owning doc) | Required tests (owner) |
|---|---|---|
| **Matter, waste included** | C1 over all 72 wares and all 16 reasons in both directions (IND-I1, WST-I1); extraction = deposit depletion (IND-I2); embodied in = embodied held + released, salvage ≤ embodied (IND-I3, VEH-I6); recipe balance (IND-I4); no lot changes holder without an actor within reach (C8, IA-6) | `Ledger_WideningNeutral` (I1a); `Recipe_ConsumesAtStartProducesAtEnd`, `Extraction_DepletesFinitely`, `Factory_ProducesAssetWithEmbodiedMatter`, `Waste_LedgerExact`, `Industry_NoActorZeroGoodsCalls` (I1); `Vehicle_FuelBurnExact`, `MoveBin_OnlyFromIndustryHooks` (I2) |
| **Energy** | per interval and component `Σ generated = Σ delivered + Σ curtailed + loss` (ELE-I1); a station-less component gets 0 (ELE-I2); fuel burned ↔ generated within one unit (ELE-I3); no generation below `StaffMin` (ELE-I4); meters ≥ bills (ELE-I6) | `Grid_LedgerIdentity`, `Station_BurnsExactFuel`, `Station_NeedsStaff`, `Grid_ComponentsDeterministic` (I4) |
| **No money creation** | IA-2; MON-I1 (K2b), MON-I2, MON-I4, MON-I5, MON-I7, MON-I9; BK-I1..I4 | static `Industry_NoMoneyApi`, `Grid_NoMoneyApi`, `NoExternalBuyer`, `MoneyWriters_OnlyK2` (I1a guarding form; owners I1, I4, K2a); MON-U2 (unload creates no money), MON-U3 (generation creates no money; only a billing act opens a bill); W-D6-4 `NoMoneyFromIndustry`; ACCEPTANCE D6.1, D6.3 |
| **Claims** | per unit Σ claims = Σ liabilities (MON-I3); settlement only by payment, delivery, write-off, forfeiture or ruling (MON-I8) | K2a with C2: `Claims_EqualLiabilities` daily; ACCEPTANCE D6.2 |
| **Knowledge copy** | no act lowers another person's knowledge (KN-I1, static writer list); AR-I1 (one live record per `Document` lot) | KN-U1 (teacher's entry byte-equal), KN-U2 (copy leaves the source unchanged), KN-U3 (K1); ACCEPTANCE D6.5 |
| **Rights are not goods** | RT-I1..I5; an assignment or licence changes no lot, asset, `NextLotId` or `KnownDesign`; no asset row is caused by a right, auction or knowledge event (W-D6-1 rules 7, 8); IA-3 | KN-U4, KN-U5, KN-U6, `Rights_NeverGate` (static + D6.8: an unlicensed builder is not blocked; exactly one `UnauthorisedDesignUse`) (K1); ACCEPTANCE D6.6, D6.7, D6.8 |
| **Vehicles and assets** | VEH-I1..I7; a vehicle is never an `Npc` (IA-4); one running cycle per facility, queue ≤ 8 (IND-I8); labour attributed (IND-I7) | `Vehicle_NoNpcRecord`, `Vehicle_MovesOnlyWithDriver`, `Vehicle_BinsFollow`, `Vehicle_LoadUnloadReachChecked` (I2) |
| **People** | P-C population conservation; no person created by industry (IMIL-I1) | `IMil_NoSoldierCreated` (I5); `Industry_NoOwnershipModelBranch` (I1) |
| **Determinism, recording, save** | pins P10–P13 (22.8); recording neutral (P7, W-D6-5); `*_SaveLoadParity` per package (22.8) | `Vehicle_ReplayExact` (I2), `Grid_ReplayExact` (I4), ACCEPTANCE D6.10, W-D6-2, W-D6-7, H-GS-12, H-GS-13 (Q3 with I/K) |
| **Vertical slice** | D6.18 steps 1–10 in one run on `ind_slice` (seeds 1–3); a step whose package has not merged is reported *not run*, never passed | `IndustrialSlice_EndToEnd` (ACCEPTANCE D6.11) with the per-step `VS1_…` to `VS10_…` (Q3 with I1–I6, K1, K2) |

### 22.11 Decisions on every proposal (accept, renumber, reject)

| # | Proposal (source) | Decision |
|---|---|---|
| 1 | `TelemetryDomain` 70–75 → 1000–1099, blocks I1 1000–1019, I2 1020–1039, I3 1040–1049, I4 1050–1069, I5 1070–1079, waste 1080–1094, I6 1095–1099 (IND §12) | **Accepted** (22.6) |
| 2 | Domains 76–79 → 1100–1199; K1 knowledge 1100–1129, rights 1130–1159, auctions 1160–1169; money 1170–1189 (KNOW K1-R11, MONEY K2-R7) | **Accepted**; block **1190–1199** added for non-loan obligations (row 35) |
| 3 | Migrations 0031–0037 (IND §12) | **Accepted** |
| 4 | Migrations 0038 K2a, 0039 K1, 0040 K1, 0041 KPI views, 0042 K2b, 0043 auctions (DATA_WAREHOUSE §13.4, K1-R12, K2-R8) | **Accepted**, with the cross-reference rule (22.7); 0044–0049 free |
| 5 | Wares 40–53, waste 56–62, coal 42 (IND §3.1, WST) | **Accepted** (coal is a new ware; `Ware.Ore` unchanged, IND open question 1) |
| 6 | `Document` 63, `Paper` 64 (K1-R4) | **Accepted** |
| 7 | `Coin` 65 (K2-R9, optional) | **Accepted** as an enum value landed by I1a; no recipe required; tuning default `SliceCommodity = Bronze`, Iron Coast uses `Coin` |
| 8 | `Wares.PersonalCount` 72 with hash-only-when-non-zero (IND I1a.1) | **Accepted** (22.3) |
| 9 | Ledger widened to 16 reasons with the same trick; produce 6–9, consume 8–12 (IND §3.2); `ConsumeReason.Research = 13` (K1-R5) | **Accepted** |
| 10 | `Goods.MoveBin` (IND I1a.4) | **Accepted** as a frozen-API amendment; one caller |
| 11 | `MoveReason.BinTransfer = 13` | **Accepted** |
| 12 | `OwnershipBasis.SupplyAgreement = 25` (I3), `Paid = 26` (K2-R3, R34) | **Accepted** |
| 13 | `Wares.IsUnique` replacing the axe-only check (K1-R4, R34) | **Accepted**, landed by I1a, behaviour-neutral |
| 14 | Vehicle = C1 productive asset, not an `Npc` (VEH-1) | **Accepted**; `AssetId` is C1's (R20) |
| 15 | `PopulationExtensions.StepTicks`, `Moved` (IND I1a.7) | **Accepted**, plus a third site **`PathCosts`** for VEH §4.3's cost profile (otherwise an undeclared seam) |
| 16 | `ThinkScratch.MaxCand` 36 → 40 | **Accepted** |
| 17 | `NpcMind.Industry` (IND I1a.6), `NpcMind.Know` (K1-R2) | **Accepted**, with **lazy creation** so P11/P12 hold |
| 18 | `NpcVerb` 200–208 + 209 Industry; 210–219 K1/K2 (IND §9.2, K1-R6, R34) | **Accepted**; members named in 22.5 |
| 19 | Switches `Industry.Enabled`, `Civ.Knowledge.Enabled`, `Civ.Money.Enabled` (all 0); `Industry.Geology.Generate`, `Industry.Waste.Enabled`, `Civ.Money.RecordPersonSales`, `Civ.Money.SliceCommodity` | **Accepted** (22.4) |
| 20 | Pins P10, P11 (IND I1a.9) | **Accepted**; **P12, P13 added** for K1 and K2 |
| 21 | System order 262 `ElectricitySystem`, 264 `IndustrySystem` (IND §4.10, ELE §4.2) | **Accepted**; separate K1/K2 systems **rejected**: their day-start work runs as `CivSystem` (257) partials |
| 22 | `GodState.Industry` and the `industry` hash part after `divine` (IND I1a.5) | **Accepted** |
| 23 | `CivState.K1`, `CivState.K2` hashed after C5 (K1-R1, K2-R1, R34) | **Accepted** (order C1, C2, C3, C5, K1, K2) |
| 24 | `ObjClass` 110–129 incl. `Vehicle = 117` (IND §9.2, R15) | **Accepted**; 122 `Laboratory` added for K1; 123–129 spare |
| 25 | `DivineAct` 22 Board, 23 Alight (VEH §4.7, R15) | **Accepted**; enum values by I1a, map rows by I2 |
| 26 | Learning verbs 21–29 incl. 21 Board, 22 Drive (IND §9.2, R15) | **Accepted**; **30 OperateSwitch, 31 ReadMeter added** (ELE's `Operate` and `ReadMeter` acts had no row) |
| 27 | K1 learning rows 114–119 for Study, CopyDocument, WriteDesign, Research, ApplyForRight, Bid (K1-R6) | **Renumbered and reduced**: 114–115 are Orgs spares and 116–118 are package D6's `Pray`, `Worship`, `MakeOffering`. Now Study **102**, WriteDocument **103** (write and copy share one row), Research **63**; ApplyForRight and Bid **rejected as new rows** (reuse `RequestService` 106 and M5 `Say(Offer)`); K2's billing act gets **127 IssueBill** |
| 28 | `TopicKind` 11 Deposit, 12 Job/Order (IND §9.2) | **Accepted** (12 named `Order`); 13 `Design`, 14 `Infringement` added for K1 |
| 29 | `SituationKind` 128–143, `SelfFeature` 72–79, `Role` 11–13, `NpcGroup` 11 (IND §9.2) | **Accepted**; 132 `StrategicStrike` named (IMIL §4.7); K reservations added |
| 30 | Episode-key kinds 40–47 (IND); 19–21 (K1-R3) | **Accepted**; 22 payment added for K2a |
| 31 | `ServiceKind` 20–23 (K1-R8, R34) | **Accepted**, rows added by K1 (22.9) |
| 32 | Licence as a truth gate: IND §4.4 step 1 ("the owner holds a licence or the design is open"), ELE §4.1 ("its owner must hold the design's licence or open access") | **Rejected** (R33, IA-3). Both documents are corrected in this revision |
| 33 | `Licensed` flag on `ProductionCycleStarted` (IND §11, KNOW §9) | **Accepted** as an analytics truth field † |
| 34 | External ore buyer for credits (first brief) | **Rejected** (D6.2); no successor, no seeded "market" organisation except a real buyer with real holdings listed as a temporary mechanism (MONEY §10) |
| 35 | `ObligationAccrued` 1176 "if C2 does not own it"; generic obligation events "C2 names" (MONEY K2-R7, DATA_WAREHOUSE §13.5) | **Accepted at 1176** (Tier 3); non-loan obligation events are **1190–1194**, catalogued by K2a, emitted by C2's API |
| 36 | `SkillChanged` 1107 "if C7 does not own it"; skill slots "whichever of C7 and K1 merges first" (KNOW §5.3) | **Accepted** with the one-accessor rule (22.4); the lead records the owner in DEV_LOG |
| 37 | Industry hooks dispatched after Divinity (IND I1a.5) | **Accepted**, including the `Kill` order (22.4) |
| 38 | K1/K2 candidates in the Civ quota (K1-R6) | **Accepted** |
| 39 | `ArchivedLife.Know` (K1-R2) | **Accepted** as a K1 additive edit after D7 |
| 40 | Scenario name: `ind_slice` (GAMEPLAY_SCENARIOS §13) vs `industrial_slice` (ACCEPTANCE D6) | **`ind_slice` is canonical** (`DotNet/God.Runner/Scenarios/ind_slice.scenario.json`, Q3); ACCEPTANCE's name is corrected in review |
| 41 | Save bump at the checkpoint that merges I1a (IND I1a.10) | **Accepted** (22.8) |
| 42 | C6, C2, C3, C7, C9, C10, D2, D7 table rows requested by the D6 documents | **Accepted as additive rows by the requesting package** (22.9), not by I1a |

### 22.12 Which package lands which shared change

| Change | Landed by | Never by |
|---|---|---|
| Everything in 22.1 (wares, ledger, goods API, state slots, hash parts, tuning roots, module calls, dispatch and sites, id ranges, mind slots, `DesignId` on assets, system shells, domains and catalogue stubs, mapping stubs, pins P10–P13) | **I1a** (lead-reviewed) | any I or K package |
| `SaveFile.cs` and the version | the lead | any package |
| Industrial bodies: deposits, recipes, runner, orders, waste (I1); vehicles (I2); agreements, sales (I3); grid (I4); military industry (I5) | their package, in its own partial files | another I package |
| Game presentation, cards, overlays, KPI views `0037` | I6 (Game layer only; never mutates sim state) | sim packages |
| Knowledge, documents, rights, auctions (K1); payments, books, monies (K2a/K2b) | their package in `CivState.K1/K2` and own files | I packages (they call the K1/K2 APIs only) |
| Rows in another package's owned tables | the requesting package by flagged additive edit after the owner merges (22.9) | I1a |
| Any shared change not listed here | an amendment row in section 20 by the lead | a package branch |

---

## 23. Unification addendum (D9, 28 Nov 2026)

**Status: binding** for every UN and UB package of the 28 November run. It settles the shared numbers and seams of the D9 architecture ([UNIFICATION_DESIGN §D9](../unification/UNIFICATION_DESIGN.md), "§D9"), implementing owner decisions D9.1–D9.14 and [LATEST_OWNER_AMENDMENT](../unification/LATEST_OWNER_AMENDMENT.md). It overrides the unification numbers proposed in UNIFICATION_DESIGN §13.4–13.6, R72, R75 and R76 where they differ (the lead records this as a ruling). Base: `unity/poc` at `fb6ca85` (save version 6, `MaxCand` 36, `Wares.PersonalCount` 40, `Drive.Count` 5, ledger 8 slots, I1a **not** merged). Paths are relative to `Assets/God/Sim/` unless they start with `DotNet/` or `docs/`. New code lives under `Unified/` (namespace `God.Sim.Unified`).

### 23.0 Who may change what

**Only UN0 and the lead modify frozen surfaces**: shared enums, `GodState`, `GodSim` (system install, hash parts), `SaveFile`, `EventCatalog`/`TelemetryDomain`, `Goods.cs`, `LotBin.cs`, `PopulationState.cs` enums, `Divinity/Control.cs` and `DivineTypes.cs`, `Comms/Message.cs`, `Economy/Wares.cs`, `PopulationSystem` dispatch sites, and the schema files UN0 creates (`Unified/Schema/*`). Every other package adds code in its own folder and partial files (`UnifiedState.UNn.cs`, `UnifiedHooks.UNn.cs`, `EffectHandlers.UNn.cs`, `UNnMappings.cs`). A missing seam is an amendment request to the lead; never a workaround. The save version is the lead's alone.

### 23.1 The unified switch

`SimTuning.Unification` (`UnifiedTuning`, `IOmitWhenDefault`, saved in the game's tuning, read by both hosts):

| Path | Default | Meaning |
|---|---|---|
| `Unification.Enabled` | 0 | 1 = unified game: `GodState.Unification` exists; goods have no title (23.4); legacy decision/economy paths off (23.3). **No sub-switch disables these two consequences.** |
| `World`, `Interactions`, `Construction`, `Exchange`, `Conflict` | 1 | behaviour sub-switches, effective only when `Enabled = 1`; 0 makes the corresponding effect kinds refuse with `Disabled` (for isolated tests) |
| `Needs.ActiveMask` | `0xFFF` | 12 need channels (23.6) |
| `Brain.Kind` | 0 | 0 `Untrained`, 1 `Trained`, 2 `Fallback` (always labelled); `Teacher` exists only in `DotNet/God.Train` and test fixtures |
| `Brain.Artifact` | 0 | artifact version; with `Kind = 1` the sim refuses to start if the embedded artifact hash does not match |
| `Brain.Planning` | 0 | bounded internal planning (§D9.2 e) |
| `Settlements`, `Roads`, `Groups` | 0 | UB |
| `Board.Radius`, `Board.SeaRing`, `Start.Tribes`, `Start.Adults`, `Start.Children`, `Start.SettlementsPerTribe` | 4 (land 3 + sea ring), 1, 6, 7, 5, 1 | scale configuration (§D9.6); never constants in code |

Helper: `Unified.On(GodState s)` (UN0). Every switch-gated site reads only this helper.

### 23.2 Neutrality pins (who lands, who re-runs)

| Pin | Statement | Landed by | Re-run by |
|---|---|---|---|
| **U-P1** `Unification_Off_Reproduces_Base` | `Unification.Enabled = 0`: every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head; Stage 3 smoke hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` (`DotNet/God.Runner.Tests/PolicyRunTests.cs:27`) unchanged | UN0 | every package and every merge |
| P1, P4, P8 (and P2, P3, P5, P9 where present) | existing D5 pins | existing | every merge |
| **U-P2** `Unification_FoundationOnly_Neutral` | UN0's widened enums, ledger and ware surface with `Enabled = 0` hash like the base (ware 40+ and ledger 8–15 hashed only when non-zero) | UN0 | UN9, UN11a |
| **U-P3** `Unified_NoLegacyTitle` | static guard: no `.Owner`, `OwnerRelation`, `Permits.`, `StoreAccess`, `RightOn`, `Truth(` reference in any file under `Unified/` or in a unified branch of a shared file; runtime: a unified 10-day run emits no `OwnershipChanged`, `TheftCommitted`, `UseRightGranted`, `EstateTransfer` | UN0 (static), UN9 (runtime) | UN11a |
| **U-P4** `Unified_OriginNeutral` (QA-01) | an identical primitive stream applied with origins Policy, Possessed and Fixture gives identical physical state hashes (`unification`, `population` parts) | UN2 | UN6, UN9, UN11a |
| **U-P5** `Unified_Recording_Neutral` | Off/Summary/Full recording of domains 80–88 gives one hash | UN11a | UN11a |
| **U-P6** `Unified_SaveReplay_Exact` | save at a mid-state (site half built, reservation open, held-out lot, strike in progress, person asleep in a bed) → load → continue equals the uninterrupted run; a replay of a unified run equals it at every checkpoint | UN2 (stub), UN11a (full) | every UN package with state |

Hashes move only at declared re-pins recorded in DEV_LOG by the lead.

### 23.3 Legacy isolation (unified mode)

With `Unified.On(s)`: `GodSim.InstallModules` skips `DiceSystem`, `EconomySystem`, `MilitarySystem`, `TradeSystem`, `FactionSystem` (VP, elimination), `EstateSystem`, `CivSystem`, `OrganisationSystem` (UN0); `PopulationSystem` runs `Body()` and then calls `UnifiedHooks.PersonTick(s, ev, npc)` instead of `Thinker.Think`/policy/`Act` (UN0 lands the site; UN6 fills it); `GodBootstrap.StartAi`/`AiHost` never attach a faction policy to a unified game (UN9); M4 estates, executor, `StoreAccess`, `SenseBins` filters and household economic verbs, M5 `Held`/own-lot filters and title basis, C1/C3/C6/C7 paths, `Ownership.Classify`, lost property and divine own-lot filters are unreachable (UN9 audit with the call-site list of [D9_CODE_CONFLICT_AUDIT](../unification/D9_CODE_CONFLICT_AUDIT.md) §8). Kept: `DivinitySystem`, `MiraclesSystem`, `LifeCycleSystem` (biology), `SocialSystem` memory/trust (fed by unified perception), `GodDirector`. Legacy events and warehouse tables keep their definitions and are simply not emitted (registry tests unchanged).

### 23.4 Physical goods without title (UN0 lands, inside `Goods.cs` behind `Unified.On`)

| Item | Unified behaviour |
|---|---|
| `Goods.OwnerAt` | always `Owner.None` |
| `Goods.Produce(…, owner, …)` | owner argument ignored (`None`) |
| `Goods.SetOwner` | returns false, emits nothing |
| `OwnershipChanged` | never emitted; `HandOver.OwnerChanged` always false; `FromOwner`/`ToOwner` `None` |
| `Goods.Mergeable` | ware, spoil day and uses equal and `!Wares.IsUnique(w)`; owner and provenance ignored |
| `PayOwnerChange`, `PayProduce`, `PaySpoiled` | pay nothing (no learning reward from goods or title) |
| Provenance | `UnifiedState.Origins`: sorted list of `LotOrigin { int Lot; int Producer; byte Reason; long Tick; byte SourceKind; int SourceId; bool Mixed }`, written by `Produce`, merged on `Insert` (Mixed, earliest tick), removed when the lot ends; hashed in the `unification` part |
| Legacy fields | `ItemLot.Owner`, `LotBin.Owner`, `OwnerKind`, `OwnershipBasis`, `UseRight` kept for legacy serialisation only; always `None`/empty in unified state |

**Ids landed by UN0** (all hashed only when non-zero, so U-P2 holds):

| Space | Values |
|---|---|
| `Ware` (int enum) | **Unification block 72–159**; `Wares.PersonalCount` 40 → **160**; 40–71 stay reserved for industry (§22.3). UA wares fixed now: 72 `Mushroom` (OBJ-044), 73 `WildEgg` (046), 74 `Rabbit` (047), 75 `WildVeg` (048), 76 `CactusFruit` (049), 77 `Seaweed` (050), 78 `Hay` (043), 79 `Sheep` (042, unique), 80 `Water` (039, container-only, UB), 81 `CookedFish` (054), 82 `Bedding` (084), 83 `Bed` (092, u), 84 `Stool` (097, u), 85 `Bench` (127, u), 86 `Toy` (132, u), 87 `CarvedOrnament` (108, u), 88 `StorageChest` (107, u), 89 `Cradle` (103, u), 90 `Latrine` (136, u), 91 `Waste` (114), 92 `StoneHammer` (068, u), 93 `SleepingMat` (093, u; founder endowment in UA). Reused existing: `Log` 1 (OBJ-024 Wood), `Clay` 2 (041), `Stone` 5 (016), `Plank` 7 (063), `Axe` 20 (069 Stone axe; NPC tool only in unified mode), `Roots` 29 (045), `Fish` 30 (012, raw, nutrition 0 in unified), `Herbs` 32 (075). 94–159 allocated by UN3a/UB in manifest order (append only). `Wares.IsUnique` covers `Axe` and every "u" ware. |
| Catalogue ids | `CatId` (ushort): objects `1000 + nnn` (OBJ-nnn), buildings `2000 + nnn` (BLD-nnn); immutable; aliases map names to ids; a `CatId` is a definition id, never an instance id |
| `ProduceReason` (ledger 16 slots, §22.3 layout) | 10 `Collected` (raw via a collection building), 11 `Drawn` (water into a container, UB), 12 `Excreted` (latrine waste); 6–9 keep §22.3's industry meanings and are unused here |
| `ConsumeReason` | 8 `ProcessInput` (§22.3 meaning: recipe input), 14 `Drunk` (UB), 15 `Washed` (UB); `Built` 2 for site materials at completion; `Eaten` 0 |
| `BinKind` | 7 `Storage` (building store), 8 `Site`, 9 `Fixtures` (installed furnishings), 10 `WorkInput`, 11 `WorkOutput`, 12 `Container` (inside an installed chest); 13–15 reserved |
| `MoveReason` | 14 `Contribute` (carry → site), 15 `Install`, 16 `Uninstall`, 17 `WorkInput`, 18 `Grab` (from an incapacitated holder); `PickUp` 1, `Drop` 2, `Deposit` 3, `Withdraw` 4, `HandOver` 5, `DeathSpill` 9 keep their meaning; 13 `BinTransfer` stays §22.3's; 19–31 reserved |
| `ObjClass` | 130 `RawNode`, 131 `ForagePlace`, 132 `WaterPoint`, 133 `ConstructionSite`, 134 `UnifiedBuilding`, 135 `Fixture`, 136 `Settlement`, 137 `Road`; 138–149 reserved (inspection and `MindView` compatibility only) |
| `GestureAct` | 28 `Claim`; existing `Offer 2`, `Promise 8`, `Accept 3`, `Refuse 4`, `Want 1`, `Surrender 11` (submission), `Accuse 13` (protest), `Threaten 26`, `Teach 18`, `Demonstrate 21`, `Greet 25`, `Thank 24` are given meaning only by learning; 29–31 reserved |
| `ControlInputKind` | 5 `Primitive` (carries a `PrimitiveControl`; amendment to the F1 seam, landed by UN0) |
| Not added | **no learning `Verb` rows, no `NpcVerb` rows, no `MaxCand` change** (stays 36), no `SelfFeature`/`SituationKind`/episode-key rows: the unified brain does not use the legacy candidate path (R81). R72's 128–139 / 220–229 and R76's `MaxCand` 40 are withdrawn. |

### 23.5 Primitive control set and executor API (owner UN2; types frozen by UN0)

```csharp
namespace God.Sim.Unified
public enum Prim : byte { None = 0, TurnLeft = 1, TurnRight = 2, StepForward = 3, StepBack = 4, StepLeft = 5, StepRight = 6,
                          Take = 7, Drop = 8, Use = 9, Gesture = 10, Sleep = 11, Wait = 12 }
public enum TargetKind : byte { None = 0, Self = 1, Npc = 2, Lot = 3, Node = 4, Facing = 5, Building = 6, Site = 7,
                                Fixture = 8, ForagePlace = 9, RawNode = 10, WaterPoint = 11, Placement = 12 }
public struct TargetRef { public TargetKind Kind; public int Id; }          // Placement: Id = node, design in Design
public struct Referent  { public byte Kind; public int Id; public short Ware; }   // ware, lot, person, place
public struct PrimitiveControl {
    public Prim Control; public int Actor;
    public int SourceLot;                 // 0 = empty hand; else a lot the actor carries or has equipped
    public TargetRef Target; public byte Slot;        // interaction slot 0-3 (0 = first matching row)
    public ushort Design;                 // CatId of a known design (USE placement)
    public byte Gesture; public Referent R1, R2; public short Quantity;   // GESTURE (quantity 0-999)
    public uint Corr;                     // optional correlation id (exchange); never enforced
}
public enum ControlOrigin : byte { Policy = 0, Possessed = 1, Compelled = 2, Avatar = 3, Fixture = 4, Teacher = 5 }
public enum MotorStatus : byte { Accepted = 0, Busy, Invalid, Dead, Asleep, OutOfReach, Blocked, NoRoom, NotEnough,
                                 NoInteraction, NotKnown, BadSite, Occupied, Full, Resisted, Disabled }
public static class Motor {   // UN2
    public static MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin);
    public static void Tick(GodState s, EventBuffer ev);            // advances action progress, ascending actor id
    public static bool Busy(GodState s, int actor);
    public static void Interrupt(GodState s, EventBuffer ev, int actor, byte reason);  // pain, collapse, strike, death
    public static void Legal(GodState s, int actor, in Percept p, ref ActMask mask);   // physical legality only
}
```

Rules: one action in progress per body; durations in ticks from data; the result of every submission is an observable proprioceptive input for the actor's next decision (`ControlFailed` 1221 for failures); origin changes nothing physical and is recorded on events; possession input `ControlInputKind.Primitive` is validated by `Embodiment.CheckAction` against the same `Motor.Submit` (a host may expand a click-to-move into step primitives labelled `Possessed`; the sim never paths for an NPC). `TAKE` covers ground, storage, workstation output, container, a person's **held-out** lot, and a person who is asleep, unconscious or dead (`MoveReason.Grab`); from an awake person's ordinary carry it returns `Resisted` and is perceived by that person.

### 23.6 Observation/action interface for the brain (owner UN2; schema frozen by UN0)

- `Unified/Schema/ObsSchema.cs` and `ActSchema.cs` (UN0): version constants (`Obs v1`, `Act v1`), field order, lengths, fixed-point scale (×1000) and a schema hash (`SchemaHash`, SHA-256 of the canonical layout text), exactly as §D9.2 a. A change takes a new version and a new hash and invalidates artifacts.
- `Percept.Encode(GodState s, int actor, ref Percept p)` and `Percept.ToVector(in Percept p, short[] obs)` (UN2): local only, through `ISpatialIndex` (UN0 interface; UN2 ships a correct simple implementation; UNP may replace it behind the interface); no truth query beyond what the person can perceive; no owner field.
- `IPrimitivePolicy { PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng); }` (UN0 interface; UN6 implements inference, overlay and fallbacks; UN6t produces artifacts). `BrainState` is per person: recurrent state (64 × int16), memory slots (8 places, 4 people, claims ring of 8), knowledge bitsets (designs, recipes, interactions over `CatId` indices), skills, overlay store id; bounded (≤ 4 KB).
- **Needs extension:** `UnifiedNpc { short[7] Needs /* Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room */; … }` in `UnifiedState.People` (sorted by NpcId; hashed in `unification`), alongside the existing five drives; the encoder exposes all 12 in the order Hunger, Energy, Warmth, Safety, Pain, Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room.
- Knowledge acquisition, teaching and overlay updates are UN6's; claims records are written by UN7 through `Brain.AddClaim(...)` (UN6 API frozen at UN6 merge).

### 23.7 Interaction data schema and catalogue (schema UN0; content and generator UN3a)

- Reviewed source: `docs/unification/catalogue/catalogue.json` (every one of the 12 controls, 12 needs, 138 objects, 65 buildings with `id` OBJ-/BLD-, `catId`, name, aliases, workbook row, workbook SHA-256 `f764df01…05a0`, `stage` UA-initial/UB/Later/Superseded, `fix` codes, status implemented/staged/deferred/rejected, reason). Only UA-initial rows with their corrections applied become active content in UA.
- Generator and validator: `DotNet/God.Catalogue` (no external dependency) → `Unified/Catalogue/Generated/*.g.cs` (deterministic tables); test `Catalogue_GeneratedMatchesSource` fails on drift; `Catalogue_WorkbookShaPinned`; `Catalogue_UAClosure` (every UA input is UA, forage or a dice raw); `Catalogue_NoCycleWithoutBootstrap`; CATALOG-1 coverage report.
- Records (UN0, `Unified/Schema/CatalogueDefs.cs`): `ObjectDef { CatId; Ware; Unique; Weight; Nutrition; StrikeAttack; Defence; ToolMultiplier[]; Interactions[] }`, `BuildingDef { CatId; Levels[] { Labour; Materials[]; Storage; SleepSpots; FixtureSlots; CollectWare; CollectRadius }; Placement rule; Capabilities; Interactions[]; Recipes[] }`, `RecipeDef { Id; Station CatId; Inputs[]; Outputs[]; LabourTicks; ToolBonus }`, `InteractionDef { TargetKind; TargetCat; Slot; SourceReq (EmptyHand | Ware | Class | Tool); Reach; Ticks; Effects[] }`, `EffectDef { EffectKind Kind; short A, B, C }`.
- `EffectKind` (byte, UN0, append-only): 1 `Forage`, 2 `DrinkWash`, 3 `Eat`, 4 `NeedRelief`, 5 `CollectRaw`, 6 `Deposit`, 7 `Install`, 8 `Occupy`, 9 `WorkInput`, 10 `RecipeWork`, 11 `Fuel`, 12 `PlaceSite`, 13 `Contribute`, 14 `Labour`, 15 `HoldOut`, 16 `Strike`, 17 `Equip`, 18 `Treat`, 19 `Relieve`, 20 `Play`; 21–63 reserved. Handlers register through `EffectRegistry.Register(kind, handler)` in fixed registration order (UN0 dispatch). Owners: UN1 (1, 2), UN3b (3–11, 19, 20), UN4 (12–14), UN7 (15), UN8a (16–18).

### 23.8 Construction site, building and world entities (record shapes frozen by UN0)

```csharp
public sealed class ConstructionSite { public int Id; public ushort Design; public byte Level; public int Node; public byte Facing;
    public int Bin;            // BinKind.Site: delivered materials, consumed (Built) only at completion
    public int LabourDone, LabourNeed; public int UpgradeOf;      // building id for an upgrade, else 0
    public int Founder; public long Placed, LastWork; public List<Contribution> Contributors; }   // provenance, bounded 16
public struct Contribution { public int Npc; public int Labour; public short Ware; public short Units; public long Last; }
public sealed class Building { public int Id; public ushort Cat; public byte Level; public int Node; public byte Facing;
    public int Condition; public int Storage, Fixtures, WorkIn, WorkOut;   // bin ids (0 = none)
    public List<int> Occupants; public int Founder; public List<Contribution> Contributors; public long Completed; }
public struct Reservation { public int Building; public int Npc; public short Units; public long Expires; }
public sealed class RawNode { public int Id, Hex, Node; public short Ware; public int Stock, Cap; }
public sealed class ForagePlace { public int Id, Node; public short Ware; public int Ticks; }
public sealed class WaterPoint { public int Id, Node; public bool Fresh; public int Building; }
```

All in `GodState.Unification` (`UnifiedState`, partial; sub-lists sorted by id; ids from `UnifiedState` counters, never reused). Owners: `Buildings`/`Reservations` behaviour UN3b, `Sites` UN4, `RawNodes`/`ForagePlaces`/`WaterPoints`/allowances UN1, `People` needs UN1 (rates) and UN6 (brain state), `Exchange` UN7, `Conflict` UN8a. Creation only through `Buildings.Create`, `Sites.Place`, `World.*` APIs of the owners. No record has an owner, right or permission field.

### 23.9 Exchange correlation and claims

- `UnifiedState.MessageExt`: `MsgExt { int Message; Referent R1, R2; short Quantity; uint Corr; }` for every unified `GESTURE` (the M5 `Message` layout is unchanged; `DealId` is not used in unified mode).
- Hold-out: `HoldOut { int Npc; int Lot; int To /* 0 = anyone */; uint Corr; long Expires; }`; a receiver's `TAKE` of a held-out lot completes `Goods.Move(MoveReason.HandOver)`; expiry leaves the lot in the giver's carry. Each actual transfer is recorded independently (`HandedOver` 1281 with `Corr`).
- `Corr` (uint, allocated by the first gesture's speaker as `UnifiedState.NextCorr++`) correlates messages and hand-overs for memory and analytics only; nothing reads it to lock, settle, enforce or award.
- Claims are brain records (`Claim { Claimant; SubjectKind; SubjectId; Source (Own, Observed, Told); Confidence; Tick; Expiry }`), communicated by `GestureAct.Claim` with referents; contradictory claims coexist; no claim is world state.

### 23.10 Strike and injury events (owner UN8a)

A strike is `USE` (empty hand or a strike-capable held lot) on a reachable person, slot `strike`: duration and fatigue from data; hit/block/damage by `Military/Duel.Resolve` on `SoldierStats` built from the body (strength trait, fatigue, injury, held attack, equipped defence) with a new `Rng` seeded by a fixed hash of (game seed, "strike", tick, actor, target) (UN8a adds the helper; no shared stream is advanced, so order cannot change results); damage by `Bodies.Damage(s, ev, victim, micro, DamageCause.Violence, actor, 0)`. Events: 1300 `StrikeAttempted` (actor, target, held ware), 1301 `StrikeResolved` (result, damage), 1302 `StrikeWitnessed` (Full: witness, actor if perceived else 0, victim), 1303 `FearFromViolence` (HV), 1304 `RemedyApplied`. The victim's memory names the actor only if the actor was perceived. No theft, crime, guilt or reputation fact is emitted.

### 23.11 Invariants (unified mode; UN0 lands U-C1–U-C6, owners extend)

U-C1 ledger conservation `Σ produced − Σ consumed = Σ (carry + ground + all bins)` per ware (existing C1, extended to 160 wares and 16 reasons); U-C2 one holder per unit; U-C3 no bin over capacity, no carry over weight (existing C5); U-C4 (replaces C4) **no lot is deleted or moved without an event**, a dead person holds nothing, nothing teleports; U-C5 every `Owner` field is `None`; U-C6 site bin contents ≤ the design's requirement, reservations ≤ storage room and each expires. Legacy C4, `HouseholdInvariants` estate rules, ORG4, CIV1, CIV3 run only in legacy mode.

### 23.12 Events and telemetry domains

`TelemetryDomain` 80 `UnifiedWorld`, 81 `Motor`, 82 `Construction`, 83 `Interaction`, 84 `Exchange`, 85 `Conflict`, 86 `Brain`, 87 `Settlement` (UB), 88 `Groups` (UB) → `RangeOf` **1200–1399** (after industry's 1000–1199; no collision). Each block belongs to one package's partial catalogue array:

| Codes | Owner | Events |
|---|---|---|
| 1200–1219 | UN1 (1206–1207 UN5) | 1200 `ForageCollected` (HV), 1201 `AllowanceGranted`, 1202 `AllowanceWasted`, 1203 `RawMaterialised` (HV), 1204 `WaterUsed` (HV), 1205 `UnifiedDay`, 1206 `TribeStarted`, 1207 `FounderEndowment` |
| 1220–1239 | UN2 | 1220 `ControlApplied` (Full: actor, control, origin, status), 1221 `ControlFailed` (HV: reason), 1222 `ActionInterrupted`, 1223 `Equipped`, 1224 `Unequipped` |
| 1240–1259 | UN4 (1250–1251 UN3b) | 1240 `SitePlaced`, 1241 `SitePlaceFailed`, 1242 `MaterialContributed` (HV), 1243 `LabourContributed` (HV), 1244 `BuildingCompleted`, 1245 `UpgradeSitePlaced`, 1246 `UpgradeCompleted`, 1247 `BuildingDamaged`, 1248 `BuildingDestroyed`, 1249 `SiteContentsDropped`, 1250 `FixtureInstalled`, 1251 `FixtureRemoved` |
| 1260–1279 | UN3b | 1260 `InteractionStarted` (HV), 1261 `InteractionCompleted` (HV), 1262 `RawCollected`, 1263 `Deposited`, 1264 `ReservationOpened`, 1265 `ReservationExpired`, 1266 `RecipeCompleted`, 1267 `OccupancyChanged`, 1268 `NeedRelieved` (Full), 1269 `WasteProduced` |
| 1280–1299 | UN7 | 1280 `HeldOut`, 1281 `HandedOver` (giver, receiver, lot, count, corr), 1282 `HoldOutLapsed`, 1283 `TakeResisted`, 1284 `Grabbed`, 1285 `ClaimCommunicated`, 1286 `ClaimFormed` (Full, analytics of beliefs) |
| 1300–1319 | UN8a | as 23.10 |
| 1320–1339 | UN6 (1320 UN6t) | 1320 `BrainAttached` (artifact version, hash, kind), 1321 `KnowledgeAcquired` (what, source own/observed/taught), 1322 `DemonstrationObserved` (HV), 1323 `TeachingGesture` |
| 1340–1359 | UB1 | settlements and roads |
| 1360–1379 | UN8b | groups |
| 1380–1399 | reserved | |

Every unified action event carries `Origin` (`ControlOrigin`) so warehouse `control_code` columns fill from the event. SAC §8 rules apply (own partial array, own mapper, codes never reused, changed meaning → new code, added field → `Version` bump).

### 23.13 Warehouse migrations (UN11 owns all; extends 18.2 and 22.7)

`0044_unification_core.sql` (world, motor, interactions, construction, needs day rows), `0045_unification_social.sql` (exchange, claims, conflict: separate tables for **events** (objective moves, strikes), **observations** (who perceived what) and **claims/promises** (beliefs and communications); no legitimate-owner or theft fact), `0046_unification_brain.sql` (artifact, training and evaluation metadata, knowledge spells, TR-01/TR-02 results), `0047` reserved for UB (settlements, roads, groups). `0048`–`0049` stay free. Per-package mappers in `DotNet/God.Runner/Warehouse/UNnMappings.cs` are written by the event-owning package as stubs and completed by UN11a; `fact_theft`, `fact_ownership_spell`, `fact_use_right`, `fact_inheritance` are labelled legacy-only.

### 23.14 Hash part and save

- Hash part `"unification"` appended after `"divine"` in `GodSim.AllHashPartNames`, present only when `GodState.Unification != null` (`industry` goes after it when I1a lands). The `population` part hashes wares 40–159 and ledger slots 8–15 only when non-zero.
- **Save plan (lead only):** `SaveFile.CurrentVersion` 6 → **7 once, in the merge commit of checkpoint UA-mech** (`GodState.Unification`, unified tuning, `UnifiedNpc`, brain state); v6 refused as today. UB → 8 only if UB state shapes change. Brain weights are not in the save (the artifact version and hash are). No package edits `SaveFile.cs`; UN0 only makes `FromJson` accept a null `Unification`.

### 23.15 Frozen surface → owner

| Surface | Owner |
|---|---|
| `UnifiedTuning`, `Unified.On`, `GodState.Unification` container, hash part, system-install gates, `PopulationSystem` → `UnifiedHooks.PersonTick` site, `Goods` unified branch, origin table, all ids of 23.4, `ControlInputKind.Primitive`, schema files (`ObsSchema`, `ActSchema`, `CatalogueDefs`, `EffectKind`, entity records 23.8), `ISpatialIndex`, `IPrimitivePolicy`, `EffectRegistry` dispatch, U-C1–U-C6, U-P1–U-P3 static | **UN0** |
| `SaveFile` version, re-pins, R-rulings, merge-time amendments | **lead** |
| `Motor`, `Percept`, `ActMask`, possession adapter, fixture harness | UN2 |
| world entities and their APIs | UN1 |
| catalogue source, generator, generated tables | UN3a |
| buildings, reservations, workstation/recipe effects | UN3b |
| sites and construction effects | UN4 |
| start generator, hall endowment | UN5 |
| brain runtime, `BrainState`, overlay, knowledge, `Brain.AddClaim` | UN6 |
| `DotNet/God.Train`, artifacts `Unified/Brain/Artifacts/*.g.cs`, thresholds file | UN6t |
| exchange records | UN7 |
| conflict effects | UN8a |
| host bootstrap, legacy call-site gates, compulsion goal driver | UN9 |
| views | UN10a/b |
| migrations 0044–0047, unified mappings completion, reports | UN11a/b |
| spatial index implementation, benchmarks `DotNet/God.Bench` | UNP |

### 23.16 Required shared tests

`Unification_Off_Reproduces_Base` (U-P1), `Unification_FoundationOnly_Neutral` (U-P2), `Unified_NoLegacyTitle_Static` (U-P3), `Wares_LayoutNeutral`, `Ledger_WideningNeutral`, `Unified_GoodsNoTitle` (produce, move, hand-over, merge, death spill: owner always `None`, no `OwnershipChanged`, no reward on transfer), `Unified_Invariants_UC1_UC6`, `Motor_OriginNeutral` (U-P4), `Unified_SaveLoadParity` (U-P6), `Schema_HashStable`, `Catalogue_*` (23.7), each package's `*_EventsRoundTrip`.
