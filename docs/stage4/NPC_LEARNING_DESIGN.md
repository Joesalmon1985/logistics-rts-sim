# NPC learning design (stage 4)

Every NPC learns from its own life. Its innate instincts give it a starting point. What it actually does comes more and more from what has worked and failed for it. This document owns instincts, the associative learning core, delayed reward, generalisation, forgetting and the five learning mechanisms. It follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md). The research it builds on is in [research/CREATURES_NOTES.md](research/CREATURES_NOTES.md).

## 1. Purpose and scope

**This document owns:**
- the five learning mechanisms and the boundaries between them (section 2.2);
- instincts as innate priors (`InstinctTable`);
- the context encoding that learning uses (`FeatureId`, conjunctions);
- the `AssociationStore`: values per (feature or conjunction, verb), with fast and slow weights;
- reward and punishment signals, eligibility traces and delayed credit;
- sleep-time consolidation, generalisation, forgetting and capacity limits;
- social learning: vicarious updates from observed outcomes, and imitation by copying a model's entries (`ExportTop`, `Import`);
- how learned state becomes input for trained policies (the interface only).

**It links to, and does not repeat:**
- drives, body, traits and skills: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- perception, attention, beliefs, situation features, the decider and gestures: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- the genes behind instincts and learning rates: [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md);
- trust, obligations and reputation about particular people: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- cultural traits (`CultureStore`), teaching of traits, conformity, memes and traditions: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md). Its section 4.9 fixes the boundary: copied association entries are *socially learned* (origin `Imitated`) and stay in this store, owned by Learning; Culture owns named traits and their transmission and never writes association weights;
- parents and children in a household: [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md);
- network encodings, `IntNet` and training: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md);
- performance targets and level of detail: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md).

## 2. Principles

### 2.1 Rules
- **Instincts start behaviour; experience shapes it.** No NPC decision is a fixed utility formula or a behaviour tree. Hand-written estimates exist only as innate priors, and learned values add to them and can outweigh them.
- **Learning lives in the NPC's mind, not in the policy.** `INpcPolicy` and `IMilitaryPolicy` read the same `AssociationStore`. Swapping a policy never erases what an NPC has learned (conventions, section 7).
- **Reward comes from the NPC's own wellbeing.** Drives falling, goods gained, harm avoided and opportunities opened are rewards. Nothing rewards a reported KPI, a state target or a theoretical equilibrium. The state can change what gets rewarded only by changing real consequences.
- **The NPC learns only from what it perceived.** Learning reads the NPC's own observation, drives and memories, never `GodState`. A reward it never noticed cannot teach it. A loss it never connected to its action cannot teach it either.
- **Lifetime learning is never inherited directly.** A newborn's store is empty. Children acquire habits only by living, watching and being taught (mechanisms 2–4).
- **Exact determinism.** Integer maths only, a fixed update order, forked random streams, all state saved, `StateHash` covers it. Recording never changes learning.
- **Bounded.** Fixed caps per NPC (about 9.9 KB), fixed work per think tick, no allocation in the hot path.
- **Never present an outcome as designed in.** The mechanics *make it possible* for NPCs to learn to cheat, cooperate, desert or stay loyal. Section 7 (telemetry) and section 10 (tests) say how to *measure* whether they did.

### 2.2 The five mechanisms (distinct in code and data)

| # | Mechanism | Where it lives | What it writes | Milestone |
|---|---|---|---|---|
| 1 | **Innate instincts and drives** | `Genome` → `InstinctTable`; drives in `Population` | Nothing at run time. A prior term in the score, and the reward rules | M3 (M4 genetic) |
| 2 | **Individual associative learning** | `Learning.AssociationStore` | The NPC's own entries, from its own outcomes | **M3 core**, M5 social contexts |
| 3 | **Social learning: observation and imitation** | `Learning.Vicarious`, `Learning.Imitation` | The observer's own entries: at a lower gain from outcomes it *saw* others get, or copied from a model's entries | M7 observation, M11 imitation |
| 4 | **Cultural transmission** | `Culture` ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) | Named traits in `CultureStore`, which add a `Culture(c)` decision term. Never association weights | M11 |
| 5 | **Offline network training** | Python, outside the sim; `Policy.IntNet` inside | Network weights, fixed for a run. Never the store | M15 inputs; training later |

Each entry records its origin (`Own`, `Vicarious`, `Imitated`) in its flags, so the share of behaviour coming from each mechanism can be measured.

### 2.3 Relation to *Creatures*

| *Creatures* idea | What we borrow | What we change, and why |
|---|---|---|
| Drives as chemical levels; reward when a present drive falls, punishment when one rises | The same rule on our integer drives (0–1000). A fall below the comfort band earns nothing, so eating when full is not rewarded [P] | We add explicit outcome signals (goods, injury, obligations, opportunities). Our society needs rewards that are not bodily, such as a promise kept |
| Decision = verb applied to the attended object class | Candidates are a verb plus a target, valued per (feature, verb) | Targets can be individual NPCs and remembered places, not only classes. The game-theory examples need individual recognition |
| Short-term weight (STW) and long-term weight (LTW) per link | `Stw` and `Ltw` per entry, as `short` | Updated by **prediction error** (the outcome minus what was expected), not by plain reinforcement. This keeps weights bounded, makes extinction work, and lets a reformed cheat be re-learned |
| Susceptibility: a link that helped fire stays creditable for a while | Eligibility traces per decision, decaying by an integer shift per think tick | Long delays (wages, repayment, promises) are tracked as explicit **episodes** that close on a later event. Susceptibility alone fades in seconds |
| Concept cells: AND-detectors over 1–4 inputs, taken from a pool and freed at zero strength | A pool of 64 conjunctions per NPC, pairs and triples, with strength, recruitment and freeing | Recruited only at sleep, from the day's most rewarding or punishing moments, in a fixed order. At most one drive per conjunction (borrowed rule 8) |
| Partial matches generalise (ABCD recalls ABD or D) | Single features and conjunctions are summed, so a new situation recalls what its parts predicted | Explicit category features (food, tool, stranger, own faction) carry transfer between similar objects and people |
| Instincts applied during sleep as replayed rewards [A] | Instincts exist and are genetic | Instincts are a **separate prior term**, never written into the store. This keeps mechanisms 1 and 2 separable, so "how much is learned" can be measured and learning can be switched off |
| Attention is not learned (a known weakness [D]) | - | Goal-driven attention and search are in Cognition. Learned values also bias attention (section 4.9) |
| Learning in batches | A sleep-time consolidation pass at a fixed point in the NPC's day | Light learning also happens while awake, so a world that changes mid-day is noticed the same day |
| Ten creatures per 486 PC | - | 500 NPCs headless at many times real time. Sparse tables replace dense neurone lobes |

## 3. Data structures (`Assets/God/Sim/Learning`)

All fields are integers. Sizes are fixed when the NPC is created.

### 3.1 Feature ids
A context is a small sorted set of `FeatureId`s. Cognition decides which features to compute ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)); this module owns the encoding.

```csharp
// FeatureId = (Kind << 24) | Value. 0 is "none". Sorted ascending in every set.
public enum FeatureKind : byte {
    DriveBand = 1,   // value = drive * 4 + band (band 1 mild, 2 high, 3 pain); band 0 emits nothing
    DriveActive = 2, // value = drive; emitted when band >= 2 (coarse, for transfer across bands)
    Self = 3,        // carrying food, carrying goods, injured, has bed, has job, owes someone, is owed
    ObjClass = 4,    // object or ware class of the target: BerryBush, FishSpot, LogPile, Spear...
    ObjCategory = 5, // Food, Raw, Tool, Weapon, Shelter, Workplace, Store (from the Economy item table)
    ObjState = 6,    // stock high / low / empty, owned by me / us / other / state
    NpcRel = 7,      // relationship class of the target NPC: Stranger, Acquaintance, Friend, Kin,
                     //   Partner, Cheat, Creditor, Debtor, Employer, Employee (from Social)
    NpcGroup = 8,    // OwnFaction, OtherFaction, EnemyFaction, StateStaff, Soldier, Elder, Child
    NpcRef = 9,      // one individual: value = NpcId & 0xFFFFFF (ids are never reused)
    MsgAct = 10,     // act of a message received this think: Want, Offer, Promise, Warn, ... (Comms)
    Place = 11,      // location type: Home, Forest, Coast, Field, Workplace, HQ, Road, Frontier, Battle
    PlaceRef = 12,   // one remembered node (forage at, go to): value = node index
    Role = 13,       // Worker, Soldier, Envoy, Child, Elder
    Posting = 14,    // job posted (pay band), contract posted, purchase offer posted (as believed)
    Situation = 15,  // Cognition's hand-written situation features ("outnumbered", "known cheat near")
    Time = 16,       // Day, Dusk, Night
    Conj = 32,       // value = (slot << 8) | generation
}
```

