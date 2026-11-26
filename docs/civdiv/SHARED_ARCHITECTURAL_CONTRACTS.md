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

## 21. Open decisions (each with the default taken)

| # | Decision | Default |
|---|---|---|
| 21.1 | Default of `Divinity.Enabled` for new games after Checkpoint B | stays 0 until the lead flips it at Checkpoint B with re-pinned hashes; god-only scenarios set 1 |
| 21.2 | Several possessions or avatar + possession at once | no (one embodied focus per god); tuning later |
| 21.3 | Whether NPCs recognise an avatar as their god | belief-based (D6), no automatic recognition |
| 21.4 | Divine-observation profile ownership (scenario, player, per god) | per scenario and per god (INFORMATION §12.3) |
| 21.5 | Worship-based Mythic | none in this run (brief §7) |
| 21.6 | Gait "run" | equals walk until a running speed exists |
