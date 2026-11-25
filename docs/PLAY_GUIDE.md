# How to play God of the Island

Run `Builds\Windows\GodOfTheIsland.exe`.

You are a god looking down on an island. Six peoples live there, each with one town hall (HQ) on the coast. You never steer a person. You place buildings, and your people build them, fetch the goods and carry them along your roads.

## Starting, saving and quitting

- **The start card** appears first. Press **Play** to play the Amber people against five computer players. Press **Watch** to watch all six play each other. You can type a seed (a number that picks the island) or roll a new one with the dice button. **Load** opens a saved game.
- **Esc** opens the menu (when you are not holding a tool). It pauses the game. It has Resume, Save, Load, New island, Watch and Quit.
- **Saving:** pick one of three slots. A loaded game starts paused; press Space to go on.
- **Quitting:** Esc, then Quit.
- From the command line, `-godseed 1234` picks an island and `-godwatch` starts a watch game.

## Controls

| Do | How |
|---|---|
| Move the view | Right-drag or middle-drag, or WASD or the arrow keys |
| Zoom | Mouse wheel |
| Pause | Space, or the Play/Pause button on the cloud |
| Speed | `+` and `-`, or the 1x / 2x / 4x buttons |
| Put a tool down | Right-click or Esc |
| Watch mode | 1 to 6 fly to a people's HQ, Tab to the latest fight or capture, speeds up to 16x |

## The six peoples and how to win

The peoples are Amber (you), Azure, Crimson, Jade, Violet and Ivory.

- **Victory points (VP):** each finished HQ is worth 1 VP. A **Citadel** is worth 2.
- The first people to **10 VP** win. The last people standing also win.
- A people with no HQ left is out.
- The list under the cloud shows everyone's VP and who is at war with whom.

## Building

1. Pick a building on the cloud strip at the top. Hover a button to see what it costs and what it makes.
2. The places where it can go glow. Click one. Hold Shift to place several.
3. **Give it a road.** Pick **Road**, click a flag (your HQ has one), then click where the road should end. Nothing gets built without a road.
4. Builders and carriers do the rest.

Where things go:
- Woodcutters need forest, clay pits need hills, quarries and mines need mountains, farms need fields.
- A new **HQ** goes on one of the glowing corners two steps from an HQ you have. It opens a new area to build in. Spread towards land you are missing.

What makes what:

| Building | Takes | Makes |
|---|---|---|
| Sawmill | logs | planks and kindling |
| Kiln | clay and kindling | bricks |
| Smelter | ore and kindling | iron |
| Barracks | grain and iron | a soldier |
| Vehicle workshop | 2 iron and a plank | a war wagon |

Click one of your buildings to set which goods it gets first (Stop, Low, Norm, High), to pause it, to keep guards at it, or to pull it down.

**Citadel:** click your HQ and press **Upgrade to Citadel**. It needs 3 planks, 4 bricks, 4 stone and 3 iron, and 30 seconds of work.

## War, peace and capture

- Everyone starts at peace. Soldiers only fight peoples they are at war with.
- Click a building of another people. At peace, the card offers **Declare war**. At war, it offers **Target** and **Offer peace**. Peace comes once both sides offer it.
- Soldiers and wagons from your barracks and workshop first fill your guard posts, then attack your targets in order. You cannot move them yourself.
- An HQ beaten down to nothing is **captured**: it changes sides with everything around it and its goods. Its VP go to the winner.

## Trade

Press **Trade** on the cloud strip. Pick a people you are not at war with, choose what you give and what you want (up to 8 goods each way), and press **Send envoy**. The goods leave your HQ at once. A herald with a pack donkey walks over the land to their nearest HQ.

- If they accept, the goods are swapped and the herald walks home with what you asked for. If they refuse, the herald brings your goods back.
- When another people's herald reaches you, a card appears. Accept or Refuse within 60 seconds, or the offer is refused.
- Soldiers at war with a herald's people shoot heralds. A killed herald loses its goods.

## How the computer players behave

- They build the same buildings you do, join them with roads, spread to new HQ corners (towards land they lack), and turn HQs into Citadels when they can.
- They keep guards at home and raise an army.
- **War:** they gang up on anyone who leads by 2 VP or more, and attack a weak neighbour when they are much stronger. They make peace once the reason is gone, or when they are losing badly.
- **Trade:** when they need goods they cannot make, for example stone with no mountain nearby, they send a herald with goods they can spare. They prefer to trade with whoever is doing worst, and never with the leader or an enemy. They send at most one new offer every 30 seconds, and do not ask again for 2 minutes after a no.
- They accept a fair offer (worth at least three quarters of what they give) when it brings something they need for goods they can spare. They refuse the leader and anyone at war with them.
- They may send you offers too.

## A good start

1. Build a woodcutter, a quarry and a sawmill near your HQ, each with a road.
2. Add a clay pit and a kiln for bricks.
3. Add a mine, a smelter, a farm and a barracks.
4. Click your HQ and set guards to 2 or 3.
5. If your start has no mountain or no forest, build a new HQ towards it early, or trade for stone or planks.

## Not in yet

- No sound and no tutorial.
- You cannot select or move units.
- Balance has been checked by computer players only, not by hand.
