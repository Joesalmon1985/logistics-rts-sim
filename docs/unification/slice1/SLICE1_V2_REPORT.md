# Slice 1 V2 held-out report: FounderPolicyV2 (unified world)

V2 was trained and selected on training and validation seeds only, frozen (commit `71dc3b9`, weights `26df24c076b168c3af0dc2e456e98a3e6eebfe0043709bdcdd09af5fa814e940`, status INTERIM) and then run **once** on held-out seeds 900001-900020 with the same arms as V1 (untrained, frozen, trained). Thresholds file and hash unchanged (`b4a5136240c7db9e...`). V1's result (`SLICE1_REPORT.md`, `eval_12p.json`, FounderPolicyV1 FAILED-TR01) stands and is not relabelled.

## V2 beside V1 (12 persons, 10 days, held-out seeds 900001-900020)

| Arm | V1 T1 | V1 T2 | V2 T1 | V2 T2 |
|---|---|---|---|---|
| untrained | 0.058 | 0.737 | 0.058 | 0.737 |
| frozen (last BC checkpoint) | 0.667 | 0.199 | 0.967 | 0.016 |
| trained (game brain, FounderPolicyV<n>, Brain.Kind = 1) | 0.558 not met | 0.236 not met | **0.975 met** | **0.022 met** |

V2 trained - untrained = 0.917 (required >= 0.40). T1 and T2 are met for the trained arm. **TR-01 is still not passed** because T3, T4 and T6 cannot be evaluated in slice 1 (no buildings, collection or 72-person start merged); UA-learned is not green.

Adults alive at day 10 by held-out layout (60 adults per layout):

| Arm | mirrored | rotated | regenerated | unfamiliar start |
|---|---|---|---|---|
| V1 frozen | 52 | 30 | 58 | 20 |
| V1 trained | 21 | 29 | 57 | 27 |
| V2 frozen | 60 | 53 | 59 | 60 |
| V2 trained | 60 | 54 | 60 | 60 |

V2 trained: 6 deaths, all thirst, all on one rotated seed (900005; the frozen arm lost 7 there and 1 on regenerated seed 900010). V1 trained: 106 deaths (thirst 83, thirst+starvation 15, starvation 8).

## Cause found and what changed

- **Cause.** V1's training never left the generator's start region: every training and validation episode started where the held-out regenerated layout starts (hence 57/60 there) and the teacher, when nothing was perceived or remembered, explored only after thirst rose and then used a **path oracle** from thirst 750 / hunger 850. Those oracle steps were labels the network could not imitate from its percept, and they covered exactly the states (water out of sight, unfamiliar ground) that held-out transformed starts produce. With 12 persons the network also had to point USE at the right one of 16 entity slots crowded by tribe members; it had little on-policy data there (12-person probes before DAgger: thousands of `Use:NoInteraction` refusals per episode).
- **Training starts** (`UnifiedConfig.Starts = 1`, CLI `--starts varied`, training/validation seeds and `Layout.Train` only): each seed's tribe starts, by a keyed draw, at the generator's start, its left-right mirror, its 120-degree rotation, a node in the far quarter of the island, or any passable node. Same kinds of transform as the held-out layouts, different seeds; held-out seeds and layouts remain refused by the trainer.
- **Percept-only teacher** (`SurvivalTeacher.PerceptOnly`, `--teacher percept`, training only): reads only the Obs v1 percept (needs, carry and own load, six cells, entities, place memory) and the static catalogue; no path oracle and no world query. When no fresh water or food is perceived or remembered it searches (straight lines, a new heading now and then, turning when blocked) before the need is pressing. On 32 training seeds, 12 persons x 10 days, varied starts: alive 371/384 with no oracle (V1 teacher with oracle 378/384).
- **DAgger on the states the policy reaches**, mostly in the 12-person 10-day scenario (14 rounds). Validation (12p x 10d, 16 seeds, varied starts) rose from alive 0.72 / pain 0.14 after BC alone to 0.97-1.00 / 0.01-0.06 in the later rounds.
- **ES** in the evaluated scenario (12 persons x 10 days, varied starts) instead of V1's 4 persons x 6 days; fitness unchanged (the thresholds file's returns, which already charge death the full-pressure level for every remaining tick).
- Unchanged: network shape `298,24,32,64,166`, input map 2 (R89), Obs v1 and Act v1 schemas and hashes, the game's integer inference, the thresholds and their hash, the evaluator.

## Training record (training seeds < 500000, validation seeds 500001-500032)

