# Hybrid AI Curriculum (PRD A.9)

Flat stages 0–9 and modular M0–M10 gates for 4-player hybrid logistics RTS training.

## First-run target

- Flat Stages 0–2: environment sanity, teacher imitation, useful building
- Modular M0–M5: candidate interface, expert imitation, arbiter imitation

## Promotion gates (summary)

| Stage | Gate |
|-------|------|
| 0 | 100 rollouts, invalid rate < 10% |
| 1 | Teacher match > 60% |
| 2 | ≥3 useful buildings in 80% eval |
| M0 | Heuristic experts + arbiter choose legal candidates |
| M5 | Arbiter imitation from scripted hybrid bot |

See PRD Part A for full reward tables.
