# Catalogue manifest: God of the Island core design workbook

**Owner of:** the row-by-row mapping of `God_of_the_Island_Core_Design.xlsx` to stable catalogue IDs, the unification stage of every row, the list of rows that must be corrected or superseded, and the gaps against the design. **Precedence:** [LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md) and decision D9 ([OWNER_DECISION_REGISTER](../civdiv/OWNER_DECISION_REGISTER.md)) win over the workbook and over D8 wherever they conflict; [UNIFICATION_DESIGN.md](UNIFICATION_DESIGN.md) §0.2 is the working design. Numeric values in the workbook (costs, quantities, capacities, recipes) are **configurable provisional defaults**, not approved facts (brief Part C). This manifest is documentation only: the workbook is unchanged and no content has been generated from it yet. UN0 and UN7 own the schema and export (design §0.2.1); they must treat this file as the reviewed mapping and record any difference.

## 1. Workbook identity

| Item | Value |
|---|---|
| File | `docs/unification/God_of_the_Island_Core_Design.xlsx` |
| SHA-256 | `f764df01de6b98aa5cc61e612b300446cf87366bda971804428544d9455405a0` |
| Tabs | `NPC Actions`, `Objects`, `Buildings` |
| `NPC Actions` | 12 controls, 12 needs (as expected) |
| `Objects` | **138** object rows (sheet rows 5-142; some cells hold several recipe lines) |
| `Buildings` | **65** building/infrastructure rows (sheet rows 5-69; includes 6 collection shelters, upgrade rows, `Settlement` and `Road`) |

Stable IDs: the workbook has no id column, so IDs are `OBJ-nnn` and `BLD-nnn` in sheet order (nnn = position in the tab, starting at 001). **IDs are immutable once exported**; if the workbook is re-sorted, rows keep their IDs through the name alias, and renamed rows keep their ID with an alias. Name is the join key to the workbook; the "WB row" column gives the sheet row at this hash.

Stage counts: Objects UA-initial **29**, UB **54**, Later **55**. Buildings UA-initial **10**, UB **13**, Later **42**.

## 2. The twelve controls and twelve needs (as in the workbook)

Header text: "The same 12 controls are used by the NPC brain and a possessing player. The world resolves effects; brains learn when, why and how to combine controls." Footer: "turn -> step -> USE empty hand on forage location -> USE mushroom on self. Fleeing, foraging, trade and work are learned behaviour, not extra controls." The workbook carries no numeric parameters for controls or needs; rates, durations and increments are to be defined in the tuning block (design §0.2.2, §0.2.4).

| # | Control | Target | Physical effect | Requirements (workbook) |
|---|---|---|---|---|
| 1 | Turn left | Facing | Rotate one permitted increment | None |
| 2 | Turn right | Facing | Rotate one permitted increment | None |
| 3 | Step forward | Adjacent location | Attempt local movement | Passable node |
| 4 | Step back | Adjacent location | Attempt local movement | Passable node |
| 5 | Step left | Adjacent location | Attempt local movement | Passable node |
| 6 | Step right | Adjacent location | Attempt local movement | Passable node |
| 7 | Take | Physical item / accessible store | Transfer into carry | Reach, room, physical access |
| 8 | Drop | Carried item / ground | Place existing item | Reach, valid location |
| 9 | Use | Object or empty hand + target | Resolve data-defined interaction | Reach, **rights**, inputs, physical validity (see correction N1) |
| 10 | Gesture | Gesture token + target | Observable gesture; meaning learned | Perception and reach |
| 11 | Sleep | Self / location | Enter sleep state | Suitable biological state |
| 12 | Wait | Self | Advance time without action | None |

| # | Need | Code status (workbook) | What changes it | How it can be relieved |
|---|---|---|---|---|
| 1 | Hunger | Implemented | Rises with time and exertion | Edible goods on self |
| 2 | Energy (inverse) | Implemented (inverse) | Rises while awake / working | Sleep or rest at appropriate location |
| 3 | Social | Partial: traits, relationships and social acts | Time without satisfying interaction; individual preference | Gestures, mutually accepted play, conversation and care |
| 4 | Fun | Missing as explicit drive | Monotony and time; varies by individual | Use toys, games, art, music, play |
| 5 | Comfort | Partial: cold, fatigue, shelter | Posture, exertion, poor furnishings | Use stool, bed, bench, mat |
| 6 | Hygiene | Missing as explicit drive | Work, waste exposure and accidents | Use water / cleaning cloth on self, wash basin |
| 7 | Bladder | Missing as explicit drive | Time and fluid intake | Use latrine or suitable outdoor location |
| 8 | Room | Missing as explicit drive | Local crowding, waste, noise and aesthetics | Clean surroundings, arrange furnishings, move |
| 9 | Warmth | Implemented | Weather and exposure | Hearth, clothing, shelter |
| 10 | Safety | Implemented | Perceived threat | Move away or obtain protection |
| 11 | Freedom from pain | Implemented | Injury and harmful events | Rest, treatment, appropriate objects |
| 12 | Thirst | Not present in core drive table | Time and exertion | Drink potable water or suitable drink |

## 3. Mapping tables

Columns: **ID**; **WB row** (sheet row); name; category/type; inputs or source -> output quantity (alternatives are separate recipe lines, shown joined by " / "); use form (abbreviated: `input` = USE on recipe input, site or storage; `tool` = USE tool on compatible target; `eat/offer` = USE on self / offer to NPC; `equip/strike` = USE on self to equip, use against valid target; `install+use` = USE to install, USE hand on installed object; `fixture` = USE hand on installed fixture; `handheld` = USE on self or compatible target) with effect text and need served in brackets; **Stage**; **Fix** = correction codes from section 5.

**Stage key.** `UA-initial`: needed by the primitive society and closed under inputs (every input is a UA item, one of the seven forages, or one of the six dice raws; see 3.3). `UB`: recipes, roads, settlements, groups, and items whose inputs lie beyond UA. `Later`: metals, mines, industry, institutions, military, vehicles, law, decoration.

### 3.1 Objects (138)