**Per think, each candidate gets one context:** the NPC's **base** features (drives, self, place, role, messages, time, situations: at most 12) plus the candidate's **target** features (class, category, state, relationship, group, ref: at most 6), plus the conjunctions that match (at most 8). So a context has at most **24 keys** (`MaxKeys`).

**Example:** a hungry NPC beside a berry bush, candidate "eat from this bush": `{DriveBand(Hunger,2), DriveActive(Hunger), Place(Forest), Time(Day), Role(Worker), ObjClass(BerryBush), ObjCategory(Food), ObjState(StockHigh), Conj(Hunger-high & Food)}`.

### 3.2 Verbs
`Verb` is a `byte` (at most 64). The first set, by domain:

| Domain | Verbs |
|---|---|
| Survival | `Eat`, `Sleep`, `Rest`, `Flee`, `Hide`, `SeekShelter`, `Approach`, `Avoid` |
| Gathering and production | `Gather`, `ForageAt`, `Haul`, `Build`, `Craft`, `WorkShift` |
| Equipment and goods | `PickUp`, `Drop`, `Store`, `TakeFromStore`, `Equip` |
| Barter and gifts | `Offer`, `AcceptOffer`, `RefuseOffer`, `HandOver`, `Withhold`, `Give` |
| Cooperation and exploitation | `Help`, `AskHelp`, `Lend`, `Repay`, `Default`, `Steal`, `Share`, `Hoard` |
| Relationships | `Greet`, `Accompany`, `Fight`, `Forgive`, `Retaliate`, `Court` |
| Communication | `Gesture(act)` as one verb per act: `SayWant`, `SayOffer`, `SayPromise`, `SayWarn`, `SayNeedFood`, ... and `Ignore` |
| Employment | `TakeJob`, `QuitJob`, `Hire`, `Fire` |
| Military service | `Enlist`, `HoldPost`, `Charge`, `Retreat`, `Desert`, `Surrender` |
| Faction membership | `AskJoin`, `Migrate`, `Defect`, `Stay` |

The verb list is data (`VerbTable`), so new verbs are added without changing the store. Each row also names one of 16 **verb groups** (the 10 domains above, with Survival, Barter and Cooperation split by sign of the act, such as `Give` vs `Withhold`), which `MindView.AssocSummary` uses (3.6).

### 3.3 The association store

```csharp
public struct AssocEntry {          // 16 bytes with padding
    public uint   Key;              // FeatureId (single or Conj)
    public byte   Verb;
    public byte   Flags;            // Origin (Own/Vicarious/Imitated), Locked (benchmark), Touched-today
    public short  Stw;              // fast weight, value points (vp), clamped to +-4000
    public short  Ltw;              // slow weight, vp, clamped to +-4000
    public ushort Use;              // usefulness for eviction: grows when used and credited, decays nightly
    public ushort LastDay;          // last day it was credited (SimCalendar day, mod 65536)
}

public struct Conjunction {         // 20 bytes with padding
    public uint   A, B, C;          // FeatureIds, A < B < C; C = 0 for a pair. At most one DriveBand/DriveActive
    public ushort Strength;         // 0 = free slot
    public ushort LastDay;
    public byte   Generation;       // bumped when the slot is reused, so stale Conj keys never match
}

public struct Trace {               // 112 bytes
    public int    Tick;             // decision tick
    public byte   Verb, KeyCount;
    public ushort E;                // eligibility 0..1024
    public short  QPred;            // learned value predicted at decision time (vp)
    public int    Return;           // reward credited so far, weighted by E (vp)
    public ushort Flags;            // Ongoing (intent still running), HasEpisode, Vicarious
    public fixed uint Keys[24];
}

public struct Episode {             // 120 bytes: a trace kept open for a long-delayed outcome
    public Trace  T;
    public int    EpisodeKey;       // ContractId, promise id, loan id, enlistment id...
    public ushort OpenedDay, ExpiresDay;
}

public struct DayMoment {           // 52 bytes: a strongly rewarded or punished moment, for recruitment
    public byte Verb, KeyCount; public short Delta; public fixed uint Keys[12]; // base + target keys only
}

public sealed class AssociationStore {
    public AssocEntry[]  Entries = new AssocEntry[256];  // sorted by (Key, Verb); Count in use
    public Conjunction[] Conj    = new Conjunction[64];
    public Trace[]       Ring    = new Trace[16];        // oldest first
    public Episode[]     Open    = new Episode[8];
    public DayMoment[]   Day     = new DayMoment[32];    // cleared at each sleep
    public int Count, RingCount, OpenCount, DayCount;
    public int RewardAcc;           // reward since the last think tick (vp)
    public LearningMode Mode;       // On, Frozen, Off
    public int Updates, Evictions, Recruits, Frees;   // lifetime counters (telemetry)
}

public enum LearningMode : byte { On, Frozen, Off }
```

**Bytes per NPC (fixed):**

| Part | Size |
|---|---|
| 256 entries × 16 | 4,096 |
| 64 conjunctions × 20 | 1,280 |
| 16 traces × 112 | 1,792 |
| 8 episodes × 120 | 960 |
| 32 day moments × 52 | 1,664 |
| Header and counters | 64 |
| **Total** | **9,856 bytes (≤ 10 KiB)** |

500 NPCs use about 4.9 MB. The caps are `SimTuning.Learning` fields, so experiments can change them; the 10 KiB test uses the defaults.

### 3.4 Instincts

```csharp
public struct Instinct {            // read from the genome at birth; see BIOLOGICAL_INHERITANCE_DESIGN.md
    public uint  A, B;              // one or two FeatureIds that must both be present (B = 0: one)
    public byte  Verb;
    public short Prior;             // vp, -2000..2000
}
public sealed class InstinctTable { public Instinct[] Rules; } // at most 32 rules, fixed order
```

The default genome encodes about 20 instincts, for example:
- `DriveActive(Hunger)` + `ObjCategory(Food)` → `Eat` +600;
- `DriveActive(Fatigue)` + `Self(HasBed)` → `Sleep` +500;
- `DriveActive(Fear)` + `NpcGroup(EnemyFaction)` → `Flee` +500;
- `DriveActive(Pain)` → `Rest` +400;
- `NpcRel(Kin)` + `Situation(KinInNeed)` → `Help` +200;
- `NpcRel(Stranger)` → `Approach` −150 (wariness).

**The physical estimates of the earlier draft's utility scoring become instincts:** "food relieves hunger" is innate knowledge with a strength, not a rule. Walking time and visible risk stay in the decider as costs, because the NPC perceives them.

### 3.5 Fixed-point helpers
- **Value points (vp):** 1,000 vp is roughly relieving one band (250 permille) of hunger.
- **`Fx.Shr(x, k)`** shifts towards zero, so positive and negative values decay at the same rate. C#'s `>>` on a negative `int` rounds towards minus infinity, which would bias weights downwards over thousands of updates.
- **`Fx.MulDiv(a, b, c)`** uses a `long` intermediate.
- `Fx.Clamp16` keeps weights within ±4000.

### 3.6 `MindView`: the read-only mind interface
`MindView` is the one read-only view of an NPC's own mind that every individual-level policy receives (`INpcPolicy`, `IMilitaryPolicy`; conventions, section 7). This document owns the type, its layout and its version. Population, Cognition and Social fill their parts; the sizes are fixed here so that scripted and trained policies, the inspector and the encoders in [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md#32-mindview-what-the-network-needs-from-the-npcs-own-mind) all read the same thing.

