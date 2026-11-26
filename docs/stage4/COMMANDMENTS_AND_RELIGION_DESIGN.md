# Commandments and religion design (owner decisions D5, 26 Nov 2026)

A commandment is a persistent, structured religious rule that a god reveals and people learn, pass on, interpret, follow, ignore or forget. Religion is what individuals believe and practise about gods, kept apart from trust in people, fear, loyalty, citizenship and culture. This document follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) and the owner decision register [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md); the owner's words are in [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §12 and §13. **The record shapes (`DivineMind`, `GodBelief`, `KnownCommandment`, `Commandment`, `CommandmentRule`), the id ranges, verbs, gesture acts, topic kinds and event names are frozen by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("SAC") §3, §13 and §14–15; this document owns D6's behaviour.**

Written against `unity/poc` at `8617eaa`.

---

## 1. Purpose and scope

**This document owns:**
- the structured commandment: every §12 field, templates, the builder and the one-way text rendering;
- per-NPC knowledge states (heard, learned, adopted, rejected, misunderstood as a variant interpretation, forgotten);
- transmission through M5 gestures and cultural channels, misunderstanding, conflicts between commandments resolved by each individual;
- how a commandment enters the decider and the learner (a culturally learned, instinct-bounded term), never as a hard restriction;
- institutions adopting a commandment as law, and why law and commandment stay distinct;
- belief, faith, trust, fear and loyalty towards gods, religious affiliation, citizenship and cultural membership kept separate (D5.15);
- prayer, worship, offerings, conversion, rejection, several gods and several religions;
- religious organisations on the generic organisation framework, and priests with their own interests.

**It links to, and does not repeat:**
- gods, the divine term, delivery and divine learning: [DIVINE_PLAYER_DESIGN.md](DIVINE_PLAYER_DESIGN.md);
- costs, and consecrated places as miracles: [MYTHIC_AND_MIRACLES_DESIGN.md](MYTHIC_AND_MIRACLES_DESIGN.md);
- the organisation framework (identity, ownership models, governance, roles, assets, records): [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md);
- cultural traits, the adoption test and channels: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md); gestures, decoding and misunderstanding: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md); the learner: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- citizenship (C5) and laws (C9/C10): [STATE_DESIGN.md](STATE_DESIGN.md), [INSTITUTIONAL_SERVICES_DESIGN.md](INSTITUTIONAL_SERVICES_DESIGN.md);
- the commandment editor and doctrine panels: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md); prayer and worship animations: [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md); revelations and sacred places on screen: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md).

---

## 2. Owner decisions and principles

**Owner decisions:** D5.1 and D5.2 (religious authority distinct from political, economic and institutional authority; religious institutions may disagree with their god), D5.15 (affiliation and citizenship distinct; belief, faith, trust, fear, loyalty and culture distinct), D5.16 and D5.17 (institutions emerge independently; religious organisations on the same framework), D5.33 (no predetermined outcomes).

**Principles:**
1. **Structured rules, not text.** A commandment is a record of enumerated fields; the player-facing words are rendered from it. **No LLM, parser or free-text interpretation runs during the simulation** (brief §12).
2. **Never enforced** (SAC §13.2): no code refuses, blocks or forces an action because of a commandment. A commandment acts only through (a) the D6 part of the divine term for candidates matching an interpretation the NPC holds, (b) a feature key so learning can confirm or erode it, and (c) the social consequences other people choose.
3. **Each person holds its own copy.** What a person follows is *its interpretation*, possibly altered, learned from a revelation, a person or a gathering. Two people can follow different versions of "the same" commandment.
4. **No obedience score.** Belief, faith, trust, fear and loyalty are five independent fields per god per person (SAC §13.1); affiliation is a separate record that neither requires nor implies belief; citizenship and culture live elsewhere.
5. **Learned from consequences.** Whether keeping a commandment pays is learned by the ordinary learner from real outcomes. No reward is invented for piety or compliance.
6. **Deterministic and bounded.** Integer maths; keyed draws (`commandment-interpret`, and the per-NPC religion stream); fixed-size tables; a daily religion pass per person in its slot.

---

## 3. Data structures

**Frozen by SAC §13 (D6 fills them):**

