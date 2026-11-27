using System.Collections.Generic;
using God.Sim.Civ;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// C1's warehouse mappers (OWNER: C1; migration <c>0015_ownership_work.sql</c>, tables in <c>Tables.C1.cs</c>). Ownership
    /// spells of assets (AssetBuilt / AssetTransferred open, AssetTransferred / AssetDestroyed close; one owner = 1000
    /// permille), use-right spells and their economic relationship (holder uses an asset of the owner), work units, source
    /// claims, and theft (analytics-only truth: at Full only). Spells are half-open and never overlap per asset (W-D5-6):
    /// a transfer closes the old spell at the tick the new one opens.
    /// </summary>
    public static class C1Mappings
    {
        /// <summary>The grain of each C1 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_ownership_spell", "one row per spell of one owner holding one asset (to_tick null while open)", "AssetBuilt, AssetTransferred, AssetDestroyed"),
            ("fact_use_right", "one row per spell of one use right on one asset", "UseRightGranted, UseRightEnded"),
            ("fact_economic_relationship", "one row per spell of a relationship of two parties (C1: kind 1 uses an asset of)", "UseRightGranted, UseRightEnded"),
            ("fact_asset_use", "one row per completed work unit; sampled below Full", "AssetUsed"),
            ("fact_theft", "one row per classified theft (truth, Full only)", "TheftCommitted"),
            ("fact_source_claim", "one row per first-arrival claim on a source", "SourceClaimed"),
        };

        public const long EndTransferred = 1, EndDestroyed = 2, RelUsesAssetOf = 1;

        /// <summary>The open spells of one run (the recorder makes a registry per run, so this lives with it).</summary>
        private sealed class State
        {
            public long NextSpell = 1, NextRight = 1, NextRel = 1;
            public readonly Dictionary<int, long> OpenSpell = new Dictionary<int, long>();
            public readonly Dictionary<int, int> KindOf = new Dictionary<int, int>();
            public readonly Dictionary<(int asset, int hk, int hid, int kind), (long right, long rel)> OpenRight = new Dictionary<(int, int, int, int), (long, long)>();
        }

        public static void Register(EventMapRegistry r)
        {
            var st = new State();
            void Open(MapContext c, int asset, int kind, God.Sim.Population.OwnerKind ok, int oid, God.Sim.Population.OwnershipBasis basis, int node)
            {
                long key = st.NextSpell++;
                st.OpenSpell[asset] = key;
                st.KindOf[asset] = kind;
                c.Insert(Tables.FactOwnershipSpellOpen, key, (long)asset, (long)kind, c.Party(ok, oid), (long)(int)ok, (long)oid, 1000L, (long)(int)basis, c.Tick, c.DateKey(),
                    node > 0 ? (object)c.NodeLocation(node) : null);
            }
            void Close(MapContext c, int asset, long end)
            {
                if (!st.OpenSpell.TryGetValue(asset, out long key)) return;
                st.OpenSpell.Remove(asset);
                c.Insert(Tables.FactOwnershipSpellClose, key, c.Tick, c.DateKey(), end);
            }
            r.Register<AssetBuilt>("fact_ownership_spell", "ownership_spell", (c, e) => Open(c, e.Asset, e.Kind, e.OwnerKind, e.OwnerId, e.Basis, e.Node));
            r.Register<AssetTransferred>("fact_ownership_spell", "ownership_spell", (c, e) =>
            {
                st.KindOf.TryGetValue(e.Asset, out int kind);
                Close(c, e.Asset, EndTransferred);
                Open(c, e.Asset, kind, e.ToKind, e.ToId, e.Basis, 0);
            });
            r.Register<AssetDestroyed>("fact_ownership_spell", "ownership_spell", (c, e) => Close(c, e.Asset, EndDestroyed));
            r.Register<UseRightGranted>("fact_use_right", "use_right", (c, e) =>
            {
                long right = st.NextRight++, rel = st.NextRel++;
                st.OpenRight[(e.Asset, (int)e.HolderKind, e.HolderId, (int)e.Kind)] = (right, rel);
                var holder = c.Party(e.HolderKind, e.HolderId); var by = c.Party(e.GrantedByKind, e.GrantedById);
                c.Insert(Tables.FactUseRightOpen, right, (long)e.Asset, holder, (long)(int)e.Kind, by, e.UntilDay >= 0 ? (object)(long)e.UntilDay : null, c.Tick, c.DateKey());
                c.Insert(Tables.FactEconRelOpen, rel, holder, by, RelUsesAssetOf, (long)e.Asset, c.Tick, c.DateKey());
            });
            r.Register<UseRightEnded>("fact_use_right", "use_right", (c, e) =>
            {
                var k = (e.Asset, (int)e.HolderKind, e.HolderId, (int)e.Kind);
                if (!st.OpenRight.TryGetValue(k, out var open)) return;
                st.OpenRight.Remove(k);
                c.Insert(Tables.FactUseRightClose, open.right, c.Tick, c.DateKey(), (long)e.Reason);
                c.Insert(Tables.FactEconRelClose, open.rel, c.Tick, c.DateKey());
            });
            r.Register<AssetUsed>("fact_asset_use", "asset_use", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactAssetUse, c.Tick, c.Seq, c.DateKey(), (long)e.Asset, (object)c.Person(e.Worker).Key, c.Party(e.OwnerKind, e.OwnerId), (long)e.Control,
                    e.Intervention != 0 ? (object)(long)e.Intervention : null, e.OutputWare != God.Sim.Ware.None ? (object)(long)(int)e.OutputWare : null, (long)e.OutputCount,
                    (long)e.Right, (long)c.Weight);
            });
            r.Register<TheftCommitted>("fact_theft", "theft", (c, e) =>
            {
                if (!c.Full || !c.RowWanted) return; // analytics-only truth
                c.Insert(Tables.FactTheft, c.Tick, c.Seq, c.DateKey(), (long)e.Kind, e.Lot != 0 ? (object)(long)e.Lot : null, e.Asset != 0 ? (object)(long)e.Asset : null,
                    e.Ware != God.Sim.Ware.None ? (object)(long)(int)e.Ware : null, (long)e.Count, c.Party(e.OwnerKind, e.OwnerId), (object)c.Person(e.Taker).Key,
                    c.NodeLocation(e.Node), (long)(int)e.FromKind, (long)e.FromHolder);
            });
            r.Register<SourceClaimed>("fact_source_claim", "source_claim", (c, e) =>
            {
                if (!c.RowWanted) return;
                c.Insert(Tables.FactSourceClaim, c.Tick, c.Seq, c.DateKey(), (long)e.Source, (object)c.Person(e.Claimant).Key, (long)(int)e.Basis);
            });
        }
    }
}