```csharp
public readonly struct MindView {            // built by Cognition per think; no allocation; holds no GodState
    public const ushort LayoutVersion = 1;   // goes into every model file header
    // Learning (this document), for the current base context set by Cognition before Decide:
    public void CandidateTerms(in Candidate c, Span<short> t);  // CAND_TERMS = 16 (below)
    public void TopAssociations(Span<AssocSlot> s);             // A = 8, by |Ltw| desc, then (Key, Verb) asc
    public void AssocSummary(Span<sbyte> v);                    // LEARN_DIM = 16: per verb group, the summed
                                                                //   Stw of matching entries, scaled to -127..127
    public void OriginShares(Span<short> v);                    // 3: Own, Vicarious, Imitated (permille)
    public LearningMode Mode { get; }
    // Filled by their owners, sizes fixed here:
    public void Drives(Span<short> d);                          // DriveCount, 0..1000 (POPULATION_DESIGN.md)
    public void BeliefSummary(Span<short> v);                   // BELIEF_DIM = 12 (COMMUNICATION_AND_COGNITION_DESIGN.md)
    public void RelationshipSummary(int npcId, Span<sbyte> v);  // REL_DIM = 8 (SOCIAL_MEMORY_DESIGN.md)
    public void Memory(Span<sbyte> v);                          // MEM_DIM = 8: reward sums over 1, 5 and 20 think
                                                                //   ticks, injuries today, cheats seen this week,
                                                                //   open episodes, frustration count, day share asleep
    public bool TryGetRelation(int npcId, out RelationRead r);  // the scripted Social(c) term reads this
    public bool TryGetBelief(BeliefKind k, int subject, out Belief b);
}

public struct AssocSlot { public byte Verb; public byte FeatureKind; public sbyte Stw; public sbyte Ltw; public byte OriginAge; }
// Stw, Ltw scaled by /32 to -125..125; OriginAge = origin * 64 + min(63, days since LastDay). 5 values per slot.
```

**`CandidateTerms` (16 values per candidate):** `Instinct`, `Learned` (`Q`), `Social`, `Culture`, `Personality`, `Cost`, the count of the candidate's keys that have an entry (familiarity), the origin of its strongest entry, then the top 4 keys' `Stw` and `Ltw` (8 values). The scripted `AssociativeNpcPolicy` sums the first six plus `Explore`; a network reads all 16 (4.15).

**Rules:** every call has a fixed, small cost; every order is a pure function of the store (ties by lowest `(Key, Verb)`); keys appear as feature kinds and verbs, never raw item ids or true positions; changing any size or meaning bumps `LayoutVersion`. A view never exposes truth flags, other NPCs' stores or `GodState` (the reflection test covers `MindView`).

`MindView` stores nothing of its own. Per-NPC memory is the sum of the stores it reads; the combined per-NPC budget table is in [POPULATION_DESIGN.md](POPULATION_DESIGN.md).

## 4. Mechanics

### 4.1 Order within a tick
NPCs update in ascending id. Within one NPC:
1. **Every tick:** reward signals that happened this tick are added to `RewardAcc` (4.4).
2. **On its think tick** (`(tick + id) % ThinkEvery == 0`):
   1. **Credit:** apply `RewardAcc` to the live traces, oldest first, then set it to 0 (4.5).
   2. **Close traces:** close the traces whose `E` fell below 32, oldest first, and apply their updates (4.6).
   3. **Context:** Cognition builds the base features. Learning finds the "armed" conjunctions, whose base parts all match (4.3).
   4. **Score:** for each candidate in build order: target features, matching conjunctions, the learned value `Q`, then the full score (4.2).
   5. **Choose:** the winner takes all; a tie goes to the lowest index.
   6. **Open a trace** for the chosen candidate with `E = 1024`, `QPred = Q`, `Ongoing` set. If the ring is full, the oldest trace closes first.
   7. **Decay:** every non-ongoing trace does `E -= E >> TraceShift`.
3. **At the NPC's sleep start:** consolidation (4.8), atomically within that tick.

**Random streams:**
- Exploration uses the NPC's own policy stream (`Rng.Fork("npc", id)`, passed to `Decide`).
- Consolidation and import noise use `Rng.Fork("npc", id).Fork("learn")`, with the `Fork(label, id)` overload that M0 adds (conventions, sections 3 and 4).
- **Exactly one exploration draw is made per candidate, always**, even when the amplitude is 0. Turning a term off must not shift the stream.

### 4.2 The decision score

```text
score(c) = Instinct(c) + Learned(c) + Social(c) + Culture(c) + Personality(c) - Cost(c) + Explore(c)
```

| Term | Source | Notes |
|---|---|---|
| `Instinct(c)` | Sum of `Prior` over matching instinct rules | Innate. 0 for a naive test NPC (`InstinctTable.None`) |
| `Learned(c)` | `Q(c) = Σ Stw(key, verb)` over the context's keys that have an entry, then `× LearnGain >> 10` | `LearnGain` 1024 by default. 0 when the mode is `Off` |
| `Social(c)` | Social memory: trust in the target, obligations, the weight of the target's welfare | Owned by [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| `Culture(c)` | Held cultural traits (norms, techniques, taboos) as score terms; 0 before M11 | Owned by [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| `Personality(c)` | Trait biases per verb (aggression → `Fight`, generosity → `Give`) | Traits are in [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| `Cost(c)` | Perceived walking time, visible risk × fear, goods given up | Perceived, not learned |
| `Explore(c)` | `rng.NextInt(Amp + 1) + Novelty(c)` | See below |

**Exploration:**
- `Amp = clamp(ExploreBase × Curiosity / 1000 + Boredom / 4 − Fear / 4, 0, 600)`.
- `ExploreBase` is 150. `Curiosity` is a genetic learning parameter, 500–1500 permille ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)); boredom and fear are drives (0..1000).
- `Novelty(c) = NoveltyBonus × Curiosity / 1000` when none of the candidate's keys has an entry for its verb. `NoveltyBonus` is 200.
- So curious or bored NPCs try untried things, and frightened ones stick to what they know.

**Why this is not hard-coded utility:**
- The instinct and personality terms are only a baseline.
- `Learned(c)` comes from the NPC's own history and is bounded only by ±4000 per entry. It can override any instinct (instincts are at most ±2000 each).
- Section 10 tests (L7, L16) check that learning changes the chosen action in every domain.

### 4.3 Conjunction matching
- **Once per think:** for each used slot in slot order, check its base-feature parts against the sorted base set (binary search). The slots whose base parts all match are **armed** (usually fewer than 16).
- **Per candidate:** for each armed slot, check its remaining target-feature parts against the candidate's target features. Each full match adds `Conj(slot, gen)` to the context, up to 8, in slot order.
- **Cost:** about 64 × 3 searches plus 16 candidates × 16 armed slots × 2 tests ≈ 1,300 comparisons.

### 4.4 Reward and punishment signals
`RewardAcc` collects signed vp from these channels. All gains are genome-modulated with defaults in `SimTuning.Learning` ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)).

| Channel | Rule (per tick or per event) | Default gain |
|---|---|---|
| **Drive relief** (borrowed) | If drive `d` fell from `p` to `n` and `p > Comfort[d]`: `+W[d] × (p − max(n, Comfort[d])) / 250` | `W`: hunger 1000, pain 1200, fear 900, fatigue 700, cold 700; later loneliness 400, boredom 300, anger 400, status 500 |
| **Drive rise** (borrowed) | If `d` rose from `p` to `n` and `n > Comfort[d]`: `−W[d] × (n − max(p, Comfort[d])) / 250` | Same `W` |
| Goods | Gained or lost items: `± BeliefValue(ware, own need) × count` | Value from the NPC's own price beliefs. **M3 (M3b, 26 Nov 2026): realised value.** Until price beliefs exist (M5), food and tools are worth what they realise for their holder: nothing is paid when they are taken; each meal from a food lot pays its hunger relief back to the decisions that gained the lot, each log gathered with a tool pays the tool's share of the work (log worth × (speed − 1000) / speed), and spoilage is a loss of the food's fixed worth. All three travel through lot episodes (4.7). Logs keep the fixed worth at once. `SimTuning.Learning.Goods` selects `Realised` (default), `LotEpisodes` (fixed worth at once, spoilage booked back) or `Immediate` (the first M3 rule) |
| Injury | Each wound taken (pain covers the rest) | −300 |
| Obligations | A promise to me kept / broken; I kept / broke one (guilt scales with honesty) | +400 / −600; +100 / −(200 + 3 × Honesty) |
| Opportunities | Bed right gained / lost; job gained / lost; accepted into / expelled from a household or faction; a new food place discovered (× curiosity) | +400 / −400; +300 / −300; +500 / −600; +100 |
| Status (M8+) | Thanked, a contest won / lost, visibly richer than neighbours | Through the status drive |
| Frustration | The sim rejected or aborted the intent (blocked, item gone, refused) | −50 |

