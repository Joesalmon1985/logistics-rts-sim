# Resurrection design (owner decisions D5, 26 Nov 2026)

A god may bring a dead person back to life. The person who returns is the same individual, with the same id, genealogy, memories, learned habits and relationship history. What happened after the death stays as it happened. To keep that possible years or generations later without simulating the dead, the dead are archived as compact, dormant records outside the tick loop. This document follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) and the owner decision register [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md); the owner's words are in [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §11.

Written against `unity/poc` at `8617eaa`.

---

## 1. Purpose and scope

**This document owns:**
- the resurrection variants (lesser, full, divine restoration) and rejuvenation as a separate intervention;
- identity restoration: same `NpcId`, genealogy, historical identity, personal memories, learned associations, relationship history;
- what is not reversed: estates, partnerships, employment, household membership, the recorded death;
- how the resurrected person re-enters the world (place, body, possessions, household);
- the archive of the dead: dormant compact records off the tick loop, what is retained at each tier, the configurable eligibility window, resurrection after generations;
- save, hash and warehouse implications.

**It links to, and does not repeat:**
- gods, commands, signs and divine learning: [DIVINE_PLAYER_DESIGN.md](DIVINE_PLAYER_DESIGN.md); costs: [MYTHIC_AND_MIRACLES_DESIGN.md](MYTHIC_AND_MIRACLES_DESIGN.md);
- the NPC record, life stages, death and disease: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- the pedigree: [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md);
- estates and their settlement: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md);
- households, partnerships and kin beliefs: [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md);
- the learning store: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md); social memory: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- saves and hashing: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md); the warehouse: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- the memorial and history view of the dead: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md); the resurrection effect: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md).

---

## 2. Owner decisions and principles

**Owner decisions:** D5.12 (resurrection permitted; restores the same persistent individual), D5.13 (does not reverse inheritance; partnerships, employment and households are not restored; the death stays recorded), D5.5 (no automatic regeneration of extinct populations: resurrection is a deliberate divine act on a named individual).

**Principles:**
1. **Same individual.** The resurrected person keeps its `NpcId` (ids are never reused, conventions §5), its pedigree record, genome, phenotype, personality, name, birth tick and life history. It is never a copy and never a new NPC (DIVINE_PLAYER I-DIV-7).
2. **History is additive.** The death stays in the record. A resurrection is a new life event after it. Nothing that other people did or received after the death is undone.
3. **Real state change.** Resurrection makes a living body at a node, with real health, drives and an empty carry, through the population module; it is not a visual.
4. **The dead cost nothing per tick.** Dormant records are never visited by the tick loop. Archiving is a daily, bounded pass.
5. **Bounded and deterministic.** The archive has caps and a fixed eviction order; every archive operation is in ascending id; its digest is in `StateHash`.

---

## 3. Data structures

### 3.1 What exists today
- `PopulationModule.Kill` (`Sim/Population/PopulationModule.cs`) marks the NPC dead, emits `NpcDied`, runs the M4 and M5 death hooks (`HouseholdHooks.OnDeath`: estate opened, partnerships ended, care ended, household left; `SocialSim.OnDeath`), spills the carry, clears hut rights, and sets **`npc.Mind = null`**: "the dead keep their record, not their mind". The dead `Npc` record stays in `PopulationState.Npcs` (sorted by id), and every system loop skips it with `if (!p.Alive)`.
- POPULATION_DESIGN "Death" plans to drop the dead after 4 generations.

Both change: the mind is archived instead of dropped, and dead records leave the hot list.

### 3.2 The archive (`Sim/Population/Archive`)

```csharp
public enum MindTier : byte { Full = 0, Compact = 1, None = 2 }

public sealed class ArchivedPerson {                 // ArchiveState.Dead, ascending NpcId; one per dead person
    public Npc Record;                               // the dead Npc record, moved out of PopulationState.Npcs (Mind == null)
    public MindTier Tier;
    public byte[] Mind;                              // the packed dormant mind (3.3), null at MindTier.None
    public long DiedTick; public DeathCause Cause; public int DeathNode;
    public int  LifeNumber;                          // 1 for a first life; +1 per resurrection
    public long ArchivedTick, CompactedTick;
}

public sealed class ArchiveState {                   // GodState.Archive; saved; covered by StateHash through Digest
    public List<ArchivedPerson> Dead;                // ascending NpcId
    public ulong Digest;                             // running hash, updated on insert, compact, drop and remove (3.4)
    public long  Bytes;                              // packed bytes held, for the budget and the benchmarks
}
```

