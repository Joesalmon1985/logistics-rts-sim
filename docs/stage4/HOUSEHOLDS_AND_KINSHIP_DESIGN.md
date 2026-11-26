# Households and kinship design (stage 4)

This document owns how NPCs pair up, live together, raise children, look after relatives and are connected by kin. Part of the life-cycle foundation (M4). Terms and numbers follow [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

## 1. Purpose and scope

**Owns:**
- partnership and courtship;
- forming and breaking households: shared huts, sleeping rights, the household store, pooling rules;
- reproduction decisions;
- child-rearing, care of elders, the sick and the injured, fostering, adoption and orphans;
- informal shared childcare between households (owner decision Q15);
- the kinship graph (what NPCs believe) and the names of kin roles;
- desertion and divorce;
- the household economy;
- household telemetry and the family view.

**Links, does not repeat:**

| Topic | Owner |
|---|---|
| Life stages, fertility, pregnancy, mortality, disease | [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| Genome, true relatedness `r`, resemblance | [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) |
| Learned weights, social learning | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| Teaching and culture (the hook is here, the content there) | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| Relationship records, trust, reputation | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| Estates and heirs | [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) |
| Property forms, jobs, barter | [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) |
| Book examples and payoffs | [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md), [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) sections C and D |

## 2. Principles
- **Everything here is a decision by NPCs.** Partnering, trying for a child, feeding, caring, fostering, leaving and expelling are intents chosen by the decider ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)), from beliefs, drives, personality, learned weights and culture. No rule makes a parent feed its child. The strong default comes from the Nurture instinct, the kin-weight trait and early learning, and it can fail.
- **Mechanics make outcomes possible; measures say whether they happened.** The costs and asymmetries are real (pregnancy, infant dependence, food), so strategies in the coy/fast, faithful/philanderer and guard/desert games *can* arise. None is scripted, and no payoff matrix is used on the island ([lab vs island](GAME_THEORY_DESIGN.md)). Section 4 ("Measuring family strategies") defines the measures.
- **NPCs perceive kin only through cues.** They use a believed kinship graph, never the true pedigree. A foster child can be treated as own, and a father can be wrong about paternity.
- **Same options for both sexes.** Both partners have the same intents. The sexes differ in bodies (pregnancy), not in rules. Any difference in behaviour is learned, chosen or inherited as a predisposition.
- **All partnerships are equal (owner decision Q6, 27 Nov).** Same-sex partnerships are explicitly supported, with the same social, emotional, household, economic, caregiving and material-inheritance capabilities as any other partnership. The only difference anywhere in the sim is that biological conception follows the reproductive model ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#fertility)). Adoption, fostering and caring for children are open to all suitable households, and no rule, score or KPI values one kind of partnership or household above another.
- **Relationships are voluntary (Q5).** Individuals enter and leave partnerships by their own decisions; no rule pairs NPCs or keeps them together.
- **Biological parents are not the only caregivers (Q15).** Any adult may feed, mind, tend or raise a child, inside or outside its household.
- **Future costs and benefits only.** The decider does not add sunk costs, so past investment does not by itself keep a partner in place (the Concorde point in the research notes).
- **Abstract and family-friendly.** Reproduction is a partnership, then a pregnancy period with a marker, then a child appears in the household. Nothing more is shown.
- **Deterministic.** All daily processing in ascending id order. Streams: `Rng.Fork("household", id)` and `Rng.Fork("npc", id)`.

## 3. Data structures (`Sim/Population/Households`)

```csharp
public sealed class Household
{
    public int Id, Faction, Hut;                  // Hut = building id, 0 if none
    public int FormedTick;
    public int[] Members = new int[12];           // sorted NpcIds, up to 12 (MemberCount tracks length)
    public ItemStack[] Store = new ItemStack[8];  // the household store
    public Norms Rules;                           // pooling rules (below)
    public int LastRuleChangeTick;
}

public struct Norms                               // owned by the members; permille 0..1000
{
    public short ChildrenFirst;       // share of food in short supply reserved for children and the sick
    public short ContributionExpect;  // share of own foraged food a working member is expected to put in the store
    public byte  StoreAccess;         // 0 adults only, 1 all members, 2 contributors only
    public short KinHouseholdGifts;   // share of a surplus expected to go to kin households
}

public struct SleepRight { public int Npc, Hut; public byte Source; } // Built, Household, Invited, Rented

public struct Partnership                         // key: pair (A < B). An NPC may hold several Partnership records;
                                                  // how many may be live at once is SimTuning.Households.MaxLivePartnerships
                                                  // (1 in M4: serial). Nothing else assumes one partner (owner decision Q5).
{
    public int A, B, StartTick, Stage;            // Stage: Courting, Partnered, Parenting, Separated
    public int GiftsAToB, GiftsBToA, HelpAToB, HelpBToA; // value and hours, for the record
}

public struct CareArrangement                     // informal shared childcare between households (Q15); up to 4 per household
{
    public int ChildId, CarerId, CarerHousehold;  // the carer is any adult outside the child's household
    public int AgreedTick, LastCareTick;
    public byte Terms;                            // Reciprocal (turns), ForGoods, Unpaid (favour), as agreed by gesture
    public int CareTicksGiven, CareTicksReturned; // the record each side keeps in social memory; nothing enforces it
}

public struct KinBelief                           // 16 B padded; one per (NPC, other); up to 32 per NPC in Mind (512 B)
{
    public int Other; public byte Role; public short Confidence; // 0..1000
    public byte Source; public int Tick;          // Born, Raised, Told, Seen, Resemblance
}
```

