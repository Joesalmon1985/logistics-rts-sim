using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// C1's rows of the event catalogue (contracts 15.1, block 270-289, domains Ownership and Production). 270-275 are the
    /// contract's; 276 and 277 are allocated by C1 from its spare block (append only) for the property-record API of spec 22
    /// (<see cref="Assets.Transfer"/>, <see cref="Assets.Destroy"/>). OWNER: C1.
    /// </summary>
    public static partial class CivEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> C1 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(AssetBuilt), 270, 1, TelemetryDomain.Ownership, "fact_ownership_spell", note: "a productive asset finished from real materials: opens its first ownership spell (basis Built)"),
            new EventInfo(typeof(AssetUsed), 271, 1, TelemetryDomain.Production, "fact_asset_use", true, note: "one completed work unit on an asset: the output belongs to the asset's owner, the worker is the actor"),
            new EventInfo(typeof(UseRightGranted), 272, 1, TelemetryDomain.Ownership, "fact_use_right", note: "a use right on an asset opened (also an economic relationship holder-owner)"),
            new EventInfo(typeof(UseRightEnded), 273, 1, TelemetryDomain.Ownership, "fact_use_right", note: "a use right closed (ended by the owner, expired, holder died, asset gone)"),
            new EventInfo(typeof(TheftCommitted), 274, 1, TelemetryDomain.Ownership, "fact_theft", note: "analytics-only truth (Full only): goods or an asset used without the true permission; never read by any rule, policy or mind"),
            new EventInfo(typeof(SourceClaimed), 275, 1, TelemetryDomain.Ownership, "fact_source_claim", note: "a first-arrival claim on a source (recorded only; no exclusion rule)"),
            new EventInfo(typeof(AssetTransferred), 276, 1, TelemetryDomain.Ownership, "fact_ownership_spell", note: "an asset's property record changed owner (sale, gift, seizure, inheritance): closes one spell, opens the next"),
            new EventInfo(typeof(AssetDestroyed), 277, 1, TelemetryDomain.Ownership, "fact_ownership_spell", note: "an asset ceased to exist (its store emptied first by Goods): closes its last spell"));
    }

    /// <summary>270: a productive asset was finished (all materials consumed with <c>ConsumeReason.Built</c>, work done).</summary>
    public sealed class AssetBuilt : DomainEvent
    {
        public int Asset, Kind, Node;
        public OwnerKind OwnerKind;
        public int OwnerId;
        public int Builder;
        public OwnershipBasis Basis;
        public int Store;
    }

    /// <summary>271: one work unit completed: the recipe ran. Control 0 own, 1 compelled, 2 possessed, 3 avatar.</summary>
    public sealed class AssetUsed : DomainEvent
    {
        public int Asset, Worker;
        public OwnerKind OwnerKind;
        public int OwnerId;
        public byte Control;
        public Ware OutputWare;
        public int OutputCount;
        public int Intervention;
        /// <summary>The worker's true right (analytics): 0 none (a use theft), 1 owner, 2 household, 3 use right.</summary>
        public byte Right;
    }

    /// <summary>272.</summary>
    public sealed class UseRightGranted : DomainEvent
    {
        public int Asset;
        public OwnerKind HolderKind;
        public int HolderId;
        public UseKind Kind;
        public OwnerKind GrantedByKind;
        public int GrantedById;
        public int UntilDay;
    }

    /// <summary>273. Reason: 1 ended by the grantor, 2 expired, 3 holder died, 4 asset destroyed, 5 asset transferred.</summary>
    public sealed class UseRightEnded : DomainEvent
    {
        public const byte ByGrantor = 1, Expired = 2, HolderDied = 3, AssetGone = 4, Transferred = 5;
        public int Asset;
        public OwnerKind HolderKind;
        public int HolderId;
        public UseKind Kind;
        public byte Reason;
    }

    /// <summary>
    /// 274 (analytics-only truth, Full only): a take of goods (<see cref="Goods"/>) or a use of an asset (<see cref="Use"/>)
    /// without the true permission (<c>Permits.Truth</c>). Classified by the sim after the act; nothing ever reacts to it.
    /// </summary>
    public sealed class TheftCommitted : DomainEvent
    {
        public const byte Goods = 1, Use = 2;
        public byte Kind;
        public int Lot, Asset;
        public Ware Ware;
        public int Count;
        public OwnerKind OwnerKind;
        public int OwnerId;
        public int Taker, Node;
        /// <summary>Where the goods were: the holder kind and id (ground node, bin id).</summary>
        public HolderKind FromKind;
        public int FromHolder;
    }

    /// <summary>275: the first arrival at a source claimed it (ECONOMY 12 land access): recorded, never enforced.</summary>
    public sealed class SourceClaimed : DomainEvent
    {
        public int Source;
        public int Claimant;
        public OwnershipBasis Basis;
    }

    /// <summary>276: <see cref="Assets.Transfer"/>.</summary>
    public sealed class AssetTransferred : DomainEvent
    {
        public int Asset;
        public OwnerKind FromKind;
        public int FromId;
        public OwnerKind ToKind;
        public int ToId;
        public OwnershipBasis Basis;
        public int Actor;
    }

    /// <summary>277: <see cref="Assets.Destroy"/>. Cause: 1 dismantled, 2 miracle (D3), 3 decay, 4 other.</summary>
    public sealed class AssetDestroyed : DomainEvent
    {
        public int Asset, Kind;
        public OwnerKind OwnerKind;
        public int OwnerId;
        public byte Cause;
        public int CauseRef;
    }
}
