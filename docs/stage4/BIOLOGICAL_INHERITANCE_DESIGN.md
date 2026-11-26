# Biological inheritance design (stage 4)

This document owns the **genome**: how genes become bodies and predispositions, how they are passed on, and how kin relatedness is computed. Part of the life-cycle foundation (M4). Terms and numbers follow [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

## 1. Purpose and scope

**Owns:**
- the `Genome` data and the gene-to-trait map;
- recombination and mutation;
- the phenotype, meaning the traits a genome produces;
- kin relatedness from the pedigree, and the cues NPCs can use to guess kinship;
- population-genetics metrics;
- the rule that biological inheritance stays separate from the other three kinds.

**Links, does not repeat:**

| Topic | Owner |
|---|---|
| The NPC record, life stages, fertility, mortality, the personality trait table | [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| Partnership, who the partners are, households, rearing, kin roles, what an NPC *believes* about kin | [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) |
| Instinct priors and learned weights | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| Cultural traits and teaching | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| Property passed on at death | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |
| Hamilton's rule, relatedness values, parent-offspring conflict (source research) | [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) section C |

## 2. Principles
- **Genes are numbers, not labels.** There are no genes "for Hawk" or "for Grudger". A gene shifts a body trait or a predisposition by a small amount. Strategies still emerge from circumstance, learning and decisions.
- **No fitness function.** Nothing in the sim scores a genome. Selection happens only because bodies with some traits survive, eat, partner and raise children more often in the situations they meet. "Reproductive success" exists only as a measure computed afterwards from events.
- **NPCs cannot see genes.** They see bodies and behaviour. Kinship is perceived only through cues (section 4.6).
- **The four inheritances never share data** (section 4.8).
- **Deterministic and cheap.** Integers only. Every random draw comes from a forked stream keyed by the child's id. 600 living NPCs cost about 77 KB of genome data.
- **The genome is read in few places.** Only the phenotype builder, reproduction and the genetics metrics read allele values. Everything else reads the phenotype. A test enforces this (section 10).

## 3. Data structures (`Sim/Population/Genetics`)

```csharp
public sealed class Genome                    // immutable after conception
{
    public const int Loci = 64, Chromosomes = 8, LociPerChromosome = 8;
    public byte[] Alleles = new byte[Loci * 2];  // [2*l] = maternal copy, [2*l+1] = paternal copy; 0..255
}

public struct LocusDef        // one row of the gene map; data, saved on SimTuning
{
    public byte Trait;                 // index into TraitDef
    public byte Weight;                // 1..4, weight of this locus inside its trait
    public ushort DominancePermille;   // 500 additive; 1000 higher allele dominant; 0 lower allele dominant
    public ushort DownBiasPermille;    // chance a mutation goes down; 500 = unbiased
}

public struct TraitDef        // fixed table in the gene map
{
    public byte Id; public string Name; public TraitGroup Group;  // Physical, Appearance, Personality, Instinct, Learning
    public byte[] LocusIds;            // 1..3 loci
    public short DevNoise;             // half-width of the non-genetic noise term (section 4.3)
}

public sealed class GeneticsTuning    // inside SimTuning
{
    public int MutationPer10k = 40;          // per allele copy, per gamete
    public int MutationStepMax = 24;         // step is 1..MutationStepMax
    public int CrossoverPerGapPermille = 150;// between neighbouring loci on a chromosome
    public int FounderSpread = 64;           // founder allele = sum of 4 draws NextRange(0, FounderSpread), mean ~126
    public int InbreedingCheckDepth = 4;     // generations of pedigree searched
}

public struct PedigreeRecord  // kept for every NPC ever born or arrived (16 bytes)
{
    public int Id, Mother, Father;     // 0 = founder or immigrant: treated as unrelated to everyone
    public int BornTick;               // death tick lives on the NPC log
}

public struct Phenotype       // derived, stored on the Npc, all integers
{
    public short Strength, Endurance, Size, Health, Fertility, Lifespan, Perception; // 0..1000
    public short Features, Complexion, Build;                          // appearance, 0..1000
    public sbyte[] Personality;                                        // 12 traits, -100..100
    public short HungerGain, FearGain, SocialGain, NurtureGain;        // instinct strengths, 500..1500 permille
    public short LearnRate, ForgetRate, Curiosity;                     // 500..1500 permille
}
```

**The 64 loci** (the default gene map; scenarios can replace it):

| Group | Traits | Loci per trait | Loci |
|---|---|---|---|
| Physical | Strength, Endurance, Size, Health, Fertility, Lifespan, Perception | 3 | 21 |
| Appearance | Features, Complexion (visible, no mechanical effect), Build (also drives Size, see 4.3) | 2, 2, 2 | 6 |
| Personality | the 12 traits in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#personality) | 2 | 24 |
| Instincts | Hunger, Fear, Social, Nurture gains | 1 | 4 |
| Learning | LearnRate, ForgetRate, Curiosity | 3 | 9 |
| | | | **64** |

Chromosome *c* holds loci 8c..8c+7. Traits are spread across chromosomes so that a trait's loci are only partly linked.

## 4. Mechanics

### 4.1 Genes to trait (genotypic value)
For each locus the two alleles `a`, `b` give `lo = min`, `hi = max`:

`effect = lo + (hi - lo) * DominancePermille / 1000`

For each trait: `G = sum(Weight * effect) / sum(Weight)`, in 0..255, then `GP = G * 1000 / 255` (permille).

- **Additive loci** (dominance 500) make the child's value the mean of its alleles. Most loci are additive.
- **Dominance rows** are used where a trait should have recessive weak alleles. Health, Fertility and Lifespan loci default to 750 (the higher allele mostly masks the lower one). This is what lets inbreeding hurt (4.7) without a special rule.
- **Genotypic value is deterministic.** Anything random sits in the next step.

### 4.2 Founder genomes
- Starting NPCs and immigrants get alleles from the founder distribution: each allele is the sum of 4 draws of `NextRange(0, FounderSpread)`, so mean about 126, standard deviation about 37.
- Stream: `Rng.Fork("founder", npcId)`, locus 0 to 63, maternal copy then paternal copy.
- Founders have no pedigree parents, so they are unrelated to each other. That is an approximation: a real founding band is slightly related. Noted in section 11.
- Immigrants use the same distribution unless a scenario sets a different mean for the "other lands", which gives population structure to measure (section 4.9).

### 4.3 Phenotype: genes, plus environment, plus noise
The phenotype is built twice, from the same genome:
- **At birth:** personality, instincts, learning, appearance. These are fixed for life.
- **At adulthood (year 1.0):** the physical traits are fixed once.

Each trait is `clamp(GP + env + noise)`:
- `noise` is `DevNoise` times a Gaussian-like draw (sum of 4 integer draws, centred), from `Rng.Fork("develop", npcId)`. Separate from the conception stream, so changing noise never shifts genomes.
- `env` applies to Size, Strength and Health only: a childhood nutrition factor, `ChildNutritionAvg` (mean of Body.Nutrition over childhood, 0..1000), scaled to -150..0 permille. A starved childhood gives a smaller, weaker, less healthy adult with the same genes. This is why heritability is below 1.
- Size is `(GP_Size + GP_Build) / 2`, so that Build, which is visible, correlates with size without being a perfect cue.

Default `DevNoise` gives the following expected **narrow-sense heritability** in a population with normal nutrition variation:

| Group | Expected h² |
|---|---|
| Appearance | 0.80 |
| Physical (Strength, Size, Endurance, Perception) | 0.50–0.65 |
| Health, Fertility | 0.35–0.50 |
| Lifespan | 0.30–0.40 |
| Personality | 0.40–0.55 |
| Instincts, learning | 0.40–0.55 |

### 4.4 What each trait does (the only places the phenotype is used)

| Trait | Effect in the sim |
|---|---|
| Strength | Carry capacity, heavy-work speed, damage dealt in a fight |
| Endurance | Fatigue rate (±25%), walking stamina |
| Size | Hunger rate (±20%), reach and carrying; larger bodies cost more food (a trade-off, not a bonus) |
| Health | Disease resistance, injury recovery, infant survival |
| Fertility | Conception chance and the end of the fertile span (see [POPULATION_DESIGN.md](POPULATION_DESIGN.md#fertility)) |
| Lifespan | Personal natural death age, 3.5–4.5 years (see POPULATION_DESIGN) |
| Perception | Sight radius multiplier 850–1150 permille |
| Features, Complexion, Build | Visible only. No mechanical effect. Cues for resemblance (4.6) |
| Personality (12) | The biases in the decider |
| Instinct gains | Strength of the instinct priors the learning core starts from |
| LearnRate, ForgetRate, Curiosity | Speed of weight updates, speed of forgetting, size of the random exploration term |

Costs exist because bodies use food, not because a rule penalises a trait. Strength, Size and Perception have food or fatigue costs. A trait with no cost would simply drift upwards, so any new trait needs a stated cost or benefit in a real mechanic.

### 4.5 Reproduction of the genome
When a pregnancy ends (see [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#reproduction-decisions)) the sim creates the child:

1. `rng = Rng.Fork("conceive", childId)`. The child id comes from the global counter, ascending, in mother-id order within a day.
2. `mat = Gamete(mother.Genome, rng)`, then `pat = Gamete(bioFather.Genome, rng)`. The biological father id was recorded in the pregnancy at conception, and may differ from the mother's partner (see HOUSEHOLDS, paternity).
3. `sex = rng.NextInt(2)`.
4. Child genome: `Alleles[2l] = mat[l]`, `Alleles[2l+1] = pat[l]`.
5. A `PedigreeRecord` is written. Phenotype (birth part) is built from `Fork("develop", childId)`.

**Gamete(g, rng):**
```text
for c in 0..7:                      // chromosomes ascending
    copy = rng.NextInt(2)           // which parental copy to start on
    for i in 0..7:                  // loci ascending
        if i > 0 and rng.Chance(CrossoverPerGapPermille, 1000): copy ^= 1
        a = g.Alleles[2*(8c+i) + copy]
        if rng.Chance(MutationPer10k, 10000):
            down = rng.Chance(locus.DownBiasPermille, 1000)
            step = 1 + rng.NextInt(MutationStepMax)
            a = clamp(a + (down ? -step : step), 0, 255)
        gamete[8c+i] = a
```
- **Recombination:** crossovers at 150 permille per gap give about 1 per chromosome. Chromosomes assort independently.
- **Mutation:** 40 per 10,000 per allele gives about 0.26 mutations per gamete, so about half of all children carry a new variant. Fast enough to see over 10 generations, slow enough not to be noise. It is **deliberately accelerated** compared with nature (owner decision Q4, 27 Nov). `MutationPer10k` is scenario tuning (`tuning.Genetics.MutationPer10k`); zero and any lower rate are supported for controlled experiments, and the rate is reported with every experiment that uses it.
- **A scenario can switch the genome off** (`GameSetup.Genetics = Fixed`): every child copies a fixed clone genome. Useful for tests that isolate other systems.

### 4.6 Kin relatedness: truth, and what NPCs can know

**True relatedness r (for measurement and tests only).** From the pedigree, using the kinship coefficient `phi` (r = 2 × phi):
- `phi(a, a) = (1 + F_a) / 2`, where `F_a = phi(mother_a, father_a)` is the inbreeding coefficient.
- For `a != b` with `a.Id > b.Id` (the younger one first): `phi(a, b) = (phi(mother_a, b) + phi(father_a, b)) / 2`.
- Founders and immigrants have `phi = 0` to everyone else.
- The search stops at `InbreedingCheckDepth` generations, memoised per query batch, with a bounded cache of 4096 pairs cleared daily.
- Integer form: `phi` is stored in units of 1/1024.

Expected values with no inbreeding (the unit tests pin them):

| Pair | r |
|---|---|
| Self | 1.0 |
| Parent and child, full siblings | 0.5 |
| Half siblings, uncle or aunt and nephew or niece, grandparent | 0.25 |
| First cousins | 0.125 |
| Second cousins (needs depth 4) | 0.03125 |

**Who reads true r:** only `Telemetry`, `Lab` and tests. A reflection test makes sure no `INpcPolicy`, `Mind`, `Social` or `Learning` type references the pedigree.

**What an NPC perceives (cues).** Hamilton's rule needs relatedness, but an NPC only has an *estimate*, built in `Mind` from cues (see the rules for kin beliefs in [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#the-kinship-graph-believed)):

| Cue | Source | Reliability |
|---|---|---|
| Co-residence | Growing up in the same household, same hut | Good for siblings and parents; wrong for foster children and unrelated hut-mates (the cuckoo case in the research notes) |
| Caregiving | Who fed and carried me, who I fed | Mothers know their own newborn almost surely; fathers less |
| Resemblance | `Resemblance(a, b)` over the visible traits (below) | Weak; AUC about 0.65 |
| Being told | A message "this is my brother", from a gesture or gossip | Can be a lie or a mistake |

`Resemblance(a, b) = 1000 - 500 * (|Δ Features| + |Δ Complexion| + |Δ Build|) / (3 * VisibleSpread)`, clamped, where `VisibleSpread` is a constant from the gene map, never from the true population. It is a score, not a belief; a belief needs more evidence.

### 4.7 Inbreeding (optional in M4, on by default if tests pass)
- **No penalty rule.** Inbreeding depression arises from the dominance table: close relatives share alleles, so more children are homozygous at loci where the low allele is partly recessive (Health, Fertility, Lifespan). The same applies to mutations biased downward (`DownBiasPermille` 600 on those loci).
- **Observable:** the metric "mean Health phenotype by `F`" shows the effect.
- **Avoidance is not scripted.** NPCs may avoid partners they believe are close kin, because of an instinct (Nurture and household cues) or a learned reaction; culture can add a taboo ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)). Whether they do is a result.

### 4.8 Four inheritances, separate in code and data

| Kind | Data | Passed on by | Changed during life? |
|---|---|---|---|
| **Biological** | `Npc.Genome` (alleles), derived `Npc.Phenotype` | Gametes at conception (4.5) | No: genome is immutable. Phenotype changes only by growth and ageing |
| **Individual lifetime learning** | the `AssociationStore` in `Mind`, skills | **Never** (not in any gamete) | Yes. See [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| **Cultural** | `Culture` records (traits, memes, dialect) | Teaching, imitation, communication | Yes. See [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| **Material** | `Economy` ownership records, `Estate` | Customs and wills at death | Yes. See [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |

Rules that keep them apart:
- `Gamete` takes a `Genome` and an `Rng`, and nothing else. No `Mind`, skills, culture or goods can reach it. This rules out inherited acquired traits.
- The learning core *starts* each life from instinct priors whose **strengths** come from the phenotype (instinct gains), with all learned weights at zero. The genome shapes how a mind starts, not what it has learned.
- A child's culture starts empty and fills from its household and neighbours.
- A child owns nothing at birth. Goods come only through the estate and gift systems.
- Mutual effects are real but go through the world: well-fed, taught children grow into different adults (via `env` and learning), and rich parents feed children better. None of this edits a genome.

### 4.9 Population-genetics metrics
Read-only code in `Telemetry` computes, per faction and for the whole island, once per sim year and at each generation boundary (cohorts by birth year):
- **Trait means and variances** of the genotypic value and the phenotype, for every trait, over living adults.
- **Allele-class frequencies:** each allele's 0..255 value is put in 4 classes of 64. 64 loci × 4 classes of counts.
- **Expected heterozygosity** per locus: `1 - sum(freq²)` over the 4 classes, then the mean.
- **Mean inbreeding coefficient `F`**, and the share of children with `F > 0.0625`.
- **Population differentiation** between factions (a simple `Fst`: variance of class frequencies between factions over total), which rises when factions stay apart and falls with migration.
- **Realised reproductive success** per NPC: children who survived to year 1, from the event stream. Used for selection analysis, never fed back.
- **Selection differential** per trait per generation: mean trait of parents (weighted by children) minus mean of the whole cohort.
- **Realised heritability** by regressing offspring on mid-parent values.

## 5. Interactions with other systems
- **Population:** `Npc.Genome`, `Npc.Phenotype`, birth (section 4.5), maturation. See [POPULATION_DESIGN.md](POPULATION_DESIGN.md).
- **Households and kinship:** conception needs partners and a decision; the pedigree and cues feed kin beliefs and Hamilton-style weighting of relatives' welfare; foster children carry no true relatedness. See [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md).
- **Cognition and learning:** personality biases, instinct strengths and learning rates come from the phenotype. See [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) and [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md).
- **Social memory:** a belief about kin is a relationship record with a source; see [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md).
- **Estate:** deaths hand over goods by custom, never by genes (MATERIAL_INHERITANCE_DESIGN.md).
- **Lab:** the maths lab can use relatedness tables directly for the kin-selection examples ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)).
- **Recording:** genetics events and snapshots (section 7); see [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).

## 6. Player and view
- **Inspector card** (the NPC's own view plus a small "body" tab): shows the *visible* body (size, build, features) and the personality tendencies as words ("bold", "patient"), not allele numbers.
- **Kinship shown to the player** comes from the faction's records (births and households as the institutions know them, layer 7), at first almost complete. The true pedigree is a debug view only, labelled as such.
- **Population panel:** a chart of trait means by generation (from the genetics snapshot), and a pyramid in POPULATION_DESIGN.
- Faces and bodies vary visibly by Features, Complexion and Build. Animations decide nothing.

## 7. Telemetry
Domain events:
- `NpcBorn` carries `Mother`, `BioFather`, `Sex`, a 64-bit genome hash, and the birth phenotype.
- `NpcMatured` carries the adult physical phenotype.
- `GeneMutated` (full recording level only): npc, locus, old, new.
- `NpcDied` has the cause (see POPULATION_DESIGN).

Snapshots:
- `GeneticsYearly`: per faction and island, per trait mean and variance (genotypic and phenotypic), 64 × 4 allele-class counts, mean `F`, `Fst`.
- A pedigree table (`PedigreeRecord`) is exported in full at the end of a run and at each save.

Recording never changes the hash of a run.

## 8. Stage 4 minimum vs later

| Stage 4 (M4) | Later |
|---|---|
| 64 loci, 8 chromosomes, diploid byte alleles, additive and dominance rows | Sex-linked loci; twins and multiple births (twins carry r = 1 for identical, 0.5 otherwise) |
| Recombination and mutation with forked seeded streams | Mutation rate variation; a mutator gene |
| Phenotype at birth and adulthood, childhood nutrition effect | Pleiotropy (one locus, two traits), for real trade-offs such as fertility against lifespan |
| Pedigree, true `r` for measurement, resemblance cue | Allele identity-by-descent tracking, for genomic relatedness |
| Inbreeding depression from the dominance table | Disease-resistance genes that co-evolve with diseases (Stage 6 healthcare) |
| Yearly genetics snapshot, metrics, tests | Genetic structure of factions after long migration (M10 onwards) |
| Four-inheritance separation tests | Gene-culture interaction measures ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md), M11) |

## 9. Migration from the existing code
- No genome exists today. `Rng` has `Fork(string label)` only. M0 adds `Fork(string label, int id)` (the form conventions section 3 already uses: `Rng.Fork("npc", id)`), by hashing the label and the id. This document only needs that overload.
- `GameSetup` gains `Genetics` (`On` default, or `Fixed`) and `SimTuning.Genetics`.
- Save version 3 stores genomes of living NPCs (128 bytes each) and all pedigree records of anyone with a living relative within 4 generations; others are pruned.
- `StateHash` covers every genome and pedigree record.

## 10. Tests and acceptance criteria
All run in `DotNet/` and Unity EditMode. Statistical tests use fixed seeds and the tolerances stated, which are about 3 standard errors.

| Test | Setup | Expected |
|---|---|---|
| `Gamete_Deterministic` | same genome and stream twice | identical gamete; different stream gives different gamete |
| `Mutation_RateHonoured` | 100,000 allele copies, `MutationPer10k = 40` (count mutation events inside `Gamete`, not value changes) | 400 ± 60. At 0 exactly 0. At 10: 100 ± 30 (a lower rate is honoured) |
| `Mutation_ScenarioOverride` | Scenario files with `tuning.Genetics.MutationPer10k` 0, 10 and 40 | each run uses its own value (read back from the saved `SimTuning`); two runs with different rates in parallel do not leak into each other |
| `Mutation_BiasHonoured` | `DownBiasPermille = 800`, 2,000 mutations | share of downward steps 0.80 ± 0.04 |
| `Recombination_Rate` | 20,000 gametes | crossovers per chromosome 1.05 ± 0.08; each parental allele appears in a gamete 50% ± 1% |
| `Offspring_BetweenParents` | 2,000 families, additive loci, mutation off, noise 0 | mean (child − mid-parent) within ± 5 permille; child variance about mid-parent equals 0.5 × population genotypic variance, within [0.42, 0.58] |
| `Heritability_Genotypic` | same, regress child genotypic value on mid-parent | slope 1.0 ± 0.07 |
| `Heritability_Phenotypic` | noise set so noise variance equals genetic variance | slope 0.50 ± 0.10 (standard error about 0.03 with 2,000 families) |
| `Heritability_ByGroup` | default tuning, 3,000 families | each group inside the section 4.3 table range |
| `Dominance_LowersSlope` | all dominance 1000, otherwise as above | narrow-sense slope at least 0.05 below the additive run |
| `Relatedness_Pedigree` | hand-made pedigree | values exactly as the table in 4.6; offspring of full siblings has `F` = 0.25 |
| `Relatedness_NotVisibleToNpcs` | reflection over `Policy`, `Cognition`, `Learning`, `Social` types | none references `PedigreeRecord`, `Genome` or the `r` function |
| `Cue_ResemblanceInformative` | 2,000 full-sibling pairs against 2,000 unrelated pairs | probability that the sibling pair scores higher (AUC) between 0.60 and 0.80 |
| `FourInheritances_Separate` | change a parent's learned weights, skills, culture and goods, same seed | child genome and phenotype hashes identical |
| `NoFitnessFunction` | reflection: only `PhenotypeBuilder`, `Reproduction`, `Genetics metrics` and tests reference allele data | pass |
| `Selection_Measured` | scarce-food scenario against a control, 20 paired seeds, 10 generations | the selection differential and the paired difference in mean Size genotypic value are computed for every generation and equal a recomputation from events and snapshots (exact); the size and sign of the difference are **reported** with an interval, never asserted. Food costs are not tuned to make a difference appear |
| `Inbreeding_Depression` | 300 sibling-mated against 300 unrelated births, Health | mean Health phenotype at least 15 permille lower (calibrate once and pin) |
| `Metrics_Match` | recompute trait mean and variance in the test | equals snapshot values exactly |
| `Replay_Deterministic` | 10 generations, same seed twice, and with recording on and off | same state hash and same pedigree hash |

## 11. Open questions
No open questions remain.

**Owner decision Q4 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):** keep the default of 40 per 10,000 allele copies (about 0.26 new variants per gamete), configurable in scenario tuning. It is deliberately accelerated so that change is visible within 10 generations. Zero and lower rates are also supported for controlled experiments (`Mutation_RateHonoured`, `Mutation_ScenarioOverride`). The rate is per gamete, so it is already per generation and does not follow `LifeScale`. Biological inheritance stays distinct from learning and culture ([section 4.8](#48-four-inheritances-separate-in-code-and-data)).

Settled here, with defaults:
- Founders are unrelated to each other (`phi = 0`). A shared ancestor pool is a later scenario option.
- Physical traits are fixed at year 1. Later change comes only through `Injury`, `Health` and elder decline ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#ageing-and-old-age)), not through a drift of the phenotype.
- Appearance traits stay free of mechanical effects (no green-beard shortcut). A later rule that needs them must be a stated design change here.
