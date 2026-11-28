# Implementation checklist (D11)

The only plan in force. Decisions: `docs/civdiv/OWNER_DECISION_REGISTER.md` (D9, D11). Design detail: `docs/unification/UNIFICATION_DESIGN.md` §D9 (read only the parts a task needs).

## Merged into unity/poc
- [x] UN0 foundation (unified switch, legacy isolation, goods without title)
- [x] UN1 world (61-hex disc, seven terrains, infinite forage, water, needs, dice clock)
- [x] UN3a catalogue (workbook rows; UA subset generated)
- [x] UN6t-a training harness (`god-train`, thresholds committed b441f57, hash b4a51362…)
- [x] UN2 primitive executor (12 controls, Obs/Act encoding, legality mask)
- [x] UN6 brain runtime (int inference, QNet parity, artifact header, per-game policy)

## Slice 1: one trained NPC survives independently (in progress)
- [ ] Fix `UN6BrainTests.Decision_OnCompletionOnly` against the real motor
- [ ] Real-world training environment (UnifiedWorld) and teacher for eat, drink, sleep
- [ ] Train S1–S3; freeze artifact; evaluate teacher-free on held-out seeds 900001–900020 against the committed thresholds; untrained/frozen controls
- [ ] Headless report: survival days, needs, actions, causes of death

## Later slices (not started; branches kept)
- Slice 2 shelter: `unity/un3b` (buildings, collection gate), `unity/un4` (construction)
- Slice 3 exchange and conflict: `unity/un8a` (strike, equip, treat), UN7 (not started)
- Legacy isolation polish: `unity/un9`
- Slices 4–7: population start (UN5), groups, settlements, scale