**Kin roles** (the names used by the sim, the HUD and the data):

| Group | Roles |
|---|---|
| Direct | Mother, Father, Daughter, Son |
| Siblings | FullSibling, HalfSibling (as believed) |
| Wider | Grandparent, Grandchild, Uncle, Aunt, Nephew, Niece, Cousin |
| By partnership | Partner, InLaw (a partner's close kin), StepParent, StepChild |
| By fostering | FosterParent, FosterChild |
| Household only | HutMate (no kin belief) |

Roles describe what the NPC **believes**. They are the only kinship an NPC sees.

**The two graphs:**
- **True pedigree:** `PedigreeRecord` in [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md). Telemetry, tests and the lab only.
- **Believed kin graph:** `Mind.Kin` (the `KinBelief` list), built from cues. It can be wrong or incomplete, and it is what drives behaviour. Capacity is 32; the lowest `Confidence × weight of bond` is dropped first.

`Npc.Household` (id or 0) and `HomeHut` stay on the NPC record ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).

## 4. Mechanics

### Partnership and courtship
- **When it starts.** From adulthood (1.0), the *desire for a partner* drive rises with time alone ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#drives)) and makes candidates salient.
- **Choosing.** The decider considers adults the NPC knows (or has seen) who are not believed close kin (`FullSibling`, `Parent`, `Child` roles with confidence above 400 are not scored; a different custom can change this). Scoring uses:
  - visible body and appearance, and observed behaviour (does it share food, work hard, have a hut or a good household);
  - social memory: trust, reputation, a past partnership;
  - the NPC's own traits (Patience, Trustfulness, Honesty), learned weights and culture;
  - how many partners are available (a small island has few).
  There is no sex rule in choosing, courting, partnering, sharing a household or raising children. Only conception needs a woman and a man ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#fertility)). Same-sex partnerships are explicitly supported (owner decision Q6) and have every other capability of a partnership: the same courtship acts and social-memory records, a shared household and store, sleeping rights, care of each other, the `Partner` kin role and its place in estate customs ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)), adoption and fostering (below).
- **Courtship acts** are ordinary intents, available to both:
  - **gifts** of food or goods (a hand-over, possibly refused, see [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md));
  - **help:** doing some of the other's work;
  - **time:** staying near, gestures of invitation and approval (the gesture set in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#49-gesture-language-simcomms));
  - **showing:** presenting its hut or store.
  Each act is recorded in the relationship record ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)) and in the `Partnership` totals.
- **Accepting.** NPCs decide when to move from courting to partnership (shared household), and later when to *try for a child*. A patient NPC may ask for more before saying yes. The amount asked is not a parameter: it comes from traits, learned experience (earlier partners who left) and gossip.
- **Partnership.** It is formed only when both choose it, and either partner may end it by its own decision (section "Desertion and divorce"): entering and leaving are always voluntary. Stage becomes `Partnered` and, if the partners share a household, the household changes (see below).
- **How many at once (owner decision Q5).** In M4 an NPC holds at most one live partnership at a time (`SimTuning.Households.MaxLivePartnerships` = 1, serial partnerships). The architecture does **not** fix this: partnerships are pair records, households are member lists, kin roles and estate candidates are lists, and the decider scores each candidate partnership on its own. Raising the cap, or letting a household custom set it ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)), allows concurrent partnerships and other household arrangements later without a data change. Code that reads "the partner" uses the list (`PartnersOf(npc)`), never a single field.
- **What can arise (not scripted).** The book's models need a long, costly courtship to filter out those who will leave, and the males-and-females asymmetry in investment ([research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) D2–D4). Here the investment asymmetry is physical (pregnancy and infant dependence, see below). Whether some NPCs become "coy" or "fast", "faithful" or "philanderer", is measured.
- **Measuring family strategies** (computed afterwards from events; never fed back to NPCs):

  | Measure | Definition |
  |---|---|
  | Courtship length | Days from first `CourtshipAct` to the first `TryForChildChosen` with that partner |
  | Courtship investment | Value of gifts plus help-hours given before that point |
  | Desertion lag | Days from a birth to the father or mother leaving the household (`HouseholdLeft` reason `Deserted`) |
  | Stayed through year 1 | Both partners are still in the child's household when it matures |
  | Guard or desert | Per birth, per parent: stayed (guard) or left (desert) within 0.5 years. The four combinations are the names used for the Maynard Smith cases: both stay, only the mother stays, only the father stays, neither |
  | Partner turnover | Partnerships per adult-year |

  A "coy" NPC is one whose courtship length is in the top quartile for the cohort and a "philanderer" is one with desertion lag under 0.5 years and a new partnership within 0.5 years. These cut-offs are for reports only.

