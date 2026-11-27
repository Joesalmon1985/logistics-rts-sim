# Mythic and miracles design (owner decisions D5, 26 Nov 2026)

Mythic is the one supernatural resource every god spends. Miracles are the only divine acts that change the physical world directly, and they do so through the same validated paths as everything else: the goods API with its conservation ledger, the body, death and estates. This document follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) and the owner decision register [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md); the owner's words are in [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §7 and §10. **Types, enum values, command and event names are frozen by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("SAC") §5, §7.2 and §8; this document owns the behaviour, the per-miracle rules and the placeholder numbers.**

Written against `unity/poc` at `8617eaa`.

---

## 1. Purpose and scope

**This document owns:**
- Mythic behaviour: balance, configurable maximum, fixed daily regeneration, action costs, continuous upkeep, cooldowns, the `Unlimited` sandbox and the `Disabled` mode; the later worship feedback seam;
- every miracle of brief §10: preconditions, the exact authoritative state change, ledger entries for created and destroyed goods, events, how NPCs perceive it, its visual cue and its tier;
- the default starting costs (configurable placeholders, deliberately not balanced).

**It links to, and does not repeat:**
- gods, commands, signs, perception and divine learning: [DIVINE_PLAYER_DESIGN.md](DIVINE_PLAYER_DESIGN.md);
- possession and avatar upkeep in context: [POSSESSION_AND_AVATAR_DESIGN.md](POSSESSION_AND_AVATAR_DESIGN.md);
- commandments and consecrated places' meaning to minds: [COMMANDMENTS_AND_RELIGION_DESIGN.md](COMMANDMENTS_AND_RELIGION_DESIGN.md);
- resurrection, rejuvenation and the archive: [RESURRECTION_DESIGN.md](RESURRECTION_DESIGN.md);
- holders, owners and the goods API: [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md), [SHARED_CONTRACTS.md §2](SHARED_CONTRACTS.md#2-inventories-and-goods-movement), SAC §7;
- health, injury, disease and death: [POPULATION_DESIGN.md](POPULATION_DESIGN.md); estates after a death: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md);
- the look of every effect: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md) (divine visual identity); NPC reactions: [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md); the Mythic meter: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md).

---

## 2. Owner decisions and principles

**Owner decisions:** D5.10 (possession and compulsion consume Mythic), D5.11 (physical miracles are validated deterministic commands with ledger entries), D5.26 (configurable divine power; human and AI gods under the same rules), D5.27 (unlimited sandbox and disabled mode), D5.33 (no predetermined outcomes).

**Principles:**
1. **A miracle is genuine.** `CastMiracle`'s `Apply` changes truth through the owning module's functions and then emits `MiracleCast` (one) and `MiracleEffectApplied` (one per affected thing, with before and after values). A visual effect without a state change is forbidden; the view animates only these events (SAC §8).
2. **No bypass.** Lots only through `Goods` (`Sim/Population/Goods.cs`); bodies only through `Bodies.Heal` and `Bodies.Damage` (SAC §4.1); death only through `PopulationModule.Kill`, so estates, households and social memory react as to any death; sources and huts only through their module's functions. No remote transfer exists: a miracle may create goods into a holder or destroy them, never move an existing lot (SAC §7.1).
3. **Conservation stays exact.** C1 (`Σ Produced − Σ Consumed = Σ holdings` per ware, `GoodsLedger` in `Sim/Population/PopulationState.cs`) holds with the two explicit miracle sources the F1 slots provide, `ProduceReason.Miracle` (= 4, "miracle created") and `ConsumeReason.Miracle` (= 5, "miracle destroyed"), and the new C7 reconciles them with the divine ledger (section 5).
4. **Mythic is not a good.** It has its own integer ledger in the god's record, never enters the goods ledger, cannot be traded, stolen, inherited or seen by any NPC.
5. **Same rules for every god.** No charging code reads the seat (SAC §5).
6. **Do not balance yet.** Numbers here only make the mechanics testable (brief §7).

---

## 3. Data structures