**Wellbeing, not attitudes (owner decision Q7, 27 Nov):**
- Faction loyalty is **not rewarded directly** and is never a reward channel. Loyalty is rebuilt daily from the same experiences ([STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)), so adding it would count each experience twice, and it would be an artificial reward for obeying a faction.
- Its causes (wages paid, protection, security, living standards, relationships, treatment by the state, expulsion) are the channels above. So an NPC can learn that staying loyal, migrating, defecting or becoming independent pays under different circumstances, and the state must earn loyalty through what it does.
- An optional, configurable `Belonging` drive ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#drives)) may add social motivation through the ordinary drive-relief channel. It is relieved by contact with people the NPC knows, never by obeying or staying in a faction, and it is off by default.

### 4.5 Delayed credit: eligibility traces
- **Spreading reward:** on each think tick, every live trace gets `Return += Fx.MulDiv(RewardAcc, E, 1024)`.
- **Ongoing intents:** a trace stays at `E = 1024` while its intent is still running (a long walk to forage, a work shift), and starts to decay when the intent ends or is replaced.
- **Decay:** `TraceShift` is 2 (×0.75 per think) by default, and 3 for patient NPCs (Patience ≥ 40).
  - With shift 2, a reward 30 ticks (3 think ticks) after the action still gets 432/1024 of the credit.
  - A trace lives about 12 think ticks (120 ticks) before `E < 32` closes it.
- **Credit is shared.** Several recent traces share one reward. Prediction error sorts out which action really causes it: an action that is always followed by reward keeps gaining, while bystander actions average out to zero (test L2).

### 4.6 The update rule (when a trace closes)

```text
δ    = clamp(Return − QPred, −4000, 4000)            // prediction error, vp
n    = number of keys in the trace
for each key k in Keys (ascending), for verb v:
    entry = find (k, v), or insert (4.10)
    Δfast = Fx.MulDiv(Fx.Shr(δ, AlphaShift), LearnRate, 1000 * n)
                                                     // AlphaShift 2 adults, 1 children, 3 elders; LearnRate 500..1500 (genome)
    entry.Stw = Clamp16(entry.Stw + Δfast)
    entry.Ltw = Clamp16(entry.Ltw + Fx.Shr(Δfast, 3))   // the slow weight sees an eighth of it while awake
    entry.Use = min(65535, entry.Use + 1 + |Δfast| / 16)
    entry.LastDay = today; mark Touched
if |δ| >= MomentThreshold (300): record a DayMoment (base and target keys, δ), replacing the smallest |δ| if full
```

- **Learning by prediction error** (a Rescorla–Wagner or TD(λ) update spread over the active keys) means:
  - a habit that keeps paying off stops growing once it is predicted;
  - a habit that stops paying off is extinguished;
  - a surprise moves the weights most.
- **`Frozen` and `Off` modes skip the update entirely.** `Off` also gives `Learned = 0` in the score.

