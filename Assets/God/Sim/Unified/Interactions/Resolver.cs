using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.5, 23.7, D9.1 b 2, D9.5 N1/E): the USE resolver. <c>USE(source or empty hand, target, slot)</c> matches the
    /// <see cref="InteractionDef"/> rows of the world's <see cref="InteractionBook"/> by target kind and the target's catalogue id (a
    /// row with <c>TargetCat</c> 0 matches any id of its kind, after the rows of the exact id), slot (0 = the first row whose source
    /// requirement the source meets; 1-3 = the row declared with that slot) and source requirement (<see cref="SourceReq"/>); checks the
    /// row's reach; takes the row's ticks, shortened only by the source lot's tool multiplier for the row's first effect (minimum 1:
    /// a tool never gates); calls every effect's <c>Validate</c> at the start (a refusal is the submission's result) and its
    /// <c>Apply</c> only at completion (in row order; a refused effect stops the row). An interruption cancels the unfinished application; completed applications persist. No row,
    /// no effect and no output before completion. Unknown designs (a USE on a placement) are <see cref="MotorStatus.NotKnown"/>.
    /// </summary>
    public static class Resolver
    {
        /// <summary>The source classes of <see cref="SourceReq.Class"/> (a default until UN3a's catalogue defines classes; DEV_LOG UN2 record).</summary>
        public static class SourceClass
        {
            public const int Food = 1, Gear = 2, Tool = 3, Strike = 4;
        }

        /// <summary>The resolved target: where it is and its catalogue id (0 when it has none).</summary>
        public struct Resolved
        {
            public int Node;
            public ushort Cat;
        }

        /// <summary>Where a target is and what catalogue id it has; false when it does not exist.</summary>
        public static bool ResolveTarget(GodState s, Npc npc, UnifiedNpc u, in PrimitiveControl c, out Resolved r)
        {
            r = default;
            var book = s.Unification.Book;
            var p = s.Population;
            var w = s.Unification;
            int id = c.Target.Id;
            switch (c.Target.Kind)
            {
                case TargetKind.Self: r.Node = npc.Node; return true;
                case TargetKind.Npc: { var t = p.Find(id); if (t == null || !Bodies.IsPerson(t)) return false; r.Node = t.Node; return true; }
                case TargetKind.Node: if (!s.Territory.Grid.IsValid(id)) return false; r.Node = id; return true;
                case TargetKind.Facing:
                {
                    int n = s.Territory.Grid.Neighbour(npc.Node, u.Facing);
                    if (n < 0) return false;
                    r.Node = n; return true;
                }
                case TargetKind.Placement: if (!s.Territory.Grid.IsValid(id)) return false; r.Node = id; r.Cat = c.Design; return true;
                case TargetKind.Building: { var b = w.BuildingById(id); if (b == null) return false; r.Node = b.Node; r.Cat = b.Cat; return true; }
                case TargetKind.Site: { var x = w.SiteById(id); if (x == null) return false; r.Node = x.Node; r.Cat = x.Design; return true; }
                case TargetKind.ForagePlace: foreach (var x in w.ForagePlaces) if (x.Id == id) { r.Node = x.Node; r.Cat = book.OfWare((Ware)x.Ware)?.CatId ?? 0; return true; } return false;
                case TargetKind.RawNode: foreach (var x in w.RawNodes) if (x.Id == id) { r.Node = x.Node; r.Cat = book.OfWare((Ware)x.Ware)?.CatId ?? 0; return true; } return false;
                case TargetKind.WaterPoint: foreach (var x in w.WaterPoints) if (x.Id == id) { r.Node = x.Node; r.Cat = book.OfWare(Ware.Water)?.CatId ?? 0; return true; } return false; // the catalogue's Water rows (drink, wash); salt is refused by the handler
                case TargetKind.Lot: case TargetKind.Fixture:
                {
                    if (!LocateLot(s, id, out var node, out var ware)) return false;
                    r.Node = node; r.Cat = book.OfWare(ware)?.CatId ?? 0; return true;
                }
                default: return false;
            }
        }

        /// <summary>A lot anywhere (ground, bin, carry): its node and ware.</summary>
        public static bool LocateLot(GodState s, int lot, out int node, out Ware ware)
        {
            node = -1; ware = Ware.None;
            if (lot <= 0) return false;
            var p = s.Population;
            foreach (var g in p.Lots) if (g.Lot.Id == lot && !g.Lot.Empty) { node = g.Node; ware = g.Lot.Ware; return true; }
            foreach (var b in p.Bins) foreach (var l in b.Lots) if (l.Id == lot && !l.Empty) { node = b.Node; ware = l.Ware; return true; }
            foreach (var n in p.Npcs) if (n.Carry != null) foreach (var l in n.Carry) if (!l.Empty && l.Id == lot) { node = n.Node; ware = l.Ware; return true; }
            return false;
        }

        /// <summary>True when the source lot (default when 0: the empty hand) meets the row's requirement.</summary>
        public static bool SourceMeets(InteractionBook book, InteractionDef row, int sourceLot, Ware ware)
        {
            switch (row.Source)
            {
                case SourceReq.EmptyHand: return sourceLot == 0;
                case SourceReq.Any: return true;
                case SourceReq.Ware: return sourceLot != 0 && (int)ware == row.SourceArg;
                case SourceReq.Tool: return sourceLot != 0 && (book.OfWare(ware)?.CatId ?? 0) == row.SourceArg;
                case SourceReq.Class:
                {
                    if (sourceLot == 0) return false;
                    var o = book.OfWare(ware);
                    switch (row.SourceArg)
                    {
                        case SourceClass.Food: return Wares.IsFood(ware);
                        case SourceClass.Gear: return Wares.IsGear(ware);
                        case SourceClass.Tool: return o != null && o.ToolMultiplier.Count > 0;
                        case SourceClass.Strike: return o != null && o.StrikeAttack > 0;
                        default: return false;
                    }
                }
                default: return false;
            }
        }

        /// <summary>The matching row index of the book (-1 none): exact catalogue id first, then the kind's wildcard rows.</summary>
        public static int Match(InteractionBook book, TargetKind kind, ushort cat, byte slot, int sourceLot, Ware ware, out bool anyRow)
        {
            anyRow = false;
            for (int pass = 0; pass < 2; pass++)
            {
                ushort key = pass == 0 ? cat : (ushort)0;
                if (pass == 1 && cat == 0) break;
                int first = book.RowsOf(kind, key, out int n);
                for (int i = first; i < first + n; i++)
                {
                    var row = book.Row(i);
                    anyRow = true;
                    if (slot != 0 && row.Slot != slot) continue;
                    if (SourceMeets(book, row, sourceLot, ware)) return i;
                }
            }
            return -1;
        }

        /// <summary>The row's duration for this source: ticks x the tool's permille for the first effect, at least 1.</summary>
        public static int Duration(InteractionBook book, InteractionDef row, Ware sourceWare, int sourceLot)
        {
            long t = row.Ticks;
            if (sourceLot != 0 && row.Effects.Count > 0)
            {
                var o = book.OfWare(sourceWare);
                if (o != null)
                    foreach (var m in o.ToolMultiplier)
                        if (m.Effect == row.Effects[0].Kind) { t = t * m.Permille / 1000; break; }
            }
            return t < 1 ? 1 : (int)System.Math.Min(int.MaxValue, t);
        }

        private static EffectCall CallOf(int actor, in PrimitiveControl c, ControlOrigin origin, InteractionDef row, int effect, int progress) =>
            new EffectCall
            {
                Actor = actor, Control = c, Origin = origin, Row = row, Effect = row.Effects.Count > effect ? row.Effects[effect] : default,
                Target = c.Target, SourceLot = c.SourceLot, Progress = progress,
            };

        /// <summary>USE: match, reach, validate; with <paramref name="apply"/> the action starts (nothing applies before completion).</summary>
        public static MotorStatus Start(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            var book = s.Unification.Book;
            Ware ware = Ware.None;
            if (c.SourceLot != 0)
            {
                int slot = Motor.CarrySlotOf(npc, c.SourceLot);
                if (slot < 0) return MotorStatus.Invalid;
                ware = npc.Carry[slot].Ware;
            }
            if (c.Target.Kind == TargetKind.Placement && !Knowledge.KnowsDesign(u.Brain, c.Design)) return MotorStatus.NotKnown;
            if (!ResolveTarget(s, npc, u, in c, out var r)) return MotorStatus.Invalid;
            int row = Match(book, c.Target.Kind, r.Cat, c.Slot, c.SourceLot, ware, out _);
            if (row < 0) return MotorStatus.NoInteraction;
            var def = book.Row(row);
            if (s.Territory.Grid.Distance(npc.Node, r.Node) > def.Reach) return MotorStatus.OutOfReach;
            var table = s.Unification.Effects;
            for (int e = 0; e < def.Effects.Count; e++)
            {
                var call = CallOf(npc.Id, in c, origin, def, e, 0);
                var st = table.Validate(s, in call);
                if (st != MotorStatus.Accepted) return st;
            }
            if (!apply) return MotorStatus.Accepted;
            int ticks = Duration(book, def, ware, c.SourceLot);
            Motor.Start(s, u, Prim.Use, ticks, c.Target, origin, true, c.SourceLot, c.Slot);
            u.ActionDesign = c.Design; u.ActionCat = r.Cat; u.ActionRow = row;
            var first = CallOf(npc.Id, in c, origin, def, 0, 0);
            UnifiedHooks.OnInteractionStarted(s, ev, in first);
            return MotorStatus.Accepted;
        }

        /// <summary>The USE ran its ticks: every effect applies now, in row order; the first refusal is the result.</summary>
        public static MotorStatus Complete(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u)
        {
            var book = s.Unification.Book;
            if (u.ActionRow < 0 || u.ActionRow >= book.RowCount) return MotorStatus.NoInteraction;
            var def = book.Row(u.ActionRow);
            var c = new PrimitiveControl { Control = Prim.Use, Actor = npc.Id, SourceLot = u.ActionSource, Target = u.ActionTarget, Slot = u.ActionSlot, Design = u.ActionDesign };
            var table = s.Unification.Effects;
            var result = MotorStatus.Accepted;
            for (int e = 0; e < def.Effects.Count; e++)
            {
                var call = CallOf(npc.Id, in c, u.ActionOrigin, def, e, u.ActionTotal);
                var st = table.Apply(s, ev, in call);
                if (st != MotorStatus.Accepted) { result = st; break; } // a refused effect stops the row: later effects do not apply
            }
            var done = CallOf(npc.Id, in c, u.ActionOrigin, def, 0, u.ActionTotal);
            UnifiedHooks.OnInteractionCompleted(s, ev, in done, result);
            s.Unification.Index.MarkDirty();
            return result;
        }
    }
}


namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.7): the resolver's immutable view of the catalogue: object and building definitions by <see cref="CatId"/>,
    /// objects by ware, and every <see cref="InteractionDef"/> row of every definition in one table sorted by (target kind, target
    /// catalogue id), declaration order kept within a key (objects by CatId, then buildings by CatId, then extra rows). Lookups are
    /// binary searches over sorted arrays; nothing is a dictionary. <see cref="Default"/> is the generated <see cref="Catalogue"/>
    /// (empty until UN3a); tests build their own from code-defined rows.
    /// </summary>
    public sealed class InteractionBook
    {
        public static readonly InteractionBook Default = new InteractionBook(Catalogue.Objects, Catalogue.Buildings);

        private readonly ReadOnlyArray<ObjectDef> _objects;   // by CatId
        private readonly ReadOnlyArray<ObjectDef> _byWare;    // by ware (first CatId of a ware wins)
        private readonly ReadOnlyArray<BuildingDef> _buildings; // by CatId
        private readonly ReadOnlyArray<InteractionDef> _rows;  // by (kind, cat), stable
        private readonly ReadOnlyArray<int> _keys;             // kind << 16 | cat, parallel to _rows

        public InteractionBook(IEnumerable<ObjectDef> objects, IEnumerable<BuildingDef> buildings, params InteractionDef[] extraRows)
        {
            var o = new List<ObjectDef>(); if (objects != null) foreach (var x in objects) if (x != null) o.Add(x);
            var b = new List<BuildingDef>(); if (buildings != null) foreach (var x in buildings) if (x != null) b.Add(x);
            o.Sort((x, y) => x.CatId.CompareTo(y.CatId));
            b.Sort((x, y) => x.CatId.CompareTo(y.CatId));
            var w = new List<ObjectDef>(o);
            Stable(w, (x, y) => ((int)x.Ware).CompareTo((int)y.Ware));
            var rows = new List<InteractionDef>();
            foreach (var x in o) foreach (var r in x.Interactions) rows.Add(r);
            foreach (var x in b) foreach (var r in x.Interactions) rows.Add(r);
            if (extraRows != null) foreach (var r in extraRows) if (r != null) rows.Add(r);
            Stable(rows, (x, y) => Key(x.TargetKind, x.TargetCat).CompareTo(Key(y.TargetKind, y.TargetCat)));
            var keys = new int[rows.Count];
            for (int i = 0; i < keys.Length; i++) keys[i] = Key(rows[i].TargetKind, rows[i].TargetCat);
            _objects = new ReadOnlyArray<ObjectDef>(o); _byWare = new ReadOnlyArray<ObjectDef>(w); _buildings = new ReadOnlyArray<BuildingDef>(b);
            _rows = new ReadOnlyArray<InteractionDef>(rows); _keys = new ReadOnlyArray<int>(keys);
        }

        private static int Key(TargetKind k, ushort cat) => ((int)k << 16) | cat;

        /// <summary>A stable insertion sort (List.Sort is not stable).</summary>
        private static void Stable<T>(List<T> a, System.Comparison<T> cmp)
        {
            for (int i = 1; i < a.Count; i++)
            {
                var x = a[i]; int j = i - 1;
                while (j >= 0 && cmp(a[j], x) > 0) { a[j + 1] = a[j]; j--; }
                a[j + 1] = x;
            }
        }

        public int RowCount => _rows.Count;
        public InteractionDef Row(int i) => _rows[i];

        /// <summary>The first row index of (kind, cat), and how many rows share that key (0 when none).</summary>
        public int RowsOf(TargetKind kind, ushort cat, out int count)
        {
            int key = Key(kind, cat);
            int lo = 0, hi = _keys.Count;
            while (lo < hi) { int mid = (lo + hi) >> 1; if (_keys[mid] < key) lo = mid + 1; else hi = mid; }
            int end = lo;
            while (end < _keys.Count && _keys[end] == key) end++;
            count = end - lo;
            return lo;
        }

        public ObjectDef Object(ushort id)
        {
            int lo = 0, hi = _objects.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; var v = _objects[mid].CatId; if (v == id) return _objects[mid]; if (v < id) lo = mid + 1; else hi = mid - 1; }
            return null;
        }

        public BuildingDef Building(ushort id)
        {
            int lo = 0, hi = _buildings.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; var v = _buildings[mid].CatId; if (v == id) return _buildings[mid]; if (v < id) lo = mid + 1; else hi = mid - 1; }
            return null;
        }

        /// <summary>The object definition of a ware (the lowest CatId of that ware), or null.</summary>
        public ObjectDef OfWare(Ware w)
        {
            int lo = 0, hi = _byWare.Count;
            while (lo < hi) { int mid = (lo + hi) >> 1; if ((int)_byWare[mid].Ware < (int)w) lo = mid + 1; else hi = mid; }
            return lo < _byWare.Count && _byWare[lo].Ware == w ? _byWare[lo] : null;
        }
    }
}
