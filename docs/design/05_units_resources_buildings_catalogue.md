# Units, Resources, Buildings Catalogue

Original placeholder names. Names may change; do not use protected reference-game names.

---

## Wares (Economy)

| Ware ID | Display name | Source |
|---------|--------------|--------|
| timber | Timber | Woodcutter |
| planks | Planks | Sawmill |
| stone | Stone | Quarry |
| grain | Grain | Farm |
| water | Water | Well |
| bread | Bread | Bakery |
| coal | Coal | Coal mine |
| iron_ore | Iron ore | Iron mine |
| iron_ingots | Iron ingots | Smelter |
| tools | Tools | Workshop |
| builder_work | Builder work | Abstract labour unit |

---

## Economy Buildings

| ID | Name | Footprint | Inputs | Outputs |
|----|------|-----------|--------|---------|
| woodcutter | Woodcutter | 1×1 | forest deposit | timber |
| sawmill | Sawmill | 2×1 | timber | planks |
| quarry | Quarry | 2×2 | rock deposit | stone |
| farm | Farm | 2×2 | fertile hex | grain |
| well | Well | 1×1 | water hex | water |
| bakery | Bakery | 2×1 | grain, water | bread |
| coal_mine | Coal mine | 2×2 | coal deposit | coal |
| iron_mine | Iron mine | 2×2 | iron deposit | iron ore |
| smelter | Smelter | 2×2 | iron_ore, coal | iron_ingots |
| workshop | Workshop | 2×2 | iron_ingots, planks | tools |
| storage_yard | Storage yard | 2×2 | — | stores wares |

Construction cost for generic building: planks + stone + tools + builder_work (amounts in config).

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

Balance values live in config YAML; tables here define IDs and roles only.

---

## Deposits

| ID | Terrain association | Renewal |
|----|---------------------|---------|
| forest | forest hex | slow regrowth |
| rock | rock hex | finite |
| coal | coal overlay | finite |
| iron | iron overlay | finite |
| fertile | fertile hex | permanent |
| water_source | water hex | permanent |

---

## IP Note

All names are generic placeholders. Replace during content pass (RUN_11) without breaking IDs.