### 4.7 Long-delayed outcomes: episodes
Some outcomes arrive days later: a wage at the end of a shift, repayment of a loan, a promise falling due, pay for enlisting, the reception at a new faction.
- **Opening:** an intent whose verb is marked `Deferred` in the `VerbTable` (`Lend`, `Give`, `TakeJob`, `Enlist`, `AskJoin`, `Migrate`, `AcceptOffer` of a promise) moves its trace into `Open` when the trace would close. It carries the `EpisodeKey` that the sim attaches to the obligation.
- **Closing:** the matching domain event (`WagePaid`, `PromiseKept`, `PromiseBroken`, `LoanRepaid`, `CitizenshipChanged`, ...) adds its reward to `Return`, discounted by `Fx.Shr(r, daysElapsed / 2)`. The episode then closes through the normal update (4.6).
- **Expiry:** an episode with no event by `ExpiresDay` (default 10 days) closes with what it has.
- **When `Open` is full,** the oldest episode of the incoming verb closes first, else the oldest (M3b: many small forage lots must not push out the one `Build` episode waiting for its hut).
- **Lot episodes (M3b, 26 Nov 2026).** `ForageAt`, `PickUp` and `Equip` are deferred too. When the decision puts food into the carry, or takes up a tool, its newest trace waits for that carried lot (`EpisodeKey = 1 << 30 | lotId`; merged food shares the carried lot's id). Each meal from the lot and each log gathered with the tool reaches the waiting decisions as partial outcomes (`Learner.Accrue`: split between them, applied at once as news, the episode stays open); spoilage closes them with the loss, split and undiscounted (it corrects goods already counted); eating the lot up, or the tool leaving the carry (worn out or set down to sleep), closes them with nothing more.
- **An outcome reaching a live trace** (the hut finished while its builder is still building) is learned at once as news under TD (M3b fix: it used to go into a return that TD never reads).
- **Social memory still records the obligation** for the trust term. Learning records the *value of the act* in that context ("lending to strangers loses goods").

### 4.8 Sleep consolidation (once per NPC per day, at sleep start)
In fixed order:
1. **Close everything:** all non-ongoing traces close (4.6).
2. **Consolidate**, for each entry in key order:
   - `Ltw += Fx.Shr(Stw − Ltw, 2)` (sleep moves the long-term view towards the day's lesson);
   - then `Stw += Fx.Shr(Ltw − Stw, 1)` (the fast weight relaxes halfway back).
3. **Decay untouched entries:** an entry not touched today does `Ltw -= Fx.MulDiv(Fx.MulDiv(Fx.Shr(Ltw, 5), ForgetRate, 1000), DecayScalePermille, 1000)`, and the same for `Stw` (`ForgetRate` 500..1500 from the genome; `SimTuning.Learning.DecayScalePermille` default 1000). At 1000 and 1000 that is about 3% a day, a half-life of about 22 days (2.2 sim years at the default calendar). Decay is per sim day and is **not** scaled by `LifeScale`, which affects biological life-cycle durations only (owner decision Q1, [conventions §4](DESIGN_CONVENTIONS.md#4-time-model)). `DecayScalePermille`, `Use` decay (step 4) and conjunction decay (step 5) are independently configurable for experiments, separately from social-memory decay ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#47-forgetting)); 0 switches forgetting off.
4. **Usefulness:** every entry does `Use -= Use >> 3`, then the `Touched` flag is cleared.
5. **Conjunction strength:**
   - an active slot that took part in a closed trace today gains `+64 + |δ|/32` (capped at 4000);
   - every slot loses `Strength >> 3`, minimum 1 per day;
   - a slot at 0 is freed: its `Generation` is bumped and its entries are deleted.
6. **Recruitment** (4.11) from `Day`; then `Day` is cleared.
7. **Eviction:** if `Count > Cap − 16`, evict down to `Cap − 16`, so the next day has headroom.

**Cost:** about 256 + 64 + 32 × 66 ≈ 2,500 steps per NPC per day, which is negligible.

### 4.9 Learned values and attention
- **Salience:** Cognition's salience score gains `+ max over verbs of |Ltw(ObjCategory, verb)| / 8` for each perceived item. Things that have mattered to this NPC catch its eye.
- **Not a decision:** this only changes what is attended. It is one line of [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#43-attention).

### 4.10 Generalisation
Transfer comes from shared keys:
- **Categories:**
  - an NPC that learned to eat berries when hungry has positive `ObjCategory(Food) → Eat` and `Conj(Hunger & Food) → Eat`;
  - the first fish it meets shares those keys and is valued at once;
  - only `ObjClass(FishSpot)` is new, and it starts at 0.
- **People by class:**
  - being cheated by one stranger lowers `NpcRel(Stranger) → AcceptOffer`, so all strangers are met with more caution;
  - the cheat's own `NpcRef` entry falls further;
  - a friend is unaffected unless the cheat was a friend, in which case `NpcRel(Friend)` falls a little and the individual a lot.
- **Factions:** `NpcGroup(OwnFaction)` and `NpcGroup(StateStaff)` let experiences with tax collectors, guards and employers generalise to "the state" as this NPC has met it.
- **Drive bands:** `DriveActive(d)` is shared across bands 2 and 3, so a lesson learned when very hungry still applies when merely hungry.
- **Specialisation:** conjunctions capture what single features can't: "food is good *when hungry*", "strangers' offers are fine *in my own village* but not *on the road*".
- **Partial matches:** a new situation that shares a pair with an old triple still recalls the pair's value (the *Creatures* result [P]).

**Inserting entries** (only when a trace closes, never while scoring):
- A key with no entry for the verb is inserted with `Stw = Ltw = 0` and `Use = 0`.
- If the store is full, the entry with the lowest `EvictScore` is removed:
  - `EvictScore = Use + |Ltw| / 4 + 256 × max(0, 30 − daysSinceLastDay) / 30`;
  - a tie goes to the lowest `(Key, Verb)`;
  - entries flagged `Locked` are never evicted.
- If the update's `|Δfast| < 4`, no entry is inserted, so noise doesn't fill the store.

### 4.11 Conjunction recruitment (at sleep)
1. **Candidates:** for each `DayMoment` in order, list the pairs `(DriveBand or DriveActive) × (one non-drive key)` and the triples `drive × target key × base key`. There are at most 12 + 30 per moment, and at most one drive feature each.
2. **Scoring:** a candidate's score is the sum of `|δ|` over the moments it appears in. Candidates that already exist are skipped.
3. **Choosing:** take the best `RecruitPerNight` (default 4) by score, then by lowest `(A, B, C)`.
4. **Placing:** each goes into a free slot, or replaces the weakest slot if that slot's strength is below 128. It starts at strength 256.
5. **Its entries** start at 0 and are learned from the next day on, exactly like single features.

### 4.12 Forgetting and adaptation
| Mechanism | Effect | Example |
|---|---|---|
| Prediction error | Fast extinction when outcomes change | A depleted berry bush: `ForageAt(PlaceRef)` keeps failing (walking, frustration, hunger rising), `Stw` turns negative within a few trips, and the NPC goes elsewhere the same day |
| Fast weight relaxing at sleep | A shock fades, a residue stays | Cheated once: very wary the next day, milder after a week |
| Nightly decay of untouched entries | Old knowledge fades if never used | A source unvisited for 40 days keeps about a quarter of its value |
| Capacity eviction | The least useful, oldest, weakest go first | A busy life pushes out childhood trivia |
| Conjunction freeing | Unused distinctions dissolve | "Hungry on the coast" goes once the NPC moves inland |
| Recency in `EvictScore` | Fresh lessons are protected for 30 days | A new job's lessons survive their first slow week |

**A reformed cheat:** honest dealings bring positive `δ` on that NPC's `NpcRef` entries, so the fast weight recovers first. The slow weight follows over several nights. How fast this happens depends on the observer's forgiveness, which is applied in Social, not here.

### 4.13 Social learning by observation (M7)
- **When it happens:** an attended NPC `j` finishes a visible action `v` with a visible outcome:
  - ate;
  - received goods;
  - was struck or chased;
  - was thanked;
  - was cheated (gave goods and got nothing back);
  - was paid at the notice board.
- **The update:** the observer builds a context from what it can see: the target's object features, `j`'s relationship class and group, and the place. It cannot see `j`'s drives, so it uses no drive features.
- **The reward** is `VisibleOutcomeValue[outcome]`, scaled by `VicariousShift` (default 2, a quarter of the gain). Kin and partners use shift 1. The prediction is the observer's own `Q`.
- **The update rule is 4.6,** with `Flags.Vicarious`. New entries are marked `Origin = Vicarious`.
- **Limits:**
  - only for verbs the observer could do itself;
  - at most 2 vicarious updates per think, the most salient first;
  - outcomes it didn't attend to teach nothing.
- **Children learn more from what they see:** `VicariousShift` minus 1 below year 1 (LifeScale applied). This is how children pick up habits from the household before any formal teaching ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)).

### 4.14 Imitation: copying a model's entries (M11)
Imitation of habits is **social learning** (mechanism 3) and stays inside this store, as [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) section 4.9 rule 2 requires. It is not culture: copied entries are not traits, the culture census never sees them, and `Teach` cannot pass them on.
- **When:** at sleep, every `ImitateEveryDays` (default 3 days), an NPC with conformity above 0 may pick one model. The model is the known adult with the highest `Esteem` in social memory (a belief, possibly wrong), if it beats the learner's own standing by a margin. For a child below year 1, a parent is the model, through the household ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)).
- **Only what was seen:** the learner copies only entries whose verbs it has **seen the model do** recently (a 16-slot `SeenVerbs` list for each of at most 4 watched models, 80 bytes per NPC, filled by the vicarious path and outside the 9,856-byte store total). A habit must show to spread.

The two deterministic operations:
- **`ExportTop(k, filter)`** returns the `k` entries with the highest `|Ltw|`, optionally filtered by verb or key kind. `NpcRef` and `PlaceRef` keys are excluded unless the filter asks for them: you can teach "strangers on the road cheat", but not your private grudge.
- **`Import(entries, gainShift, ref Rng)`** adds `Fx.Shr(srcLtw, gainShift) + noise(±32)` to the learner's `Stw` and `Ltw`, inserts if needed (normal eviction), and marks `Origin = Imitated`. The learner's own later experience then confirms or overturns it, exactly like its own entries.

A parent's example for a child uses gain shift 1. An imitator of a successful neighbour uses shift 2, or 3 when its conformity is low. Cultural traits the learner holds act separately, through `Culture(c)`.

### 4.15 Offline training and network inputs (M15)
- **What `IntNetNpcPolicy` reads:** the `MindView` of 3.6: `CandidateTerms` for each candidate (the score terms, familiarity and the top 4 keys' weights), `TopAssociations`, `AssocSummary`, drives, beliefs, relationships and `Memory`. They are fixed-size integer inputs, alongside the observation ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- **Lifetime learning keeps running under a network policy.** The store is updated from outcomes exactly as before. The network learns *how to use* an individual's experience. It never stands in for it. In the owner's words of 27 Nov: a **shared trained network gives general competence, and individual associations give personal experience.** Two NPCs running the same network still behave differently because their stores differ (test LS9).
- **The network never writes to the store**, and its weights are fixed during a run.
- **Benchmark experiments** can set `LearningMode.Frozen` to compare policies on equal, fixed experience.

### 4.16 Learning modes
| Mode | Learned term | Updates | Use |
|---|---|---|---|
| `On` | yes | yes | Default |
| `Frozen` | yes (as loaded) | none (traces still run, so switching back is seamless) | Benchmarks, policy comparisons, `STRATEGY_BENCHMARKS.md` policies. `Frozen` from birth has an empty store, so it decides exactly as `Off`; the LS scenarios use it as the "nothing learned" baseline. The island control (M3.27) also runs a trained-then-frozen arm (On for 10 days, then `Frozen`), which is what "as loaded" means for a population |
| `Off` | 0 | none | Instinct-only baseline; ablation experiments |

The mode is set per NPC by the scenario, or for all NPCs with `SimTuning.Learning.DefaultMode`. Benchmark policies such as `TitForTatNpcPolicy` act only where they apply and fall back to the default policy, which still uses the store.

### 4.17 Required lifetime learning scenarios (owner, 27 Nov)
*Creatures*-style lifetime learning is fundamental to the game. It is **never mere telemetry, a skill counter or machine-learning scaffolding**: what an NPC learns changes what it does next, in context, and two NPCs with the same genome act differently because they lived differently. The owner named eight examples that must work. Each is a scenario in the runner (`scenarios/learning/ls<n>_*.scenario.json`) and an acceptance test (section 10, LS1–LS8; [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md)).

Rules for all eight:
- **The rewarding choice is not the instinct default** in the test arm, so only learning can explain the change. Each is compared with the same NPC in `Frozen`-from-birth mode and, where stated, with a control environment whose consequences are reversed (different experience gives different behaviour).
- **Credit is contextual:** the lesson holds in the context where it was learned and not, or much less, outside it (for example a learned preference for the state's postings does not spread to a private employer who defaults).
- **Credit is delayed** where the owner's example is delayed: the reward arrives through a trace (4.5) or an episode (4.7), and the test also runs with traces or episodes disabled to show the delay mechanism carries the lesson.
- No scenario scripts the learner's choices. Other NPCs may be benchmark policies (`bench.*`) so that the consequences are controlled.

| # | Owner example | Mechanism | Scenario | Milestone |
|---|---|---|---|---|
| LS1 | **Hungry near food** | Drive relief on `Eat` in context `DriveBand(Hunger) & ObjCategory(Food)`; no reward when full (L1, L2) | An NPC with `InstinctTable.None` in a glade with food in sight, hunger rising; days 1–5 | M3 |
| LS2 | **Hungry with no food: producing goods enables a later food exchange** | Two links of delayed credit. (a) The exchange is rewarded by hunger relief, so `Offer` and `AcceptOffer` of logs for food gain value in `Hunger & no food`. (b) The NPC's price belief for logs rises from its own completed exchanges ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)), so `Gather(LogPile)` while hungry with no food earns a Goods reward at once, and its trace also receives part of the later hunger relief | No wild food reachable; logs reachable; a `bench` trader NPC at a fixed spot gives food for logs and nothing for empty hands; days 1–10 | M5 |
| LS3 | **Tired near shelter** | Fatigue relief is faster in a hut, so `SeekShelter` and `Sleep` at the hut beat sleeping on the spot in context `DriveBand(Fatigue) & ObjCategory(Shelter)` | A hut with a free bed within 3 nodes; the instinct default is to sleep where tired; days 1–5 | M3 |
| LS4 | **Gathering wood with an axe** | An axe multiplies the gathering rate, so `Equip(Axe)` before `Gather(LogPile)` yields more goods per trace (Goods channel), and the conjunction `ObjClass(LogPile) & Self(carrying tool)` is recruited | An axe lies by the hut; logs 4 nodes away; the NPC values logs (it needs a hut); days 1–8 | M3 |
| LS5 | **Helping someone: future reciprocity** | `Help` opens an episode (4.7) carrying the obligation; a returned favour or gift closes it with its value days later | Neighbours are `bench.tft` (they return help) in the test arm and `bench.alld` (never return) in the control arm; days 1–15 | M7 |
| LS6 | **Lending to an unreliable person: loss** | `Lend` opens an episode; `LoanDefaulted` closes it with the loss; `NpcRef` and `NpcRel` entries carry it to the borrower and, more weakly, to similar borrowers | Borrower A always defaults, borrower B always repays; both ask the learner repeatedly; days 1–15 | M7 |
| LS7 | **Working for a reliable state: paid** | `TakeJob` on a state posting opens a wage episode; `WagePaid` closes it with the wage, `WageUnpaid` with nothing; the lesson attaches to `Posting(state)` and `NpcGroup(StateStaff)` | Test arm: the state pays every contract on time. Control arm: the state defaults on half. A private employer posting the same work at the same pay is present in both; days 1–15 | M6 |
| LS8 | **Joining a poorly managed military: injury, hunger, unpaid wages** | `Enlist` opens a service episode; injury, rising hunger from missing rations, and `WageUnpaid` all close into it; later `Desert` while hungry and unpaid gains value | Test arm: a quartermaster with no rations, pay withheld, squads sent into losing fights. Control arm: fed, paid, sensible assignments. Recruitment postings repeat after the first tour; days 1–20 | M9 |

What each test asserts is in section 10 (LS1–LS9). Together with L16 (every domain learns) they are the evidence for "genuine individual learning", "contextual and delayed reinforcement" and "different behaviour from different experience" in the owner's evidence list ([ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md#43-owner-evidence-list-27-nov)).

## 5. Interactions with other systems

| System | What flows |
|---|---|
| `Population` ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)) | Drive levels and comfort bands (reward); traits (patience, honesty, conformity); life stage (learning rates). A newborn gets an empty store |
| `Genome` ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)) | Instinct gains (Hunger, Fear, Social, Nurture) that scale the instinct priors; `LearnRate`, `ForgetRate` and `Curiosity` (500–1500 permille) |
| `Cognition` ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)) | Builds base and target features and candidates; calls `Score` and `OnChosen`; uses learned salience |
| `Social` ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)) | Relationship classes (`NpcRel`); obligation events close episodes; trust stays in Social |
| `Comms` | `MsgAct` features from decoded messages; `Say*` verbs learn from listeners' responses. Learned lexicons are Comms' own tables (later) |
| `Culture` ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) | Held traits add `Culture(c)` to the score, and Learning learns from the consequences as normal. No code path writes the store from `CultureStore` or the reverse |
| `Economy` ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)) | Ware categories for `ObjCategory`; exchange steps and theft as goods signals |
| `State` ([STATE_DESIGN.md](STATE_DESIGN.md)) | Job and contract postings (`Posting`); wages, taxes and welfare as goods and opportunity signals; episodes for contracts |
| `Military` ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) | `Role(Soldier)` and battle features; `IMilitaryPolicy` reads the same store; pay, rations, wounds and defeat as signals |
| `Policy` | `MindView` (3.6), owned here: `CandidateTerms`, top entries, the summary and origin shares, plus the drive, belief and relationship parts its owners fill |
| `Telemetry` | Events and snapshot fields (section 7) |

