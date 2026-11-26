# Vision and philosophy (stage 4 and beyond)

## 1. Purpose and scope

This document says what *God of the Island* is trying to be, and why. It owns:
- the arc **from Darwin, through Dawkins, to Marx**, with Adam Smith, Robert Axelrod, Elinor Ostrom and the *Creatures* games as equal influences;
- for each influence: the core idea, the mechanics that make it *possible*, and how we would *measure* whether it happened;
- the neutrality principles that every other document must follow;
- the player's role as a state;
- what makes it a game and not only a simulation.

It does not define mechanics. Each mechanic is owned by one document in the [document map](DESIGN_CONVENTIONS.md#11-document-map-one-owner-topic-each-link-dont-repeat), and this document links to it. Where this document and [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) disagree, the conventions win.

## 2. Principles

### 2.1 The one-sentence vision
A population of persistent, mortal, learning people on an island, governed by a player who can change their circumstances but never their minds, in which biology, cooperation, production, ownership and politics are allowed to happen, and are measured when they do.

### 2.2 Neutrality (binding on every document)
1. **Nothing is forced to emerge.** No mechanic exists to make a particular social form appear: not cooperation, not markets, not classes, not unions, not revolutions. A mechanic exists because it is a real possibility for real people: walking, eating, giving, promising, owning, refusing.
2. **Nothing is built to succeed.** No ownership form, institution, strategy or ideology has a hidden bonus. Identical inputs, labour and skill produce identical output whoever owns the workshop. The only differences between forms are *rules*: who decides, who may join, who receives what. Those rules change incentives, and incentives change behaviour.
3. **Outcomes come from circumstances and decisions.** The island (terrain, wild food, luck), the people (genome, personality, experience, culture), the institutions and the player's choices together decide what happens. If an outcome cannot be traced to these, it is a bug.
4. **Experiments must not manufacture outcomes.** A scenario may set up circumstances (scarce food, one large employer, a closed border) but may never set up results (a forced strike, a fixed share of cooperators). Benchmark strategies such as Tit for Tat are separate test policies, never labels on island NPCs. See [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md) and [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md).
5. **Descriptive and causal claims are kept apart.** "Wage workers in our runs complained more" is descriptive. "Losing the commons *causes* wage dependency" is causal, and needs a randomised treatment across seeds. Reports label which kind they are. See [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md).
6. **The theories are questions, not scripts.** Darwin, Dawkins, Marx and the others give us things worth looking for. The game does not assume any of them is right. A run in which classes never form, or in which a cooperative economy outcompetes private firms, or the reverse, is a valid run.
7. **Tuning is honest.** Balancing a number so that a *game* is playable (food yields, walking speed, lifespan) is fine. Balancing a number so that a *theory* comes true is not. Every tuning change records which of the two it serves. Genuine collapse, economic failure or extinction is never tuned away; a failure is first classified as a meaningful outcome, a defective mechanic or a bad baseline parameter ([conventions §14](DESIGN_CONVENTIONS.md#14-further-owner-requirements-27-nov-2026)).
8. **Explore, don't instruct (owner decision Q2).** The game explores the theories; it does not instruct players to accept them, and it has no predetermined political or economic conclusion. Ordinary gameplay, tutorials, institutional reports and KPIs use **neutral, descriptive terms** ("share of adults who work for an employer", not a theory's vocabulary). The intellectual influences are named only on an optional **About/Philosophy page**, with appropriate references (section 8).
9. **The whole vision, not an RTS with NPC stats.** Biology, learning, communication, culture, economy, institutions and information all interact. A milestone that would reduce one of them to a number on a unit card is a design change that needs the owner.

### 2.3 Engineering principles that serve the vision
- **People, not units.** One NPC record for life; nothing is spawned to do a job ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).
- **No one reads the true state.** NPCs act on perception, memory, inference and what they are told ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Saying is not doing.** Exchanges, promises and threats are separate acts that can fail ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Exact determinism.** The same seed and commands give the same history. This makes the science repeatable and the "what if" replay a game feature.
- **Measurement never touches the sim.** Recording on or off gives the same state hash ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)).

## 3. The arc: biology, society, political economy

The arc is a sequence of **questions the sim lets us ask**, in order of what has to exist before the next question makes sense. It is not a sequence of stages a society must pass through.

