using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>
    /// L8 and L8b. Axelrod-like round robin (PDF 231 to 239) and the known last round; plus the dollar Prisoner's Dilemma
    /// preset (PDF 226). STRATEGY_BENCHMARKS 4.3, 4.6 L8, test 9; ACCEPTANCE M2.2, M2.10.
    /// </summary>
    public sealed class L08RoundRobin : LabExperiment
    {
        public override string Id => "L8";
        public override string Title => "Axelrod-like round robin (L8b known last round; dollar PD preset)";
        public override string Source => "PDF 226, 231-239";
        public override ReproStatus Status => ReproStatus.ExactPayoffsCorrectedOrDifferent;
        public override string StatusText => "Exact payoffs, different tournament: the payoffs (5, 3, 1, 0) are the book's; the entrants are our 12 strategies, not Axelrod's 63, so scores and ranks are not compared with his figures. The dollar preset (PDF 226) is Exact and checks only the payoff ordering";
        public override string ExpectedSummary => "30 seeds: every score within 15 of the 4.6 table; TFT and TF2T top 4; AllD bottom 2; >= 4 of top 5 nice; TFT 600 against every nice strategy, never leads a deterministic partner by more than 5; TFT vs Naive Prober 253 +/- 50, vs Remorseful 490 +/- 50; L8b TFT-End 602 vs 597, no gain at unknown length";

        public static RepeatedKind[] Entrants() => new[]
        {
            RepeatedKind.Tft, RepeatedKind.Tf2t, RepeatedKind.Stft, RepeatedKind.AllD, RepeatedKind.AllC, RepeatedKind.Random,
            RepeatedKind.Grudger, RepeatedKind.NaiveProber, RepeatedKind.RemorsefulProber, RepeatedKind.Pavlov, RepeatedKind.Tester, RepeatedKind.Sneak,
        };

        /// <summary>The 4.6 table (proto, mean of 30 seeds), in entrant order.</summary>
        public static long[] ProtoScores() => new long[] { 493, 508, 387, 336, 489, 431, 445, 349, 428, 474, 484, 400 };

        /// <summary>M2 values (x10) for the entries whose published score is more than 15 away from the state machines' result
        /// (measured at M2, seed 1, replicates 1-30); 0 = the published value stands.</summary>
        public static long[] M2Scores() => new long[] { 0, 0, 4065, 0, 0, 0, 4138, 3320, 0, 0, 4557, 4192 };

        /// <summary>The L8 round robin, also used by L9.</summary>
        public static Tournament RunTournament(ulong seed, int replicates)
        {
            var t = new Tournament { Kinds = Entrants(), Rounds = 200, Pay = PdPayoffs.Axelrod };
            t.Run(seed, "L8", replicates);
            return t;
        }

        protected override void Execute(LabConfig cfg, LabResult res, CheckList ck, List<string> notes)
        {
            // ---- dollar preset (test 9) ----
            var d = PdPayoffs.Dollars;
            ck.True("dollar payoffs (PDF 226): T > R > P > S and (T + S)/2 < R", d.IsPrisonersDilemma, "true", status: ReproStatus.Exact, source: "test 9");
            var broken = new PdPayoffs(700, 300, -10, -100); // (T + S)/2 = 300 = R
            ck.True("changing the dollar payoffs to (T + S)/2 >= R fails the check", !broken.IsPrisonersDilemma, "the check fails", status: ReproStatus.Exact, source: "test 9");

            int reps = cfg.ParamInt("replicates", 30);
            var t = RunTournament(cfg.Seed, reps);
            var kinds = t.Kinds;
            int k = kinds.Length;
            var proto = ProtoScores();
            res.Strategies = new string[k];
            var scores = new long[k];
            for (int i = 0; i < k; i++) { res.Strategies[i] = RepeatedRules.Name(kinds[i]); scores[i] = t.TotalX1000[i]; }
            res.SharesPerGeneration = new long[0][];
            res.MeanPayoffX1000 = new[] { scores };
            res.SeriesTitle = "mean score per match (200 rounds, " + reps + " seeds), one row";
            var m2 = M2Scores();
            for (int i = 0; i < k; i++)
            {
                if (m2[i] == 0)
                    ck.Near("score: " + RepeatedRules.Name(kinds[i]), LabMath.MicroX1000(t.TotalX1000[i]), proto[i] * 1000000, 15000000, 1);
                else
                    ck.Near("score: " + RepeatedRules.Name(kinds[i]) + " (published " + proto[i] + ")", LabMath.MicroX1000(t.TotalX1000[i]), m2[i] * 100000, 15000000, 1, CheckKind.Amended,
                        "Amended at M2. The published (proto) score " + proto[i] + " is more than 15 from what the 4.3 state machines give (" + Fx.FormatScaled(m2[i], 1) + " at M2, seeds 1-30). "
                        + "Evidence for Grudger: its deterministic entries are fixed by the rules (600 x 5 nice, AllD 199, STFT 203, Tester 203, Sneak 301), so even 600 against Random and both probers caps it at 475, and 445 needs about 478 against each, while a q = 0.1 prober triggers the grudge within about 10 rounds (about 230). "
                        + "The other amended entries (STFT, Naive Prober, Tester, Sneak) follow from the same pairs. Tester is the literal 4.3 row (round 2 always C); the Axelrod-style reading (apology on round 3) was tried and moves only Tester (475). Corrected expectation: the M2 value +/- 15.");
            }
            var rank = t.Ranking();
            int Pos(RepeatedKind kk) { for (int r = 0; r < k; r++) if (kinds[rank[r]] == kk) return r + 1; return -1; }
            var order = new List<string>();
            for (int r = 0; r < k; r++) order.Add((r + 1) + " " + RepeatedRules.Name(kinds[rank[r]]));
            ck.Report("ranking", 0, string.Join(", ", order), 0);
            ck.True("Tit for Tat and Tit for Two Tats both in the top 4", Pos(RepeatedKind.Tft) <= 4 && Pos(RepeatedKind.Tf2t) <= 4, "TFT #" + Pos(RepeatedKind.Tft) + ", TF2T #" + Pos(RepeatedKind.Tf2t));
            ck.True("Always Defect in the bottom 2", Pos(RepeatedKind.AllD) >= k - 1, "AllD #" + Pos(RepeatedKind.AllD));
            int nice5 = 0;
            for (int r = 0; r < 5; r++) if (RepeatedRules.IsNice(kinds[rank[r]])) nice5++;
            ck.Add("nice strategies in the top 5", nice5 * 1000000L, ">= 4", nice5 >= 4, decimals: 0, note: "our set has one nasty strategy (Tester) near the top: it feeds on exploitable nice players (AllC, TF2T)");
            int tft = 0;
            long minNice = long.MaxValue, maxLead = long.MinValue;
            for (int j = 0; j < k; j++)
            {
                if (RepeatedRules.IsNice(kinds[j]) && t.MinScoresX1000[tft, j] < minNice) minNice = t.MinScoresX1000[tft, j];
                if (j != tft && RepeatedRules.IsDeterministic(kinds[j]))
                {
                    // the largest lead in any replicate equals the mean lead for deterministic pairs
                    if (t.MeanLeadX1000[tft, j] > maxLead) maxLead = t.MeanLeadX1000[tft, j];
                }
            }
            ck.Add("Tit for Tat's lowest score against a nice strategy", LabMath.MicroX1000(minNice), "600", minNice == 600000, decimals: 0);
            ck.AtMost("Tit for Tat's largest lead over a deterministic partner (not envious)", LabMath.MicroX1000(maxLead), 5000000, 0);
            ck.Near("Tit for Tat against Naive Prober (echo lock-in)", LabMath.MicroX1000(t.MeanScoresX1000[tft, 7]), 253000000, 50000000, 1);
            ck.Near("Tit for Tat against Remorseful Prober (remorse breaks the echo)", LabMath.MicroX1000(t.MeanScoresX1000[tft, 8]), 490000000, 50000000, 1);

            // ---- L8b: the known last round ----
            RepeatedMatch.Play(RepeatedKind.TftEnd, 0, RepeatedKind.Tft, 0, 200, PdPayoffs.Axelrod, null, null, out long end, out long plain, 200, 0);
            ck.Add("L8b: TFT-End against TFT over a known 200 rounds", end * 1000000, "602 against 597", end == 602 && plain == 597, decimals: 0, source: "L8b");
            ck.Report("L8b: TFT's score in that match", plain * 1000000, "597", 0, source: "L8b");
            // unknown length: geometric with w = 0.995, 200 seeds. The length is hidden, so TFT-End never knows the final round.
            var geo = new GeometricLength(995);
            long gainHidden = 0, gainGuess = 0;
            int seeds = cfg.ParamInt("l8b-seeds", 200);
            for (int s = 0; s < seeds; s++)
            {
                var gr = LabRandom.Root(cfg.Seed + (ulong)s, Id, "l8b-length");
                int len = geo.Draw(gr);
                RepeatedMatch.Play(RepeatedKind.Tft, 0, RepeatedKind.Tft, 0, len, PdPayoffs.Axelrod, null, null, out long base1, out _);
                RepeatedMatch.Play(RepeatedKind.TftEnd, 0, RepeatedKind.Tft, 0, len, PdPayoffs.Axelrod, null, null, out long hid, out _, 0, 0);
                RepeatedMatch.Play(RepeatedKind.TftEnd, 0, RepeatedKind.Tft, 0, len, PdPayoffs.Axelrod, null, null, out long guess, out _, 200, 0);
                gainHidden += hid - base1;
                gainGuess += guess - base1;
            }
            ck.Near("L8b: unknown length (w = 0.995): TFT-End's mean gain over TFT (length hidden)", LabMath.MicroRatio(gainHidden, seeds), 0, 3000000, 2, source: "L8b",
                note: "With the length hidden TFT-End cannot know the final round and plays as TFT, so the gain is 0 by construction; the next line shows what a guessed final round costs.");
            ck.AtMost("L8b: unknown length: a TFT-End that guesses round 200 has no systematic advantage (mean gain)", LabMath.MicroRatio(gainGuess, seeds), 3000000, 2, CheckKind.Amended, source: "L8b",
                note: "Amended at M2 (made concrete). Published: 'no systematic advantage (mean gain within +/- 3)'. A TFT-End that defects on a guessed round 200 starts an echo when the match runs on and loses on average (expected about -36), so the check is one-sided: the gain is at most +3.");

            // ---- reported: 1 per cent execution noise (open question 4) ----
            var noisy = new Tournament { Kinds = Entrants(), Rounds = 200, Pay = PdPayoffs.Axelrod, NoisePermille = 10 };
            noisy.Run(cfg.Seed, "L8-noise", 10);
            var nr = noisy.Ranking();
            var no = new List<string>();
            for (int r = 0; r < 5; r++) no.Add((r + 1) + " " + RepeatedRules.Name(kinds[nr[r]]) + " " + Fx.FormatScaled(noisy.TotalX1000[nr[r]], 0));
            ck.Report("1% noise (10 seeds): top 5", 0, string.Join(", ", no), 0);
            for (int i = 0; i < k; i++)
            {
                var row = new List<string>();
                for (int j = 0; j < k; j++) row.Add(Fx.FormatScaled(t.MeanScoresX1000[i, j] / 100, 1));
                notes.Add("score matrix row " + RepeatedRules.Name(kinds[i]) + " (columns in entrant order): " + string.Join(" ", row));
            }
            notes.Add("Scores are the mean over the 12 opponents (self included) of the score per 200-round match; 600 is the maximum.");
            notes.Add("Our scores are for our entrants only and are not compared with Axelrod's (we do not have his 63 entries).");
        }
    }
}