**Learning across domains** comes from the same mechanism. Only the features, verbs and signals differ:

| Domain | Typical keys | Signals that teach |
|---|---|---|
| Survival | drives, `Place`, `NpcGroup(Enemy)`, `Time(Night)` | drive relief, injury |
| Gathering | `ObjClass`, `PlaceRef`, `ObjState` | hunger relief, goods, frustration |
| Production | `ObjCategory(Workplace)`, `Self(HasJob)`, skills as `Situation` | goods, wages (episode) |
| Equipment | `ObjCategory(Tool/Weapon)`, `Role` | work speed as goods, wounds avoided |
| Barter | `NpcRel`, `MsgAct(Offer)`, `ObjCategory` | goods gained or lost per hand-over |
| Cooperation | `NpcRel(Friend/Kin)`, `Situation(FriendInNeed)` | reciprocity (episode), loneliness relief |
| Exploitation | `Situation(Unwatched)`, `NpcRel`, `Self(Owes)` | goods gained, later retaliation or expulsion |
| Relationships | `NpcRef`, `NpcRel` | anger, loneliness, gifts, betrayal |
| Communication | `MsgAct`, speaker `NpcRel`; `Say*` verbs | what followed: food given, offer accepted, warned danger real |
| Employment | `Posting`, `NpcGroup(StateStaff)`, `NpcRel(Employer)` | wages (episode), fatigue, dismissal |
| Military service | `Role(Soldier)`, `Situation(Outnumbered)`, `Place(Battle)` | pay, rations, wounds, fear relief on retreat |
| Faction membership | `NpcGroup(OwnFaction/OtherFaction)`, `Posting` | treatment by officials, acceptance or rejection on migration |

## 6. Player and view
- **The NPC inspector gets a "Habits" panel.** It shows the NPC's own top 5 positive and top 5 negative associations, in words built from the keys: "When hungry near food: eat (strong, own experience)", "Offers from strangers on the road: refuse (moderate, seen others)".
- **"Why" line:** the last decision is shown with its term breakdown: instinct, learned, social, personality, cost, explore.
- **Counters:** the panel shows entry and conjunction counts and the origin shares.
- **The view reads the store only.** Nothing in the panel changes it. All of this is the NPC's own mind, not true world facts.
- **Optional "learned something" marker:** a small marker over an NPC when a sampled `AssociationUpdated` event with a large `|δ|` arrives. It is off by default and decorates an event; it decides nothing.

