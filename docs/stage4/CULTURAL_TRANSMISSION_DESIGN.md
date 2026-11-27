# Cultural transmission design (stage 4)

## 1. Purpose and scope

**This document owns** cultural inheritance: how named, shareable traits (norms, techniques, beliefs, conventions, stories and traditions) are held by NPCs, spread between them and between generations, change, drift and are measured.

**It links to, and does not repeat:**
- genes and personality predispositions: [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md);
- individual associative learning, including observational (vicarious) learning: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md). The boundary between that store and the cultural store is defined in section 4.9 and is the key rule of this document;
- the relationship table that supplies models and their esteem: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- the gesture lexicon and the cost of dialect mismatch: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#learned-meanings-and-dialects-later);
- recipes, yields and prices themselves: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md). Culture decides *who knows* a technique;
- inheritance customs as traits, and what they do at a death: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md);
- political beliefs and their consequences: [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md);
- institutional education and ages unlocked by shared knowledge: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#ages-and-unlocks);
- households and child-rearing (who the parents are): [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md);
- source research, Dawkins on memes (Ch11): [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md#f1-memes-ch11-pdf-212224-bk-189201).

## 2. Principles
1. **Culture is data, not script.** A trait is an id with a strength in an NPC's store. It changes the decider's *scores* through defined hooks. It never forces an action.
2. **No trait is good by design.** Traits have costs and benefits through ordinary consequences (time, goods, trust, safety). Useful, neutral and harmful traits all exist in the content, and the sim does not know which is which.
3. **Transmission is occasion-based.** Traits move only through contact: a child with its household, workmates, a teacher with a learner, an NPC watching a neighbour. No global broadcast, no "everyone learns at once".
4. **Nobody sees inside another head.** An NPC copies what it *sees displayed* and what it is *told*. It can copy wrongly.
5. **Four kinds of inheritance stay separate in code and data.** Genes (a `Genome`), lifetime learning (`AssociationStore`), culture (`CultureStore`), material (`Estate`). A change in one never silently writes the other.
6. **Bounded and cheap.** At most 32 traits per NPC, a daily pass per NPC, a fixed order.
7. **Measure, never steer.** The sim reports how common each trait is by faction and generation. No code rewards a trait for being common or rare.
8. **Determinism.** One forked stream per NPC (`Rng.Fork("npc", id).Fork("culture")`), ascending id, no unordered enumeration.

## 3. Data structures (`Sim/Culture`)

### 3.1 Trait definitions (content, read-only, in `SimTuning.CultureDefs`)

```csharp
enum TraitKind : byte { Norm, Technique, Belief, Convention, Story, Tradition }
enum ParamKind : byte { None, Hamming, Absolute }       // how two Param values differ

struct TraitDef {
  ushort Id;                 // unique across the content
  TraitKind Kind;
  byte   Locus;              // 0 = none. Variants at the same locus compete (see 4.4)
  ParamKind ParamKind;
  byte   Visibility;         // 0..255: how easily it is displayed to onlookers
  byte   Cost;               // 0..255: effort to hold it (attention slot pressure)
  ushort[3] SynergyWith;     // co-adapted partners (complexes, 4.7); 0 = none
  short  SynergyBonus;       // per mille added to adoption odds per partner held
  ushort[3] Excludes;        // rivals that cannot be held strongly together
  ushort Hook;               // what it does: recipe id, decision term id, belief id, lexicon id ...
  ushort ParentId;           // for refined techniques and variants; 0 = none
}
```

### 3.2 The per-NPC store (bounded)

```csharp
struct CultureStore {                       // about 270 bytes per NPC
  byte Count;                               // up to 32
  TraitEntry[32] Trait;                     // 6 bytes each = 192 B
  Exposure[16] Recent;                      // ring, 4 bytes each = 64 B
  short DayCounter, Flags;
}
struct TraitEntry {
  ushort Id;
  byte   Strength;      // 0..255: 0-63 "heard of it", 64-127 "tried", 128+ "holds it"
  byte   Flags;         // source channel (2 bits), innovated, taught, last-day bits
  short  Param;         // value for ParamKind traits (a ratio, a dialect code); else 0
}
struct Exposure { ushort TraitId; byte ModelEsteem; byte Channel; }  // what I saw lately
```

- **Holding** a trait means `Strength ≥ 128`. Below that the NPC has heard of it and it has little effect (a Technique below 64 is not usable; a Norm below 64 adds no guilt).
- **Eviction** when the store is full: remove the entry with the lowest `Strength × (Visibility + 64) / 256`, ties by highest index. Entries that fall below `Strength 8` are deleted.
- **Locus:** variants at one locus are alternatives (inheritance custom, dialect, a price convention, a stance on a rival faction). An NPC may carry several variants at different strengths. The strongest is its **active variant** and is the one the decider and the estate code read.
- **Param traits** carry a number so a convention can change without a new definition: an exchange ratio (fish per berries, in per mille), or a 16-bit dialect code (distance is Hamming). Copies of a Param trait mutate by small noise, which is how drift happens without a registry.

### 3.3 What a trait can do (hooks)

| Kind | Example content | How it enters the sim |
|---|---|---|
| **Norm** | Share with hut-mates; first arrival owns; repay debts; greet strangers; punish cheats | Adds a **guilt cost** for violating and a small **reward** for complying to candidate-intent scores (scaled by Strength). Holders also gain a score term to `Accuse` violators. Never a rule. |
| **Technique** | Smoking fish (food lasts 3 times longer); a better foraging route; weaving; a hut-building method; a gear recipe | Knowing the technique (`Strength ≥ 128`) permits the recipe or gives a yield multiplier, through the `Economy` hook. A learner may try at `64..127` at reduced yield. |
| **Belief** | "Faction X cheats"; "the winter is long"; "the state's tax is fair" | Seeds `Mind.Beliefs` and `GroupRep` as a prior with confidence `Strength`. Later, direct experience can override it. |
| **Convention** | Gesture dialect; an exchange ratio; how work shifts are timed; an inheritance custom | Coordination content. A convention's value is only that others share it. Read by `Comms`, `Economy` and `Estate`. |
| **Story** | "The tale of the broken promise" | Carries one belief or norm. Being told it adds Strength to its payload trait, and it spreads easily (high Visibility). |
| **Tradition** | A feast every 5 days; the first-fruits gathering | A recurring event at a place. Everyone present is exposed to a set of traits at once. Held in the faction's calendar, not in an NPC. |

## 4. Mechanics and algorithms

### 4.1 When and who

- Each NPC runs a **culture pass** once per day on its slot: `(tick + id × 11) % TicksPerDay == 0`. The pass takes at most 3 exposures (a limit on attention), so the cost per NPC per day is constant.
- NPCs are processed in ascending id within a tick. Reads of another NPC's store see its current state. This is deterministic, because the order is fixed.
- **Candidate models** for the pass come from social memory and context, in this fixed order, deduplicated:
  1. household members and parents (kinship from [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md), as known to the NPC);
  2. relations with `LastTick` in the last 2 days ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#3-data-structures-simsocial)), ascending `OtherId`;
  3. NPCs in the same building or worksite this tick (up to 6).
  Up to 8 candidates are kept.
- For each exposure, the **model displays one trait**, drawn by weight `Strength × (Visibility + 32)` from its own store, using the *learner's* stream (so the learner's draw order is fixed). The exposure goes into the learner's `Recent` ring.

