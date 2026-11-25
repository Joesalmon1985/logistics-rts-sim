# God of the Island: Unity proof of concept plan (24 Nov 2026)

Branch `unity/poc` (from `main` 42cadf0). It replaces the Godot project with a Unity 6000.3.28f1 project. `main` changes only when the owner says so. Nothing is pushed without asking.

## The game in one paragraph

You are a god looking down on an island. You never control a person directly. You place buildings, and the people build them. You set which buildings get wares first. Raw materials appear on the land when the hidden Catan dice roll a tile's number. Harvesters collect them, and carriers walk them along roads. Workshops turn them into finer wares, or split them into two. Two factories turn wares into soldiers and light vehicles. These leave on their own to attack the enemy buildings you have marked, or to guard the places you have flagged.

## Owner decisions (24 Nov)

| Question | Answer |
|---|---|
| Repo | New branch `unity/poc`. Unity replaces the Godot project. Docs and history stay. |
| Enemy | A static enemy base: pre-placed buildings plus a factory that sends waves. No enemy economy. |
| Logistics | Carriers walk on roads that you lay, Settlers-style. Priorities decide who is served first. |
| Land and dice | The god-board hex island with blended tiles. The dice numbers stay hidden, and richness shows as a larger centrepiece. |
| Placement | **The HQ (warehouse) goes only on a Catan corner.** It opens a round build area whose radius is half the distance between corners, so it never overlaps a neighbouring corner's area. Inside that area, building is Settlers-style on a fine grid. |
| Units | Two types: infantry and a light vehicle. |
| Control | God orders only. You mark enemy buildings as targets and set defend flags, and units choose for themselves. No unit selection. |
| Look | The god-board look is reused: tilted god camera, blended island, buildings in the people's style, small carriers, day and night, and a stripped-down cloud HUD. |

## What the proof of concept must let you do

1. Open the build and see the island from the god view, with your HQ on one corner and the enemy base on a far corner.
2. Place a woodcutter, a quarry and a clay pit inside your build area, and roads to them. Watch builders and carriers build them.
3. See the dice produce goods on the tiles, and harvesters bring them in.
4. Build a sawmill, a kiln, a smelter, a barracks and a vehicle workshop. Set an input priority on them and see deliveries change.
5. Build a second HQ on another corner, to grow the territory.
6. Tap an enemy building and mark it as a target. Units go and attack it.
7. Put a defend flag on one of your places. Some units stay there and fight the enemy waves.
8. Win by destroying the enemy HQ, or lose when your HQ falls.

## Rules for the first version (my defaults; say if any is wrong)

### Land and dice

- The island is 19 hexes, Catan-shaped, dealt fresh each game (the existing `BoardGenerator`).
- Tile kinds:
  - forest gives logs;
  - hills give clay;
  - mountains give stone or ore;
  - fields give grain;
  - desert gives nothing.
- Each tile has a hidden number from 2 to 12.
- The dice roll every 6 seconds of game time.
  - Each tile whose number comes up puts one **pile** of its raw goods at a harvest spot on the tile, inside your build area. Harvesters on that tile collect it.
  - A tile with no harvester holds up to 3 piles. After that, its piles go to waste.
- A 7 does nothing for now.
- Richness shows only through how dense and big the dressing is.
- **Owner, 24 Nov:** the hexes are abstract. The sim stays a random set of hexes, but the island must *look* natural and uneven, with no visible hexagons, circles or rings:
  - terrain borders wander;
  - the coastline is irregular;
  - dressing is scattered naturally.
  
  This is package P4a (`unity/p4-organic-look`).

### Territory and placement

- **HQ:** goes on a corner where three land hexes meet. Catan's distance rule applies: no HQ on a corner next to another HQ.
  - A new HQ must sit within your territory's reach: a corner on the edge of an existing build area.
  - Cost: 4 planks and 4 stone, carried there.
- **Build area:** a circle around the HQ corner, with a radius of half a hex edge (the "no-build zone" between corners), cut into a fine grid of small and medium plots.
  - Plots sit on the parts of the three neighbouring hexes inside the circle. A harvester belongs to the hex its plot is on.