### 3.3 What the dormant mind keeps, by tier

| Part | Living size (POPULATION budget) | `Full` (first `FullArchiveDays`) | `Compact` (after) | `None` |
|---|---|---|---|---|
| Identity core: id, sex, faction at death, parents, genome, phenotype, traits, learning rates, instincts, birth and death ticks, cause, name seed, life number | in `Npc` | kept | kept | kept (always) |
| Pedigree record | 16 B | kept (in `PopulationState.Pedigree`, as now) | kept | kept |
| Association entries (`AssociationStore.Entries`) | 4,096 B | all 256 | top `CompactAssoc` (64) by `|Ltw|`, ties by `(Key, Verb)` | — |
| Conjunctions | 1,280 B | all 64 | those referenced by kept entries | — |
| Traces, episodes, day moments | 4,416 B | dropped at death (closed with what they had; open episodes expire) | — | — |
| `Mind.Beliefs` | 1,536 B | all | dropped except beliefs about kin and home places (top 8) | — |
| Kin beliefs (`Mind.Kin`) | 512 B | all | all | — |
| Social memory (`SocialMind`): relations, obligations | about 3,136 B | all | top `CompactRelations` (16) by importance; obligations dropped | — |
| Culture store (M11) | 270 B | all | traits with `Strength ≥ 128` | — |
| Religion: divine relations, commandment knowledge, affiliation | about 830 B | all | all relations; adopted commandments only | — |
| **Packed size** | | **about 11 KiB** | **about 2.5 KiB** | **about 0.3 KiB** (the record) |

Defaults (`SimTuning.Archive`): `FullArchiveDays` 10 (one sim year), `CompactAssoc` 64, `CompactRelations` 16, `MindArchiveYears` 0 (keep a `Compact` mind for ever), `MaxArchivedMinds` 20,000 (beyond it the oldest-dead compact minds drop to `None` first, by `DiedTick` then id), `RetainMinds` on whenever `DivinePower != Disabled`, else off (the dead then archive at `None` directly and the run hashes as before, section 10). The packing format is a versioned binary layout owned by this document (`MindPack.Version`), with an equivalence test: pack then unpack gives identical stores.

**The dormant mind does not change.** Nothing decays while it is archived. A person resurrected after ten years remembers what it knew when it died, minus what compaction discarded. A configurable `DecayWhileDead` (default off) applies the learning and social decay of the elapsed days at restore, for experiments that want "memories fade with time dead".

### 3.4 Hashing the archive
`StateHash` includes `ArchiveState.Digest`, a 64-bit content digest: the sum (mod 2^64) over archived persons of `H(NpcId, Tier, LifeNumber, H(packed bytes))`. Each insert, compaction, drop or removal subtracts the person's old term and adds its new one, so the digest costs O(1) per operation, while re-hashing every dormant byte at every check would make the hash cost grow with history. Because the digest is defined by content, not by history, load recomputes it from the stored archive and checks it against the saved value.

---

## 4. Mechanics

### 4.1 Archiving the dead (daily, off the tick loop)

