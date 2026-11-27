using System.Collections.Generic;
using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>F0.12: the social module's state (M5). Null unless M5 starts it, so every older game keeps its hash. OWNER: M5.</summary>
    public sealed partial class GodState
    {
        /// <summary>Social counters, deal traces and contests (M5); null when the module is not running.</summary>
        public Social.SocialState Social;
    }
}

namespace God.Sim.Social
{
    /// <summary>How a deal ended, as the telemetry sees it (ECONOMY 3.6). The minds keep their own beliefs about it.</summary>
    public enum DealOutcome : byte { Open = 0, Completed = 1, Defected = 2, Partial = 3, Abandoned = 4, Refused = 5 }

    /// <summary>
    /// The telemetry record of one deal (ECONOMY 3.6 <c>DealTrace</c>): who, the terms as each side meant them, what
    /// passed each way, the outcome. <b>Telemetry only</b>: no rule, mind or policy reads it to decide anything (reflection
    /// test M5.10); the minds remember their own <see cref="DealMemory"/>. It links the hand-overs for the warehouse.
    /// </summary>
    public sealed class DealTrace
    {
        public int DealId;
        /// <summary>A opened it (the first offer); B is the one addressed.</summary>
        public int A, B;
        public DealKind Kind;
        public int OpenedTick, LastStepTick, MessageId;
        /// <summary>The terms as meant by their sayer (A's give and A's want, last offer).</summary>
        public Ware AWare, BWare;
        public int ACount, BCount;
        public int AGiven, BGiven, Steps;
        public bool Agreed;
        public DealOutcome Outcome;
        /// <summary>Each side's believed worth gain at closing (filled from each mind's own valuation, telemetry).</summary>
        public int AGain, BGain;
    }

    /// <summary>
    /// The social module's state (SHARED_CONTRACTS 9.3): public fields, ascending-id lists, no dictionaries. OWNER: M5. Its
    /// hash part ("social") is appended only while <c>GodState.Social</c> is non-null. The path searcher and the scratch
    /// stream are caches, never saved.
    /// </summary>
    public sealed class SocialState
    {
        public int NextMessageId = 1, NextDealId = 1, NextContestId = 1;
        /// <summary>The last sim day whose daily pass ran (-1 none).</summary>
        public int DayDone = -1;
        /// <summary>Deals not yet closed, ascending DealId (telemetry only).</summary>
        public List<DealTrace> Deals = new List<DealTrace>();
        /// <summary>Counters for reports (not read by any rule).</summary>
        public long Gestures, Decoded, Misread;

        [JsonIgnore] internal Population.NodePather Pather;
        [JsonIgnore] internal readonly Rng Scratch = new Rng();
        [JsonIgnore] internal readonly int[] Near = new int[2 * Cognition.ThinkScratch.MaxPeople];

        public DealTrace Trace(int dealId)
        {
            int lo = 0, hi = Deals.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Deals[mid].DealId;
                if (v == dealId) return Deals[mid];
                if (v < dealId) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(NextMessageId); h.Add(NextDealId); h.Add(NextContestId); h.Add(DayDone);
            h.Add(Deals.Count);
            foreach (var d in Deals)
            {
                h.Add(d.DealId); h.Add(d.A); h.Add(d.B); h.Add((int)d.Kind | ((int)d.Outcome << 8) | (d.Agreed ? 1 << 16 : 0));
                h.Add(d.OpenedTick); h.Add(d.LastStepTick); h.Add(d.MessageId);
                h.Add((int)d.AWare | ((int)d.BWare << 8)); h.Add(d.ACount); h.Add(d.BCount); h.Add(d.AGiven); h.Add(d.BGiven); h.Add(d.Steps);
                h.Add(d.AGain); h.Add(d.BGain);
            }
            h.Add(Gestures); h.Add(Decoded); h.Add(Misread);
        }
    }
}
