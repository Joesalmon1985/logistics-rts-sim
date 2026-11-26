using System.Collections.Generic;

namespace God.Sim.Population
{
    /// <summary>Who is entitled to a lot (SHARED_CONTRACTS 3). Estate: Id = the deceased's NpcId; State: the FactionId.</summary>
    public enum OwnerKind : byte { None = 0, Npc = 1, Household = 2, Group = 3, Enterprise = 4, State = 5, Estate = 6 }

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

    /// <summary>Kinds of bin (3+ reserved: site, granary, workshop).</summary>
    public enum BinKind : byte { HouseholdStore = 1, Estate = 2 }

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
