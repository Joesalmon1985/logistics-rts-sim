using System.Collections.Generic;

namespace God.Sim.Lab
{
    /// <summary>A move in an exchange or help game (Prisoner's Dilemma family).</summary>
    public enum Move : byte { Cooperate = 0, Defect = 1 }

    /// <summary>A move in a contest (STRATEGY_BENCHMARKS 4.2).</summary>
    public enum CMove : byte { Display, Escalate, Withdraw }

    /// <summary>A contest role tag (resident/intruder games); None elsewhere.</summary>
    public enum Role : byte { None, Resident, Intruder }

    public enum FemaleStrategy : byte { Coy, Fast }
    public enum MaleStrategy : byte { Faithful, Philanderer }
    public enum HousingStrategy : byte { Dig, Enter }

    /// <summary>
    /// The reproduction-status labels (STRATEGY_BENCHMARKS "Reproduction status", owner rule 27 Nov 2026). Only
    /// <see cref="Exact"/> may call itself a reproduction.
    /// </summary>
    public enum ReproStatus : byte
    {
        /// <summary>The book gives every payoff and parameter used, and the result checked is the book's.</summary>
        Exact,
        /// <summary>The payoffs are the book's, but the result checked is a corrected statement or a different setting.</summary>
        ExactPayoffsCorrectedOrDifferent,
        /// <summary>Payoffs computed by us from the book's own numbers, or results derived analytically from them.</summary>
        Derived,
        /// <summary>Payoffs or parameters chosen by us because the book gives none.</summary>
        Invented,
    }

    public static class ReproStatusText
    {
        public static string Code(ReproStatus s)
        {
            switch (s)
            {
                case ReproStatus.Exact: return "exact";
                case ReproStatus.ExactPayoffsCorrectedOrDifferent: return "exact-payoffs-corrected-or-different";
                case ReproStatus.Derived: return "derived";
                default: return "invented";
            }
        }

        public static string Label(ReproStatus s)
        {
            switch (s)
            {
                case ReproStatus.Exact: return "Exact";
                case ReproStatus.ExactPayoffsCorrectedOrDifferent: return "Exact payoffs, corrected or different result";
                case ReproStatus.Derived: return "Derived";
                default: return "Invented";
            }
        }
    }

    /// <summary>What kind of line a check is.</summary>
    public enum CheckKind : byte
    {
        /// <summary>A pass criterion from the spec, unchanged.</summary>
        Check,
        /// <summary>A pass criterion whose expectation was corrected at M2 with recorded evidence (the note says why).</summary>
        Amended,
        /// <summary>Reported only (calibration, exploratory, or a proto number without a tolerance); never fails a run.</summary>
        Report,
    }

    /// <summary>One check: measured value (micro units, i.e. millionths, so it is an exact integer), expectation, pass.</summary>
    public sealed class Check
    {
        public string Name;
        public long MeasuredMicro;
        /// <summary>How many decimals to show for the measured value.</summary>
        public int Decimals = 3;
        public string Expected;
        public bool Pass;
        public CheckKind Kind;
        public ReproStatus Status;
        /// <summary>The spec row the check comes from ("L1", "test 10", "ACCEPTANCE M2.3").</summary>
        public string Source;
        public string Note;

        public string Measured => Fx.FormatScaled(Round(MeasuredMicro, Decimals), Decimals);

        private static long Round(long micro, int decimals)
        {
            if (decimals >= 6) return micro * Fx.Pow10(decimals - 6);
            long d = Fx.Pow10(6 - decimals);
            return micro >= 0 ? (micro + d / 2) / d : -((-micro + d / 2) / d);
        }

        /// <summary>A failing line that counts: a Check or an Amended check that did not pass.</summary>
        public bool Fails => Kind != CheckKind.Report && !Pass;
    }

    /// <summary>The lab experiment contract (STRATEGY_BENCHMARKS 3). Zero or null means "the experiment's default".</summary>
    public sealed class LabConfig
    {
        public string Experiment;
        public ulong Seed = 1;
        public int Agents;
        public int Generations;
        public int ContestsPerAgent;
        public int BaselineX1000 = -1;
        public int MutationPermille = -1;
        public int MatchRounds;
        public int ContinuePermille;
        /// <summary>"replicator" | "agents" | null (the experiment's default).</summary>
        public string Dynamics;
        /// <summary>Experiment-specific key=value overrides, in the order given.</summary>
        public List<KeyValuePair<string, string>> Params = new List<KeyValuePair<string, string>>();

        public string Param(string key)
        {
            string v = null;
            foreach (var kv in Params) if (kv.Key == key) v = kv.Value;
            return v;
        }

        public int ParamInt(string key, int def)
        {
            string v = Param(key);
            if (v == null) return def;
            if (!int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int r))
                throw new System.ArgumentException("--param " + key + " must be an integer: " + v);
            return r;
        }

        public int AgentsOr(int def) => Agents > 0 ? Agents : def;
        public int GenerationsOr(int def) => Generations > 0 ? Generations : def;
        public int BaselineOr(int def) => BaselineX1000 >= 0 ? BaselineX1000 : def;
        public int MutationOr(int def) => MutationPermille >= 0 ? MutationPermille : def;
        public int ContestsOr(int def) => ContestsPerAgent > 0 ? ContestsPerAgent : def;

        public LabConfig Copy()
        {
            var c = (LabConfig)MemberwiseClone();
            c.Params = new List<KeyValuePair<string, string>>(Params);
            return c;
        }
    }

    /// <summary>The result of one experiment run.</summary>
    public sealed class LabResult
    {
        public string Experiment;
        public ulong Seed;
        public ReproStatus Status;
        public string StatusText;
        /// <summary>What the main series is ("agents N=1000, Hawk and Dove").</summary>
        public string SeriesTitle;
        public string[] Strategies = new string[0];
        /// <summary>[gen][strategy] in share units (2^30 = 1) for the main series; some experiments store another Q30 quantity, named in <see cref="Strategies"/>.</summary>
        public long[][] SharesPerGeneration = new long[0][];
        /// <summary>[gen][strategy] mean payoff per game, x1000; empty when not meaningful.</summary>
        public long[][] MeanPayoffX1000 = new long[0][];
        public Check[] Checks = new Check[0];
        /// <summary>Free-text report lines (what was run, design notes). Never claims a reproduction unless Status is Exact.</summary>
        public string[] Notes = new string[0];
        public ulong ResultHash;

        public bool Pass
        {
            get { foreach (var c in Checks) if (c.Fails) return false; return true; }
        }
    }
}
