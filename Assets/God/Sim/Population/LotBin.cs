using System.Collections.Generic;

namespace God.Sim.Population
{
    /// <summary>Who is entitled to a lot (SHARED_CONTRACTS 3). Estate: Id = the deceased's NpcId; State: the FactionId. F1 (contracts 7.1): <see cref="Org"/> (Id = OrgId) for every organisation; <see cref="Enterprise"/> is never written.</summary>
    public enum OwnerKind : byte { None = 0, Npc = 1, Household = 2, Group = 3, Enterprise = 4, State = 5, Estate = 6, Org = 7 }

    /// <summary>
    /// The owner of a lot or a bin (SHARED_CONTRACTS 2.1). A plain struct with public fields (it is saved and hashed with the
    /// lot); compare with <see cref="Same"/>. The default is <see cref="None"/> (ownerless).
    /// </summary>
    public struct Owner
    {
        public OwnerKind Kind;
        public int Id;

        public Owner(OwnerKind kind, int id) { Kind = kind; Id = kind == OwnerKind.None ? 0 : id; }

        public static Owner None => default;
        public static Owner Npc(int id) => new Owner(OwnerKind.Npc, id);
        public static Owner Household(int id) => new Owner(OwnerKind.Household, id);
        public static Owner Estate(int deceased) => new Owner(OwnerKind.Estate, deceased);
        public static Owner Group(int id) => new Owner(OwnerKind.Group, id);

        public bool IsNone => Kind == OwnerKind.None;
        public bool Same(Owner o) => Kind == o.Kind && Id == o.Id;
        public override string ToString() => Kind == OwnerKind.None ? "none" : Kind + " " + Id;
    }

    /// <summary>Where a lot is physically (possession): a person's carry, the ground at a node, or a bin.</summary>
    public enum HolderKind : byte { None = 0, Carry = 1, Ground = 2, Bin = 3 }

    /// <summary>A holder of lots (SHARED_CONTRACTS 2.1). Carry: the NpcId; Ground: the node; Bin: the BinId.</summary>
    public readonly struct Holder
    {
        public readonly HolderKind Kind;
        public readonly int Id;

        public Holder(HolderKind kind, int id) { Kind = kind; Id = id; }

        public static Holder Carry(int npc) => new Holder(HolderKind.Carry, npc);
        public static Holder Ground(int node) => new Holder(HolderKind.Ground, node);
        public static Holder Bin(int bin) => new Holder(HolderKind.Bin, bin);

        public bool Same(Holder h) => Kind == h.Kind && Id == h.Id;
        public override string ToString() => Kind + " " + Id;
    }

    /// <summary>Kinds of bin. F1 (contracts 7.1): 3 OrgStore (C6), 4 AssetStore (C1), 5 CommunalStore (C8), 6 Shrine (D6), each created only through <c>Goods.NewBin</c> by its owner package.
    /// UN0 (contracts 23.4): the unified containers 7 Storage (a building's store), 8 Site (a construction site's materials), 9 Fixtures (installed
    /// furnishings), 10 WorkInput, 11 WorkOutput (a workstation), 12 Container (inside an installed chest); 13-15 reserved. A unified bin has a
    /// location and a capacity, never an owner.</summary>
    public enum BinKind : byte { HouseholdStore = 1, Estate = 2, OrgStore = 3, AssetStore = 4, CommunalStore = 5, Shrine = 6,
        Storage = 7, Site = 8, Fixtures = 9, WorkInput = 10, WorkOutput = 11, Container = 12 }

    /// <summary>
    /// A store of lots at a node with an owner (SHARED_CONTRACTS 2.1): a household store (M4) or an estate (M4). Lots are
    /// sorted by id; <see cref="WeightCap"/> 0 means no cap. Created and removed only through <see cref="Goods.NewBin"/> and
    /// <see cref="Goods.RemoveBin"/>; its lots change only through <see cref="Goods"/>.
    /// </summary>
    public sealed class LotBin
    {
        public int Id;
        public BinKind Kind;
        public Owner Owner;
        public int Node;
        public int WeightCap;
        public List<ItemLot> Lots = new List<ItemLot>();

        public int Weight
        {
            get { int w = 0; foreach (var l in Lots) w += PopulationModule.Weight(l.Ware) * l.Count; return w; }
        }
    }
}