### 4.2 Transmission channels

All channels use the same adoption test in 4.3. They differ in who the models are and in a base rate (per mille per exposure, defaults in `SimTuning`).

| Channel | Models | Base rate | Notes |
|---|---|---|---|
| **Vertical** (household and parents to children) | The child's parents and household adults | 120 | Strongest for children under sim year 1. Weight of `Susceptibility` below. Parents are the first candidates, every day. |
| **Horizontal** (peers) | Same life stage, from contact lists | 40 | Workmates, hut-mates, play. |
| **Oblique** (elders and teachers) | Elders, or anyone with higher `Esteem` than the learner in the same trade | 60 | Includes apprenticeship. Institutional schools are the same channel later. |
| **Prestige bias** (imitate the successful) | Chosen from the candidates by **observed success** | rate × model weight | See 4.3. The learner sees goods, hut, job, skill and others' deference. It does not read true wealth. The cue is stored as `Relation.Esteem`. |
| **Conformity bias** (majority) | The learner's own tally of recent exposures at that locus | multiplies odds | See 4.4. |
| **Teaching** (costly, deliberate) | A teacher who chose the `Teach` intent | 400 per session | See 4.5. |
| **Tradition** | Everyone at a gathering | 100 | Reinforces, rarely introduces. |
| **Migrants** | The newcomer's carried store | n/a | A migrant arrives with its store intact (4.8). |
| **Institutional education** | A school's staff, Stage 6 | n/a | Same adoption test, higher base rate, paid for by the state. |

