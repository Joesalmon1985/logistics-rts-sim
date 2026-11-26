using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L7. Sucker, Cheat, Grudger (PDF 206 to 209). STRATEGY_BENCHMARKS 4.3, 4.6 L7; ACCEPTANCE M2.9.</summary>
    public sealed class L07SuckerCheatGrudger : LabExperiment
    {
        public override string Id => "L7";
        public override string Title => "Sucker, Cheat, Grudger";
        public override string Source => "PDF 206-209";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented payoffs: the book reports Dawkins's simulation qualitatively and gives no payoffs; b = 3, c = 1, n = 10 and the start shares are ours; only the order of events and the bistability are tested";
        public override string ExpectedSummary => "Sucker falls below 0.6 (about gen 40); Cheat peaks 0.25-0.40 (gen 40-90) then < 0.01 by gen 150; Grudger ends 0.70 +/- 0.05, Sucker 0.30 +/- 0.05; G/C boundary 1/18 +/- 0.003 (0.04 -> Cheat, 0.07 -> Grudger)";

        /// <summary>The help game as a Prisoner's Dilemma: helped +b, helping -c (b = 3, c = 1): T 3, R 2, P 0, S -1.</summary>
        public static PdPayoffs Help => new PdPayoffs(3, 2, 0, -1);

        public static long[,] MatrixX1000()
        {
            var kinds = new[] { RepeatedKind.AllC, RepeatedKind.AllD, RepeatedKind.Grudger };
            var m = new long[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    RepeatedMatch.Play(kinds[i], 0, kinds[j], 0, 10, Help, null, null, out long a, out _);
                    m[i, j] = a * 1000;
                }
            return m;
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            var m = MatrixX1000();
            long[] doc = { 20000, -10000, 20000, 30000, 0, 3000, 20000, -1000, 20000 };
            bool same = true;
            for (int i = 0; i < 9; i++) if (m[i / 3, i % 3] != doc[i]) same = false;
            ck.True("the state machines (allc, alld, grudger) over 10 rounds give the published matrix", same, "20 -10 20 / 30 0 3 / 20 -1 20");

            var rep = new Replicator(m, 50000);
            int gens = cfg.GenerationsOr(1000);
            var s = LabMath.RunReplicator(rep, Replicator.SharesPermille(780, 20, 200), gens);
            res.Strategies = new[] { "Sucker", "Cheat", "Grudger" };
            res.SharesPerGeneration = s;
            res.SeriesTitle = "replicator from S 0.78, C 0.02, G 0.20, w0 = 50, no mutation";
            int suckerFall = LabMath.FirstBelow(s, 0, Fx.FromPermille(600));
            int peakGen = 0;
            for (int g = 1; g <= gens; g++) if (s[g][1] > s[peakGen][1]) peakGen = g;
            long peak = s[peakGen][1];
            int cheatGone = -1;
            for (int g = peakGen; g <= gens; g++) if (s[g][1] < Fx.FromPermille(10)) { cheatGone = g; break; }
            ck.Between("generation at which Suckers fall below 0.6", suckerFall * 1000000L, 20000000, 60000000, 0, note: "spec: by about generation 40");
            ck.Between("Cheat peak share", LabMath.Micro(peak), 250000, 400000, note: "proto 0.315 at 60");
            ck.Between("generation of the Cheat peak", peakGen * 1000000L, 40000000, 90000000, 0);
            ck.Between("generation at which Cheats fall below 0.01", cheatGone * 1000000L, 0, 150000000, 0);
            ck.Near("final Grudger share", LabMath.Micro(s[gens][2]), 700000, 50000);
            ck.Near("final Sucker share (ties with Grudger and drifts)", LabMath.Micro(s[gens][0]), 300000, 50000);
            ck.True("order of events: Suckers fall, then Cheats peak, then Cheats collapse", suckerFall >= 0 && suckerFall <= peakGen && peakGen < cheatGone, "fall < peak < collapse");

            // bistability: Grudger against Cheat only
            var gc = new long[,] { { m[2, 2], m[2, 1] }, { m[1, 2], m[1, 1] } };
            var rep2 = new Replicator(gc, 50000);
            var f04 = LabMath.Final(rep2, Replicator.SharesPermille(40, 960), 3000);
            ck.Above("Grudger 0.04 against Cheats: final Cheat share", LabMath.Micro(f04[1]), 990000);
            var f07 = LabMath.Final(rep2, Replicator.SharesPermille(70, 930), 3000);
            ck.Above("Grudger 0.07 against Cheats: final Grudger share", LabMath.Micro(f07[0]), 990000);
            long lo = Fx.FromPermille(10), hi = Fx.FromPermille(200);
            for (int it = 0; it < 24; it++)
            {
                long mid = (lo + hi) / 2;
                var f = LabMath.Final(rep2, Replicator.SharesFx(mid, Fx.One - mid), 3000);
                if (f[0] > Fx.Half) hi = mid; else lo = mid;
            }
            ck.Near("critical Grudger share against Cheats, g* = c/((n-1)(b-c)) = 1/18", LabMath.Micro((lo + hi) / 2), 55556, 3000, 4);
            notes.Add("A Sucker ties with a Grudger (20 = 20), so Suckers survive and drift; over a very long run a Cheat seed can return while Suckers are many (a finding, not a bug).");
        }
    }
}
