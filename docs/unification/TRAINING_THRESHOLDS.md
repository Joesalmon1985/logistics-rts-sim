# Training thresholds v1 (TR-01): predeclared before any held-out run

Owner: package UN6t (contracts §23.15). Source: [UNIFICATION_DESIGN.md §D9.2 c](UNIFICATION_DESIGN.md) (thresholds T1–T7, copied verbatim below), amendment §6 ([LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md)), owner decisions D9.8 and D9.10, lead ruling R86 (the TR-01 gate is T1, T2, T3, T4 and T6; T5 and T7 are evaluated and reported every time and are not part of the gate).

**This file was committed as the first commit of branch `unity/un6t`, before any held-out evaluation existed.** Its SHA-256 (rule in §7) is recorded in `docs/stage4/DEV_LOG.md` ("UN6t-a record"), in that commit's successor message, in every evaluation result and in every artifact header. **This file does not change after the first held-out run. A later change is a new versioned file (`TRAINING_THRESHOLDS_V2.md`) with a recorded reason, and the earlier result stays reported and counts.** The harness (`god-train eval`) refuses to run unless `--thresholds-hash` equals this file's hash, reads every number below from the machine-readable block in §8 (never from constants in code), and writes the hash into every result file.

## 1. Thresholds (§D9.2 c, verbatim)

All runs teacher-free, divinity off, no hidden path oracle, same need mask as training. Comparisons: **untrained** (same network, random initial weights), **frozen** (behaviour-cloned only, no fine-tuning) and **trained**.

| Id | Measure (held-out seeds 900001–900020 unless stated) | Threshold (L) |
|---|---|---|
| T1 survival | 12-person tribe, 10 sim days: share of adults alive | trained ≥ 0.90 and ≥ untrained + 0.40 |
| T2 need control | share of person-ticks in the pain band of hunger or thirst | trained ≤ 0.10 |
| T3 construction | seeds with ≥ 1 level-1 collection shelter completed by day 5 (no fixed order, own choice of design and site) | ≥ 14/20 |
| T4 collection | seeds with ≥ 1 raw unit collected and deposited by day 8 | ≥ 10/20 |
| T5 interruption | interrupted construction tasks resumed and completed (scripted interruption fixture, teacher-free afterwards) | ≥ 0.50 |
| T6 full start | 72-person unified start, held-out seeds 900001–900003, 20 days: adults alive | ≥ 0.75 on each seed |
| T7 interaction | seeds with ≥ 1 hand-over between persons by day 10 (reported for EP-03 learned; not a TR-01 blocker if S7 not trained) | ≥ 10/20 |

Basic survival (T1, T2, T6) and basic construction/collection (T3, T4) are the TR-01 gate. Social outcomes (strategy mix, cooperation rate, Hawk/Dove proportions, population growth) are **measured, never thresholds**. **A failed TR-01 means UA-learned is not green**: the result, metrics and artifact are kept; if a fallback is attached it is labelled `Brain.Kind = Fallback` in the save, the HUD and every report; a scripted controller or the old `Eat/Flee/Build` rules behind primitives is never called trained.

**Gate (R86):** TR-01 passes only if T1, T2, T3, T4 and T6 all hold, for the **trained** arm, on the real unified world. T5 and T7 are evaluated and reported prominently every time; a missed T5 or T7 does not fail TR-01 and is never hidden.

## 2. Exact measure definitions

Common terms. A **sim day** is `GameSetup.TicksPerDay` ticks of the evaluated game (1200 by default); "by day d" means by the end of tick `d × TicksPerDay` counted from the evaluation start (tick 0 of the episode). An **adult** is a person whose age band is adult at tick 0 of the episode; births and immigrants during the episode are never in a denominator. **Alive** means not dead at the end of the last tick of the stated horizon. The **pain band** of a need is pressure ≥ the band's lower bound that the needs package (UN1) defines for that channel in the evaluated tuning; if the evaluated tuning defines none, **800 permille**; the bound used is written into every result. A **person-tick** is one person alive at the start of one tick. Every measure is computed for every arm; thresholds apply to the **trained** arm (T1 also compares it with the untrained arm).

