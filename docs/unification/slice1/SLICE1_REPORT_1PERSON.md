# Slice 1 held-out report (unified world)

- SUPPLEMENTARY held-out run, 1 person(s) per episode (not the T1 scenario of 12; gate values below are not TR-01 results); seeds 900001-900020 (20), 1 persons per episode, 10 days x 1200 ticks; need mask 0x033; pain band 800 permille.
- Thresholds `TRAINING_THRESHOLDS.md` hash `b4a5136240c7db9e7cbf552002466b959a45c7563e923dfb5eb534959887dc73` (v1).
- Layouts (seed mod 4): mirrored / rotated / regenerated / unfamiliar start, all on regenerated held-out boards; teacher-free, no oracle, divinity off.
- **Supplementary run: the measures below use the T1-T7 formulas on a different population; they are not TR-01 results and no threshold is applied.**
- **Verdict: TR-01: not passed; UA-learned: not green** (gate not met: T3 (not evaluable); T4 (not evaluable); T6 (not evaluable)).

## Measures by arm

| Arm | Brain | T1 survival | T2 pain share | T3 shelter | T4 collection | T5 interruption | T6 full start | T7 hand-over |
|---|---|---|---|---|---|---|---|---|
| untrained | untrained (same shape, init seed 1, injected) | 0.050 | 0.751 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| frozen | frozen (last BC checkpoint, injected) | 0.950 | 0.059 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| trained | trained (FounderPolicyV1 INTERIM (game Brain.Kind = 1)) | 0.900 | 0.045 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |

Thresholds apply to the trained arm (T1 >= 0.90 and >= untrained + 0.40; T2 <= 0.10; T3 >= 14/20; T4 >= 10/20; T6 >= 0.75 each seed). T1: trained - untrained = 0.850 (required >= 0.40). T3, T4, T5, T6, T7 cannot be evaluated in slice 1 (no buildings, construction, collection, hand-over or 72-person start are merged), so TR-01 cannot pass here whatever the survival result.

## Survival

| Arm | adults alive at day 10 | mean survival days per person | deaths by day 1/2/3/4/5/6/7/8/9/10 |
|---|---|---|---|
| untrained | 1/20 | 4.72 | 0 0 0 13 2 1 1 0 0 2 |
| frozen | 19/20 | 9.67 | 0 0 0 1 0 0 0 0 0 0 |
| trained | 18/20 | 9.38 | 0 0 0 1 1 0 0 0 0 0 |

Survival days per person (each seed: the persons in id order; 10.0 = alive at the end):

| Seed | Layout | untrained | frozen | trained |
|---|---|---|---|---|
| 900001 | rotated | 3.3 | 10.0 | 10.0 |
| 900002 | regenerated | 4.8 | 10.0 | 10.0 |
| 900003 | unfamiliar_start | 10.0 | 10.0 | 10.0 |
| 900004 | mirrored | 4.4 | 10.0 | 10.0 |
| 900005 | rotated | 3.3 | 3.3 | 3.4 |
| 900006 | regenerated | 5.2 | 10.0 | 10.0 |
| 900007 | unfamiliar_start | 9.9 | 10.0 | 4.2 |
| 900008 | mirrored | 3.3 | 10.0 | 10.0 |
| 900009 | rotated | 3.4 | 10.0 | 10.0 |
| 900010 | regenerated | 3.3 | 10.0 | 10.0 |
| 900011 | unfamiliar_start | 3.3 | 10.0 | 10.0 |
| 900012 | mirrored | 3.3 | 10.0 | 10.0 |
| 900013 | rotated | 3.4 | 10.0 | 10.0 |
| 900014 | regenerated | 3.4 | 10.0 | 10.0 |
| 900015 | unfamiliar_start | 9.9 | 10.0 | 10.0 |
| 900016 | mirrored | 3.3 | 10.0 | 10.0 |
| 900017 | rotated | 6.5 | 10.0 | 10.0 |
| 900018 | regenerated | 3.4 | 10.0 | 10.0 |
| 900019 | unfamiliar_start | 3.3 | 10.0 | 10.0 |
| 900020 | mirrored | 3.3 | 10.0 | 10.0 |

## Need levels over time

Mean pressure (permille) of the living persons at the end of each day; share of person-ticks in the hunger-or-thirst pain band in the last column.

**Hunger**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 679 | 999 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 |
| frozen | 289 | 299 | 305 | 331 | 312 | 338 | 303 | 303 | 318 | 303 |
| trained | 139 | 257 | 260 | 213 | 202 | 207 | 212 | 234 | 195 | 177 |

**Thirst**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 619 | 817 | 863 | 488 | 371 | 173 | 230 | 307 | 114 | 329 |
| frozen | 232 | 191 | 173 | 141 | 164 | 129 | 120 | 105 | 202 | 143 |
| trained | 212 | 266 | 194 | 122 | 114 | 111 | 82 | 128 | 75 | 99 |

**Energy (fatigue)**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 147 | 461 | 439 | 431 | 429 | 456 | 459 | 465 | 402 | 408 |
| frozen | 328 | 482 | 500 | 492 | 499 | 473 | 484 | 514 | 531 | 535 |
| trained | 221 | 215 | 212 | 227 | 206 | 177 | 207 | 231 | 240 | 222 |

| Arm | person-ticks in hunger-or-thirst pain band |
|---|---|
| untrained | 85035/113236 = 0.751 |
| frozen | 13761/231998 = 0.059 |
| trained | 10063/225077 = 0.045 |

## Accepted controls by control

Totals over the 20 episodes (per living person-day in brackets).

| Arm | TurnLeft | TurnRight | StepForward | StepBack | StepLeft | StepRight | Take | Drop | Use | Gesture | Sleep | Wait | decisions |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 2632 (27.9) | 2219 (23.5) | 2244 (23.8) | 2122 (22.5) | 1731 (18.3) | 2268 (24.0) | 5 (0.1) | 42 (0.4) | 220 (2.3) | 1960 (20.8) | 2983 (31.6) | 1749 (18.5) | 21265 |
| frozen | 1653 (8.6) | 3631 (18.8) | 1920 (9.9) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 2017 (10.4) | 0 (0.0) | 243 (1.3) | 8432 (43.6) | 26338 |
| trained | 1703 (9.1) | 2954 (15.7) | 2289 (12.2) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 2481 (13.2) | 0 (0.0) | 1002 (5.3) | 7068 (37.7) | 24707 |

## Causes of death

Read from the last living tick (thirst at UN1's damage level 900, nutrition below the starvation level, both, else the legacy cause); the legacy body labels every health loss "starvation".

| Arm | deaths | causes |
|---|---|---|
| untrained | 19 | thirst 13, thirst+starvation 3, starvation 3 |
| frozen | 1 | thirst 1 |
| trained | 2 | thirst 2 |

Weights: untrained `e273a56ff0a4e004...`; frozen `204758c5ff631aa4...`; trained `485312934c34e6de...`. Rollout time 3 s.
