# How to play God of the Island

Run `Builds\Windows\GodOfTheIsland.exe`.

You are a god looking down on an island. Six peoples live there, each with one town hall (HQ). You never steer a person. You place buildings, and your people build them, fetch the goods and carry them along your roads. Raw goods turn into finer goods, finer goods into weapons and armour, and a barracks turns those into soldiers who fight on their own.

## Starting, saving and quitting

- **The start card** appears first.
  - **Tutorial** starts a fixed island (seed 40) with guiding cards. New players should start here.
  - **Play** pits you (Amber) against five computer players.
  - **Watch** lets you watch all six play each other.
  - You can type a seed (a number that picks the island) or roll one with the dice button. **Load** opens a saved game.
- **Esc** opens the menu when you are not holding a tool. It pauses the game. It has Resume, Save, Load, New island, Watch, **Tutorial tips: on/off** and Quit.
- **Saving:** pick one of three slots. A loaded game starts paused; press Space to go on.
- From the command line, `-godseed 1234` picks an island and `-godwatch` starts a watch game.

## Controls

| Do | How |
|---|---|
| Move the view | Right-drag or middle-drag, or WASD or the arrow keys |
| Zoom | Mouse wheel |
| Pause | Space, or the Play/Pause button on the cloud |
| Speed | `+` and `-`, or the 1x / 2x / 4x buttons |
| Build bar tabs | Tab and Shift+Tab cycle them; 1 to 5 pick one |
| Put a tool down | Right-click or Esc |
| Watch mode | 1 to 6 fly to a people's HQ, Tab to the latest fight or capture, speeds up to 16x |

## Winning

- The peoples are Amber (you), Azure, Crimson, Jade, Violet and Ivory.
- Each finished **HQ** is worth 1 victory point (VP). A **Citadel** (an upgraded HQ) is worth 2.
- The first people to **10 VP** win. The last people standing also win. A people with no HQ left is out.
- The list under the cloud shows everyone's VP and who is at war with whom.

## The tutorial

The tutorial island gives Amber forest, hills and mountain in its first area; the fields are one HQ away. The computer players play normally, but they never declare war on you. They take any fair trade you offer them, if they have the goods.

Cards sit in the bottom-left corner and never stop the game. Each card has a goal that ticks off by itself when you do it. **Next** (or **Skip step**) moves on, and **Hide tutorial** hides them all. An arrow or glow shows where to click.

The fifteen cards:
1. Welcome and the camera.
2. Build a woodcutter.
3. Join it with a road.
4. Dice and piles.
5. A quarry and a clay pit.
6. A sawmill and a brick kiln.
7. A second HQ, toward the fields.
8. A farm, a mine and a bakery.
9. A smelter and an axesmith.
10. The barracks and your first soldier.
11. Guards.
12. Upgrading a harvester.
13. Trade.
14. War and targets.
15. The Citadel and 10 VP.

A scripted player who does what each card says reaches the last card at about minute 13 of game time, and gets its first soldier at about minute 12.

## Building

1. Pick a building on the build bar at the top. Hover a button to see what it costs and what it makes.
2. The places where it can go glow. Click one. Hold Shift to place several.
3. **Give it a road.** Right after you place a building, the road tool starts at its flag: click your HQ flag, or any of your roads or flags, to join it. Any time, press **R** or the **Road (R)** button beside the tabs, click a flag (your HQ has one), then click where the road should end. A building card with no road has **Lay a road from here**. Right-click stops. Nothing gets built or delivered without a road.
4. Builders and carriers do the rest. Each HQ has 6 carriers and 2 builders.

The build bar has five tabs:
- **Gather**: the 5 harvesters.
- **Process**: the 10 processors.
- **Arms**: the 10 armouries.
- **Army**: the barracks.
- **Land**: HQ, road, demolish and trade.

The stock bar below it shows your raw goods, processed goods and gear in three groups. Hover a swatch for its name.

Click one of your buildings to:
- set which goods it gets first (**Stop, Low, Norm, High** for each input);
- pause it;
- keep guards at it;
- upgrade it (harvesters);
- pull it down.

### Raw goods and dice

Every land hex has a hidden number. Every 6 seconds the dice roll. Each hex whose number comes up gets a **pile** of its goods (at most 3 piles wait on a hex). A harvester walks to piles on its own hex and brings them home. A 6 or an 8 comes up most often, and a 2 or a 12 least.

| Harvester | Land | Gives |
|---|---|---|
| Woodcutter | forest | log |
| Clay pit | hills | clay |
| Farm | fields | grain |
| Quarry | mountain | stone |
| Mine | mountain | ore |

The quarry and the mine share the rock piles on a mountain hex. A harvester holds at most 12 goods at its flag. All harvesters cost 2 logs and 1 stone, so any start can begin.

### Harvester upgrades