- **T1 survival.** Scenario: small generated island, one tribe of 12 persons (the training generator's population mix), held-out layout, 10 sim days, per seed 900001–900020. Value (pooled) = Σ_seeds adults alive at day 10 ÷ Σ_seeds adults at tick 0. Pass: trained ≥ 0.90 **and** trained − untrained ≥ 0.40 (both pooled over the same 20 seeds and layouts).
- **T2 need control.** Same 20 episodes as T1 (10 days). Value = (person-ticks with hunger **or** thirst pressure in its pain band) ÷ (all person-ticks), summed over all persons (adults and children) and seeds. Pass: trained ≤ 0.10.
- **T3 construction.** Same 20 episodes as T1, first 5 days. A seed counts when, by day 5, at least one building whose design is a level-1 collection shelter (a catalogue building whose level 1 has a non-zero `CollectWare`) reached completed level ≥ 1 during the episode; buildings present at tick 0 do not count. The design and site are the persons' own choice (no fixture, no teacher, no script). Pass: count ≥ 14 of 20.
- **T4 collection.** Same 20 episodes, first 8 days. A seed counts when at least one raw unit was collected (`CollectRaw` effect completed) and that same unit was deposited (`Deposit` effect completed into a collection building) by day 8. Pass: count ≥ 10 of 20.
- **T5 interruption.** Same 20 seeds and layouts, separate episodes. The interruption fixture (origin `ControlOrigin.Fixture`, the only non-policy origin allowed in an evaluation) waits until the first construction site that has received ≥ 25 % of its level-1 labour, then displaces every person within reach of that site by scripted steps away (at least 3 cells) and stops; the people then run teacher-free. Interrupted task = one such site (at most one per seed). Value = (interrupted sites completed within 3 sim days after the interruption) ÷ (seeds where an interruption happened). Pass: ≥ 0.50. A denominator of 0 is reported "not evaluable" and counts as not met.
- **T6 full start.** The 72-person unified start (UN5), held-out seeds 900001–900003, 20 sim days, trained arm. Value per seed = adults alive at day 20 ÷ adults at tick 0. Pass: ≥ 0.75 on **each** of the 3 seeds.
- **T7 interaction.** Same 20 episodes as T1, first 10 days. A seed counts when at least one hand-over between two distinct persons happened: person A holds out a lot (`HoldOut` effect) and person B ≠ A takes that lot (`TAKE` of a held-out lot) by day 10. No reciprocity is required or counted. Pass: count ≥ 10 of 20. If stage S7 was not trained the result is still computed and reported, labelled "S7 not trained".

## 3. Seed split and layouts

- **Training seeds:** 1–499,999. **Validation seeds** (model selection and stage criteria): 500,000–899,999. Both `< 900,000`.
- **Held-out evaluation seeds:** **900001–900020** (T1–T5, T7); **900001–900003** for T6. The trainer (`demo`, `bc`, `es`) and the training environment refuse every seed ≥ 900,000.
- **Harness self-test seeds** (stub world only, never a result): 990001–990020. They are ≥ 900,000, so the trainer refuses them too; the real held-out seeds are never run on the stub world.
- **Held-out layouts:** mirrored, rotated, regenerated (the board generator run on the held-out seed family) and unfamiliar start positions (start region outside the training start regions). The trainer refuses to load any of them; only `eval` opens them.

## 4. Arms

- **untrained:** the same network shape, random initial weights from initialisation seed 1 (no training).
- **frozen:** behaviour-cloned only (the last behaviour-cloning checkpoint, no evolution-strategies fine-tuning).
- **trained:** the frozen artifact (`FounderPolicyV<n>`) after fine-tuning, exactly as quantised to int16 and run by the game's integer inference.
- A **fallback** brain (`Brain.Kind = 2`) is never an arm, is labelled Fallback wherever it appears and is never counted as trained.
- An evaluation rollout containing any `ControlOrigin.Teacher` submission is invalid and aborts the evaluation.

## 5. Curriculum stage criteria (predeclared)

Each stage is advanced only when its criteria are met on validation seeds (never held-out). A stage that has not met its criteria when its compute budget ends is recorded **"criteria not met"**, kept in the report and the artifact header, and training continues with the next stage so the freeze still happens; such a stage is never reported as passed. Need masks are `Unification.Needs.ActiveMask` (bit order Hunger, Energy, Warmth, Safety, Pain, Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room). Evaluation uses the need mask of the last trained stage.

| Stage | Content | Need mask | Validation episodes | Criteria |
|---|---|---|---|---|
| S1 | motor control and exploration | 0x021 | 16 seeds, 1 person, 1 day | accepted-submission share ≥ 0.80 and mean distinct cells visited ≥ 12 |
| S2 | food, water, sleep | 0x033 | 16 seeds, 4 persons, 3 days | adults alive ≥ 0.95 and hunger-or-thirst pain-band share ≤ 0.15 |
| S3 | hygiene, comfort, social, fun, bladder | 0x7F3 | 16 seeds, 4 persons, 3 days | adults alive ≥ 0.95 and any-active-need pain-band share ≤ 0.20 |
| S4 | take, store, withdraw and carrying | 0xFFF | 16 seeds, 4 persons, 3 days | adults alive ≥ 0.95 and seeds with ≥ 1 deposit and ≥ 1 withdrawal ≥ 0.75 |
| S5 | selecting and completing useful construction | 0xFFF | 16 seeds, 6 persons, 5 days | seeds with ≥ 1 level-1 collection shelter completed ≥ 0.60 |
| S6 | raw collection, deposit, simple recipes | 0xFFF | 16 seeds, 6 persons, 8 days | seeds with raw collected and deposited ≥ 0.50 and seeds with ≥ 1 recipe completed ≥ 0.25 |
| S7 | offer, promise, hand-over, non-reciprocation | 0xFFF | 16 seeds, 6 persons, 10 days | seeds with ≥ 1 hand-over ≥ 0.40 |
| S8 | aggression, avoidance, retaliation, contested goods, childcare, observation | 0xFFF | 16 seeds, 12 persons, 5 days | adults alive ≥ 0.85 (strategies measured, none required) |

S5 waits for UN4, S7 for UN7, S8 for UN8a; S1–S4 need UN1, UN2, UN3b and UN6.

## 6. Feedback (what training may and may not reward)

Returns are computed **only** from the experienced change and level of the active need pressures (including pain), health and death of the person: per tick, for each active channel, the drive-change term `Learner.DriveReward(w=1000, comfort=250, prev, now)` plus a level term −(pressure − 250)⁺ ÷ 4, plus (health now − health before); death adds, once, the level term at full pressure on every active channel for every remaining tick of the episode (so dying is never better than living in need). Behaviour cloning fits labelled teacher demonstrations; evolution strategies optimise the undiscounted whole-episode return. **No reward** for professions, buildings completed, goods held, title, civilisational milestones, morality, cooperation or winning a dispute. The return function reads no holder, lot, building, site, ownership or count of goods (test `Returns_NoGoodsNoTitle`).

## 7. Hash rule

The thresholds hash is the lower-case hex SHA-256 of this file's UTF-8 bytes after converting every CR LF to LF (so a Windows checkout with `core.autocrlf` and a Unix checkout give the same hash). `god-train thresholds-hash` prints it.

## 8. Machine-readable block (read by the harness)

<!-- thresholds-json-begin -->
```json
{
  "name": "TRAINING_THRESHOLDS",
  "version": 1,
  "trainingSeeds": [1, 499999],
  "validationSeeds": [500000, 899999],
  "heldOutSeeds": [900001, 900020],
  "full72Seeds": [900001, 900003],
  "selfTestSeeds": [990001, 990020],
  "refuseSeedsFrom": 900000,
  "heldOutLayouts": ["mirrored", "rotated", "regenerated", "unfamiliar_start"],
  "arms": ["untrained", "frozen", "trained"],
  "untrainedInitSeed": 1,
  "painBandDefault": 800,
  "gate": ["T1", "T2", "T3", "T4", "T6"],
  "measures": [
    { "id": "T1", "kind": "adults_alive_share_pooled", "population": 12, "days": 10, "op": ">=", "value": 0.90, "minAboveUntrained": 0.40 },
    { "id": "T2", "kind": "pain_band_share_hunger_or_thirst", "population": 12, "days": 10, "op": "<=", "value": 0.10 },
    { "id": "T3", "kind": "seeds_with_l1_collection_shelter", "population": 12, "days": 5, "op": ">=", "count": 14, "of": 20 },
    { "id": "T4", "kind": "seeds_with_raw_collected_deposited", "population": 12, "days": 8, "op": ">=", "count": 10, "of": 20 },
    { "id": "T5", "kind": "interrupted_sites_completed_share", "population": 12, "days": 3, "op": ">=", "value": 0.50 },
    { "id": "T6", "kind": "adults_alive_share_each_seed", "population": 72, "days": 20, "op": ">=", "value": 0.75 },
    { "id": "T7", "kind": "seeds_with_handover", "population": 12, "days": 10, "op": ">=", "count": 10, "of": 20 }
  ],
  "stages": [
    { "id": "S1", "needMask": 33, "seeds": 16, "population": 1, "days": 1, "criteria": [ { "kind": "accepted_share", "op": ">=", "value": 0.80 }, { "kind": "mean_distinct_cells", "op": ">=", "value": 12 } ] },
    { "id": "S2", "needMask": 51, "seeds": 16, "population": 4, "days": 3, "criteria": [ { "kind": "adults_alive_share_pooled", "op": ">=", "value": 0.95 }, { "kind": "pain_band_share_hunger_or_thirst", "op": "<=", "value": 0.15 } ] },
    { "id": "S3", "needMask": 2035, "seeds": 16, "population": 4, "days": 3, "criteria": [ { "kind": "adults_alive_share_pooled", "op": ">=", "value": 0.95 }, { "kind": "pain_band_share_any_active", "op": "<=", "value": 0.20 } ] },
    { "id": "S4", "needMask": 4095, "seeds": 16, "population": 4, "days": 3, "criteria": [ { "kind": "adults_alive_share_pooled", "op": ">=", "value": 0.95 }, { "kind": "seeds_with_deposit_and_withdrawal_share", "op": ">=", "value": 0.75 } ] },
    { "id": "S5", "needMask": 4095, "seeds": 16, "population": 6, "days": 5, "criteria": [ { "kind": "seeds_with_l1_collection_shelter_share", "op": ">=", "value": 0.60 } ] },
    { "id": "S6", "needMask": 4095, "seeds": 16, "population": 6, "days": 8, "criteria": [ { "kind": "seeds_with_raw_collected_deposited_share", "op": ">=", "value": 0.50 }, { "kind": "seeds_with_recipe_share", "op": ">=", "value": 0.25 } ] },
    { "id": "S7", "needMask": 4095, "seeds": 16, "population": 6, "days": 10, "criteria": [ { "kind": "seeds_with_handover_share", "op": ">=", "value": 0.40 } ] },
    { "id": "S8", "needMask": 4095, "seeds": 16, "population": 12, "days": 5, "criteria": [ { "kind": "adults_alive_share_pooled", "op": ">=", "value": 0.85 } ] }
  ]
}
```
<!-- thresholds-json-end -->