- **Roads:** run between road nodes (flags) on the grid, inside your build areas. Carriers walk them, so a building with no road gets nothing.

### Wares and buildings

| Building | Size | Takes | Gives | Note |
|---|---|---|---|---|
| HQ / warehouse | corner | - | - | Stores everything and holds the carriers and builders. Starts with 8 planks, 6 stone and 4 brick. |
| Woodcutter | small | dice piles on forest | log | harvester |
| Clay pit | small | dice piles on hills | clay | harvester |
| Quarry | small | dice piles on mountains | stone | harvester |
| Mine | small | dice piles on mountains | ore | harvester |
| Farm | medium | dice piles on fields | grain | harvester |
| Sawmill | medium | log | plank + kindling | **split** ware |
| Kiln | medium | clay + kindling | brick | **composite** |
| Smelter | medium | ore + kindling | iron | composite |
| Barracks | medium | grain + iron | 1 infantry | unit factory |
| Vehicle workshop | medium | iron + iron + plank | 1 light vehicle | unit factory |

- **Construction:** each building costs planks, brick or stone, carried to its site. One builder walks out from the HQ.
- **Priority:** tap any building that takes wares and set each input to Stop, Low, Normal or High.
  - Carriers serve requests by priority first, then by distance.
  - A factory can also be paused.

### Military (OpenRA-style feel, god orders only)

| Unit | Speed | Health | Damage | Good against |
|---|---|---|---|---|
| Infantry | slow | low | low, fast fire | infantry |
| Light vehicle | fast | medium | high, slow fire | buildings and vehicles |

- Units are small 3D figures with a small health bar, as in OpenRA.
- **Your orders:**
  - Tap an enemy building and choose **Target**. You can mark several. They are attacked in the order you marked them.
  - Tap your own building, or a spot of land, and choose **Defend**. Give it a number of guards, 0 to 5.
- **How units choose:** a new unit first fills any defend flag short of guards, then joins the attack on the first target.
  - With no target, units wait at the HQ.
  - Any unit fights an enemy that comes within its sight. A guard returns to its flag afterwards.
- **Damage:** a destroyed building leaves rubble. Its plot can be built on again.
- **Enemy base:**
  - an HQ, a barracks, a vehicle workshop and 3 other buildings, pre-placed on a far corner;
  - the first wave after 8 minutes, then one every 2 minutes, starting at 2 infantry and growing by 1 each wave, with a vehicle from wave 3. (The plan first said every 90 seconds. On 24 Nov the smoke run showed that a passive town fell by 6½ minutes.);
  - each wave attacks your nearest building.
- **Win and lose:** destroy the enemy HQ to win. You lose if your HQ falls while you have no other HQ.

### Out of scope for this proof of concept

Enemy AI economy, trading, ports, multiplayer, save and load, sound, tutorials, phones, neural bots, the 7 / robber, specialist workers walking (they stay abstract, DDR-LOG-004), unit selection.

## What we reuse

