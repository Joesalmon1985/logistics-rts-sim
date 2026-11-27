using System.Collections.Generic;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D3's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D3). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>D3: one accepted miracle (one per intervention of kind Miracle), kept as its record. <see cref="Magnitude"/> is the raw command value.</summary>
    public struct MiracleRecord
    {
        public int Intervention, God;
        public MiracleKind Kind;
        public DivineTarget Target;
        public int Magnitude;
        public long Cost, Tick;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Intervention); h.Add(God); h.Add((int)Kind); Target.AddTo(ref h); h.Add(Magnitude); h.Add(Cost); h.Add(Tick);
        }
    }

    /// <summary>D3: a timed modifier on one person (<see cref="MiracleKind.Bless"/> or <see cref="MiracleKind.Protect"/>); at most one of each per person.</summary>
    public struct Blessing
    {
        public int Npc;
        public MiracleKind Kind;
        public long Until;
        public int God, Intervention;

        public void AddTo(ref StateHasher h) { h.Add(Npc); h.Add((int)Kind); h.Add(Until); h.Add(God); h.Add(Intervention); }
    }

    /// <summary>D3: a mark at a node left by a miracle, for a while (ring of 64; DIVINE_PLAYER 4.8): what U3b draws and D6 may read.</summary>
    public struct DivineSign
    {
        public int Intervention, God;
        public byte Kind;
        public int Node;
        public byte Radius, Loudness;
        public long Until;

        public void AddTo(ref StateHasher h) { h.Add(Intervention); h.Add(God); h.Add(Kind | (Radius << 8) | (Loudness << 16)); h.Add(Node); h.Add(Until); }
    }

    /// <summary>Contracts 6: a consecrated place (a place, never an owner): <c>Id</c> is the InterventionId that made it.</summary>
    public struct ConsecratedPlace
    {
        public int Id, God, Node, Radius;
        public long SinceTick;

        public void AddTo(ref StateHasher h) { h.Add(Id); h.Add(God); h.Add(Node); h.Add(Radius); h.Add(SinceTick); }
    }

    /// <summary>D3: regrowth of a resource source is suspended until <see cref="Until"/> (stock held at or below <see cref="Stock"/>). Keyed by source id.</summary>
    public struct SourceBlight
    {
        public int Source, Stock, Intervention;
        public long Until;

        public void AddTo(ref StateHasher h) { h.Add(Source); h.Add(Stock); h.Add(Intervention); h.Add(Until); }
    }

    /// <summary>
    /// D3 (contracts 7.2, C7): per god per ware the units miracles created and destroyed, <c>[god * Wares.PersonalCount + ware]</c>
    /// (index god 0 unused). Booked only by <see cref="DivineLedger"/>.
    /// </summary>
    public sealed class MiracleLedger
    {
        public long[] Created, Destroyed;

        public MiracleLedger() { }
        public MiracleLedger(int gods) { Created = new long[(gods + 1) * Wares.PersonalCount]; Destroyed = new long[(gods + 1) * Wares.PersonalCount]; }

        public long CreatedOf(Ware w) { long n = 0; for (int i = (int)w; i < Created.Length; i += Wares.PersonalCount) n += Created[i]; return n; }
        public long DestroyedOf(Ware w) { long n = 0; for (int i = (int)w; i < Destroyed.Length; i += Wares.PersonalCount) n += Destroyed[i]; return n; }
        public long CreatedBy(int god, Ware w) => Created[god * Wares.PersonalCount + (int)w];
        public long DestroyedBy(int god, Ware w) => Destroyed[god * Wares.PersonalCount + (int)w];

        public void AddTo(ref StateHasher h)
        {
            h.Add(Created.Length);
            for (int i = 0; i < Created.Length; i++) { h.Add(Created[i]); h.Add(Destroyed[i]); }
        }
    }

    /// <summary>
    /// D3's sub-object (OWNER: D3), <c>DivinityState.Miracles</c>: the miracle records (ascending intervention id), blessings
    /// (ascending (Npc, Kind)), the miracle ledger, consecrated places (ascending id), the sign ring and the source blights
    /// (ascending source id). Created at the first miracle (null before, so a game without one is unchanged).
    /// </summary>
    public sealed partial class MiracleState
    {
        public const int SignRing = 64;

        public List<MiracleRecord> Records = new List<MiracleRecord>();
        public List<Blessing> Blessings = new List<Blessing>();
        public MiracleLedger Ledger;
        public List<ConsecratedPlace> Places = new List<ConsecratedPlace>();
        public List<DivineSign> Signs = new List<DivineSign>();
        public List<SourceBlight> Blights = new List<SourceBlight>();
        /// <summary>The last day whose day-start work (blessing expiry and recovery) ran; -1 before the first.</summary>
        public long DayDone = -1;

        partial void Hash(ref StateHasher h)
        {
            h.Add(Records.Count); foreach (var r in Records) r.AddTo(ref h);
            h.Add(Blessings.Count); foreach (var b in Blessings) b.AddTo(ref h);
            if (Ledger != null) { h.Add(1); Ledger.AddTo(ref h); } else h.Add(0);
            h.Add(Places.Count); foreach (var p in Places) p.AddTo(ref h);
            h.Add(Signs.Count); foreach (var g in Signs) g.AddTo(ref h);
            h.Add(Blights.Count); foreach (var b in Blights) b.AddTo(ref h);
            h.Add(DayDone);
        }

        /// <summary>The index of a blessing of <paramref name="kind"/> on a person (binary search), or the complement of its insertion point.</summary>
        public int IndexOf(int npc, MiracleKind kind)
        {
            int lo = 0, hi = Blessings.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var b = Blessings[mid];
                int c = b.Npc != npc ? b.Npc.CompareTo(npc) : ((int)b.Kind).CompareTo((int)kind);
                if (c == 0) return mid;
                if (c < 0) lo = mid + 1; else hi = mid - 1;
            }
            return ~lo;
        }
    }
}