```csharp
public sealed class DivineMind {                        // NpcMind.Divine, null unless Divinity.Enabled
    public DecodedDivine[] Inbox;   public int InboxCount;      // D2, cap 4
    public GodBelief[] Gods;        public int GodCount;        // D6, cap = god count
    public KnownCommandment[] Known; public int KnownCount;     // D6, cap 8, eviction: lowest salience, ties by id
}
public struct GodBelief { public int God; public short Belief, Faith, Trust, Fear, Loyalty; public long LastEvidenceTick; }
public struct KnownCommandment { public int Commandment, Revision; public CommandmentSource Source; public int FromNpc;
                                 public short Adoption /* -1000 rejected .. 1000 adopted */, Salience; public CommandmentRule Interpretation; }

public enum Modality : byte { Required = 1, Prohibited = 2, Encouraged = 3 }
public struct CommandmentRule { public Modality Modality; public byte LearningVerb; public byte ObjClass; public Ware Ware;
                                public ApplySelector Applies; public ConditionKind Condition; public int ConditionParam; public DivineTarget Scope; }
public sealed class Commandment { public int Id, God, Revision; public int Template; public CommandmentRule[] Rules; public int Importance;
                                  public long IntroducedTick; public bool Revoked; public List<CommandmentRevision> History; }   // DivinityState.Doctrine
```

