# Strategy benchmarks and the game-theory lab (stage 4)

This document defines the **named benchmark policies** exactly, the **maths-only lab** that tests them against the book's results (milestone M2), the **island benchmark scenarios** that use them as controls in later milestones, and the runner commands. The mapping of each book example onto the island, and how institutions change the payoffs, is in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md). The source of every payoff, example and page number is [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md) ("PDF n" below refers to those notes).

## 1. Purpose and scope

**Owns:**
- the exact state machines and memory of every benchmark policy;
- the lab: payoffs, dynamics, parameters, expected results with tolerances;
- the island benchmark scenarios, what they measure and how they are judged;
- the lab command interface;
- tests and acceptance criteria for all of the above.

**Links to, does not repeat:**
- why each example matters and how it appears on the island: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md);
- the policy interface and registry: [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#7-policy-levels), [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md);
- the runner: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md);
- experiment design for the island (treatments, controls, pre-registration): [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md);
- warehouse tables: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- the acceptance list: [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

## 2. Principles

1. **Benchmarks are policies, not people.** They are registered `INpcPolicy` implementations (keys such as `bench.tft`) used in the lab and in scripted island scenarios. Ordinary NPCs never get one, and the sim has no strategy field.
2. **Lab and island stay apart.**
   - The lab uses the book's payoffs and abstract agents. It must reproduce known results, within stated tolerances.
   - The island uses real consequences. It is allowed to disagree with the book. It is never tuned to agree.
3. **Same definitions in both places.** The island adapter wraps exactly the lab state machine (section 4.1). A test feeds both the same scripted history and requires the same moves.
4. **Integers only, seeded, ordered.** Lab shares are fixed-point. Random draws use forked streams. Iteration is by ascending id. The lab compiles in `Sim/Lab` (pure C#) and runs under `dotnet test DotNet`.
5. **Honest about the book.** Two of its published equilibria were wrong (Retaliator, coy/fast). The lab tests the **corrected** statements. Where the book gives no numbers (Sucker/Cheat/Grudger, vampire bats, the war of attrition) we choose numbers and say so.
6. **Honest about our strategy set.** Axelrod's 63 entries are not available. Our round robin uses a set we define (section 4.3). It can show the *shape* of his result, not his scores. Where our result differs from the book's (for example one nasty strategy in our top five), the document says so and the test records what we got.
7. **Tolerances come from a prototype, not from wishes.** Numbers marked (proto) came from a throwaway Python prototype of these exact definitions, run on 26 Nov 2026. The C# lab must match within the tolerance. If it does not, find the bug before touching a tolerance. If a tolerance is changed, record why.

## 3. Data structures

All values are integers. Shares are `long` fixed point at 2^30 (`Share`). Payoffs are integers scaled by 1000 (`PayoffX1000`) so that half-points (12.5, 13.5) are exact.

```csharp
namespace God.Sim.Lab
{
    public enum Move : byte { Cooperate = 0, Defect = 1 }        // exchange and help games
    public enum CMove : byte { Display, Escalate, Withdraw }      // contests

    // ---- repeated exchange / help games (Prisoner's Dilemma family) ----
    public interface IRepeatedStrategy
    {
        string Key { get; }                         // "bench.tft"
        int MemoryBits { get; }                     // documented in the table in 4.1
        IRepeatedState NewPair();                   // fresh state for one partner
    }
    public interface IRepeatedState
    {
        Move Decide(ref Rng rng);                   // first call = round 1
        void Observe(Move mine, Move theirs);       // after the round
    }

    // ---- contests ----
    public struct ContestView
    {
        public int Turn;                            // 1-based
        public Role MyRole;                         // None / Resident / Intruder
        public CMove OppLast;                       // opponent's last move (Display on turn 1)
        public CMove MyLast;
        public bool OppEscalatedEver;               // this contest
        public bool MutualExchangeHappened;         // both escalated in one turn
        public int MyHealthPermille;                // 0..1000
    }
    public interface IContestStrategy
    {
        string Key { get; }
        IContestState NewContest(ref Rng rng);      // may draw once (probe decision)
    }
    public interface IContestState { CMove Decide(in ContestView v, ref Rng rng); }

    // ---- partnership and housing (one-shot role strategies) ----
    public enum FemaleStrategy : byte { Coy, Fast }
    public enum MaleStrategy : byte { Faithful, Philanderer }
    public enum HousingStrategy : byte { Dig, Enter }

    // ---- lab experiment contract ----
    public struct LabConfig
    {
        public string Experiment;       // "L1" ...
        public ulong Seed;
        public int Agents;              // N (agent-based)
        public int Generations;
        public int ContestsPerAgent;    // k
        public int BaselineX1000;       // w0 in payoff units x1000
        public int MutationPermille;    // per offspring
        public int MatchRounds;         // repeated games; 0 = geometric with ContinuePermille
        public int ContinuePermille;    // w x 1000, used when MatchRounds == 0
        public string Dynamics;         // "replicator" | "agents"
        public string[] Params;         // experiment-specific key=value
    }
    public sealed class LabResult
    {
        public string Experiment; public ulong Seed;
        public long[][] SharesPerGeneration;        // [gen][strategy] in Share units
        public long[][] MeanPayoffX1000;            // [gen][strategy]
        public Check[] Checks;                      // each: name, measured, expected, tolerance, pass
        public ulong ResultHash;                    // hash of the above, for golden files
    }
}
```

**Island adapter (milestone M5 onwards):**

```csharp
// in Sim/Policy
public sealed class BenchmarkNpcPolicy : INpcPolicy
{
    // Acts only inside its domain (a contest turn, an exchange step, a help request with a known partner).
    // Outside it, delegates to AssociativeNpcPolicy (the default). Reads only NpcObservation and the NPC's own
    // MindView (social memory included); see NPC_LEARNING_DESIGN for MindView.
    public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng) { ... }
}
```

The per-partner pair state of a benchmark is rebuilt from the NPC's own per-partner memory ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)). A benchmark never sees more than a normal NPC could remember. Its memory limit in the table below is the minimum the NPC's mind must hold per partner.

## 4. Mechanics and algorithms

### 4.1 Definitions common to all benchmarks

- **Round.** One simultaneous decision in a repeated game. In the island adapter a round is one exchange step pair or one help request and its answer, as defined by the economy.
- **Cooperate / Defect.**
  - In a barter: hand over what was agreed, or keep it.
  - In help on credit: return the help owed, or not.
  - In a gift or grooming game: give, or do not.
- **Observation.** `Observe(mine, theirs)` is called after each round with both moves. The partner's move is what the NPC saw happen (on the island this can be missed or misread, which is a deliberate difference from the lab).
- **Randomness.** Only where stated, from the forked stream given to the state.
- **First round.** All strategies decide before any history.
- **Memory per partner** (the table in 4.3 and 4.2 gives each).

### 4.2 Contest strategies (aggression)

Contest rules used by the lab (the island uses the real contest record, [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record)):

- Prize `V = 50`, serious injury `I = 100`, time cost of one display block `T = 10`, scuffle cost `S = 10`.
- Turns are simultaneous. A **display block** is a turn where both Display: each pays `T`, and the winner (coin flip) gets `V`. Dove against Dove ends after one block.
- **One-sided escalation**: if one side Escalates and the other Displays, the displayer's next move is made after seeing it. A side that Withdraws gives the prize to the other with no cost to either.
- **Mutual exchange**: a turn where both Escalate. If they escalate in two consecutive turns it becomes a **fight**: coin flip, winner `+V`, loser `-I`, ends. If one Withdraws after a mutual exchange, the contest ends: the other wins `V`, and both pay `S`. If both Withdraw, the winner is a coin flip, and both pay `S`.
- A **contest** is limited to `MaxTurns = 8`. Hitting the limit is treated as a display block.

The strategies:

| Key | Round 1 | Later turns | Per-contest memory | Notes |
|---|---|---|---|---|
| `bench.hawk` | Escalate | Escalate always (the fight resolves it) | none | Retreats only through the fight's injury |
| `bench.dove` | Display | Display, unless the opponent Escalated last turn: then Withdraw | 0 bits (looks at the last turn) | Never first to escalate |
| `bench.retaliator` | Display | Escalate iff the opponent's last move was Escalate, else Display | 0 bits | Mirrors escalation. Ties with Dove against Dove and Retaliator |
| `bench.bully` | Escalate | Withdraw if the opponent's last move was Escalate, else Escalate | 0 bits | Flees when hit back |
| `bench.prober` (Prober-retaliator) | With probability `q = 100` permille (drawn once at contest start): Escalate (a probe). Otherwise Display | In **probe mode**: if the opponent's last move was Display, keep Escalating. If it was Escalate, the probe failed: switch to Retaliator rules for the rest of the contest. In **normal mode**: Retaliator rules | 1 bit (probe mode) | Not an ESS. The book withdraws "almost an ESS" (PDF 306) |
| `bench.resident_rule` (Bourgeois) | Escalate if `MyRole == Resident`, else Withdraw | same | role is read from the contest | Needs a role tag |
| `bench.intruder_rule` (Anti-Bourgeois) | Escalate if `MyRole == Intruder`, else Withdraw | same | role | The self-destructive mirror convention |

Lab matrix derived from these rules for `V=50, I=100, T=10, S=10, q=0.1` (row player's expected payoff; computed by hand and checked against the prototype by Monte Carlo):

| row \ col | Hawk | Dove | Retal. | Bully | Prober |
|---|---|---|---|---|---|
| Hawk | -25 | 50 | -25 | 40 | -25 |
| Dove | 0 | 15 | 15 | 0 | 13.5 |
| Retaliator | -25 | 15 | 15 | 40 | 14 |
| Bully | -10 | 50 | -10 | 15 | -10 |
| Prober | -25 | 18.5 | 14 | 40 | 13.2 |

The Prober row and column are approximate because of the one-sided-hit simplification in the rules above. Test L2b checks the matrix against a Monte Carlo run of the turn-level rules with tolerance 3 per cell. If a cell fails, the turn rules are right and the table is corrected.

**Resident/intruder matrix** (roles are random, 50:50; strategies Hawk, Dove, Bourgeois, Anti-Bourgeois; derived with the same prize and injury):

| row \ col | Hawk | Dove | Bourgeois | Anti |
|---|---|---|---|---|
| Hawk | -25 | 50 | 12.5 | 12.5 |
| Dove | 0 | 15 | 25 | 25 |
| Bourgeois | -12.5 | 25 | 25 | -12.5 |
| Anti | -12.5 | 25 | -12.5 | 25 |

Two Bourgeois never fight and never waste time: each wins half the time at no cost. If both sides claim to be resident (the Bourgeois player believes it is resident and so does its opponent), the contest becomes a fight: -25 each on average.

**Dig and Enter (digger wasps), one decision per housing need:**

| Key | Rule |
|---|---|
| `bench.dig` | Plan to build a hut, pay `Cd` (time and logs), receive `B` |
| `bench.enter` | Pick a hut believed empty. If it is free: receive `B`, pay `Ce`. If in use: pay `Cw` and gain nothing |
| `bench.digenter` | Enter with probability `x` permille (a parameter), else Dig. Used for the ESS test |

Lab values: `B = 100`, `Cd = 40`, `Ce = 5`, `Cw = 30`. Entering finds a free hut with probability `1 - x_pop` (the population share entering). Payoffs: Dig = `B - Cd = 60`. Enter = `(1-x)(B - Ce) + x(-Cw) = 95 - 125x`. The two are equal at `x* = (Cd - Ce)/(B - Ce + Cw) = 35/125 = 0.28`.

### 4.3 Exchange strategies (help, barter, cooperation)

All act in `IRepeatedStrategy` form. "Their last" means the partner's move in the previous round with this partner.

| Key | Round 1 | Round n > 1 | Per-partner memory | Class |
|---|---|---|---|---|
| `bench.alld` Always Defect (also the **Cheat** in help games: takes, never gives) | Defect | Defect | 0 | nasty |
| `bench.allc` Always Cooperate (also the **Sucker**: helps anyone) | Cooperate | Cooperate | 0 | nice |
| `bench.random` Random(p) | Cooperate with probability p permille (default 500) | same | 0 | neither |
| `bench.tft` Tit for Tat | Cooperate | Their last | 1 bit | nice |
| `bench.tf2t` Tit for Two Tats | Cooperate | Defect only if their last **two** were both Defect, else Cooperate | 2 bits | nice |
| `bench.stft` Suspicious Tit for Tat | Defect | Their last | 1 bit | nasty |
| `bench.grudger` Grudger (Friedman in Axelrod; the book's Grudger) | Cooperate | Cooperate until they have ever defected, then Defect for ever | 1 bit | nice |
| `bench.naive_prober` | Cooperate | Their last, but if their last was Cooperate, Defect with probability `q = 100` permille (a probe) | 1 bit | nasty by our definition (it defects first) |
| `bench.remorseful_prober` | Cooperate | If my last move was a probe and their last is Defect (they punished): Cooperate once (take the hit, do not echo). Else as Naive Prober | 2 bits (their last, probed flag) | nasty by our definition |
| `bench.pavlov` Win-stay, lose-shift | Cooperate | Repeat my last move if their last was Cooperate, else switch | 2 bits | nice |
| `bench.tester` | Defect | If they retaliated on round 2: Cooperate on round 2 (apology), then play their last. If they did not retaliate: alternate Defect on even rounds and Cooperate on odd rounds | 2 bits | nasty |
| `bench.sneak` | Cooperate | Their last, except Defect on rounds 50, 100 and 150 (a sneaky defection) | round counter | nasty |

**Class.** *Nice* means never the first to defect. Our probers and Sneak defect first, so they are nasty in this scoring, as in the book where the Naive Prober is a nasty-looking variant.

**Sucker, Cheat, Grudger in the help game.** The help-on-credit lab game uses the same three strategies with per-round payoffs `b = 3` (value of being helped) and `c = 1` (cost of helping). Per-round payoff for a player: helped by partner `+b`, helping `-c`. The Sucker gives to everyone, the Cheat gives to no one, and the Grudger gives to anyone who has not cheated it (strangers get one gift). These are the same state machines as `allc`, `alld`, `grudger`.

**Prisoner's Dilemma payoffs.**
- Axelrod scoring (used in L8 and L9): reward 3, temptation 5, punishment 1, sucker 0 (PDF 231).
- The dollar example (PDF 226) is a second preset: +300, +500, -10, -100. It is used for a single regression test of the payoff ordering and the rule `(T + S)/2 < R`.

### 4.4 Partnership strategies (one-shot role pair)

| Key | Rule | Costs it pays |
|---|---|---|
| `bench.coy` (female) | Mates only after a courtship the partner pays for (a period of gifts and help). Without courtship, no mating | Courtship cost to the male is 3 |
| `bench.fast` (female) | Mates at once, no courtship | none |
| `bench.faithful` (male) | Courts until accepted, then stays to raise the child | Child costs 20, split between the pair when both stay |
| `bench.philanderer` (male) | Courts briefly (gives up if the female is coy), mates, then leaves | Pays nothing after mating |

Lab payoffs (book, PDF 174): each child is worth 15 to each parent, raising it costs 20 (shared when both stay), courtship costs 3.

| Pair | Female | Male |
|---|---|---|
| Coy with faithful | 2 (15 - 10 - 3) | 2 |
| Fast with faithful | 5 | 5 |
| Fast with philanderer | -5 | 15 |
| Coy with philanderer | 0 (no mating) | 0 |

Equilibrium (the book's, not stable): coy `5/6`, faithful `5/8`. Maynard Smith's four **family types** (duck, stickleback, fruit-fly, gibbon) are not separate benchmarks. They are the four ways a pair can handle care after a birth, measured on the island (see guard/desert in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#45-partners-chapter-9-pdf-163-to-188)).

### 4.5 The lab: dynamics

Two engines run each experiment. Results must agree where the experiment allows both.

**Replicator (fixed point, deterministic).**
- State: share vector `x` in `Share` (2^30 = 1). Payoff matrix in `PayoffX1000`.
- Step: `f_i = w0 + sum_j A_ij x_j` (payoff units x 1000, `x` rescaled to integers), then `x_i' = x_i f_i / sum_j x_j f_j`. All integer arithmetic in `long` with a remainder-to-largest correction so the shares sum to exactly 2^30.
- Mutation (optional): `x' = (1 - mu) x' + mu / K`, with `mu` in 2^-20 units.
- Two-population games (coy/fast): two vectors, each updated against the other, with a fourth-order Runge-Kutta step in fixed point (the orbit is a neutral centre, so a plain Euler step spirals outwards and fakes the answer; see L6).
- No random numbers, so the same everywhere.

**Agent-based (seeded random pairing).**
- `N` agents each hold a strategy index. Each generation:
  1. For each of `k` rounds, shuffle the agent ids with `Rng.Fork("lab", exp, "pair", gen, round)` (Fisher-Yates, ascending ids as the base order) and pair neighbours.
  2. Each pair plays one game. Fixed-matrix games use the matrix cell, with realised (not expected) random outcomes where the rules call for them (a coin flip decides a fight). Repeated games play out `MatchRounds` rounds or a geometric number with `ContinuePermille`.
  3. Fitness `f = max(1, w0 + sum of payoffs)`.
  4. The next generation is drawn by Wright-Fisher sampling: `N` draws from the cumulative fitness with `Rng.Fork("lab", exp, "sel", gen)`.
  5. Each offspring mutates with probability `MutationPermille`: it takes a uniformly chosen other strategy using `Rng.Fork("lab", exp, "mut", gen)`.
- **The baseline `w0` matters.** Too small, and the floor at 1 clips unlucky Hawks and biases the result (the first prototype got a Hawk share of 0.64, not 0.58, with `w0 = 100`). Default `w0 = 500` for payoffs of this size.
- **Seeds.** Each experiment's result is a function of `(config, seed)`. Replicate runs use seeds `s, s+1, ...`.

**Reporting.** Each run records shares and mean payoffs per generation. The time-average over a stated window is the measured quantity unless the experiment says otherwise.

### 4.6 Lab experiments and expected results

Each experiment lists: setup, expected result, tolerance, and the book's source. **(proto)** marks a prototype number.

**L1. Hawk/Dove (PDF 93 to 96).**
- Setup: `V=50, I=100, T=10`, matrix: HH -25, HD 50, DH 0, DD 15. Agents: `N=1000, k=10, w0=500, mu=10 permille, 300 generations`, time average over generations 100 to 300.
- Expected:
  - the replicator fixed point is `p* = 7/12 = 0.5833`. Tolerance: 583 permille ± 3 (fixed point), 0.583 ± 0.03 (agents, N=1000; proto 0.582 ± 0.01 over 5 seeds), ± 0.05 at `N=100` (proto 0.585);
  - mean payoff at the ESS: `6.25` ± 0.5 per contest, against `15` for all Doves (ratio 0.42);
  - the group optimum is `1/6` Hawks with mean payoff `16.67`. The ESS is **below** it (test: mean payoff at ESS < 0.5 × optimum);
  - a "conspiracy of Doves" (all Dove) is invaded: one Hawk among 999 Doves earns 50 against 15. Hawk share passes 0.3 within 30 generations (N=1000, k=10);
  - per-agent mixed strategy (each agent plays Hawk with its own heritable probability): the population mean converges to 0.583 ± 0.03;
  - a predictable player (7 Hawk then 5 Dove in a fixed cycle) loses to a reader that knows the phase: reader payoff `5/12 × 50 = 20.8` against 6.25 for the random mix, a gain of at least 10.

**L2. Retaliator, Bully, Prober (PDF 97 and 98, endnote 306).**
- Setup: the matrix in 4.2 (strategies H, D, R, B, and P in the second variant). Replicator with mutation `mu = 0.002`, and agents with mutation `2 permille`, 6000 generations, window 3000 to 6000, N = 200 and 1000.
- Facts that hold exactly (replicator):
  - Dove ties with Retaliator against both (15 = 15), so an all-Retaliator population is **not** a strict ESS: a Dove share stays constant, neither growing nor shrinking, over 20000 generations (no mutation). Test: Dove share stays within 0.1 ± 0.001 from a start of 0.1.
  - In a mix of Retaliators and Doves with Dove share `d`, a Bully beats the Retaliators when `50d - 10(1 - d) > 15`, that is `d > 25/60 = 0.417`. A Hawk needs `75d - 25 > 15`, that is `d > 0.533`. Test: start (R 0.5, D 0.45, B 0.05): Bully share rises. Start (R 0.65, D 0.30, B 0.05): it falls.
- Agent results: **the corrected claim is that Retaliator does not stay dominant once drift and Hawks or Bullies are present.**
  - Strategies H, D, R, B, N = 200 (proto, 6 seeds): mean shares H 0.20, D 0.15, R 0.58, B 0.08. Tolerance: Retaliator below 0.75; Hawk plus Bully above 0.10. At N = 1000: Retaliator 0.80, Hawk plus Bully 0.05 (tolerance R between 0.65 and 0.90). Smaller populations drift further.
  - Strategies H, D, R, B, P, replicator with mutation (proto): D 0.177, R 0.620, P 0.198, H 0.002, B 0.003.
- **Honest limit:** our matrix does not reproduce Gale and Eaves's stable Hawk/Bully mix. With our payoffs, a Dove invades a Hawk/Bully mixture (it earns 0 against a mixture whose members earn -0.6), and Retaliators tie with it. The book does not give the payoffs that produced the Hawk/Bully result. We test only the mechanism claims above, and we do not claim a Hawk/Bully ESS. See the open questions.
- **L2b:** a Monte Carlo of the turn-level rules reproduces every cell of the matrix within 3 (50,000 contests per cell).

**L3. War of attrition (PDF 99 and 100).**
- Setup: prize `V`, cost rate 1 per tick for both sides until one quits, higher persistence wins. Each agent has a heritable **mean** `m` and draws its private giving-up time from an exponential distribution of that mean. `N = 1000, k = 20, 400 generations`, soft selection `exp(f/(0.3 k V))`, mutation: multiply `m` by a log-normal factor with spread 5 per cent, initial `m` uniform in `[0.2 V, 3 V]`.
- Expected:
  - mean `m` over the last 100 generations is `V` ± 0.2 `V` (proto 1.105 to 1.129 V in 3 seeds);
  - the coefficient of variation of realised persistence is between 1.0 and 1.5 (an exponential has 1; a spread of means adds). This is a calibration target, not yet seen;
  - a **fixed limit** `L < V` is invaded by `L + 1`, which always wins: test for every `L` in `[1, V - 2]`;
  - a **tell**: a reader who sees an opponent's quitting time in advance, stays one tick longer when the opponent will quit before `V` and quits at once otherwise. Its gain per contest against exponential opponents of mean `V` is `V/e = 0.368 V`. Tolerance 0.368 V ± 0.01 V over 100,000 contests (proto 0.368 V). A population in which a tell is readable lets readers spread: reader share above 0.9 within 100 generations. A population whose tell is hidden shows no reader advantage (gain within ± 0.01 V).

**L4. Resident/intruder (PDF 100 to 103).**
- Setup: the matrix in 4.2 (H, D, B, A). Roles random 50:50.
- Expected (replicator, mutation `0.0005`):
  - Bourgeois vs Anti-Bourgeois (no Hawks or Doves): the share of the larger starts the takeover. Starting B 0.51: B above 0.99. Starting B 0.49: A above 0.99. The boundary is 0.5 ± 0.01;
  - both are stable on their own (a rare mutant of the other loses);
  - a Hawk/Dove mix at its ESS (0.58/0.42) **resists** a small Bourgeois share, because a Bourgeois earns 3.1 against the mix, below 6.25 (proto: starting B at 0.05, 0.2 or 0.4, B ends near 0.02). With B at 0.6 it takes over: B share ends at 0.9 ± 0.05. The boundary lies between 0.4 and 0.6;
  - when both sides think they are resident, mean payoff of Bourgeois against Bourgeois drops from 25 to -25 (test on the matrix).
- **L4b (optional):** paradoxical strategy (attack larger, flee smaller). Heavier injury, where `I` grows with the opponent's size, plus an initial majority of paradoxical players: the paradoxical rule persists. Below a critical share, the sensible rule takes over (zone of attraction). Calibrated at M2, not yet seen.

**L5. Pecking orders (PDF 104 and 105).**
- Setup: groups of 8 with hidden strengths 10 to 99, 400 random-pair contests each. *Cricket rule:* the chance to escalate is `0.5 + 0.1 × streak`, where `streak` is a decayed win-minus-loss count. *Hen rule:* after losing to a partner, never escalate against that partner; otherwise escalate with probability 0.5. Mutual escalation ends in a fight won by strength-weighted chance.
- Expected (proto, 60 groups each, wide tolerance):
  - Hen rule: the share of fights in the first 100 contests is 0.07; in the last 100 it is ≤ 0.02. Fighting falls to almost nothing;
  - Cricket rule: the fight share stays flat, last 100 over first 100 between 0.7 and 1.3 (proto 0.25 and 0.25);
  - the Hen rule has fewer total fights than the Cricket rule by the end (test: last-100 hen fights < last-100 cricket fights).
- Landau's h is **not** a pass criterion here, because outcomes depend heavily on chance (proto h 0.33 for hens, 0.54 for crickets). It is reported.

**L6. Coy/fast, faithful/philanderer (PDF 174 to 176, endnote 326).**
- Setup: the payoffs in 4.4. Two-population replicator, state `(c, f)` = (coy share of females, faithful share of males), start `(0.5, 0.3)`, RK4 step 0.01 in fixed point, 4000 time units.
  - `dc/dt = c(1-c)(5 - 8f)`, `df/dt = f(1-f)(12c - 10)`.
- Expected (proto):
  - the **time average** is `(5/6, 5/8) = (0.833, 0.625)`, tolerance ± 0.01 on `c`, ± 0.02 on `f`;
  - the instantaneous state **keeps cycling**: standard deviation of `c` above 0.1 and of `f` above 0.2 over the second half (proto 0.198 and 0.301); the distance to `(5/6, 5/8)` averages above 0.1 in the last quarter;
  - the conserved quantity `H = -10 ln c - 2 ln(1-c) - 5 ln f - 3 ln(1-f)` drifts by less than 0.1 per cent over the run (a test of the integrator: an Euler step fails it);
  - agents, `N = 1000` per sex, soft selection `exp(0.02 × payoff)`, mutation `1 permille`: time-averages `c = 0.83 ± 0.03`, `f = 0.60 ± 0.05` over generations 5000 to 20000, standard deviation of `c` above 0.05 (proto 0.16); at `N = 10000` the cycle shrinks (proto std 0.06 to 0.07) but does not vanish.
- The book's equilibrium is the **mean** of the cycle, not a place the system sits. A system that settles at `(5/6, 5/8)` is wrong.

**L7. Sucker, Cheat, Grudger (PDF 206 to 209).**
- Setup: help game `b = 3, c = 1`, `n = 10` rounds per pairing, `w0 = 50`. Per-pair payoffs over 10 rounds (row, column):

  | | Sucker | Cheat | Grudger |
  |---|---|---|---|
  | Sucker | 20 | -10 | 20 |
  | Cheat | 30 | 0 | 3 |
  | Grudger | 20 | -1 | 20 |

  Start: Sucker 0.78, Cheat 0.02, Grudger 0.20 (Dawkins's own setup was mostly Suckers with Grudgers above the critical share; we add a small Cheat seed). Replicator, no mutation.
- Expected (proto):
  - Suckers fall first (below 0.6 by about generation 40);
  - Cheats rise to a peak of 0.25 to 0.40 around generation 40 to 90 (proto 0.315 at 60), then fall below 0.01 by generation 150;
  - Grudgers end at 0.70 ± 0.05; Suckers survive at 0.30 ± 0.05, because a Sucker ties with a Grudger (20 = 20) and drifts. Over the very long run a Cheat seed can return while Suckers are many. This is a finding, not a bug;
  - **both Grudger and Cheat are stable.** Critical Grudger share against Cheats `g* = c/((n-1)(b-c)) = 1/18 = 0.0556`. Test: starting Grudger share 0.04 (rest Cheat) ends at Cheat above 0.99; 0.07 ends at Grudger above 0.99. Tolerance on the boundary ± 0.003;
  - the order of events (Sucker falls, Cheat booms then falls, Grudger takes over) is the pass criterion, not the exact peak.
- Mackie's point (selection between ESSs, endnote PDF 344) is not tested.

**L8. Axelrod-like round robin (PDF 231 to 239).**
- Setup: the 12 strategies in 4.3 (TFT, TF2T, STFT, AllD, AllC, Random(500), Grudger, Naive Prober, Remorseful Prober, Pavlov, Tester, Sneak). Every strategy plays every other **and itself**, 200 rounds, payoffs 5/3/1/0. Scores are averaged over 30 seeds (Random and the probers use random draws). Maximum is 600.
- Expected (proto, mean of 30 seeds, tolerance ± 15 points per entry):

  | Rank | Strategy | Score | % of 600 |
  |---|---|---|---|
  | 1 | Tit for Two Tats | 508 | 84.7 |
  | 2 | Tit for Tat | 493 | 82.2 |
  | 3 | Always Cooperate | 489 | 81.6 |
  | 4 | Tester | 484 | 80.7 |
  | 5 | Pavlov | 474 | 78.9 |
  | 6 | Grudger | 445 | 74.2 |
  | 7 | Random | 431 | 71.9 |
  | 8 | Remorseful Prober | 428 | 71.3 |
  | 9 | Sneak | 400 | 66.6 |
  | 10 | Suspicious TFT | 387 | 64.5 |
  | 11 | Naive Prober | 349 | 58.1 |
  | 12 | Always Defect | 336 | 56.1 |

- Pass criteria:
  - TFT and TF2T are both in the top 4; Always Defect is in the bottom 2;
  - at least 4 of the top 5 are nice;
  - Tit for Tat scores 600 against every nice strategy;
  - Tit for Tat never beats a deterministic partner in a match by more than 5 points (one temptation gap), the "not envious" point (proto: it never leads by more than that);
  - Tit for Tat's score against Naive Prober is 253 ± 50 (the echo lock-in the book describes), and against Remorseful Prober is 490 ± 50: the remorse step breaks the echo.
- **What our set cannot reproduce.**
  - The book's top 8 were all nice. Ours has one nasty strategy (Tester) in the top 5, because the set contains exploitable nice players (Always Cooperate, Tit for Two Tats) for it to feed on. The book's own second round showed the same: results depend on who enters (Tit for Two Tats lost its crown to subtle nasty strategies, PDF 235).
  - Tit for Two Tats ranks first here and would have won round 1 in the book, too.
  - We do not have the 63 original entries, so scores are not comparable with the book's 504.5 or 96 per cent.
- **L8b, the known last round:** a Tit for Tat that defects on the final round (`TFT-End`) scores 602 against Tit for Tat's 597 over 200 rounds (exact; the other side gets 0 on the last round). With an unknown length (geometric, `w = 0.995`) it has no systematic advantage (test: mean gain within ± 3 over 200 seeds).

**L9. The ecological run (PDF 237 to 239).**
- Setup: the replicator on the L8 mean score matrix, uniform start (1/12 each), 1000 generations, no mutation.
- Expected (proto): nasty strategies and probers fall below 1 per cent in this order: Always Defect (generation 8), Naive Prober (9), Suspicious TFT (16), Random (20), Sneak (20), Remorseful Prober (22), Tester (35). Final shares: TFT 0.33, Grudger 0.22, Pavlov 0.19, TF2T 0.14, AllC 0.11, tolerance ± 0.05 each.
- Pass criteria: all nasty strategies and probers below 1 per cent by generation 200; Always Defect below 1 per cent by generation 30; Tit for Tat has the largest final share; the survivors are all nice. Always Cooperate survives only because nothing invades it here. With a defector seed it would be exploited. That is a finding about Always Cooperate, not a result for it.

**L9b. Tit for Tat against Always Defect, and clustering (PDF 239 to 243).**
- Setup: two strategies, geometric match length with continuation probability `w`. Payoffs per match: TFT-TFT `3/(1-w)`, TFT-AllD `w/(1-w)`, AllD-TFT `5 + w/(1-w)`, AllD-AllD `1/(1-w)`.
- **The critical TFT share** (the invasion threshold) is `p* = (1 - w)/(3w - 1)` for `w > 1/2` (for `w ≤ 1/2`, Tit for Tat cannot invade at all).

  | w | 0.6 | 0.75 | 0.9 | 0.95 | 0.99 | 0.995 (200 rounds) |
  |---|---|---|---|---|---|---|
  | p* | 0.500 | 0.200 | 0.059 | 0.027 | 0.0051 | 0.0025 |

- Expected: the replicator boundary matches `p*` to ± 0.003 for each `w`. Agents, `N = 1000`, 20 seeds: starting at `1.5 p*`, TFT takes over (above 0.95) in at least 90 per cent of runs; at `0.5 p*` it dies in at least 90 per cent.
- **Clustering.** If a fraction `k` of each agent's partners is chosen from its own strategy, the effective chance of meeting its own kind is `k + (1-k) p`. With `w = 0.9` and a start of `p = 0.01`, the critical clustering is `k* = (p* - p)/(1 - p) = 0.049`. Test: `k = 0.10` lets Tit for Tat take over; `k = 0.02` does not. Clusters do not help Always Defect (its own meetings pay `1/(1-w)`, below TFT-TFT's `3/(1-w)`).
- A **Tit for Tat is not a strict ESS**: Always Cooperate behaves identically among nice players, so with mutation a share of Always Cooperate drifts in and then Always Defect invades. Test (agents, `mu = 5 permille`, 5000 generations): the Always Cooperate share does not stay near zero; its time average is above 0.02 (calibration, not yet seen).

**L10. Digger wasps (endnote PDF 307 and 308).**
- Setup: `B = 100, Cd = 40, Ce = 5, Cw = 30`. Each wasp has a heritable entering probability `x_i`. Agents: `N = 500`, soft selection `exp(0.02 × payoff)`, mutation of `x_i` by Gaussian-like noise of 0.01, 2000 generations.
- Expected: population mean `x` over the last 500 generations is `0.28 ± 0.03` (proto 0.278 to 0.282, 4 seeds); at that point `|payoff(Dig) - payoff(Enter)| < 3`. A start at `x = 0.9` and a start at `x = 0.05` both end there.
- Variant: pure types (each wasp always digs or always enters, heritable) gives the same population mean ± 0.03, as a polymorphic population. The book found a true mixed ESS in one population and not the other, so the lab treats both versions as valid.

**L11. Hamilton's rule and the mushroom call (PDF 112 to 122).**
- Decision test: `Call iff own_if_call + sum(r_i × b_i) > own_if_keep`. The book's example: keep quiet scores 18; calling scores 12 (own) + 6 (brother, r 1/2 × 12) + 1.5 (cousin, r 1/8 × 12) = 19.5, so **call**. Without the cousin the total is 18.0, a tie, resolved to Keep. A stranger (r 0) never adds anything. A brother needs a benefit above `cost / r = 12` to make the same call at a cost of 6.
- Spread test (agents, assortment `r`: the chance that the helped partner carries the same allele, beyond chance, is `r`): the helper allele has a fitness edge of `r b - c`, independent of its frequency. At `r = 0.5, c = 1`:
  - `b = 2.6` (r b / c = 1.3): helper allele above 0.95 after 300 generations;
  - `b = 1.4` (0.7): below 0.05;
  - the threshold is `b = 2` ± 15 per cent.
- Estimated versus true relatedness: with the helper using estimated `r' = r × 0.55` (the book's 0.275 for uncertain sibs, PDF 118), the threshold in `b` rises by the same factor (test: `b* = c / r'`).

**L12. The selfish herd (PDF 190 to 192).**
- Setup: a ring of length 1000, 20 prey at random places. Each step every prey moves 1 unit towards its nearest neighbour; a predator appears at a random place and takes the nearest prey. 300 steps.
- Expected (proto, 50 runs): mean nearest-neighbour distance falls to under 20 per cent of its random-start value (random start 24.8; proto end 0.14, ratio 0.006). Tolerance: ratio below 0.2. Edge prey suffer more attacks than centre prey (calibration, not yet seen).

**L13. Vampire bats (PDF 253 to 256).**
- The book gives no payoffs. The lab defines: nightly foraging success random per bat, a hunger store that falls by one per night, a bat dies at zero, a well-fed bat may give one unit to a roostmate with at most one left, reciprocators give to those who gave to them, non-sharers never give.
- **No numeric expectation is claimed.** A first prototype with simple payoffs showed no clear advantage for sharers across roost turnover, so the model needs a proper design (needs vary strongly day to day; giving costs little). L13 is **exploratory** at M2: it must run and report. A directional pass criterion (sharers do better in stable roosts, worse when roost membership turns over fast) is added after calibration and recorded here.

### 4.7 Island benchmark scenarios (later milestones)

An island scenario assigns benchmark policies to a chosen share of NPCs (the rest use the default `AssociativeNpcPolicy`), changes one environment or institution, runs many seeds, and compares measures. It uses the island rules from [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#2-principles): real consequences, no forcing. Experiment design, controls and pre-registration live in [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md).

| Id | Question | Policy mix | Treatments | Measures | Milestone |
|---|---|---|---|---|---|
| S1 | Do contests over berries and beds turn violent? | 0, 10 and 30 per cent `bench.hawk`; rest default | no institution; guards; healer; welfare store | escalation rate, injury per contest, deaths, food intake | M9 |
| S2 | Does resident/intruder arise by custom? | 20 per cent `bench.resident_rule`, 20 per cent `bench.intruder_rule` (seeded); rest default | with and without a hut register | yield-to-resident, injuries, share of hut disputes | M5 |
| S3 | Does help on credit survive cheats? | 5 per cent `bench.alld` (Cheat), 10 per cent `bench.grudger`, 10 per cent `bench.tft`; rest default | none; elders' mediation; headman's court (later); media (later) | unreturned-favour rate, cooperative-cluster survival, fingerprints | M7 |
| S4 | Barter as a Prisoner's Dilemma | 10 per cent `bench.alld`; rest default | stalls with regulars; state market; none | short-change rate, repeat-customer rate, trade volume | M7 |
| S5 | Do hut-mates share on unlucky days? | none (observe default NPCs) | shared store on or off; stable vs high-turnover huts | giving by hunger and surplus; gifts to non-kin | M5 |
| S6 | Do opposing units make truces? | two units, 50 per cent `bench.tft`-like contest policy for shots | rotation on or off; bounty on or off | engagement rate over time, symmetry of retaliation | M9 |
| S7 | Do partnership strategies cycle? | seed 20 per cent `bench.fast`, 20 per cent `bench.philanderer`; rest default | courts or none; welfare or none | shares of long-courtship and staying partners, cycle amplitude | M4 |
| S8 | Does the herd form in a raid? | none | guards or none | domain of danger before and during the raid | M9 |
| S9 | Are fakes caught? | 5 per cent sellers pass off low quality (visible quality above true quality, [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)) | inspector on or off | detected-lie share, reputation cost | M8 |
| S10 | Dig or enter? | none | hut register on or off; scarce or plentiful huts | share entering, payoff of each route | M3 |

**Acceptance style.**
- **Direction over many seeds, never exact numbers.** Each scenario runs at least 30 seeds per arm, paired by seed (same world, one treatment changed).
- **Two kinds of check.**
  1. **Mechanism checks (must pass):** the treatment really changed the payoffs it claims to (for example, with a court on, the realised cost of a broken promise is higher; with a hut register, entering an occupied hut carries the stated penalty). These are accounting checks on the event stream.
  2. **Outcome hypotheses (recorded, never pass/fail):** the expected direction from [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#411-how-institutions-shift-payoffs). The report gives the mean paired difference, a bootstrap 95 per cent interval, and the share of seeds where the sign matched. "No effect" and "opposite effect" are valid results and are published as found.
- **No tuning to get an effect.** If an expected effect does not show, the finding is recorded. Parameters are changed only for a documented bug or for balance reasons unrelated to the effect.
- **Benchmark assignment is recorded** as a treatment (see Telemetry), so analysts can separate seeded NPCs from the rest.

### 4.8 The runner interface (labs)

The runner is owned by [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md), which lists these `lab` subcommands in its command table. This section states what the lab needs from it.

```text
god-runner lab list                                  # experiments L1..L13 with their expected checks
god-runner lab run L1 [--seed N] [--seeds A..B] [--agents N] [--gens G]
                      [--dynamics replicator|agents] [--param key=value ...]
                      [--out DIR] [--format jsonl|csv]
god-runner lab all --check [--seeds 1..5]            # all experiments, exit code 1 if any check fails
god-runner lab report DIR                            # summarise result files into a table
god-runner lab hash L1 --seed N                      # print the result hash, for golden files
```

- Output per run: `result.jsonl` (one row per generation: shares and mean payoffs per strategy), `checks.json` (each check with measured, expected, tolerance, pass), and `summary.txt`. Results go in `runs/lab/<experiment>/<seed>/`.
- Exit code 0 only if every check passes. A failed check prints the tolerance and the measured value.
- Parallel runs are by seed, as for island runs. The result hash of one `(config, seed)` is identical wherever it runs.
- Island scenarios use the normal runner commands (`run`, `compare`) with a scenario file that lists benchmark assignments and treatments. See [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md).

## 5. Interactions with other systems

| System | Interaction |
|---|---|
| Policy registry ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) | Benchmarks register under `bench.*` keys. They use the same intents as every policy |
| Cognition and learning | A benchmark runs *instead of* the default policy only inside its domain. NPC learning continues in the background and is not used by the benchmark |
| Social memory | Per-partner histories feed the benchmark pair state |
| Contest record ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record)) | Contest benchmarks decide `ContestMove`s |
| Economy | Exchange benchmarks decide separate hand-overs (cooperate = hand over, defect = keep) |
| Telemetry and warehouse | Lab files and benchmark assignments |
| Policy experiments | Island scenarios are inputs to experiment designs |

## 6. Player and view

- **The player never sees benchmarks.** They are for experiments. In a normal game none are assigned.
- **Lab:** no view. Reports are files and a console table. A later editor chart tool may plot share curves.
- **Island scenarios:** the runner produces reports. If a scenario is opened in the Unity view for debugging, NPCs with an assigned benchmark show its key in the (debug-only) NPC inspector. The normal inspector never shows it.

## 7. Telemetry

**Lab (separate from the sim stream):**

| Record | Fields |
|---|---|
| `LabRunStarted` | experiment, seed, config hash, dynamics |
| `LabGeneration` | generation, shares per strategy, mean payoff per strategy |
| `LabCheck` | check name, measured, expected, tolerance, pass |
| `LabRunEnded` | result hash, pass or fail |

Files are JSONL. Warehouse tables (owner: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)): `fact_lab_match` and `fact_lab_population_step`; M2 writes the JSONL and CSV files, and M13 loads them. Lab data never enters a game's `StateHash`.

**Island:**
- `BenchmarkAssigned` (npc, policy key, tick), written once at scenario start. The warehouse stores the policy key as an NPC attribute so analyses can group on it.
- Normal domain events and fingerprints ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#7-telemetry)) do the rest.

## 8. Stage 4 minimum versus later

| Milestone | Delivered here |
|---|---|
| **M2** | Everything in `Sim/Lab`: all strategy state machines, the payoff matrices, the replicator and agent engines, L1 to L12 passing their checks, L13 running (exploratory), the `lab` commands, golden result hashes |
| M3 | Sunk-cost rule test on the default `AssociativeNpcPolicy`; `bench.dig` and `bench.enter` adapters for S10 |
| M4 | S7 (coy/fast and faithful/philanderer on the island) |
| M5 | `BenchmarkNpcPolicy` adapter for contests and gifts; S2, S5 |
| M7 | Exchange benchmarks on the island; S3, S4 |
| M8 | S9 (fakes need item quality) |
| M9 | Contest benchmarks with duel injuries; S1, S6, S8 |
| M13 | Lab and benchmark tables in the warehouse |
| M14 | Benchmark scenarios used as balance controls, not as targets |
| Later | Heavier institutions in the scenarios (courts, police, banks, media); a Gale-Eaves-faithful matrix if the payoffs are found; noise (trembling hand) and a round-two-sized strategy set |

**Out of the lab:** haplodiploidy, the origin of two sexes, meiotic drive, parasite manipulation, a full cuckoo-host race, stotting, Fisher's runaway. See the scope table in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#49-scope-summary).

## 9. Migration from the existing code

- Nothing exists yet. `Sim/Lab` is a new folder. It uses the existing seeded `Rng` and its forks, and compiles in `DotNet/` for fast tests.
- Lab code must not use `System.Random`, floating point, wall clock, or hash-ordered collections. The prototype used floating point only to find expected values. The C# lab is integer.
- The only existing code touched later is `Duel.cs` and `DuelSim.cs`, which island contests reuse (see [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#41-shared-mechanics)). The lab does not use them.

## 10. Tests and acceptance criteria

All tests run in EditMode and under `dotnet test DotNet`. The lab as a whole must run `lab all --check` in under 60 seconds on one core.

**State machines (unit tests, golden move sequences).**
1. Against a scripted partner `C D D C C D`, Tit for Tat answers `C C D D C C` (and so on for each strategy). One test per strategy with the exact expected sequence.
2. Tit for Two Tats: against `C D C D C D` it never defects. Against `C D D C` it defects on round 4 only.
3. Grudger: after one defection, it defects for 100 rounds whatever the partner does.
4. Remorseful Prober: after a probe that is answered with a defection, it cooperates once, and the next round follows the partner's last move.
5. Retaliator against Hawk escalates from turn 2. Bully against Retaliator withdraws on turn 3. Dove against Hawk withdraws on turn 2.
6. Bourgeois in role Intruder withdraws on turn 1. Prober with `q = 1000` permille probes on turn 1; with 0 never.
7. Random(p) over 100,000 draws cooperates at p permille ± 5.

**Matrices and payoffs.**
8. The contest rules produce each cell of the 4.2 matrices within 3 (L2b), including the resident/intruder matrix.
9. The dollar payoffs satisfy `T > R > P > S` and `(T + S)/2 < R`; changing them to violate the second fails the check.
10. Hawk/Dove fixed point is 583 ± 3 permille for several starts (0.01, 0.5, 0.99).

**Determinism.**
11. The same `(config, seed)` gives the same `ResultHash` twice in one process, in two processes, and under `dotnet test DotNet`. Golden hashes are stored after the first accepted run.
12. The replicator conserves the total: shares always sum to exactly 2^30.
13. Pairing shuffles are by forked stream: changing the number of agents in one experiment does not change another experiment's hash.
14. No `UnityEngine`, `System.Random`, `float` or `double` in `Sim/Lab` (a source scan test).

**Experiments.**
15. Each of L1 to L12 passes its listed checks for seeds 1 to 5. L13 runs and reports.
16. ABM against replicator: at `N = 10000`, Hawk share is within 0.03 of the replicator value; L9b threshold within 0.01.
17. **Negative controls:** an Euler step in place of RK4 in L6 fails the conserved-quantity check; a baseline `w0 = 100` in L1 fails the Hawk tolerance (the test documents the clipping bias).

**Island adapter.**
18. For a scripted history, the island adapter and the lab state machine produce the same move (all exchange benchmarks). 
19. A benchmark policy reads only `NpcObservation` and the NPC's mind view (the reflection test in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#7-policy-levels) covers `bench.*`).
20. A game with no benchmark assigned is identical (same hash) to one built before the benchmark module existed.

**Island scenarios (M5 and later).**
21. Each scenario runs 30 seeds per arm without invariant failures, and writes its report.
22. Mechanism checks pass (the treatment changed the payoff it claims to).
23. Outcome hypotheses are **recorded**; no test requires an effect.

## 11. Open questions
None needs the owner. Settled here, with defaults:
1. **Gale and Eaves.** Their payoffs are not in the book. We accept the mechanism-level result (L2) and claim no Hawk/Bully ESS. If the original payoffs are found, they become an extra experiment, L2c.
2. **Vampire bats.** L13 stays exploratory at M2 and gets a directional pass criterion only after its payoff design is calibrated and recorded here.
3. **Pecking orders.** L5 keeps weak expectations; Landau's h is reported, not checked. A more strength-dependent contest can tighten it later.
4. **Noise.** Noisy variants of L8 and L9 (1 per cent mistakes) are reported runs at M2, not pass criteria; trembling-hand checks come later.
5. **Probers count as nasty,** by Axelrod's definition (nice means never first to defect). The "nasty strategies go extinct" check uses that definition.
6. **Benchmark keys.** The `bench.*` registry prefix plus the `BenchmarkAssigned` event (stored as an NPC attribute in the warehouse) is enough; experiments need no separate treatment table.