Frozen by SAC: `GodRecord.Mythic`, `MythicMax` (`long` milli-Mythic; 1 Mythic = 1000), `CooldownUntil` (per `InterventionKind`); `Divinity.Mode` (`Standard`, `Unlimited`, `Disabled`); `CastMiracle { God, Kind, Target, Magnitude }`; `MiracleKind` (`HealInjury 1, HealDisease 2, Bless 3, Protect 4, SmitePerson 5, SmiteBuilding 6, SmiteSource 7, CreateResource 8, DestroyResource 9, RevealKnowledge 10, RevealTechnology 11, Consecrate 12, Rejuvenate 13, Area 64 + kind`; weather and terrain reserved); `ConsecratedPlace`; `LedgerId`; the enum values `ProduceReason.Miracle = 4`, `ConsumeReason.Miracle = 5`, `OwnershipBasis.Miracle = 15`, `BinKind.Shrine = 6`, `ObjClass.Shrine = 80`.

**D3's own records** (in `DivinityState.Miracles`):

```csharp
public struct MiracleRecord { public int Intervention, God; public MiracleKind Kind; public DivineTarget Target; public int Magnitude; public long Tick; }
public struct Blessing      { public int Npc; public MiracleKind Kind; public long Until; public int God, Intervention; }   // Bless or Protect; at most one of each per person
public sealed class MiracleLedger {                // per god per ware: created and destroyed totals (C7)
    public long[] Created, Destroyed;              // [god * Wares.PersonalCount + ware]
}
public struct DivineSign { public int Intervention, God; public byte Kind; public int Node; public byte Radius, Loudness; public long Until; }  // ring of 64 (DIVINE_PLAYER §4.8)
```

Tuning (SAC §5): `Divinity.Mythic.{Start, Max, RegenPerDay}`, `Divinity.Costs.<CommandName>`, `Divinity.Upkeep.<Kind>PerTick`, `Divinity.Cooldown.<Kind>Ticks`; per-miracle parameters in `Divinity.Miracles`.

---

## 4. Mythic mechanics

### 4.1 Order within a tick
Divine commands apply at the tick head (miracles change truth there). `DivinitySystem` runs at order 230 (SAC §11.4): at day start it credits regeneration (`MythicCredited`), writes `MythicBalanceDay` and expires instructions; every tick it charges upkeep, sweeps control spells (`Control.Sweep`) and resolves possessed and avatar movement. Gods are processed in ascending id; spells in ascending `InterventionId`.

### 4.2 Regeneration (fixed, Tier 1)
- At each day start: `Mythic = min(MythicMax, Mythic + RegenPerDay)`. Defaults: `Start` 50 Mythic, `Max` 100 Mythic, `RegenPerDay` 20 Mythic. What the cap discards is not credited.
- No worship, population or territory term in this run (brief §7, SAC 21.5). A god with no believers regenerates like any other (D5.5).
- **Later seam:** a worship source (Tier 3, D6 with D1, by SAC amendment) may add a daily credit from perceived worship, prayers and offerings at consecrated places, read from religion state, never from the warehouse.

### 4.3 Spending, upkeep and cooldowns
- **Spending** goes only through `Mythic.TrySpend(s, ev, god, kind, costMilli, intervention)`: `Validate` calls `Mythic.CanSpend` (no mutation); `Apply` calls `TrySpend` first and applies nothing if it fails (`mythic_short`, `cooldown`, `divinity_disabled`). If the miracle's state change then finds nothing to change (the target died this tick), `Apply` refuses before spending, so nothing is charged without effect.
- **Upkeep** (per tick, `Mythic.ChargeUpkeep`, aggregated as `MythicUpkeepCharged` per god per day): possession, avatar and, when configured, compulsion. When a god cannot pay, the dependent control ends that same tick with `MythicExhausted`, in ascending `InterventionId` (SAC §5).
- **Cooldowns** are per god per `InterventionKind` (`CooldownUntil[kind]`). Defaults are small or zero so that kinds do not block each other in play testing (section 4.5).
- **`Unlimited`** (sandbox, D5.27): nominal costs are recorded on every spend (`MythicSpent` with the nominal amount and `waived`), nothing is charged, the balance is held at max. Reports still show what the play would have cost.
- **`Disabled`:** gods and records exist; every divine command is refused `divinity_disabled`; state is untouched (pin P3). `Divinity.Enabled = 0` removes divinity state altogether (pin P1).
- **`SetMythicMode { Mode }`** is a logged sandbox command (`MythicConfigChanged`).