**Susceptibility** by life stage (per mille multiplier): child 1000, adult 400, elder 200. A trait learned in childhood tends to be held for life because its Strength is high.

### 4.3 The adoption test (one exposure)
Learner L sees model M display trait `t` with M's `Strength s_M`.

```text
odds = BaseRate(channel)                       // per mille
     × Susceptibility(L)
     × (s_M + 32) / 288                         // stronger models persuade more
     × (500 + Esteem(M)) / 500                  // prestige: Esteem 0..255 gives ×1.0 to ×1.5
     × (1000 + Synergy + FrequencyBias) / 1000
     × TraitCostFactor
     if already held:  reinforce, else try to acquire
adopt if  rngLearner.Next(1000) < clamp(odds, 0, 950)
```

- **Reinforce** (trait already held): `Strength += 8` (cap 255).
- **Acquire:** new entry at `Strength 64 + 8`, plus noise `±16` (copying is noisy). A Param is copied with a mutation of ±1 step with probability `MutationPermille` (default 20 per transmission; 1 Hamming bit flip for codes).
- **Decay** daily: `Strength −= 1` if not displayed or reinforced for 5 days, floor 0. A trait that nobody uses fades.
- **Rival variants** at the same locus: acquiring `t` reduces the active variant's Strength by 12 (competition for attention). `Excludes` pairs lose 16 on each side each time the other is reinforced.

The **success cue** `Esteem(M)` is the learner's own belief, from [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md), updated by what it saw (goods carried, a hut, a job, skill, contest results) and by `Praise` gossip. A model whose Esteem is wrong (flashy but poor) is imitated anyway. That is a property of the mechanism.

### 4.4 Conformity (majority bias)
Each NPC keeps a tally in `Recent` of the last 16 exposures. For an exposure to variant `v` at a locus with `N` exposures in the ring and `n_v` of them for `v`:

```text
share_v = 1000 × n_v / N
FrequencyBias = Conformity × (2 × share_v − 1000) / 100        // Conformity is the NPC's trait, -100..100
```

- At `Conformity +100`: a variant with share 1000 gets `+1000` (odds doubled). A variant with share 0 gets `−1000` (odds zero).
- At `Conformity −100`: the sign flips. Rare variants are favoured (an anti-conformist).
- At `Conformity 0`: no frequency effect.
- **Conformity also scales the whole rate**: `Susceptibility` is multiplied by `(1000 + Conformity × 4)/1000` (so `+0.4` to `−0.4`). This follows the meaning of the trait in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#personality), "copying what neighbours do".
- Conformity is a **gene-based predisposition** (a personality trait). The *frequency* it responds to is cultural. Two causes, kept apart.

### 4.5 Teaching (a costly act)
`Teach(trait, learner)` is an NPC intent scored by the decider. Teaching is an action, never automatic.
- **Costs:** the teacher's time (one session is 300 ticks, 30 seconds at 1x) and what it would have earned working. The learner also spends the time (an apprentice works beside the teacher, so a technique session can double as work).
- **How it looks:** teacher and learner do the task together: `Follow`, `Cooperate`, then demonstrate. No new gestures. Norms are taught by example, `Accuse` at violators, and storytelling at a gathering.
- **Effect:** adoption odds `400 × s_T/255 × Susceptibility(L) × skill factor`, always 1 try per session. Success sets the learner's Strength to at least 128 for a Technique (practice then raises it).
- **Why a teacher would:** kin weight (a child or relative), a payment in barter, being thanked (status drive), help in return (a ledger entry), a norm ("teach the young"), a state wage later. The scoring uses social memory: affection, kin cue, creditor flags.
- **Why it can fail:** the learner may lack the skill; the teacher may teach a thin version (its Strength is low), or may deliberately hide a technique. A hidden technique is simply an unscored teach intent.
- **Telemetry** records every session with cost and result.