| ID | WB row | Name | Type | Source / inputs -> output | Use | Effect [need] | Stage | Fix |
|---|---|---|---|---|---|---|---|---|
| OBJ-001 | 5 | Battle axe | Weapon | Ax Workshop: 1 iron + 1 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-002 | 6 | Heavy battle axe | Weapon | War Mill: 2 steel + 1 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-003 | 7 | Beer | Drink | Micro Brewery: 2 wheat + 2 water -> 2 | USE on self / recipe input | Drink or supply a food recipe [Thirst] | Later |  |
| OBJ-004 | 8 | Blackwood | Material | Wood Hardener: 2 wood + 1 charcoal -> 1 | input |  | Later |  |
| OBJ-005 | 9 | Bread paddle | Tool | Carpenter: 1 wood -> 1 / Metal Workshop: 1 iron + 1 wood -> 1 | tool | Baking | Later | F |
| OBJ-006 | 10 | Broad axe | Weapon | Ax Workshop: 2 iron + 2 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-007 | 11 | Bronze axe | Weapon | War Mill: 2 bronze + 1 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-008 | 12 | Cloth | Material | Weaving Mill: 2 wool -> 1 / Weaving Mill: 2 reed -> 1 | input |  | UB | F |
| OBJ-009 | 13 | Coal | Fuel | Coal Mine: Finite coal deposit -> 1 / Deep Coal Mine: Finite coal deposit -> 2 / Deeper Coal Mine: Finite coal deposit -> 3 | input |  | Later | F |
| OBJ-010 | 14 | Felling axe | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Wood collection; still needs a collection shelter | Later | E |
| OBJ-011 | 15 | Fire tongs | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Heated material handling | Later | E |
| OBJ-012 | 16 | Fish | Raw | Fisher's shelter: Sea dice stock -> 1 | input | Dice stock; collection requires matching operational shelter. Cook before eating | UA-initial | E |
| OBJ-013 | 17 | Fishing rod | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Fish collection; still needs a fisher's shelter | Later | E |
| OBJ-014 | 18 | Gold | Metal | Smelting Works: 2 gold ore + 1 charcoal -> 2 | input |  | Later |  |
| OBJ-015 | 19 | Gold ore | Mineral | Gold Mine: Finite gold deposit -> 1 / Deep Gold Mine: Finite gold deposit -> 2 / Deeper Gold Mine: Finite gold deposit -> 3 | input |  | Later | F |
| OBJ-016 | 20 | Stone | Raw | Stonecutter's shelter: Mountain dice stock -> 1 / Granite Mine: Finite stone deposit -> 1 | input | Dice stock; collection requires matching operational shelter. Physical input, transfer or transformation | UA-initial | F |
| OBJ-017 | 21 | Mortar | Material | Primitive hearth: 1 stone + 1 water + 1 charcoal -> 1 / Lime Kiln: 2 stone + 1 water + 1 charcoal -> 2 / Lime Kiln: 2 stone + 1 water + 1 coal -> 2 | input |  | UB | F |
| OBJ-018 | 22 | Hammer | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Construction work | Later | E |
| OBJ-019 | 23 | Helmet | Armour | Helm Smithy: 1 iron + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-020 | 24 | Hunting spear | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Hunting / handling game | Later | E |
| OBJ-021 | 25 | Iron | Metal | Primitive forge: 1 iron ore + 1 charcoal -> 1 / Smelting Works: 2 iron ore + 1 charcoal -> 2 | input |  | Later |  |
| OBJ-022 | 26 | Iron ore | Mineral | Iron Mine: Finite iron deposit -> 1 / Deep Iron Mine: Finite iron deposit -> 2 / Deeper Iron Mine: Finite iron deposit -> 3 | input |  | Later | F |
| OBJ-023 | 27 | Kitchen tools | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Preparing food | Later | E |
| OBJ-024 | 28 | Wood | Raw | Woodcutter's shelter: Forest dice stock -> 1 | input | Dice stock; collection requires matching operational shelter. Physical input, transfer or transformation | UA-initial |  |
| OBJ-025 | 29 | Helmet mask | Armour | Helm Smithy: 2 iron + 1 leather + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-026 | 30 | Meal | Food | Big Inn: 1 bread + 1 strong beer + 1 cooked fish -> 1 / Big Inn: 1 bread + 1 strong beer + 1 meat -> 1 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | Later | C |
| OBJ-027 | 31 | Meat | Food | Primitive hearth: 1 raw meat + 1 wood -> 1 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | UB | C |
| OBJ-028 | 32 | Pick | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Mineral extraction | Later | E |
| OBJ-029 | 33 | Bread | Food | Bakery: 2 wheat + 1 water + 1 charcoal -> 2 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | Later | C |
| OBJ-030 | 34 | Ration | Food | Tavern: 1 bread -> 1 / Tavern: 1 cooked fish -> 1 / Tavern: 1 meat -> 1 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | Later | C |
| OBJ-031 | 35 | Reed | Material | Reed Yard: Wetland cultivation -> 2 | input |  | UB |  |
| OBJ-032 | 36 | Scythe | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Harvest crops | Later | E |
| OBJ-033 | 37 | Sharp axe | Weapon | Ax Workshop: 2 iron + 1 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-034 | 38 | Shovel | Tool | Metal Workshop: 1 iron + 1 wood -> 1 | tool | Prepare ground / plant crops | Later | E |
| OBJ-035 | 39 | Snack | Food | Inn: 1 bread + 1 beer + 1 cooked fish -> 1 / Inn: 1 bread + 1 beer + 1 meat -> 1 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | Later | C |
| OBJ-036 | 40 | Strong beer | Drink | Brewery: 3 wheat + 2 water -> 2 | USE on self / recipe input | Drink or supply a food recipe [Thirst] | Later |  |
| OBJ-037 | 41 | Warhelm | Armour | Helm Smithy: 2 steel + 1 gold + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-038 | 42 | Warriors axe | Weapon | War Mill: 2 steel + 1 gold + 1 wood + 1 charcoal -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-039 | 43 | Water | Drink / input | Well: Fresh water location -> 1 | USE on self (drink / wash slot) or recipe input | Relieves thirst or dirtiness; consumed or transferred as appropriate [Thirst] | UA-initial | E |
| OBJ-040 | 44 | Wheat | Crop | Farm: 1 wheat seed -> 3 | input |  | Later |  |
| OBJ-041 | 45 | Clay | Raw | Clay gatherer's shelter: Wetland dice stock -> 1 | input | Dice stock; collection requires matching operational shelter. Physical input, transfer or transformation | UA-initial |  |
| OBJ-042 | 46 | Sheep | Animal | Shepherd's shelter: Pasture dice stock -> 1 | USE hand on animal / enclosure | Dice stock; collection requires matching operational shelter. Herd, care or process through valid animal interaction; living identity | UA-initial | E |
| OBJ-043 | 47 | Hay | Raw | Hay gatherer's shelter: Fields dice stock -> 1 | input | Dice stock; collection requires matching operational shelter. Physical input, transfer or transformation | UA-initial |  |
| OBJ-044 | 48 | Mushrooms | Forage | No building: Forest infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-045 | 49 | Roots | Forage | No building: Mountain infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-046 | 50 | Wild eggs | Forage | No building: Wetland infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-047 | 51 | Rabbit | Forage | No building: Pasture infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-048 | 52 | Wild vegetables | Forage | No building: Fields infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-049 | 53 | Cactus fruit | Forage | No building: Desert infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-050 | 54 | Seaweed | Forage | No building: Sea coast infinite forage -> 1 | eat/offer | Infinite local forage; finite gathering time. Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-051 | 55 | Charcoal | Fuel | Primitive hearth: 2 wood -> 1 / Charcoal Kiln: 2 wood -> 1 | input |  | UB |  |
| OBJ-052 | 56 | Wheat seed | Seed | Seed plot: Field seed plants -> 1 / Farm: 1 wheat seed -> 1 | USE on cultivation plot | Plant and reproduce through crop growth | UB | F |
| OBJ-053 | 57 | Raw meat | Raw | Hunter's Hut: 1 rabbit -> 1 / Sheep pen: 1 sheep -> 2 | input | Cook before eating | UB | F |
| OBJ-054 | 58 | Cooked fish | Food | Primitive hearth: 1 fish + 1 wood -> 1 | eat/offer | Eat; offer to another person (may refuse) [Hunger] | UA-initial | C |
| OBJ-055 | 59 | Hide | Material | Sheep pen: 1 sheep -> 1 | input |  | UB | F |
| OBJ-056 | 60 | Wool | Material | Sheep pen: Live sheep -> 2 | input |  | UB | F |
| OBJ-057 | 61 | Copper ore | Mineral | Copper Mine: Finite copper deposit -> 1 | input |  | Later |  |
| OBJ-058 | 62 | Copper | Metal | Smelting Works: 2 copper ore + 1 charcoal -> 2 | input |  | Later |  |
| OBJ-059 | 63 | Tin ore | Mineral | Tin Mine: Finite tin deposit -> 1 | input |  | Later |  |
| OBJ-060 | 64 | Tin | Metal | Smelting Works: 2 tin ore + 1 charcoal -> 2 | input |  | Later |  |
| OBJ-061 | 65 | Bronze | Metal | Smelting Works: 2 copper + 1 tin + 1 charcoal -> 1 | input |  | Later |  |
| OBJ-062 | 66 | Steel | Metal | Smelting Works: 2 iron + 1 charcoal -> 1 | input |  | Later |  |
| OBJ-063 | 67 | Planks | Material | Carpenter: 1 wood -> 2 | input |  | UA-initial |  |
| OBJ-064 | 68 | Pottery | Material | Pottery: 2 clay + 1 wood -> 1 | input |  | UB |  |
| OBJ-065 | 69 | Brick | Material | Pottery: 2 clay + 1 charcoal -> 2 | input |  | UB |  |
| OBJ-066 | 70 | Building block | Material | Stone workshop: 2 stone -> 1 | input |  | UB |  |
| OBJ-067 | 71 | Leather | Material | Tannery: 1 hide + 1 water -> 1 | input |  | UB |  |
| OBJ-068 | 72 | Stone hammer | Tool | Carpenter: 1 wood + 1 stone -> 1 | tool | Early construction work | UA-initial | E |
| OBJ-069 | 73 | Stone axe | Tool | Carpenter: 1 wood + 1 stone -> 1 | tool | Early wood collection; still needs a collection shelter | UA-initial | E |
| OBJ-070 | 74 | Stone pick | Tool | Carpenter: 1 wood + 1 stone -> 1 | tool | Early mineral extraction | UB | E |
| OBJ-071 | 75 | Wood tongs | Tool | Carpenter: 1 wood -> 1 | tool | Early heated material handling | UB | E |
| OBJ-072 | 76 | Cattle | Animal | Cattle Farm: Existing suitable adult animals -> 1 / Cattle Farm: Wild cattle population -> 1 | USE hand on animal / enclosure | Herd, care or process through valid animal interaction; living identity | Later | F |
| OBJ-073 | 77 | Ferry | Vehicle | Ferry Yard: 6 wood + 3 cloth -> 1 | USE hand on vehicle | Operate / transport people or goods; needs operator | Later | I |
| OBJ-074 | 78 | Ship | Vehicle | Shipyard: 10 blackwood + 4 cloth + 2 wood -> 1 | USE hand on vehicle | Operate / transport people or goods; needs operator | Later | I |
| OBJ-075 | 79 | Herbs | Medical supply | No building: Root habitat -> 1 | USE on injured NPC / self | Treatment subject to knowledge and consent [Pain / Health] | UA-initial | D |
| OBJ-076 | 80 | Sword | Equipment | Metal Workshop: 2 iron + 1 wood -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-077 | 81 | Spear | Equipment | Metal Workshop: 1 wood + 1 iron -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-078 | 82 | Bow | Equipment | Metal Workshop: 2 wood + 1 cloth -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | UB | F |
| OBJ-079 | 83 | Sling | Equipment | Metal Workshop: 1 leather + 1 cloth -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | UB | F |
| OBJ-080 | 84 | Big shield | Equipment | Metal Workshop: 2 planks + 1 iron -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | F |
| OBJ-081 | 85 | Small shield | Equipment | Metal Workshop: 1 planks + 1 leather -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | UB | F |
| OBJ-082 | 86 | Breastplate | Equipment | Metal Workshop: 3 iron + 1 leather -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | Later | H |
| OBJ-083 | 87 | Light armour | Equipment | Metal Workshop: 2 cloth + 1 leather -> 1 | equip/strike | Protection or combat / training, future NPC military [Safety] | UB | F |
| OBJ-084 | 88 | Bedding | Material | Carpenter: 2 hay -> 1 | input |  | UA-initial |  |
| OBJ-085 | 89 | Painting board / easel | Furnishing | Carpenter: 2 wood + 1 cloth -> 1 | install+use | Supports art creation using pigment [Fun] | UB | G |
| OBJ-086 | 90 | Fish basin | Furnishing | Pottery: 3 clay + 1 charcoal -> 1 | install+use | Display living fish; requires water and animal care [Fun; Room] | Later |  |
| OBJ-087 | 91 | Living display fish | Living animal | Fisher's shelter (capture route TBD) [route TBD] | USE / place in fish basin | Keep a separately tracked live fish; not an edible fish lot at same time [Fun] | Later | F |
| OBJ-088 | 92 | Drink counter | Furnishing | Carpenter: 3 planks -> 1 | install+use | Supports serving stored drinks [Social; Thirst] | UB | E |
| OBJ-089 | 93 | Cooking grate | Furnishing | Primitive forge: 2 iron -> 1 | install+use | Supports food cooking over hearth [Hunger] | Later |  |
| OBJ-090 | 94 | Hide ball | Portable good | Tannery: 1 leather + 1 hay -> 1 | handheld | Manipulable ball for play [Fun] | UB |  |
| OBJ-091 | 95 | Hoop / ball target | Furnishing | Carpenter: 2 wood + 1 reed -> 1 | install+use | Target for ball play [Fun; Social] | UB | G |
| OBJ-092 | 96 | Bed | Furnishing | Carpenter: 3 wood + 1 bedding -> 1 | install+use | One adult sleeping place; reusable [Energy; Comfort] | UA-initial | A |
| OBJ-093 | 97 | Sleeping mat | Furnishing | Carpenter: 2 hay + 1 reed -> 1 | install+use | Low-cost sleeping place; reusable [Energy; Comfort] | UB | AG |
| OBJ-094 | 98 | Tally token | Portable record | Carpenter: 1 wood -> 1 | USE / display to NPC or notice board | Records quantity or obligation; no currency creation | UB | C |
| OBJ-095 | 99 | Record rack | Furnishing | Carpenter: 2 planks -> 1 | install+use | Stores knowledge artefacts [Room; Fun] | UB | E |
| OBJ-096 | 100 | Rattle alarm | Furnishing | Carpenter: 1 wood + 1 reed + 1 pottery -> 1 | install+use | Physical local warning when disturbed [Safety] | UB |  |
| OBJ-097 | 101 | Stool | Furnishing | Carpenter: 2 wood -> 1 | install+use | One seat [Comfort] | UA-initial |  |
| OBJ-098 | 102 | Strategy board | Portable good | Carpenter: 1 wood + 1 stone -> 1 | handheld | Solo or consensual shared play [Fun; Social] | UB | D |
| OBJ-099 | 103 | Sun marker | Fixture | Stone workshop: 2 stone -> 1 | fixture | Local daylight time cue | Later | G |
| OBJ-100 | 104 | Infusion pot | Furnishing | Pottery: 2 clay + 1 charcoal -> 1 | install+use | Supports preparing herb drink [Thirst; Comfort] | UB |  |
| OBJ-101 | 105 | Herbal infusion | Consumable | Primitive hearth: 1 water + 1 herbs + 1 charcoal -> 1 | USE on self | Drinkable; no automatic medical claim [Thirst] | UB |  |
| OBJ-102 | 106 | Work surface | Furnishing | Carpenter: 2 planks -> 1 | install+use | Work and display surface [Room] | UB |  |
| OBJ-103 | 107 | Cradle | Furnishing | Carpenter: 2 wood + 1 bedding -> 1 | install+use | Safe resting slot for infant; caregiver access [Energy; Comfort] | UA-initial | A |
| OBJ-104 | 108 | Wash basin | Furnishing | Pottery: 2 clay + 1 charcoal -> 1 | install+use | Wash self or objects with supplied water [Hygiene] | UB |  |
| OBJ-105 | 109 | Low jump platform | Fixture | Carpenter: 3 wood -> 1 | fixture | Safe water play only; no automatic swimming [Fun] | UB | G |
| OBJ-106 | 110 | Model hut | Portable good | Carpenter: 1 wood + 1 reed -> 1 | handheld | Imaginative play [Fun] | UB |  |
| OBJ-107 | 111 | Storage chest | Furnishing | Carpenter: 2 planks -> 1 | install+use | Finite physical container [Room] | UA-initial | A |
| OBJ-108 | 112 | Carved ornament | Portable good | Carpenter: 1 wood -> 1 | handheld | Display improves perceived surroundings [Room] | UA-initial |  |
| OBJ-109 | 113 | Grinding stone | Furnishing | Stone workshop: 2 stone -> 1 | install+use | Supports grinding wheat [Hunger] | UB |  |
| OBJ-110 | 114 | Flour | Processed food | Primitive hearth: 1 wheat -> 1 | USE on compatible recipe / storage target | Optional ingredient for baking; not automatic food [Hunger] | UB |  |
| OBJ-111 | 115 | Food stall | Fixture | Carpenter: 3 wood + 1 cloth -> 1 | fixture | Holds goods for voluntary exchange [Hunger; Social] | Later | C |
| OBJ-112 | 116 | Water feature | Fixture | Pottery: 3 clay + 1 charcoal -> 1 | fixture | Retains delivered water; decorative [Room; Fun] | Later | G |
| OBJ-113 | 117 | Waste basket | Furnishing | Weaving Mill: 2 reed -> 1 | install+use | Finite waste storage [Hygiene; Room] | UB | A |
| OBJ-114 | 118 | Waste | Material | Consumption / disposal | USE on compatible recipe / storage target | Material byproduct; transfer and treatment required [Hygiene; Room] | UA-initial | E |
| OBJ-115 | 119 | Memorial stone | Fixture | Stone workshop: 2 stone -> 1 | fixture | Records identity of deceased; memorial interaction [Social; Room] | UB |  |
| OBJ-116 | 120 | Potted plant | Furnishing | Pottery: 1 pottery + 1 water -> 1 | install+use | Plant care; living specimen required [Room; Fun] | Later |  |
| OBJ-117 | 121 | Climbing frame | Fixture | Carpenter: 4 wood + 2 reed -> 1 | fixture | Play; energy expenditure and safety constraints [Fun] | UB |  |
| OBJ-118 | 122 | Medicine box | Furnishing | Carpenter: 1 wood -> 1 | install+use | Finite storage for herbs and remedies [Health; Pain] | UB | A |
| OBJ-119 | 123 | Toy cart | Portable good | Carpenter: 2 wood -> 1 | handheld | Push or manipulate toy [Fun] | UB |  |
| OBJ-120 | 124 | Unusual plant | Furnishing | Pottery: 1 pottery + 1 water -> 1 | install+use | Rare plant variant; no magic effect assumed [Room; Fun] | Later |  |
| OBJ-121 | 125 | Notice board | Fixture | Carpenter: 2 planks -> 1 | fixture | Display durable symbols and records [Social] | UB | E |
| OBJ-122 | 126 | Pigment | Material | Stone workshop: 1 clay + 1 charcoal -> 1 | USE on compatible recipe / storage target | Abstract colour material for art | UB |  |
| OBJ-123 | 127 | Wall art | Portable good | Carpenter: 1 cloth + 1 pigment -> 1 | handheld | Aesthetic display [Room] | UB |  |
| OBJ-124 | 128 | Drum | Portable good | Carpenter: 1 wood + 1 leather -> 1 | handheld | Make audible rhythm; listeners may join [Fun; Social] | UB |  |
| OBJ-125 | 129 | Target game | Fixture | Stone workshop: 2 stone + 1 wood -> 1 | fixture | Simple rolling/throwing game [Fun; Social] | UB |  |
| OBJ-126 | 130 | Cool storage chest | Furnishing | Carpenter: 3 wood + 2 stone -> 1 | install+use | Slows spoilage; finite capacity [Hunger] | UB | AG |
| OBJ-127 | 131 | Bench | Furnishing | Carpenter: 3 wood -> 1 | install+use | Multiple seats [Comfort; Social] | UA-initial |  |
| OBJ-128 | 132 | Fire warning station | Fixture | Carpenter: 1 wood + 1 reed -> 1 | fixture | Staffed observation point; warning via gesture [Safety] | Later | IG |
| OBJ-129 | 133 | Bathing pool | Fixture | Stone workshop: 4 stone + 2 clay -> 1 | fixture | Water-filled shallow basin; safe access [Hygiene; Fun] | Later |  |
| OBJ-130 | 134 | Message token | Portable record | Carpenter: 1 wood -> 1 | USE / display to NPC or notice board | Carry a physical message; no telepathy [Social] | UB | E |
| OBJ-131 | 135 | Story circle | Fixture | Stone workshop: 3 stone -> 1 | fixture | Multiple seats for live story or music [Fun; Social] | UB |  |
| OBJ-132 | 136 | Toy | Portable good | Carpenter: 1 wood -> 1 | handheld | Manipulation and imaginative play [Fun] | UA-initial |  |
| OBJ-133 | 137 | Toy box | Furnishing | Carpenter: 1 planks -> 1 | install+use | Finite toy storage [Room; Fun] | UB | A |
| OBJ-134 | 138 | Waste pit | Fixture | Stone workshop: 2 stone + 1 clay -> 1 | fixture | Contains waste; disposal process recorded [Hygiene; Room] | UB |  |
| OBJ-135 | 139 | Training stone | Furnishing | Stone workshop: 2 stone -> 1 | install+use | Physical exercise; increases fatigue [Fun; Health] | UB |  |
| OBJ-136 | 140 | Latrine | Fixture | Carpenter: 2 wood + 1 clay -> 1 | fixture | Relieve bladder privately; generates waste [Bladder; Hygiene] | UA-initial | A |
| OBJ-137 | 141 | Water jug | Portable good | Pottery: 1 clay + 1 charcoal -> 1 | USE on water source / self / compatible target | Carry physical water; finite capacity [Thirst; Hygiene] | UB |  |
| OBJ-138 | 142 | Cleaning cloth | Portable good | Weaving Mill: 1 cloth -> 1 | USE on water source / self / compatible target | Reusable cleaning implement; needs washing [Hygiene; Room] | UB |  |