### 4.4 Scaled costs
Instructions scale with recipients (`Base + PerExtra × (n − 1)`); area miracles with the number of affected things at apply; create and destroy with units; heal and smite with magnitude; resurrection with variant and years dead (RESURRECTION §4.2).

### 4.5 Default costs (placeholders: `Divinity.Costs`, `Divinity.Upkeep`, `Divinity.Cooldown`)
In whole Mythic (× 1000 milli). Chosen only so a 100-Mythic god can do a few things a day.

| Action | Cost | Upkeep per tick | Cooldown (per kind) |
|---|---|---|---|
| `IssueSuggestion` | 1 + 0.5 per extra recipient | — | 0 |
| `IssueCommand` | 4 + 2 per extra recipient | — | 0 |
| `IssueCompulsion` | 10 | 0.01 (12 a day) | 0 |
| `BeginPossession` | 15 | 0.05 (60 a day) | 0 |
| `ManifestAvatar` | 20 | 0.02 (24 a day) | 600 ticks after a destruction |
| `RevealCommandment` / `Revise` / `Revoke` | 25 / 10 / 5 | — | 0 |
| `CastMiracle` HealInjury | 1 per 100 permille healed (min 3) | — | 0 |
| HealDisease | 12 | — | 0 |
| Bless, Protect (5 days) | 15 each | — | 0 |
| SmitePerson | 2 per 100 permille of injury (min 4) | — | 0 |
| SmiteBuilding | 30 | — | 0 |
| SmiteSource | 10 | — | 0 |
| CreateResource | 1 per food or raw unit, 5 per tool | — | 0 |
| DestroyResource | 0.5 per unit | — | 0 |
| RevealKnowledge | 8 | — | 0 |
| Consecrate | 40 | — | 0 |
| Area (any kind) | the kind's cost per affected thing | — | 0 |
| Resurrect, Rejuvenate | RESURRECTION §4.2, §4.3 | — | 0 |

---

## 5. Ledgers and invariants