## 7. Telemetry

| Event | When | Fields |
|---|---|---|
| `AssociationUpdated` | Sampled: `|δ| ≥ 1000` always; otherwise when `hash(npcId, day) % SampleEvery == 0` (default 16). The sampling uses a hash, never the sim's random streams | tick, npc, key, verb, δ, newStw, newLtw, origin |
| `ConjunctionRecruited`, `ConjunctionFreed` | At sleep | tick, npc, slot, A, B, C, strength |
| `EpisodeClosed` | When an episode closes | tick, npc, episodeKey, verb, return, daysElapsed |
| `DecisionMade` | One per chosen intent, built only when the host asks (`EventBuffer.TraceDecisions`; the runner sets it at Full recording of the learning domain, M3b). Not sampled; off at every other level, so it never costs anything or touches the hash | tick, npc, verb, target kind, id and node, target class, total, instinct, need, learned, social, culture, personality, cost, explore, candidates, learnedChangedWinner, exploreWon, continued, runner-up verb and total. Table `fact_decision` (code 334) |
| `AssociationsImitated` | When an `Import` runs (4.14, M11) | tick, learner, model, entries copied, gainShift. This is Learning's imitation; trait adoption is Culture's `TraitAcquired` |

**Daily person snapshot fields:**
- `LearnEntries`, `LearnConj`;
- `LearnOriginOwn`, `LearnOriginVicarious`, `LearnOriginImitated` (permille);
- `MeanAbsLtw`, `RewardSumDay`, `PunishSumDay`;
- `LearnedShareDay`: the permille of decisions where the winner differs from the winner without the learned term. This costs one extra comparison per candidate;
- `ExploreShareDay`: the permille of decisions won only because of `Explore`;
- `Top3Keys`.

**Learning snapshot on demand:** the runner's `learn-dump --npc N` writes the full store as JSONL for analysis. It is outside the sim.

With recording off, none of these are built, and the hash is unchanged ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)).

## 8. Stage 4 minimum vs later stages

| Milestone | Learning delivers |
|---|---|
| **M3 core** | `AssociationStore` with the caps above; feature kinds 1–6, 11–13, 15–16; pairs only in conjunctions; instincts as priors (fixed table until M4); drive and frustration signals, plus goods and the bed right; traces and the update rule; sleep consolidation, decay (with `DecayScalePermille`) and eviction; exploration; the three modes; save version 3 and the hash; inspector "Habits"; tests L1–L13; owner scenarios LS1, LS3, LS4 |
| M4 | Instincts from the `Genome`; learning rates by life stage; kin features; empty store at birth; children watching parents within the household (the M7 rule at a basic level) |
| **M5 social** | `NpcRel`, `NpcGroup`, `NpcRef`, `MsgAct` features; triples; barter and gift signals; `Say*` verbs; obligation signals for direct exchanges; tests L14 and L16 (survival, gathering, barter, relationships, communication); owner scenario LS2 |
| M6 | `Posting` features; the employment verbs; episodes for wages and contracts; owner scenario LS7 |
| **M7 observational** | Episodes for promises and loans; vicarious learning (4.13); exploitation and cooperation signals; test L15; owner scenarios LS5, LS6 |
| M8 | Enterprise, hiring and theft contexts (production, exploitation domains) |
| M9 | Military features, verbs and signals; `IMilitaryPolicy` reads the store; owner scenario LS8 |
| M10 | Faction membership contexts and signals (migration, defection, acceptance) |
| **M11 cultural** | Imitation (4.14) with `ExportTop` and `Import`, origin `Imitated`, `AssociationsImitated`; a parent's example for children; `Culture(c)` in the score; origin shares in snapshots; test L17 |
| M13 | Learning facts in the warehouse (from events and snapshots) |
| **M15 network inputs** | `MindView` encoding for `IntNetNpcPolicy`; tests L18 and LS9 |
| Stage 5+ | Learned perception categories (new `ObjCategory` from experience); learned attention; learned lexicons and emergent signals (Comms); conjunctions over remembered episodes; level-of-detail learning for distant NPCs |

## 9. Migration from the existing code
- There are no NPCs in the stage 3 code, so nothing is removed.
- `AiBrain` is unaffected: factions don't use this module.
- The earlier cognition draft's hand-made utility scoring splits into:
  - instinct priors (expected drive relief);
  - perceived costs;
  - the learned term.

  The default policy is `AssociativeNpcPolicy` ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#47-the-decider)).
- Every random draw needs a forked stream. M0 adds the `Fork(string, int)` overload, so learning uses `Rng.Fork("npc", id).Fork("learn")`.
- `GameSetup.Population` off means no stores and the stage 3 hash is unchanged.

## 10. Tests and acceptance criteria
All tests run under `dotnet test DotNet` unless marked *runner*. "Seeds" means distinct game seeds. "Think ticks" assumes `ThinkEvery` 10. Numbers are first targets, to be confirmed in M3 and then frozen in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

