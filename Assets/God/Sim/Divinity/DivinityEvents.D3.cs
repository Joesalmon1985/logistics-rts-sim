using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D3's rows of the event catalogue (contracts 15.1, codes 825-829, domains Miracle 63 and Ledger 64). OWNER: D3. 830 is
    /// held for <c>LocationDeconsecrated</c> (LEAD_RULINGS R53; not implemented: nothing deconsecrates in this run).
    /// A changed field meaning takes a new code; an added field bumps the version.
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D3 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(MiracleCast), 825, 1, TelemetryDomain.Miracle, "fact_miracle", note: "one per accepted miracle (CastMiracle)"),
            new EventInfo(typeof(MiracleEffectApplied), 826, 1, TelemetryDomain.Miracle, "fact_miracle_effect", note: "one per affected thing, with before and after; carries the divine attribution of a death"),
            new EventInfo(typeof(ResourceCreated), 827, 1, TelemetryDomain.Ledger, "fact_resource_ledger", note: "exactly one per miracle Goods.Produce (ledger id, ware, count, lot, holder)"),
            new EventInfo(typeof(ResourceDestroyed), 828, 1, TelemetryDomain.Ledger, "fact_resource_ledger", note: "exactly one per miracle Goods.Consume"),
            new EventInfo(typeof(LocationConsecrated), 829, 1, TelemetryDomain.Miracle, "generic", note: "a consecrated place was made (state: DivinityState.Miracles.Places)"));
    }

    /// <summary>What a <see cref="MiracleEffectApplied"/> changed (the <c>ThingKind</c> values).</summary>
    public static class MiracleThing
    {
        public const byte Person = 1, Hut = 2, Source = 3, Lot = 4, Node = 5, Building = 6, Witness = 7, Place = 8, Bin = 9;
    }

    /// <summary>825: a miracle was accepted (after Mythic was spent and the intervention opened). <c>Magnitude</c> is the raw command value.</summary>
    public sealed class MiracleCast : DomainEvent
    {
        public int Intervention, God;
        public MiracleKind Kind;
        public DivineTargetKind TargetKind;
        public int TargetId, TargetNode, TargetRadius;
        public int Magnitude;
        public long Cost;
    }

    /// <summary>
    /// 826: one thing a miracle changed, with the value before and after (<c>Field</c> names it: "injury", "health", "fear",
    /// "illness", "stock", "standing", "blessing", "lot", "place"). <c>Death</c> is set (with <c>Cause</c>) when the change killed a
    /// person: the divine attribution of a death that the sim records as <c>DeathCause.Violence</c> (contracts 20.4).
    /// </summary>
    public sealed class MiracleEffectApplied : DomainEvent
    {
        public int Intervention;
        public byte ThingKind;
        public int ThingId;
        public string Field;
        public long Before, After;
        public bool Death;
        public DeathCause Cause;
    }

    /// <summary>827: goods a miracle created (<c>Lot</c> is the new lot; <c>IntoLot</c> the lot it has where it merged).</summary>
    public sealed class ResourceCreated : DomainEvent
    {
        public int LedgerId, Intervention, God;
        public Ware Ware;
        public int Count, Lot, IntoLot;
        public HolderKind HolderKind;
        public int HolderId, Node;
    }

    /// <summary>828: goods a miracle destroyed.</summary>
    public sealed class ResourceDestroyed : DomainEvent
    {
        public int LedgerId, Intervention, God;
        public Ware Ware;
        public int Count, Lot;
        public HolderKind HolderKind;
        public int HolderId, Node;
    }

    /// <summary>829: a place was consecrated (<c>Intervention</c> is the place's id).</summary>
    public sealed class LocationConsecrated : DomainEvent
    {
        public int Intervention, God, Node, Radius;
    }
}