1. **At death** (`PopulationModule.Kill`), before `npc.Mind = null`: if `RetainMinds`, the mind is packed at `Full` into a pending slot on the record (one allocation per death, not per tick). Everything else of `Kill` is unchanged: estates open, partnerships end, the household is left, hut rights end, the carry spills.
2. **Archive pass** (once a day, the first tick of the day, ascending id): every dead record still in `PopulationState.Npcs` whose estate is settled (`EstateSettled` emitted) and whose death is at least `ArchiveAfterDays` (default 1) old moves into `ArchiveState.Dead` with its pending mind. Kin and pedigree lookups (`PopulationState.Find`) fall back to a binary search of the archive, so estate, kin and genealogy code keep working.
3. **Compaction** (same pass): `Full` minds older than `FullArchiveDays` are re-packed at `Compact`.
4. **Window and cap** (same pass): with `MindArchiveYears > 0`, minds older than that drop to `None`; above `MaxArchivedMinds` the oldest compact minds drop to `None`. Identity cores are never dropped (genealogy across generations needs them; POPULATION's "drop after 4 generations" is replaced by this rule).
5. Each step emits `PersonArchived`, `MindCompacted`, `MindDropped` and updates the digest.

**Cost.** One pass per day touches only that day's deaths and compactions: O(deaths per day). The tick loop no longer visits dead records at all, which also removes a growing cost from every `foreach Npcs` loop over long runs.

### 4.2 The resurrection command

`Resurrect { God, Npc, Variant, Node }` with `Variant ∈ { Lesser, Full, Restoration }`.

**Preconditions (Validate and Apply):**
- the person is dead and archived, or dead and not yet archived (it is in `Npcs` with a pending mind);
- its mind tier is `Full` or `Compact`; at `None` the command is rejected `NoMemories` unless `AllowMindlessResurrection` (default off) is set, when the person returns with identity and genealogy but an empty mind (flagged in the event);
- `Node` is land and, in `Standard` power, one of: the death node, a node within 3 of its grave or estate bin, a sacred site of the god (MYTHIC §6.10), or the god's avatar's node; in `Unlimited` sandbox any land node in the god's sight;
- the god has the Mythic and is off cooldown for this person (1 day).

**Cost** (placeholder, MYTHIC §4.5 style): `Lesser` 30, `Full` 60, `Restoration` 90 Mythic, × `(1000 + YearsDeadPermille × years dead) / 1000` with `YearsDeadPermille` 100 (10% more per sim year dead, capped at × 5).

**State change (one tick, in this order):**
1. **The record returns.** The archived `Npc` moves back into `PopulationState.Npcs` at its id's sorted position; `Alive = true`; `LifeNumber += 1`; `DiedTick`, `DeathCause` keep the last death (and the life record keeps every death, 4.4).
2. **The mind is restored.** A fresh `NpcMind` is built and filled by unpacking: association entries and conjunctions (with their `Use` and `LastDay` as at death), beliefs, kin beliefs, social relations, culture and religion. Traces, episodes and day moments start empty; the inbox and exposure ring start empty. Learning mode, policy name and keyed streams (`Rng.Fork("npc", id)` and children) are re-derived from the id, so draws stay keyed by entity and purpose.
3. **The body is set by variant:**

   | Variant | Health | Nutrition | Fatigue, fear | Injury and illness | Note |
   |---|---|---|---|---|---|
   | Lesser | 300 permille | 300 | fatigue 700, fear 600 | the lethal condition is lifted to just below its death threshold (injury to 650 if it died of wounds; illness to `Recovered` if it died of disease; nutrition as listed if it starved); other ailments stay | Fragile: may die again soon |
   | Full | 700 | 600 | fatigue 300, fear 400 | lethal condition removed; non-lethal ailments stay | Reasonable health |
   | Restoration | 1000 | 800 | 0, fear 200 | all injury 0, illness `Recovered` with immunity | Divine restoration |

   `Npc.Injury`, `Health` and `Nutrition` are micro units in code (`Sim/Population/PopulationState.cs`); the table is per mille.
4. **Biological age is preserved, not advanced while dead.** A new field `Npc.AgeShiftTicks` (default 0) is added, and every age read goes through one helper, `Ages.BioAgeMilli(s, npc) = cal.AgeMilli(npc.BornTick + AgeShiftTicks, s.Tick)`, replacing the direct `cal.AgeMilli(npc.BornTick, s.Tick)` calls (about ten sites: `LifeCycle`, `HouseholdHooks`, `HouseholdActs`, `PopulationKnowledge`, `Estates` seniority uses `BornTick` and keeps it). On resurrection `AgeShiftTicks += (now − DiedTick)`, so the person resumes the age it had at death. `BornTick` stays the historical birth tick (genealogy, the warehouse, seniority).
5. **Old age.** A person who died of `OldAge` would die again at once. Unless the command is combined with rejuvenation (4.3), its age is set back to `MaxAge − OldAgeGrace` (default 0.25 sim years) by `AgeShiftTicks`, and the event records it.
6. **Place and possessions.** The body appears at `Node` with an **empty carry**. Its former goods went to its estate and belong to their present owners (D5.13). A god may give goods separately (create resources into its carry, MYTHIC §6.8, a separate command and cost).
7. **Household, partnerships, employment.** It belongs to no household (`Household = 0`, as left at death) and has no partnership, contract, hut right or care arrangement. These are not restored (D5.13). The household module treats it like a homeless adult (or, for a child, an orphan displaying need), and re-entry happens by ordinary decisions (4.5).
8. **Obligations** in its restored social memory that fell due while it was dead are marked `Orphaned`; open ones keep their terms (others may or may not honour them: their own memories decide).
9. **Signs and events:** a loud divine sign at `Node` (DIVINE_PLAYER §4.8), `DivineInterventionIssued`, `MiracleApplied(Resurrect)`, `NpcResurrected`. The resurrected person records a `Target` exposure and its divine relation takes the "possessed by it" level of evidence at double value (COMMANDMENTS_AND_RELIGION §4.2: belief +400, trust +150, devotion +100).

### 4.3 Rejuvenation (separate, potentially expensive)

`Rejuvenate { God, Npc, Years }` on a living person (or combined with a resurrection command in the same tick): `AgeShiftTicks -= Years` in ticks, bounded so that the biological age stays at least `RejuvenateMinAge` (default the start of adulthood, 1.0) and the change per command is at most 1.0 sim year. Life stage follows from the new age at the next daily life-cycle check (an elder may become an adult again; fertility follows the age rule). Cost placeholder: 80 Mythic per sim year, cooldown 10 days per person. Event `NpcRejuvenated { npc, years, ageBefore, ageAfter }`. It does not change the genome, `MaxAge`, memories or history.

### 4.4 The death stays recorded

- Each `Npc` gains a bounded **life record** (`Lives[4]`: `{ StartTick, EndTick, Cause, ResurrectedBy }`), so a person who died twice has both deaths. The warehouse already holds `NpcDied` events; `NpcResurrected` opens the next life.
- `PopulationStats.Deaths` and `DeathsByCause` are not decremented. Mortality KPIs count deaths as events; population counts count the living; a new KPI counts resurrections (KPI_CATALOGUE, Q1).
- Others' memories of the death (grief, kin beliefs forgotten, `KinCues.Forget` for a partner) are not undone. The partner who was told of the death keeps that memory; seeing the resurrected person is a new perception (4.5).

### 4.5 Re-entering the world

What the resurrected person does next is its own policy's decision from its restored mind:
- **It remembers** its home, its kin, its friends and enemies, its habits and its god. It walks to where it believes its home is; the hut may belong to others now, or be gone (beliefs update on sight, the cost of stale beliefs).
- **Others recognise it** only through their own memories. A former partner or friend whose relation to it still exists (not yet evicted) recognises it; someone whose record faded meets "a stranger". A relation's familiarity has decayed by the days elapsed (SOCIAL_MEMORY §4.7). Witnesses of the resurrection's sign know something divine happened to that person.
- **Household:** a resurrected adult may be invited into a household, re-partner through ordinary courtship (its restored affection may make it want to), or found its own; a resurrected child appears as an orphan in need and may be taken in by any household through `Adopt` (M4), most likely by its former carers if they still recognise it. **Rejoining the former household is the household's and the person's decision**, never automatic (D5.13). A dedicated "ask to rejoin" act is Tier 3 (`AskJoinHousehold`, HOUSEHOLDS_AND_KINSHIP).
- **Estate and property:** it owns nothing. It may ask for its things back (a request gesture, M5/M7), and heirs may agree, refuse or bargain; a later justice institution may hear claims (C9). Nothing is returned by rule.
- **Citizenship** (`Npc.Faction`) is the faction at death. If that faction is gone, the person is a citizen of no living state until it migrates or is accepted (STATE citizenship rules), which is ordinary.
- **Employment:** none; it may take postings like anyone.

### 4.6 Resurrection after generations

Eligibility needs only the identity core and a mind tier other than `None`. Because the identity core is never dropped and `Compact` minds are kept for ever by default (capped by `MaxArchivedMinds`), a founder who died 100 sim years ago can return with its kin beliefs, its strongest habits and its closest relations' records, into a world where everyone it knew is dead or old. Its genealogy links (pedigree) connect it to descendants who never met it; they meet it as a stranger whom the god raised, and its own kin beliefs name people long dead. Nothing in the design needs the old world to still exist. The cost multiplier caps at × 5.

---

## 5. Interactions with other systems

| System | What flows |
|---|---|
| `Population` | `Kill` packs the mind before clearing it; the daily archive pass; `Find` falls back to the archive; the record returns on resurrection; `AgeShiftTicks` and `Ages.BioAgeMilli`; the life record |
| `Learning`, `Cognition`, `Social`, `Culture`, `Religion` | Pack and unpack of their stores (`MindPack`); each owner provides its pack/unpack pair and its compaction rule |
| `Households` | Resurrected person starts with no household, partnership or care; ordinary decisions afterwards; orphan display for a child |
| `Estate` | Archive waits for `EstateSettled`; estates are never reopened or reversed |
| `State` | Citizenship as at death |
| `Divinity` | Command, cost, sign, exposures and evidence |
| `Save`, `Telemetry` | Archive in saves and the digest in the hash; section 7 |

---

## 6. Player and view

- **Memorial and history view** (PLAYER_UI_DESIGN): the dead are listed with their life record, cause of death, family tree, and an eligibility badge (`Full`, `Compact`, `No memories`) with the cost of each variant. A resurrected person's history shows each life.
- **In the world:** the resurrection sign and the rising animation are played from `NpcResurrected` (VISUAL_WORLD_DESIGN, NPC_ANIMATION_DESIGN); the person then simply walks off by its own decisions.
- **Inspector:** shows "returned from death" with the life number, the variant's starting body, and that its old goods remain with their owners.

---

## 7. Telemetry

| Event | Fields |
|---|---|
| `PersonArchived` | npc, diedTick, cause, tier, bytes |
| `MindCompacted` | npc, fromTier, toTier, bytesBefore, bytesAfter, entriesKept, relationsKept |
| `MindDropped` | npc, reason (Window, Cap) |
| `NpcResurrected` | intervention, god, npc, variant, lifeNumber, deathTick, deathCause, yearsDead, node, mindTier, ageMilliAfter, oldAgeGraceApplied, mindless |
| `NpcRejuvenated` | intervention, god, npc, years, ageBefore, ageAfter, stageAfter |

**Warehouse** (Q1, [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)): `dim_person` keeps one row per `NpcId` (persistent identity) and gains `life_count`; a new `fact_life` (one row per life: start, end, cause, resurrected by) and `fact_resurrection`; `fact_person_day` simply has no rows for days spent dead. Every KPI that counts deaths, births or the living stays correct because resurrection is a separate event, not a negative death.

**Daily snapshot:** archive counts by tier, archive bytes, resurrections today.

---

## 8. Invariants

| Id | Invariant | Checked by |
|---|---|---|
| I-RS-1 | A resurrected NPC has the same `NpcId`, pedigree record, genome and `BornTick` as before its death; no new id is allocated | Unit test |
| I-RS-2 | Resurrection creates no lot, moves no lot and changes no ownership | Event check: no `ItemMoved`, `Produced` or `OwnershipChanged` with its intervention id; C1 holds |
| I-RS-3 | Estates, partnerships, contracts, household memberships and hut rights of others are unchanged by a resurrection | Snapshot comparison before and after |
| I-RS-4 | Every death remains in the life record and in the counters | Unit test |
| I-RS-5 | No archived record is visited by the tick loop | Profiler counter: archive reads only from `Find` fallbacks, the daily pass and resurrection |
| I-RS-6 | Pack then unpack is the identity on `Full`; `Compact` keeps exactly the documented subset | Round-trip test over 1,000 random minds |
| I-RS-7 | The archive digest matches a recomputation from the archive after load | Save/load test |

---

## 9. Tier and package mapping

| Capability | Package | Tier |
|---|---|---|
| Pack the mind at death (`MindPack` for learning, beliefs, kin, social; religion when D6 lands) | D7 | 2 |
| `ArchiveState`, the daily archive pass, `Find` fallback, digest in the hash, save/load | D7 (+ Q3 for save/load parity) | 2 |
| `Resurrect` with the three variants, `AgeShiftTicks` and `Ages.BioAgeMilli`, life record, old-age grace | D7 | 2 |
| Re-entry through ordinary decisions (no household, orphan display for a child) | D7 | 2 |
| Compaction tiers, window and cap | D7 | 2 |
| Rejuvenation | D7 | 3 |
| `AskJoinHousehold`, property claims through justice | C3/C9 | 3 |
| Warehouse `fact_life`, `fact_resurrection`, memorial view | Q1, U5 | 2 |

Resurrection is Tier 2 in the brief (§42). The one Tier 1 dependency is cheap and should land early (D1 or F1): **do not discard the mind at death when divinity is on** (step 1 of 4.1), because a mind discarded before D7 lands cannot be recovered later in the same save.

---

## 10. Migration from the existing code

- `PopulationModule.Kill`: pack before `npc.Mind = null` when `RetainMinds`; otherwise unchanged. With `RetainMinds` off (the default when divinity is disabled) the archive pass still moves dead records out of the hot list at tier `None`. **That move changes iteration over `Npcs` and so changes the hash once;** it is accepted as an F-level migration (recorded in DEV_LOG), with the equivalence check that behaviour (every living NPC's decision sequence) is identical with and without the move over the `band_6x20` runs.
- `PopulationState.Find` gains the archive fallback; `AliveCount` and `AliveOf` become cheaper.
- `cal.AgeMilli(npc.BornTick, ...)` call sites move to `Ages.BioAgeMilli` (identical while `AgeShiftTicks` is 0).
- POPULATION_DESIGN's "dropped after 4 generations" and its "archive list (no `Mind`)" are superseded by section 4.1 (D7 updates that line).
- `Npc` gains `AgeShiftTicks`, `LifeNumber` and `Lives[4]` (saved; written only when non-default).

---

## 11. Tests and acceptance hooks

| Id | Test | Pass | Scenario |
|---|---|---|---|
| RS1 | **Same identity and memories.** An NPC with a known store, relations and kin beliefs dies of wounds carrying 3 fish; its estate settles to its child; 2 days later it is resurrected (`Full`) at its death node | Same `NpcId`, pedigree and genome; its association entries, beliefs, kin beliefs and relations equal those at death (store hash equal); its child still owns the 3 fish; its carry is empty; `NpcResurrected` emitted; the death is in its life record and `DeathsByCause[Wound]` unchanged | 14 |
| RS2 | **Nothing reversed.** The same person had a partner and a job; the partner re-partnered after the death | Partnerships, the new partnership, contracts, household memberships and hut rights are unchanged by the resurrection (I-RS-3); the resurrected person has none | 14 |
| RS3 | **Variants differ in body.** Lesser, Full and Restoration on identical dead | Health, nutrition and injury as in the table exactly; a lesser-resurrected person in a scenario with no food dies again sooner than a fully restored one in ≥ 90% of 30 seeds | 14 |
| RS4 | **Age resumes, not advances.** A person dead for 3 sim years is resurrected | Its biological age equals its age at death (±1 tick); its `BornTick` unchanged; an `OldAge` death returns at `MaxAge − 0.25` with the grace flag | 14 |
| RS5 | **Re-entry by decision.** The resurrected adult and its former household | No household on return; joining or partnering happens only through ordinary decision events, in the runs where it happens | 14 |
| RS6 | **Generations later.** A founder dead for 50 sim years (archive at `Compact`) | Resurrection succeeds; the kept subset (top 64 associations, 16 relations, kin beliefs) is restored exactly; cost multiplier capped | 14 |
| RS7 | **The dead are off the loop.** *runner*: 100 sim years, 500 living, archive on | Tick time per living NPC is independent of the number of dead (within ±5% between year 10 and year 100); archive bytes within the cap | — (performance, brief §48) |
| RS8 | **Save/load and replay.** Save with dead in every tier and a pending mind; load; resurrect; replay | Identical hashes; digest verified (I-RS-7) | 14, 18 |
| RS9 | **No new people.** 1,000 random resurrections and kills | Population ids ⊆ births and immigrants; I-DIV-7 holds | 14 |

---

## 12. Open questions

Settled with configurable defaults: archive tiers and sizes, `FullArchiveDays` 10, compact minds kept for ever up to 20,000, no decay while dead, the old-age grace, re-entry with no household, citizenship as at death, costs. One consequential point is a cost, not a choice, and is stated for the owner's awareness: **keeping minds of the dead costs memory** (about 2.5 KiB per dead person after a year; about 50 MB for 20,000 dead), which is why the cap exists; raising it is a scenario setting.