Click a finished harvester and press **Upgrade**. Its wares are carried to it like a site's, and it keeps working meanwhile. When everything is in, it takes 5 seconds of work.

| Level | Costs | Each pile gives | Gathering |
|---|---|---|---|
| 1 (built) | 2 log, 1 stone | 4 | normal |
| 2 | 2 plank, 2 brick | 5 | a third faster |
| 3 | 2 mortar, 2 iron, 2 bread | 6 | twice as fast; with no pile on its hex it still makes 1 ware every 40 s |

The building looks bigger at each level.

### Processors: one for each pair of raws

Each processor takes one of each of its two raws per cycle. All of them cost 2 planks, 1 brick and 1 stone.

| Building | Takes | Makes | Cycle |
|---|---|---|---|
| Sawmill | log + stone | **2 planks** | 4 s |
| Brick kiln | log + clay | brick | 5 s |
| Smelter | log + ore | iron | 6 s |
| Bakery | log + grain | bread | 4 s |
| Mortar mill | clay + stone | mortar | 5 s |
| Bronze foundry | clay + ore | bronze | 6 s |
| Pottery | clay + grain | pottery | 5 s |
| Steel forge | stone + ore | steel | 7 s |
| Tannery | grain + stone | leather | 5 s |
| Weaver | grain + ore | cloth | 5 s |

### Armouries: one for each piece of gear

Each armoury takes one of each of its two inputs and makes one piece of gear every 8 seconds. All of them cost 2 planks, 2 bricks and 1 mortar.

| Building | Takes | Makes | Slot |
|---|---|---|---|
| Swordsmith | steel + leather | sword | weapon |
| Spearmaker | plank + bronze | spear | weapon |
| Axesmith | iron + plank | axe | weapon |
| Bowyer | plank + cloth | bow | weapon (two hands: no shield) |
| Slingmaker | leather + pottery | sling | weapon |
| Shieldwright | plank + leather | big shield | shield |
| Helmsmith | iron + cloth | helmet | head |
| Bucklersmith | bronze + leather | small shield | shield |
| Armourer | steel + cloth | breastplate | body |
| Leatherworker | leather + cloth | light armour | body |

Every weapon needs mountain goods (stone or ore) somewhere in its chain. Bread needs forest and fields.

### The barracks

- The barracks costs 2 planks, 2 bricks and 1 mortar.
- Each soldier takes **1 bread and 1 weapon**. It also takes one shield (not with a bow), one helmet and one body item, if the barracks holds them.
- It arms the soldier with the weapon it holds most of, ties going sword, spear, axe, bow, sling. To steer the mix, set the input priorities on its card. For example, Stop on axes and High on bows makes archers.
- Each soldier takes 8 seconds. It walks out wearing its kit, and you can see the kit on the model.
- The building card shows the kit of the last soldier.

### Other costs

| What | Costs |
|---|---|
| HQ | 3 plank, 2 brick, 2 mortar, 2 stone |
| Citadel upgrade | 4 brick, 4 mortar, 3 plank, 2 steel, then 30 s of work |
| Start stock at every HQ | 8 log, 8 stone, 6 clay, 4 grain, 8 plank, 8 brick, 6 mortar |

- A new **HQ** goes on one of the glowing corners two steps from an HQ you have. It opens a new area to build in. A corner touches only three hexes, so no single area has all four lands. Spread towards the land you are missing.
- **Citadel:** click your HQ and press **Upgrade to Citadel**.

## Gear and kits

Every soldier has 100 health and walks at 3 m/s. What it fights with is its **kit**: one weapon, and optionally a shield, a helmet and a body item.

### Weapons

| Weapon | Damage | Pierce | Hit % | Ticks between attacks | Range | Vs buildings | Special |
|---|---|---|---|---|---|---|---|
| Sword | 11 | 1 | 80 | 9 (0.9 s) | 2 m | 100% | **parry**: +10% block when it carries no shield |
| Spear | 11 | 4 | 90 | 10 | 3.5 m | 75% | **first strike**: ready the moment a melee fight starts |
| Axe | 19 | 3 | 75 | 13 | 2 m | 200% | **shield breaker**: halves the target's block; a blocked axe still does 3 |
| Bow | 8 | 2 | 65 | 13 | 23 m | 25% | |
| Sling | 6 | 1 | 75 | 8 | 16 m | 25% | cheap chain (no logs) |

### Defence

| Item | Block % | Armour | Dodge % | Speed | Special |
|---|---|---|---|---|---|
| Big shield | 25 (45 against bows and slings) | 0 | 0 | -10% | |
| Small shield | 15 | 0 | +3 | 0 | |
| Helmet | 0 | 2 | 0 | 0 | stops critical hits |
| Breastplate | 0 | 4 | -5 | -15% | |
| Light armour | 0 | 1 | +5 | 0 | |
| Nothing | 0 | 0 | 5 (base) | 0 | |