| Step | Question | What must already exist |
|---|---|---|
| Biology | How do individuals with bodies, needs and inherited predispositions survive and reproduce? | Bodies, drives, food, death, genome, mating |
| Strategy | Which behaviours spread, and which are stable, when individuals meet again and again? | Recognition, memory, contests, help, a payoff from real consequences |
| Culture | How do habits and beliefs spread without genes? | Observation, imitation, teaching, communication |
| Cooperation | When do unrelated people help each other, share, keep promises, and govern a common resource? | Social memory, reputation, gossip, sanctions |
| Production | How does work become specialised, and when does a surplus appear? | Skills that grow with practice, recipes, tools, exchange |
| Property | Who owns land, tools and workshops, and how do they pass on? | Ownership records, estates, inheritance customs |
| Labour | When do some people work for others, and on what terms? | Jobs, wages in goods, loss of independent access to subsistence |
| Class | Do stable positions form in relation to property, labour and surplus? | Measures over all of the above |
| Politics | Do people form interests from their position, act together, and change institutions? | Interests, grievances, coordination, associations, a state that responds |

The island may stop at any step. A faction rich in wild food may never need wage labour. A faction with strong sharing customs may never concentrate property. That is a result, not a failure.

### 3.1 Civilisation ages and emergent unlocks
Ages describe **what institutions a society can sustain**, not what era it is. Each faction's age is measured from its own society, and factions can be in different ages at once. The table is owned by [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#9-civilisation-ages-emergent-unlocks); the unlock rules by [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#ages-and-unlocks).

| Age | The social question it opens | Typical emergent conditions (examples, not a schedule) |
|---|---|---|
| **Band** | Can a small group feed itself and settle disputes? | A few dozen people, kin and friendship ties, elders who are listened to |
| **Village** | What happens when there is a surplus to store, trade and fight over? | A food surplus over several days; skills specialised; belief in storing food spread by culture |
| **Town** | What happens when many people work for others and markets are anonymous? | Many exchanges between strangers; employment at scale; enough trust or enforcement for credit |
| **City** | What happens when institutions are too large for anyone to see whole? | Large population; literacy-like knowledge spread; institutions that already work |

An unlock makes an institution *possible*. The player still chooses whether to found it, and NPCs still choose whether to staff it, use it or obey it.

## 4. The influences

Each influence below gives: the idea in our own words; the mechanics that make it possible (with the owning document); and the measures that tell us whether it happened (KPIs are defined in [KPI_CATALOGUE.md](KPI_CATALOGUE.md)). Page references for Dawkins are to [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md).

### 4.1 Darwin: variation, inheritance, selection
- **The idea.** Individuals differ. Some differences are inherited. In a world where not everyone survives or reproduces equally, inherited differences that help survival and reproduction become more common, with no one intending it.
- **What makes it possible.**
  - A `Genome` with recombination and mutation that sets body and personality predispositions ([BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)).
  - Real mortality: starvation, wounds, cold, old age ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).
  - Partnership and reproduction that depend on the NPC's own choices and circumstances ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)).
  - No fitness function anywhere in the code. Fitness is only what happened.
- **How we measure it.**
  - Trait means and variances per generation, per faction.
  - The covariance between a trait and the number of surviving children (the selection term of the Price equation), against a **neutral control**: the same scenario with the genome-to-trait map switched off, so change from drift alone is known.
  - Whether trait change differs between environments (for example, generosity under scarce vs plentiful wild food).

### 4.2 Dawkins: the gene's eye view, stable strategies and memes
- **The idea.** Behaviour can be understood by asking what spreads, not what is good for the group. A strategy that does well against the strategies around it becomes common, even if everyone would be better off with something else (the conspiracy of doves, Hawk/Dove, PDF 93–95). Helping relatives pays in proportion to relatedness (Hamilton's rule, PDF 116). Ideas spread the same way, by copying (memes, PDF 212–224).
- **What makes it possible.**
  - Recognisable individuals, memory and repeated meetings ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
  - A kin-weight trait, and relatedness as each NPC *estimates* it from cues in its believed kinship graph; true relatedness is used only for measurement ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md), [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)).
  - Contested resources, ownership tags visible to both sides, costly fights ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
  - Cultural traits that spread by imitation and teaching ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)).
  - The maths-only lab, kept apart from the island ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)).