### Forming and breaking households
A household is a group of NPCs who sleep in the same hut and share a store. An NPC is in at most one household.

**Forming (events `HouseholdJoined`, with a reason):**

| Reason | When |
|---|---|
| `Founded` | One or more NPCs without a bed build a hut together: each builder who gives labour or materials gets a sleeping right (communal ownership, recorded per hut in [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#property)). A small hut has 3 beds (a child counts as half a bed unit) |
| `Partnered` | Partners decide where to live. One moves into the other's household, or both build or take a new hut |
| `Born` | A newborn joins its mother's household |
| `Fostered` | An orphan or abandoned child is taken in (below) |
| `Invited` | A member invites a relative, friend, elder or the sick |
| `Reunited` | A former member returns |

- **Hut building** is unchanged from earlier designs: an NPC without a bed looks for a hut with room; if none is known it plans to build one from logs (clay later), alone or with others. The state can build public housing and rent beds ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **Space limits are real.** A full hut cannot take another member. Households decide to add a bed or build another hut for a growing family, or send someone out. Crowding raises disease risk ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#disease-simple-model-in-stage-4-healthcare-later)).

**Breaking (events `HouseholdLeft`, with a reason):**

| Reason | When |
|---|---|
| `Died` | Death of the member |
| `LeftHome` | An adult child decides to set up on its own, usually with a partner |
| `Separated` | Mutual end of a partnership (divorce, below) |
| `Deserted` | One leaves without the other's agreement |
| `Expelled` | The members decide to turn someone out (for example a non-contributor) |
| `FosteredOut` | A child moves to another household |
| `Emigrated` | The NPC leaves the island or the faction |
| `Captured` | Taken by another faction ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) |

A household with no members is dissolved (`HouseholdDissolved`). Its hut and store pass through the estate system.

**Sleeping rights.** Each right is a record `(Npc, Hut, Source)`. A right stays with the NPC when it leaves a household unless a custom or the hut's other rights-holders say otherwise. If it leaves without taking the right, the right lapses after 2 years of not sleeping there. Who keeps a hut after a split is decided by who holds rights (and, in conflict, by the usual disputes mechanics; later by elders and mediation, M12).

**The household store and pooling rules.**
- **The store** is a small stock of 8 stacks in the hut. Members deposit (a `Contribute` intent) and withdraw (eating or taking for others).
- **The pooling rules** (`Norms`) are decided by the members, not set by the sim:
  - at formation, the starting values are the mean of the founding adults' norm beliefs (a cultural trait, see [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)); the fallback is `ChildrenFirst` 700, `ContributionExpect` 300, `StoreAccess` 1 (all members), `KinHouseholdGifts` 100;
  - any adult member may propose a change by gesture and speech-less request. The change is adopted if more than half of the adults agree within 2 days (each decides for itself). Children do not vote.
- **Norms are not enforced by code.** A member can take more than its share or contribute less. Others notice only if they see it or remember the store level, and they respond with intents (a warning gesture, a reduced share, an expulsion proposal). That is how free-riding, and answers to it, can arise; whether they do is measured.

### Reproduction decisions
See [POPULATION_DESIGN.md](POPULATION_DESIGN.md#fertility) for the conditions on conception (fertile woman, partner, both trying, nutrition). The decision is here.
- **Intents:** `TryForChild(partner)` and `StopTrying`, available to any adult in a partnership. A conception needs both to hold `TryForChild`.
- **Inputs (all from the NPC's own beliefs and body):**
  - household food, as the NPC believes it (days of food in the store, the recent yield);
  - free beds and the number of dependants in the household;
  - its own and the partner's health and age (a woman near the end of her fertile span, a sick partner);
  - trust in the partner, the partner's investment so far, and any sign that it might leave (past desertions, gossip);
  - Patience, Kin weight and the Nurture instinct;
  - learned weights (earlier children who lived or died), and culture (norms about family size);
  - for the mother, the cost of pregnancy: higher need for food, slower work, a small risk of death.
- **Result.** Both sexes pay costs in the same units (food, work time) but pregnancy puts a larger and earlier cost on one body. Whether partners agree is the outcome of their own scoring; mismatches simply mean no conception.
- **Biological father.** In M4 the biological father is the partner at conception (the record is `Pregnancy.BioFather`). If the partnership ends during the pregnancy, the child still has the same biological father, but the household and the believed father may differ. Concurrent partners and extra-pair conception are later, and make paternity uncertainty common.
- **After conception** the pregnancy period (0.25 years) is shown as a marker on the mother. The child appears in the household when it ends ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#birth)).

### Child-rearing
- **Kin beliefs at birth.** The mother believes the newborn is her child (confidence 950: she saw the birth). The believed father gets `Father` at a confidence built from partnership, co-residence and resemblance (typically 600–900), never from the pedigree. Siblings in the household are believed `FullSibling` or `HalfSibling` from the parents' history; the child's own beliefs form from who raises it.
- **Feeding.**
  - A child's hunger is signalled by **begging**: a gesture with an intensity of 0–3 chosen by the child (not by its true hunger). Intensity costs a little energy and draws attention. How loud to beg is learned: it starts from the instinct and is tuned by what gets the child fed.
  - Any member, parent or not, may respond with `Feed(child)` from its own food or from the store. Each decides from: the child's visible condition and gesture, its own hunger, the believed relatedness `r̂` times its Kin weight (Hamilton), competing needs (other children, a sick partner), the household norms, and learned weights.
  - Infants (under 0.25 years) must have an adult carer present (below) and can be fed by any member.
- **Protection.** An unattended child is exposed to world hazards: Accident or Exposure with a daily chance of 1 in 300 under 0.25 years, 1 in 1000 from 0.25 to 0.5, and 1 in 5000 after that when no adult is within 3 nodes. A carer may carry (see view) or keep an infant close, at the cost of lost work time. This is a physical fact, not a rule on parents; it is what makes a deserted partner's position hard.
- **Weaning and conflict (can arise).** The book's parent-offspring conflict ([research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) C5) needs limited food, begging, hidden true hunger and sibling relatedness. All four exist: parents decide how much to give; a child can beg harder than its hunger justifies; the parent sees only the gesture and the child's body; and siblings compete in the same store. Weaning, over-begging and favouritism are therefore **measurable, not designed**. A parent that cuts off feeding at 0.7 years does so because its scores say so.
- **Teaching hook.**
  - An adult working with a child near (within 2 nodes) emits an `ActivityObservable` event that the learning system can use for **social learning** by the child ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
  - An adult may choose `Teach(child, skill)`: slower, demonstrative work at 70% output. The effect and what is taught are in [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md). This document only provides the opportunity and the cost.
  - Habits, gestures and norms are learned the same way, so a child inherits its household's culture without any genetic link.
- **Growing up.** From 0.5 years a child does light work if its household and it decide. At 1.0 it becomes an adult and may leave (`LeftHome`) or stay.

### Care of elders, the sick and the injured
- **Carer intents:** `Feed(patient)`, `FetchFood`, `Tend(patient)` (rest and watch), `Carry` (move an injured member), `Mind(child)`.
- **Effects:** tending an ill or injured member reduces illness severity by 30% and doubles injury recovery ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#mortality)). The carer loses work time and the food it gives.
- **Decision:** scores come from `r̂ × Kin weight`, partnership bond, obligations owed (the patient helped before), learned experience, the patient's usefulness (an elder who minds children), and the cost in food and time. Nothing forces care.
- **Elders:** a household may keep an elder, who still contributes by minding children, teaching and light work. An elder with no kin and no friends may be neglected and may die of starvation or illness. Whether this happens is measured; later institutions (elders' welfare, shared store, M12) change it.
- **Sick children and newborns** are tended by the household in the same way, at higher priority through the `ChildrenFirst` norm.

### Fostering and orphans
- **Orphan:** a child (under 1.0) with no living believed caregiver in its household after a death or a desertion. `ChildOrphaned` is emitted.
- **Who may take it in.** Any adult can choose `Adopt(child)`: a couple of either sex or the same sex, a single adult, or a multi-adult household. Suitability is judged in the same way for every household (free beds, food, the adults' own care load and their decision), never by the kind of partnership (owner decision Q6). Likely candidates are kin who believe themselves related (by `r̂`), household or hut-mates who know the child, and neighbours moved by gossip. The decision weighs the child's need, `r̂ × Kin weight`, free beds and food, and the other children already there. The child also acts: it can seek a household (follow, beg, stay at the hut).
- **Spread of the request.** An orphan's plight spreads through gestures and gossip (a "needs a home" message), so more distant adults may learn of it. Institutions (elders, Band age, M12) later add mediation.
- **Foster relationship.** The child joins with reason `Fostered`. Believed role is `FosterParent` or, if the child is not told and cues say "ours", `Mother` or `Father` by cue. True relatedness is unchanged. That gap (treating co-resident unrelated children as kin) is the exploit the research notes describe for cuckoos, and its size can be measured.
- **If nobody takes the child** it survives only by begging and foraging, with the high hazards above. This also is measured, not prevented.
- **Estates:** an orphan's inherited goods are held by the new household ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).

### Shared childcare between households
**Owner decision Q15 (27 Nov):** informal shared childcare between households is supported in Stage 4; a formal institution (a nursery) can emerge in Stage 5 or later ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **What it is.** An adult minds, feeds or tends a child of another household (`Mind(child)`, `Feed(child)`, `Tend(child)` are already open to any adult). When two households do this repeatedly they may agree an arrangement by gesture (`SayOffer` with topic "mind child", accepted or refused), recorded as a `CareArrangement` with its terms: turns (I mind yours today, you mind mine tomorrow), for goods, or as an unpaid favour.
- **Why it can pay.** A carer can mind several children at once at a small extra cost, so parents in two households gain work time (the guillemot and crèche cases in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)). Kin, friends and neighbours are the likely partners, by `r̂`, trust and learned experience.
- **It can be exploited.** Nothing enforces the terms. A household can take turns and not return them, or a carer can neglect a minded child. The record each side keeps in social memory, and gossip, are the only answers in Stage 4, so cooperation, free-riding and retaliation are all possible and measured (`CareTicksGiven` against `CareTicksReturned` per arrangement).
- **Care work.** Minding children is care work: it costs the carer time and is reported as unpaid care unless paid in goods ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#11-open-questions)).
- **Events:** `CareArrangementMade(child, carer, terms)`, `CareArrangementEnded(reason)`; each act is a `CareGiven` with `kind = MindChild`.

### The kinship graph (believed)
- **Cues** (in order of strength; see [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md#46-kin-relatedness-truth-and-what-npcs-can-know)): seeing a birth, caring for or being cared for in childhood, co-residence, being told, resemblance.
- **Building and updating.** On each cue the NPC adds or updates a `KinBelief` for the other NPC. Confidence rises with repeated cues and falls when contradicted (a lie found out, a divorce). Beliefs on a third NPC's kin come from being told ("that is her brother").
- **Derived roles.** A role such as Uncle or Cousin is derived by walking the NPC's *own* beliefs two steps (my mother's brother). If a link is missing the role is unknown.
- **Estimated relatedness `r̂`.** From believed role: Parent, Child, FullSibling 500; HalfSibling, Grandparent, Uncle, Aunt, Nephew, Niece 250; Cousin 125; Partner, StepParent, StepChild, FosterChild (when known to be fostered) 0. Multiplied by `Confidence/1000`, and for roles with no direct belief, set from the resemblance score. Kin weighting in the decider is `KinWeight trait × r̂`: the relatedness values and Hamilton's rule are in [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) C1.
- **No access to the pedigree.** A reflection test checks that `Mind`, `Policy` and `Social` types never read `PedigreeRecord`.

### Inheritance hooks
- **Partners of any sex** are heir candidates in the same way ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)); so are adopted and fostered children who are acknowledged.
- **At a member's death,** the Estate module opens the estate and emits `EstateOpened` ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) is its only emitter). This document supplies the heir candidates: the *decedent's* believed kin (confidence above 400) and household members. The household store stays with the household unless it dissolves.
- **What is decided** (custom, will, partner first, eldest child, equal split) belongs to [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md). The kinship graph supplies the candidates, not the rule.
- **No biological or learned traits pass here.** See [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md#48-four-inheritances-separate-in-code-and-data).

### Desertion and divorce
- **Intents:** `EndPartnership(mutual)` and `Leave` (desertion). Both are decisions based on **future** costs and benefits only: the NPC's expected drive relief with and without the household, the partner's behaviour, availability of a better option, kin weight for the children, and learned weights. Sunk costs are not an input.
- **Divorce (mutual).** Both agree. The household splits: the hut goes to whoever holds the sleeping rights or, if both do, to whoever the partners agree on; the store is divided by the household's `ContributionExpect` norm; each child follows the caregiver it is most bonded to (a child's own decision, tied to the bond in its relationship records). A disagreement is a conflict handled by the usual intents, not by a rule.
- **Desertion.** One leaves. The remaining partner keeps the hut and the children and takes on the full rearing cost: this is the "cruel bind" in the notes, and here it can arise from the hazards for infants and the extra work. Witnesses and kin record the event in social memory, and gossip spreads it, so a deserter's later courtship is harder if others remember.
- **Reconciliation** is possible: a returning partner is a `Reunited` join, if accepted.
- **Institutions later.** Courts and enforced agreements about marriage, support and custody come with later ages ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)); Band-age elders may mediate (M12).

### Household economy
- **Shared production and consumption.** Members forage, build, haul and work (jobs from M6). They decide how much to **contribute** to the store and when to **take** from it. Food in the store is eaten at the hut; carried food is eaten on the spot or brought home.
- **Division of tasks can emerge.** Nobody is assigned. Skills grow with practice ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#skills)), so members who are good at a task tend to do it, and members may ask each other for help by gesture. Care for children and the sick reduces the time of those who give it, which shapes who forages. A measure, the **task concentration index** (Herfindahl share of work-hours by task per member), is reported by household ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) covers division of labour more widely).
- **Household surplus** can go to kin households (gifts), to barter, or to the state as tax. Gifts to kin follow `r̂`; this is where Hamilton's rule, reciprocity and the vampire-bat example overlap.
- **Wealth and dependence:** a household's goods are the base for later economic classes ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)).

## 5. Interactions with other systems
- **Population:** the pregnancy and birth steps, life stages, hazards, disease ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).
- **Biological inheritance:** `Pregnancy.BioFather`, the pedigree, resemblance ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)).
- **Cognition and learning:** intents decided in the decider; begging and feeding are learned through the reward signal ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md), [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Social memory:** courtship acts, desertions and care obligations are relationship records and gossip ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Culture:** norms about households, family size and teaching ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)).
- **Economy and estate:** stores, sleeping rights, gifts, heirs ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).
- **State and institutions:** public housing, welfare for orphans and elders, loyalty from household ties ([STATE_DESIGN.md](STATE_DESIGN.md), [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Game theory:** the measures above feed the experiments in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md); benchmark policies for coy/fast and guard/desert live in the lab ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)), separate from the island.