- **Mythic (M1, SAC §5):** `0 ≤ Mythic ≤ MythicMax` always; per god `initial + Σ credited − Σ charged = balance` exactly, reconstructable from events (`MythicTrace`, F1).
- **Goods C1** holds unchanged: the miracle slots are ordinary ledger reasons.
- **C7 (D3):** `Ledger.ProducedBy(Miracle, w) = Σ ResourceCreated(w)` and `ConsumedBy(Miracle, w) = Σ ResourceDestroyed(w)`, at every day boundary and after save/load. Every miracle `Produce` or `Consume` is paired with exactly one `ResourceCreated` or `ResourceDestroyed` (`LedgerId`, ware, count, lot, holder, `InterventionId`) and booked in `DivinityState.Miracles.Ledger` per god per ware.
- **C8:** no lot changes holder without an `ItemMoved` by an actor within reach; divine control included. A miracle never moves a lot except where it destroys a hut and its bin must be emptied (6.6, a `Goods.Move` to the ground at the bin's own node, actor 0, zero distance).

---

## 6. Miracles

Common rules:
- **Preconditions** are checked from true state by the handler (a god's power, not an NPC's perception), within the god's observation profile (DIVINE_PLAYER §4.10). Refusals: `bad_target`, `target_dead`, `not_a_person`, `act_unavailable` (reserved kinds).
- **Events** for every miracle: `DivineActionAccepted`, `MiracleCast { intervention, god, kind, target, magnitude }`, then `MiracleEffectApplied { intervention, thing kind, id, field, before, after }` per affected thing, then the ordinary state events (`InjuryTaken`, `IllnessEnded`, `Produced`, `Consumed`, `ItemMoved`, `NpcDied`, `BuildingDestroyed`...), each carrying the `InterventionId`.
- **Perception:** each leaves a `DivineSign` at its node (DIVINE_PLAYER §4.8): perceived as `SituationKind.DivineSign` (80) within sight (loud ones within hearing), a smite of another as `SmiteWitnessed` (83); the target perceives the effect on its own body or goods. Witnesses update beliefs (COMMANDMENTS_AND_RELIGION §4.2).
- **Visual cue:** one entry per kind in VISUAL_WORLD_DESIGN's divine visual identity, played from `MiracleCast` and `MiracleEffectApplied` only.

Code units: `Npc.Health` and `Npc.Injury` are micro (0..1,000,000); magnitudes below are per mille of that scale.

| # | Miracle | Preconditions | Exact state change | Ledger | How NPCs perceive it | Visual cue | Tier |
|---|---|---|---|---|---|---|---|
| 6.1 | **Heal injury** (`HealInjury`) | a living person (or the god's own avatar) with injury; magnitude 1–1000, capped at the injury | `Bodies.Heal(..., Injury, magnitude)`: injury falls by the magnitude; pain extra falls by the same share; below the wound-infection threshold (300) the infection risk ends | none | the patient feels pain relief (a reward to its live traces through the ordinary drive channel) and keeps `SelfFeature.Healed` (49) for `MemoryDays`; witnesses see the sign and the visible injury mask clear | healing light on the body | 1 |
| 6.2 | **Heal disease** (`HealDisease`) | a living person whose M4 illness is `Infected` (`Sim/Population/Households/Disease.cs`) | `Bodies.Heal(..., Illness)` through the M4 hook: illness to `Recovered`, normal immunity (`ImmuneUntilDay = today + 15`), `Severity = 0`, `IllDaysLeft = 0`; Health +`HealDiseaseHealth` (default 100), capped at 1000; it stops being a contact | none | `IllnessEnded` (cause miracle); the visible "sick" display ends; `SelfFeature.Healed` | a cleansing glow; the sick posture ends | 1 |
| 6.3 | **Bless** (`Bless`) | a living person without an active blessing | a `Blessing { Kind = Bless, Until = now + BlessDays }` (default 5 days), read by the M4 health rolls through their hook: daily Health recovery +15 when Nutrition > 300; work-hazard injury chance and infection chance × `BlessRiskPermille` (default 500). Nothing else (no luck stat, no hidden reward) | none | a visible aura while active; `SelfFeature.Blessed` (50) | a soft aura | 1 (these two effects) / 2 (more) |
| 6.4 | **Protect** (`Protect`) | a living person without an active protection | a `Blessing { Kind = Protect, Until = now + ProtectDays }` (default 5 days), read by `Bodies.Damage`: every injury from work hazards, contests, combat or another god's smite is multiplied by `ProtectPermille` (default 300). It does not stop starvation, disease or old age | none | an attacker sees its blow land weakly (a visible outcome it can learn from) | a shield shimmer on each blow | 1 / 2 |
| 6.5 | **Smite a person** (`SmitePerson`) | a living person; magnitude 1–`SmiteMax` (default 800, so one smite rarely kills a healthy adult outright) | `Bodies.Damage(..., magnitude, DamageCause.Divine, actor 0, intervention)`: injury rises (protection applies first), fear drive +`SmiteFear` (default 400). If the injury rule then kills (POPULATION "Mortality") or Health reaches 0, death is `Kill(DeathCause.Violence)` (no new death cause: `DeathsByCause` is hashed, SAC 20.4) and `MiracleEffectApplied` records the divine attribution; the estate opens and everything follows as for any death | none (a death's estate follows MATERIAL_INHERITANCE) | `InjuryTaken`; pain and fear punish the victim's own live traces (it is never told why); `SelfFeature.Smitten` (48); witnesses get `SmiteWitnessed` (83), fear, and the visible outcome `Struck` for vicarious learning | a strike in the god's colour; heard at hearing range | 1 |
| 6.6 | **Smite a building** (`SmiteBuilding`) | a hut (`Hut`, `Sim/Population/PopulationState.cs`) or a territory building | **Huts:** a hut-removal function D3 adds to `PopulationModule`: first its bins are emptied by `Goods.Move` to the ground at the bin's node (`MoveReason.Drop`; ownership per the ground rule of the branch: ownerless before C1, kept after); with `SmiteBurnsContents` (default off) they are instead `Goods.Consume(..., ConsumeReason.Miracle)`; then sleeping rights and lodgers end by the same code as any lost hut, residents lose their bed (the ordinary opportunity-loss signal, −400 vp); people inside are unhurt unless `SmiteBuildingInjures` (default off). **Territory buildings:** the existing destruction path (`BuildingDestroyed`). Built materials are not refunded | `ResourceDestroyed` per burnt unit; nothing for scattered goods | the ruin is seen; residents' beliefs about their home update when they see it | a strike and the collapse | 1 (huts) |
| 6.7 | **Smite a tree or resource source** (`SmiteSource`) | a `ResourceSource` with stock | through a D3 function in `PopulationModule`'s source API: `Stock` falls by the magnitude (default all); with `SmiteBlights` (default on) regrowth is suspended for `BlightDays` (default 5). Sources are not lots, so the change is `MiracleEffectApplied` with stock before and after | none (wild stock is not in the goods ledger) | the source is seen empty; NPCs with a stale belief keep walking to it until they look (the cost of a stale belief) | a withering strike | 1 |
| 6.8 | **Create basic resources** (`CreateResource`) | a target node in the god's view, a person's carry, or a bin; a basic personal ware (food, logs, the axe in Tier 1); count 1–10; room | `Goods.Produce(holder, ware, count, ..., ProduceReason.Miracle, actor 0, source = intervention)` into: the ground at the node (ownerless); a target person's carry (owner `Npc(target)`, basis `Miracle`, no goods reward on receipt: SHARED_CONTRACTS §2.3 pays only foraging; its value is realised when used); or a bin (owner the bin's). Never where nobody can reach. Food gets its ware's normal spoil day | `ResourceCreated` per call; `ProducedBy(Miracle, w)` | goods appear in sight with the sign; a receiver's later use pays its decisions | resources manifesting in light | 1 |
| 6.9 | **Destroy resources** (`DestroyResource`) | a lot on the ground, in a bin or in a living person's carry; count up to the lot's; an open estate's bin may not be targeted (estates stay intact) | `Goods.Consume(holder, lot, count, ConsumeReason.Miracle, actor 0, refId = intervention)` (actor 0 is the sim: `Goods.InReach` passes it). The owner loses the goods; the loss is booked against the owner like spoilage, so the owner's learning feels it | `ResourceDestroyed`; `ConsumedBy(Miracle, w)` | goods vanish from sight; an owner who sees or later checks updates its beliefs | goods crumbling to dust | 1 |
| 6.10 | **Reveal knowledge** (`RevealKnowledge`) | a living person (or `Location` scope) | `BeliefStore.Upsert` into the target's own mind (layer 6) with the true node of a place or thing, source divine; never into an institution's records (D5.28) | none | as a vision; the belief may later be stale like any other | a brief light over the head | 2 |
| 6.11 | **Reveal technology** (`RevealTechnology`) | needs a skills or technique store (C7, C10) | reserved: refused `act_unavailable` until then; then the technique is granted through the same path as innovation (CULTURAL_TRANSMISSION §4.6) and is teachable | — | as an insight | — | 3 |
| 6.12 | **Consecrate a location** (`Consecrate`) | a land node; no other god's consecrated place within `SacredSpacing` (default 6 nodes) | a `ConsecratedPlace { Id = intervention, God, Node, Radius, SinceTick }`; perceived through the sense seam as `ObjClass.Shrine` (80) and `SituationKind.ConsecratedPlace` (82); an offerings bin of `BinKind.Shrine` may be created by D6 (owned by the receiving organisation, or offerings consumed with `ConsumeReason.Offering`). A place, never an owner | none | a lasting visible mark; worship gathers there (COMMANDMENTS_AND_RELIGION §4.6) | a lasting glow and marker | 2 |
| 6.13 | **Resurrect** (`Resurrect`, its own command) | RESURRECTION §4.2 | RESURRECTION §4.2 | none | a loud sign | rising light | 2 |
| 6.14 | **Rejuvenate** (`Rejuvenate`) | RESURRECTION §4.3 | `BioAgeOffset` | none | — | — | 3 |
| 6.15 | **Area miracles** (`Area 64 + kind`) | a node and `Target.Radius` ≤ 8; a Tier 1 kind | the single-target effect over every eligible thing within the radius in ascending id, draws only from `miracle-effect(intervention, step)`; cost per affected thing | per thing as the kind | one large sign | a wide effect | 3 |
| 6.16 | **Weather**, **terrain** | need ecology and terrain support | reserved (refused `act_unavailable`) | — | — | — | later |

---

## 7. Interactions with other systems

| System | What flows |
|---|---|
| `Population` (`Goods`, `GoodsLedger`) | `ProduceReason.Miracle`, `ConsumeReason.Miracle`, `OwnershipBasis.Miracle`; hut removal and source stock functions (D3) |
| `Population` (`Bodies`) | `Bodies.Heal`, `Bodies.Damage(DamageCause.Divine)`; blessings read by the health rolls and the damage path; deaths through `Kill(DeathCause.Violence)` |
| `Households` (disease) | `IllnessEnded`; blessing lowers infection chance |
| `Estate` | A smiting death opens an estate as any other; destroy never touches an open estate bin |
| `Military`, contests | Protection reduces their damage |
| `Cognition` | `BeliefStore.Upsert` for revelations; `ObjClass.Shrine`, `SituationKind` 80, 82, 83 through the sense seam |
| `Divinity` | Costs and upkeep for every divine command (DIVINE_PLAYER, POSSESSION_AND_AVATAR, COMMANDMENTS_AND_RELIGION, RESURRECTION) |
| Warehouse | Section 9 |

---

## 8. Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.

| Capability | Package | Tier |
|---|---|---|
| Mythic: `TrySpend`, daily regeneration, cap, cooldowns per kind, upkeep, `Standard`/`Unlimited`/`Disabled`, `SetMythicMode`, Mythic events and invariant M1 | D1 | 1 |
| Heal injury, heal disease, smite a person, create resources, destroy resources; miracle ledger and C7 | D3 | 1 |
| Smite a hut, smite a source | D3 | 1 |
| Bless and Protect as timed modifiers with the effects above | D3 | 1 (minimal) / 2 |
| Reveal knowledge, consecrate | D3 (+ D6 for meaning) | 2 |
| Resurrect | D7 | 2 |
| Rejuvenate, area miracles, reveal technology | D7, D3, C7/C10 | 3 |
| Weather, terrain | — | later |
| Worship-fed Mythic | D6 + D1 (amendment) | 3 |

---

## 9. Telemetry

Codes and names are SAC §15.1.

| Event | Fields |
|---|---|
| `MythicConfigChanged` (803) | mode, by |
| `MythicCredited` (804) | god, amount, capped away |
| `MythicSpent` (805) | god, intervention, kind, amount (nominal), waived, balance after |
| `MythicUpkeepCharged` (806, HV) | daily per god and spell kind: ticks, amount |
| `MythicBalanceDay` (809) | god, balance, max, credited, spent by kind, waived |
| `MiracleCast` (825) | intervention, god, kind, target, magnitude, cost |
| `MiracleEffectApplied` (826) | intervention, thing kind and id, field, before, after; divine attribution for a resulting death |
| `ResourceCreated` (827), `ResourceDestroyed` (828) | ledger id, intervention, god, ware, count, lot, holder |
| `LocationConsecrated` (829) | intervention, god, node, radius |
| `IllnessEnded`, `InjuryTaken` (existing, extended) | `Intervention` |

The warehouse reconciles created and destroyed units with the goods ledger by reason, so divine goods are never invisible in KPI reconciliation (scenario 17).

---

## 10. Invariants

| Id | Invariant | Checked by |
|---|---|---|
| M1 | Mythic ledger identity per god; `0 ≤ Mythic ≤ MythicMax` | `MythicTrace` (F1) daily; 10,000 random commands |
| C1, C7, C8 | Goods conservation with the miracle reasons; miracle ledger equals the goods ledger's miracle slots; no holder change without a reach-checked `ItemMoved` | Existing C1 check extended; D3 checks |
| I-MY-3 | Every `MiracleCast` has at least one `MiracleEffectApplied` with `before != after` | Unit test per kind |
| I-MY-4 | No miracle writes a lot, owner or holder except through `Goods`; no miracle moves a lot between people | Static test on `Sim/Divinity` (F0 test extended) |
| I-MY-5 | A divine death goes through `PopulationModule.Kill` (`NpcDied`, estate opened) | Unit test |
| I-MY-6 | Nothing is charged when nothing is applied | Unit test |

---

## 11. Tests and acceptance hooks

| Id | Test | Pass | Scenario |
|---|---|---|---|
| MM1 | **Regeneration is exact.** 10 days at 20 Mythic a day from 0 with max 1000 | Credited exactly 200 Mythic; with max 100 the balance never exceeds it and the excess is reported as capped | 9, 18 |
| MM2 | **Costs and refusals.** A god with 3 Mythic issues a compulsion costing 10 | Refused `mythic_short`, no state change; with 12 Mythic it applies and the balance falls by exactly 10 | 9 |
| MM3 | **Upkeep ends possession the same tick.** Upkeep 50 milli per tick, balance 1,000 milli | Ends `MythicExhausted` at tick 20; the person's own policy decides at its next think | 10 |
| MM4 | **Unlimited sandbox.** The same commands in `Unlimited` | Balance held at max; `MythicSpent` carries the nominal costs with `waived` | — (D5.27) |
| MM5 | **Disabled mode.** A scripted god commanding daily in `Disabled` | All refused `divinity_disabled`; pin P3 | 18 |
| MM6 | **Heal is genuine.** Injury 600, heal 400 | Injury 200 in state; pain falls; the live trace receives a positive reward; `MiracleEffectApplied` before/after match | 12 |
| MM7 | **Smite is genuine and conserves goods.** Smite 800 on a person carrying 3 fish, which kills it | `NpcDied(Violence)` with divine attribution in `MiracleEffectApplied`; estate opened with the 3 fish; C1 holds; witnesses' fear rises | 12, 14 |
| MM8 | **Create and destroy reconcile.** Create 5 berries on the ground; a person takes 3; destroy 2 | `ProducedBy(Miracle, Berries)` 5 = Σ `ResourceCreated`; `ConsumedBy(Miracle, Berries)` 2 = Σ `ResourceDestroyed`; C1 and C7 hold at every tick and after save/load; the taker owns 3 by first arrival | 12, 17 |
| MM9 | **Hut smite.** A hut with a store of 6 roots | Hut gone; 6 roots on the ground at its node; residents lose beds; with `SmiteBurnsContents` 6 `ResourceDestroyed` instead | 12 |
| MM10 | **Protect.** A protected person struck in a contest for 300 | Injury rises by 90 (× 300 per mille) | 12 |
| MM11 | **No effect without state.** Static test of `Assets/God/Game`: every divine effect is triggered by `MiracleCast`/`MiracleEffectApplied` or a state event | Passes | 12 |
| MM12 | **Replay with miracles.** *runner*: DP13 plus one of each Tier 1 miracle | Identical hashes; Mythic and goods ledgers reconcile with events in the warehouse | 18 |

---

## 12. Open questions

None that block the run. Settled with configurable defaults: every cost, upkeep and cooldown (section 4.5, placeholders), smite cap 800, hut contents scattered rather than burnt, blessing and protection limited to concrete body and hazard terms, fixed regeneration until a worship source is added by amendment. Balancing is deferred on purpose (brief §7).
