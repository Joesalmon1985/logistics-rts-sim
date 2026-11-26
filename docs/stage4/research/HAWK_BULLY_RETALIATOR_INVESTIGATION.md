# Hawk/Bully against Retaliator: why L2 ends in Retaliator

Investigation of the open owner finding from M2 (L2, [STRATEGY_BENCHMARKS.md](../STRATEGY_BENCHMARKS.md) Amendment A2). Branch `unity/lab-hb`, 26 Nov 2026.

**The question.** The book's corrected statement (endnote PDF 306; see [SELFISH_GENE_NOTES.md](SELFISH_GENE_NOTES.md) A2) is that Retaliator is not an ESS and that a dynamic simulation found a stable Hawk/Bully mixture instead. L2 converges to Retaliator under the default drift. Why?

**The owner's rule.** L2's model is not changed to reproduce the book. L2's semantics and its golden hash (`815ddf13e8116bc9`, seed 1) are unchanged. Everything below is new analysis code and new runs.

## 1. Short answer

1. **The payoff matrix is the main cause.** L2 is not Maynard Smith and Price's move-based contest. It is a different, simpler turn-based contest, reduced to a 4x4 (or 5x5) expected-payoff matrix. Under its turn rules a Retaliator's opening display against an escalation costs nothing. One turn later it is in the same fight with a Hawk, or sees the same Bully retreat, as a Hawk would be. So **R earns exactly what H earns against H and against B, for every value of V, I, T and S** (test `TurnRules_RetaliatorIsPayoffEquivalentToHawk_AgainstHawkAndBully`). A Retaliator mutant therefore ties with Hawks inside any Hawk/Bully mix, and wins against itself. So a pure Hawk/Bully mix can never be an ESS in L2. In Maynard Smith and Price's Table 1 the Retaliator pays for letting the opponent strike first (R vs H −22.3 < H vs H −19.5; R vs B 57.1 < H vs B 74.6), and the Hawk/Bully mix is a strict ESS.
2. **L2 does have a Hawk/Bully-like ESS, but it is weak.** It is H 35/57, D 1/57, B 21/57 (0.614 / 0.018 / 0.368). Its only defence against Retaliators comes from its 1.75% Doves: R loses 35 × 1/57 = 0.614 payoff units per contest. A basin-boundary saddle lies only 2.4% Retaliator away from it.
3. **Mutation and drift push the population over that saddle.** Uniform mutation keeps a standing Retaliator share in the population. At the default selection strength this share is larger than 2.4%. With mutation, the replicator therefore has **one** attractor, Retaliator-dominant (R 0.718), from every start. In finite populations, drift alone carries a 1% Retaliator group across the saddle in 2 of 5 runs.
4. **Initial distribution.** Without mutation both L2 and the published table are **bistable**. The uniform start of L2's agent runs lies in the Retaliator basin for L2's matrix: 80% of grid starts end in R. For the published table it lies in the Hawk/Bully basin: 63% of grid starts end in H/B.
5. **What does not explain it:** population size, simulation duration, the selection mechanism and the encounter rules. Run under L2's own engine (same N, k, w0, mutation, seeds and start), the published table goes to the Hawk/Bully mix in 5 of 5 seeds. L2's table goes to Retaliator in 5 of 5 seeds. Expected payoffs instead of realised contests change nothing. Durations from 2,000 to 100,000 generations change nothing.
6. **Even the published table does not make Hawk/Bully the global outcome.** With all five strategies (Prober included) and a uniform start, Maynard Smith and Price's own Table 1 goes to **Retaliator** in the replicator and in the agents. This is the 1973 paper's own conclusion. Hawk/Bully is an *alternative* ESS, reached from Hawk-rich starts. So "Hawk/Bully is the corrected equilibrium" is itself a statement about which basin a population starts in, not a global result.

## 2. Method

Code (all deterministic: integers, Q30 fixed point, exact rationals, keyed `Rng` streams; nothing in L2 changed):

- `Assets/God/Sim/Lab/ContestStudy.cs`:
  - the four matrices (below);
  - exact rest points by support enumeration in rational arithmetic (`Rational.cs`, big integers);
  - exact first- and second-order invasion fitness for every absent strategy;
  - negative (semi)definiteness inside the support;
  - replicator and agent runs on the existing `Replicator` and `AgentEngine`;
  - a basin census over a simplex grid.
