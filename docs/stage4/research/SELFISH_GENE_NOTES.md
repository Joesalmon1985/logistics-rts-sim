# Research notes: game theory in *The Selfish Gene*

Source: Richard Dawkins, *The Selfish Gene*, 30th-anniversary Oxford edition (the owner's PDF copy, 385 PDF pages). These notes were made on 26 Nov 2026 by reading the whole book, including the endnotes. They paraphrase the book. The book text itself is copyrighted and is **not** in the repository.

**Page numbers:** "PDF p. X (bk Y)". In the main text and endnotes, the book page is the PDF page minus 23.

**Chapters by PDF page:**

| Ch5 | Ch6 | Ch7 | Ch8 | Ch9 | Ch10 | Ch11 | Ch12 | Ch13 | Endnotes | Index |
|---|---|---|---|---|---|---|---|---|---|---|
| 89 | ~111 | 132 | 146 | 163 | 189 | 212 | 225 | 257 | 290–355 | 368–375 |

How these notes are used: [GAME_THEORY_DESIGN.md](../GAME_THEORY_DESIGN.md) maps each example onto the game. [STRATEGY_BENCHMARKS.md](../STRATEGY_BENCHMARKS.md) turns the ones that fit into controlled experiments.

## Corrections the book itself makes (read first)

1. **Retaliator is NOT an ESS.**
   - The endnote (PDF 306, bk 283) withdraws the main-text claim (PDF 97).
   - Dove drifts in neutrally, and then Hawks, Bullies and Probers invade.
   - A dynamic simulation by Gale & Eaves found the true ESS is a stable mixture of Hawks and Bullies.
   - The remark that Prober-retaliator is "almost an ESS" is also withdrawn.
2. **The coy/fast and faithful/philanderer equilibrium is NOT stable.**
   - Endnote, PDF 326 (bk 303). Schuster & Sigmund showed the 5/6 and 5/8 point is a knife-edge.
   - The dynamics cycle forever, which is the same cycle the main text tells as a story.
   - This makes a good stability test for any simulation.
3. **The Concorde fallacy is a separate example.** It is in Ch9 (PDF 173, bk 150) and is about deserting a mate. Digger wasps appear only in a Ch5 endnote.
4. **"Bourgeois" is not used in the text.** Only the index has "bourgeois butterfly". The text says resident/intruder, and the speckled-wood butterfly work is in an endnote.
5. **Some examples have no numeric payoffs.** Sucker/Cheat/Grudger and the vampire bats get only an ordering of outcomes; the war of attrition gets no numbers at all. Dawkins ran his own Sucker/Cheat/Grudger simulation but reports only what happened, not numbers.

## A. Aggression (Ch5, PDF 89–111)

### A1. Hawk vs Dove (PDF 93–95, bk 70–72)
- **Strategies:**
  - A Hawk always escalates, and retreats only when seriously injured.
  - A Dove only displays, and backs down if the other escalates.
  - Neither can tell the opponent's type beforehand, and neither remembers past fights.
- **Payoffs:** win +50, lose 0, serious injury −100, wasted time −10.

  | Pairing | Result |
  |---|---|
  | Dove vs Dove | Winner +40, loser −10; average +15 |
  | Hawk vs Dove | Hawk +50, Dove 0 |
  | Hawk vs Hawk | Average (50 − 100) / 2 = −25 |

- **Result:**
  - Neither pure strategy is an ESS.
  - The stable mix is 7/12 Hawk and 5/12 Dove. Both then average 6¼, well below the all-Dove 15.
  - The best mix for the group would be 1/6 Hawk, giving 16⅔. An ESS is not the best outcome for the group.
  - A "conspiracy of doves" gets invaded by a single Hawk. The book's human analogy is price-fixing garages.
- **Mixed strategy (PDF 96, bk 73):**
  - The same ESS arises if every individual plays Hawk with probability 7/12 in each contest.
  - The randomness must be unpredictable. A simple sequence, such as 7 Hawk plays then 5 Dove, can be exploited.
- **What a simulation needs:**
  - random pairwise meetings;
  - a shared resource;
  - an injury cost larger than the prize, plus a small time cost;
  - no recognition of opponents and no memory;
  - inheritance of the strategy, or of each individual's P(hawk).

### A2. Extended strategies (PDF 97–98, bk 74–75; endnote PDF 306)
- **Retaliator:** plays Dove, and escalates only if attacked.
- **Bully:** plays Hawk until hit back, then flees.
- **Prober-retaliator:** a Retaliator that sometimes tries a brief escalation. It keeps escalating if not opposed, and goes back to display if the opponent fights.
- **Main text:**
  - Hawk and Bully invade Dove.
  - Dove and Bully invade Hawk.
  - Hawk invades Bully.
  - Retaliator looks stable, but Dove ties with it and drifts in, which lets Prober-retaliator, Hawk and Bully gain ground.
- **Correction:** see the corrections above. The true ESS is a Hawk/Bully mixture.
- **What a simulation needs:**
  - contests of several turns, so a move can react to the opponent's escalation;
  - mutation or drift, so neutral invaders can enter;
  - a time cost;
  - a probing option.

### A3. War of attrition (PDF 99–100, bk 76–77)
- **Setting:** no injury, only display. Time is the currency: both pay the time, and one gets the resource.
- **Results:**
  - A fixed limit on how long to wait is unstable, because a mutant who waits slightly longer always wins.
  - Two equal waiters give up together.
  - The ESS is to wait an unpredictable time, averaging the resource's true value, with no sign of when the individual will quit.
  - Any "tell" (such as a whisker flicker) gets exploited, so a poker face evolves.
  - A bluffing signal is unstable, cycling with calling the bluff. Honest signalling is unstable too.
- **What a simulation needs:**
  - an ongoing time cost and a resource of known value;
  - private random giving-up times, with how long each will wait kept hidden;
  - any readable signal must be exploitable.

### A4. Asymmetric contests and ownership: resident vs intruder (PDF 100–103, bk 77–80; endnote PDF 308)
- **Three kinds of asymmetry:**
  - size or weapons;
  - how much winning is worth (an old male has less to lose);
  - an arbitrary one, such as who arrived first.
- **The convention:**
  - "If resident, attack; if intruder, retreat" is an ESS even when owning gives no real advantage.
  - It means no injuries and no wasted time, and each side wins half the time. It settles disputes the way a coin toss would.
  - The mirror rule, "intruder wins", is also stable in principle, but self-destructive.
  - Which convention takes over depends on which one is the majority first.
  - Rebels do worse: a pure Hawk, or someone playing the reverse convention.
- **Evidence:**
  - Tinbergen's sticklebacks: moving the test tubes between nests flipped which fish attacked (PDF 102–103).
  - Davies's speckled-wood butterflies (endnote PDF 308): whoever was released first into a sunny patch was treated by both as the owner. When both were tricked into thinking they owned it, they fought long and hard.
- **Dawkins's reading:** territoriality may simply be this ESS, with no benefit to the group needed.
- **What a simulation needs:**
  - an ownership or arrival-order tag that both sides can see;
  - a strategy that depends on that tag;
  - a costly fight option.

### A5. Paradoxical strategies (PDF 103–104, bk 80–81; endnotes PDF 308–309)
- **The rule:** "pick fights with larger opponents, flee from smaller ones."
- **When it is stable:** only if serious injury is very costly and paradoxical players heavily outnumber sensible ones. Sensible players are then attacked ferociously.
- **Zone of attraction:** once sensible players pass a critical share, the population moves to the sensible ESS.
- **Real cases:** Mexican social spiders (*Oecobius*), where residents flee from intruders; gulls on a flagpole.
- **Within one lifetime (endnote PDF 309, bk 286):** the book calls this a developmentally stable strategy (DSS). Baldwin & Meese's pigs in a Skinner box: the dominant pig works the lever and the subordinate one eats. A convention like an ESS can arise within a single lifetime by learning.
- **What a simulation needs:** a size or strength signal, and an injury cost that grows with the opponent's size.

### A6. Memory, pecking orders and recognising individuals (PDF 104–105, bk 81–82; endnote PDF 309)
- **Crickets:** they keep only a general memory.
  - A recent winning streak makes a cricket more hawkish; a losing streak makes it more dovish.
  - A rank order forms without anyone recognising anyone.
  - Alexander's model-cricket experiment is cited.
- **Hens and monkeys:** they recognise individuals and learn their place.
  - They act more dovish towards whoever has beaten them.
  - In stable flocks fighting drops and egg-laying rises.
- **Dawkins's view:** a pecking order has no purpose of its own. It falls out of ESSs in uneven contests where individuals recognise and remember each other.
- **What a simulation needs:**
  - cricket version: an internal "recent wins" value that changes aggressiveness;
  - hen version: a per-pair memory of who beat whom.

### A7. Conflict between species (PDF 106–107, bk 83–84)
- Lions don't hunt lions, because cannibalism invites retaliation, as with Hawk.
- Antelopes run rather than fight, because the gap between the species is large.
- Expect conditional rules such as "if smaller, run away".

### A8. Why not kill every rival? (PDF 91, bk 68)
- Killing rival B may help rival C more than you.
- Fighting costs time, energy and injury risk.
- Waiting may pay better. No numbers given.

### A9. Gene-level ESSs (PDF 107–110, bk 84–87)
- **The rowing-crew analogy:**
  - All-English and all-German crews are both stable, because a lone speaker of the other language is penalised.
  - A crew of four left-handers and four right-handers is a stable balanced mix.
  - Minority candidates lose, which looks like group selection but isn't.
- **Evolutionarily stable sets:**
  - A gene pool can be an "evolutionarily stable set" that resists invasion.
  - There may be several stable plateaux, and a population can jump between them.
- **What a simulation needs:** fitness that depends on how common the other strategies are.

## B. Deception in signals (end of Ch4, PDF 87–88, bk 64–65)
- Expect lying wherever interests differ. Examples:
  - false alarm calls;
  - the anglerfish's lure;
  - bee orchids;
  - *Photuris* fireflies that mimic *Photinus* females and eat the males;
  - children lying to parents.
- No payoffs; this motivates the signalling models in A3, C3, C5 and D5.
- **Gambling metaphor** (PDF 78–80, bk 55–57):
  - an animal weighs the stake, the odds and the prize;
  - genes build in rules of thumb, so animals behave as if they had done the sums.

## C. Kin and family (Ch6–8, PDF 111–162)

### C1. Kin selection and Hamilton's rule (Ch6, PDF 112–122, bk 89–99)
- **Relatedness r:**

  | Relationship | r |
  |---|---|
  | Identical twin | 1 |
  | Full sibling, parent–child | ½ |
  | Uncle, aunt, nephew, niece, grandparent, half-sibling | ¼ |
  | First cousin | ⅛ |
  | Second cousin | 1/32 |

- **The rule (PDF 116, bk 93):**
  - A rare gene for helping spreads if the helper saves more than 2 siblings, or 4 half-siblings, or 8 first cousins.
  - In general: help when the cost to yourself is less than the benefit to the other times r.
  - Dawkins writes it as a sum: net benefit = (benefit − risk) to self, plus r × (benefit − risk) for each relative.
- **Worked example (PDF 120):**
  - The finder of 8 mushrooms (each worth +6) has a brother (r ½), a cousin (r ⅛) and a stranger (r 0) nearby.
  - Keeping quiet scores +18. Calling the others scores 12 + 6 + 1.5 = +19.5, so call.
- **Adjustments:**
  - age and life expectancy;
  - how certain the relationship is. Parental care is the surest case, which is why it is commoner than help between siblings.
  - What counts is estimated relatedness (for example 0.275 for uncertain half- or full siblings), not true relatedness.
- **Recognising kin:**
  - **Rules of thumb:** resemblance; being in the same place; or "be nice to anyone you meet" in small groups (dolphin rescues, chick calls).
  - **The rules can be exploited:** "be nice to any small bird in your nest" is what cuckoos use.
  - **Lion prides:** Bertram estimated relatedness in a pride at 0.22 for males and 0.15 for females; Packer & Pusey dispute this (endnote PDF 318).
- **The green-beard effect (PDF 112, bk 89):** one gene for a visible label, plus a gene for helping anyone with that label. Possible in theory, unlikely in practice.
- **What a simulation needs:**
  - a relatedness table (from a family tree or inherited markers);
  - a cost and a benefit for each act;
  - a way to recognise kin, or rules based on place.
  - If agents can't tell kin apart, place-based rules need offspring to stay near their parents.

### C2. Guillemot egg-sitting (PDF 125–126, bk 102–103)
- Communal egg-sitting is invaded by cheats who lay extra eggs and don't sit.
- Punishing them by sitting on one egg only fails, because a sitter can't tell its own egg.
- The only ESS: recognise your own egg and sit only on it.
- Herring gulls don't recognise their eggs, but do recognise their chicks, which wander.

### C3. Cuckoos and the arms race with their hosts (Ch6 PDF 126–127; Ch8 PDF 155–158; Ch13 PDF 271–274, bk 248–251)
- **The race itself:**
  - Hosts evolve egg markings so they can spot foreign eggs; cuckoos evolve mimicking eggs.
  - Better liars and sharper detectors both spread.
- **Cuckoo chick behaviour (Ch8):**
  - Ejecting the host's eggs.
  - Honeyguide chicks slashing their foster siblings.
  - Zahavi's idea: a chick screams to attract predators as blackmail. It pays because the foster mother loses more than the chick.
- **Ch13:**
  - The cuckoo chick's red gape works like an addictive super-stimulus.
  - The life/dinner principle: hosts don't evolve full resistance, because a failed cuckoo chick loses its life while the host loses only one brood, and resistance has costs.

### C4. Family planning: bearing vs caring, clutch size (Ch7, PDF 132–145, bk 109–122)
- **Bearing vs caring:** pure caring can't be an ESS, because "bearers" invade. Caring is stable only mixed with some bearing.
- **Clutch size (Lack):** each parent picks the clutch size that gives the most surviving young, not what's best for the group.
- **Group-selection view rejected:**
  - Wynne-Edwards's group-selection view is rejected.
  - Red grouse outcasts "wait and hope" when the chance of inheriting a territory beats fighting for one. Holding a territory vs floating is a gamble.
- **"Beau Geste" starlings:** reactions to crowding are selfish. Starlings may sing to sound like more birds, so neighbours lay fewer eggs.
- **What a simulation needs:**
  - a resource that limits breeding;
  - a trade-off between number and quality of young;
  - no safety net;
  - optionally, cues of crowding.

### C5. Parent–offspring conflict, sibling rivalry, weaning (Ch8, PDF 146–162, bk 123–139)
- **The basic conflict:**
  - A parent is equally related to all its young (½), so equal investment suits it, adjusted for life expectancy.
  - Each child values itself twice as highly as a sibling. It should grab extra until the cost to its siblings, including their future children, is more than twice its own gain (PDF 151, 161).
- **Weaning:** the mother wants to stop once the current child has had its fair share. The child wants to go on until the cost to its siblings is more than twice its own gain. They fight over the gap between.
- **Begging:**
  - Chicks are tempted to exaggerate hunger.
  - Loudness can't come back down, because the quieter chick gets less.
  - Predators and energy costs limit it.
- **Runts:**
  - Below a threshold of expected benefit, a runt should give up (PDF 153).
  - Mothers lay one spare egg as insurance.
- **Who wins:**
  - Alexander's claim that parents always win is rejected (PDF 158–161).
  - Endnote PDF 322: Charnov's case where the mother does win: a young monogamous female doesn't mind whether she stays to help or leaves, but her mother does.
- **Grandparents:** helping by grandparents, and menopause (PDF 149–150).
- **What a simulation needs:**
  - limited parental food;
  - begging signals;
  - hidden true hunger;
  - sibling relatedness;
  - a benefit from feeding that changes with age.

### C6. Sex ratio, Fisher's ESS (Ch9, PDF 166–168, bk 143–145)
- 50:50 is an ESS, measured as equal *investment* in sons and daughters.
- Three daughters per son, when sons cost three times as much, is also stable (elephant seals).

### C7. Haplodiploidy and worker sterility (Ch10, PDF 194–203, bk 171–180)
- **Relatedness:** in ants and bees, sisters are related by ¾, brothers by ¼, and the mother's own offspring by ½.
- **Queen vs workers:**
  - The queen prefers a 1:1 investment ratio; workers prefer 3 sisters to 1 brother.
  - Trivers & Hare's data on 20 ant species fit 3:1, so workers win.
  - Slave-making ants should come closer to 1:1.
- **Endnotes (PDF 337–344):**
  - naked mole rats;
  - inbreeding in termites;
  - the "hollow tree" argument for helping at the nest;
  - disputes from Alexander & Sherman and from Grafen.

## D. Sex (Ch9, PDF 163–188, bk 140–165)

### D1. How two sexes arose (PDF 164–166; endnote PDF 323–324)
- Starting from equal sex cells, an honest "large investment" strategy opens the door to a "small investment" exploiter, which becomes sperm.
- Anything in between does worse, so the two drift apart.
- Generally: any two classes forced to mate with each other, which differ slightly in the payoff from competing vs caring for young, will diverge.

### D2. Desertion, the "cruel bind" and the Concorde fallacy (PDF 169–173, bk 146–150)
- **Trivers's view:**
  - The partner left with the baby is in a cruel bind, so it pays to desert first.
  - A deserted female may abort (the Bruce effect) or try to fool a new male.
  - Males may kill stepchildren (lions) or demand long courtships.
- **Fish:** Carlisle's idea that external fertilisation favours care by males ("who spawns first"). Ridley's review didn't support it (endnote PDF 327).
- **The Concorde fallacy (PDF 173):**
  - Trivers thought past investment commits a parent to stay. It doesn't: only future costs and benefits count.
  - Making a male invest before mating deters desertion only if most females play the same game.
- **What a simulation needs:**
  - decisions in sequence;
  - the option to desert;
  - different investment before and after mating;
  - decision rules that ignore sunk costs.

### D3. "Domestic bliss" vs "he-man" (PDF 172–181, bk 149–158)
- **Domestic bliss:**
  - Coyness and long courtships weed out philanderers.
  - Courtship can involve gifts of food or nest-building. Useful costs beat pointless ones such as "dragon-slaying".
  - Males can fake faithfulness, so females are coy with new males but quick with last year's mate.
  - Honest fathers win in the long run.
- **He-man:**
  - Females give up on help from fathers and choose good genes instead.
  - A few males get almost all the matings: among elephant seals, 4% of males account for 88% of matings.
  - Fisher's runaway sexual selection gives things like long tails (PDF 181–182).

### D4. Coy/fast females, faithful/philanderer males (PDF 174–176, bk 151–153; endnote PDF 326)
- **Strategies:**
  - A coy female won't mate without a long, costly courtship. A fast female mates at once.
  - A faithful male courts patiently and stays to raise the young. A philanderer gives up quickly and leaves after mating.
- **Payoffs:** +15 for each child raised, −20 to raise it (shared when both stay), −3 for time spent courting.

  | Pairing | Payoffs |
  |---|---|
  | Coy with faithful | +2 each (15 − 10 − 3) |
  | Fast with faithful | +5 for the female |
  | Fast with philanderer | Female −5, philanderer +15 |
  | Coy with philanderer | 0 each (no mating) |

  A faithful male among all-coy females gets +2, better than a philanderer's 0.
- **The cycle:** fast spreads, then philanderers, then coy females, then faithful males, then round again.
- **Main text equilibrium:** 5/6 coy females and 5/8 faithful males.
- **Corrected (endnote PDF 326):** a knife-edge. The real dynamics cycle forever, like predators and prey.
- **Maynard Smith's guard/desert model (endnote PDF 325):** four stable outcomes:
  - Duck: the male deserts and the female guards.
  - Stickleback: the female deserts and the male guards.
  - Fruit-fly: both desert.
  - Gibbon: both guard.

  Only Duck with Stickleback, or Gibbon with Fruit-fly, can be jointly stable.
- **What a simulation needs:**
  - two roles with two strategies each, which can be inherited;
  - fitness that depends on payoffs;
  - a simulation that runs over time, since the equilibrium isn't stable.

### D5. The handicap principle (endnote PDF 332–336, bk 309–313)
- **Grafen's model:**
  - Males vary in true quality, which females can't see.
  - Males choose how much to advertise based on their quality, and females choose how to respond.
  - The ESS is honest, costly advertising ("believe the males"), with the cost higher for low-quality males.
- **Hamilton's parasite theory:** males honestly advertise their health.
- **What a simulation needs:**
  - hidden quality;
  - a signal whose cost depends on quality;
  - a response rule that can change over time.

## E. Cooperation and reciprocity (Ch10, PDF 189–211; Ch12, PDF 225–256)

### E1. Selfish herd, alarm calls, stotting (Ch10, PDF 190–194, bk 167–171)
- **Hamilton's selfish herd:**
  - Each prey animal wants a smaller "domain of danger", meaning less ground closer to it than to anyone else.
  - So everyone moves towards the middle. Herds form with no cooperation, and the edges are the most exposed.
- **Alarm calls:**
  - kin selection;
  - or selfish reasons: the cave theory (silence the flock so the hawk doesn't notice it) and "never break ranks" (warn everyone so all fly up together).
- **Stotting:** Zahavi's idea that the display is aimed at the predator. It is a contest to look the fittest, so the predator picks someone else.
- **What a simulation needs:**
  - a predator that attacks the nearest prey;
  - positions in space;
  - the options to flee and to call out.

### E2. Sucker / Cheat / Grudger (PDF 206–209, bk 183–186; endnote PDF 344)
- **Setting:**
  - Birds can't reach their own heads, so they groom each other.
  - Mutual grooming is common in birds and mammals. Mice kept alone get sores on their heads.
- **Strategies:**
  - A Sucker grooms anyone.
  - A Cheat accepts grooming but never gives it.
  - A Grudger grooms strangers and anyone who has groomed it, and refuses anyone who ever cheated it.
- **Payoffs:** no numbers, only that being groomed is worth more than the cost of grooming.
- **Results:**
  - Cheats always beat Suckers, so Suckers die out.
  - A rare Grudger loses among Cheats, but wins once Grudgers pass a critical share.
  - Grudger and Cheat are both ESSs. Each has its own "zone of attraction".
  - An ESS that leads to extinction is still an ESS.
- **Dawkins's own simulation:** mostly Suckers, plus Grudgers above the critical share. Suckers crash first, Cheats boom and then fall, and Grudgers take over. While the Suckers last, they feed the Cheats, which puts Grudgers at risk for a while.
- **Endnote (PDF 344):** Mackie notes that Cheat populations are more likely to go extinct, so there could be selection between ESSs.
- **What a simulation needs:**
  - repeated meetings with the same partners;
  - recognising individuals;
  - remembering who cheated;
  - acts whose benefit exceeds their cost;
  - Grudgers starting above the critical share.

### E3. Cleaner fish and other mutual help (PDF 205–210, bk 182–187)
- **Cleaner fish:**
  - Cleaners have stripes and a dance, and big fish don't eat them.
  - Mimics that look like cleaners bite chunks out of clients.
  - The system holds because cleaners keep territories and clients come back.
- **Other examples:**
  - ants and aphids;
  - lichens;
  - hyena packs;
  - birds taking turns to lead a V-shaped flight.
- **Trivers's idea:** envy, guilt and gratitude evolved to detect cheats. "Subtle cheats" pay back slightly less than they get.

### E4. The Prisoner's Dilemma, one-off and repeated (Ch12, PDF 226–231, bk 203–208)
- **One-off payoffs (dollars):**

  | Outcome | Payoff |
  |---|---|
  | Both cooperate (reward) | +$300 each |
  | Both defect (punishment) | −$10 each |
  | Defecting on a cooperator (temptation) | +$500 |
  | Being defected on (sucker) | −$100 |

  The order must be temptation > reward > punishment > sucker, and (temptation + sucker) / 2 must not exceed reward.
- **One-off vs repeated:**
  - In a single game, Always Defect is the rational choice.
  - In the repeated game, the same players meet an unknown number of times, so trust and retaliation are possible.

### E5. Axelrod's tournaments (PDF 231–239, bk 208–216)
- **Scoring:** reward 3, temptation 5, punishment 1, sucker 0. Each match is 200 rounds, and every strategy meets every other, and itself.
- **Round 1:**
  - 14 entries plus Random.
  - Tit for Tat (Rapoport) won with about 504.5 points per match, 84% of the all-cooperate benchmark of 600.
  - The 8 "nice" strategies took the top 8 places.
  - The best nasty strategy was Graaskamp, at 66.8%.
  - Friedman, the same as Grudger, came next to worst among the nice ones.
- **Round 2:**
  - 62 entries plus Random, with match length unknown.
  - Tit for Tat won again, at about 96%.
  - Maynard Smith entered Tit for Two Tats. It would have won round 1, but in round 2 subtle nasty strategies preyed on it. The results depend on who else enters.
- **Round 3, the "ecological" run:**
  - 1,000 generations, with points paid out as offspring.
  - Most nasty strategies were extinct by generation 200.
  - Harrington, the longest-lived nasty one, peaked around generation 150.
  - Tit for Tat came top in 5 of 6 runs; another nice strategy that hits back won the sixth.
- **Strategies:**

  | Strategy | How it plays |
  |---|---|
  | Tit for Tat | Cooperate first, then copy the partner's last move |
  | Tit for Two Tats | Defect only after two defections in a row |
  | Naive Prober | Tit for Tat with occasional random defections (about 1 in 10). It can lock into alternating 5 and 0, averaging 2.5, which is why the rule (T + S) / 2 < R matters. |
  | Remorseful Prober | Like Naive Prober, but allows one free hit after its probe is punished. Needs longer memory. |
  | Suspicious Tit for Tat | Defects on the first move, then plays Tit for Tat |
  | Always Defect, Always Cooperate, Random | As named |
  | Grudger (Friedman) | Cooperates until defected on once, then defects for ever |

- **Terms:**
  - nice: never the first to defect;
  - forgiving: short memory;
  - envious: tries to beat the partner instead of maximising its own score. Tit for Tat can never beat its partner in a single match.
  - Winning strategies are nice, forgiving, quick to hit back, and not envious.

### E6. How stable Tit for Tat is (PDF 239–243, bk 216–220)
- **Not a strict ESS:**
  - Always Cooperate behaves the same among nice players, so it drifts in, and then Always Defect invades.
  - Axelrod calls Tit for Tat "collectively stable". Always Defect is too, so there are two stable outcomes.
- **Boyd & Lorberbaum:** a mix of Tit for Two Tats and Suspicious Tit for Tat can invade Tit for Tat.
- **Knife-edge:** Tit for Tat beats Always Defect only above a critical share.
- **Clustering:**
  - Neighbourhoods of relatives or nearby players let Tit for Tat cross that threshold.
  - Clusters don't help defectors, who only hurt each other.
- **What a simulation needs:** structure in space or in families, and a small starting cluster.

### E7. Non-zero-sum games and the shadow of the future (PDF 243–248, bk 220–225)
- **Zero-sum vs non-zero-sum:**
  - Chess is zero-sum.
  - Divorce lawyers turn a divorce into a zero-sum game for their clients and a non-zero-sum one for themselves.
  - The 1977 Sunderland/Bristol/Coventry football match became non-zero-sum once news of another score arrived.
- **The shadow of the future:**
  - It must be long, and players must not know when the game ends.
  - If the number of rounds is known, working backwards from the end makes everyone defect.
  - The shorter players expect the game to last, the nastier they get.

### E8. The First World War's "live and let live" (PDF 248–251, bk 225–228)
- **Why it arose:**
  - The same units faced each other for months.
  - The generals wanted fighting, which made it a true Prisoner's Dilemma for the soldiers.
- **Local conventions:**
  - Units copied Tit for Tat, for example "three Tits for a Tat": three German replies for every British grenade.
  - They showed they could retaliate by shooting near the enemy rather than at them.
  - They apologised after accidental shots.
  - Predictable rituals, such as an evening gun, signalled peace.
- **What a simulation needs:**
  - stable local pairs of opposing units;
  - retaliation the other side can see;
  - cheap ways to show capability.

### E9. Prisoner's Dilemmas in nature (PDF 252–253, bk 229–230)
- Bacteria turn nasty in a wounded host, because the host's future is shorter.
- Fig trees abort figs in which the fig wasp laid too many eggs.
- Sea bass pairs take turns being male and female, which looks like Tit for Tat, and uneven pairs split up.
- Dawkins doubts blood donation counts as reciprocity.

### E10. Vampire bats (PDF 253–256, bk 230–233)
- **Wilkinson's study:**
  - In roosts, bats that fed well spit up blood for bats that didn't.
  - Most of this is mothers feeding young, or between kin, but non-kin share too, if they roost together often.
- **Why it pays:**
  - The giver loses little from a lucky night's surplus; the receiver gains a lot, since bad nights risk starvation.
  - The exchange was measured in hours of life.
  - The temptation is not to share on a lucky night.
- **Experiment:**
  - A starved bat was returned to a roost holding bats from two caves.
  - 12 of 13 donations came from old roostmates from its own cave, with odds of this by chance under 1 in 500.
  - So bats recognise individuals.
  - (A scanning error garbles the total count in the PDF.)
- **What a simulation needs:**
  - needs that differ between individuals and from day to day;
  - giving that costs less than it's worth;
  - stable groups;
  - recognising individuals.

## F. Memes and the extended phenotype

### F1. Memes (Ch11, PDF 212–224, bk 189–201)
- **What they are:** memes are replicators that spread by imitation. Their "survival" means surviving in the pool of memes.
- **Meme complexes:**
  - Memes compete for attention.
  - Memes that support each other form complexes, such as an organised church combining belief in a god, hell-fire and celibacy.
  - A meme pool tends to settle into a stable set that new memes find hard to invade.
- **Links to game theory:**
  - The "conspiracy of doves" comes back (PDF 223).
  - Conscious foresight might let humans resist selfish replicators (PDF 223–224).
- **What a simulation needs:**
  - transmission by imitation;
  - limited attention, or a limited number of "slots";
  - a benefit that depends on which other memes are held.

### F2. The extended phenotype, manipulation and conflict inside the genome (Ch13, PDF 257–289, bk 234–266)
- **Genes cheating inside the body:** segregation distorters such as the mouse *t* gene, carried by up to 95% of sperm.
- **Parasites manipulating hosts:**
  - flukes thicken snail shells;
  - *Nosema* mimics a beetle hormone;
  - *Sacculina* castrates crabs.
- **Shared exit routes:**
  - A parasite and host whose genes leave the body the same way, such as bacteria passed on in beetle eggs, have aligned interests, and the parasite tends to become helpful.
  - Different routes keep it harmful, as with algae in hydra.
- **Parasite queens:** in some ants, a parasite queen gets the host workers to kill their own queen (PDF 274–276).

## G. Smaller items
- **Honest signals:** handicap (D5), poker face (A3), begging (C5), stotting (E1).
- **Other items:**
  - Beau Geste starlings (C4).
  - Bruce effect and infanticide (D2).
  - Selection between ESSs (Mackie, endnote PDF 344).
  - Reciprocity vs kinship in lions (Packer & Pusey, endnote PDF 318).
- **On method:**
  - Dawkins says ESS models don't depend on any particular genetic system: "like begets like" is enough (endnote PDF 310).
  - All ESS models are approximations (PDF 332). He recommends checking stability by running the dynamics (Gale & Eaves; Schuster & Sigmund).
- **Digger wasps (endnote PDF 307–308, bk 283–284; Brockmann, Grafen & Dawkins):**
  - **The choice:** a wasp digs her own nest, or enters one another wasp dug. Some nests are abandoned, so entering can pay.
  - **The risk:** a wasp entering can't tell whether the nest is still in use. She may share it for days, then find it sealed with the other wasp's egg.
  - **The critical share:** at one share of entering, digging and entering pay equally. More entering makes burrows scarce, so digging pays; less leaves plenty, so entering pays.
  - **Results:** the New Hampshire population showed a true mixed ESS, where each wasp digs or enters with some probability. The Michigan population didn't.
  - **How costs were measured:** as time budgets of individually marked wasps.

## What a simulation needs across all examples
1. **Population dynamics:**
   - many individuals with heritable strategies, or heritable chances of each move;
   - reproduction in proportion to payoff;
   - mutation or drift, so neutral invaders appear;
   - checking stability by running the dynamics, not only by maths. Two of the book's own published equilibria were wrong.
2. **Payoffs that depend on how common each strategy is**, so mixed equilibria and two-way stability can show up.
3. **A shared, contested resource** worth V. Fights cost injury (more than V) and time, and how long each side will wait is unpredictable.
4. **Random pairwise meetings**, with contests of several turns.
5. **Repeated meetings with the same partner**, an unknown end, and a long shadow of the future.
6. **Recognising individuals and remembering them:**
   - last move (Tit for Tat);
   - all past defections (Grudger);
   - longer history (probers);
   - or general memory of recent wins (crickets).
7. **A visible asymmetry** (who arrived first, size, ownership), rules that use it, and injury costs that make ignoring it expensive.
8. **Relatedness**, from family trees or inherited similarity.
9. **Structure in space or limited spreading out**, so local clusters form.
10. **Two sexes or roles** with different investment, plus decisions to mate and to desert. Decision rules must ignore sunk costs.
11. **Parents and young:** limited parental food, begging signals and hidden true need.
12. **Needs that vary over time**, so a gift is worth more to the receiver than it costs the giver.
13. **Signals and deception:**
    - hidden quality or intent;
    - a response rule that can change over time;
    - signal costs that depend on quality;
    - mimicry.
14. **Predators and herd shape:** a predator that attacks the nearest prey, and positions in space.
15. **Two interacting species or lineages** with unequal costs of failure.
16. **A random game length**, and optionally a signal that the end is near.
17. **Optional:**
    - an environment that changes payoffs;
    - a meme layer, with imitation and complexes that fit together.
