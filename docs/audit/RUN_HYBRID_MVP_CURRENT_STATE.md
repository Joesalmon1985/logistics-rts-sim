# Hybrid MVP — Current State Audit (C1)

Date: 2026-06-26  
Branch: `feature/mrp-full-stack-autonomous`

## Sim state

| Area | Status | Notes |
|------|--------|-------|
| `HybridVariant` | **Stub** | Extends `VariantStubBase`; no `EconomyInit`/`MilitaryInit` |
| Hybrid subsystem | **Missing** | No `sim/hybrid/` module |
| HYB-SMOKE tests | **Missing** | Planned in test matrix only |
| Scenarios | **Missing** | Only `kernel_smoke_hybrid.json` |

## Economy wares military needs (MVP)

- Construction: `planks`, `stone`, builder work
- Production: `field_supplies` or simplified `rations` + training time for Rifle Squad

## Military in hybrid

- Buildings: Barracks, Guard Post, Command Hub (economy HQ dual role)
- Production queues: ware-fed, no credits (DDR-031)
- Control: **delegated only** — `DelegatedAttackCommand`, defend posture; reject direct unit micro

## Snapshot gaps

Need for observer/3D: construction progress, carried ware, bot intent, blocked reason, combat events, player ownership on all entities.

## UI coverage

- Settler MVP: complete (reference)
- Military viewer: debug only
- Hybrid client: none
- 3D spectator: none

## Bot coverage

- Economy: `HeuristicEconomyBot`, planned `TutorialEconomyBot`
- Military: `HeuristicMilitaryBot`
- Hybrid: planned `TutorialHybridBot`, `HybridSpectatorBotA/B`

## Gap register (implementation order)

1. RUN_07: wire `HybridVariant`, ware-fed military construction/production, delegated commands
2. HYB-SMOKE-001–009 tests
3. Hybrid snapshot exporter extensions
4. 2D observer → playable MVP → tutorial → bots
5. 3D spectator consuming snapshots only

## Scope confirmation

- **Control model:** Option A delegated control
- **No credits** in hybrid variant
- **No neural training** in this run