**Every brief §12 field has a place:** required, prohibited and encouraged behaviour (`Modality`, one per rule; a commandment may hold several rules, e.g. "share food" plus "do not hoard"); relevant actions and objects (`LearningVerb`, `ObjClass`, `Ware`); applicable individuals or groups (`Applies`); conditions (`Condition`, `ConditionParam`); geographical scope (`Scope`, a `DivineTarget`: everywhere, a location and radius, a household, an organisation's members); date of introduction (`IntroducedTick`); issuing god (`God`); importance (`Importance`, 1–5); revision history (`Revision`, `History`); revocation (`Revoked`, with its tick in `History`).

**D6's enums** (its own, not shared):

| `ApplySelector` | `ConditionKind` (each is something the person perceives: layers 2, 3, 6) |
|---|---|
| `Everyone`, `Adults`, `Children`, `Elders`, `Affiliated` (to the issuing god's religion), `Community` (`Npc.Faction` = param), `Role` (param), `HouseholdHeads` | `None`, `TargetIsChild`, `TargetIsElder`, `TargetIsStranger`, `TargetBegging` (the M4 display), `TargetInjured`, `TargetSick`, `CarryingFood`, `HouseholdSurplus`, `StockLow` (the target source), `NotPermitted` (the affordance had `NoPermission`), `IOwe` (the target), `TargetApologised`, `EnemyInSight`, `AtHome`, `NearShrine` |

A condition is evaluated from the person's own context (its `ThinkScratch`: base and target features, displays, affordance flags), never from true state.

**Religions and affiliation:**

```csharp
public sealed class Religion {                 // DivinityState.Doctrine.Religions, ascending id; a tradition, not a god and not an organisation (SAC §1)
    public int  Id; public int[] Gods;         // 1..4 gods it worships
    public int[] Canon;                        // commandment ids it teaches
    public CommandmentRule[][] CanonRules;     // its own interpretation of each (may differ from the god's current revision)
    public int  ParentReligion;                // a schism's origin, 0 for founding religions
    public long FoundedTick;
}
public struct Affiliation { public int Npc, Religion, Org; public long From, To; }   // spells in DivinityState.Doctrine.Affiliations; several per person allowed
```

**What is not here, on purpose (SAC §3):**
- **Community of origin** is `Npc.Faction`; **political citizenship** is C5's `CivState.Citizenship` spells; neither is written by any religion code.
- **Faction loyalty** (STATE, rebuilt from experience) is not `GodBelief.Loyalty`, which is attachment to a god; neither code path reads the other.
- **Cultural membership** is C10's culture records (and CULTURAL_TRANSMISSION's `CultureStore`): a religion's gatherings may become a cultural tradition, but holding a cultural trait does not make anyone a member of a religion.
- **Trust and fear of people** stay in M5's `SocialMind`; D6 never writes `SocialMind`. Trust in a priest is ordinary social trust.

**Per-person memory:** `GodBelief` 6 × 20 B, `Known` 8 × about 40 B: about 440 B, plus D2's inbox and exposures (DIVINE_PLAYER §3). D1 and D6 add the "Divinity" row to the POPULATION memory table.

---

## 4. Mechanics

### 4.1 Founding religions and initial beliefs (Tier 1)
At game start D6 founds one religion per god (`Doctrine.FoundReligion`, `ReligionFounded`), with that god only and an empty canon. The origin faction's founders get an affiliation spell to it and a `GodBelief` seeded from scenario tuning (`Divinity.Doctrine.Founders`: belief 600 ± 200, faith 300 ± 200, trust 0, fear 100, loyalty 300 ± 200; keyed per person). Children start with no beliefs and acquire them from their household (4.5). These are starting conditions, not destiny.

### 4.2 Beliefs about gods: evidence and decay
Each field moves only from the person's own perceptions and what it is told; each change is a `BeliefChanged` with its cause (SAC §13.1). Defaults (`Divinity.Doctrine.Evidence`), applied to the god the person *attributes* the evidence to (DIVINE_PLAYER §4.2):

| Evidence (as perceived) | Belief | Faith | Trust | Fear | Loyalty |
|---|---|---|---|---|---|
| A divine instruction to me | +40 | | | | |
| I followed it; the outcome was good / bad (the closed trace's sign) | +10 | +40 / −40 | +30 / −30 | | +20 / −10 |
| Healed or blessed me | +120 | +60 | +150 | −20 | +80 |
| Smote me | +150 | | −250 | +300 | −80 |
| Gave me goods | +80 | +30 | +80 | | +40 |
| Witnessed a sign | +60 | | | | |
| Witnessed a smite of another (`SmiteWitnessed`) | +60 | | | +120 | |
| Witnessed a healing of another | +60 | +20 | +40 | | +20 |
| Told of a divine act (a decoded `Praise` or `Warn` about the god) | +20 × confidence | | ±10 | +30 (warn) | |
| Prayer answered (4.6) | +40 | +120 | +80 | | +60 |
| Prayer unanswered (the episode expired) | | −30 | −10 | | −10 |
| Possessed by it | +200 | | ± outcome | +150 | |
| Recognised its avatar and was given to / struck by it | +100 | | +80 / −200 | / +200 | +40 / |
| Daily decay towards 0 (loyalty does not decay on a day the person worshipped) | −1% | −1% | −1% | −2% | −1% |

**These are separable by construction:** a believer smitten twice ends with high belief, high fear and low trust: it believes the god exists and fears and distrusts it (brief §13). A household member raised on its family's commandments may act on them with low belief: custom without strong belief.

### 4.3 Lifecycle of a commandment: reveal, revise, revoke

- **`RevealCommandment { God, Template, Rules[], Importance, Recipients }`.** `CommandmentRules.Validate` (4.9) checks the rules; `Doctrine.Issue` creates the record (`Revision = 1`, `CommandmentIssued`). The revelation is delivered to the recipients (a `DivineTarget`: a person, a household, a location, the god's believers in view, at most `MaxRecipients`) like a divine command (DIVINE_PLAYER §4.2), with a visible sign; witnesses hear it too at half salience. Each recipient's entry starts with `Source = Revelation`, `Salience = 400 + Belief / 2`, `Adoption = (Belief × Loyalty) / 1000 − 200`, and its interpretation copied with the revelation's misunderstanding (4.5).
- **`ReviseCommandment { God, Commandment, Rules[], Importance }`** keeps the id, bumps `Revision`, moves the old rules into `History` (`CommandmentRevised`). Only people told of the revision update; others keep the old interpretation and `Revision`. A religion's canon keeps its interpretation until its organisation decides to change it (4.7). Revisions spread at the speed of people.
- **`RevokeCommandment { God, Commandment }`** sets `Revoked` (`CommandmentRevoked`) and is revealed like a revision. A person told of it moves its adoption towards rejection (−600); untold people go on following it. The god cannot erase knowledge.
- Costs: MYTHIC §4.5.

### 4.4 How a held interpretation enters the decision and the learner

For each candidate `c` and each known commandment `k` with `Adoption > 0` (in candidate order, then `Known` order), each rule `r` of `k`'s **interpretation** matches when, by the person's own view: `c`'s learning verb equals `r.LearningVerb`; `c`'s target class or ware equals `r.ObjClass`/`r.Ware` (or the rule leaves them 0); `r.Condition` holds in `c`'s context; the person is in `r.Applies` as it believes itself to be; and it believes it is within `r.Scope` (its own node against the scope).

Each match contributes:
1. **The D6 part of the divine term** (DIVINE_PLAYER §4.3.1, SAC §10.3):
   ```text
   sign      = +1 (Required, Encouraged) or -1 (Prohibited)
   weight    = ImportanceVp[Importance]          // default 150, 300, 500, 800, 1200 vp; Encouraged at half
   term_r(c) = sign x weight x Adoption/1000 x Salience/1000 x (1000 + 2 x Conformity)/1000
   commandment term(c) = clamp( sum of term_r over matches, -2000, 2000 )
   ```
   `Adoption` already carries how much the person accepts it (built from belief, faith, loyalty and fear, 4.5), so the god's standing in that person's mind weighs every rule.
2. **A feature key** `Feature.Divine(CommandmentRequired 5 | Prohibited 6 | Encouraged 7)` (SAC §13.2) in `c`'s target features. The learner then learns, per verb, what keeping or breaking commandments has brought this person: a hungry person who keeps sharing food and stays hungry learns a negative value for `(Divine(Encouraged), Give)`; one who is thanked, helped back or spared a smiting learns a positive one.
3. **A believed norm** for `Permits.Believed` (SAC §6): a prohibited take is labelled "not permitted by my faith" in the affordance for the inspector and for C9/C10 classification; it is never a block.

**Never a restriction.** The term is bounded by the instinct bound and can be outweighed; it only adds to scores; it never removes a candidate. A rule about a verb that does not exist yet (theft before C1) simply matches nothing. A static test checks that no D6 code removes or filters a candidate.

**Conflicts between commandments** (and between a commandment and a law, a norm or a need) are resolved by each individual: terms simply add (SAC §13.2), each weighted by that person's adoption, salience and conformity, and its learned values on the feature keys. "Defend your community" and "assist strangers" can both match a stranger from an enemy community at the gate with opposite signs; one person warns, another helps, and the split is measured (test CR5), never set. Each such choice may be logged as `CommandmentChoiceMade` (866, HV).

**Selective following** is the same mechanism: the term competes with hunger, fear, obligations, culture and learned values at every think.

### 4.5 Knowledge states, misunderstanding and transmission

**States** are bands of the two numbers, evaluated at each change and in the daily religion pass (slot `(tick + id × 13) % TicksPerDay == 0`):

| State | Condition | Effect |
|---|---|---|
| **Heard** | entry exists, `Salience < 200` | none (it can still be passed on, weakly) |
| **Learned** | `Salience ≥ 200`, `−300 < Adoption < AdoptFrom` (300) | the term at the current adoption (small) |
| **Adopted** | `Adoption ≥ 300` | the full term |
| **Rejected** | `Adoption ≤ −300` | no term; still known; passed on only critically (a `Warn` about it) |
| **Misunderstood** | orthogonal: the interpretation differs from the revision it was told (analytics truth `VariantMask`) | the variant is what it matches and passes on |
| **Forgotten** | salience decays to 0 and the entry is evicted | `CommandmentForgotten` (865) |

Changes of state emit `CommandmentLearned` (863) and `CommandmentStanceChanged` (864).

**Adoption** moves by: belief, faith and loyalty in the issuing god (daily pull towards `(Belief × (Faith⁺ + Loyalty)) / 2000 + Fear / 4 − 200`, at 5% a day); each exposure from a trusted model (+ the model's own adoption × its esteem / 4000); the person's own learned values on the matching feature keys (a sum of `Ltw` below −1500 pulls it down by 100 a day: experience can make a person reject a commandment); being told of a revocation (−600).

**Salience** rises with each exposure (+150 × channel rate / 100), with acting on it (+20), with worship (+50); falls by `SalienceDecay` (default 10) a day.

**Misunderstanding.** Each field of a copied rule is altered with a chance: at revelation `RevealBlurPermille × (1000 − Belief) / 1000` (default 60), so sceptics hear it less clearly; through a gesture, the M5 field-decode error of that message (distance, night, presented goods). The draw is `commandment-interpret`. Operators: drop the condition; replace the verb by another of the same `VerbGroup`; widen `ObjClass` to 0 (any object) or narrow `Ware`; turn `Required` into `Encouraged` or the reverse; widen or shrink a radius scope by one step. The variant is what the person matches and passes on; variants drift and compete like Param traits (CULTURAL_TRANSMISSION §4.6), and a population can split into interpretations (the seed of a schism, 4.7).

**Channels:**

| Channel | How | Rate (exposure strength) | Tier |
|---|---|---|---|
| **Revelation** | the god's reveal (4.3) | direct | 1 |
| **Telling (M5 gestures)** | a speaker chooses `Say(Teach)` (Teach act 18, C7's) with `TopicKind.Commandment` (7) carrying **its own interpretation**; listeners decode it by M5 rules; `Source = Told`, `FromNpc` = speaker | 80 | 1 (the verb row is D6's until C7 lands `Teach`) |
| **Household** | in the daily pass, a child or household member is exposed to a household adult's adopted commandments (the CULTURAL vertical channel; adoption test of CULTURAL_TRANSMISSION §4.3, one shared function) | 120 | 1 (a minimal pass in D6 until C10's cultural pass carries it) |
| **Gathering** | people worshipping together are exposed to each other's adopted commandments (4.6) | 100 | 2 |
| **Priests** | a priest's preaching task, a costly teaching act (CULTURAL §4.5) | 400 a session | 3 |
| **Migrants** | a migrant keeps its knowledge | — | 1 |

**Speaking is a choice:** telling is a candidate scored by the ordinary decider (a small instinct prior raised by loyalty, then learned from the listeners' responses), so zealous believers talk more, and people learn whether talking about their god pays among these neighbours.

### 4.6 Prayer, worship, offerings, conversion and several gods

- **Prayer (Tier 2).** `Pray` (learning verb 116) is offered when a drive is in band ≥ 2, the person or a household member is injured or ill, and some `GodBelief` has belief ≥ `PrayFrom` (200). It targets the god with the highest `Belief × max(0, Faith)`, costs `PrayTicks` (30), shows as the gesture act `Pray` (19), and emits `PrayerMade` (870), which the god's divine view lists (channel `Prayer`). `Pray` is deferred: its trace waits for an episode `EpisodeKey.Make(37, prayerSerial)` (kind 37, Divinity's spare range 32–39) that closes when the person perceives an intervention it attributes to any god that addresses it or its need within `PrayerWindowTicks` (default 2400): the relief the miracle brings is the prayer's reward. Otherwise the episode expires with nothing. So people **learn whether praying pays**, in context, from what actually happens; faith and trust follow 4.2.
- **Worship (Tier 2).** `Worship` (117, gesture 20) at a consecrated place (MYTHIC §6.12, `ObjClass.Shrine`) or with others of the same affiliation: it costs time; loyalty does not decay that day; everyone present is exposed to the adopted commandments of those present (the gathering channel); `WorshipHeld` (871). Gatherings that recur at one place may become a tradition through C10.
- **Offerings (Tier 2).** `MakeOffering` (118): a real `Give` of goods into a shrine bin (`BinKind.Shrine`, owned by the religious organisation if one exists, basis `Offering` 21), or consumed (`ConsumeReason.Offering` 7) where no organisation holds the shrine. A god never receives goods (it owns nothing).
- **Several gods.** A person may hold beliefs about every god (`GodCount` = god count), pray to any, worship several. A religion may worship more than one god.
- **Conversion (Tier 2).** In the daily pass: if the god with the highest `Loyalty + Belief / 2` is not among the gods of any religion the person is affiliated with for `ConvertDays` (3) and its loyalty there is at least `ConvertFrom` (400), the person takes an affiliation spell to that god's religion it knows of (the one it learned the god from, else the founding one), and ends its other spells unless `KeepOtherAffiliations` (default off) — `AffiliationChanged` (868). With religious organisations (Tier 3), joining becomes an organisation membership decision (C6's `Join`, 104) under the organisation's admission rule.
- **Rejecting a religion.** Its spell ends when every belief in its gods falls below `LapseBelief` (100), or by the person's own decision once a `Renounce` act exists (Tier 3).
- **Citizenship never moves.** Conversion, rejection and every religious event leave C5 citizenship and `Npc.Faction` unchanged (test `Affiliation_Change_Keeps_Citizenship`, scenario 16). A state may *react* to religion through its own process (a law, an admission rule); that is its decision.

### 4.7 Religious organisations and priests (Tier 3; seams in Tier 2)

A religious organisation is an ordinary organisation (`OrgId`, `OwnerKind.Org`) in [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md), with the same charter, ownership models, governance (`DecisionRule`), roles, assets, budget and records as any other. What is specific:
- **Charter:** the religion it serves and its canon (`Religion.Canon`, `CanonRules`): the organisation's own interpretation of each commandment, which may differ from the god's current revision and from members' versions.
- **Roles:** priest (`Role.Priest` 6, `NpcGroup.Clergy` 8): leads worship, preaches, interprets, receives offerings; plus acolytes and members. Priests are ordinary people who took the role by their own decision, paid as the organisation's rules say.
- **Assets:** a temple (an ordinary building on the organisation's record), the shrine bin of offerings.
- **Founding** is independent (D5.16): believers who recognise the need found it through C6's founding rules. A god cannot found one (it has no authority); it can suggest the idea to people.
- **Priests have their own interests.** A priest's choices run through its own policy and the organisation's governance: it may accumulate property from offerings, gain esteem and influence, prefer interpretations that benefit the organisation or itself (an interpretation decision weighing the budget and its standing against fidelity to what it believes the god said), refuse or modify the god's instructions to the organisation (DIVINE_PLAYER §4.6), quarrel with other organisations, or preach a version it knows differs (truth flag `CanonMatchesRevelation`, analytics only).
- **Schisms.** When members hold sharply different interpretations (the share of a variant above `SchismShare`, default 400 per mille, for `SchismDays`, 10), members may found a new religion and organisation through the founding rules (`ParentReligion` set). Made possible, never scheduled.

### 4.8 Commandments adopted as law

An organisation with rule-making powers (a government, a community with its own rules; C9/C10) may adopt a law derived from a commandment through its own decision: `Law { OrgId, SourceCommandment, Rules, Enforcement }` (SAC §13.2), `LawAdopted` (590). The law **copies the rules at a revision**; from then on:
- the law is enforced by people with powers (guards, cases, fines), the commandment by conscience, community and the god;
- each person holds them separately: law knowledge gives the expected-sanction cost (STATE "Laws v1", C9); commandment knowledge gives the divine term;
- revising or revoking the commandment does not change the law; repealing the law does not touch the commandment;
- a *norm* (a person's or culture's learned expectation, C10) is a third thing again.
The organisation may adopt such a law because priests, believers or a divine request pressed it, or refuse.

### 4.9 Templates, builder and rendering

**Builder.** The commandment editor (PLAYER_UI_DESIGN) composes rules from enumerated menus only: modality; verb (rows of `VerbTable`, greyed while no candidate uses them); object class or ware; who it applies to; condition (the `ConditionKind` menu); scope (everywhere, a circle on the map, a household, an organisation's members); importance. `CommandmentRules.Validate` (pure, in the sim) rejects empty or oversized rules and unknown ids (`bad_target`). The command carries rules, never text.

**Rendering.** `CommandmentText.Render(rules, locale)` (U2, outside the sim) builds the sentence from phrase tables. It is one-way: nothing parses text into rules, and `Sim/` never calls it (static test). A person's interpretation renders the same way, so the player can read "what Ana thinks the commandment says".

**Templates** (`CommandmentTemplates`, D6; the brief's §12 examples). "Tier 1" means every verb and condition exists or is D6's own; otherwise the package that supplies the missing piece is named.

| # | Template | Rules (modality, verb, object or ware, applies, condition) | Status |
|---|---|---|---|
| T1 | Share surplus food with hungry children | Encouraged `Give` (48) food · Adults · `TargetBegging` and `CarryingFood`; Encouraged `Feed` · Adults · `TargetIsChild` | Tier 1 |
| T2 | Do not steal | Prohibited `PickUp` (15) · Everyone · `NotPermitted`; Prohibited `TakeFromStore` (18) · Everyone · `NotPermitted` | Matches nothing until C1 makes unpermitted takes possible |
| T3 | Honour agreements | Required `HandOver` (49) · Everyone · `IOwe`; later Required `Repay` (98) | Tier 1 (M5 deals); C2 for loans |
| T4 | Protect forests | Prohibited `Gather` (9) `WoodStand` · Everyone · `StockLow` | Tier 1 |
| T5 | Teach children | Encouraged `Teach` (108) · Adults · `TargetIsChild` | C7 |
| T6 | Assist strangers | Encouraged `Give` (48) · Everyone · `TargetIsStranger` and need; Encouraged `Say(Cooperate)` (71) · `TargetIsStranger` | Tier 1 partial; `Help` with C2 |
| T7 | Care for elderly people | Encouraged `Tend` · Adults · `TargetIsElder`; Encouraged `Give` food · `TargetIsElder` | Tier 1 |
| T8 | Forgive those who repent | Encouraged `Forgive` · Everyone · `TargetApologised` | C2 (M7 acts) |
| T9 | Protect communal property | Prohibited `TakeFromStore` (18) communal store · Everyone · `NotPermitted` | C1, C8 |
| T10 | Contribute to public services | Encouraged `Store` (17) into a communal or organisation store · Adults · `HouseholdSurplus` | C8 |
| T11 | Defend your community | Encouraged `Say(Warn)` (74) and later `Attack` · Adults · `EnemyInSight` and `AtHome` | Tier 1 (warn); C4 (attack) |
| T12 | Treat injured individuals | Encouraged `Tend` (later `Treat` 107) · Adults · `TargetInjured` | Tier 1 (`Tend`); C7 |

---

## 5. Interactions with other systems

| System | What flows |
|---|---|
| `Divinity` (D2) | Revelation delivery and decoding; the divine term's D6 part; evidence from interventions |
| `Cognition` | Rule matching per candidate from the person's own context; `Pray`, `Worship`, `MakeOffering`, telling candidates; `Permits.Believed` labels |
| `Learning` | `Feature.Divine` 5–7; prayer episode kind 37; learned values feed adoption |
| `Comms` | `Say(Teach)` with `TopicKind.Commandment` (7); `Praise`/`Warn` with `TopicKind.God` (8); gestures `Pray` (19), `Worship` (20); M5 decode errors produce variants |
| `Culture` (C10) | The shared adoption-test function; vertical and tradition channels; norms |
| Organisations (C6) | Religious organisations, canon, priests, offerings, membership; schisms by founding |
| Justice and politics (C9, C10) | `Law.SourceCommandment`; `LawAdopted` |
| Citizenship (C5) | Untouched by every religion path |
| `Telemetry` | Section 7 |

---

## 6. Player and view

- **Commandment editor:** templates first, then the structured builder; a live rendering of the sentence; a preview of who would hear it and the cost. The player never types a rule.
- **Doctrine panel:** per commandment, how many people hold it in each state, the most common variants (rendered), revision history, where it is spreading; per religion, members and canon.
- **Person inspector:** five separate bars per god (belief, faith, trust, fear, loyalty), affiliations, citizenship shown separately, the person's interpretations in words with their state, and the divine term in the "why" line.
- **In the world:** prayer and worship animations, gatherings at shrines, revelations as light (VISUAL_WORLD_DESIGN, NPC_ANIMATION_DESIGN). Unity animates events only.

---

## 7. Telemetry

Codes and names are SAC §15.1 (D6, 860–879).

| Event | Fields |
|---|---|
| `CommandmentIssued` (860), `CommandmentRevised` (861), `CommandmentRevoked` (862) | intervention, god, commandment, revision, template, rules, importance, recipients |
| `CommandmentLearned` (863) | npc, commandment, revision, source, from npc, salience, adoption; truth: variant mask |
| `CommandmentStanceChanged` (864) | npc, commandment, from state, to state, cause |
| `CommandmentForgotten` (865) | npc, commandment |
| `CommandmentChoiceMade` (866, HV) | npc, commandment, candidate verb, term, chosen or not |
| `ReligionFounded` (867) | religion, gods, parent, founders |
| `AffiliationChanged` (868) | npc, from, to, org, cause (Founded, Converted, Lapsed, Joined, Schism); citizenship and community as unchanged fields for the scenario 16 check |
| `BeliefChanged` (869, HV; always kept when \|Δ\| ≥ 100) | npc, god, field, delta, evidence kind, intervention |
| `PrayerMade` (870, HV) | npc, god, need, node |
| `WorshipHeld` (871) | place, religion, attendees, leader |
| `LawAdopted` (590, C10) | `SourceCommandment` |

**Snapshots:** person-day row: affiliations, the five fields for the top god, commandments held and adopted; daily religion row: members, believers by band per god, worshippers, prayers; daily commandment census per community and religion: counts by state and the top 4 variants. Warehouse tables are added by Q1 ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) §12).

---

## 8. Invariants

| Id | Invariant | Checked by |
|---|---|---|
| I-CR-1 | No D6 code removes, filters or blocks a candidate or an act | Static test; unit test with the term at its maximum: the prohibited candidate still exists and wins when other terms outweigh it |
| I-CR-2 | The commandment part of the divine term is within ±2000 | Unit test |
| I-CR-3 | Affiliation changes never change C5 citizenship or `Npc.Faction`; D6 never writes `SocialMind` | Static test; `Affiliation_Change_Keeps_Citizenship` |
| I-CR-4 | The sim never parses text; rules enter only as structured commands | Static test (no renderer use in `Sim/`) |
| I-CR-5 | Knowledge tables within caps; `Adoption`, `Salience` within range | `SimInvariants` |
| I-CR-6 | A law derived from a commandment keeps its copied rules through later revisions | Unit test |

---

## 9. Tier and package mapping

| Capability | Package | Tier |
|---|---|---|
| `Commandment` records with every field, `CommandmentRules.Validate`, templates T1, T3, T4, T6, T7, T11, T12; reveal, revise, revoke | D6 | 1 |
| Per-person knowledge (heard, learned, adopted, rejected, forgotten) and variant interpretations | D6 | 1 |
| The commandment part of the divine term and `Feature.Divine` 5–7 | D6 | 1 |
| Transmission by telling (`Say(Teach)`, `TopicKind.Commandment`) and the minimal household pass; migrants | D6 (+ U4 animation) | 1 |
| `GodBelief` five fields with the evidence table; founding religions and affiliation spells | D6 | 1 (minimal evidence) / 2 (full table) |
| Prayer with episodes, worship, offerings, conversion, several gods | D6 | 2 |
| Commandment adopted as law | D6 + C9/C10 | 2 |
| Templates T2, T5, T8–T10 as their verbs land | D6 with C1, C2, C7, C8 | 2–3 |
| Religious organisations, canon, priests' interests, schisms | D6 + C6 + C10 | 3 |
| Commandment exposures through C10's cultural pass; traditions; priests' preaching | C10 | 3 |
| Worship-fed Mythic | D6 + D1 (amendment) | 3 |

Brief §42: structured commandments are Tier 1; basic religious beliefs Tier 2; advanced religious institutions and advanced beliefs and conversion Tier 3.

---

## 10. Migration from the existing code

Structural pieces are F1's (SAC §0.1): `NpcMind.Divine`, the id ranges (`Pray` 116, `Worship` 117, `MakeOffering` 118; gestures 19, 20; topic kinds 7, 8; `Feature.Divine`), the event codes. D6 adds its partial state (`DivinityState.D6.cs`), hook bodies (`DivinityMindHooks.D6.cs`), the templates table, the validation function and the adoption-test function shared with C10 (written once in `Sim/Culture` so C10 reuses it). Until C7 lands `Teach`, D6 adds the `Say(Teach)` verb row (82) and C7 later adopts it.

---

## 11. Tests and acceptance hooks

| Id | Test | Pass | Scenario |
|---|---|---|---|
| CR1 | **Revealed, learned, communicated.** Reveal T1 to 3 believers in a village of 20 with telling on | Within 3 days ≥ 8 people hold it (Learned or better) in ≥ 80% of 30 seeds; every holder's entry traces back through `CommandmentLearned.FromNpc` to the revelation | 13 |
| CR2 | **Influences behaviour, not universally.** T1 adopted by half the adults; begging children present | Adopters give food to begging children ≥ 2× as often as non-holders over 10 days (30 seeds), and still refuse in ≥ 20% of opportunities when hungry themselves | 13 |
| CR3 | **Never a restriction.** T4 adopted at importance 5 by a person with no other wood and a hut to build | It still gathers from the low stand at least once in ≥ 90% of 30 seeds; I-CR-1 passes | 13 |
| CR4 | **Misunderstanding makes variants.** Telling at night and at distance along a chain of 5 people, 200 runs | The share of altered copies at each hop is within ±3 points of the computed field error rate; variants are matched and passed on as held | 13 |
| CR5 | **Conflicts resolved individually.** T6 and T11 both held; a stranger from an enemy community approaches | Both responses occur; the share helping versus warning correlates with each person's adoption values and learned values (rank correlation > 0.3), not with id order | 13 |
| CR6 | **Forgetting.** A held commandment never reinforced | Salience falls by the daily decay; at 0 `CommandmentForgotten` | 13 |
| CR7 | **Rejection from experience.** A holder of T1 who is always hungry after giving and never helped back | Its learned value on `(Divine(Encouraged), Give)` turns negative and the stance becomes Rejected within 15 days in ≥ 70% of 30 seeds; the control (helped back) keeps it | 13 |
| CR8 | **Affiliation changes without citizenship.** A person affiliated to god 1's religion is healed three times by god 2 | `AffiliationChanged(Converted)` to god 2's religion; C5 citizenship and `Npc.Faction` unchanged; its faction loyalty changes only through its own experiences | 16 |
| CR9 | **Separate quantities.** A believer smitten twice by its god | Belief ≥ 800, fear ≥ 600, trust ≤ −300; loyalty fell; no single score drives all five | 16, 12 |
| CR10 | **Law ≠ commandment.** An organisation adopts a law from T4; then the god revises T4 | The law's rules are unchanged; people hold the law cost and the commandment term separately; repealing the law leaves the commandment | 13, 15 |
| CR11 | **Prayer learned from outcomes.** Ill people pray; arm A's god heals within a day of each prayer, arm B's never | By day 15 the praying rate when ill is ≥ 20 points higher in A in ≥ 90% of 30 paired seeds; faith rises in A and falls in B | 12, 13 |
| CR12 | **Structured only.** The builder's output round-trips through save/load and renders identically; no text reaches the sim | Unit and static tests (I-CR-4) | 13 |
| CR13 | **Replay.** *runner*: reveal, revise and revoke during a 10-day run with telling | Identical hashes and census on replay and after save/load | 18 |

---

## 12. Open questions

Minor parameters are configurable defaults above (evidence values, thresholds, channel rates, caps). One consequential question for the owner:

- **May a god reveal a deliberately harmful commandment, and may priests knowingly preach false canon?** The design allows both (gods are not assumed benevolent; priests have their own interests), with truth flags for analysis. Default until answered: both allowed in sandbox; scenarios may restrict them.
