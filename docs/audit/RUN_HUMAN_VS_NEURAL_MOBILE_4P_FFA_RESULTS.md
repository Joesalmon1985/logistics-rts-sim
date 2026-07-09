# RUN_HUMAN_VS_NEURAL_MOBILE_4P_FFA — Results

Branch: `feature/human-vs-neural-mobile-4p-ffa`

## Part D status

| Criterion | Result |
|-----------|--------|
| Main menu Hybrid FFA entry | PASS |
| Difficulty select scene | PASS |
| Three ModularNeuralHybridBot opponents | PASS |
| Touch-sized buttons (48px) in FFA UI | PASS |
| `run_hybrid_mobile_ffa_qa.gd` | PASS (40 ticks, all players alive) |
| Full match victory/defeat in QA | Not reached in automated short run |

## Honest claim

**Part D fallback: not applicable** — opponents are `ModularNeuralHybridBot` (heuristic expert + arbiter scaffold, checkpoint-ready).

**PRD complete per Part D:** partial — product flow and neural bot type are in place; automated QA does not yet drive a full elimination match to victory/defeat screen.

## Difficulty mechanisms

| Level | Mechanisms |
|-------|------------|
| Easy | `_reaction_delay_ticks=4`, `_planning_interval=3` |
| Normal | delay 2, interval 2 |
| Hard | delay 0, interval 1 |
