# Units, Resources, Buildings Catalogue

Original placeholder names. Names may change; do not use protected reference-game names.

**Logistics terminology (ratified):** Simulation entity is **road node**. **Flag** is a player-facing label only. Buildings connect to the road network via **`ports[]`** offsets from anchor (default `[[0, 0]]` when omitted). See `15_worker_carrier_road_node_logistics_foundation_RATIFIED.md`.

**Config IDs vs catalogue names:** Runtime config may use different display IDs (e.g. config `canteen` vs catalogue “Meal hall”). Implementation and tests use **config IDs** under `godot_project/sim/data/config/`.

---

## Wares (Economy)

| Ware ID | Display name | Source / role |
|---------|--------------|---------------|
| timber | Timber | Woodcutter |
| planks | Planks | Sawmill |
| stone | Stone | Quarry |
| grain | Grain | Farm |
| water | Water | Well |
| bread | Bread | Bakery |
| rations | Rations | Rations kitchen (mine food support, basic tier) |
| meals | Meals | Meal hall (deep mine food support) |
| coal | Coal | Coal mine |
| iron_ore | Iron ore | Iron mine |
| iron_ingots | Iron ingots | Smelter |
| tools | Tools | Workshop (generic Run 02; schema supports tool categories later) |
| builder_work | Builder work | Abstract construction progress (after material delivery); not a site-assigned worker in v1 |

### Config-present wares not yet fully tested in economy suite

| Ware ID | Notes |
|---------|--------|
| bricks | Defined in `wares.json`; `brick_kiln` + `bricks_from_stone` in config — **RUN_02.7** should prove config-only chain |

### Future expansion (config-driven, not necessarily implemented)

New harvestables (clay, sheep, etc.) and chains should be addable via `wares.json`, `recipes.json`, `buildings.json`, and `construction_costs.json` without simulation enum changes (DDR-LOG-007). Do not document as implemented until tests exist.

### Future tool categories (schema hook, not required Run 02)

`cutting_tool`, `digging_tool`, `building_tool`, `farming_tool`, `mining_tool`, `smithing_tool`

Run 02 may map all specialist needs to `tools` until categories are added.

---

## Economy Buildings

Buildings connect to the logistics network at **ports** — `[dq, dr]` offsets from `anchor_q`/`anchor_r` (see `06_config_and_data_schema.md`). One road node per port when RUN_02.7 entrance model is implemented.

| ID | Name | Footprint | Inputs | Outputs | Notes |
|----|------|-----------|--------|---------|-------|
| headquarters | Headquarters | 3×3 | — | holds workers, starting wares | Starting building; worker pool |
| storehouse | Storehouse | 2×2 | — | stores wares, holds workers | Worker/ware hub |
| woodcutter | Woodcutter | 1×1 | forest tree density | timber | Requires woodcutter worker + tools |
| forester | Forester | 1×1 | — | increases tree density | No direct ware output |
| sawmill | Sawmill | 2×1 | timber | planks | Upgrade target: enhanced_sawmill |
| enhanced_sawmill | Enhanced sawmill | 2×1 | timber | planks (faster) | Upgrade from sawmill |
| quarry | Quarry | 2×2 | rock deposit | stone | |
| farm | Farm | 2×2 | fertile hex | grain | |
| well | Well | 1×1 | water hex | water | |
| bakery | Bakery | 2×1 | grain, water | bread | |
| rations_kitchen | Rations kitchen | 2×1 | bread or grain+water | rations | |
| meal_hall | Meal hall | 2×2 | rations + grain/water | meals | |
| coal_mine | Coal mine | 2×2 | coal deposit, rations | coal | Basic tier |
| deep_coal_mine | Deep coal mine | 2×2 | coal deposit, meals | coal (more) | Upgrade from coal_mine |
| iron_mine | Iron mine | 2×2 | iron deposit, rations | iron_ore | Basic tier |
| deep_iron_mine | Deep iron mine | 2×2 | iron deposit, meals | iron_ore (more) | Upgrade from iron_mine |
| smelter | Smelter | 2×2 | iron_ore, coal | iron_ingots | |
| workshop | Workshop | 2×2 | iron_ingots, planks | tools | ECON-SMOKE-005 target |
| storage_yard | Storage yard | 2×2 | — | stores wares | Optional overflow storage |

