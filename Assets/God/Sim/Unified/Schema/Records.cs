using System.Collections.Generic;

namespace God.Sim.Unified
{
    // UN0 (contracts 23.8, 23.9, 23.4): the record shapes of the unified world, frozen. Behaviour belongs to the owners named
    // on each type; creation only through their APIs (Buildings.Create, Sites.Place, World.*). No record has an owner, right or
    // permission field: founders and contributors are provenance. Every record hashes itself in a fixed field order.

    /// <summary>One contributor's share of a site or building (provenance only; bounded 16 per record).</summary>
    public struct Contribution
    {
        public int Npc;
        public int Labour;
        public short Ware;
        public short Units;
        public long Last;

        public void AddTo(ref StateHasher h) { h.Add(Npc); h.Add(Labour); h.Add(Ware); h.Add(Units); h.Add(Last); }
    }

    /// <summary>An unfinished construction site (OWNER UN4). Materials wait in <see cref="Bin"/> (<c>BinKind.Site</c>) and are consumed (<c>Built</c>) only at completion.</summary>
    public sealed class ConstructionSite
    {
        public const int MaxContributors = 16;

        public int Id;
        public ushort Design;
        public byte Level;
        public int Node;
        public byte Facing;
        public int Bin;
        public int LabourDone, LabourNeed;
        /// <summary>The building id for an upgrade, else 0.</summary>
        public int UpgradeOf;
        public int Founder;
        public long Placed, LastWork;
        public List<Contribution> Contributors = new List<Contribution>();

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(Design | (Level << 16) | (Facing << 24)); h.Add(Node); h.Add(Bin); h.Add(LabourDone); h.Add(LabourNeed); h.Add(UpgradeOf);
            h.Add(Founder); h.Add(Placed); h.Add(LastWork);
            h.Add(Contributors.Count); foreach (var c in Contributors) c.AddTo(ref h);
        }
    }

    /// <summary>A finished building (OWNER UN3b): condition, its bins (0 = none) and occupants; founder and contributors are provenance.</summary>
    public sealed class Building
    {
        public int Id;
        public ushort Cat;
        public byte Level;
        public int Node;
        public byte Facing;
        public int Condition;
        public int Storage, Fixtures, WorkIn, WorkOut;
        public List<int> Occupants = new List<int>();
        public int Founder;
        public List<Contribution> Contributors = new List<Contribution>();
        public long Completed;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(Cat | (Level << 16) | (Facing << 24)); h.Add(Node); h.Add(Condition); h.Add(Storage); h.Add(Fixtures); h.Add(WorkIn); h.Add(WorkOut);
            h.Add(Occupants.Count); foreach (int o in Occupants) h.Add(o);
            h.Add(Founder); h.Add(Contributors.Count); foreach (var c in Contributors) c.AddTo(ref h);
            h.Add(Completed);
        }
    }

    /// <summary>A temporary physical capacity reservation at a building's store (OWNER UN3b): prevents overbooking, confers nothing, always expires.</summary>
    public struct Reservation
    {
        public int Building;
        public int Npc;
        public short Units;
        public long Expires;

        public void AddTo(ref StateHasher h) { h.Add(Building); h.Add(Npc); h.Add(Units); h.Add(Expires); }
    }

    /// <summary>A raw-material node on a hex (OWNER UN1): stock materialises gradually from the dice allowance.</summary>
    public sealed class RawNode
    {
        public int Id, Hex, Node;
        public short Ware;
        public int Stock, Cap;

        public void AddTo(ref StateHasher h) { h.Add(Id); h.Add(Hex); h.Add(Node); h.Add(Ware); h.Add(Stock); h.Add(Cap); }
    }

    /// <summary>An infinite, time-limited forage place (OWNER UN1).</summary>
    public sealed class ForagePlace
    {
        public int Id, Node;
        public short Ware;
        public int Ticks;

        public void AddTo(ref StateHasher h) { h.Add(Id); h.Add(Node); h.Add(Ware); h.Add(Ticks); }
    }

    /// <summary>A water point (OWNER UN1): fresh or salt; <see cref="Building"/> a well (0 = natural).</summary>
    public sealed class WaterPoint
    {
        public int Id, Node;
        public bool Fresh;
        public int Building;

        public void AddTo(ref StateHasher h) { h.Add(Id); h.Add(Node); h.Add(Fresh ? 1 : 0); h.Add(Building); }
    }

    /// <summary>The source kinds of a <see cref="LotOrigin"/>.</summary>
    public static class OriginSource
    {
        public const byte None = 0, ForagePlace = 1, RawNode = 2, Building = 3, Site = 4, WaterPoint = 5, Npc = 6, Miracle = 7, Start = 8;
    }

    /// <summary>
    /// UN0 (contracts 23.4): the compact origin of one live lot (objective provenance, never title): producer, produce reason,
    /// tick, source. <see cref="Mixed"/> after a merge of different origins (the earliest tick kept). Written by <c>Goods.Produce</c>,
    /// copied on a split, merged on insert, removed when the lot ends.
    /// </summary>
    public struct LotOrigin
    {
        public int Lot;
        public int Producer;
        public byte Reason;
        public long Tick;
        public byte SourceKind;
        public int SourceId;
        public bool Mixed;

        public void AddTo(ref StateHasher h) { h.Add(Lot); h.Add(Producer); h.Add(Reason | (SourceKind << 8) | (Mixed ? 1 << 16 : 0)); h.Add(Tick); h.Add(SourceId); }
    }

    /// <summary>
    /// UN0 (contracts 23.6, UN0 spec 5.2): a person's unified extension (sorted by <see cref="Npc"/>): the seven further need
    /// channels (Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room; pressures 0-1000, higher = more urgent), the motor fields UN2
    /// fills (facing 0-5 of §D9.4 a, the control in progress, its ticks left, target and slot, the last result, the equipped lot,
    /// the occupied building and spot) and the <see cref="BrainState"/> UN6 fills. Sleep stays the existing <c>Npc.Action.Asleep</c>.
    /// PARTIAL: an owner adding fields hashes them in its <c>AddToUNn</c> slot.
    /// </summary>
    public sealed partial class UnifiedNpc
    {
        public int Npc;
        public short[] Needs = new short[NeedChannels.Extensions];
        public byte Facing;
        public Prim ActionKind;
        public int ActionTicksLeft;
        public TargetRef ActionTarget;
        public byte ActionSlot;
        public MotorStatus LastResult;
        /// <summary>The equipped lot (0 none); it stays in the carry.</summary>
        public int Equipped;
        /// <summary>The building whose spot the body occupies (0 none) and the spot.</summary>
        public int Occupying;
        public byte OccupyingSpot;
        public BrainState Brain = new BrainState();

        public short NeedOf(NeedChannel c) => Needs[(int)c - NeedChannels.FirstExtension];
        public bool Busy => ActionKind != Prim.None && ActionTicksLeft > 0;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Npc); h.Add(Facing | ((int)ActionKind << 8) | (ActionSlot << 16) | ((int)LastResult << 24));
            for (int i = 0; i < Needs.Length; i++) h.Add(Needs[i]);
            h.Add(ActionTicksLeft); ActionTarget.AddTo(ref h); h.Add(Equipped); h.Add(Occupying); h.Add(OccupyingSpot);
            Brain.AddTo(ref h);
            AddToUN1(ref h); AddToUN2(ref h); AddToUN6(ref h); AddToUN7(ref h); AddToUN8(ref h);
        }

        partial void AddToUN1(ref StateHasher h);
        partial void AddToUN2(ref StateHasher h);
        partial void AddToUN6(ref StateHasher h);
        partial void AddToUN7(ref StateHasher h);
        partial void AddToUN8(ref StateHasher h);
    }

    /// <summary>UN0 (contracts 23.9): the unified side record of one GESTURE message (the M5 <c>Message</c> layout is unchanged; its DealId is unused).</summary>
    public struct MsgExt
    {
        public int Message;
        public Referent R1, R2;
        public short Quantity;
        public uint Corr;

        public void AddTo(ref StateHasher h) { h.Add(Message); R1.AddTo(ref h); R2.AddTo(ref h); h.Add(Quantity); h.Add(Corr); }
    }

    /// <summary>UN0 (contracts 23.9): a lot held out to a receiver (<see cref="To"/> 0 = anyone); a receiver's TAKE completes the hand-over; expiry leaves it in the giver's carry (OWNER UN7).</summary>
    public struct HoldOut
    {
        public int Npc;
        public int Lot;
        public int To;
        public uint Corr;
        public long Expires;

        public void AddTo(ref StateHasher h) { h.Add(Npc); h.Add(Lot); h.Add(To); h.Add(Corr); h.Add(Expires); }
    }
}