## 6. Player and view
- **Inspector card, household tab** (from the NPC's own view): its household members with the roles it *believes*, hut, the store level as it knows it, its partner, its dependants, and who it last fed or cared for.
- **Faction view** (through `FactionKnowledgeView`): households by size, children per household, orphans and elders without kin, partnership counts. Nearly complete at first, limited by statistics institutions later.
- **Kin lines** (an optional overlay): thin lines between believed kin of the selected NPC. A debug view can show the true pedigree, clearly labelled.
- **Animations** show events only:
  - **feeding a child:** the adult crouches and holds out a food item, the child reaches and eats; the child's **begging** is a reaching and tugging pose whose amplitude is the gesture intensity;
  - **embrace:** partners, or a parent and child, on reunion, a gift accepted or a birth;
  - **carry:** an adult lifts a small child onto a hip or an injured member onto a shoulder and walks;
  - **sit beside the sick** (kneeling, hand on the shoulder), **tuck in** at the hut, **teach demonstration** (slow, exaggerated work with a child watching), **grief** at a death;
  - a small **expecting** marker on the mother, and at birth a short "new arrival" effect in the hut.
- Reproduction is never depicted beyond the partnership, the marker and the new child.

## 7. Telemetry
Domain events (each stamped with a tick and ids):
- **Household membership** (important for the warehouse): `HouseholdFormed`, `HouseholdJoined(npc, household, reason, fromHousehold)`, `HouseholdLeft(npc, household, reason, toHousehold)`, `HouseholdDissolved`. Reasons are the enumerations above.
- **Partnership:** `CourtshipAct(giver, receiver, kind, value)`, `PartnershipFormed`, `PartnershipEnded(a, b, reason, initiator)`, `TryForChildChosen`, `TryForChildStopped`.
- **Shared childcare:** `CareArrangementMade`, `CareArrangementEnded`.
- **Children and care:** `ChildFed(feeder, child, amount, source)`, `BeggingMade(child, intensity, trueHungerBucket)` (the true hunger is recorded for analysis only), `CareGiven(carer, patient, kind)`, `ChildOrphaned`, `ChildFostered`, `ChildUnattendedHazard`.
- **Kin:** `KinBeliefFormed(npc, other, role, source, confidence)` (full level only). `EstateOpened` is emitted by Estate, not here.

Snapshots:
- **Daily household snapshot:** id, faction, hut, member count, children, elders, store value, norms, food days.
- **Person snapshot** adds household id, partner ids (a list, length 1 in M4), believed mother and father.
- **Membership history** is built in the warehouse as a slowly changing table (`npc, household, from_tick, to_tick, join_reason, leave_reason`) from `HouseholdJoined` and `HouseholdLeft`. Gaps (no household) are valid. See [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).
- **Derived KPIs** (see [KPI_CATALOGUE.md](KPI_CATALOGUE.md)): children fed share, orphans fostered within N days, household size, partner turnover, the family-strategy measures above.

Recording never changes the hash.

## 8. Stage 4 minimum vs later

| M4 (life cycle and households) | Later |
|---|---|
| Serial partnerships (`MaxLivePartnerships` 1), courtship acts, voluntary entry and exit; same-sex partnerships with every capability except conception; data structures that allow several partners | Concurrent partnerships (raise the cap or a household custom sets it), extra-pair conception, other household customs |
| Households with huts, sleeping rights, a store, simple norms decided by vote of adults | Compounds of several huts, extended-family households, rent and tenancy |
| `TryForChild` decision, pregnancy marker, birth into the mother's household | Twins, longer child dependence, apprenticeship |
| Feeding, begging, protection from hazards, teaching hook; informal shared childcare between households (`CareArrangement`) | A formal nursery institution (Stage 5 or later, if it emerges); schools (Town age) |
| Care of elders and the sick by household (feed, tend, carry) | Healers (M12, Band age), hospitals (Stage 6) |
| Fostering, adoption and orphans by individual decision and gossip, open to all suitable households | Welfare and mediation for orphans (M12 elders, Village-age welfare, Stage 5) |
| Believed kin graph with cues; `r̂` and Hamilton-style weighting | Learned kin terms in language; kinship taboos as culture (M11) |
| Desertion and divorce by decision; social-memory consequences | Courts, marriage law, custody rules (Town age) |
| Estate hook with heir candidates | Customs and wills (MATERIAL_INHERITANCE_DESIGN.md, M4 basic, M11 culture) |
| Telemetry, measures of family strategy | Policy experiments on family policies ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)) |