### 3.2 Buildings (65)

Columns: ID; WB row; name; build/upgrade cost; upgrade from; produces / enables; furnishings; stage; fix. The function/rules text is omitted where it repeats "produces"; where it differs it is summarised in section 5.

| ID | WB row | Building | Cost | Upgrade from | Produces / enables | Furnishings | Stage | Fix |
|---|---|---|---|---|---|---|---|---|
| BLD-001 | 5 | Communal hall | Founder-provided; later construction TBD | - | Founder building; sufficient shelter and shared storage | Mats / beds for 12; benches; chest; hearth | UA-initial | AF |
| BLD-002 | 6 | Fisher's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 fish | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-003 | 7 | Gamekeeper's Hut | 1 stone + 4 wood | - | Manage animal habitats; no increase/depletion of infinite rabbit forage | As required / space allows | Later | E |
| BLD-004 | 8 | Hunter's Hut | 1 stone + 4 wood | - | 1 raw meat | As required / space allows | UB |  |
| BLD-005 | 9 | Woodcutter's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 wood | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-006 | 10 | Stonecutter's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 stone | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-007 | 11 | Ranger's Hut | 4 wood | - | Maintain visible woodland; no extra dice allowance | As required / space allows | Later | E |
| BLD-008 | 12 | Scout's Hut | 1 stone + 2 wood | - | Shelter and route knowledge; rations are provisions not construction materials | As required / space allows | Later | E |
| BLD-009 | 13 | Sentry | 2 blackwood | - | Shelter and observation; no automatic territorial claim | As required / space allows | Later | EH |
| BLD-010 | 14 | Well | 4 wood | - | 1 water | As required / space allows | UA-initial | E |
| BLD-011 | 15 | Bakery | 2 blackwood + 2 stone + 2 wood + 2 reed | - | 2 bread | Bread paddle; work surface; hearth / oven | Later |  |
| BLD-012 | 16 | Barracks | 2 stone + 4 mortar + 4 wood + 1 reed | - | Equip recruit | As required / space allows | Later | BH |
| BLD-013 | 17 | Barrier | 5 blackwood + 2 mortar | - | Defensive asset | As required / space allows | Later | H |
| BLD-014 | 18 | Charcoal Kiln | 2 mortar + 3 wood + 2 reed | - | 1 charcoal | As required / space allows | UB |  |
| BLD-015 | 19 | Ferry Yard | 2 blackwood + 2 cloth + 3 stone + 3 wood | - | 1 ferry | As required / space allows | Later |  |
| BLD-016 | 20 | Lime Kiln | 1 blackwood + 2 stone + 4 wood | - | 2 mortar | As required / space allows | UB |  |
| BLD-017 | 21 | Metal Workshop | 1 blackwood + 2 stone + 1 mortar + 1 wood + 1 reed | - | 1 bread paddle; 1 felling axe; 1 fire tongs; 1 fishing rod; 1 hammer; 1 hunting spear; 1 kitchen tools; 1 pick; 1 scythe; 1 shovel; 1 sword; 1 spear; 1 bow; 1 sling; 1 big shield; 1 small shield; 1 breastplate; 1 light armour | Work surface; tools; fuel storage | Later | FG |
| BLD-018 | 22 | Micro Brewery | 2 blackwood + 3 stone + 3 wood + 2 reed | - | 2 beer | Infusion pot / brewing vessels; counter | Later |  |
| BLD-019 | 23 | Reed Yard | 2 stone + 5 wood | - | 2 reed | As required / space allows | UB |  |
| BLD-020 | 24 | Shipyard | 2 blackwood + 2 cloth + 3 stone + 3 wood | - | 1 ship | As required / space allows | Later |  |
| BLD-021 | 25 | Smelting Works | 1 blackwood + 1 stone + 1 mortar + 3 wood + 2 reed | - | 2 iron; 2 gold; 2 copper; 1 bronze; 1 steel; 2 tin | As required / space allows | Later |  |
| BLD-022 | 26 | Tavern | 2 blackwood + 1 stone + 3 wood + 1 reed | - | 1 ration | Counter; work surface; benches; hearth | Later | BF |
| BLD-023 | 27 | Tower | 7 blackwood + 4 stone + 1 wood | - | Observation and defence | As required / space allows | Later | H |
| BLD-024 | 28 | Warehouse | 2 blackwood + 2 stone + 3 mortar + 3 wood + 1 reed | - | Shelter and storage | Chests; record racks; waste baskets | Later | A |
| BLD-025 | 29 | Wood Hardener | 1 stone + 3 wood | - | 1 blackwood | As required / space allows | UB |  |
| BLD-026 | 30 | Ax Workshop | TBD | Metal Workshop | 1 battle axe; 1 sharp axe; 1 broad axe | As required / space allows | Later | F |
| BLD-027 | 31 | Big Inn | TBD | Inn | 1 meal | Beds; benches; counter; hearth; wash basin; latrine | Later | BF |
| BLD-028 | 32 | Brewery | TBD | Micro Brewery | 2 strong beer | Brewing vessels; counter | Later | F |
| BLD-029 | 33 | Inn | TBD | Tavern | 1 snack | Beds; benches; counter; hearth; wash basin; latrine | Later | BF |
| BLD-030 | 34 | War Mill | TBD | Ax Workshop | 1 bronze axe; 1 heavy battle axe; 1 warriors axe | As required / space allows | Later | F |
| BLD-031 | 35 | Battle Arena | 4 gold + 4 stone + 6 mortar + 6 wood + 3 reed | - | Train evasion | Training stones; seating | Later | EH |
| BLD-032 | 36 | Cattle Farm | 1 blackwood + 2 stone + 2 wood | - | 1 cattle; Train ox | Animal enclosure; water basin | Later | E |
| BLD-033 | 37 | Farm | 1 blackwood + 3 stone + 4 wood | - | 3 wheat + 1 wheat seed | As required / space allows | Later | G |
| BLD-034 | 38 | Fortress | 9 blackwood + 4 stone + 2 mortar + 5 wood | - | Defence | As required / space allows | Later | H |
| BLD-035 | 39 | Helm Smithy | 1 blackwood + 2 stone + 2 mortar + 3 wood + 3 reed | - | 1 helmet; 1 helmet mask; 1 warhelm | As required / space allows | Later |  |
| BLD-036 | 40 | Market | 3 blackwood + 5 stone + 3 mortar + 3 wood + 4 reed | - | Physical meeting and exchange location; not automatic trade | Food stalls; notice board; work surfaces | Later | C |
| BLD-037 | 41 | Training Camp | 4 gold + 4 stone + 6 mortar + 6 wood + 3 reed | - | Train attack and defence | Training stones; gear storage | Later | EH |
| BLD-038 | 42 | Weaving Mill | 2 stone + 5 wood + 2 reed | - | 1 cloth; 1 waste basket; 1 cleaning cloth | As required / space allows | UB |  |
| BLD-039 | 43 | Citadel | TBD | Fortress | Upgrade Fortress | As required / space allows | Later | FH |
| BLD-040 | 44 | Coal Mine | 2 stone + 4 wood | - | 1 coal | As required / space allows | Later |  |
| BLD-041 | 45 | Gold Mine | 2 stone + 4 wood | - | 1 gold ore | As required / space allows | Later |  |
| BLD-042 | 46 | Granite Mine | 2 stone + 4 wood | - | 1 stone | As required / space allows | Later |  |
| BLD-043 | 47 | Iron Mine | 2 stone + 4 wood | - | 1 iron ore | As required / space allows | Later |  |
| BLD-044 | 48 | Deep Coal Mine | TBD | Coal Mine | 2 coal | As required / space allows | Later | F |
| BLD-045 | 49 | Deep Gold Mine | TBD | Gold Mine | 2 gold ore | As required / space allows | Later | F |
| BLD-046 | 50 | Deep Iron Mine | TBD | Iron Mine | 2 iron ore | As required / space allows | Later | F |
| BLD-047 | 51 | Deeper Coal Mine | TBD | Deep Coal Mine | 3 coal | As required / space allows | Later | F |
| BLD-048 | 52 | Deeper Gold Mine | TBD | Deep Gold Mine | 3 gold ore | As required / space allows | Later | F |
| BLD-049 | 53 | Deeper Iron Mine | TBD | Deep Iron Mine | 3 iron ore | As required / space allows | Later | F |
| BLD-050 | 54 | Port | 3 blackwood + 2 gold + 5 stone + 2 mortar + 2 iron + 3 wood + 4 reed | - | Berthing, storage, trade and shelter | As required / space allows | Later | C |
| BLD-051 | 55 | Clay gatherer's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 clay | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-052 | 56 | Shepherd's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 sheep | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-053 | 57 | Hay gatherer's shelter | L1: labour only; L2: 10 wood; L3: 20 wood + 10 stone (draft) | - | 1 hay | Sleeping mat / bed; store; stool | UA-initial | FG |
| BLD-054 | 58 | Primitive hearth | 1 stone | - | 1 meat; 1 cooked fish; 1 charcoal; 1 mortar; 1 herbal infusion; 1 flour | Cooking grate; infusion pot; grinding stone | UA-initial | G |
| BLD-055 | 59 | Carpenter | 3 wood + 1 stone | - | 2 planks; 1 bread paddle; 1 stone hammer; 1 stone axe; 1 stone pick; 1 wood tongs; 1 bedding; 1 painting board; 1 drink counter; 1 target ball; 1 bed; 1 bedding mat; 1 tally token; 1 record rack; 1 rattle alarm; 1 stool; 1 strategy board; 1 work surface; 1 cradle; 1 jump platform; 1 model hut; 1 storage chest; 1 carved ornament; 1 food stall; 1 climbing frame; 1 medicine box; 1 toy cart; 1 notice board; 1 wall art; 1 drum; 1 cool storage; 1 bench; 1 fire watch; 1 message token; 1 toy; 1 toy box; 1 latrine | Work surface; tool storage | UA-initial | G |
| BLD-056 | 60 | Pottery | 3 wood + 2 clay | - | 1 pottery; 2 brick; 1 fish basin; 1 infusion pot; 1 wash basin; 1 water basin; 1 potted plant; 1 unusual plant; 1 water jug | Work surface; storage | UB | G |
| BLD-057 | 61 | Stone workshop | 2 wood + 2 stone | - | 1 building block; 1 sundial; 1 grinding stone; 1 memorial stone; 1 pigment; 1 target game; 1 bathing pool; 1 story circle; 1 waste pit; 1 training stone | Work surface; tool storage | UB | G |
| BLD-058 | 62 | Sheep pen | 2 wood | - | 2 wool; 2 raw meat + 1 hide | Animal enclosure; water basin | UB | FG |
| BLD-059 | 63 | Tannery | 3 wood + 2 stone | - | 1 leather; 1 hide ball | As required / space allows | UB |  |
| BLD-060 | 64 | Copper Mine | 4 wood + 2 stone | - | 1 copper ore | As required / space allows | Later |  |
| BLD-061 | 65 | Primitive forge | 3 stone + 2 clay | - | 1 iron; 1 cooking grate | As required / space allows | Later | G |
| BLD-062 | 66 | Settlement | TBD | - | Catan vertex and development area | As required / space allows | UB | AF |
| BLD-063 | 67 | Road | TBD | - | Between settlement sites; improves walking | As required / space allows | UB | F |
| BLD-064 | 68 | Seed plot | 2 wood | - | 1 wheat seed | As required / space allows | UB | E |
| BLD-065 | 69 | Tin Mine | 4 wood + 2 stone | - | 1 tin ore | As required / space allows | Later |  |

