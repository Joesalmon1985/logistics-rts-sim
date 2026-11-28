using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace God.Train.Eval
{
    /// <summary>One measure's value for one arm (thresholds file section 2).</summary>
    public sealed class MeasureResult
    {
        public string Id = "";
        public string Kind = "";
        public bool Evaluable;
        public double Value;
        public int Count, Of;
        public bool Met;
        public bool Gate;
        public string Note = "";
    }

    /// <summary>One seed of one arm.</summary>
    public sealed class SeedResult
    {
        public long Seed; public string Layout = "";
        public int Adults0, AdultsAlive;
        public long PersonTicks, PainTicksHungerThirst;
        public long ShelterTick = -1, RawDepositTick = -1, HandoverTick = -1;
        public long Return, Decisions, TeacherSubmissions;
        public string FinalHash = "";
        /// <summary>Accepted controls per Prim (index 0-12; unified world only).</summary>
        public long[] Applied = new long[13];
        public List<PersonResult> People = new List<PersonResult>();
    }

    /// <summary>One person of one evaluated episode (unified world): survival, cause, end-of-day need pressures (-1 = dead).</summary>
    public sealed class PersonResult
    {
        public int Person; public bool Adult; public long DeathTick = -1; public string Cause = "";
        public long PersonTicks, PainTicksHungerThirst;
        public int CellsVisited;
        public List<int> Hunger = new List<int>(), Energy = new List<int>(), Thirst = new List<int>();
    }

    public sealed class ArmResult
    {
        public string Name = "";
        /// <summary>"untrained", "frozen", "trained" or "fallback" (a fallback is never counted as trained).</summary>
        public string Brain = "";
        public string WeightsSha256 = "";
        public string ArtifactStatus = "";
        public List<SeedResult> PerSeed = new List<SeedResult>();
        public List<MeasureResult> Measures = new List<MeasureResult>();
    }

    public sealed class VerdictResult
    {
        public bool Tr01Passed, UaLearnedGreen;
        public string Headline = "";
        public List<string> FailedGate = new List<string>();
        public List<string> ReportedNotGate = new List<string>();
        public string Recommendation = "";
    }

    /// <summary>
    /// The machine-readable result of one evaluation (schema <c>god-train-eval/1</c>, stable: UN11a maps it into the warehouse).
    /// Every result carries the thresholds hash it was evaluated against.
    /// </summary>
    public sealed class EvalResults
    {
        public string Schema = "god-train-eval/1";
        public string ThresholdsHash = "";
        public int ThresholdsVersion;
        public string World = "";
        public bool SelfTest;
        public string Label = "";
        public bool Invalid;
        public string InvalidReason = "";
        public int PainBand, TicksPerDay, NeedMask, Population, Days;
        public List<long> Seeds = new List<long>();
        public List<string> Gate = new List<string>();
        public List<ArmResult> Arms = new List<ArmResult>();
        public VerdictResult Verdict = new VerdictResult();
        public double RolloutSecondsTotal;
        public double TicksPerSecondPerWorker;

        public ArmResult Arm(string name) { foreach (var a in Arms) if (a.Name == name) return a; return null; }

        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };

        public string ToJson() => JsonSerializer.Serialize(this, Json);
        public static EvalResults FromJson(string s) => JsonSerializer.Deserialize<EvalResults>(s, Json);
        public void Save(string path) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))); File.WriteAllText(path, ToJson().Replace("\r\n", "\n")); }
    }
}
