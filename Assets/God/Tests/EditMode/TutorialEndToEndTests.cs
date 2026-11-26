using System.Collections.Generic;
using System.Linq;
using God.Sim;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// Q4: the tutorial played end to end on its island (seed 111 since M0; 40 before) with the real building defs and the five AIs (with trade)
    /// running. A scripted learner reads the current card and does what it says, with commands only, as a person would
    /// (place, road, HQ, guards, upgrade, trade offer, war, mark, Citadel). It presses Next on cards without a goal and never
    /// presses Skip step.
    /// </summary>
    public class TutorialEndToEndTests
    {
        /// <summary>The learner: an <see cref="EconomyBot"/> (place and join by road) that follows the card it is shown.</summary>
        private sealed class Learner : EconomyBot
        {
            private readonly TutorialGuide _guide;
            private readonly HashSet<string> _done = new HashSet<string>();
            public readonly List<string> Notes = new List<string>();
            private long _lastOffer = -10000;
            private int _partner;

            public Learner(GodSim sim, TutorialGuide guide) : base(sim, Faction.F0) { _guide = guide; }

            private bool Has(string kind) => S.Economy.Buildings.Any(b => b.Faction == Side && !b.IsHq && b.Kind == kind);

            /// <summary>Places a building of the kind once (it waits for wares like any site).</summary>
            private void Once(string kind)
            {
                if (Has(kind) || _done.Contains("tried " + kind + " " + S.Tick / 600)) return;
                _done.Add("tried " + kind + " " + S.Tick / 600);
                if (PlaceAndConnect(kind) != 0) Notes.Add("t" + S.Tick + " placed " + kind);
            }

            private Hq FirstHq => T.HqsOf(Side).OrderBy(h => h.Id).First();

            public void Think()
            {
                var step = _guide.Step;
                if (step == null) return;
                switch (step.Id)
                {
                    case "welcome": _guide.Skip(); break; // Next
                    case "woodcutter": Once(BuildingKinds.Woodcutter); break;
                    case "road":
                        foreach (var b in T.Buildings.Where(b => b.Faction == Side && b.Kind == BuildingKinds.Woodcutter)) ConnectFlag(b.FlagNode);
                        break;
                    case "dice": break; // wait for the dice
                    case "stone-clay": Once(BuildingKinds.Quarry); Once(BuildingKinds.ClayPit); break;
                    case "sawmill-kiln": Once(BuildingKinds.Sawmill); Once(BuildingKinds.Kiln); break;
                    case "second-hq":
                        if (T.Hqs.Any(h => h.Faction == Side && h.Site)) break;
                        int corner = TutorialIsland.SecondHqCornerTouching(Sim, Side, Terrain.Fields);
                        if (corner >= 0 && PlaceHqAndConnect(corner) != 0) Notes.Add("t" + S.Tick + " placed the second HQ");
                        break;
                    case "farm-mine-bakery": Once(BuildingKinds.Farm); Once(BuildingKinds.Mine); Once(BuildingKinds.Bakery); break;
                    case "smelter-axesmith": Once(BuildingKinds.Smelter); Once(BuildingKinds.Axesmith); break;
                    case "barracks": Once(BuildingKinds.Barracks); break;
                    case "guards": Send(SetDefend.OnBuilding(Side, FirstHq.Id, 2)); break;
                    case "upgrade":
                        if (S.Economy.Buildings.Any(b => b.Faction == Side && b.Upgrading)) break;
                        foreach (var b in S.Economy.Buildings.Where(b => b.Faction == Side && b.Built && b.UpgradeDef != null && b.Level == 1).OrderBy(b => b.Id))
                            if (Sim.Check(new UpgradeBuilding(Side, b.Id)).Ok) { Send(new UpgradeBuilding(Side, b.Id)); Notes.Add("t" + S.Tick + " upgrade " + b.Kind); break; }
                        break;
                    case "trade": Trade(); break;
                    case "war":
                        if (!S.Factions.AtWar(Side, Faction.F1)) Send(new DeclareWar(Side, Faction.F1));
                        var target = T.HqsOf(Faction.F1).FirstOrDefault();
                        if (target != null && S.Military.MarkedOf(Side).Count == 0) Send(new MarkTarget(Side, target.Id));
                        break;
                    case "citadel":
                        if (!S.Economy.Buildings.Any(b => b.Faction == Side && b.IsHq && b.Upgrading)) Send(new UpgradeToCitadel(Side, FirstHq.Id));
                        break;
                }
            }

            /// <summary>A fair offer to the next faction in turn: logs (or any spare raw) for one ware it has in stock.</summary>
            private void Trade()
            {
                if (S.Trade.EnvoyCountOf(Side) > 0 || S.Tick - _lastOffer < 300) return;
                for (int k = 0; k < 5; k++)
                {
                    var to = (Faction)(1 + (_partner++ % 5));
                    if (S.Factions.IsEliminated(to)) continue;
                    foreach (var want in new[] { Ware.Clay, Ware.Grain, Ware.Log, Ware.Ore, Ware.Stone })
                    {
                        if (TradeRules.FreeStock(S, to, want) < 2) continue;
                        int value = 2 * TradeTuning.ValueOf(want);
                        foreach (var give in new[] { Ware.Log, Ware.Stone, Ware.Clay, Ware.Grain, Ware.Plank })
                        {
                            if (give == want) continue;
                            int n = (value + TradeTuning.ValueOf(give) - 1) / TradeTuning.ValueOf(give);
                            if (TradeRules.FreeStock(S, Side, give) < n + 2) continue;
                            var offer = new OfferTrade(Side, to, new[] { new WareCount(give, n) }, new[] { new WareCount(want, 2) });
                            if (!Sim.Check(offer).Ok) continue;
                            Send(offer);
                            _lastOffer = S.Tick;
                            Notes.Add("t" + S.Tick + " offer " + n + " " + give + " for 2 " + want + " to " + FactionInfo.Name(to));
                            return;
                        }
                    }
                }
            }
        }

        [Test]
        public void ScriptedLearner_FollowsAllFifteenCards_ToTheLastStep()
        {
            var sim = GodSim.Create(TutorialIsland.Setup());
            var s = sim.State;
            AiDirector.Attach(sim, AiDirector.NonHuman(s), null, new AiTrader());
            var guide = new TutorialGuide();
            var learner = new Learner(sim, guide);
            var reached = new Dictionary<string, long>();
            const long Limit = 60 * 600;
            while (!guide.Finished && s.Tick < Limit && !s.IsGameOver && !s.Factions.IsEliminated(Faction.F0))
            {
                var step = guide.Step;
                if (step != null && !reached.ContainsKey(step.Id)) reached[step.Id] = s.Tick;
                if (s.Tick % 10 == 0) learner.Think();
                sim.Step();
                foreach (var ev in sim.DrainEvents())
                    if (ev is BuildingCompleted bc && bc.Faction == Faction.F0) learner.Notes.Add("t" + s.Tick + " built " + bc.Kind);
                    else if (ev is UnitProduced up && up.Faction == Faction.F0) learner.Notes.Add("t" + s.Tick + " soldier " + up.Kit);
                if (s.Tick % 1200 == 0) learner.Notes.Add("t" + s.Tick + " stock " + string.Join(" ", Enumerable.Range(1, Wares.Count - 1).Where(w => s.Economy.TotalStock(Faction.F0, (Ware)w) > 0).Select(w => (Ware)w + "=" + s.Economy.TotalStock(Faction.F0, (Ware)w))));
                guide.Update(s, Faction.F0);
            }
            string Min(long t) => (t / 600.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var table = string.Join("\n", TutorialSteps.All.Select((st, i) => "  " + (i + 1).ToString().PadLeft(2) + " " + st.Id.PadRight(18)
                + (reached.TryGetValue(st.Id, out long t) ? "card at minute " + Min(t) : "never shown")));
            TestContext.WriteLine("Tutorial end to end on seed " + TutorialIsland.Seed + ", finished at minute " + Min(s.Tick) + ":\n" + table
                + "\n" + string.Join("\n", learner.Notes.Take(60)));
            Assert.IsTrue(guide.Finished, "the learner reached the end: stuck on " + guide.Step?.Id + " at minute " + Min(s.Tick) + "\n" + table + "\n" + string.Join("\n", learner.Notes));
            Assert.Less(s.Tick, 45 * 600, "within 45 game minutes");
            // The first soldier (card 10 shows once the barracks card's goal is met) comes in a learner's reasonable time.
            Assert.Less(reached["guards"], 16 * 600, "first soldier by minute 16");
            Assert.IsEmpty(SimInvariants.Check(s));
        }
    }
}
