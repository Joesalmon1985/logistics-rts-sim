using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using God.Recording;
using God.Sim;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M0.10 and G6.1: <c>run --check smoke</c> on watch6 gives the same verdict, counters and final standings line as the
    /// old Unity SmokeRun logic, for seeds 1-4. <see cref="LegacySmoke"/> is the pre-M0 SmokeRun loop copied without Unity
    /// (frames only batch ticks, so a plain loop steps the same ticks).
    /// </summary>
    public class SmokeParityTests
    {
        private sealed class LegacySmoke
        {
            private int _wars, _captures, _eliminations, _invariantFailures, _offered, _accepted, _refused, _killed;
            private readonly int[] _maxHqs = new int[FactionInfo.Max];
            private string _invariantNote = "ok";
            public string LastLine; public bool Ok;

            public void Play(GodSim sim, long endTick)
            {
                var s = sim.State;
                while (s.Tick < endTick && !s.IsGameOver)
                {
                    sim.Step();
                    foreach (var ev in sim.DrainEvents())
                    {
                        if (ev is WarDeclared) _wars++;
                        else if (ev is HqCaptured) _captures++;
                        else if (ev is FactionEliminated) _eliminations++;
                        else if (ev is TradeOffered) _offered++;
                        else if (ev is TradeAccepted) _accepted++;
                        else if (ev is TradeRefused) _refused++;
                        else if (ev is EnvoyKilled) _killed++;
                    }
                    if (s.Tick % 200 == 0) foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = System.Math.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
                    if (s.Tick % 500 == 0) CheckInvariants(s);
                    if (s.Tick % 1000 == 0) LastLine = "SMOKE " + Summary(s);
                }
                Finish(s);
            }

            private void CheckInvariants(GodState s)
            {
                var bad = SimInvariants.Check(s);
                if (bad.Count == 0) return;
                _invariantFailures++;
                _invariantNote = "tick " + s.Tick + ": " + string.Join("; ", bad);
            }

            private string Summary(GodState s)
            {
                var parts = FactionRules.Standings(s).Select(st => FactionInfo.Name(st.Faction) + " " + st.VictoryPoints + " VP " + st.Hqs + " HQ "
                    + st.Citadels + " Cit " + s.Military.CountUnits(st.Faction) + " units"
                    + (st.Eliminated ? " (out)" : s.Factions.Enemies(st.Faction).Count > 0 ? " war " + string.Join("/", s.Factions.Enemies(st.Faction).Select(FactionInfo.Name)) : ""));
                return "tick " + s.Tick + " min " + (s.Tick / 600.0).ToString("0.0", CultureInfo.InvariantCulture) + " | " + string.Join(" | ", parts)
                    + " | soldiers made " + s.Economy.UnitsProduced + " harvester upgrades " + s.Economy.Buildings.Count(b => b.Level > 1)
                    + " | wars " + _wars + " captures " + _captures + " eliminations " + _eliminations
                    + " | trades offered " + _offered + " accepted " + _accepted + " refused " + _refused + " killed " + _killed;
            }

            private void Finish(GodState s)
            {
                foreach (var f in s.Setup.Factions) _maxHqs[(int)f] = System.Math.Max(_maxHqs[(int)f], s.Territory.HqsOf(f).Count);
                CheckInvariants(s);
                int expanded = s.Setup.Factions.Count(f => _maxHqs[(int)f] >= 2);
                bool won = s.IsGameOver && s.Factions.Winner >= 0;
                bool healthy = expanded >= 3 && _wars >= 1 && _captures >= 1;
                bool ok = _invariantFailures == 0 && (won || healthy);
                string over = s.IsGameOver
                    ? "GAME OVER at tick " + s.Factions.GameOverTick + " (minute " + (s.Factions.GameOverTick / 600.0).ToString("0.0", CultureInfo.InvariantCulture) + "): "
                      + (s.Factions.Winner >= 0 ? FactionInfo.Name((Faction)s.Factions.Winner) : "nobody") + " wins by " + s.Factions.EndReason
                    : "no game over by tick " + s.Tick;
                LastLine = "SMOKE END " + Summary(s) + " | " + over + " | factions expanded " + expanded + " | invariants " + _invariantNote
                    + " | exceptions " + 0 + " | " + (ok ? "OK" : "FAIL");
                Ok = ok;
            }

            public Dictionary<string, long> Counters() => new Dictionary<string, long>
            {
                ["smoke.wars"] = _wars, ["smoke.captures"] = _captures, ["smoke.eliminations"] = _eliminations, ["smoke.trades_offered"] = _offered,
                ["smoke.trades_accepted"] = _accepted, ["smoke.trades_refused"] = _refused, ["smoke.envoys_killed"] = _killed, ["smoke.invariant_failures"] = _invariantFailures,
            };
        }

        [Test]
        public void RunCheckSmoke_MatchesTheOldSmokeRun_Seeds1To4()
        {
            var specs = Enumerable.Range(1, 4).Select(seed => TestUtil.Spec("watch6", (ulong)seed, 40000)).ToList();
            var runs = TestUtil.RunAll(specs);
            System.Threading.Tasks.Parallel.For(0, 4, i =>
            {
                ulong seed = (ulong)(i + 1);
                // the old smoke: GodBootstrap's setup (watch mode) and AiHost's brains, stepped by the old loop
                var sim = GodSim.Create(new GameSetup(seed, GameSetup.NoHuman));
                AiDirector.Attach(sim, AiDirector.NonHuman(sim.State), AiTuning.Default, new AiTrader());
                var legacy = new LegacySmoke();
                legacy.Play(sim, 40000);
                var r = runs[i];
                Assert.AreEqual(legacy.LastLine, r.SmokeLine, "seed " + seed + ": the final line");
                Assert.AreEqual(legacy.Ok, r.SmokeOk, "seed " + seed + ": the verdict");
                foreach (var kv in legacy.Counters()) Assert.AreEqual(kv.Value, r.Counters[kv.Key], "seed " + seed + " " + kv.Key);
                Assert.AreEqual(sim.StateHash(), r.FinalHash, "seed " + seed + ": the runner's game is the old smoke's game");
                Assert.IsTrue(r.SmokeOk == true, "the stage 3 smoke passes (G6.1): " + r.SmokeLine);
            });
        }
    }
}
