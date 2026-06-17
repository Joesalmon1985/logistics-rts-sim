# RUN_02.8e — Human Economy Proof Harness and Scenario Viability

**Branch:** `feature/run-02-8c-test-runtime-efficiency`

## Goal

Answer whether the `ECON_BOT_002` proof scenario is human/oracle passable from the exact same start as the bot test — without weakening gates or changing authoritative economy rules.

## Hard rules

- Do not weaken `ECON_BOT_002` thresholds or test file
- Do not add starter buildings or hidden roads
- Do not change authoritative economy rules unless oracle proves impossibility and user approves
- Oracle (B0) before interactive UI (B)

## Acceptance checklist

- [ ] Phase 0: 02.8d WIP stashed/branched; tree clean for harness work
- [ ] Phase A: scenario contract documented
- [ ] Phase B0: oracle one-building fixture passes headlessly
- [ ] Phase B: interactive harness scene with dual-turn and gate overlays
- [ ] Phase C: replay export/verify
- [ ] Phase D: harness tests + smoke pass
- [ ] Phase E: playtest guide with outcome classification A–E
- [ ] Phase F: focused verification recorded
- [ ] Phase G: final report + checkpoint commits

## Success criteria

**RUN_02.8e succeeds even if `ECON_BOT_002` remains red**, provided the report classifies passability (outcome A–E).

See [`docs/audit/RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md`](../audit/RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md).