| Id | Milestone | Test | Pass |
|---|---|---|---|
| L1 | M3 | **Naive forager.** An NPC with `InstinctTable.None`, hunger rising at the default rate, a berry bush in sight, everything else default. Over days 3–5, count the think ticks at which it is hungry (band ≥ 2) and attending the bush | It chooses `Eat` within 5 think ticks of becoming hungry in ≥ 95% of hungry episodes, in ≥ 95% of 100 seeds |
| L2 | M3 | **Eat when hungry, not when full** (*Creatures*' result). Same as L1, with its comfort-band episodes counted | Eats in ≤ 15% of think ticks with hunger below comfort while a bush is attended |
| L3 | M3 | **Delayed reward, 30 ticks.** Lab scenario with two test verbs `TestA` and `TestB` and a random filler verb chosen in between. `TestA` gives +800 vp exactly 30 ticks later; `TestB` gives nothing | After 200 trials, `TestA` is chosen in ≥ 90% of the last 50 trials in ≥ 95% of 100 seeds. With traces disabled (`TraceShift` 16) it stays ≤ 65%, which shows the traces matter. The filler verb's mean `Q` stays within ±100 vp |
| L4 | M3 | **Avoidance.** A food item class `Toxic` that raises pain by 300 when eaten | After ≤ 3 tastes, `Eat` on `Toxic` is chosen in ≤ 5% of opportunities over the next 5 days, in ≥ 95% of 100 seeds |
| L5 | M3 | **Forgetting after the source moves.** Bush A, visited daily for 5 days, is emptied permanently on day 6; bush B appears 6 nodes away | On days 8–10, ≤ 10% of foraging trips go to A. By day 40, A's `PlaceRef` entries have `Ltw ≤ 0` or have been evicted |
| L6 | M3 | **Adaptation of the fast weight.** As L5, but A regrows on day 12 and B empties | Trips to A are back above 50% by day 15 |
| L7 | M3 | **Learning on vs off.** A 20-NPC village scenario, 10 days, run with `On` and `Off` | `Off` runs with no errors, and the deaths it suffers are within the scenario's baseline limit (instincts alone keep NPCs alive). The decision-sequence hash differs between the modes in 100% of 20 seeds. `Frozen` leaves the store hash unchanged for the whole run |
| L8 | M3 | **Same nature, different nurture.** Pairs of NPCs with identical genome, traits and instincts. One is raised 5 days in a rich, safe glade, the other where strangers sometimes cheat (as in L14) or food is scarce | The measured rate of a behaviour (accepting a stranger's offer, or `ForageAt` vs `Gather` choice) differs by ≥ 30 percentage points in ≥ 95% of 50 seed pairs. Their store hashes differ. Control: identical setup and the same streams give identical behaviour |
| L9 | M3 | **Save and load.** Save at day 2.5 (mid-day, with traces and episodes open), load, run to day 5 | `StateHash` equals the uninterrupted run at every 100-tick checkpoint, in 10 seeds |
| L10 | M3 | **Replay and recording.** The same seed twice; recording at full vs off | Identical hashes in 10 seeds |
| L11 | M3 | **Bounds.** A 50-sim-year headless stress scenario: 200 NPCs, a varied world, random imitation imports (simulated) | At every daily check, for every NPC: `Count ≤ 256`, slots ≤ 64, `RingCount ≤ 16`, `OpenCount ≤ 8`, learning bytes ≤ 10,240, all weights within ±4000. No allocation in `Score` or the update rule (allocation counter) |
| L12 | M3 | **Integer purity.** IL scan of `God.Sim.Learning` | No `float`, `double` or `decimal`; no `System.Random`; no `DateTime` |
| L13 | M3 | **Generalisation and conjunctions.** NPC raised on berries (L1 setup), then shown fish for the first time while hungry. Control: an NPC raised where berries were never edible | The first `Eat` on fish comes within 3 think ticks in ≥ 80% of 100 seeds (control ≤ 40%). A `Hunger & Food` conjunction is recruited by day 3 in ≥ 90% of seeds |
| L14 | M5 | **Avoidance after being cheated.** An NPC trades with stranger S, who takes the goods and leaves, 2 times. Social trust is zeroed to isolate learning | Accept rate for S's next 5 offers ≤ 20% (baseline before the cheating ≥ 60%). Accept rate for other strangers falls by ≥ 15 points but stays above S's (generalisation with specificity). After 10 honest trades with other strangers, stranger acceptance is back above 50% within 5 days |
| L15 | M7 | **Vicarious learning.** An observer watches 3 others eat `Toxic` and show pain, and 3 eat a new safe food and relax. It never tastes either | Its first-choice `Eat` rate for `Toxic` is ≤ 50% of a naive control's; for the safe food it is ≥ 150% of the control's. In 100 seeds |
| L16 | M5 → M10 | **Every domain learns.** For each of the 12 domains (section 5), a scenario where the rewarding choice differs from the instinct default | Compared with `Frozen`-from-birth, the learned NPC's rate of the rewarding choice differs by ≥ 20 points in ≥ 90% of 50 seeds. Each domain's test is added in the milestone that introduces it |
| L17 | M11 | **Import is deterministic and bounded.** A parent with 200 entries teaches a child (`Import`, shift 1) | The same seed gives the same child store hash; caps hold; the child's imported entries change by its own experience within 5 days in an environment that contradicts them |
| L18 | M15 | **Network inputs use learned state.** `IntNetNpcPolicy` with a test model that copies `Q(c)` into its score | It picks the same action as the default policy with the instinct term zeroed, in 100% of 1,000 recorded decisions |
| LS1 | M3 | **Hungry near food** (4.17). 100 seeds | L1 and L2 pass in this scenario. `Frozen`-from-birth with `InstinctTable.None` eats within 5 think ticks of becoming hungry in ≤ 40% of hungry episodes, so the learned rate (≥ 95%) is shown to come from learning |
| LS2 | M5 | **Hungry with no food; producing goods enables a later food exchange.** 50 seeds | By days 8–10, when hungry with no food, the learner chooses `Gather(LogPile)` or goes to the trader with logs in ≥ 20 points more think ticks than `Frozen`, in ≥ 90% of seeds. Contextual: when food is in sight it still eats first in ≥ 90% of hungry episodes. Delayed: with episodes off and `TraceShift` 16 the gap shrinks by at least half (reported with interval). Control (trader gives nothing): no gain over `Frozen` (within ± 10 points) |
| LS3 | M3 | **Tired near shelter.** 50 seeds | By days 4–5 the learner sleeps in the hut in ≥ 20 points more fatigue episodes than `Frozen`, in ≥ 90% of seeds. Contextual: with no hut within 10 nodes it still sleeps where it is (no wasted walk beyond 10 nodes in ≥ 95% of episodes) |
| LS4 | M3 | **Gathering wood with an axe.** 50 seeds | By days 6–8 the learner equips the axe before gathering logs in ≥ 20 points more gathering trips than `Frozen`, in ≥ 90% of seeds, and a conjunction containing `ObjClass(LogPile)` is recruited by day 8 in ≥ 80%. Control (the axe gives no speed-up): no gain over `Frozen` (within ± 10 points) |
| LS5 | M7 | **Helping someone: future reciprocity.** 50 seeds per arm | By days 12–15 the learner's `Help` rate towards neighbours is ≥ 20 points higher in the `bench.tft` arm than in the `bench.alld` arm, in ≥ 90% of paired seeds. Delayed: with episodes off the difference shrinks by at least half (reported). The arms differ only in the neighbours' policies, so the difference is learned from experience |
| LS6 | M7 | **Lending to an unreliable person: loss.** 50 seeds | After 3 defaults by A, the learner lends to A in ≤ 20% of A's later requests while still lending to B in ≥ 60% of B's, in ≥ 90% of seeds (contextual, by `NpcRef`); acceptance of a new stranger's loan request falls by ≥ 10 points but stays above A's (generalisation with specificity) |
| LS7 | M6 | **Working for a reliable state: paid.** 50 paired seeds | By days 12–15 the learner takes state postings over the identical private posting ≥ 20 points more often in the reliable-state arm than in the defaulting-state arm, in ≥ 90% of paired seeds. Contextual: the learned preference attaches to the state; a private employer who pays reliably is not avoided in either arm (difference within ± 10 points). Delayed: with episodes off, the arm difference shrinks by at least half (reported) |
| LS8 | M9 | **Joining a poorly managed military.** 50 paired seeds | After the first tour, re-enlistment when a posting reappears is ≥ 20 points lower in the poorly managed arm than in the well-managed arm, and `Desert` is chosen in ≥ 20 points more hungry, unpaid think ticks, in ≥ 90% of paired seeds. `Frozen` learners show no arm difference in re-enlistment (within ± 10 points), so the difference is learned, not instinct or morale alone |
| LS9 | M15 | **Shared network, personal experience.** Two NPCs with identical genome run the same `IntNetNpcPolicy`; one lived LS6's arm with A defaulting, the other with A repaying | They differ in lending to A by ≥ 30 points in ≥ 95% of 50 seed pairs; with identical histories their decisions are identical. Lifetime learning stays on under the network (store hash changes during the run) |
| L19 | M3 *runner* | **Cost.** 500 NPCs, 1 sim day, the default scenario | Learning (context, conjunctions, `Q`, credit, updates) ≤ 20 µs mean per think on the reference machine, and ≤ 25% of total NPC update time. Consolidation ≤ 5 ms per sim day for all 500 |

**The cost estimate behind L19:**
- One think does about 5,000 simple operations:
  - context: about 200;
  - conjunctions: about 1,300;
  - `Q` lookups: 16 candidates × 24 keys × 8 comparisons ≈ 3,000;
  - one or two trace closes: about 500.
- That is about 5–10 µs.
- 500 NPCs make 50 thinks per tick, so learning costs 0.25–0.5 ms per tick:
  - about 6 s of CPU per sim year (12,000 ticks);
  - about 10 minutes for 100 sim years.

## 11. Open questions
No owner question remains. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
- **Q1 Decay and `LifeScale`:** keep the default. `LifeScale` affects biological life-cycle durations only; it does not scale learning or association decay. The decay rates are independently configurable for experiments (`SimTuning.Learning.DecayScalePermille`, use and conjunction decay; section 4.8), separately from social-memory decay.
- **Q7 Loyalty as a reward:** no. Faction loyalty is never rewarded directly; it emerges from experiences, relationships, security, living standards, cultural beliefs and treatment by the state. An optional configurable `Belonging` drive may give social motivation, never a reward for obeying a faction. NPCs can learn that staying loyal, migrating, defecting or becoming independent pays under different circumstances; the state must earn loyalty (section 4.4).
- **Further requirement (27 Nov):** the eight owner learning examples are required scenarios with acceptance tests (section 4.17, LS1–LS8), and learning stays active after networks arrive (section 4.15, LS9).

Settled here, with defaults:
- **`NpcRef` vs trust.** Keep both: `NpcRef` entries learn "trading with Ana pays", social trust learns "Ana is honest". M5 runs L14 both ways and reports the overlap; a measured double-count is fixed by lowering `NpcRef`'s gain, not by removing it.
- **Vicarious context.** No inferred drives in stage 4 (as in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#11-open-questions)). Vicarious lessons use only visible keys.
