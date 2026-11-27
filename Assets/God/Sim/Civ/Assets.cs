using System;
using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>The C1 object classes (contracts 14: Civ assets 90-99), perceived through the Sense seam.</summary>
    public static class CivClass
    {
        public const int Field = 90, Workshop = 91, Store = 92;
        /// <summary>A free plot where the person stands and could found an asset (a place, not an object: Ref 0).</summary>
        public const int Plot = 93;
        /// <summary>C1c: the store of an asset, seen as a store holding goods it may take (a bin: Ref is the BinId; the asset is the one at its node).</summary>
        public const int AssetStore = 94;
        public static bool IsAsset(int cls) => cls >= Field && cls <= Store;
    }

    /// <summary>The first asset kinds (rows of <see cref="AssetTable"/>; data, never an enum switch: industry appends rows).</summary>
    public static class AssetKinds
    {
        public const int Field = 1, Workshop = 2, Store = 3;
    }

    /// <summary>What a use right allows (spec 6).</summary>
    public enum UseKind : byte { None = 0, Work = 1, Store = 2, Gather = 3, Pass = 4 }

    /// <summary>A ware and a count (a build cost line or a recipe line).</summary>
    public readonly struct WareCount
    {
        public readonly Ware Ware;
        public readonly int Count;
        public WareCount(Ware w, int n) { Ware = w; Count = n; }
    }

    /// <summary>
    /// One cycle of an asset: inputs (any number, consumed from the asset's store or the worker's carry) into outputs (any
    /// number, produced into the store, owned by the asset's owner). Non-ware terms (fuel, electricity) are left to later rows.
    /// </summary>
    public sealed class Recipe
    {
        public readonly ReadOnlyArray<WareCount> Inputs, Outputs;
        public readonly ProduceReason Reason;
        public Recipe(ProduceReason reason, WareCount[] inputs, WareCount[] outputs)
        {
            Reason = reason; Inputs = new ReadOnlyArray<WareCount>(inputs); Outputs = new ReadOnlyArray<WareCount>(outputs);
        }
    }

    /// <summary>One row of the asset table (spec 5.2).</summary>
    public sealed class AssetDef
    {
        public readonly int Kind;
        public readonly string Name;
        public readonly int ObjClass;
        public readonly ReadOnlyArray<WareCount> BuildCost;
        /// <summary>Work ticks to finish once every material is in.</summary>
        public readonly int BuildTicks;
        /// <summary>Work ticks (at speed 1000) of one recipe cycle; 0: the asset is not worked (a store).</summary>
        public readonly int WorkTicks;
        public readonly Recipe Recipe;
        /// <summary>The store's weight cap (0: no store).</summary>
        public readonly int StoreCapacity;
        public readonly int StaffSlots;
        /// <summary>A mobile asset keeps its location in the owning package's record (its <c>Node</c> is 0).</summary>
        public readonly bool Mobile;

        public AssetDef(int kind, string name, int objClass, WareCount[] cost, int buildTicks, int workTicks, Recipe recipe, int storeCapacity, int staff, bool mobile = false)
        {
            Kind = kind; Name = name; ObjClass = objClass; BuildCost = new ReadOnlyArray<WareCount>(cost); BuildTicks = buildTicks; WorkTicks = workTicks;
            Recipe = recipe; StoreCapacity = storeCapacity; StaffSlots = staff; Mobile = mobile;
        }

        public int MaterialCount { get { int n = 0; foreach (var c in BuildCost) n += c.Count; return n; } }
    }

    /// <summary>
    /// The asset kinds (data). C1's rows: a Field (worked plot: roots from labour alone, by work ticks), a Workshop (a
    /// log into a plank: no goods from nothing) and a Store (only a store). Defaults recorded in the C1 record.
    /// </summary>
    public static partial class AssetTable
    {
        public static readonly ReadOnlyArray<AssetDef> Rows = new ReadOnlyArray<AssetDef>(
            new AssetDef(AssetKinds.Field, "field", CivClass.Field, new[] { new WareCount(Ware.Log, 1) }, 30, 60,
                new Recipe(ProduceReason.HandGathered, new WareCount[0], new[] { new WareCount(Ware.Roots, 2) }), 40, 2),
            new AssetDef(AssetKinds.Workshop, "workshop", CivClass.Workshop, new[] { new WareCount(Ware.Log, 2) }, 40, 80,
                new Recipe(ProduceReason.Crafted, new[] { new WareCount(Ware.Log, 1) }, new[] { new WareCount(Ware.Plank, 1) }), 48, 1),
            new AssetDef(AssetKinds.Store, "store", CivClass.Store, new[] { new WareCount(Ware.Log, 2) }, 30, 0, null, 60, 0));

        public static AssetDef Get(int kind) { foreach (var d in Rows) if (d.Kind == kind) return d; return null; }
        public static AssetDef OfClass(int cls) { foreach (var d in Rows) if (d.ObjClass == cls) return d; return null; }
        public static bool IsMaterial(Ware w) { foreach (var d in Rows) foreach (var c in d.BuildCost) if (c.Ware == w) return true; return false; }
    }

    /// <summary>A use right on an asset (spec 6).</summary>
    public struct UseRight
    {
        public Owner Holder;
        public UseKind Kind;
        public Owner GrantedBy;
        /// <summary>The last day it holds (-1: open-ended).</summary>
        public int UntilDay;
        public int Terms;
    }

    /// <summary>
    /// A productive asset and its property record (ECONOMY 3.5): owner, basis, shares (one owner = 1000 permille in C1),
    /// use rights, disputes. The AssetId is C1's (R20), allocated only by <see cref="Assets.Create"/>, never reused.
    /// </summary>
    public sealed class ProductiveAsset
    {
        public const byte Unfinished = 1, Destroyed = 2;
        public int Id;
        public int Kind;
        /// <summary>Its node; 0 when the owning package keeps the location (a vehicle later).</summary>
        public int Node;
        public Owner Owner;
        public OwnershipBasis Basis;
        public long SinceTick;
        /// <summary>The BinId of its store (<see cref="BinKind.AssetStore"/>), 0 none (unfinished, or a kind without a store).</summary>
        public int Store;
        public List<UseRight> Rights = new List<UseRight>();
        public List<Owner> Disputes = new List<Owner>();
        public int Condition = 1000;
        public long BuiltTick;
        public byte Flags;
        /// <summary>The person who founded the site.</summary>
        public int Founder;
        /// <summary>Materials put in, per build-cost line (unfinished only).</summary>
        public int[] MaterialsIn;
        /// <summary>Build work done once the materials are in (ticks).</summary>
        public int BuildWork;
        /// <summary>Work progress of the current cycle, permille-ticks (a half-done unit survives a save).</summary>
        public int WorkProgress;
        /// <summary>Completed work units (cycles), ever.</summary>
        public int Units;

        public bool IsFinished => (Flags & Unfinished) == 0;

        /// <summary>The only writer of the owner field (property record changes go through <see cref="Assets"/>).</summary>
        internal void Own(Owner o, OwnershipBasis basis, long tick) { Owner = o; Basis = basis; SinceTick = tick; }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(Kind); h.Add(Node); h.Add((int)Owner.Kind); h.Add(Owner.Id); h.Add((int)Basis); h.Add(SinceTick); h.Add(Store);
            h.Add(Rights.Count);
            foreach (var r in Rights) { h.Add((int)r.Holder.Kind); h.Add(r.Holder.Id); h.Add((int)r.Kind); h.Add((int)r.GrantedBy.Kind); h.Add(r.GrantedBy.Id); h.Add(r.UntilDay); h.Add(r.Terms); }
            h.Add(Disputes.Count);
            foreach (var d in Disputes) { h.Add((int)d.Kind); h.Add(d.Id); }
            h.Add(Condition); h.Add(BuiltTick); h.Add(Flags); h.Add(Founder);
            h.Add(MaterialsIn == null ? -1 : MaterialsIn.Length);
            if (MaterialsIn != null) foreach (var m in MaterialsIn) h.Add(m);
            h.Add(BuildWork); h.Add(WorkProgress); h.Add(Units);
        }
    }

    /// <summary>
    /// The property-record API of productive assets (spec 5.2, 22). Every asset of every later package (workshops, mines,
    /// vehicles, power stations) takes its id here. Nothing here branches on the owner's kind (C1-T11): a household, a
    /// person and an organisation own through the same code.
    /// </summary>
    public static class Assets
    {
        public const int MaxRights = 8, MaxDisputes = 4;

        public static OwnershipState StateOf(GodState s) => s.Civ?.Ownership;

        public static ProductiveAsset Find(GodState s, int id)
        {
            var o = StateOf(s);
            if (o == null || id <= 0) return null;
            // ascending ids: binary search
            int lo = 0, hi = o.Assets.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = o.Assets[mid].Id;
                if (v == id) return o.Assets[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public static ProductiveAsset ByStore(GodState s, int bin)
        {
            var o = StateOf(s);
            if (o == null || bin <= 0) return null;
            foreach (var a in o.Assets) if (a.Store == bin) return a;
            return null;
        }

        /// <summary>The asset at a node (the first by id), or null.</summary>
        public static ProductiveAsset At(GodState s, int node)
        {
            var o = StateOf(s);
            if (o == null) return null;
            foreach (var a in o.Assets) if (a.Node == node && a.Kind != 0) return a;
            return null;
        }

        /// <summary>
        /// A new asset (the only AssetId allocator, R20). <paramref name="finished"/> false: a site that still needs its
        /// materials and work (the builder's path); true: a finished asset with its store (scenario content, industry).
        /// Emits <see cref="AssetBuilt"/> only when finished.
        /// </summary>
        public static ProductiveAsset Create(GodState s, EventBuffer ev, int kind, int node, Owner owner, OwnershipBasis basis, int founder, bool finished)
        {
            var o = StateOf(s) ?? throw new InvalidOperationException("Assets.Create needs Civ.Enabled = 1");
            var def = AssetTable.Get(kind) ?? throw new ArgumentOutOfRangeException(nameof(kind));
            var a = new ProductiveAsset
            {
                Id = o.NextAssetId++, Kind = kind, Node = def.Mobile ? 0 : node, Founder = founder,
                Flags = ProductiveAsset.Unfinished, MaterialsIn = new int[def.BuildCost.Count],
            };
            a.Own(owner, basis, s.Tick);
            o.Assets.Add(a); // ids ascend
            if (finished) Finish(s, ev, a, founder);
            return a;
        }

        /// <summary>Spec 22 / contracts 22.4 name: <see cref="Create"/> of a finished asset.</summary>
        public static ProductiveAsset Register(GodState s, EventBuffer ev, int kind, int node, Owner owner, OwnershipBasis basis, int actor) =>
            Create(s, ev, kind, node, owner, basis, actor, true);

        /// <summary>The site is complete: its store opens and <see cref="AssetBuilt"/> names the owner.</summary>
        internal static void Finish(GodState s, EventBuffer ev, ProductiveAsset a, int builder)
        {
            var def = AssetTable.Get(a.Kind);
            a.Flags = (byte)(a.Flags & ~ProductiveAsset.Unfinished);
            a.MaterialsIn = null;
            a.BuildWork = 0;
            a.BuiltTick = s.Tick;
            if (def.StoreCapacity > 0 && a.Node > 0) a.Store = Goods.NewBin(s, BinKind.AssetStore, a.Owner, a.Node, def.StoreCapacity);
            ev?.Add(new AssetBuilt { Asset = a.Id, Kind = a.Kind, Node = a.Node, OwnerKind = a.Owner.Kind, OwnerId = a.Owner.Id, Builder = builder, Basis = a.Basis, Store = a.Store });
        }

        /// <summary>
        /// The property record changes owner (sale, gift, seizure, inheritance) with a basis; emits <see cref="AssetTransferred"/>.
        /// Use rights granted by the old owner end (<see cref="UseRightEnded.Transferred"/>). The goods in the store keep their
        /// own owners (each lot is its own property; the bin's owner field has no Goods setter: C1 reads the asset record).
        /// </summary>
        public static bool Transfer(GodState s, EventBuffer ev, int assetId, Owner to, OwnershipBasis basis, int actor)
        {
            var a = Find(s, assetId);
            if (a == null || a.Owner.Same(to)) return false;
            var from = a.Owner;
            for (int i = a.Rights.Count - 1; i >= 0; i--)
                if (a.Rights[i].GrantedBy.Same(from)) EndAt(s, ev, a, i, UseRightEnded.Transferred);
            a.Own(to, basis, s.Tick);
            ev?.Add(new AssetTransferred { Asset = a.Id, FromKind = from.Kind, FromId = from.Id, ToKind = to.Kind, ToId = to.Id, Basis = basis, Actor = actor });
            return true;
        }

        /// <summary>Grants a use right (at most <see cref="MaxRights"/>; a holder has one row per kind). Emits <see cref="UseRightGranted"/>.</summary>
        public static bool GrantUse(GodState s, EventBuffer ev, int assetId, Owner holder, UseKind kind, Owner grantedBy, int untilDay, int terms = 0)
        {
            var a = Find(s, assetId);
            if (a == null || holder.IsNone || kind == UseKind.None || a.Rights.Count >= MaxRights) return false;
            foreach (var r in a.Rights) if (r.Holder.Same(holder) && r.Kind == kind) return false;
            a.Rights.Add(new UseRight { Holder = holder, Kind = kind, GrantedBy = grantedBy, UntilDay = untilDay, Terms = terms });
            ev?.Add(new UseRightGranted { Asset = a.Id, HolderKind = holder.Kind, HolderId = holder.Id, Kind = kind, GrantedByKind = grantedBy.Kind, GrantedById = grantedBy.Id, UntilDay = untilDay });
            // the holder is told by the grant itself (the act it accepted): it now believes the owner and its right
            Ownership.ToldOfRight(s, a, holder, kind);
            return true;
        }

        public static bool EndUse(GodState s, EventBuffer ev, int assetId, Owner holder, UseKind kind, byte reason = UseRightEnded.ByGrantor)
        {
            var a = Find(s, assetId);
            if (a == null) return false;
            for (int i = 0; i < a.Rights.Count; i++)
                if (a.Rights[i].Holder.Same(holder) && a.Rights[i].Kind == kind) { EndAt(s, ev, a, i, reason); return true; }
            return false;
        }

        internal static void EndAt(GodState s, EventBuffer ev, ProductiveAsset a, int i, byte reason)
        {
            var r = a.Rights[i];
            a.Rights.RemoveAt(i);
            ev?.Add(new UseRightEnded { Asset = a.Id, HolderKind = r.Holder.Kind, HolderId = r.Holder.Id, Kind = r.Kind, Reason = reason });
        }

        /// <summary>
        /// The asset ceases to exist (D3 calls this for a miracle; nothing in C1 does). Its store must be emptied first by
        /// <see cref="Goods"/>: the caller consumes or moves every lot (no goods vanish here). Returns false while the store holds goods.
        /// </summary>
        public static bool Destroy(GodState s, EventBuffer ev, int assetId, byte cause, int causeRef = 0)
        {
            var o = StateOf(s);
            var a = Find(s, assetId);
            if (a == null) return false;
            if (a.Store != 0)
            {
                var bin = Goods.BinById(s.Population, a.Store);
                if (bin != null && bin.Lots.Count > 0) return false;
                if (bin != null) Goods.RemoveBin(s, a.Store);
                a.Store = 0;
            }
            for (int i = a.Rights.Count - 1; i >= 0; i--) EndAt(s, ev, a, i, UseRightEnded.AssetGone);
            o.Assets.Remove(a);
            ev?.Add(new AssetDestroyed { Asset = a.Id, Kind = a.Kind, OwnerKind = a.Owner.Kind, OwnerId = a.Owner.Id, Cause = cause, CauseRef = causeRef });
            return true;
        }

        /// <summary>Assets this owner holds (finished or not).</summary>
        public static int CountOwnedBy(GodState s, Owner owner)
        {
            var o = StateOf(s);
            if (o == null) return 0;
            int n = 0;
            foreach (var a in o.Assets) if (a.Owner.Same(owner)) n++;
            return n;
        }
    }
}