### 3.3 The UA-initial subset (39 rows) and its closure

Raws and forage (no inputs): Wood, Stone, Clay, Hay, Sheep, Fish (from the six collection shelters, labour-only L1, L2 10 wood, L3 20 wood + 10 stone); the seven forages (Mushrooms, Roots, Wild eggs, Rabbit, Wild vegetables, Cactus fruit, Seaweed); Water (Well, 4 wood); Herbs (environmental source, see G3).

Processed items (all inputs inside the set): Cooked fish (Primitive hearth: fish + wood); Planks (Carpenter: wood); Bedding (hay); Bed (3 wood + bedding); Stool, Bench, Toy, Carved ornament (wood); Storage chest (planks); Cradle (2 wood + bedding); Latrine (2 wood + clay) with its Waste by-product; Stone hammer and Stone axe (wood + stone). Buildings: Communal hall (founder endowment), six collection shelters, Well, Primitive hearth (1 stone), Carpenter (3 wood + 1 stone).

Every UA recipe therefore bottoms out in labour-only L1 shelters and forage; no metal, reed, charcoal, mortar, blackwood, leather or cloth is needed. Only the Primitive hearth's **cooked fish** and **wood-only** outputs are UA; its meat, charcoal, mortar, infusion and flour lines are UB. The Carpenter's other products are UB or Later. The founder hall carries an explicit endowment ledger (mats/beds for 12, benches, chest, hearth) because mats need reed and the hall's furniture otherwise has no source.

