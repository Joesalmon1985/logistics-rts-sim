using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Social;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M5 on the owner's island, 20 seeds (ACCEPTANCE M5.15): refusals and counter-offers occur naturally, the composed acts
    /// appear with several topics, and the deals' outcomes are reported (completed, refused, abandoned, defected).
    /// </summary>
    public class M5NaturalTests
    {
        [Test]
        public void M5_15_Band6x20_20Seeds_RefusalsAndCounterOffersOccurNaturally()
        {
            var rows = Enumerable.Range(1, 20).AsParallel().AsOrdered().Select(sd =>
            {
                var setup = new GameSetup((ulong)sd, GameSetup.NoHuman, 6) { Population = true, StartNpcsPerFaction = 20, Victory = VictorySpec.SandboxOnly() };
                var sim = SimBuilder.Create(setup).Sim;
                var evs = new List<SimEvent>();
                var buf = new List<SimEvent>();
                for (int t = 0; t < 1200 * 4; t++)
                {
                    sim.Step(); buf.Clear(); sim.DrainInto(buf);
                    foreach (var e in buf) if (e is GestureMade || e is ExchangeClosed || e is ExchangeStep || e is GiftGiven) evs.Add(e);
                }
                var g = evs.OfType<GestureMade>().ToList();
                int counters = g.Where(x => x.Act == GestureAct.Offer && x.DealId != 0).GroupBy(x => x.DealId).Count(x => x.Select(y => y.Speaker).Distinct().Count() > 1);
                return (sd, refuse: g.Count(x => x.Act == GestureAct.Refuse), counters, acts: g.Select(x => x.Act).Distinct().ToList(),
                    closed: evs.OfType<ExchangeClosed>().GroupBy(c => c.Outcome).ToDictionary(c => c.Key, c => c.Count()), steps: evs.OfType<ExchangeStep>().Count(), gifts: evs.OfType<GiftGiven>().Count());
            }).ToList();
            foreach (var r in rows)
                TestContext.WriteLine("seed " + r.sd + ": refusals " + r.refuse + ", counter-offers " + r.counters + ", exchange steps " + r.steps + ", gifts " + r.gifts + "; acts " + string.Join(",", r.acts)
                    + "; outcomes " + string.Join(",", r.closed.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));
            Assert.Greater(rows.Sum(r => r.refuse), 0, "refusals occur");
            Assert.Greater(rows.Sum(r => r.counters), 0, "counter-offers occur");
            Assert.Greater(rows.Sum(r => r.steps), 0);
            Assert.Greater(rows.Sum(r => r.gifts), 0);
            TestContext.WriteLine("warnings: " + rows.Count(r => r.acts.Contains(GestureAct.Warn)) + " of 20 seeds");
        }
    }
}