## 9. Migration from the existing code
- Nothing like this exists yet. The earlier design had huts with 3 beds and sleeping rights in [POPULATION_DESIGN.md](POPULATION_DESIGN.md); that text is now **here**, and POPULATION_DESIGN.md links to it.
- `Npc.Household` and `Npc.HomeHut` already appear in the NPC record (M3). M3 households are hut-sharing groups with a store; M4 adds `Norms`, partnerships, children and the kin beliefs.
- Save version 3 stores households, partnerships, sleeping rights and kin beliefs. `StateHash` covers them.
- `GameSetup.LifeCycle` off keeps M3 behaviour (adult-only households).

## 10. Tests and acceptance criteria
**Calibration checks, not targets.** `Children_Fed_*`, `Orphan_Fostered_*`, `Care_FollowsKin`, `KinGifts_OrderedByBelief` and `Partnership_Forms` check that the default instinct priors and the default `AssociativeNpcPolicy` give plausible individual behaviour in controlled conditions. A failure is fixed by correcting a bug or the instinct priors, never by a rule that forces feeding, care or fostering. `Stable_10Generations` and `Household_Size` are sanity bands in the same sense as in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#10-tests-and-acceptance-criteria).

"Default sandbox seed" is defined in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#10-tests-and-acceptance-criteria). Tests run in `DotNet/` and EditMode. Percentages are over the stated denominators and use fixed seeds.

