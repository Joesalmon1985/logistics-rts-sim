# God of the Island: MVP tester guide

For people testing `Builds\civdiv-mvp\GodOfTheIsland.exe`. You are a god watching a living island. You never steer anyone directly: you speak, you act, and the people decide for themselves. Written from the code and the Stage 4 development log (`docs/stage4/DEV_LOG.md`) at `unity/poc` head `f9046f6`. The older stage 3 guide (`docs/PLAY_GUIDE.md`) covers the build-and-fight game, which this mode does not use.

## 1. Starting

1. Run the exe. On the start card type a seed (any number) or press the dice for a random island.
2. Press **Living island (you as a god)**. Under it, pick the **Mythic** mode first:
   - **Normal**: powers cost Mythic, a limited stock that refills each day. Use this for the real test.
   - **Unlimited**: no cost. Use it to try every power quickly.
   - **Disabled**: no divine powers. People live on their own. Use it as a baseline for comparison.
3. 20 people per faction (six factions, about 120 people) start as an established band: adults, children and households. You are the god of the first faction.

Other buttons on the card (Play as Amber, Watch 6 AIs, Tutorial) are the older stage 3 game, not this MVP. **Load a game** opens a saved game, which starts paused.

**Command-line flags** (for example `GodOfTheIsland.exe -godonly -godseed 7`):

| Flag | Effect |
|---|---|
| `-godonly` | Skips the start card and starts the god-only Living island, Normal Mythic |
| `-godseed N` | Island seed |
| `-goddev` | Developer game: enables the F6 and F9 overlays (see section 7) |
| `-godwatch` | Old stage 3 watch game, no god |
| `-godpopulation` | People on the island, Divinity off. Not the MVP |
| `-godrun`, `-godspeed X` | Start running, at speed X |
| `-godhexlook` | Draws the Catan hexes |

## 2. Camera and selection

| Do | How |
|---|---|
| Zoom to the cursor | Mouse wheel |
| Pan | Right-drag, or W/A/S/D or arrow keys |
| Orbit | Middle-drag, or Q / E |
| Tilt | Alt + wheel, or R / T, or PgUp / PgDn |
| Whole island | Home |
| Zoom to the selected person | Z |
| Follow the selected person | F (panning detaches) |
| Select their household (home if nothing selected) | H |
| Select a person | Left click |
| Add to or toggle in the selection | Shift or Ctrl + click |
| Box select | Left-drag |
| Area brush (for area-scoped orders) | Alt + left-drag; wheel sets radius |
| Focus a person and select their household | Double-click them |
| Cycle the roster chips (your favourites and watched people) | Tab / Shift+Tab |
| Clear selection, put a power down, then open the menu | Esc |

## 3. Time

| Do | How |
|---|---|
| Pause / resume | Space |
| Speed (1x, 2x, 5x, 10x) | `+` / `-` |
| Step one tick | `.` (Shift+`.` steps ten) |
| Accelerated mode (as fast as frames allow, capped at 50x) | `]` |

Divine orders issued while paused wait as "pending" and are sent when time runs. Esc also opens the menu (Resume, Save, Load, Quit).

## 4. The divine toolbar

Click a power to arm it, then click a person (or box-select several). Esc or right-click puts it down. The Mythic meter shows balance, daily regeneration and time to full. The cost shown is an estimate; a power the sim refuses is greyed or answered with a reason.

**Speaking to people.** The person hears you only if they perceive it (it can be missed, or misread: a place or act understood as a similar one, more so at night). They weigh it with their own mind, belief in you and trust, and may refuse. Response chips report "delivered, followed, refused, not reached", and each person's Divine tab shows what they heard and did.

| Power | What it does | What the person does |
|---|---|---|
| **Suggest** | One act, expires in about a day | Weak. Usually ignored or refused unless they already lean that way. |
| **Command** | One act, expires in about two days | Stronger, still their own choice. Followed when they believe and trust you and are not in pain; **a person in pain (for example very hungry) refuses**; a distrustful person defies you. |
| **Compel** | One physical act now (gather, eat, sleep, work, take, drop, give, retreat); costs Mythic every tick while it lasts | They walk and act regardless of hunger, but **physics still applies**. If the thing is out of reach, absent or not possible, the compulsion is shown as **Blocked** and ends at expiry. It ends if they die or you run out of Mythic. |

Acts offered in the popover: Gather, Eat, Sleep, Work, Build, Cooperate, Talk, Tend, Protect, Retreat, Teach, Trade for suggest and command; some are greyed or refused as "act unavailable" in this build (the sim says so; it does not fake them). Scopes: Person, Group (expanded into one order per person, up to 32), Household, Area, Place.

**Miracles** act at once on real state. Each is a click on the target.

| Power | Effect |
|---|---|
| **Heal** | Reduces a person's injury (pain falls) |
| **Cure** | Ends an illness; they are immune for a time |
| **Bless** | Faster recovery for a while |
| **Protect** | Damage taken is cut to about 30 % of normal for a while |
| **Smite** (person) | Injury, perhaps death. Witnesses nearby become afraid |
| **Smite hut** | Destroys a building; its goods scatter or burn, sleepers are cleaned out |
| **Blight** (source) | A wild food source's stock falls and does not regrow for some days |
| **Create** | Food appears on the ground at a place (3 units). Counted in a ledger |
| **Destroy** | Destroys goods lying at a place. Open estates are refused |

Miracles change real state; the people have no idea why. Nothing tells a victim who struck them.

