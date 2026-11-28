# Slice 1 held-out report (unified world)

- held-out evaluation; seeds 900001-900020 (20), 12 persons per episode, 10 days x 1200 ticks; need mask 0x033; pain band 800 permille.
- Thresholds `TRAINING_THRESHOLDS.md` hash `b4a5136240c7db9e7cbf552002466b959a45c7563e923dfb5eb534959887dc73` (v1).
- Layouts (seed mod 4): mirrored / rotated / regenerated / unfamiliar start, all on regenerated held-out boards; teacher-free, no oracle, divinity off.
- **Verdict: TR-01: not passed; UA-learned: not green** (gate not met: T1; T2; T3 (not evaluable); T4 (not evaluable); T6 (not evaluable)).

## Measures by arm

| Arm | Brain | T1 survival | T2 pain share | T3 shelter | T4 collection | T5 interruption | T6 full start | T7 hand-over |
|---|---|---|---|---|---|---|---|---|
| untrained | untrained (same shape, init seed 1, injected) | 0.058 | 0.737 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| frozen | frozen (last BC checkpoint, injected) | 0.667 | 0.199 | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |
| trained | trained (FounderPolicyV1 INTERIM (game Brain.Kind = 1)) | 0.558 **not met** | 0.236 **not met** | not evaluable | not evaluable | not evaluable | not evaluable | not evaluable |

Thresholds apply to the trained arm (T1 >= 0.90 and >= untrained + 0.40; T2 <= 0.10; T3 >= 14/20; T4 >= 10/20; T6 >= 0.75 each seed). T1: trained - untrained = 0.500 (required >= 0.40). T3, T4, T5, T6, T7 cannot be evaluated in slice 1 (no buildings, construction, collection, hand-over or 72-person start are merged), so TR-01 cannot pass here whatever the survival result.

## Survival

| Arm | adults alive at day 10 | mean survival days per person | deaths by day 1/2/3/4/5/6/7/8/9/10 |
|---|---|---|---|
| untrained | 14/240 | 4.42 | 0 0 0 167 27 11 5 2 5 9 |
| frozen | 160/240 | 8.41 | 0 0 0 27 9 21 11 5 3 4 |
| trained | 134/240 | 8.19 | 0 0 0 30 6 21 14 9 21 5 |

Survival days per person (each seed: the persons in id order; 10.0 = alive at the end):

| Seed | Layout | untrained | frozen | trained |
|---|---|---|---|---|
| 900001 | rotated | 3.3 3.3 4.2 3.7 3.3 3.3 3.3 3.3 3.5 4.6 3.9 3.5 | 3.4 5.9 4.7 5.6 3.2 5.1 10.0 10.0 3.4 3.5 7.0 10.0 | 3.3 4.7 3.2 10.0 3.5 5.0 3.2 5.8 3.3 3.9 5.1 10.0 |
| 900002 | regenerated | 4.2 6.1 3.3 6.0 6.2 4.5 3.3 3.8 4.5 4.6 4.0 5.4 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900003 | unfamiliar_start | 3.4 10.0 9.9 9.7 5.6 10.0 3.3 3.3 10.0 3.8 10.0 3.9 | 5.0 3.3 3.9 4.0 4.9 10.0 5.5 3.7 6.3 6.8 4.5 3.6 | 5.9 10.0 10.0 5.4 5.2 9.8 5.2 7.3 8.5 10.0 5.0 6.1 |
| 900004 | mirrored | 10.0 4.4 5.0 3.3 9.6 3.7 3.8 10.0 4.5 3.5 10.0 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 5.7 | 6.1 8.9 5.7 6.2 6.2 5.8 5.5 3.6 6.1 7.2 6.9 10.0 |
| 900005 | rotated | 3.3 3.3 3.3 3.7 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 3.3 3.3 3.2 3.3 10.0 4.4 3.3 3.2 3.5 4.7 3.4 3.4 | 3.4 3.4 10.0 3.3 3.3 3.3 8.5 3.3 7.4 3.5 6.9 3.5 |
| 900006 | regenerated | 4.7 3.9 3.3 4.4 5.1 5.6 4.9 4.1 3.5 5.2 6.1 4.1 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900007 | unfamiliar_start | 5.5 7.3 4.3 3.7 4.2 10.0 3.3 3.3 5.6 3.5 9.2 3.9 | 5.8 5.2 5.2 5.6 5.8 7.1 5.5 6.5 5.4 9.3 7.1 7.4 | 5.1 5.1 6.1 5.1 4.9 9.7 7.4 5.3 7.5 5.2 8.8 6.5 |
| 900008 | mirrored | 3.3 3.7 3.3 3.3 3.3 3.3 3.3 3.8 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 3.3 10.0 3.8 3.2 4.2 10.0 10.0 8.3 10.0 |
| 900009 | rotated | 4.8 4.8 3.3 5.3 3.3 3.7 3.3 3.3 3.5 3.5 3.5 5.7 | 3.3 3.3 3.2 10.0 3.3 6.1 3.2 3.1 10.0 3.5 8.3 3.5 | 3.6 3.9 3.8 7.4 10.0 3.3 3.2 4.6 4.0 5.3 10.0 4.0 |
| 900010 | regenerated | 3.3 3.4 3.3 4.7 3.6 3.3 5.7 4.8 3.5 5.3 3.5 4.6 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 8.4 10.0 10.0 6.9 10.0 |
| 900011 | unfamiliar_start | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 5.5 6.1 6.2 6.2 5.6 5.0 6.5 5.7 10.0 5.3 5.0 6.2 | 8.4 8.1 8.4 9.2 10.0 8.3 7.8 8.3 9.4 10.0 8.4 8.3 |
| 900012 | mirrored | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 5.2 5.0 5.5 4.7 10.0 10.0 8.5 10.0 5.7 10.0 | 10.0 3.9 5.5 5.9 3.9 3.9 5.7 5.9 6.0 6.2 6.4 6.2 |
| 900013 | rotated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900014 | regenerated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 4.0 3.5 3.5 | 7.3 10.0 10.0 10.0 10.0 10.0 10.0 8.7 10.0 10.0 10.0 10.0 | 3.4 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900015 | unfamiliar_start | 3.3 7.5 8.6 10.0 8.4 9.9 9.1 3.3 8.2 10.0 10.0 4.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 4.6 10.0 10.0 10.0 10.0 10.0 |
| 900016 | mirrored | 3.3 3.4 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 3.7 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 8.7 8.4 7.7 7.9 8.3 8.3 8.4 8.4 9.3 8.1 8.6 10.0 |
| 900017 | rotated | 9.9 10.0 9.1 8.9 8.0 6.9 4.2 9.6 4.4 4.6 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900018 | regenerated | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 |
| 900019 | unfamiliar_start | 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 6.0 7.1 3.2 10.0 9.9 10.0 9.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 3.5 10.0 |
| 900020 | mirrored | 3.3 3.4 3.3 3.3 3.3 3.3 3.3 3.3 3.5 3.5 3.5 3.5 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 | 10.0 10.0 10.0 10.0 10.0 10.0 10.0 10.0 3.5 10.0 10.0 10.0 |

