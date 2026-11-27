using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>L13. Vampire bats (PDF 253 to 256): exploratory, no numeric expectation. STRATEGY_BENCHMARKS 4.6 L13; ACCEPTANCE M2.13.</summary>
    public sealed class L13VampireBats : LabExperiment
    {
        public override string Id => "L13";
        public override string Title => "Vampire bats (exploratory)";
        public override string Source => "PDF 253-256";
        public override ReproStatus Status => ReproStatus.Invented;
        public override string StatusText => "Invented payoffs, exploratory: the book gives no payoffs; the roost model is ours; nothing here is checked against the book and no numeric expectation is claimed";
        public override string ExpectedSummary => "runs and reports (no numeric expectation at M2)";

        private const int Roost = 20, MaxStore = 6;

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            int nights = cfg.ParamInt("nights", 3000);
            Run(cfg.Seed, "stable", nights, 2, out long sdS, out long ndS, out var series);
            Run(cfg.Seed, "turnover", nights, 100, out long sdT, out long ndT, out _);
            ck.Report("stable roost (turnover 2 permille a night): sharer deaths per 1000 bat-nights", sdS, "exploratory", 3);
            ck.Report("stable roost: non-sharer deaths per 1000 bat-nights", ndS, "exploratory", 3);
            ck.Report("high turnover (100 permille a night): sharer deaths per 1000 bat-nights", sdT, "exploratory", 3);
            ck.Report("high turnover: non-sharer deaths per 1000 bat-nights", ndT, "exploratory", 3);
            ck.True("L13 runs and reports", true, "runs (no numeric expectation)");
            res.Strategies = new[] { "sharer_share_stable" };
            res.SharesPerGeneration = series;
            res.SeriesTitle = "share of sharers in the stable roost, every 10th night";
            notes.Add("Model: 20 bats; each night every bat's store falls by 1 (max 6); it forages with success 500 permille (+3); a bat with store 0 dies and a newcomer of a random type takes its place. A sharer with store >= 4 gives 1 unit to a roostmate with store <= 1 that has not refused it before (it remembers a roostmate that, when able, did not give to it). Non-sharers never give.");
            notes.Add("Turnover: each night each bat leaves with the stated probability and a newcomer (random type, no memory) replaces it.");
            notes.Add("No directional pass criterion yet (STRATEGY_BENCHMARKS open question 2); this is a report only.");
        }

        private static void Run(ulong seed, string part, int nights, int turnoverPermille, out long sharerDeaths, out long otherDeaths, out long[][] series)
        {
            var rng = LabRandom.Root(seed, "L13", part);
            var sharer = new bool[Roost]; var store = new int[Roost];
            var refused = new bool[Roost, Roost]; // refused[i, j]: j refused i when able
            for (int i = 0; i < Roost; i++) { sharer[i] = i % 2 == 0; store[i] = 3; }
            long sd = 0, od = 0, sn = 0, on = 0;
            var s = new List<long[]>();
            for (int night = 0; night < nights; night++)
            {
                int cnt = 0;
                for (int i = 0; i < Roost; i++) if (sharer[i]) cnt++;
                if (night % 10 == 0) s.Add(new[] { Fx.FromRatio(cnt, Roost) });
                for (int i = 0; i < Roost; i++)
                {
                    store[i]--;
                    if (LabRandom.Permille(rng, 500)) store[i] = System.Math.Min(MaxStore, store[i] + 3);
                }
                // sharing: needy bats in ascending id ask the well-fed in ascending id
                for (int need = 0; need < Roost; need++)
                {
                    if (store[need] > 1) continue;
                    for (int giver = 0; giver < Roost; giver++)
                    {
                        if (giver == need || store[giver] < 4) continue;
                        if (sharer[giver] && !refused[giver, need]) { store[giver]--; store[need]++; break; }
                        refused[need, giver] = true; // able and did not give
                    }
                }
                for (int i = 0; i < Roost; i++)
                {
                    if (sharer[i]) sn++; else on++;
                    bool dies = store[i] <= 0;
                    bool leaves = !dies && LabRandom.Permille(rng, turnoverPermille);
                    if (dies) { if (sharer[i]) sd++; else od++; }
                    if (dies || leaves)
                    {
                        sharer[i] = rng.NextInt(2) == 0;
                        store[i] = 3;
                        for (int j = 0; j < Roost; j++) { refused[i, j] = false; refused[j, i] = false; }
                    }
                }
            }
            sharerDeaths = sn > 0 ? Fx.MulDiv(1000000000, sd, sn) : 0;
            otherDeaths = on > 0 ? Fx.MulDiv(1000000000, od, on) : 0;
            series = s.ToArray();
        }
    }
}