Construction cost for generic building: planks + stone + tools + builder_work (amounts in config).

---

## Worker Types (config-driven)

| Worker type | Typical building | Tool required (Run 02) |
|-------------|------------------|------------------------|
| woodcutter | woodcutter | tools |
| forester | forester | tools |
| carpenter | sawmill | tools |
| miner | coal_mine, iron_mine, deep_* | tools |
| farmer | farm | tools |
| carrier_operator | (implicit on carriers) | — |
| builder | production buildings (e.g. brick kiln) | tools |

> **Construction sites (v1):** Do not assign **builder** workers to sites. Sites use material delivery + abstract **builder_work** ware (DDR-LOG-005).

Workers are provisioned from Headquarters or Storehouses (see ECON-WORKER-*).

---

## Military Buildings (Trial / Hybrid)

> Trial mode uses **credits** as a simplified cost. The **Hybrid Game has no credits** (DDR-031): the "Hybrid construction wares" column is the only cost path in hybrid.

| ID | Name | Footprint | Trial cost | Hybrid construction wares |
|----|------|-----------|------------|---------------------------|
| command_hub | Command hub | 3×3 | credits | planks, stone, tools, builder_work |
| power_generator | Power generator | 2×2 | credits | planks, stone, tools |
| resource_processor | Resource processor | 2×2 | credits | planks, iron_ingots |
| barracks | Barracks | 2×2 | credits | planks, stone, tools |
| vehicle_plant | Vehicle plant | 3×2 | credits | planks, iron_ingots, tools |
| radar_tower | Radar tower | 1×1 | credits | planks, tools |
| research_lab | Research lab | 2×2 | credits | planks, stone, tools |
| repair_depot | Repair depot | 2×2 | credits | planks, iron_ingots |
| defence_turret | Defence turret | 1×1 | credits | stone, iron_ingots |

---

## Deposits and Forest

| ID | Terrain association | Behaviour |
|----|---------------------|-----------|
| forest | forest hex | tree_density; woodcutters reduce; foresters increase |
| rock | rock hex | finite extraction |
| coal | coal overlay | finite; depletes |
| iron | iron overlay | finite; depletes |
| fertile | fertile hex | permanent |
| water_source | water hex | permanent |

---

## Military Resource Fields (Trial)

| ID | Name | Behaviour |
|----|------|-----------|
| mineral_field | Mineral field | Abstract harvestable patch; depletes; collector gathers |
| rich_mineral_field | Rich mineral field | Higher yield; optional scenario variant |

Trial mode: fields feed resource processor → credits. Hybrid mode: economy deposits may substitute or supplement per scenario config.

---

## Military Units (expanded)

| ID | Name | Category | Produced at | Trial cost |
|----|------|----------|-------------|------------|
| rifle_squad | Rifle squad | infantry | barracks | credits |
| rocket_squad | Rocket squad | infantry | barracks | credits |
| scout | Scout | infantry | barracks | credits |
| engineer | Engineer | infantry | barracks | credits |
| collector | Collector | vehicle | vehicle plant | credits |
| scout_vehicle | Scout vehicle | vehicle | vehicle plant | credits |
| light_vehicle | Light vehicle | vehicle | vehicle plant | credits |
| tank | Tank | vehicle | vehicle plant | credits |
| artillery | Artillery | vehicle | vehicle plant | credits |

Hybrid production recipes (example placeholders) — **ware-only, no credits** (DDR-031):

| Unit | Wares required |
|------|----------------|
| rifle_squad | tools, bread |
| rocket_squad | tools, bread, iron_ingots |
| scout | tools, bread |
| engineer | tools, iron_ingots |
| collector | iron_ingots, tools, planks |
| scout_vehicle | iron_ingots, tools, planks |
| light_vehicle | iron_ingots, tools, planks |
| tank | iron_ingots, tools, planks |
| artillery | iron_ingots, tools, planks |

---

## IP Note

All names are generic placeholders. Replace during content pass (RUN_11) without breaking IDs.