- **How we measure it.**
  - Strategies measured after the fact: how Grudger-like or Tit-for-Tat-like each NPC's recorded choices were ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#410-measuring-strategies-in-the-island)).
  - Help given as a function of relatedness, cost and benefit; a positive slope on relatedness is evidence of kin bias, not proof of Hamilton's rule.
  - Whether a convention such as "resident wins" (PDF 100–103) appears: the share of contests won by the owner, against what personality and strength alone predict.
  - Meme frequencies over time, and whether they track payoffs or conformity.

### 4.3 Adam Smith: the division of labour
- **The idea.** People become far more productive when each does fewer tasks, gets better at them and exchanges the rest. How far this goes is limited by the extent of exchange: a small, isolated group cannot support many specialists.
- **What makes it possible.**
  - Skills that grow with practice and fade with disuse ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#skills)).
  - Recipes with tools and inputs; barter as separate hand-overs; walking costs that limit the reach of exchange ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
  - Learned associations that reward an NPC for what it is good at ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
- **How we measure it.**
  - A specialisation index per adult: the share of its work time in its most frequent activity.
  - Output per work hour against specialisation and against the number of trading partners an NPC has.
  - Exchange network size and density per faction.

### 4.4 Axelrod: the evolution of cooperation
- **The idea.** Among self-interested individuals with no central authority, cooperation can arise and hold when the same people meet again and again, can recognise each other and remember what happened, and do not know when they will stop meeting. Strategies that are nice, retaliatory, forgiving and clear do well (PDF 231–243).
- **What makes it possible.**
  - Deferred exchange, promises, loans and help requests that can be broken ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
  - Stable households and neighbourhoods, so clusters form (PDF 239–243).
  - Mortality and migration, so the shadow of the future is long but uncertain.
- **How we measure it.**
  - Cooperation rate (promises kept, help returned) against the expected number of future meetings between the pair.
  - Whether cooperation spreads from clusters, by mapping it on the kinship and neighbourhood graphs.
  - The same quantities with courts or other enforcement present and absent, as a policy experiment.

### 4.5 Elinor Ostrom: governing the commons
- **The idea.** A shared resource such as a fishery or forest is not doomed to be overused, and does not need either a state or private owners to survive. Communities can and often do write their own rules for it. They do better when boundaries are clear, rules fit local conditions, users help make the rules, someone monitors, sanctions start small and grow, disputes can be settled cheaply, and outsiders respect the arrangement.
- **What makes it possible.**
  - Wild food and timber that regrow and can be overharvested ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
  - Communal ownership with recorded rights; witnesses; gossip; graduated social sanctions ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
  - Band-age elders and communal mediation ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
  - The player can choose to enclose, regulate or leave a commons alone ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **How we measure it.**
  - The stock of each commons over time and its collapse rate.
  - Which of Ostrom's conditions are *measurably present* for each commons (bounded membership, monitoring events, sanction events, mediation cases), and whether their presence predicts survival across seeds.
  - The same commons under state enclosure, private allocation and communal rules, as a randomised treatment.

### 4.6 Marx: material conditions, class and surplus
- **The idea.** How people produce their living shapes how they relate to each other. When some own the means of production and others can live only by working for them, a class relation exists, defined by that relation and not by income or character. The surplus that labour produces beyond its own upkeep is divided according to these relations, and conflict over that division can change institutions.
- **What makes it possible.**
  - Ownership forms (state, private, communal, cooperative), enterprises and employment ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
  - Loss of independent access to subsistence: exhausted or enclosed commons, debt, lost tools.
  - Material inheritance that can concentrate or spread assets over generations ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).
  - Political interests computed from each NPC's own material position, alongside experience, relationships and culture ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)).
- **How we measure it.**
  - Class *measures* (ownership, hiring, selling labour, surplus share, dependency) and derived positions, used only for analysis ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)).
  - Inequality (Gini), mobility (transition matrices, intergenerational correlation).
  - How much of the variance in political stances is explained by material position, against relationships and culture. This tests the materialist claim rather than assuming it.
  - Whether institutional change follows collective action, measured as a causal effect where it can be randomised.

### 4.7 *Creatures*: drives, perception and lifetime learning
- **The idea.** A believable creature is not scripted. It has needs that rise and fall, sees only what is near, attends to a few things, and learns from what happens to it: what reduced its hunger is tried again; what hurt it is avoided. Each individual becomes different through its own life ([research/CREATURES_NOTES.md](research/CREATURES_NOTES.md)).
- **What makes it possible.**
  - Drives as the reward signal; perception, attention and beliefs ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
  - Instincts as priors and associative learning with short- and long-term weights ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
  - Learning in every domain, from foraging to faction membership. Offline training never replaces it ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- **How we measure it.**
  - Learning curves: the change in an NPC's choice frequencies after rewarded or punished experiences.
  - The **twins test**: two NPCs with identical genomes in different circumstances diverge in learned associations and behaviour, by an amount we report.
  - How much of behaviour is explained by genome, by own experience and by culture, from controlled scenarios.