### How a fight works

Soldiers fight in **duels**, in rounds of seeded rolls. On its turn (its wait is over and a foe is in reach), a soldier attacks:
1. **Hit roll**: its hit % minus the target's dodge. A bow or sling shooting at a foe within 4 m (**close quarters**) has only half its hit chance. Archers belong behind a melee line.
2. **Block roll**: the target's block %. An axe halves it, and a sword with no shield adds its parry. A blocked blow does nothing, except an axe's, which still does 3.
3. **Damage**: the weapon's damage, doubled on a 10% critical (never against a helmet), minus (armour - pierce) when that is above 0. A hit does at least 1.

Within a tick, the longer reach acts first. Ranged soldiers stop at their range, and melee soldiers close in. Everyone sees 23 m, as far as a bow shoots. HQs have 3000 health and Citadels 5000. Small buildings have 400 and medium ones 700.

### Reading fights

- A soldier's model shows its kit. A swing or a shot flashes, and a small puff says **blocked** or **miss**.
- The unit tooltip and the latest-fight toast name the kits that fought.

### What the numbers mean in practice

**One against one.** The table shows win % of the row against the column, over 400 duels that start 25 m apart.

| | Sword | Spear | Axe | Bow | Sling |
|---|---|---|---|---|---|
| Sword | 49 | 50 | 41 | 100 | 99 |
| Spear | 50 | 49 | 42 | 100 | 99 |
| Axe | 58 | 57 | 48 | 99 | 99 |
| Bow | 0 | 0 | 0 | 47 | 17 |
| Sling | 0 | 0 | 0 | 80 | 51 |

- The three melee weapons are even. The axe wins a little more often, and the spear pierces armour best.
- Alone, a bow or a sling loses to any melee soldier.
- Defence matters a lot: a full kit (shield, helmet and body item) beats a bare soldier with the same weapon 85 to 100% of the time.

**Five against five.** These run on a real island with the real rules, ranged soldiers one step behind the melee. In a straight line-up:
- The bare melee weapons are close: spear against sword 62/37, axe against sword 58/41, axe against spear 50/50.
- An all-melee five beats three melee plus two archers or slingers almost every time.
- The mixed line still costs the winner one to three soldiers.

**In real games** the picture is different. Bows and slings shoot from behind guards and busy melee, and the soldiers they shoot are often walking up to a building. Over 36 AI games, counting each death as a duel against the foe that dealt most of its damage:

| Kit | Duels won | Duels lost | Win % |
|---|---|---|---|
| Spear (bare) | 598 | 521 | 53% |
| Bow (bare) | 521 | 365 | 58% |
| Sling (bare) | 140 | 121 | 53% |
| Axe (bare) | 592 | 728 | 44% |
| Sword (bare) | 155 | 303 | 33% |

So bows and slings are worth making, as long as melee soldiers stand in front of them.

## War, peace and capture

- Everyone starts at peace. Soldiers only fight peoples they are at war with.
- Click a building of another people. At peace, the card offers **Declare war**. At war, it offers **Mark target** and **Offer peace**. Peace comes once both sides offer it.
- Soldiers from your barracks first fill your guard posts, then attack your marked targets in order. Otherwise they wait in a ring around your nearest HQ. You cannot move them yourself.
- An HQ beaten down to nothing is **captured**. It changes sides with everything in its area and its goods, at half its health, and its VP go to the winner. Other buildings burn and leave rubble for 30 seconds.

## Trade

- Press **Trade** (Land tab). Pick a people you are not at war with, choose what you give and what you want (up to 8 goods each way), and press **Send envoy**.
- The goods leave your HQ at once, and a herald walks them over the land.
- If they accept, the goods are swapped and the herald brings home what you asked for. If they refuse, the herald brings your goods back.
- When another people's herald reaches you, a card appears. Accept or Refuse within 60 seconds, or the offer is refused.
- Soldiers shoot heralds of peoples they are at war with. A killed herald's goods are lost.

## How the computer players behave

**Opening.** They build a woodcutter, a quarry and a sawmill, then a clay pit, a brick kiln and a mortar mill. They skip any processor whose raws they cannot get. If their start lacks forest, mountain or hills, they spend the start stock on a new HQ toward that land. When no corner in reach has it, they take the corner closest to it as a stepping stone. They start an army only once planks and bricks flow and they have reached the missing land, or hold three HQs.

**Gear plan.** Each people picks:
- one melee weapon and one ranged weapon, from what its land can make, with a fixed taste of its own so neighbours on similar land arm differently;
- later, a second melee weapon (from 3 HQs) and defence items, once both weapon armouries stand (a second item from 3 HQs).

