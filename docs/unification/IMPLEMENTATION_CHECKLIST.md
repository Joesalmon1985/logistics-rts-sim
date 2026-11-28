# Implementation checklist (D11)

The only plan in force. Decisions: `docs/civdiv/OWNER_DECISION_REGISTER.md` (D9, D11). Design detail: `docs/unification/UNIFICATION_DESIGN.md` §D9 (read only the parts a task needs).

## Merged into unity/poc
- [x] UN0 foundation (unified switch, legacy isolation, goods without title)
- [x] UN1 world (61-hex disc, seven terrains, infinite forage, water, needs, dice clock)
- [x] UN3a catalogue (workbook rows; UA subset generated)
- [x] UN6t-a training harness (`god-train`, thresholds committed b441f57, hash b4a51362…)
- [x] UN2 primitive executor (12 controls, Obs/Act encoding, legality mask)
- [x] UN6 brain runtime (int inference, QNet parity, artifact header, per-game policy)

## Slice 1: one trained NPC survives independently (branch `unity/s1`; TR-01 not passed)
- [x] Fix `UN6BrainTests.Decision_OnCompletionOnly` against the real motor
- [x] Real-world training environment (UnifiedWorld) and teacher for eat, drink, sleep
- [x] Train S1–S3; freeze artifact; evaluate teacher-free on held-out seeds 900001–900020 against the committed thresholds; untrained/frozen controls (S1, S2 trained; S3 not trainable until UN3b's reliefs; `FounderPolicyV1` FAILED-TR01: 12-person T1 0.558, T2 0.236; one person alone 18/20)
- [x] Headless report: survival days, needs, actions, causes of death (`docs/unification/slice1/`)
- [ ] Owner: confirm the network input map 2 (lead default R89: accepted) (derived nearest water/food inputs) or choose another input design
- [x] V2 founder policy that finds water from unfamiliar starts in a 12-person tribe (reported beside V1; branch `unity/s1b`: `FounderPolicyV2` INTERIM, held-out T1 0.975, T2 0.022, both met; TR-01 still not passed: T3, T4, T6 not evaluable; `docs/unification/slice1/SLICE1_V2_REPORT.md`)
- [ ] Owner: V2 label (INTERIM, as frozen) or FAILED-TR01 like V1, since TR-01 cannot pass in slice 1

## Later slices (not started; branches kept)
- Slice 2 shelter: `unity/un3b` (buildings, collection gate), `unity/un4` (construction)
- Slice 3 exchange and conflict: `unity/un8a` (strike, equip, treat), UN7 (not started)
- Legacy isolation polish: `unity/un9`
- Slices 4–7: population start (UN5), groups, settlements, scale