| Test | Setup | Expected |
|---|---|---|
| `Membership_Invariants` | Any run, every day | each living NPC in at most one household; member lists sorted, no duplicates, sizes at most 12; `Npc.Household` matches member lists; sleeping rights match huts |
| `Children_Fed_WhenFoodAvailable` | Default sandbox seed, 10 years; child-days (child under 1.0) when the household store plus carried food is at least 2 days of need | child eats at least 80% of its need in more than 90% of those child-days |
| `Children_Fed_DependsOnFood` | Same, child-days when household food is under 0.5 days of need | fed share is lower than the above by at least 25 percentage points (shows that feeding follows resources) |
| `Orphan_Fostered_WithKin` | Orphans (under 1.0, no living caregiver in household) when a believed kin adult with `r̂ ≥ 250` is in the faction | fostered within 5 days in at least 80% of cases (measured over at least 30 orphans across 10 seeds) |
| `Orphan_Fostered_WithoutKin` | Orphans with only unrelated adults in the faction | fostered within 10 days in at least 50% of cases |
| `Orphan_Survival` | Fostered against unfostered orphans | survival to 1.0 at least 2× as likely when fostered |
| `Partnership_Forms` | Adults aged 1.2 or more over 10 years | 40–95% have had at least one partnership by age 2.0 (a sanity band; far outside means a degenerate rule) |
| `Household_Size` | Mean over households with members | 2.5–6.0, none above 12 |
| `Stable_10Generations` | Default sandbox seed, 15 sim years | no extinction; population between 0.5× and 2× of its year-3 value at the end; households per capita within 0.15–0.5; no more than 1% of living NPCs without a household for over 1 year |
| `Care_ReducesIllness` | Controlled scenario: two ill adults, one tended, one not | tended illness severity 30% ± 10% lower; recovery earlier by at least 1 day |
| `Care_FollowsKin` | Controlled scenario with `r̂` 500, 250, 0 patients and equal need | share of care acts: 500 patient more than 250 more than 0 in at least 90% of 20 seeds |
| `KinGifts_OrderedByBelief` | Controlled scenario, food gifts to full sibling, hut-mate, stranger | gifts ordered sibling > hut-mate > stranger in at least 90% of 20 seeds; and changing the NPC's `Kin weight` trait from −50 to +50 raises the sibling share |
| `Kin_NoPedigreeAccess` | Reflection | no `Mind`, `Policy`, `Social`, `Learning` type reads the pedigree, and every `KinBelief` has a source |
| `Kin_Belief_CanBeWrong` | Foster child raised from 0.0 | at least one `FosterParent`/`Mother` belief with true `r` = 0 exists in at least 70% of fostered cases where the child was not told |
| `Foster_TreatedAsKin` | Measurement of feeding by believed role | reported, with the difference between believed-kin and true-kin feeding; acceptance is only that it matches the recomputation from events |
| `Family_Strategies_Measured` | Default sandbox seed | the measures in section 4 compute for every birth and equal a recomputation from events (exact); nothing in the sim reads them |
| `Divorce_SplitsHousehold` | Controlled separation | store split by norm within 1 unit; each child in exactly one household afterwards; both histories closed with reasons |
| `Telemetry_Membership` | Any run | every `HouseholdJoined` is matched by a `HouseholdLeft` or the NPC is alive at the end; intervals of one NPC never overlap; for every dead NPC the last interval ends at its death |
| `Determinism` | 10 generations, same seed twice; recording on and off | same state hash at every day boundary and same membership table |
| `SameSex_EqualCapabilities` | 1000 random household fixtures, each run twice with the partners' sexes swapped to make the pair same-sex (conception disabled in both runs) | identical household store, care acts, sleeping rights, estate shares and social-memory records, to the unit, in every fixture; a static check finds no branch on partner sex outside `Reproduction` and `PhenotypeBuilder` |
| `Adoption_OpenToAll` | Orphan scenario with only same-sex couples and single adults as candidate households, 10 seeds, at least 30 orphans | orphans are fostered at a rate within ± 10 points of the mixed-household control on the same seeds; no `Adopt` is refused by a rule |
| `Partnership_Voluntary` | Any run | every `PartnershipFormed` follows a "yes" from both NPCs in the same or the previous think; every `PartnershipEnded` names its initiator; no partnership forms or persists by a rule |
| `Partnership_CapConfigurable` | Test scenario with `MaxLivePartnerships` 1 and 2 | at 1 no NPC ever holds two live partnerships; at 2 at least one NPC does in ≥ 5 of 10 seeds, and every invariant, estate and membership test still passes (the architecture is not one-partner) |
| `SharedChildcare_Possible` | Two neighbouring households with infants and adults who work; 20 seeds | at least one `CareArrangementMade` occurs in ≥ 10 of 20 seeds; in a fixture where one side never returns turns, the other side's arrangement with it ends within 10 days in ≥ 80% of cases (exploitation is possible and answered by decision, not by a rule) |