Coverage of the needs by UA relief paths: Hunger (forage, cooked fish), Thirst (Water), Energy (bed, shelter floor, hall), Comfort (stool, bench, bed), Warmth (hearth, shelter), Hygiene (Water on self), Bladder (Latrine), Room (Carved ornament, storage chest, Waste handling), Fun (Toy), Social (gestures, shared play), Pain (Herbs), Safety (movement; empty-hand or stone tool as strike-capable held object, see G4).

## 4. Stage rules in brief

UA is the smallest set that lets 72 founders live, store, sleep, wash, relieve themselves, care for the injured and build the six shelters, a well, a hearth and a carpenter with real labour, and that gives CF-01 a physical strike (empty hand or a held stone tool). UB adds recipes beyond the first tier (reed, clay products, wool and leather, meat, charcoal, mortar), settlements and roads, and the first soft armour and missile items. Later holds everything that needs blackwood, mortar-heavy masonry, metals, mines, inns, markets, ports, vehicles, barracks and fortifications (industry deferral, D9.14).

## 5. Superseded and needs-correction list

Codes used in the Fix column. Every correction follows D9 and the amendment: the world records physical possession, location, barriers, occupancy, capacity and objective history; claims and permissions are beliefs or communications; a transfer is a separate physical handover; a trade is offer + promise (GESTURE) plus independent handovers; professions, roles and membership are not engine facts.

