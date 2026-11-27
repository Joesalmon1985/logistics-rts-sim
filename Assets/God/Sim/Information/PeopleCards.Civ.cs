using System.Collections.Generic;
using God.Sim.Civ;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>F1.14: the economy tab of a person's card (OWNER: C1). Its rows carry their knowledge layer.</summary>
    public sealed class CivCard
    {
        public KnowledgeLayer Layer = KnowledgeLayer.Belief;
        /// <summary>Lines for the tab, in order; each starts with its layer in brackets (<c>[Truth]</c>, <c>[Belief]</c>).</summary>
        public List<string> Lines = new List<string>();
        /// <summary>The structured rows (U5 draws them): what, the layer, and the text.</summary>
        public List<CivCardRow> Rows = new List<CivCardRow>();
    }

    /// <summary>One row of the economy tab. Kind: 1 possession, 2 asset owned, 3 use right held, 4 believed owner of an asset.</summary>
    public readonly struct CivCardRow
    {
        public const byte Possession = 1, AssetOwned = 2, UseRightHeld = 3, BelievedOwner = 4;
        public readonly byte Kind;
        public readonly KnowledgeLayer Layer;
        public readonly int Ref;
        public readonly string Text;
        public CivCardRow(byte kind, KnowledgeLayer layer, int reference, string text) { Kind = kind; Layer = layer; Ref = reference; Text = text; }
    }

    /// <summary>
    /// Fills <see cref="CivCard"/> for the inspector (OWNER: C1). Called by <c>PopulationKnowledge.Inspect</c>; read-only.
    /// Possessions, assets owned and use rights held are truth (the inspector's view of the record); the believed owners of
    /// assets are the person's own beliefs (layer 6), which may be wrong.
    /// </summary>
    public static partial class CivCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
            var c = card.Economy;
            if (s.Civ?.Ownership == null) return;
            void Row(byte kind, KnowledgeLayer layer, int reference, string text) { c.Rows.Add(new CivCardRow(kind, layer, reference, text)); c.Lines.Add("[" + layer + "] " + text); }
            foreach (var l in n.Carry)
                if (!l.Empty) Row(CivCardRow.Possession, KnowledgeLayer.Truth, l.Id, "carries " + l.Count + " " + l.Ware.ToString().ToLowerInvariant() + (l.Owner.IsNone ? " (nobody's)" : Ownership.OwnerRelation(n, l.Owner) != Ownership.RightNone ? "" : " (owned by " + l.Owner + ")"));
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            foreach (var a in s.Civ.Ownership.Assets)
            {
                var def = AssetTable.Get(a.Kind);
                byte rel = Ownership.OwnerRelation(n, a.Owner);
                if (rel != Ownership.RightNone)
                    Row(CivCardRow.AssetOwned, KnowledgeLayer.Truth, a.Id, (rel == Ownership.RightHousehold ? "household's " : "own ") + def.Name + " #" + a.Id
                        + (a.IsFinished ? ", " + a.Units + " work units" : ", being built"));
                foreach (var r in a.Rights)
                    if (Ownership.OwnerRelation(n, r.Holder) != Ownership.RightNone && (r.UntilDay < 0 || r.UntilDay >= day))
                        Row(CivCardRow.UseRightHeld, KnowledgeLayer.Truth, a.Id, r.Kind.ToString().ToLowerInvariant() + " right on " + def.Name + " #" + a.Id + " (from " + r.GrantedBy + ")");
            }
            var cm = n.Mind?.Civ;
            if (cm != null)
                foreach (var b in cm.Own)
                {
                    var def = AssetTable.OfClass(b.Class);
                    Row(CivCardRow.BelievedOwner, KnowledgeLayer.Belief, b.Asset, "believes " + (def?.Name ?? "asset") + " #" + b.Asset + " belongs to " + b.BelievedOwner + " (confidence " + b.Confidence + ")");
                }
        }
    }
}