### 4.6 Innovation and mutation
- **Individual discovery becoming cultural.** A Technique can be authored with an **unlock rule**: `Skill ≥ X` in a trade, `Attempts ≥ Y` of the related work, and a roll per day at a small rate (`DiscoveryPermille`, default 2). On success the NPC gains the trait at Strength 200 and the sim emits `TraitInnovated`. It is then teachable and imitable like any other. Chains (`ParentId`) give refinements: a better net after a net.
- **Param drift.** Conventions that carry a `Param` (ratios, dialect codes) mutate in copying, so new variants appear without authoring. Mutations are not better or worse by design; they are only different.
- **Misheard teaching:** a copy of a Technique at an adjacent variant (same locus) with probability `MutationPermille/4`.
- **Never by design:** a technique does not appear because the state wants it. The state can only change consequences (see [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Age unlocks.** A civilisation age's unlock rules can include "technique T held by at least X percent of adults". The measured census (section 7) is the source. Culture neither forces nor schedules ages.

### 4.7 Co-adapted complexes (Dawkins, Ch11)
Traits that help each other spread form a **meme complex**. The mechanism makes complexes possible without scripting them:
- `SynergyWith` and `SynergyBonus`: holding a partner raises the odds of acquiring the trait by `SynergyBonus` for each partner held (default +150 per mille), and reduces decay.
- Limited attention (32 slots, a cost per trait, a cap of 3 exposures per day) means traits compete.
- `Excludes`: rival complexes exclude each other.
- **Example content** (all optional, all with costs): `StoreFood` technique, `ShareGranary` norm, `Winter-is-long` belief, a `FirstFruits` tradition. Together they raise one another's odds; a belief alone is less attractive.
- **The effect Dawkins notes**: a pool of mutually supporting traits is hard for a newcomer to invade, because the newcomer lacks the partners. The sim can show this or not. Section 10 has an invasion measure, not an assertion.

### 4.8 Dialects, drift between factions and migrants
- **Dialects.** The gesture dialect is a Convention trait with a 16-bit Param code. Distance between two NPCs is the Hamming distance of their active dialect codes. [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) owns what a bit means and how misunderstanding grows with distance (proposed +20 per mille per bit).
- **Drift.** Within a connected population, the shared dialect is held together by conformity. Separate groups with no contact drift apart through mutation and sampling.
- **Between factions.** Exposures come from contact lists, so factions that rarely meet mix little. The measured dialect distance between factions is expected to grow with time since separation (measure, section 10).
- **Migrants carry culture.**
  - A migrant arrives with its whole `CultureStore`. It displays its own traits to its new contacts, so it seeds new variants.
  - The migrant's own `Susceptibility` is the adult value, so it adopts slowly. Under strong local conformity a lone migrant's variant is usually crowded out, while a group of migrants may persist.
  - Children of migrants are exposed to both household and neighbourhood, so they are expected to assimilate more, a result to measure.
  - Captured or conquered populations keep their store; what changes is who they meet.
- **Faction-level culture is not stored.** A faction's culture is the distribution of its citizens' traits. A "faction norm" is only a high share.

### 4.9 The boundary between culture, learning, genes and material inheritance

| | Biological | Individual learning | **Cultural** | Material |
|---|---|---|---|---|
| Unit | Gene values in a `Genome`; personality predispositions | Weights on (situation, relationship class, intent) in an `AssociationStore` | A named trait id, a strength and maybe a param | Items, buildings, shares, enterprises, claims |
| Where | `Npc.Genome` | `Mind.Associations` | `Mind.CultureStore` | `Estate`, ownership records |
| Passed by | Conception, recombination, mutation | **Never inherited** | Contact: household, peers, elders, imitation, teaching | Death, gift, sale |
| From whom | Two parents | Own experience, and vicarious watching | Anyone seen or taught | Chosen by custom, will or law |
| Can be lost | No (fixed at birth) | Decays | Decays, can be dropped | Consumed, lost or transferred |
| Document | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) | this document | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |

**The boundary rules:**
1. **Associations are personal.** An association is a weight on a pair that the NPC's own consequences shaped. It has no name, no id shared with anyone, and no strength that another NPC can read.
2. **Imitation of associations stays inside the learning store.** When an NPC copies from a more successful neighbour (the imitation described in the learning document), it copies selected association entries *into its own association store*, as low-weight entries with origin tag `Imitated`, following [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#414-imitation-copying-a-models-entries-m11). Learning owns this imitation of associations; Culture owns named traits and their transmission. That is learned, not cultural. It is not counted in the culture census, and it is not teachable by `Teach`.
3. **Only named traits are cultural.** A trait exists in `CultureDefs` (or is a Param variant of one). It has a definition, a hook and a shared id. Only those are counted, taught and measured as culture.
4. **How learning becomes culture:** through the defined routes only. Individual **discovery** (4.6) gives a Technique with an authored id. A **norm** can be adopted by an NPC because its own experience of sanctions made it comply, but the norm trait is only *acquired* through the channels in 4.2. A regularity that many NPCs learn separately (for example "avoid the wolf pack") is **not** a trait; it is many personal associations that happen to match. The census cannot see it.
5. **How culture shapes learning:** a held trait modifies decision scores (guilt, reward, recipes). The learning core then learns from the consequences of those choices as normal. Culture never writes association weights directly.
6. **Genes versus culture:** personality traits (Conformity, Generosity, Honesty) shape *how readily* an NPC takes a cultural trait, and how strongly it acts on norms. A child's personality is not changed by culture, and a culture trait is not stored in the genome. A gene-culture interaction (a norm that changes who reproduces) can happen through consequences, and is not designed in.
7. **Material versus culture:** goods change owner by transfer records. Culture is copied, so the model keeps it. An inheritance custom is a cultural trait; the estate itself is material.

## 5. Interactions with other systems
- **Population:** `Npc.Personality.Conformity` enters 4.4. Life stage sets `Susceptibility`. Births create an empty store and start vertical exposure the next day. Deaths drop the store; traits survive only in others. The household is the vertical channel.
- **Cognition and Learning:** a held trait adds decision terms. Beliefs seeded by culture are ordinary beliefs with source "culture". Observational learning is separate (4.9).
- **Social memory:** supplies candidate models, `Esteem`, kin cue and the affection used by teaching decisions. Gossip carries stories; a `Praise` raises a model's `Esteem`. Norms such as "repay debts" change the guilt term for broken promises.
- **Comms:** dialect code in; misunderstanding out.
- **Economy:** techniques unlock recipes and yields. Exchange ratios are Param conventions that shape expected prices.
- **Estate:** the active inheritance-custom variant of the executor decides how an estate is split. Until M11 the custom is a fixed per-faction default; see [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md#43-customs-as-cultural-traits).
- **State and Institutions:** the state changes consequences, not traits. Schools, elders and a records office are channels or reinforcers. Ages read the census. Political beliefs are Belief traits owned by [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md).
- **Military:** drilled behaviour (Stage 6 doctrine) would be a Technique or Norm. Truces ("live and let live", SELFISH_GENE_NOTES E8) are conventions held by opposing local units.

## 6. Player and view
- **NPC inspector card:** lists the NPC's held traits by name ("knows smoking fish", "believes in sharing with hut-mates"), the channel it learned each from, and whether it is teaching. Only its own store.
- **Faction culture panel (through `FactionKnowledgeView`):** the share of adults holding each trait, and a breakdown by generation. In stage 4 it reads the full census. Later ages can limit it to what a census office has surveyed.
- **Animations:** teaching shows as the two NPCs working together with the learner mirroring. A tradition is a gathering at a place. Dialect shows only as gestures that differ in detail. The view never decides who learns what.
- The player cannot assign traits. The state's levers are incentives and, later, schools.

## 7. Telemetry

**Events:**

| Event | Fields |
|---|---|
| `TraitAcquired` | npc, trait, channel, model (or 0), strength, adoption odds |
| `TraitReinforced` | sampled 1 in 8 |
| `TraitLost` | npc, trait, strength at loss, cause (decay, eviction, rival) |
| `TeachingGiven` | teacher, learner, trait, ticks spent, success, motive tag |
| `TraitImitated` | learner, model, trait, model esteem, channel (prestige-biased adoption; sampled 1 in 4 above 100 per day). Not Learning's `AssociationsImitated` |
| `TraitInnovated` | npc, trait, parent trait, skill |
| `TraitMutated` | npc, trait, from param, to param |
| `TraditionHeld` | faction, tradition, place, attendees |
| `Migrated` (from [POPULATION_DESIGN.md](POPULATION_DESIGN.md#citizenship-and-loyalty)) | adds the carried-trait count |

**Snapshots:**
- **Daily person snapshot:** `CultureCount`, `HeldCount`, active variant per locus.
- **Culture census** (daily per faction, and per generation bucket of 1 sim year): for each trait, `Holders` (Strength ≥ 128), `SumStrength`, `Learners` (64..127), `Adults`, `Children`. The warehouse computes **frequency by faction and generation**, and **diversity** (richness, Shannon entropy, mean Hamming distance within and between factions) from this. Entropy is computed outside the sim (floating point is allowed there). The census itself is integer and read-only.
- A dimension `trait` (kind, locus, parent) and facts `fact_trait_census_daily`, `fact_trait_transmission` are specified in [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).

## 8. Stage 4 minimum versus later

| Milestone | Included |
|---|---|
| **M4** | Nothing cultural except a **static per-faction default** for the inheritance custom, read by the estate code through a `StaticCultureProvider` (section 5). |
| **M7** | Social learning by observation stays in the learning store (4.9 rule 2). No cultural store yet. |
| **M11 (the minimum)** | `CultureStore`, `TraitDef` content, the daily pass, vertical, horizontal and oblique channels, prestige and conformity bias, teaching as an intent, innovation of Techniques (unlock rule), Param drift (a dialect code and one exchange ratio), norms with guilt and reward hooks, belief priors, the census and events, the faction culture panel. Content set of about 20 traits: 6 norms, 6 techniques, 3 beliefs, 3 conventions, 1 story, 1 tradition. |
| **M12** | Elders as teachers and norm enforcers; informal sanction by gossip. |
| **Later (Stage 5)** | Complexes in the content set (synergy rules authored), traditions at scale, schools-lite as an oblique channel, political beliefs spreading, stories as a media-like channel. |
| **Later (Stage 6-7)** | Schools, doctrine, published media as a separate channel (layer 5), learned lexicons and emergent signals, cultural evolution analysed with trained policies. |

## 9. Migration from the existing code
There is no culture in stage 3. The earlier draft's "memes" (imitation of successful neighbours in the learned weights) are now two things: social learning in the learning store (observation M7, imitation with origin `Imitated` M11) and the cultural trait store (M11). Save version 3 includes the store (an empty one before M11). `StateHash` covers it.

## 10. Tests and acceptance criteria

All run in `dotnet test DotNet` and Unity EditMode. "Controlled run" means a scenario file with fixed content, 20 replicate seeds, and reported mean and spread. **Baselines are recorded at the first M11 run and then act as regression bounds (±15 percent).** The directions and thresholds below are the design's expectations; if a run disagrees, the finding is recorded and the model reviewed. It is not forced.

**Store and boundary**
1. `CultureStore` size ≤ 280 B; 600 NPCs ≤ 170 KB. A store never exceeds 32 entries; eviction removes the lowest `Strength × (Visibility+64)/256`, ties by highest index.
2. No code path writes `AssociationStore` from `CultureStore` or the reverse (static check, like the policy reflection test). The census contains no association data.
3. A copied association (learning store) is absent from the census; a discovered Technique is present.

**Adoption maths (unit tests, 10,000 draws)**
4. A child (Susceptibility 1000) with parents who hold trait T at Strength 200, no bias: acquisition rate per exposure 120 × (232/288) = about 97 per mille (±1 percent). An adult: about 39.
5. `Conformity +100` on a variant with share 800: `FrequencyBias = +600`, so odds ×1.6. On share 200: `−600`, odds ×0.4.
6. Same seed gives identical `TraitAcquired` event streams in two runs. Recording on and off give the same hash.

**Controlled runs (reported with tolerance)**
7. **Useful technique, conformity.** Village of 40 adults, a Technique with a real productivity benefit, seeded at **60 percent** of adults. Two variants: all NPCs `Conformity +80`, versus all `−80`. Expected: the high-conformity run reaches 90 percent holders sooner. Threshold: mean time to 90 percent at least 25 percent shorter, over 20 seeds. **Note** (open question 1): seeded at 20 percent, a majority bias slows the spread until the technique is common; the same experiment is reported at 20 percent with no pass/fail, to show the crossover.
8. **Neutral drift.** Two neutral variants of a Convention, each at 50 percent in a village of 30, no prestige or benefit, `Conformity 0`. After 10 sim years the mean share across 20 seeds is 0.5 ± 0.1, and the spread across seeds exceeds 0.15 (standard deviation), showing that chance alone moves the share.
9. **Costly norm under conformity.** A Norm with a 10 percent productivity cost to holders, seeded at 70 percent, in a village with `Conformity +80`. Report the share at 3 sim years. Expected at least 50 percent. In the same setting with `Conformity −60` expected under 30 percent. Measured, not forced; no trait is rewarded for being common.
10. **Teaching cost.** A teaching session spends 300 ticks of teacher and learner time. Over a day, an NPC that teaches 3 times earns the measured work-time reduction (no free lunch: work output down by 3 × 300 ticks where not combined with work).
11. **Innovation spread.** One NPC innovates a Technique. With the default content the time for it to reach 25 percent of a village of 40 is reported, and it is always longer than the time the discoverer takes to teach one person.
12. **Dialect drift.** Two factions with no contact, same starting code, `MutationPermille 20`. The mean Hamming distance between factions after 5 sim years is at least 2 bits. With ongoing contact, it stays below 1 bit.
13. **Migrants.** One migrant carrying a variant arrives in a village at 100 percent opposing variant with `Conformity +80`: the variant has under 5 percent share after 2 years in at least 80 percent of 20 seeds. A group of 10 migrants (in 40) keeps over 30 percent. Children of migrants hold the local variant at a higher rate than their parents.
14. **Complex invasion** (reported only). A population holding a 3-trait complex versus one holding the same traits without synergy: the share a single rival variant reaches after 3 sim years from a 5 percent start, over 20 seeds.

**Measures and recording**
15. The census equals a recount from the stores for every faction and generation (read-only, exact).
16. A run with recording off and on gives the same `StateHash` at every day boundary. Save and load mid-run gives an identical stream afterwards.

## 11. Open questions
No owner questions. Settled here, with defaults:
- **Conformity's rate effect vs majority bias** (4.4): keep the balance above, and report test 7 at both the 60 and 20 percent seeds; retune only if a lone useful technique cannot spread at all.
- **Imitation wording.** Resolved: copied associations carry origin `Imitated` in the learning store ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#414-imitation-copying-a-models-entries-m11)); [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) says the same.
- **Conformity is heritable.** It is one of the 12 personality traits in the genome ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)), so test 7 has the variation it needs.
- **No runtime trait registry in stage 4.** Only Param variants and Technique refinements mutate. Revisit if the content set proves too small.
- **Exposure cap.** 3 exposures per day in total, not per locus. Revisit only if test 7 or 13 shows one locus starving the others.

---

## 12. Commandments and religious transmission (owner D5)

Owner decisions D5 add commandments: structured religious rules revealed by gods ([COMMANDMENTS_AND_RELIGION_DESIGN.md](COMMANDMENTS_AND_RELIGION_DESIGN.md), which owns them). How they relate to this document:

- **Separate store, shared maths.** Commandment knowledge lives in its own bounded per-NPC table (8 entries, `CommandmentKnowledge`), not in `CultureStore`, because it carries a structured rule, an issuing god and a revision. Exposures use **this document's adoption test (4.3)** and conformity bias (4.4) as one shared pure function, and the same 0..255 strength scale (heard, learned, adoptable).
- **Channels.** Revelation (from a god), `Recount` gestures between NPCs (M5 comms, with M5 decoding errors), the vertical household channel (a minimal pass in the Religion module until M11; from M11 this document's daily pass carries commandment exposures), gatherings as the tradition channel, and priests' preaching as costly teaching (4.5). Migrants keep their knowledge (4.8).
- **Variants.** Copies can be altered field by field (a dropped condition, a neighbouring verb, a widened object), exactly like Param drift (4.6): each NPC follows and passes on its own interpretation.
- **Decision hook.** A held commandment adds a bounded `Doctrine` part to the divine score term, like a Norm's guilt and reward hook (3.3), plus a learnable feature key. It never forces or blocks an action.
- **Boundary (4.9).** A commandment is not an association and not a named cultural trait: rule 2 still holds (no code writes associations from it), and the culture census does not count it; a separate commandment census does. A religion's gatherings and customs may become ordinary Tradition or Norm traits through the usual routes.
- **Religion is not culture.** Religious affiliation, belief and devotion are separate from cultural membership (D5.15).
