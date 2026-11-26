using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L4. Resident/intruder (PDF 100 to 103). STRATEGY_BENCHMARKS 4.6 L4; ACCEPTANCE M2.6. L4b (optional) is not built.</summary>
    public sealed class L04ResidentIntruder : LabExperiment
    {
        public override string Id => "L4";
        public override string Title => "Resident/intruder (Bourgeois)";
        public override string Source => "PDF 100-103";
        public override ReproStatus Status => ReproStatus.Derived;
        public override string StatusText => "Derived: the matrix is derived from L1's book payoffs with random roles (the book describes Bourgeois qualitatively); L4b (invented) is optional and not built";
        public override string ExpectedSummary => "B vs A boundary 0.5 +/- 0.01 (B .51 -> B > .99; B .49 -> A > .99); each stable alone; H/D mix resists B at .05/.2/.4; B ends 0.9 +/- 0.05 from 0.6; Bourgeois vs Bourgeois 25 -> -25 when both think they are resident";

        /// <summary>B share b; the rest split 0.583 : 0.417 between Hawk and Dove.</summary>
        private static long[] Start(long b)
        {
            long rest = Fx.One - b;
            long h = Fx.MulDiv(rest, 580, 1000);
            return Replicator.SharesFx(h, rest - h, b);
        }

        private static bool TakesOver(Replicator rep, long b, int gens) => LabMath.Final(rep, Start(b), gens)[2] > Fx.Half;

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            var exact = L02Retaliator.Exact(L02Retaliator.Hdba(), true); // H, D, B, A; equals the published table (L2b)
            int gens = cfg.GenerationsOr(20000);
            long mu = Replicator.MuQ20(5, 10000); // 0.0005
            // ---- Bourgeois against Anti-Bourgeois only ----
            var ba = new long[,] { { exact[2, 2], exact[2, 3] }, { exact[3, 2], exact[3, 3] } };
            var rep2 = new Replicator(ba, 500000, mu);
            var s51 = LabMath.RunReplicator(rep2, Replicator.SharesPermille(510, 490), gens);
            res.Strategies = new[] { "Bourgeois", "Anti-Bourgeois" };
            res.SharesPerGeneration = LabMath.Thin(s51, 20);
            res.SeriesTitle = "replicator B vs A from B 0.51, mu 0.0005, every 20th generation";
            ck.Above("B vs A from B 0.51: final Bourgeois share", LabMath.Micro(s51[gens][0]), 990000);
            var s49 = LabMath.Final(rep2, Replicator.SharesPermille(490, 510), gens);
            ck.Above("B vs A from B 0.49: final Anti-Bourgeois share", LabMath.Micro(s49[1]), 990000);
            // boundary by bisection on the start share
            long lo = Fx.FromPermille(400), hi = Fx.FromPermille(600);
            for (int it = 0; it < 24; it++)
            {
                long mid = (lo + hi) / 2;
                var f = LabMath.Final(rep2, Replicator.SharesFx(mid, Fx.One - mid), 3000);
                if (f[0] > Fx.Half) hi = mid; else lo = mid;
            }
            ck.Near("B vs A boundary (start share of B that tips the population)", LabMath.Micro((lo + hi) / 2), 500000, 10000);
            var bAlone = LabMath.Final(rep2, Replicator.SharesPermille(990, 10), gens);
            ck.Above("all-Bourgeois is stable: a 0.01 Anti-Bourgeois share loses (final B)", LabMath.Micro(bAlone[0]), 990000);
            var aAlone = LabMath.Final(rep2, Replicator.SharesPermille(10, 990), gens);
            ck.Above("all-Anti-Bourgeois is stable: a 0.01 Bourgeois share loses (final A)", LabMath.Micro(aAlone[1]), 990000);

            // ---- a Hawk/Dove mix at its ESS against Bourgeois (H, D, B) ----
            var hdb = new long[3, 3];
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) hdb[i, j] = exact[i, j];
            var rep3 = new Replicator(hdb, 500000, mu);
            long payB = (583 * hdb[2, 0] + 417 * hdb[2, 1]) / 1000;
            ck.Report("Bourgeois payoff against the 0.583/0.417 Hawk/Dove mix", LabMath.MicroX1000(payB), "3.1 (below 6.25)");
            // the boundary: the smallest starting B share (H:D at 0.583:0.417 for the rest) from which B takes over
            long blo = Fx.FromPermille(10), bhi = Fx.FromPermille(600);
            for (int it = 0; it < 20; it++)
            {
                long mid = (blo + bhi) / 2;
                if (TakesOver(rep3, mid, gens)) bhi = mid; else blo = mid;
            }
            long boundary = (blo + bhi) / 2;
            ck.Report("H/D/B: boundary start share above which Bourgeois takes over", LabMath.Micro(boundary), "published: between 0.4 and 0.6");
            foreach (int b in new[] { 50, 200 })
            {
                var f = LabMath.Final(rep3, Start(Fx.FromPermille(b)), gens);
                ck.Below("H/D mix resists Bourgeois from B " + Fx.FormatScaled(b, 3) + ": final B share below its start", LabMath.Micro(f[2]), b * 1000L, note: "proto: ends near 0.02");
            }
            var f4 = LabMath.Final(rep3, Start(Fx.FromPermille(400)), gens);
            ck.Above("H/D mix from B 0.400: Bourgeois takes over (final B share)", LabMath.Micro(f4[2]), 400000, kind: CheckKind.Amended,
                note: "Amended at M2. Published: the mix resists B at 0.4 (proto: B ends near 0.02) and the boundary lies between 0.4 and 0.6. On the derived matrix (which equals the published table) the replicator's boundary is " + Fx.Format(boundary, 3)
                    + ": at B = 0.4 (H 0.35, D 0.25) Bourgeois earns 11.9 against a population mean of 11.25 and grows. Corrected expectation: from 0.4 Bourgeois takes over; the boundary is below 0.4.");
            ck.Between("H/D/B boundary (corrected: between 0.2 and 0.4)", LabMath.Micro(boundary), 200000, 400000, kind: CheckKind.Amended, note: "Amended at M2: published 'between 0.4 and 0.6'; see the previous line.");
            var f6 = LabMath.Final(rep3, Start(Fx.FromPermille(600)), gens);
            ck.Near("B from 0.6 takes over: final B share", LabMath.Micro(f6[2]), 900000, 50000, note: "the rest is Doves, which tie with Bourgeois once Hawks are gone");

            // ---- both sides think they are resident ----
            var r = ContestRules.Lab;
            long normal = exact[2, 2];
            long confused = 0;
            for (int coin = 0; coin < 2; coin++) confused += Contest.Resolve(ContestKind.Bourgeois, ContestKind.Bourgeois, Role.Resident, Role.Resident, false, false, coin == 0, r).PayA * 1000L;
            confused /= 2;
            ck.Add("Bourgeois against Bourgeois: 25 with clear roles, -25 when both think they are resident", LabMath.MicroX1000(confused),
                "-25 (from " + Fx.FormatScaled(normal / 100, 1) + ")", normal == 25000 && confused == -25000, decimals: 1);

            notes.Add("Replicator with w0 = 500 and mutation 0.0005; the matrix is the turn rules' exact expectation with random roles, which equals the published table (L2b).");
            notes.Add("L4b (paradoxical strategy, optional in the spec) is not built at M2.");
        }
    }
}