- Demos (percept teacher, varied starts): S1 seeds 1001-1064 (1 person, 1 day; 8,088 decisions), S2 seeds 2001-2256 (4 persons, 3 days; 311,186), seeds 3001-3048 (12 persons, 10 days; 518,837). BC 20 epochs (lr 0.003, batch 4).
- DAgger: rounds 1-6 (beta 0.2, 0.2, then 0; odd rounds 32 seeds of 4p x 6d, even rounds 16 seeds of 12p x 10d), rounds 7-14 (beta 0, 24 seeds of 12p x 10d each), seeds 10101-11424; BC 3 epochs per round (batch 8) on all data. Frozen checkpoint = last BC checkpoint (round 14): validation 12p x 10d (32 seeds) alive 0.987, pain 0.021; S1 criteria met (accepted 0.895, cells 25.9); S2 criteria met (alive 1.000, pain 0.001).
- ES from the frozen checkpoint: seeds 30001-30400, 16 antithetic pairs, 1 episode of 12p x 10d per candidate, sigma 0.005, lr 0.0005; 4 chunks x 5 iterations, each validated (12p x 10d, 32 validation seeds): alive/pain 0.995/0.018, **1.000/0.016**, 0.995/0.013, 0.987/0.031. Predeclared rule (V1's): highest validation alive, ties lower pain -> chunk 2 (10 iterations) = FounderPolicyV2.
- Compute: about 45 minutes wall time on 15 workers for V2 (demos, BC, DAgger, ES, validation, held-out run 12 s).

## Open for the owner

- Artifact status: V2 meets T1 and T2 but TR-01 cannot pass in slice 1 (T3, T4, T6 not evaluable). It stays **INTERIM** (never TRAINED); the owner may prefer the FAILED-TR01 label V1 carries.
- Input map 2 (R89, lead default) remains unconfirmed by the owner; V2 uses it.
- Training on transformed starts uses the same kinds of transform as the held-out layouts (on training seeds); if the owner wants the held-out layouts to be unseen kinds, a different held-out family would be needed in a new thresholds version.

## Generated held-out report (god-train s1-report of eval_v2_12p.json)

- held-out evaluation; seeds 900001-900020 (20), 12 persons per episode, 10 days x 1200 ticks; need mask 0x033; pain band 800 permille.
- Thresholds `TRAINING_THRESHOLDS.md` hash `b4a5136240c7db9e7cbf552002466b959a45c7563e923dfb5eb534959887dc73` (v1).
- Layouts (seed mod 4): mirrored / rotated / regenerated / unfamiliar start, all on regenerated held-out boards; teacher-free, no oracle, divinity off.
- **Verdict: TR-01: not passed; UA-learned: not green** (gate not met: T3 (not evaluable); T4 (not evaluable); T6 (not evaluable)).

## Measures by arm

| Arm | Brain | T1 survival | T2 pain share | T3 shelter | T4 collection | T5 interruption | T6 full start | T7 hand-over |
|---|---|---|---|---|---|---|---|---|
| untrained | untrained (same shape, init seed 1, injected) | 0.058 | 0.737 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| frozen | frozen (last BC checkpoint, injected) | 0.967 | 0.016 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| trained | trained (FounderPolicyV2 INTERIM (game Brain.Kind = 1)) | 0.975 met | 0.022 met | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |

Thresholds apply to the trained arm (T1 >= 0.90 and >= untrained + 0.40; T2 <= 0.10; T3 >= 14/20; T4 >= 10/20; T6 >= 0.75 each seed). T1: trained - untrained = 0.917 (required >= 0.40). T3, T4, T5, T6, T7 cannot be evaluated in slice 1 (no buildings, construction, collection, hand-over or 72-person start are merged), so TR-01 cannot pass here whatever the survival result.

## Survival

| Arm | adults alive at day 10 | mean survival days per person | deaths by day 1/2/3/4/5/6/7/8/9/10 |
|---|---|---|---|
| untrained | 14/240 | 4.42 | 0 0 0 167 27 11 5 2 5 9 |
| frozen | 232/240 | 9.80 | 0 0 0 5 2 1 0 0 0 0 |
| trained | 234/240 | 9.86 | 0 0 0 2 3 0 1 0 0 0 |

Survival days per person (each seed: the persons in id order; 10.0 = alive at the end):

| Seed | Layout | untrained | frozen | trained |
|---|---|---|---|---|
| 900001 | rotated | 3.3 3.3 4.2 3.7 3.3 3.3 3.3 3.3 3.5 4.6 3.9 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900002 | regenerated | 4.2 6.1 3.3 6.0 6.2 4.5 3.3 3.8 4.5 4.6 4.0 5.4 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900003 | unfamiliar_start | 3.4 10.0 9.9 9.7 5.6 10.0 3.3 3.3 10.0 3.8 10.0 3.9 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900004 | mirrored | 10.0 4.4 5.0 3.3 9.6 3.7 3.8 10.0 4.5 3.5 10.0 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900005 | rotated | 3.3 3.3 3.3 3.7 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 3.3 10.0 4.4 4.3 10.0 10.0 10.0 3.2 10.0 3.4 3.5 3.4 | 10.0 10.0 10.0 4.8 3.3 6.0 4.6 3.3 10.0 10.0 5.0 10.0 |
| 900006 | regenerated | 4.7 3.9 3.3 4.4 5.1 5.6 4.9 4.1 3.5 5.2 6.1 4.1 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900007 | unfamiliar_start | 5.5 7.3 4.3 3.7 4.2 10.0 3.3 3.3 5.6 3.5 9.2 3.9 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900008 | mirrored | 3.3 3.7 3.3 3.3 3.3 3.3 3.3 3.8 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900009 | rotated | 4.8 4.8 3.3 5.3 3.3 3.7 3.3 3.3 3.5 3.5 3.5 5.7 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900010 | regenerated | 3.3 3.4 3.3 4.7 3.6 3.3 5.7 4.8 3.5 5.3 3.5 4.6 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 5.7 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900011 | unfamiliar_start | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900012 | mirrored | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900013 | rotated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900014 | regenerated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 4.0 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900015 | unfamiliar_start | 3.3 7.5 8.6 10.0 8.4 9.9 9.1 3.3 8.2 10.0 10.0 4.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900016 | mirrored | 3.3 3.4 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900017 | rotated | 9.9 10.0 9.1 8.9 8.0 6.9 4.2 9.6 4.4 4.6 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900018 | regenerated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900019 | unfamiliar_start | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900020 | mirrored | 3.3 3.4 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |

## Need levels over time

Mean pressure (permille) of the living persons at the end of each day; share of person-ticks in the hunger-or-thirst pain band in the last column.

**Hunger**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 694 | 999 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 |
| frozen | 294 | 289 | 269 | 295 | 285 | 293 | 286 | 277 | 301 | 276 |
| trained | 119 | 180 | 183 | 197 | 197 | 198 | 200 | 203 | 199 | 203 |

**Thirst**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 624 | 872 | 862 | 568 | 496 | 367 | 245 | 260 | 314 | 260 |
| frozen | 191 | 166 | 184 | 164 | 145 | 146 | 144 | 156 | 151 | 165 |
| trained | 152 | 133 | 141 | 118 | 117 | 111 | 119 | 114 | 116 | 110 |

**Energy (fatigue)**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 138 | 447 | 438 | 440 | 452 | 439 | 431 | 435 | 457 | 443 |
| frozen | 286 | 415 | 390 | 374 | 349 | 372 | 350 | 363 | 351 | 361 |
| trained | 237 | 203 | 193 | 195 | 199 | 219 | 198 | 199 | 201 | 215 |

| Arm | person-ticks in hunger-or-thirst pain band |
|---|---|
| untrained | 937401/1272243 = 0.737 |
| frozen | 44282/2821474 = 0.016 |
| trained | 63607/2840288 = 0.022 |

## Accepted controls by control

Totals over the 20 episodes (per living person-day in brackets).

| Arm | TurnLeft | TurnRight | StepForward | StepBack | StepLeft | StepRight | Take | Drop | Use | Gesture | Sleep | Wait | decisions |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 26949 (25.4) | 25815 (24.3) | 25365 (23.9) | 26365 (24.9) | 18757 (17.7) | 25347 (23.9) | 373 (0.4) | 803 (0.8) | 1927 (1.8) | 22169 (20.9) | 31220 (29.4) | 19901 (18.8) | 241016 |
| frozen | 11944 (5.1) | 38889 (16.5) | 25772 (11.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 26237 (11.2) | 0 (0.0) | 2547 (1.1) | 103555 (44.0) | 273957 |
| trained | 14315 (6.0) | 34616 (14.6) | 26300 (11.1) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 29433 (12.4) | 0 (0.0) | 20766 (8.8) | 89463 (37.8) | 311941 |

## Causes of death

Read from the last living tick (thirst at UN1's damage level 900, nutrition below the starvation level, both, else the legacy cause); the legacy body labels every health loss "starvation".

| Arm | deaths | causes |
|---|---|---|
| untrained | 226 | thirst 172, thirst+starvation 36, starvation 18 |
| frozen | 8 | thirst 8 |
| trained | 6 | thirst 6 |

Weights: untrained `e273a56ff0a4e004...`; frozen `4a34a856f3459d56...`; trained `26df24c076b168c3...`. Rollout time 12 s.
