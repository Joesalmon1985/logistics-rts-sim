using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>What a step's goal can read: the sim state and the faction being taught. Read only.</summary>
    public readonly struct TutorialContext
    {
        public readonly GodState S;
        public readonly Faction Me;
        public TutorialContext(GodState s, Faction me) { S = s; Me = me; }

        /// <summary>How many buildings of this kind the faction has (construction sites count: the goal is to place one).</summary>
        public int Count(string kind)
        {
            int n = 0;
            foreach (var b in S.Economy.Buildings) if (b.Faction == Me && !b.IsHq && b.Kind == kind) n++;
            return n;
        }

        public bool Has(string kind) => Count(kind) > 0;

        /// <summary>True when a road of the faction ends at the flag of one of its buildings of this kind.</summary>
        public bool RoadJoins(string kind)
        {
            var t = S.Territory;
            foreach (var b in t.Buildings)
            {
                if (b.Faction != Me || b.Kind != kind) continue;
                foreach (var r in t.Roads) if (r.FlagA == b.FlagId || r.FlagB == b.FlagId) return true;
            }
            return false;
        }

        public int Soldiers()
        {
            int n = 0;
            foreach (var u in S.Military.Units) if (u.Faction == Me && u.Kind == UnitKind.Infantry) n++;
            return n;
        }
    }

    /// <summary>Where the view should draw the pointer for a step (a spot on the map).</summary>
    public enum TutorialSpot { None = 0, Hq = 1, SecondHq = 2 }

    /// <summary>
    /// One card of the tutorial. Data only, so the steps are easy to edit: a title, a few short lines, a goal line and a test
    /// that reads sim state (null means no goal: the player presses Next). <see cref="Button"/> names the build-bar button to
    /// glow (a building kind, or "road", "hq", "demolish", "trade"), <see cref="Spot"/> a place on the map.
    /// </summary>
    public sealed class TutorialStep
    {
        public string Id;
        public string Title;
        public string[] Lines;
        public string Goal;
        public Func<TutorialContext, bool> Done;
        public string Button;
        public TutorialSpot Spot;
    }

    /// <summary>The fifteen steps. Edit here to change the tutorial.</summary>
    public static class TutorialSteps
    {
        public static readonly IReadOnlyList<TutorialStep> All = new List<TutorialStep>
        {
            new TutorialStep
            {
                Id = "welcome", Title = "Welcome, god of the island",
                Lines = new[] { "You are Amber. Your people live around your HQ, and they build what you tell them to.",
                    "Right-drag pans the view and the wheel zooms. Space pauses; plus and minus change the speed.",
                    "These cards never stop the game. Skip any step, or hide them all." },
                Goal = "Look around, then press Next.", Spot = TutorialSpot.Hq,
            },
            new TutorialStep
            {
                Id = "woodcutter", Title = "Your first building",
                Lines = new[] { "Open the Gather tab, pick Woodcutter and click a glowing spot in a forest.",
                    "Builders walk out from your HQ and carry the wares it needs." },
                Goal = "Place a woodcutter.", Button = BuildingKinds.Woodcutter,
                Done = c => c.Has(BuildingKinds.Woodcutter),
            },
            new TutorialStep
            {
                Id = "road", Title = "Roads carry everything",
                Lines = new[] { "Nothing moves without a road. Pick Road on the Land tab, click your HQ flag, then the flag of the woodcutter.",
                    "Click the last flag to finish. Carriers walk the road and haul wares both ways." },
                Goal = "Join the woodcutter to a road.", Button = "road",
                Done = c => c.RoadJoins(BuildingKinds.Woodcutter),
            },
            new TutorialStep
            {
                Id = "dice", Title = "Dice and piles",
                Lines = new[] { "Every hex has a number. When the dice roll that number, a pile of its goods appears on the hex.",
                    "Your woodcutter takes logs from piles in its own hex. No pile, no work, so watch the dice." },
                Goal = "Get a log delivered from your woodcutter.",
                Done = c => { foreach (var b in c.S.Economy.Buildings) if (b.Faction == c.Me && b.Kind == BuildingKinds.Woodcutter && b.Built && b.Produced >= 1) return true; return false; },
            },
            new TutorialStep
            {
                Id = "stone-clay", Title = "Stone and clay",
                Lines = new[] { "A quarry on mountain gives stone. A clay pit on hills gives clay.",
                    "Place each one, and join them to your road net." },
                Goal = "Place a quarry and a clay pit.", Button = BuildingKinds.Quarry,
                Done = c => c.Has(BuildingKinds.Quarry) && c.Has(BuildingKinds.ClayPit),
            },
            new TutorialStep
            {
                Id = "sawmill-kiln", Title = "Plank and brick",
                Lines = new[] { "Raw goods become better goods in the Process tab. A sawmill turns log and stone into planks.",
                    "A brick kiln turns log and clay into bricks. Every building you add needs them." },
                Goal = "Place a sawmill and a brick kiln.", Button = BuildingKinds.Sawmill,
                Done = c => c.Has(BuildingKinds.Sawmill) && c.Has(BuildingKinds.Kiln),
            },
            new TutorialStep
            {
                Id = "second-hq", Title = "A second HQ",
                Lines = new[] { "Some land you need lies beside your first area. A second HQ, two corners from your first, reaches it.",
                    "Pick HQ on the Land tab and click a glowing corner next to fields. Join it to your roads." },
                Goal = "Place a second HQ.", Button = "hq", Spot = TutorialSpot.SecondHq,
                Done = c => c.S.Territory.HqsOf(c.Me).Count >= 2,
            },
            new TutorialStep
            {
                Id = "farm-mine-bakery", Title = "Bread needs a farm",
                Lines = new[] { "A farm on fields gives grain. A mine on mountain gives ore. A bakery turns log and grain into bread.",
                    "Bread feeds soldiers and pays for the best harvester upgrades." },
                Goal = "Place a farm, a mine and a bakery.", Button = BuildingKinds.Farm,
                Done = c => c.Has(BuildingKinds.Farm) && c.Has(BuildingKinds.Mine) && c.Has(BuildingKinds.Bakery),
            },
            new TutorialStep
            {
                Id = "smelter-axesmith", Title = "Iron and a first weapon",
                Lines = new[] { "A smelter turns log and ore into iron. The Arms tab holds the armouries.",
                    "An axesmith takes iron and planks and makes axes: the first weapon your soldiers will carry." },
                Goal = "Place a smelter and an axesmith.", Button = BuildingKinds.Smelter,
                Done = c => c.Has(BuildingKinds.Smelter) && c.Has(BuildingKinds.Axesmith),
            },
            new TutorialStep
            {
                Id = "barracks", Title = "Your first soldier",
                Lines = new[] { "The barracks (Army tab) takes bread and a weapon for each soldier.",
                    "It also fits a shield, a helmet and body armour if it has them in stock." },
                Goal = "Get a soldier out of your barracks.", Button = BuildingKinds.Barracks,
                Done = c => c.Soldiers() >= 1,
            },
            new TutorialStep
            {
                Id = "guards", Title = "Guards",
                Lines = new[] { "Click one of your buildings and set guards on its card. Guards stand by it and fight raiders.",
                    "Put them where an enemy would strike first: your HQ and your barracks." },
                Goal = "Put guards on a building.",
                Done = c => { foreach (var f in c.S.Military.Defends) if (f.Faction == c.Me && f.Guards > 0) return true; return false; },
            },
            new TutorialStep
            {
                Id = "upgrade", Title = "Better harvesters",
                Lines = new[] { "Click a harvester and press Upgrade on its card. Level 2 costs planks and bricks and gathers faster.",
                    "A bigger building means more goods from each pile." },
                Goal = "Upgrade a harvester to level 2.",
                Done = c => { foreach (var b in c.S.Economy.Buildings) if (b.Faction == c.Me && !b.IsHq && b.Level >= 2) return true; return false; },
            },
            new TutorialStep
            {
                Id = "trade", Title = "Trade",
                Lines = new[] { "Another faction may have what you lack. Open Trade on the Land tab, choose a faction and make an offer.",
                    "An envoy walks your goods over. A fair offer is usually accepted." },
                Goal = "Complete one trade.", Button = "trade",
                Done = c => { foreach (var r in c.S.Trade.History) if (r.Outcome == TradeOutcome.Accepted && (r.From == c.Me || r.To == c.Me)) return true; return false; },
            },
            new TutorialStep
            {
                Id = "war", Title = "War and targets",
                Lines = new[] { "The other factions will not attack you while you learn. When you are ready, open an enemy building's card and declare war.",
                    "Then press Mark target on an enemy building. Your soldiers go for marked buildings." },
                Goal = "Declare war and mark a target.",
                Done = c => c.S.Factions.Enemies(c.Me).Count > 0 && c.S.Military.MarkedOf(c.Me).Count > 0,
            },
            new TutorialStep
            {
                Id = "citadel", Title = "Citadel and ten points",
                Lines = new[] { "The first faction to 10 victory points wins. Each HQ is worth 1 point and each Citadel 2.",
                    "Click your HQ and press Upgrade to Citadel. It needs bricks, mortar, planks and steel.",
                    "Steel comes from a steel forge (Process tab), which takes stone and ore." },
                Goal = "Start the Citadel upgrade.",
                Done = c =>
                {
                    foreach (var h in c.S.Territory.HqsOf(c.Me)) if (h.Citadel) return true;
                    foreach (var b in c.S.Economy.Buildings) if (b.Faction == c.Me && b.IsHq && b.Upgrading) return true;
                    return false;
                },
            },
        };
    }

    /// <summary>
    /// Where a player is in the tutorial. It reads sim state and never changes it. <see cref="Update"/> ticks the goal of the
    /// current step off when the player has done it (it stays ticked), and moves on after <see cref="HoldTicks"/> sim ticks so
    /// the tick can be seen. <see cref="Skip"/> moves on at any time; <see cref="Hide"/> and <see cref="Show"/> switch the
    /// cards off and on (progress stops while they are off).
    /// </summary>
    public sealed class TutorialGuide
    {
        /// <summary>Sim ticks a ticked-off goal stays on screen before the next card (1.5 s at 10 ticks a second).</summary>
        public int HoldTicks = 15;

        public int Index { get; private set; }
        public bool Hidden { get; private set; }
        public bool GoalMet { get; private set; }
        public long MetTick { get; private set; }
        /// <summary>True after the last card has been dismissed.</summary>
        public bool Finished { get; private set; }

        public int StepCount => TutorialSteps.All.Count;
        public TutorialStep Step => Finished || Index >= StepCount ? null : TutorialSteps.All[Index];
        /// <summary>True when a card should show now.</summary>
        public bool Visible => !Hidden && !Finished && Step != null;
        /// <summary>The first button reads Next when there is nothing to wait for (no goal, or it is ticked), else Skip step.</summary>
        public bool CanContinue => Step != null && (Step.Done == null || GoalMet);

        public void Update(GodState s, Faction me)
        {
            var step = Step;
            if (step == null || Hidden || s.Economy == null) return;
            if (step.Done == null) return;
            if (!GoalMet)
            {
                if (!step.Done(new TutorialContext(s, me))) return;
                GoalMet = true; MetTick = s.Tick;
            }
            if (s.Tick - MetTick >= HoldTicks) Advance();
        }

        /// <summary>Next or Skip step: moves to the following card. After the last card the tutorial is finished.</summary>
        public void Skip() { if (Step != null) Advance(); }

        public void Hide() { Hidden = true; }
        public void Show() { Hidden = false; }

        private void Advance()
        {
            Index++; GoalMet = false; MetTick = 0;
            if (Index >= StepCount) Finished = true;
        }

        // ---- saved with the game (the bootstrap's save extras) ----

        public string SaveState() => Index + ";" + (Hidden ? 1 : 0) + ";" + (GoalMet ? 1 : 0) + ";" + MetTick + ";" + (Finished ? 1 : 0);

        public void LoadState(string data)
        {
            var p = (data ?? "").Split(';');
            if (p.Length < 5) return;
            int.TryParse(p[0], out int idx); Index = Math.Max(0, Math.Min(idx, StepCount));
            Hidden = p[1] == "1"; GoalMet = p[2] == "1";
            long.TryParse(p[3], out long mt); MetTick = mt;
            Finished = p[4] == "1" || Index >= StepCount;
        }
    }
}