**Law (commandment).** Opens a template picker: choose a template, fill its slots, reveal it to a person, household or place. People learn it, pass it on (sometimes garbled), adopt or reject it by their own experience, and forget it over days. It is a nudge to their decisions, never a restriction; some templates are listed but do nothing yet because the economy they refer to is not in this build.

## 5. Possession

**Not available in the HUD in this build.** Possession (taking over a person's inputs while their body, mind and learning carry on) exists in the simulation and is exercised by headless scenarios (`d4_embodiment`) and tests only. No key or button starts or ends it. The Alerts and effects code knows possession events, but nothing in the window can create them. Do not report "cannot possess anyone" as a bug; do report it if you can find any control.

## 6. Inspecting people

Click a person with Inspect (or any time nothing is armed) to open their card. Tabs:

- **Mind**: what they want now, drives, what they are doing and why. Shows what they know and believe, and the reasons behind the current choice.
- **Household**: who they live with, stores, dependants.
- **Relations**: trust, fear, kin and friends as they believe them.
- **Divine**: what you told them, what they perceived and understood, how they responded, plus the "why they act" term. Layer tags separate what is true from what they believe.

You can mark favourites and watched people; they appear as roster chips (up to 12; saved with the game). Alerts: critical cards for individuals, merged cards for groups, one digest a day; toasts are off in this mode.

## 7. What to watch for

- **Gestures.** People communicate by visible gestures (raised arms, offers, begging, thanks, fear). Look for a gesture, a listener's turn, then an act. Pointing out where a gesture and the reaction did not match is useful feedback.
- **Carried goods.** The real item is shown in hand or on the head. When someone gives, the receiver takes the same item at the same moment.
- **Body states.** Bandages and slings for injury, limping, a tint and slump for illness, droop for fatigue, belly for pregnancy, grey hair and head size by age.
- **Divine effects.** Ring for a suggestion, double ring for a command, tether while compelled (a cross when blocked), heal cross, bless aura, smite bolt, create sparkle, dissolve on destroy.
- **Households and children.** Adults feeding begging children, couples, births, deaths, orphans.
- **Organisations and communal stores; employment; owners using their fields.** In the code these are behind switches (`Civ`, `Orgs`) that the god-only start does not turn on at head `f9046f6`. If the final build enables them, look for: a person owning a field and its store; a hired worker working the owner's field and being paid by hand; owners taking produce out of their store; a shared store founded by agreement of trusting people of different households and handing out rations. If you see none of these, that is expected unless the lead says the final build enables them.
- **Whether people refuse you.** The honest test is whether your power makes sense to the person, not whether it always works.

**Developer overlays** (not for normal play):

| Key | Overlay | Where |
|---|---|---|
| F6 | Decode: shows what each gesture means and who it is addressed to | Developer games only (`-goddev`) |
| F7 | Captions: a short plain caption per gesture | Any game |
| F8 | True-state view (truth against belief) | See the True state button |
| F9 | Grid: the node lattice and producing-hex glow | Developer games only (`-goddev`) |

## 8. Known limitations

State these plainly in any report; they are not bugs:

- **Villages decline** over a few in-game years. Child mortality and food are the limit; the population-viability targets are not met. Extinction on some seeds within years is expected. The last measured runs have most seeds collapsing within about three years in the favourable setting.
- **No avatar** (a manifested body) in this build. Only possession exists, and only headless (section 5).
- **No industry, money, taxes, governments or elections.** Organisation support is a shared store and a few records, off by default.
- **No resurrection.**
- **No healthcare institutions or institutional reports.** The healthcare package (C7a) is not merged into this tree (the development log has no C7 record); treatment is only what individuals do on their own.
- **Commandment influence is weak.** It changes decisions a little (measured: adopters feed begging children about as often as non-adopters, because people already do it); it does not command people. The tests that want a larger effect are marked as outstanding.
- **Prayer, worship and offerings are not implemented.** Belief changes (Tier 2) are off by default.
- **Bless** speeds recovery but does not yet lower illness or work-injury chance; **Protect** applies to damage dealt by the game's damage code, not to every injury source.
- **Some acts are refused as unavailable**: Trade, Communicate and Tend orders may be refused. Group commands expand to one order per person (cap 32).
- **Performance** is limited above about 500 people (about 200 ticks per second at 500 people on the development machine, busy; the full island is about 120). Fast speeds will lag at large populations. Frame time at 10x has not been measured in the window.
- **Save compatibility.** Saves from before this build (version 5) are refused.
- OnGUI drawing was not exercised under automated tests; visual glitches are plausible. Praying and teaching animations are not triggered yet.

## 9. Feedback we want

Please note the seed, the speed, and roughly the in-game day for each item.

- [ ] Did the start card, Mythic modes and controls work first time? What was unclear?
- [ ] Could you tell what each power does and what it cost, without help?
- [ ] After each power, did the person's reaction make sense (followed, refused, misread, blocked)? Was the reason clear on the Divine and Mind tabs?
- [ ] Did a compulsion ever do something odd, hang or leave someone stuck?
- [ ] Did a miracle change the world as it said (health, buildings, food)? Anything that did not?
- [ ] Gestures: any that looked wrong, silent or unrelated to what happened next?
- [ ] Did people look believable (carrying, injured, ill, pregnant, old)? Glitches or floating items?
- [ ] How long before the village visibly declined? Which seed, and what seemed to cause it (food, children, illness)?
- [ ] Was it fun to watch with no input at all (Disabled mode)? When did you get bored?
- [ ] Performance: frame rate at 1x and at 10x; any stutter; a crash or error (send `player.log`).
- [ ] Anything you tried to do and could not (for example, possess a person).
- [ ] Save and load: did the game resume with the same people, Mythic and orders?