- Runner: `lab invade` and `lab sweep` (`DotNet/God.Runner/LabStudyCommand.cs`). Every table below comes from one of these commands. The commands are listed with each table.
- Tests:
  - `Assets/God/Tests/EditMode/HawkBullyStudyTests.cs` (EditMode, and compiled into `DotNet/God.Sim.Tests`): matrix values, invasion verdicts, replicator fixed points, the basin census and the matched agent comparison;
  - `DotNet/God.Runner.Tests/LabRunnerTests.cs`: `LabInvade_*`, `LabSweep_*`.

Matrices (`--matrix`):

| Name | What | Strategies |
|---|---|---|
| `l2` | L2's exact turn-rule matrix (STRATEGY_BENCHMARKS 4.2, `V 50, I 100, T 10, S 10, MaxTurns 8, q 100`). What L2's agents play | H, D, R, B |
| `l2p` | the same with the Prober (amended A1 cells) | H, D, R, B, P |
| `msp` | Maynard Smith and Price (1973), Table 1, reordered (Mouse = D) | H, D, R, B, P |
| `msp4` | its H/D/R/B block | H, D, R, B |

Dynamics:
- **Replicator.** L2's discrete fixed-point replicator, `w0 = 500` unless stated, mutation `x' = (1 − mu) x + mu/K`. No drift.
- **Agents.** L2's engine: random pairing, `k` contests per generation, Wright-Fisher selection on `w0 + payoffs`, mutation to a uniformly chosen other strategy. Defaults as L2: `k = 10, w0 = 500, mu = 2 permille`. The game is realised turn-rule contests for `l2` and `l2p`, as in L2, and expected payoffs for `msp`.

Shares in the agent tables are means over generations `gens/2..gens`, averaged over seeds 1–5.

Starting distributions (`--starts`):

| Start | H, D, R, B (with P: H, D, R, B, P) |
|---|---|
| uniform | 0.25 each (0.2 each) |
| hawk | .70 .10 .10 .10 (.60 .10 .10 .10 .10) |
| ret | .10 .10 .70 .10 (.10 .10 .60 .10 .10) |
| hb | .58 .02 .02 .38 (.57 .02 .02 .37 .02) |

Basins: **H/B** if H + B ≥ 0.8; **R** if R + D (+ P) ≥ 0.8; otherwise **mixed**.

## 3. The payoff matrices

L2 (`lab invade --matrix l2p`): row player's expected payoff, exact enumeration of the turn rules.

| row \ col | H | D | R | B | P |
|---|---|---|---|---|---|
| H | -25 | 50 | -25 | 40 | -25 |
| D | 0 | 15 | 15 | 0 | 13.5 |
| R | **-25** | 15 | 15 | **40** | 11 |
| B | -10 | 50 | -10 | 15 | -10 |
| P | -25 | 18.5 | 11 | 40 | 7.4 |

Maynard Smith and Price 1973, Table 1 (`--matrix msp`): averages over 2,000 simulated contests per pairing, as published, reordered.

| row \ col | H | D (Mouse) | R | B | P |
|---|---|---|---|---|---|
| H | -19.5 | 80 | -18.1 | 74.6 | -18.9 |
| D | 19.5 | 29 | 29 | 19.5 | 17.2 |
| R | **-22.3** | 29 | 29 | **57.1** | 23.1 |
| B | 4.9 | 80 | 11.9 | 41.5 | 11.2 |
| P | -20.1 | 56.7 | 26.9 | 59.4 | 21.9 |

The bold cells are the decisive difference. In L2, R vs H equals H vs H and R vs B equals H vs B. In the published table both are lower. Neither matrix is a rescaling of the other. In L2 a display costs nothing until a display block ends the contest; injury happens only in a fight (two consecutive mutual escalations); and a one-sided escalation does no harm. In the 1973 model every dangerous move risks injury (probability 0.1) and costs a scratch (−2) otherwise, and long contests lose a time bonus of up to +20. So the side that answers a blow, rather than striking first, pays for it. Varying L2's parameters (`--rule S=0, 5, 20; T=0; I=200; V=80`) never makes a Hawk/Bully mix an ESS. In every case a Retaliator ties at first order and invades at second order.

## 4. Invasion matrix

Exact (`lab invade`). A cell shows "first-order fitness" and, after a tie, "/ second-order fitness". Units are payoff per contest. A negative value means the mutant loses.

**L2 (H, D, R, B):**

| Candidate | Mean | + H | + D | + R | + B | Verdict |
|---|---|---|---|---|---|---|
| R | 15 | −40 | 0 / 0 neutral | – | −25 | neutrally stable: Dove drifts in |
| R .9, D .1 (edge) | 15 | −32.5 | – | – | −19 | neutrally stable along the R/D edge |
| R .55, D .45 (edge) | 15 | −6.25 | – | – | **+2 invades** | Bully invades once d > 25/60 |
| H .625, B .375 | −0.625 | – | **+0.625 invades** | 0 / **+34.4 invades** | – | not stable |
| **H .614, D .018, B .368** | 0.263 | – | – | −0.614 | – | **ESS** (weak margin) |
| H .583, D .028, R .024, B .365 | 0.781 | – | – | – | – | interior saddle (basin boundary) |
| H .583, D .417 | 6.25 | – | – | −14.6 | **+8.75 invades** | not stable |

With the Prober (`l2p`), pure R also resists P at −4, and the H/D/B ESS resists P at −0.553. Pure P is invaded by D (+6.1) and R (+3.6).

**Maynard Smith and Price, Table 1 (H, D, R, B, P):**

| Candidate | Mean | + H | + D | + R | + B | + P | Verdict |
|---|---|---|---|---|---|---|---|
| R | 29 | −47.1 | 0 / 0 neutral | – | −17.1 | −2.1 | neutrally stable: Mouse drifts in |
| R .9, D .1 | 29 | −37.3 | – | – | −10.3 | **+0.88 invades** | Prober invades the R/D edge |
| **H .576, B .424** | 20.43 | – | −0.931 | −9.038 | – | −6.795 | **strict ESS** |
| P | 21.9 | −40.8 | −4.7 | **+1.2 invades** | −10.7 | – | not stable (so not "almost an ESS") |
| H .371, R .187, B .443 | 22.40 | – | −1.13 | – | – | **+1.44 invades** | not stable |

The Hawk/Bully point h = 331/575 = 0.5757 follows from Table 1 alone. On these numbers the Hawk/Bully mix is a strict ESS and Retaliator is only neutrally stable, in the published table itself. Note how thin the Mouse margin is: −0.93 per contest, on averages of 2,000 noisy contests (see section 8).

**Dynamic invasion tests.**
- Replicator, 1% of the mutant added, 20,000 generations, no mutation:
  - **L2:** 1% R added to H/B (.625/.375) takes over (R → 1.0); 1% D added moves it to the H/D/B ESS. At the H/D/B ESS, 1% R dies out, but 3% R crosses the saddle and R wins (test `InvasionRuns_OnePercentRetaliator`). At pure R, H and B die out and D stays where it was put (neutral).
  - **Published table:** 1% of D, R or P at the H/B mix all die out, and 3% R dies out too.
- Agents, N = 1000 with 10 mutants, no mutation, 2,000 generations, seeds 1–5:

| Matrix | Candidate | + H | + D | + R | + B | + P |
|---|---|---|---|---|---|---|
| l2 | R | lost 5/5 | lost 5/5 (drift) | – | lost 5/5 | – |
| l2 | H/D/B ESS | – | – | **fixed 2/5**, lost 3/5 | – | – |
| l2p | H/D/B ESS | – | – | **fixed 1/5** | – | lost 5/5 |
| msp | R | lost 5/5 | lost 5/5 | – | lost 5/5 | lost 5/5 |
| msp | H/B ESS | – | lost 5/5 | lost 5/5 | – | lost 5/5 |

## 5. Results tables

**No-drift replicator** (`lab sweep --dynamics replicator --mu 0,2,20 --gens 6000,100000`, w0 500). The final state is identical at 6,000 and 100,000 generations in every row, so duration plays no role.

| Matrix | mu | uniform | hawk | ret | hb |
|---|---|---|---|---|---|
| l2 | 0 | R (R .837 D .163) | **H/B** (H .614 D .018 B .368) | R (.892) | R (.915) |
| l2 | 2 | R (H .013 D .244 R .718 B .025), every start | same | same | same |
| l2 | 20 | mixed (H .19 D .22 R .36 B .23), every start | same | same | same |
| msp4 | 0 | **H/B** (H .576 B .424) | H/B | R (.907) | H/B |
| msp4 | 2 | **H/B** (H .514 B .405) | H/B | R (.783) | H/B |
| msp4 | 20 | mixed (H .32 D .17 R .17 B .35), every start | same | same | same |
| l2p | 0 | R (D .416 R .584) | R | R | R |
| l2p | 2 | R (R .607 D .264 P .094), every start | same | same | same |
| msp | 0 | R (.999) | **H/B** | R | **H/B** |
| msp | 2 | R (R .43 P .49) | **H/B** (H .48 B .41) | R | **H/B** |
| msp | 20 | mixed (P .36 R .22 B .19), every start | same | same | same |

**Basin census** (`lab sweep --basins 20 --gens 20000`): starts on a 1/20 grid with every strategy present (969 starts with 4 strategies, 3,876 with 5).

| Matrix | w0 | mu 0: R / H/B | mu 2: R / H/B / mixed | mu 20 |
|---|---|---|---|---|
| l2 | 500 | 780 / 189 | 969 / 0 / 0 | all mixed |
| l2 | 50 | 780 / 189 | 969 / 0 / 0 | all R |
| msp4 | 500 | 355 / 614 | 333 / 636 / 0 | all mixed |
| msp4 | 50 | 359 / 610 | 355 / 614 / 0 | 328 / 641 |
| l2p | 500 | 3664 / 212 | 3876 / 0 / 0 | all mixed |
| msp | 500 | 3199 / 677 | 3452 / 424 / 0 | all mixed |
| msp | 50 | 3206 / 670 | 3232 / 644 / 0 | all R |

**Agents, L2's engine** (`lab sweep --matrix l2|msp4 --mu 0,2,20 --agents 100,200,1000`, k 10, w0 500, 6,000 generations, seeds 1–5). Each cell gives the mean shares and the basins of the five seeds (R / H/B / mixed).

| Start | N | mu | L2 (`l2`) | Published table (`msp4`) |
|---|---|---|---|---|
| uniform | 100 | 2 | R .85 D .08 H .05 B .03; 5/0/0 | H .31 B .23 R .42; 2/3/0 |
| uniform | 200 | 0 | R .80 H .13 B .07; 4/1/0 | H .58 B .42; 0/5/0 |
| uniform | 200 | 2 | **R .865** D .11 H .01 B .01; **5/0/0** | **H .561 B .414**; **0/5/0** |
| uniform | 200 | 20 | R .71 D .21 H .03 B .05; 5/0/0 | R .51 H .17 B .20 D .12; 0/0/5 |
| uniform | 1000 | 2 | R .805 D .19; 5/0/0 | H .557 B .417; 0/5/0 |
| hawk | 200 | 0 | H .635 B .365; 0/5/0 | H .579 B .421; 0/5/0 |
| hawk | 200 | 2 | R .870; 5/0/0 | H .563 B .415; 0/5/0 |
| hawk | 1000 | 2 | R .803; 5/0/0 | H .558 B .417; 0/5/0 |
| ret | 200 | 2 | R .792; 4/0/1 | R .56 H .23 B .17; 3/2/0 |
| ret | 1000 | 2 | R .792; 5/0/0 | R .855; 5/0/0 |
| hb | 200 | 0 | H .51 B .30 R .20; 1/4/0 | H .58 B .42; 0/5/0 |
| hb | 200 | 2 | R .893; 5/0/0 | H .537 B .389; 0/4/1 |
| hb | 1000 | 2 | R .828; 5/0/0 | H .559 B .415; 0/5/0 |

The full 36-row tables for each matrix, including mu 0 and 20 for every N and start, come from the commands above. Without mutation, L2 often reaches Hawk/Bully (with the Doves lost by drift) from the hawk and hb starts: 5/5 and 4/5 at N = 200, but only 3/5 and 2/5 at N = 1000. One mutation per 500 births removes that outcome from every start.

**Drift strength, duration, encounter rules (L2, N = 200, seeds 1–5):**

| Variant | uniform start | hb start |
|---|---|---|
| default k 10, w0 500, mu 2 | R .865; 5/0/0 | R .893; 5/0/0 |
| k 1 (more outcome noise per generation), w0 500, mu 2 | R .59 H .17 D .17 B .08; 3/0/2 | R .63; 2/0/3 |
| k 10, w0 100 (much stronger selection), mu 2 | H .41 R .38; **1/2/2** | H .57 B .12 R .18; **0/3/2** |
| k 1, w0 100, mu 2 | R .76; 4/0/1 | R .74; 4/0/1 |
| 2,000 / 6,000 / 20,000 generations, mu 2 | R .866 / .865 / .884; all 5/0/0 | R .849 / .893 / .821; all 5/0/0 |
| expected payoffs instead of realised contests, mu 2 | R .829; 5/0/0 | R .833; 5/0/0 |
| expected payoffs, N 1000, mu 2 | R .802; 5/0/0 | R .789; 5/0/0 |

With Prober (`l2p`, agents, mu 2, N 200 and 1000): R dominates in 39 of 40 seed runs (mean R .75–.91). The published five-strategy table (`msp`, agents) reaches Hawk/Bully only from the hawk and hb starts at N = 1000 (5/5 with mu 0 and with mu 2). From the uniform start it ends in R in every seed at N = 1000, and in 4 of 5 seeds at N = 200 with mu 2. At mu 20 the Prober leads (P .58).

Stronger selection (`w0 100`) reproduces the prototype's earlier observation that Hawk/Bully appears in some N = 200 runs. Seen through the analysis above, this works by making selection against the standing Retaliator share stronger than the mutation that feeds it. It does not come from "more drift".

## 6. Findings, by item checked

| Item | Finding |
|---|---|
| Payoff matrix | **Main cause.** L2's derived matrix makes R equal to H against H and B for all parameters. A Hawk/Bully mix is then never an ESS; R invades it at second order (+34.4). The only Hawk/Bully-like ESS (H/D/B) resists R by 0.614, entirely through its 1.75% Doves. The 1973 matrix makes Hawk/Bully a strict ESS (D −0.93, R −9.04, P −6.80) |
| Strategy implementations | Checked turn by turn: H vs H, H vs R and R vs B as the 4.2 rules say (golden moves in `LabTests`). No bug found. The difference from 1973 is in the rules (no per-move injury risk, free displays, injury only in a fight), not in a coding error |
| Population size | Not the cause. With mutation, N = 100, 200 and 1000 all end in the R basin (or, in 3 of 120 runs, mixed with R largest), from every start. Smaller N only adds noise |
| Selection mechanism | Not the cause in kind. Wright-Fisher on `w0 + payoffs` reaches H/B with the published table. Its strength matters: very strong selection (`w0 100`) sometimes holds L2's weak H/D/B state against mutation |
| Mutation | **Second cause.** Uniform mutation keeps a standing R share above the 2.4% saddle distance. With mu = 2 permille the L2 replicator has one attractor (R .718) from every start, and with mutation (2 or 20 permille) 117 of 120 L2 agent runs end in the R basin and none in H/B; the other 3 are mixed, with R the largest share. The published table keeps its H/B basin under the same mutation |
| Genetic drift | Contributes. Without mutation, drift lets 1% R cross the saddle in 2 of 5 runs (N = 1000), and lets Doves drift along the R/D edge (D fixed in 3 of 5 N = 200 runs from the ret start). Drift is not needed for the R outcome: the no-drift replicator reaches R too |
| Initial distribution | **Decides the basin when there is no mutation.** Both matrices are bistable. L2's uniform start is in the R basin; the published table's uniform start (4 strategies) is in the H/B basin. Even 2% Retaliators in a Hawk/Bully-rich start (hb) lead L2 to R |
| Encounter rules | Not the cause. Realised contests and expected payoffs give the same result (R in 5/5 seeds) |
| Simulation duration | Not the cause. Results are the same at 2,000, 6,000, 20,000 and 100,000 generations |

## 7. How L2 differs from an exact replication

L2 is **not** a reproduction of Maynard Smith and Price (1973) nor of the later dynamic analyses. It was labelled "Derived and invented" from the start, and that label stays. The differences:

| 1973 model (from the paper) | L2 |
|---|---|
| Moves C (conventional), D (dangerous), R (retreat); contestants move alternately | Display, Escalate, Withdraw; simultaneous turns |
| Every D received risks serious injury (p = 0.1); a scratch costs −2 | No injury until a fight (two consecutive mutual escalations), then a coin flip: winner +V, loser −I |
| Win +60, serious injury −100, time bonus 0 to +20 (shorter contests are better) | V = 50, I = 100, display block T = 10, scuffle S = 10 (all chosen by us) |
| Mouse plays C until a preset contest length, so Mouse vs Mouse runs to that length | Dove vs Dove ends after one display block |
| Retaliator and Prober-Retaliator retreat at a preset length | MaxTurns 8, treated as a display block |
| Probe probability 0.05 | 0.1 |
| Payoffs are Monte Carlo means of 2,000 contests | Payoffs are exact expectations |
| Dynamics: none in the 1973 paper (a column-by-column ESS check of the table and a verbal argument) | Replicator and an agent engine of our design |

The `msp` and `msp4` experiments use the published *table*, not the published *contest*. They show what our dynamics do with the 1973 payoffs. They are not a re-simulation of the 1973 contest.

## 8. What remains uncertain

- **Gale and Eaves (1975).** Their letter (Nature 254: 463) is cited by Leimar and McNamara (2023) as the first challenge to the 1973 numbers. I could not read it: the publisher page is paywalled. I therefore cannot say what dynamics, starting frequencies or payoff table they used, or whether they recomputed the contests. A search-engine summary attributes a Hawk/Bully mix of about 0.58/0.42 to them. That matches the 0.576/0.424 that follows from Table 1, but it is **not verified**. I also could not read Maynard Smith's reply.
- **Whether Table 1 is itself in error.** The task framing mentions "errors". From the paper alone I can establish only this: the published Table 1 already contains the Hawk/Bully strict ESS, which the paper's column-by-column check did not report. The paper also calls Prober-Retaliator "almost an ESS". On its own numbers, R invades pure P by +1.2, and P invades the R/Mouse edge once Mouse exceeds 2.1/29.8 = 0.0705. That matches the paper's own 7% threshold, which is a check on our transcription. Whether any Table 1 cell is a computational error I cannot establish without rerunning the 1973 contest. The paper does not give everything needed for that: how the "preassigned number of moves" is set, how the time bonus scales, and how Bully's "C in response to D" interacts with alternation.
- **Sampling noise in Table 1.** The Hawk/Bully mix beats Mouse by only 0.93 per contest. That margin rests on 2,000-contest averages of outcomes that range from about −100 to +80. I did not estimate the cells' standard errors (the paper gives none). So whether the H/B ESS is robust to the table's own noise is open.
- **The book's endnote.** It is paraphrased in our research notes and I did not reread the book. The claim that Doves drift in and then Hawks, Bullies and Probers invade **is** what our L2 shows mechanically: Bully invades the R/D edge once Dove exceeds 25/60. But in L2 the population returns to the R/D edge instead of settling at Hawk/Bully.
- **The prototype numbers** quoted in STRATEGY_BENCHMARKS (H .20 D .15 R .58 B .08 at N = 200) come from a prototype whose code is not in this repository. I did not re-derive them.

## 9. Sources actually read

- J. Maynard Smith and G. R. Price, "The logic of animal conflict", *Nature* 246:15–18 (1973). Read: the strategy definitions, the simulation parameters and Table 1 (scanned PDF from a course library at public.ek-cer.hu). The numbers in `ContestStudy.Msp1973X1000` are transcribed from Table 1 and checked against the page image.
- O. Leimar and J. M. McNamara, "Game theory in biology: 50 years and onwards", *Phil. Trans. R. Soc. B* 378:20210509 (2023). Read for the citation of Gale and Eaves (1975) and for the remark that the 1973 numerical conclusion was quickly questioned. It does not give Gale and Eaves's result.
- Not read: Gale and Eaves (1975); Maynard Smith's 1975 reply; the book's endnote (only our paraphrase in SELFISH_GENE_NOTES).

## 10. Reproducing the tables

```
dotnet run --project DotNet/God.Runner -c Release -- lab invade --matrix l2      # also l2p, msp, msp4
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --basins 20 --mu 0,2,20 --w0 500,50 --gens 20000
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --dynamics replicator --mu 0,2,20 --gens 6000,100000
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --mu 0,2,20 --agents 100,200,1000 --workers 8
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix msp4 --mu 0,2,20 --agents 100,200,1000 --workers 8
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --starts uniform,hb --k 1,10 --w0 100,500 --mu 0,2
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --starts uniform,hb --mu 2 --gens 2000,6000,20000
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --matrix l2 --expected --agents 200,1000 --mu 0,2
dotnet run --project DotNet/God.Runner -c Release -- lab invade --matrix l2 --rule S=20       # parameter sensitivity
```

Every run is deterministic for its options. `--workers` does not change the output (tested).

## 11. First-blow cost variant (D3)

Owner decision D3 (DEV_LOG, "Owner decisions D3", item 7): L2 stays as it is, and a first-blow cost is added as a separate, optional, lab-only variant. Branch `unity/lab-firstblow`, 26 Nov 2026. **This is our variant with a first-blow cost. It is not claimed to reproduce Gale and Eaves (1975), nor any "corrected" result: their assumptions and payoffs are not established here (section 8).**

### 11.1 Definition

`ContestRules.FirstBlow` (integer payoff points, default 0; `ContestRules.WithFirstBlow(c)`; CLI `--rule FirstBlow=N` on `lab invade` and `lab sweep`). In terms of the 4.2 turn rules:

> A contestant that plays **Display on turn 1 while its opponent plays Escalate on turn 1** pays the cost once, subtracted from its payoff, whatever it does afterwards and however the contest ends (fight, withdrawal, display block).

Hawk and Bully open with Escalate, so they never pay. Retaliator, Dove and a non-probing Prober open with Display, so they pay against an opening Hawk, Bully or probing Prober. Against Dove, Retaliator and a non-probing Prober nothing changes (nobody escalates on turn 1). So, relative to L2, rows D and R lose c in the H and B columns, and nothing else changes. At c = 10 (row against column):

| | H | D | R | B |
|---|---|---|---|---|
| H | -25 | 50 | -25 | 40 |
| D | -10 | 15 | 15 | -10 |
| R | -35 | 15 | 15 | 30 |
| B | -10 | 50 | -10 | 15 |

Two points to keep in view. (1) The charge falls on Dove as well as on Retaliator, because the rule keys on the opening move and on being struck, not on the later reply. That also helps Hawk against Dove; it is a feature of this variant, not of Maynard Smith and Price's table. (2) The key is 0 in L2, L2b and every preset, so L2 and its golden hash `815ddf13e8116bc9` are unchanged (tested: FirstBlow=0 equals L2 contest by contest; the existing L2 golden test still passes). No new lab preset was added, so there is no new golden hash; the variant is pinned by the matrix and threshold tests in `FirstBlowStudyTests`.

### 11.2 Method

Same tools as sections 2 to 5, with `--rule FirstBlow=c` for c in 0, 2, 5, 10, 15, 20, 30: `lab invade` (exact rest points, first and second order invasion, replicator invasion runs, agent invasion runs with N 1000 and 5 seeds), `lab sweep --basins 12` (165 grid starts, replicator, 6000 generations, mu 0, 2, 20 permille), replicator sweeps from the named starts, and agent sweeps (N 200 and 1000, k 10, w0 500, 6000 generations, seeds 1 to 5, starts uniform, hawk, ret, hb, mu 0, 2, 20 permille). Basins as in section 5 (R, H/B, mixed).

### 11.3 Results

| Cost | Strict ESS (exact) | Replicator grid, mu 0: R / H/B (of 165) | mu 2: R / H/B / mixed | Agents N 200, mu 2: seeds in R/HB/mixed for uniform, hawk, ret, hb | Agents N 1000, mu 2: same |
|---|---|---|---|---|---|
| 0 (L2) | H .614 D .018 B .368 (weak) | 139 / 26 | 165 / 0 / 0 | 5/0/0, 5/0/0, 4/0/1, 5/0/0 | 5/0/0 in all four |
| 2 | H 5/8, B 3/8 | 123 / 42 | 165 / 0 / 0 | 5/0/0, 4/0/1, 5/0/0, 5/0/0 | 5/0/0 in all four |
| 5 | H 5/8, B 3/8 | 104 / 61 | 117 / 48 / 0 | 3/0/2, 3/0/2, 3/1/1, 3/0/2 | 5/0/0, 0/5/0, 5/0/0, 0/5/0 |
| 10 | H 5/8, B 3/8 | 74 / 91 | 75 / 90 / 0 | 2/2/1, 0/5/0, 1/4/0, 0/5/0 | 3/2/0, 0/5/0, 5/0/0, 0/5/0 |
| 15 | H 5/8, B 3/8 | 58 / 107 | 55 / 110 / 0 | 0/5/0, 0/5/0, 1/4/0, 0/5/0 | 0/5/0, 0/5/0, 5/0/0, 0/5/0 |
| 20 | H 5/8, B 3/8 | 42 / 123 | 38 / 127 / 0 | 0/5/0, 0/5/0, 1/4/0, 0/5/0 | 0/5/0, 0/5/0, 4/0/1, 0/5/0 |
| 30 | H 5/8, B 3/8 | 24 / 141 | 23 / 142 / 0 | 0/5/0, 0/5/0, 0/4/1, 0/5/0 | 0/5/0, 0/5/0, 5/0/0, 0/5/0 |

("ret" is Retaliator 70% with 10% of each other strategy. The Retaliator/Dove edge is neutral at every cost, so a Retaliator-heavy N 1000 population stays in the Retaliator basin: drift cannot cross the saddle.) At high mutation (20 permille) the L2 and low-cost runs end in a mutation-dominated state; from cost 20 the Hawk/Bully mixture survives it in agents from the uniform, Hawk-heavy and Hawk/Bully starts (agent sweep files `ag*.md`, not reproduced here; rerun with the commands below).

**Exact invasion analysis.** For every c of at least 5/8 (all tested costs from 2 to 30) the Hawk 5/8, Bully 3/8 mixture is a strict ESS: Dove earns -c against it, the mixture earns -5/8, and Retaliator earns -5/8 - c. It is the only ESS among those costs. L2's weak Hawk/Dove/Bully ESS (section 1) is gone once c is positive. The Retaliator/Dove edge is untouched (R against R and D unchanged) and stays neutrally stable, with Bully invading it once Dove exceeds the same share as in L2. Agent invasion runs at c = 10: from the Hawk/Bully state, Dove and Retaliator mutants gain in 0 of 5 seeds (all lost); from all-Retaliator, Hawk, Dove and Bully mutants gain in 0 of 5.

### 11.4 Threshold

The exact threshold is **c\* = 5/8 of a payoff point** (V = 50). Dove's first-order payoff against the Hawk/Bully mixture is -c, the mixture's own is -5/8. Below 5/8 Dove invades. At exactly 5/8 Dove ties at first order and loses at second order (its payoff against itself, 15, is below the mixture's payoff against it, 50), so the mixture is still an ESS, a second-order one. Above 5/8 it is a first-order strict ESS. So the ESS exists iff c >= 5/8. Retaliator already resists for any c above 0, so Dove is the binding mutant. With integer costs the first cost that works is 1. Tested with payoffs scaled by 10 (cost 6 invaded, cost 7 ESS) and by 8 (cost 5: first-order tie, second-order ESS).

**Where the basins change.** The ESS appears at c\* = 0.625, but its basin grows only gradually. Of the 165 grid starts, 16% end in H/B at c = 0, 37% at 5, 55% at 10, 75% at 20, 85% at 30 (mu 0). Hawk/Bully overtakes Retaliator in the grid between cost 5 and 10. Under the default mutation (2 permille), cost 0 and 2 send every start to Retaliator; Hawk/Bully becomes reachable at cost 5 and equals Retaliator at about 10. In agents, the uniform start reaches Hawk/Bully in all 5 seeds at N 200 and at N 1000 from cost 15. So "an ESS exists" (c at least 0.625) and "the uniform start reaches it under default drift" (c about 15) are different thresholds.

### 11.5 Limits

- Not Gale and Eaves (1975), and not the book's corrected result: their original assumptions and payoffs are not established (section 8). Describe it only as "our variant with a first-blow cost".
- The cost is a lab strategy penalty on every opener that is struck, Dove included; it is not an injury model. The strategy set (H, D, R, B) and the payoff scale (V 50, I 100, T 10, S 10) are ours.
- The Prober obeys the same rule (matrix test) but the sweeps above are four-strategy (`--matrix l2`).
- Basin shares come from one grid (step 1/12), one w0 (500), 6000 generations, 5 seeds per agent cell. Costs were not searched finer than the table, apart from the exact threshold.
- **Separation.** The variant is lab-only. In the full island simulation, a first-mover advantage must emerge from combat, injury and reaction mechanics (who lands the first hit, who is hurt or slowed, how reaction time works), not from a strategy penalty. No island rule reads `ContestRules.FirstBlow`, and none should.

### 11.6 Reproducing

```
dotnet run --project DotNet/God.Runner -c Release -- lab invade --rule FirstBlow=10 --workers 8
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --rule FirstBlow=10 --basins 12 --mu 0,2,20 --gens 6000 --workers 8
dotnet run --project DotNet/God.Runner -c Release -- lab sweep --rule FirstBlow=10 --starts uniform,hawk,ret,hb --mu 0,2,20 --agents 200,1000 --gens 6000 --workers 8
dotnet run --project DotNet/God.Runner -c Release -- lab invade --rule V=500 --rule I=1000 --rule T=100 --rule S=100 --rule FirstBlow=6   # threshold at scale 10
```
