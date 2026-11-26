using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using God.Recording;
using God.Sim;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>M0.7 and G2.6: replay files round-trip, and a tampered file is caught at its first bad checkpoint.</summary>
    public class ReplayTests
    {
        /// <summary>
        /// A run with a human faction (F0) played from outside the sim: a scripted chain builder plus random pause and
        /// priority commands, all external, so all logged; plus a scenario command schedule. Returns the run directory.
        /// </summary>
        private static RunResult RecordedRun(string dir, int wantCommands, out int accepted)
        {
            var spec = TestUtil.Spec("watch6", 11, 30000, "setup.human=0");
            spec.Scenario.Commands.Add(new ScheduledCommand { Tick = 9000, Type = "DeclareWar", Args = JObject.Parse("{\"PlayerId\":1,\"Against\":2}") });
            spec.Scenario.Commands.Add(new ScheduledCommand { Tick = 12000, Type = "OfferPeace", Args = JObject.Parse("{\"PlayerId\":1,\"To\":2}") });
            spec.OutRoot = dir; spec.WriteReplay = true;
            ChainBot bot = null;
            var rng = new Random(42); // test-side randomness: what a person might click; the sim never sees this generator
            int n = 0;
            spec.BeforeStep = sim =>
            {
                var s = sim.State;
                if (bot == null) bot = new ChainBot(sim, Faction.F0);
                if (s.Factions.IsEliminated(Faction.F0)) return;
                if (s.Tick % 20 == 0) bot.Think();
                if (s.Tick % 30 != 0 || n >= wantCommands) return;
                var mine = s.Economy.Buildings.Where(b => b.Faction == Faction.F0 && !b.IsHq).ToList();
                for (int tries = 0; tries < 8; tries++)
                {
                    Command c;
                    int pick = rng.Next(3);
                    if (pick == 0 && mine.Count > 0) c = new SetPaused(Faction.F0, mine[rng.Next(mine.Count)].Id, rng.Next(4) == 0);
                    else if (pick == 1 && mine.Count > 0)
                    {
                        var b = mine[rng.Next(mine.Count)];
                        var inputs = Enumerable.Range(1, Wares.Count - 1).Where(w => b.Def.In[w] > 0 || b.Def.Cost[w] > 0).ToList();
                        if (inputs.Count == 0) continue;
                        c = new SetInputPriority(Faction.F0, b.Id, (Ware)inputs[rng.Next(inputs.Count)], (Priority)rng.Next(4));
                    }
                    else c = new DeclareWar(Faction.F0, (Faction)(1 + rng.Next(5)));
                    if (c is DeclareWar && rng.Next(10) != 0) continue; // wars are rare clicks
                    if (sim.Submit(c).Ok) { n++; break; }
                }
            };
            var r = RunEngine.Run(spec);
            accepted = n;
            return r;
        }

        [Test]
        public void Replay_RoundTrip_VerifiesAndReplays()
        {
            string dir = TestUtil.TempDir("replay");
            try
            {
                var r = RecordedRun(dir, 200, out int random);
                Assert.AreEqual(200, random, "200 random human commands were accepted");
                string file = Path.Combine(r.RunDir, "replay.godr");
                var rf = ReplayFile.Read(file);
                var cmds = rf.Of(ReplayEntryKind.Command).ToList();
                Assert.GreaterOrEqual(cmds.Count, 200 + 2, "every accepted external command is logged");
                long refused = r.Counters["commands_refused_at_submit"];
                Assert.GreaterOrEqual(cmds.Count(c => c.Origin == "schedule"), 1);
                Assert.AreEqual(2 - refused, cmds.Count(c => c.Origin == "schedule"), "the schedule's accepted commands are logged as external too (refused ones never queued)");
                Assert.IsTrue(cmds.All(c => c.Origin == "schedule" || c.Origin == "external"));
                Assert.AreEqual(r.Checkpoints.Count, rf.Of(ReplayEntryKind.Checkpoint).Count());

                Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out var o, out var e, "verify", file), o + e);
                StringAssert.Contains("match", o);

                // `replay` re-runs it (the schedule is not applied twice: only the log drives a replay)
                var spec = Cli.ReplaySpec(file, default);
                var again = RunEngine.Run(spec);
                Assert.IsNull(again.Divergence, again.Divergence?.ToString());
                Assert.AreEqual(r.FinalHash, again.FinalHash);
                Assert.AreEqual(r.Ticks, again.Ticks);

                // re-record a cheap Off run at Full afterwards (replay --record full)
                string re = Path.Combine(dir, "rerecord");
                Assert.AreEqual(ExitCodes.Ok, TestUtil.Cli(out o, out e, "replay", file, "--record", "full", "--out", re, "--quiet"), o + e);
                Assert.IsTrue(Directory.GetFiles(re, "events.jsonl.gz", SearchOption.AllDirectories).Length == 1);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        [Test]
        public void Replay_DetectsTampering_AtTheFirstBadCheckpoint()
        {
            string dir = TestUtil.TempDir("tamper");
            try
            {
                var r = RecordedRun(dir, 60, out _);
                string file = Path.Combine(r.RunDir, "replay.godr");
                var lines = ReadLines(file);
                // move the scheduled war (tick 9000) 400 ticks later
                int idx = lines.FindIndex(l => l.Contains("\"o\":\"schedule\"") && l.Contains("DeclareWar"));
                Assert.Greater(idx, 0);
                var o = JObject.Parse(lines[idx]);
                long t = (long)o["t"];
                Assert.AreEqual(9000, t);
                o["t"] = t + 400;
                lines.RemoveAt(idx);
                int at = lines.FindIndex(1, l => { var j = JObject.Parse(l); var tt = (long?)j["t"] ?? (long?)j["end"] ?? 0; return tt > t + 400; });
                lines.Insert(at < 0 ? lines.Count - 1 : at, o.ToString(Newtonsoft.Json.Formatting.None));
                string bad = Path.Combine(dir, "tampered.godr");
                WriteLines(bad, lines);

                int code = TestUtil.Cli(out var output, out var err, "verify", bad);
                Assert.AreEqual(ExitCodes.Divergence, code, output + err);
                StringAssert.Contains("first bad checkpoint at tick", output);
                long expectedBad = ((t + 1199) / 1200) * 1200;
                StringAssert.Contains("first bad checkpoint at tick " + expectedBad, output, "the checkpoint after the moved command");
                // By 9600 both versions are at war (factions agree again), but the original's soldiers fought for 400 more
                // ticks: the military part names the difference.
                StringAssert.Contains("modules military", output);
            }
            finally { TestUtil.Cleanup(dir); }
        }

        private static List<string> ReadLines(string gz)
        {
            using (var r = new StreamReader(new GZipStream(File.OpenRead(gz), CompressionMode.Decompress), Encoding.UTF8))
                return r.ReadToEnd().Split('\n').Where(x => x.Length > 0).ToList();
        }

        private static void WriteLines(string gz, List<string> lines)
        {
            using (var w = new StreamWriter(new GZipStream(File.Create(gz), CompressionLevel.Optimal), new UTF8Encoding(false)) { NewLine = "\n" })
                foreach (var l in lines) w.WriteLine(l);
        }
    }
}