They build the harvesters, processors and armouries for the plan. They steer their barracks with input priorities toward about 60% melee and 40% ranged. An armoury pauses when its gear piles up, and so does a processor with 10 of its ware in stock.

**Economy.**
- They upgrade harvesters one at a time, lowest level first, stone and log before the others.
- They spread harvesters over every hex of a short raw.
- A people boxed in with no corner left builds a steel forge early and turns its HQs into Citadels.

**Trade.**
- They ask for raws they have no land for (a people without mountain buys stone and ore all game), and for the inputs of their armouries they cannot make.
- They pay with goods they can spare, and prefer to trade with whoever is doing worst. They never trade with the leader or an enemy.
- They accept a fair offer (worth at least three quarters of what they give) if it brings something they need. They also accept one that only takes goods they have far too many of.
- They send at most one offer every 30 seconds.

**War.**
- **Opening truce:** nobody declares war in the first 15 minutes.
- After that, they gang up on anyone who leads by 2 VP or more, as long as their side can stand up to the leader's army.
- They attack a weak neighbour when they are twice as strong and have at least 15 soldiers' worth. The leader does not start such wars unless it can no longer grow in peace (no corner left and every HQ a Citadel). A people in that state also arms to its full size.
- They make peace once the reason is gone, or when they are losing badly.
- Army strength counts kits: a fully kitted soldier counts about one and a half to two times a bare one.
- They keep 1 to 5 guards on every HQ, more on borders and under attack, and call reinforcements home.
- In a tutorial game they never declare war on you.

## Balance (Q4, AI watch games, seeds 1 to 12)

Each row is one game of six AI peoples with trade, played headless.

| Seed | Winner | Minute | Highest VP per people | Soldiers per people | Sword/spear/axe/bow/sling made | Captures | Trades offered/accepted | Upgrades to L2/L3 |
|---|---|---|---|---|---|---|---|---|
| 1 | Violet | 72.6 | 7,4,8,7,10,7 | 89,0,40,119,71,48 | 20/44/115/134/54 | 20 | 285/150 | 56/12 |
| 2 | Crimson | 63.2 | 2,9,10,3,2,5 | 10,88,91,0,5,39 | 36/67/42/88/0 | 7 | 289/156 | 40/7 |
| 3 | Jade | 24.9 | 3,3,2,10,2,4 | 0,9,2,33,0,18 | 7/32/18/4/1 | 4 | 160/75 | 36/4 |
| 4 | Crimson | 45.8 | 4,2,10,4,7,7 | 19,0,70,1,48,52 | 33/42/56/57/2 | 9 | 285/157 | 47/7 |
| 5 | Jade | 31.5 | 4,6,2,10,4,3 | 20,31,0,49,19,0 | 5/16/80/10/8 | 4 | 186/107 | 41/4 |
| 6 | Azure | 31.8 | 3,10,3,3,2,4 | 0,32,15,0,0,0 | 0/26/14/0/7 | 4 | 135/62 | 30/8 |
| 7 | Jade | 39.3 | 4,3,3,11,4,6 | 21,29,4,38,7,22 | 5/35/13/41/27 | 3 | 233/126 | 47/8 |
| 8 | Amber | 41.2 | 10,3,2,3,9,3 | 40,23,0,19,56,0 | 23/25/42/27/21 | 4 | 217/113 | 41/0 |
| 9 | Amber | 37.6 | 10,3,4,2,2,6 | 43,0,0,0,0,18 | 13/32/2/5/9 | 1 | 241/116 | 23/0 |
| 10 | Violet | 30.8 | 3,2,4,4,10,3 | 7,14,21,21,42,19 | 20/16/43/27/18 | 3 | 165/72 | 43/8 |
| 11 | Jade | 24.4 | 2,3,3,10,3,4 | 0,0,0,37,0,22 | 0/13/36/1/9 | 3 | 133/82 | 32/4 |
| 12 | Azure | 56.9 | 2,10,8,5,6,8 | 0,66,71,26,35,41 | 89/19/56/68/7 | 7 | 276/157 | 52/13 |

- All 12 games end with a winner; 9 end within 30 to 70 minutes.
- Seeds 3 and 11 end at minute 24.9 and 24.4, just short of 25. Seed 1 ends at 72.6.
- 4 or more peoples reach 4 VP in 5 of the 12 games.
- Every weapon is used: swords in 10 games, spears and axes in 12, bows and slings in 11.
- Over 36 seeds, 30 games end within 30 to 70 minutes and 20 are broad.
- One kit breaks the 60% line. Among kits with 50 or more duels over 36 seeds, the bow with a helmet wins 73% (44 won, 16 lost). An archer in a helmet, standing behind the line, is rarely reached. The bare bow is next at 58%. Every other kit is between 33% and 57%.

## Not in yet

- No sound.
- You cannot select or move units.
- Balance has been checked by computer players and a scripted tutorial player, not by hand.
