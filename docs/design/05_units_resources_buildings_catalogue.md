# Units, Resources, Buildings Catalogue

Original placeholder names. Names may change; do not use protected reference-game names.

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
| builder_work | Builder work | Abstract construction labour |

### Future tool categories (schema hook, not required Run 02)

`cutting_tool`, `digging_tool`, `building_tool`, `farming_tool`, `mining_tool`, `smithing_tool`

Run 02 may map all specialist needs to `tools` until categories are added.

---

## Economy Buildings

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
| builder | construction sites | tools |

Workers are provisioned from Headquarters or Storehouses (see ECON-WORKER-*).

---

## Military Buildings (Trial / Hybrid)

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

## Military Units

| ID | Name | Category | Produced at | Trial cost |
|----|------|----------|-------------|------------|
| rifle_squad | Rifle squad | infantry | barracks | credits |
| scout_vehicle | Scout vehicle | vehicle | vehicle plant | credits |

Hybrid production recipes (example placeholders):

| Unit | Wares required |
|------|----------------|
| rifle_squad | tools, bread |
| scout_vehicle | iron_ingots, tools, planks |

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

## IP Note

All names are generic placeholders. Replace during content pass (RUN_11) without breaking IDs.