### 4.8 How the influences meet
- Darwin and Dawkins explain why NPCs care about themselves and their kin. Axelrod and Ostrom explain how such people can still cooperate. Smith explains why cooperation in production pays. Marx asks who gets the gain, and what follows when the answer is unequal. *Creatures* makes every one of these happen inside individual minds instead of in equations.
- **Supporting references** we draw on but do not build around: Mancur Olson (selective incentives in collective action), Albert Hirschman (exit, voice and loyalty), Charles Goodhart (targets that stop measuring), Lewis (signalling conventions).

## 5. The player's role: a state, not a god's hand

- **The player governs a state through institutions and incentives.** It sets taxes, wages, purchase offers, contracts, welfare, laws and budgets; it designates sites and commits treasury goods; it founds and funds institutions when the society can sustain them ([STATE_DESIGN.md](STATE_DESIGN.md), [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **The player never controls a person.** It cannot order an NPC to work, believe, love or obey. It can only change what happens to people who make choices.
- **Institutions develop through the ages.** Elders, healers, communal mediation, guards and informal lending come first. Treasury, health, education, military, police, courts, prisons, banks, media, housing, welfare, environment and statistics follow when unlocked.
- **Institutions are made of people.** Their staff are NPCs with their own interests. Institutions can fail through poor management, corruption, conflicting incentives, staff shortages, or Goodhart's law: optimising a reported target while the real outcome gets worse.
- **The player sees through its institutions.** The HUD reads only `FactionKnowledgeView`. In stage 4 that view is nearly complete; in later ages, what the player knows depends on the statistics, records and media it has built ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **The player can favour anything.** Neutrality binds the sim, not the player. The player may subsidise cooperatives, protect property, enclose the commons or nationalise workshops. The sim's job is to give each choice its real consequences.

## 6. Why it is a game, and why it is fun

### 6.1 What the player does minute to minute
- Reads the island: who is hungry, who is building, what is short, where people are complaining.
- Pulls levers: a tax, a wage, a contract, a purchase offer, a welfare ration, a law.
- Watches people respond, and sometimes refuse.
- Handles crises it did not script, such as a failed harvest, a defecting patrol, or (in later ages) a strike at the only smithy or a corrupt granary keeper.

### 6.2 Sources of fun
- **Stories nobody wrote.** A family's workshop might pass through three generations; a feud might start with one broken promise; a commons might collapse because of one greedy household. None of these is scripted, and each may never happen. The NPC inspector shows each person's own view and the reason for their last decision, so the stories are legible.
- **Hard choices with no right answer.** Conceding to a strike costs treasury and invites more strikes; repressing it costs legitimacy and may send families to a rival faction. The game never says which is correct.
- **Surprise from honest rules.** Because nothing is forced, the player can be surprised, and can trust that the surprise is real.
- **Mastery of a system, not a script.** Players learn how incentives propagate: what a wage floor does to independent producers, what free food does to foraging skill, what a court does to credit.
- **Replay as play.** Determinism lets the player rewind to a saved day and try another policy on the same seed. "What if I had not enclosed the forest?" becomes a button.
- **Competition between factions.** Rival states run the same rules. People can vote with their feet, so a badly governed state can lose its people.

### 6.3 Modes
- **Sandbox (primary).** Open-ended, multi-generation. Design and balance target it first. Optional player-chosen goals (prosperity, population, economic development, low inequality, longevity of the faction) are pluggable victory conditions, and none is privileged.
- **10 VP race (secondary).** A shorter competitive mode on the same rules: about 60 minutes and about 2 generations at default `LifeScale` ([conventions section 4](DESIGN_CONVENTIONS.md#4-time-model)). Class and political dynamics appear only as far as they develop in that time. No rule is changed to speed them up.
- **Experiment mode (for us, and for curious players).** Headless runs across seeds, with the warehouse and reports ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md), [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).

### 6.4 Guardrails on fun
- Balancing for fun works on circumstances (yields, distances, lifespans, event frequencies), never on outcomes.
- If the game is dull because nothing emerges, the fix is to add *possibilities* (a new action, a new resource, better perception), not to script an emergence.
- Pacing tools are allowed: `LifeScale`, time speed, tutorial scenarios with chosen circumstances.

## 7. Interactions with other systems
This document is upstream of all others. In particular:
- Every design doc's section 4 must state which outcomes its mechanics make possible and which measures detect them, in the spirit of section 4 here.
- [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md) turns the neutrality principles (2.2) into a checklist for every experiment.
- [KPI_CATALOGUE.md](KPI_CATALOGUE.md) holds the definitions of every measure named in section 4.

## 8. Player and view
- The HUD never declares a society "capitalist", "communist" or anything else. It shows measured facts: who owns what, who works for whom, how surplus is shared, what people say they want.
- **About/Philosophy page (optional, from the main menu).** It names the influences in section 4 with references (the books and papers in [research/](research/SELFISH_GENE_NOTES.md) and their published sources), explains which mechanics each one suggested, and says plainly that the game tests these ideas rather than endorsing them. No other screen, tutorial card, report or KPI name uses the theorists' names or vocabulary.
- Reports label descriptive statistics and experimental results differently.
- Animations only show events; they never decide them.

## 9. Telemetry
This document emits nothing. Each measure in section 4 is computed from the domain events and snapshots defined in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#8-domain-events-and-snapshots) and owned by the documents linked there.

## 10. Stage 4 minimum vs later

| Influence | Stage 4 makes possible | Later |
|---|---|---|
| Darwin | Genome, mortality, reproduction (M4) | Long headless runs over hundreds of years; selection experiments |
| Dawkins | Lab benchmarks (M2); recognition, memory, contests, help (M5, M7); kin weight (M4); memes (M11) | Learned signalling conventions; meme complexes |
| Smith | Skills, recipes, barter, jobs (M3–M6); workshops (M8) | Money (Town age); markets between strangers |
| Axelrod | Deferred exchange, promises, reputation (M7) | Courts as enforcement (Town age) and their measured effect |
| Ostrom | Communal property, witnesses, sanctions (M8); forest and other renewables that can be overused (M6); elders and mediation, and communal institutions founded by communities without the state (M12, owner decision Q17) | Commons councils; nested institutions |
| Marx | Ownership forms, employment, class measures (M8); political-interest measures (M10); recorded in M13 | Collective action, movements, disputes (stage 5 onwards) |
| *Creatures* | Drives, perception, associative learning (M3); social learning (M7); culture (M11); network inputs (M15) | Learned concepts and lexicons |

## 11. Tests and acceptance criteria
These are neutrality checks that every relevant milestone adds to [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md):
1. **No outcome flags.** A static test fails if sim code (outside `Lab` and `Telemetry`) references any analysis category: class positions, measured strategy names, ideology labels.
2. **Form neutrality.** For each ownership form, the same recipe, inputs, tools, skill and labour ticks produce the same output, to the unit (see [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md#10-tests-and-acceptance-criteria)).
3. **Recording neutrality.** Recording at every level gives the same `StateHash` after 10 sim years on 20 seeds.
4. **Not inevitable.** For each headline emergent outcome a scenario claims is *possible* (a strike, a commons collapse, a concentration of property), the outcome occurs in more than 0% and fewer than 100% of 100 seeds, unless the document explains why it is physically determined.
5. **Neutral controls exist.** Every causal claim in a report names its control treatment and seed count.
6. **Neutral vocabulary (Q2).** A static check over HUD strings, tutorial cards, KPI names and report labels finds no theorist's name and no term from a fixed list of theory vocabulary (for example "proletariat", "bourgeoisie", "capitalist", "selfish gene", "ESS") outside the About/Philosophy page. The list lives with the test and only grows.

## 12. Open questions
No open questions remain.

**Owner decision (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026) Q2):** keep the default. The intellectual influences go on an optional About/Philosophy page with appropriate references (section 8). Ordinary gameplay, tutorials, institutional reports and KPIs use neutral, descriptive terms. The game explores the theories and does not instruct players to accept them. There is no predetermined political or economic conclusion (principle 8 in section 2.2).

Owned elsewhere: whether a faction can fall back an age ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#ages-and-unlocks)); suggested goals in the sandbox ([STATE_DESIGN.md](STATE_DESIGN.md)).