## Need levels over time

Mean pressure (permille) of the living persons at the end of each day; share of person-ticks in the hunger-or-thirst pain band in the last column.

**Hunger**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 694 | 999 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 | 1000 |
| frozen | 322 | 454 | 508 | 486 | 440 | 405 | 344 | 355 | 332 | 344 |
| trained | 209 | 380 | 467 | 423 | 427 | 406 | 385 | 371 | 269 | 248 |

**Thirst**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 624 | 872 | 862 | 568 | 496 | 367 | 245 | 260 | 314 | 260 |
| frozen | 367 | 415 | 367 | 282 | 290 | 234 | 181 | 161 | 163 | 183 |
| trained | 353 | 428 | 396 | 277 | 298 | 244 | 238 | 220 | 156 | 121 |

**Energy (fatigue)**

| Arm | d1 | d2 | d3 | d4 | d5 | d6 | d7 | d8 | d9 | d10 |
|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 138 | 447 | 438 | 440 | 452 | 439 | 431 | 435 | 457 | 443 |
| frozen | 420 | 534 | 529 | 508 | 510 | 509 | 530 | 518 | 510 | 522 |
| trained | 271 | 262 | 289 | 288 | 289 | 274 | 266 | 260 | 244 | 228 |

| Arm | person-ticks in hunger-or-thirst pain band |
|---|---|
| untrained | 937401/1272243 = 0.737 |
| frozen | 480955/2422033 = 0.199 |
| trained | 556627/2359248 = 0.236 |

## Accepted controls by control

Totals over the 20 episodes (per living person-day in brackets).

| Arm | TurnLeft | TurnRight | StepForward | StepBack | StepLeft | StepRight | Take | Drop | Use | Gesture | Sleep | Wait | decisions |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| untrained | 26949 (25.4) | 25815 (24.3) | 25365 (23.9) | 26365 (24.9) | 18757 (17.7) | 25347 (23.9) | 373 (0.4) | 803 (0.8) | 1927 (1.8) | 22169 (20.9) | 31220 (29.4) | 19901 (18.8) | 241016 |
| frozen | 19521 (9.7) | 36012 (17.8) | 21592 (10.7) | 1 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 0 (0.0) | 18335 (9.1) | 0 (0.0) | 4085 (2.0) | 59843 (29.6) | 596416 |
| trained | 18008 (9.2) | 27483 (14.0) | 19450 (9.9) | 0 (0.0) | 1 (0.0) | 0 (0.0) | 0 (0.0) | 1 (0.0) | 18449 (9.4) | 0 (0.0) | 23530 (12.0) | 50290 (25.6) | 667134 |

## Causes of death

Read from the last living tick (thirst at UN1's damage level 900, nutrition below the starvation level, both, else the legacy cause); the legacy body labels every health loss "starvation".

| Arm | deaths | causes |
|---|---|---|
| untrained | 226 | thirst 172, thirst+starvation 36, starvation 18 |
| frozen | 80 | thirst 64, thirst+starvation 14, starvation 2 |
| trained | 106 | thirst 83, thirst+starvation 15, starvation 8 |

Weights: untrained `e273a56ff0a4e004...`; frozen `204758c5ff631aa4...`; trained `485312934c34e6de...`. Rollout time 15 s.