| Code | Problem in workbook text | Correction (D9) |
|---|---|---|
| N1 | `NPC Actions` / Use requirement "Reach, **rights**, inputs, physical validity" | Remove "rights". USE is valid on reach, inputs, capacity, barriers and physical validity only. No owner/UseRight filter (EP-01). |
| A | Ownership, permission or privilege implied: "one adult sleeping place", "caregiver access", "Finite physical container", Warehouse storage, Settlement "development area", hall "founder building" | Physically accessible to any able NPC unless a physical barrier prevents it; occupancy and capacity are physical and first come first served; founders/contributors are provenance, not title; claims to a bed or store are beliefs; the hall is a shared place with learned expectations. |
| B | Membership, profession, recruitment or employment gating: Barracks "recruit and equip", Tavern/Inn/Big Inn service staff, "Train existing individuals" | No recruitment or roles as engine facts; a person using the building is just using it. Equipment is picked up and used; training is practice with physical effects (fatigue, skill); service is labour plus separate handovers. |
| C | Offer/exchange semantics in a use form: "offer to NPC (may refuse)", Tally token "records quantity or obligation", Food stall "holds goods for voluntary exchange", Market/Port "trade" | "Offer" is a GESTURE (communication); the transfer is a normal TAKE/DROP handover and the receiver may decline by behaviour. Tally token is a portable symbol, not an enforced debt; stall is a container or surface; market is a place; no atomic trade, no automatic reciprocity (EP-03). Foraged and other foods only differ by edibility, not by offer rules. |
| D | Consent or knowledge flags in a physical interaction: Herbs "subject to knowledge and consent", Strategy board "consensual" | No consent flag. A remedy is used on a reachable injured person; the person can move away or resist. Knowledge is the brain's, not an interaction gate. Shared play needs a second actor's actions, not a flag. |
| E | Ambiguous interaction/target, no slot or effect: Gamekeeper/Ranger/Scout huts ("manage habitats", "maintain woodland", "route knowledge"), Sentry, Cattle Farm "train ox", Seed plot, Well/Water "consumed or transferred as appropriate", Water vs wash slot, Waste ("consumption/disposal" no recipe), Sheep and Fish (living or raw, "cook before eating"), Record rack/Notice board/Message token/Drink counter, all tools (effect on time or gate?) | Define explicit interaction slots (design §0.2.2): drink vs wash; deposit vs install; tool effects as efficiency modifiers only (collection still needs the building). Habitat/route/woodland buildings have no engine effect until a physical effect is defined; knowledge is not a building output. Raw fish and raw meat are not edible (no effect on hunger) until cooked. |
| F | Ambiguous or non-conserving cost/recipe: `TBD` costs and upgrade-only rows; multiple alternative lines with different outputs (Mortar, Cloth, Ration, Cattle, Stone, Bread paddle, Meal, Snack); non-consuming outputs (Wool "live sheep -> 2", Sheep pen); quantities for multi-product buildings; draft L2/L3 shelter costs; Hall "later construction TBD" | Alternatives become separate recipes with their own inputs and outputs; coproducts and live animals explicit; TBD costs recorded as `TBD` status and excluded from active content until set; L2/L3 values are configurable defaults; the building's produce list is only what a recipe states. |
| G | Producer or name mismatch (cross-reference errors): Carpenter produces "bedding mat", "target ball", "jump platform", "cool storage", "fire watch", "painting board" (objects: Sleeping mat, Hoop / ball target, Low jump platform, Cool storage chest, Fire warning station, Painting board / easel); Pottery "water basin" (no object; Water feature has Pottery as source but is not listed); Stone workshop "sundial" (object Sun marker); `Seed plot`; collection shelters, Carpenter and Pottery listed as "tool storage" not furnishing; furnishings listed for L1 shelters (mat, bed, stool) though L1 is labour-only | Normalise via name aliases to the object ID; add missing lines to the building; shelter L1 has only a basic sleeping spot (design §0.2.4), furnishings are separate crafted objects. |
| H | Abstract military or territorial effect: weapons "future NPC military", Barrier/Tower/Fortress/Citadel "defensive asset", Sentry "observation and defence", Battle Arena "train evasion", Training Camp "train attack and defence" | No faction military. Weapons and armour are held or worn objects used by USE in the physical conflict slice (reach, fatigue, injury); defensive structures are physical obstacles or viewpoints; training is practice with fatigue. All are Later, with only a minimal strike by empty hand or compatible held object in UA. |
| I | Operator or staffing assumed: Ferry, Ship ("needs operator"), Fire warning station ("staffed"), Cattle Farm | Operation is an NPC choosing to act; no assigned operator or post. |