| From | What | How |
|---|---|---|
| WorldMVP `World.Sim/Core` | seeded `Rng`, commands, events, snapshots, the `WorldSim` tick loop | copy and trim |
| WorldMVP `World.Sim/Board` | `HexCoord`, `BoardTopology`, `BoardGenerator`, `Lattice`, the `PlotLayout` idea | copy; plots rebuilt as a fine grid inside the build circle |
| WorldMVP `World.Sim/Economy/Logistics.cs` | routing and the carrier job model | adapted from corner roads to the plot-grid road nodes |
| WorldMVP `World.Sim/War` | integer seeded damage rules | adapted to ranged OpenRA-style fire |
| WorldMVP `World.Game` | island terrain and blended tiles (`TerrainBuilder`, `HexDressing`), centrepieces by richness, `PeopleBuildings`, `CollectWalkers` (small carriers), `SoldierView` meshes, `BoardSky` and night light, the tilted god camera, `MeshBuilder`/`MaterialFactory`, the shaders, the ledger cards (shrink to fit) | copy; strip god-board-only parts |
| WorldMVP tooling | asmdefs (`Sim` pure C#, `Game` view only), Unity CLI test runs, `dotnet test` mirror of the sim, AutoPlay headless smokes, the Windows build script | copy |
| logistics-rts-sim | design rules (deterministic integer sim; bots use the same commands as people), DDR-LOG decisions (road nodes = flags, per-job carriers, abstract specialists), the units and buildings catalogue as a source of names | rules kept; GDScript code is not ported |

Not reused: anything from Sims 2, rigged characters that need local art, third-party art or audio, and the council, rhythm and cave games.

## Layers (same rule as WorldMVP)

- `Assets/God/Sim` (`God.Sim`): pure C# with no Unity calls. It holds the whole game state, ticks at 10 Hz in integers, and receives commands. It is tested in EditMode and with `dotnet test`.
- `Assets/God/Game` (`God.Game`): views, camera, HUD and input. It reads snapshots and sends commands, and **never changes sim state**.

## Packages and order

The work runs as parallel sub-agents, Sonnet where possible and Opus for the hard parts, each in its own worktree with one Unity editor per folder.

| # | Package | Contents | Depends on |
|---|---|---|---|
| P0 | Project skeleton | Remove `godot_project/` and the Godot scripts. Add the Unity project, asmdefs, copied core, board and render helpers, the test setup and the build script. Update the README. | - |
| P1 | Land, dice, territory | Island deal, hidden numbers, the dice clock, piles, HQ corners and the distance rule, build circles, plot grid, road nodes. EditMode tests. | P0 |
| P2 | Economy | Building defs, construction, harvesters, workshops (split and composite), carriers on roads, priorities and pause. Tests, including a 10-minute headless economy run. | P1 |
| P3 | Military | Units, sight and fire, targets, defend flags with guard counts, the enemy base and waves, rubble, win and lose. Tests. | P1 (P2 only for the factory inputs) |
| P4 | View | God camera, island look, buildings, roads, carriers and piles, units and fire, health bars, day and night. | P0; then it follows P1 to P3 |
| P5 | God HUD | Cloud strip with the build menu, the building card with priority and pause, Target and Defend on tap, a dice tick, and win and lose cards. | P4 |
| P6 | Proof | AutoPlay smoke (build the economy, make 3 units, kill the enemy HQ), the Windows build, a play guide, screenshots for the owner. | all |

P2 and P3 can run at the same time, and so can P4 alongside P1 to P3.

## Done means

- `dotnet test` and the EditMode and PlayMode tests pass, the headless smoke wins against the enemy base, and a Windows build runs.
- The report says plainly what was tested by hand and what was not.

---

# Stage 2: six factions, AI, trade and conquest (25 Nov 2026)

The owner's request after playing the first proof of concept:

> "expand this to a bigger island (big enough for 6 player catan). Lets get the AI working, along with trade between factions. Factions should be able to send trade envoys with offers of goods for trade, if these are accepted the actual goods should be sent between factions. Also the AI should be able to play the game between 6 factions, each starting with one HQ, they should expand to get 10 VPs eventually, with conflict and conquest and units created."

They also couldn't find how to save or quit.

## Owner decisions (25 Nov)

| Question | Answer |
|---|---|
| Victory points | An HQ is 1 VP. An HQ upgraded to a **Citadel** is 2 VP. The first faction to 10 VP wins. |
| Conquest | An HQ brought to zero is **captured**. It and the buildings in its area switch to the attacker, along with their VP. A faction with no HQ left is out. |
| Players | You and 5 AIs. **Watch** mode lets all 6 AIs play each other. |
| Trade | Envoy caravans walk. The envoy walks overland from the offering HQ to the other faction's HQ, carrying the goods on offer. If the offer is accepted, it drops them and walks home with the goods paid in return. If it is refused, it brings the goods back. Units can kill the envoys of factions they are at war with, and the goods are then lost. |

## Rules (my defaults; say if any is wrong)

### Island
- 30 land hexes, as in six-player Catan: rows of 3, 4, 5, 6, 5, 4 and 3.
  - Forest 8, Hills 6, Fields 7, Mountain 7, Desert 2.
  - The 28 number tokens are dealt so that no 6 or 8 touches another.
- The organic look is kept, and the coastline wraps the new shape.
- Each of the 6 factions starts with one HQ and its start stock. The start corners are spread round the island, as far apart as possible, all on legal corners.

### Factions and war
- There are 6 factions, each with its own colour and people's style.
- Each pair of factions is either **at peace** or **at war**. Everyone starts at peace.
- Units fight only factions they are at war with, and walk past everyone else.
- Commands:
  - `DeclareWar(a, b)` makes a pair go to war at once.
  - `OfferPeace(a, b)` makes them peaceful once both sides have offered.
- You can only target buildings of a faction you are at war with.

### VP, Citadel, capture
- A finished HQ is worth 1 VP, and a Citadel is worth 2 VP.
- **Citadel:** upgrade a finished HQ for 3 plank, 4 brick, 4 stone and 3 iron, carried to it. It takes 30 s. It has 3000 health (an HQ has 1500).
- **Capture:**
  - When an HQ or Citadel reaches 0 health from a faction's units, it passes to that faction at 50% health.
  - Everything in its build area passes with it: buildings, sites, flags and the roads wholly inside it.
  - So do the stock and idle carriers in its store.
  - The old owner's walkers inside the area are removed.
- A faction with no HQ is **eliminated**: its units disband and its roads stay as ruins. Ordinary buildings still burn to rubble.
- The game ends when a faction reaches 10 VP. With one faction left standing, it wins anyway.

### Trade
- `OfferTrade(from, to, give[], want[])` sends at most 8 wares each way.
  - The offered goods leave the offering HQ's stock at once.
  - An envoy walker carries them overland to the target's nearest HQ.
- The target answers with `AnswerTrade(accept)`. AIs answer straight away; you get 60 s, with an offer card.
  - **Accepted:** the goods go into the target's store. The wanted goods are taken from the target's stock and carried home by the envoy.
  - **Refused**, or the target lacks the wanted goods: the envoy brings the offer home.
- An envoy killed on the way loses its goods.

### AI (owner rules from the 18 Nov design, adapted)
- Each AI faction runs a bot that uses only commands, like a person.
- **Economy and expansion:** build the chain, expand to new HQ corners towards missing terrain and rich land, and upgrade to a Citadel once the economy runs.
- **Army:** build units, keep guards on its HQs, and mark the targets of factions it is at war with.
- **War:**
  - Declare war on a faction leading by 2 or more VP. If two factions are tied for the lead, the leaders fight each other.
  - Declare war on a weak neighbour when its own army is clearly stronger.
  - Make peace once the reason is gone.
- **Trade:**
  - Trade with anyone who isn't winning, preferring the worst-placed faction.
  - Trade for target builds or missing wares, or to shed surplus.
  - Accept fair offers that help.

### Menu, save, quit
- **Esc** opens a menu: Resume, Save, Load, New island, Watch AIs, Quit.
- Saving writes the whole sim state, plus the bots' state, as JSON to the player's data folder. Loading the same save gives the same state hash.
- A small start card offers **Play** (you are faction 1) or **Watch**. `-godwatch` starts watch mode.

### HUD and views
- All 6 colours and styles appear in the views.
- A small standings list shows VP per faction.
- The enemy card gains Declare war / Offer peace.
- A trade panel lets you send offers.
- An offer card lets you answer incoming envoys.
- Envoys are drawn as a walker with a pack animal.

## Packages

| # | Package | Depends on |
|---|---|---|
| P7 | Six factions: `Faction` becomes 6 ids. The human faction becomes a game setting. 30-hex island, 6 starts, war and peace, VP, Citadel, capture, elimination, 10 VP win. The static enemy base and waves are removed. Minimal view and HUD updates so it compiles and shows 6 colours. | - |
| P8 | Trade: envoys in the sim, offers and answers, killing envoys, HUD trade panel and offer card, envoy model. | P7 |
| P9 | AI: bot for the economy, expansion, Citadels, army and war; a 6-AI headless game reaching 10 VP. | P7 (trade hooks after P8) |
| P10 | Menu and save/load/quit, start card and watch mode, standings, war/peace on the enemy card, 6-faction look. | P7 |
| P11 | Integration: AI trading, balance across seeds, a watch smoke that ends with a winner on the build, play guide. | P8, P9, P10 |

# Stage 3: full economy, gear, duels, tutorial (25 Nov 2026)

The owner's request after stage 2:

> "introduce a tutorial mode which you can start as a new player, where pop ups will guide you through what to do. The player can ignore or skip past these pop ups. ... production buildings for all 5 raw resources ... processing buildings for each combination (there should be 10 different combinations of raw materials). These should be used to 1 upgrade the buildings used to collect raw materials into more efficient versions and 2 to make defensive and offensive weapons. ... 5 different attacking weapons (1. sword, 2 spear, 3 axe, 4 bow, 5 sling) and 5 different defending weapons (1. big shield, 2 helmet, 3 small shield, 4 breast plate, 5 light armour). These are each produced by a different processing building which takes processed materials, which in turn supplies a barracks which creates troops based on the combination of defensive and offensive weapons produced. Come up with a simple combat system ... Random numbers and turn based combat, with weapons having different stats like 'block chance' 'damage reduction' 'attack speed' 'attack range' ... Tweak the balance of the game, and polish the AI until we've got a fun little game ecosystem and economy please."

## Owner decisions (25 Nov, stage 3)

| Question | Answer |
|---|---|
| Old chain (kindling, sawmill/kiln/smelter recipes, vehicle workshop, war wagons) | **Replaced** by the new economy. Wagons go. HQ and Citadel costs use the new goods. |
| Combat | **Live duels in rounds.** Soldiers march and meet on the map; each fight is resolved in rounds of seeded rolls. |
| What a soldier needs | **Any gear, at least a weapon.** Defence items are optional extras. |
| Harvester upgrades | **Two levels** (level 1 is the built harvester; it can go to level 2, then level 3). |

## Rules (my defaults; say if any is wrong)

### Raws and harvesters
- Five hauled raws: log, clay, grain, stone, ore. The dice still drop piles; quarry and mine both take rock piles.
- Harvesters: woodcutter, clay pit, farm, quarry, mine. They cost raw goods only (2 log, 1 stone), so any start can begin.
- **Upgrades** (`UpgradeBuilding`), carried to the building like a site's wares; it keeps working meanwhile:
  - Level 2 costs 2 plank and 2 brick. Each pile gives 5 wares (was 4), and gathering is a third faster.
  - Level 3 costs 2 mortar, 2 iron and 2 bread. Each pile gives 6 wares, and gathering is twice as fast as level 1. It also makes 1 ware every 40 s on its own when its hex has no pile, so a level 3 harvester never stands idle.
  - The building looks bigger at each level.

### Processors: one for each pair of raws
| Building | Takes | Makes |
|---|---|---|
| Sawmill | log + stone | plank (Q4: 2 planks per cycle) |
| Brick kiln | log + clay | brick |
| Smelter | log + ore | iron |
| Bakery | log + grain | bread |
| Mortar mill | clay + stone | mortar |
| Bronze foundry | clay + ore | bronze |
| Pottery | clay + grain | pottery |
| Steel forge | stone + ore | steel |
| Tannery | grain + stone | leather |
| Weaver | grain + ore | cloth |

### Armouries: one for each piece of gear
| Building | Takes | Makes | Slot |
|---|---|---|---|
| Swordsmith | steel + leather | sword | weapon |
| Spearmaker | plank + bronze | spear | weapon |
| Axesmith | iron + plank | axe | weapon |
| Bowyer | plank + cloth | bow | weapon (two-handed: no shield) |
| Slingmaker | leather + pottery | sling | weapon |
| Shieldwright | plank + leather | big shield | shield |
| Helmsmith | iron + cloth | helmet | head |
| Bucklersmith | bronze + leather | small shield | shield |
| Armourer | steel + cloth | breastplate | body |
| Leatherworker | leather + cloth | light armour | body |

### Barracks
- Takes 1 bread and 1 weapon per soldier. It also takes, if it has them, one shield (not with a bow), one helmet and one body item.
- Weapon choice: the weapon it holds most of (ties by the order sword, spear, axe, bow, sling). The input priorities (Stop/Low/Norm/High) on each gear ware let you steer the mix, as for any input.
- The soldier walks out wearing its kit (`UnitOrder.Kit`), and the view shows it.

### Building costs
- Small (harvesters): 2 log, 1 stone.
- Processors: 2 plank, 1 brick, 1 stone.
- Armouries and the barracks: 2 plank, 2 brick, 1 mortar.
- HQ: 3 plank, 2 brick, 2 mortar, 2 stone.
- Citadel: 4 brick, 4 mortar, 3 plank, 2 steel.
- Start stock at every HQ: 8 log, 8 stone, 6 clay, 4 grain, 8 plank, 8 brick, 6 mortar (Q4; was 8/6/6/4/6/4/2). This is enough for two HQs toward a land the start lacks.

### Combat: live duels in rounds
- Every soldier has 100 health. Stats come from its kit.
- **Weapons** (Q4 final numbers; the first stage 3 draft is in git history):

| Weapon | Damage | Pierce | Hit % | Rounds between attacks | Range | Vs buildings | Special |
|---|---|---|---|---|---|---|---|
| Sword | 11 | 1 | 80 | 9 ticks | 2 m | 100% | parry: +10% block when it has no shield |
| Spear | 11 | 4 | 90 | 10 ticks | 3.5 m | 75% | first strike: it attacks first when a fight starts in melee (not on every change of foe in a crowd) |
| Axe | 19 | 3 | 75 | 13 ticks | 2 m | 200% | shield breaker: halves the target's block chance |
| Bow | 8 | 2 | 65 | 13 ticks | 23 m | 25% | |
| Sling | 6 | 1 | 75 | 8 ticks | 16 m | 25% | cheap |

- **Defence** (Q4 final numbers):

| Item | Block % | Armour (damage reduction) | Dodge % | Speed | Special |
|---|---|---|---|---|---|
| Big shield | 25 (45 vs bow and sling) | 0 | 0 | -10% | |
| Small shield | 15 | 0 | +3 | 0 | |
| Helmet | 0 | 2 | 0 | 0 | stops critical hits |
| Breastplate | 0 | 4 | -5 | -15% | |
| Light armour | 0 | 1 | +5 | 0 | |
| Nothing | 0 | 0 | 5 (base) | 0 | |

- **Close quarters (Q4):** a bow or sling shooting at a foe within 4 m has half its hit chance.
- **Sight (Q4):** everyone sees 23 m, as far as a bow shoots. Before, it was 20 m, and a bow could shoot soldiers that could not see it.
- **Buildings (Q4):** HQ 3000 health, Citadel 5000 (were 1500 and 3000).

- **An attack** (on the attacker's turn: its cooldown is over and the target is in range):
  1. Roll to hit: `hit% - target dodge%`.
  2. Roll to block: the target's block %, changed by the axe or the sword parry. A blocked blow does nothing, except an axe, which still does 3.
  3. Damage = weapon damage, doubled on a 10% critical (none against a helmet), minus `max(0, armour - pierce)`, at least 1.
- **Turn order:** within a tick, the longer reach acts first, then the lower unit id. All rolls use the sim `Rng`, so a game replays exactly.
- **Movement:** ranged soldiers stop at their range; melee soldiers close in. Base speed is 3 m/s, changed by the gear's speed.
- The visible duel: the swing or shot flash, plus a small "blocked" or "miss" puff.

### Tutorial
- The start card gets **Tutorial**, which starts a fixed island chosen for a rich start (all five raws inside or next to the first area) with `GameSetup.Tutorial = true`.
- The AIs play normally but never declare war on the human, and they decline human trade only when the trade is unfair.
- Pop-up cards guide you, one step at a time. Each has **Next** or **Skip step**, **Hide tutorial**, and a goal line that ticks off by itself when you do the thing. An arrow or glow shows where to click, when it can.
- Steps:
  1. Welcome and camera.
  2. Build a woodcutter.
  3. Make a road.
  4. Dice and piles.
  5. Quarry and clay pit.
  6. Sawmill and brick kiln.
  7. Farm, mine and bakery.
  8. Smelter and axesmith.
  9. Barracks and your first soldier.
  10. Guards.
  11. Upgrade a harvester.
  12. A second HQ.
  13. Trade.
  14. War and targets.
  15. Citadel and 10 VP.
- The menu has **Tutorial tips on/off**, to bring the cards back.

### HUD
- The build bar gets tabs: **Gather** (5), **Process** (10), **Arms** (10), **Army** (barracks), **Land** (HQ, road, demolish, trade).
- The stock bar shows raws, processed goods and gear, in groups.
- The building card shows an upgrade button with its cost (harvesters) and the kit of the last soldier (barracks).
- The unit tooltip, or the latest-fight toast, names the kits that fought.

### AI and balance targets
- The AI builds toward its gear plan: a weapon mix chosen from what its land makes, with defence items added when it can.
- It upgrades harvesters once processors run, and trades for raws it lacks.
- Balance target over 12 seeds:
  - Every game ends with a winner in 30 to 70 game minutes; none ends before 25 minutes.
  - At least 4 factions reach 4 VP.
  - Every weapon is used in some game.
  - No single kit wins over 60% of duels it fights.

### Q4 result (26 Nov)

**AI.**
- **Gear plan:** a melee and a ranged weapon by land and a per-faction taste. A second melee weapon and defence items come later. The barracks is steered 60/40 melee/ranged by input priorities.
- **Opening:** land-aware. Processors it cannot feed are skipped, and HQs (stepping stones if needed) go toward missing land first.
- **Trade:** it trades for raws and armoury inputs it has no land for, and sheds gluts.
- **War:**
  - Strength counts kits.
  - No AI declares war in the first 15 minutes.
  - A faction boxed in builds steel first.
  - A faction that cannot grow in peace arms to the cap and may start a war even as leader.
  - Coalition, peace and the tutorial truce are unchanged.
- **Tutorial trades:** the AIs take any fair trade from the tutorial player that they can pay.

**Economy.**
- A sawmill makes 2 planks per cycle.
- Start stock is larger (see above).
- Dice stay at 6 s. Harvester levels and HarvesterOutputCap are unchanged.

**Results over seeds 1 to 12** (`docs/PLAY_GUIDE.md` has the per-seed table):

| Target | Result | Met? |
|---|---|---|
| Every game ends with a winner | 12 of 12 | yes |
| Winner in 30 to 70 minutes | 9 of 12. Seeds 3 and 11 end at 24.9 and 24.4 minutes, and seed 1 at 72.6. Over 36 seeds, 30 of 36. | no |
| At least 4 factions reach 4 VP | 5 of 12 games (20 of 36). Most other games have 2 or 3. | no |
| Every weapon is used in some game | swords in 10, spears and axes in 12, bows and slings in 11 | yes |
| No kit wins over 60% of its duels | Duel = a death against the foe that dealt most of the damage. Bare kits: spear 53%, bow 58%, sling 53%, axe 44%, sword 33%. Bow with helmet: 73% over 60 duels. | all but one |

**Group check.** In straight five-a-side line-ups, an all-melee five beats three melee plus two ranged.

**Tutorial.** A scripted player follows all 15 cards on seed 40 and finishes at minute 13, with its first soldier at minute 12.3 (`TutorialEndToEndTests`).

## Packages (stage 3)

| # | Package | Depends on |
|---|---|---|
| S0 | Seam (me): `Ware` extended (10 processed, 10 gear), `Gear`/`GearSlot`/`Kit`, `UnitOrder.Kit`, new `BuildingKinds` and groups, `UpgradeBuilding` command, `GameSetup.Tutorial`. | - |
| Q1 | Economy (Opus): new defs and costs, the old chain removed, harvester levels and upgrades, barracks kit assembly, start stock, save version, the AI kept working on the new chain (simple), HUD building card (upgrade, kit), tests. | S0 |
| Q2 | Combat (Opus): kit stats, duel rounds, block/dodge/crit/armour/pierce, ranged/melee movement, vehicles removed, building damage by weapon, soldier models with kit, duel effects, tests. | S0 |
| Q3 | Tutorial and HUD (Sonnet): start card Tutorial, tutorial cards and step goals, tabbed build bar, grouped stock bar, models for the 17 new buildings and the harvester levels. | S0 |
| Q4 | AI and balance (Opus): gear plans, upgrades, trade of processed goods, war with kits, tuning across seeds to the targets, play guide. | Q1, Q2, Q3 |
