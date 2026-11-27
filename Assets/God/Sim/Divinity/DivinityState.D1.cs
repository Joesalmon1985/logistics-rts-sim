using System.Collections.Generic;
using God.Sim.Information;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D1's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D1). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>Contracts 2: who drives a god. <see cref="Ai"/> gods are seated by the god director with their <c>PolicyName</c>.</summary>
    public enum GodSeat : byte { None = 0, Human = 1, Ai = 2 }

    /// <summary>
    /// Contracts 2: one persistent god (fields fixed by the contracts). Created once by <see cref="DivinityModule.StartGame"/>,
    /// never removed, merged or re-id'd (D5.4, D5.5). Nothing about it depends on its origin faction existing (G2).
    /// </summary>
    public sealed class GodRecord
    {
        /// <summary>GodId = (int)faction + 1.</summary>
        public int Id;
        /// <summary>Deterministic from the setup (<see cref="Names.God"/>); never player text in the sim.</summary>
        public string Name;
        /// <summary>History only: the faction it was created for (D5.3). Never an authority.</summary>
        public Faction OriginFaction;
        /// <summary>Who drives it (contracts 12); changed only by <c>SwitchGod</c> and <c>SetGodPolicy</c>.</summary>
        public GodSeat Seat;
        /// <summary>The <c>policies.god</c> name the god director seats it with while <see cref="Seat"/> is <c>Ai</c>.</summary>
        public string PolicyName;
        /// <summary>Milli-Mythic (contracts 5): <c>0 &lt;= Mythic &lt;= MythicMax</c> (M1).</summary>
        public long Mythic, MythicMax;
        /// <summary>Per <see cref="InterventionKind"/> (index), the first tick a spend of that kind is allowed again.</summary>
        public long[] CooldownUntil = new long[Gods.KindCount];
        /// <summary>0 until the first manifestation (D5), then <c>AvatarBodyId</c>.</summary>
        public int AvatarBody;
        /// <summary>Contracts 9: how this god observes (only <c>Omniscient</c> in this run).</summary>
        public DivineObservationProfile Profile = new DivineObservationProfile();
        public long CreatedTick;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(Name ?? ""); h.Add((int)OriginFaction | ((int)Seat << 8)); h.Add(PolicyName ?? "");
            h.Add(Mythic); h.Add(MythicMax);
            int n = CooldownUntil?.Length ?? 0;
            h.Add(n);
            for (int i = 0; i < n; i++) h.Add(CooldownUntil[i]);
            h.Add(AvatarBody);
            h.Add(Profile == null ? -1 : (int)Profile.Default | ((int)Profile.Channels << 8));
            h.Add(CreatedTick);
        }
    }

    /// <summary>
    /// D1: one god's Mythic movements of the current day (state, so a mid-day save continues exactly). Reported at the next
    /// day start as <c>MythicUpkeepCharged</c> (per spell kind) and <c>MythicBalanceDay</c>, then cleared.
    /// </summary>
    public sealed class GodDay
    {
        public long Credited, Capped, Spent, Waived;
        public int Actions;
        /// <summary>Per <see cref="InterventionKind"/>: ticks of upkeep, milli charged, milli waived (Unlimited).</summary>
        public int[] UpkeepTicks = new int[Gods.KindCount];
        public long[] Upkeep = new long[Gods.KindCount], UpkeepWaived = new long[Gods.KindCount];

        public void Clear()
        {
            Credited = Capped = Spent = Waived = 0; Actions = 0;
            for (int i = 0; i < Gods.KindCount; i++) { UpkeepTicks[i] = 0; Upkeep[i] = 0; UpkeepWaived[i] = 0; }
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Credited); h.Add(Capped); h.Add(Spent); h.Add(Waived); h.Add(Actions);
            for (int i = 0; i < Gods.KindCount; i++) { h.Add(UpkeepTicks[i]); h.Add(Upkeep[i]); h.Add(UpkeepWaived[i]); }
        }
    }

    /// <summary>
    /// Contracts 2: the god registry (OWNER: D1). <see cref="Gods"/> sorted by Id, count fixed at StartGame (G1). Besides the
    /// contract's list, D1 keeps the current Mythic mode (set from <c>Divinity.Mode</c> at StartGame, changed only by
    /// <c>SetMythicMode</c>), the day bookkeeping of <c>DivinitySystem</c> and the G4 flag.
    /// </summary>
    public sealed partial class GodRegistry
    {
        public List<GodRecord> Gods = new List<GodRecord>();
        /// <summary>Per god (same index as <see cref="Gods"/>): today's Mythic movements.</summary>
        public List<GodDay> Days = new List<GodDay>();
        /// <summary>The current mode (contracts 5). Every rule reads this, never the tuning.</summary>
        public DivinityMode Mode;
        /// <summary>G4: <c>GodCreated</c> has been emitted (on the first divinity tick).</summary>
        public bool Announced;
        /// <summary>The last day whose day-start work ran.</summary>
        public long DayDone;

        /// <summary>The god's record, or null (GodId 1..N is index Id - 1).</summary>
        public GodRecord Of(int god) => god >= 1 && god <= Gods.Count ? Gods[god - 1] : null;

        public GodDay DayOf(int god) => god >= 1 && god <= Days.Count ? Days[god - 1] : null;

        partial void Hash(ref StateHasher h)
        {
            h.Add(Gods.Count);
            foreach (var g in Gods) g.AddTo(ref h);
            h.Add(Days.Count);
            foreach (var d in Days) d.AddTo(ref h);
            h.Add((int)Mode | (Announced ? 1 << 8 : 0));
            h.Add(DayDone);
        }
    }

    /// <summary>D1: one accepted divine action (one per InterventionId), as opened by <see cref="Interventions.Open"/>.</summary>
    public struct InterventionRecord
    {
        public int Id, God;
        public InterventionKind Kind;
        public DivineAct Act;
        public DivineTarget Target;
        public DivineScope Scope;
        /// <summary>Nominal cost (milli-Mythic) and what was actually charged (0 in <c>Unlimited</c>).</summary>
        public long Cost, Charged;
        public long Tick;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(God); h.Add((int)Kind | ((int)Act << 8) | ((int)Scope << 16)); Target.AddTo(ref h);
            h.Add(Cost); h.Add(Charged); h.Add(Tick);
        }
    }

    /// <summary>Accepted interventions, ascending Id (OWNER: D1). Packages keep their own status records keyed by the id.</summary>
    public sealed partial class InterventionRegistry
    {
        public List<InterventionRecord> Items = new List<InterventionRecord>();

        /// <summary>The record of an intervention (binary search; false when unknown).</summary>
        public bool TryGet(int id, out InterventionRecord r)
        {
            int lo = 0, hi = Items.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Items[mid].Id;
                if (v == id) { r = Items[mid]; return true; }
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            r = default;
            return false;
        }

        partial void Hash(ref StateHasher h)
        {
            h.Add(Items.Count);
            foreach (var r in Items) r.AddTo(ref h);
        }
    }
}