## 11. Open questions
No open questions remain. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
- **Q5 Concurrent partners:** serial partnerships are enough for M4, but the relationship architecture must not permanently restrict individuals to one partner. Concurrent partnerships, different household customs and other arrangements must be possible later. Individuals enter and leave relationships voluntarily ([Partnership and courtship](#partnership-and-courtship), `Partnership_CapConfigurable`, `Partnership_Voluntary`).
- **Q6 Same-sex partnerships:** explicitly supported, with the same social, emotional, household, economic, caregiving and material-inheritance capabilities as other partnerships. Biological reproduction stays constrained by the reproductive model, but adoption, fostering and caring for children are open to all suitable households. No arbitrary differences in value (`SameSex_EqualCapabilities`, `Adoption_OpenToAll`).
- **Q15 Shared nurseries:** informal shared childcare between households in Stage 4; a formal institution can emerge in Stage 5 or later. Individuals can cooperate in raising children, share caregiving and potentially exploit such arrangements. Biological parents are not the only possible caregivers ([Shared childcare between households](#shared-childcare-between-households)).

Settled here, with defaults:
- When a partnership ends during a pregnancy, no rule makes the believed father care. Whether he does is his decision and is measured (desertion lag, `Foster_TreatedAsKin`).
- Household norms change by a simple majority of adult members in M4. Band-age elders (M12) may later offer mediation, but do not take over the vote.