## 6. Gaps (needed by D8/D9 or the design, absent from the workbook) and contradictions

| Id | Gap or contradiction | Resolution (the amendment and D9 win; workbook numbers are configurable defaults) |
|---|---|---|
| G1 | **No sea-ring/coastal items.** Seven terrains need Seaweed (present) and Fish (present) but there is no shore-access rule, no boat, no raft; Ferry/Ship need blackwood and cloth and are Later. | Shore nodes only for Seaweed and fish shelters (design §3.5); no boats in UA/UB. |
| G2 | **Raw fish edibility.** Fish is "cook before eating"; the only cooking route is the Primitive hearth. If no hearth exists fish is inedible and the Fisher's shelter produces only a recipe input. | UA includes Primitive hearth (1 stone) and Cooked fish; Seaweed remains edible without a building. |
| G3 | **Water source.** Water comes from `Well` (4 wood) only; no natural fresh-water point, stream or shore, no carrying container before Pottery (Water jug UB). D9.12 requires accessible water without a circular tool dependency if thirst is active. Desert has no natural source. | Add natural fresh-water points (environment affordance, per hex type) as a design decision; the Well is UA as an additional source. Water is carried as a quantity by hand until Water jug exists (decide capacity). |
| G4 | **No wood- or stone-only weapon and no protective equipment in UA.** Every workbook weapon needs iron, steel, bronze, leather or cloth; Bow, Sling, Shields, Light armour are listed under Metal Workshop though they use no iron (Sling, Light armour, Small shield) and need leather or cloth. | CF-01 uses an empty-hand strike plus any compatible held object (Stone hammer, Stone axe as strike-capable data flag). Producer of soft equipment must be corrected (Carpenter/Tannery) before UB; `Spear` needs a stone-point recipe before UA if a spear is wanted. |
| G5 | **Herbs source** "No building: Root habitat" with no habitat definition. | Treat as a forage-like environmental source (suggested Mountain/Forest), infinite and time-limited; owner/design to confirm. |
| G6 | **Founder hall endowment.** Hall furnishings (mats/beds for 12, benches, chest, hearth) have no source in UA (Sleeping mat needs reed; Bed needs Carpenter). The earlier R80 bed count of 8 is superseded by 12 (D9.12). | Explicit founder endowment ledger; no injected goods elsewhere. |
| G7 | **Reed and Seeds.** Reed comes from `Reed Yard` ("wetland cultivation", 2 stone + 5 wood); wheat seed from `Seed plot`/`Farm`; Farm needs blackwood. Reed is outside the six dice raws. | Reed Yard and Seed plot are UB; Sleeping mat, Waste basket, Hoop, Model hut, Climbing frame wait for them. |
| G8 | **Waste and sanitation.** Latrine generates Waste (no source row), Waste basket needs reed, Waste pit needs Stone workshop. | UA Latrine stores finite Waste internally and the hall has a pit endowment, or Waste is dropped; decide with the sanitation design. |
| G9 | **Rabbit double role.** Rabbit is infinite forage (hunt, no tool) and also `Hunter's Hut: 1 rabbit -> 1 raw meat`. | Forage Rabbit stays UA and edible raw; Hunter's Hut is UB and consumes a carried rabbit; no depletion of forage. |
| G10 | **Mines and deposits** (coal, gold, iron, copper, tin, granite) are "finite deposits" with no placement data; Deep/Deeper upgrades TBD. | Later; reachability audit (design §0.2.1) must flag them as without bootstrap path until deposits exist. |
| G11 | **No road/settlement recipe.** Settlement and Road have cost `TBD`. D9 requires the build-site pattern with real labour and materials, roads only between existing settlements. | UB; costs defined by UN7 as data; no flags or carriers. |
| G12 | **Missing object rows referenced by buildings:** water basin, target ball, sundial, fire watch (alias-able, see G in section 5); Meal/Ration/Snack inputs depend on Brewery/Inn chains (Later). | Alias or add rows; Later. |
| G13 | **Meaning of tools.** Workbook says tools "still need a collection shelter" without stating whether they gate, speed or enable. L1 shelters are labour-only (D8.6). | Tools are efficiency modifiers (and strike-capable objects); no tool gates a UA collection. |
| G14 | **Contradictions with design defaults:** (a) L1 sleeping and storage values in design §5 (storage 10/30/60, sleep 2/4/6) versus workbook furnishings "mat/bed, store, stool" at all levels; (b) workbook "owner"/rights wording versus D9 (D9 wins); (c) workbook says Warehouse/Market/Port "trade"; design says exchange is separate handovers; (d) design §5.8 upgrade disruption (50 percent) is not in the workbook. | The design's capacities and D9 win; workbook furnishing lists are craft options, not automatic content; all numbers are tuning defaults. |
| G15 | **Needs not in the code table:** Social partial; Fun, Hygiene, Bladder, Room, Thirst missing in code. Relief objects exist only in Objects. | Defined in the tuning block; enabled channels must have reachable relief (NEED-1). |

## 7. Use of this manifest

Generated content must record, per row, the manifest ID, workbook SHA-256, status (`UA-initial`, `UB`, `Later`, `Superseded` for any row whose text is not yet corrected), and the correction codes that apply. CI should reject a row that appears in the content export without a manifest entry, and any UA row with an input that is not in the UA closure. Rows marked with correction codes must not be turned into active content until the correction in section 5 is applied and recorded; the workbook remains an authoring backlog, not a runtime catalogue.
