using God.Sim.Population;


namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.6, D9.6, D10): the simple, correct <see cref="ISpatialIndex"/>. One bucket per lattice node over living persons,
    /// ground lots, buildings, sites, raw nodes, forage places, water points and installed fixtures, laid out as one sorted array
    /// (counting sort by node, stable over sources visited in kind then id order, so each node's entries are by kind then id). A query
    /// walks hex rings outwards from its node and touches only the cells within its radius: no all-pairs scan, no dictionary, no
    /// allocation once the buffers have grown. A cache: rebuilt from the state (<see cref="Ensure"/>: once per tick, or after the motor
    /// moved goods), never saved, never hashed. UNP may replace it behind the interface.
    /// </summary>
    public sealed class SimpleSpatialIndex : ISpatialIndex
    {
        private int[] _start = new int[1];
        private SpatialKind[] _kind = new SpatialKind[64];
        private int[] _id = new int[64], _node = new int[64], _tmpNode = new int[64], _tmpId = new int[64];
        private SpatialKind[] _tmpKind = new SpatialKind[64];
        private int[] _count = new int[1];
        private SpatialHit[] _ring = new SpatialHit[64];
        private int _entries, _nodes;
        private long _builtTick = long.MinValue;
        private bool _dirty = true;

        /// <summary>Instrumentation (never state): lattice cells a query looked at, entity tests made, rebuilds done.</summary>
        public long CellsTouched, EntityTests, Rebuilds;

        /// <summary>Marks the cache stale (the motor moved goods or a body this tick).</summary>
        public void MarkDirty() => _dirty = true;

        /// <summary>Forgets the cache entirely (a building or site appeared outside the tick order: hosts and fixtures).</summary>
        public void Reset() { _dirty = true; _builtTick = long.MinValue; }

        /// <summary>Rebuilds when the tick changed since the last build or the cache was marked stale.</summary>
        public void Ensure(GodState s)
        {
            if (_dirty || _builtTick != s.Tick) Rebuild(s);
        }

        public void Rebuild(GodState s)
        {
            Rebuilds++;
            _builtTick = s.Tick; _dirty = false;
            var g = s.Territory.Grid;
            _nodes = g.Count;
            if (_start.Length < _nodes + 1) { _start = new int[_nodes + 1]; _count = new int[_nodes + 1]; }
            _entries = 0;
            var p = s.Population;
            var u = s.Unification;
            if (p != null)
                foreach (var n in p.Npcs) if (Bodies.IsLivingPerson(n)) Add(SpatialKind.Npc, n.Id, n.Node);
            if (p != null)
                foreach (var gl in p.Lots) if (!gl.Lot.Empty) Add(SpatialKind.Lot, gl.Lot.Id, gl.Node);
            if (u != null)
            {
                foreach (var b in u.Buildings) Add(SpatialKind.Building, b.Id, b.Node);
                foreach (var x in u.Sites) Add(SpatialKind.Site, x.Id, x.Node);
                foreach (var x in u.RawNodes) Add(SpatialKind.RawNode, x.Id, x.Node);
                foreach (var x in u.ForagePlaces) Add(SpatialKind.ForagePlace, x.Id, x.Node);
                foreach (var x in u.WaterPoints) Add(SpatialKind.WaterPoint, x.Id, x.Node);
                if (p != null)
                    foreach (var b in u.Buildings)
                    {
                        if (b.Fixtures == 0) continue;
                        var bin = Goods.BinById(p, b.Fixtures);
                        if (bin == null) continue;
                        foreach (var l in bin.Lots) if (!l.Empty) Add(SpatialKind.Fixture, l.Id, b.Node);
                    }
            }
            // stable counting sort by node
            for (int i = 0; i <= _nodes; i++) _count[i] = 0;
            for (int i = 0; i < _entries; i++) _count[_tmpNode[i] + 1]++;
            for (int i = 0; i < _nodes; i++) _count[i + 1] += _count[i];
            for (int i = 0; i <= _nodes; i++) _start[i] = _count[i];
            if (_kind.Length < _entries) { _kind = new SpatialKind[_tmpKind.Length]; _id = new int[_tmpId.Length]; _node = new int[_tmpNode.Length]; }
            for (int i = 0; i < _entries; i++)
            {
                int at = _count[_tmpNode[i]]++;
                _kind[at] = _tmpKind[i]; _id[at] = _tmpId[i]; _node[at] = _tmpNode[i];
            }
            // the ring buffer of the widest query seen so far, at the densest cell: queries never grow it (no allocation in a query)
            int dense = 0;
            for (int i = 0; i < _nodes; i++) { int c = _start[i + 1] - _start[i]; if (c > dense) dense = c; }
            long need = (long)dense * 6 * System.Math.Max(1, _maxRadius);
            if (need > _ring.Length) _ring = new SpatialHit[(int)System.Math.Min(need, _entries + 64)];
        }

        private int _maxRadius = 10;

        private void Add(SpatialKind k, int id, int node)
        {
            if (node < 0 || node >= _nodes) return;
            if (_entries == _tmpId.Length)
            {
                int n = _tmpId.Length * 2;
                System.Array.Resize(ref _tmpId, n); System.Array.Resize(ref _tmpNode, n); System.Array.Resize(ref _tmpKind, n);
            }
            _tmpKind[_entries] = k; _tmpId[_entries] = id; _tmpNode[_entries] = node; _entries++;
        }

        public int At(GodState s, int node, SpatialKind kinds, SpatialHit[] into)
        {
            Ensure(s);
            if (node < 0 || node >= _nodes) return 0;
            CellsTouched++;
            int n = 0;
            for (int e = _start[node]; e < _start[node + 1] && n < into.Length; e++)
            {
                EntityTests++;
                if ((_kind[e] & kinds) == 0) continue;
                into[n++] = new SpatialHit { Kind = _kind[e], Id = _id[e], Node = node, Distance = 0 };
            }
            return n;
        }

        /// <summary>
        /// True when anything of <paramref name="kinds"/> is at <paramref name="node"/>. For the static kinds only (buildings, sites,
        /// world entities) it rebuilds on a new tick but not after bodies moved: every completing step asks it.
        /// </summary>
        public bool Any(GodState s, int node, SpatialKind kinds)
        {
            if (_builtTick != s.Tick || _dirty && (kinds & (SpatialKind.Npc | SpatialKind.Lot | SpatialKind.Fixture)) != 0) Rebuild(s);
            if (node < 0 || node >= _nodes) return false;
            for (int e = _start[node]; e < _start[node + 1]; e++) if ((_kind[e] & kinds) != 0) return true;
            return false;
        }

        public bool Walkable(GodState s, int node) { var g = s.Territory.Grid; return node >= 0 && node < g.Count && g.IsLand(node); }

        public int Nearest(GodState s, int node, int radius, SpatialKind kinds, SpatialHit[] into)
        {
            Ensure(s);
            var g = s.Territory.Grid;
            if (!g.IsValid(node) || into.Length == 0) return 0;
            if (radius > _maxRadius) _maxRadius = radius; // the next rebuild sizes the ring for it
            int n = 0;
            int ci = g.I(node), cj = g.J(node);
            for (int d = 0; d <= radius && n < into.Length; d++)
            {
                int r = 0;
                if (d == 0) r = Collect(node, 0, kinds, r);
                else
                {
                    int i = ci + NodeGrid.DI[4] * d, j = cj + NodeGrid.DJ[4] * d;
                    for (int side = 0; side < 6; side++)
                        for (int k = 0; k < d; k++)
                        {
                            r = Collect(g.Id(i, j), d, kinds, r);
                            i += NodeGrid.DI[side]; j += NodeGrid.DJ[side];
                        }
                }
                // this ring by kind then id (insertion sort; rings are small), then as many as fit
                for (int a = 1; a < r; a++)
                {
                    var x = _ring[a]; int b = a - 1;
                    while (b >= 0 && (_ring[b].Kind > x.Kind || _ring[b].Kind == x.Kind && _ring[b].Id > x.Id)) { _ring[b + 1] = _ring[b]; b--; }
                    _ring[b + 1] = x;
                }
                for (int a = 0; a < r && n < into.Length; a++) into[n++] = _ring[a];
            }
            return n;
        }

        private int Collect(int cell, int d, SpatialKind kinds, int r)
        {
            CellsTouched++;
            if (cell < 0 || cell >= _nodes) return r;
            for (int e = _start[cell]; e < _start[cell + 1]; e++)
            {
                EntityTests++;
                if ((_kind[e] & kinds) == 0) continue;
                if (r == _ring.Length) System.Array.Resize(ref _ring, r * 2);
                _ring[r++] = new SpatialHit { Kind = _kind[e], Id = _id[e], Node = cell, Distance = d };
            }
            return r;
        }

        public int NodesWithin(GodState s, int node, int radius, int[] into)
        {
            var g = s.Territory.Grid;
            if (!g.IsValid(node) || into.Length == 0) return 0;
            int n = 0;
            into[n++] = node;
            int ci = g.I(node), cj = g.J(node);
            for (int d = 1; d <= radius && n < into.Length; d++)
            {
                // a ring's nodes by id (the interface's order: distance, then id)
                int first = n;
                int i = ci + NodeGrid.DI[4] * d, j = cj + NodeGrid.DJ[4] * d;
                for (int side = 0; side < 6; side++)
                    for (int k = 0; k < d; k++)
                    {
                        CellsTouched++;
                        int id = g.Id(i, j);
                        if (id >= 0 && n < into.Length) into[n++] = id;
                        i += NodeGrid.DI[side]; j += NodeGrid.DJ[side];
                    }
                for (int a = first + 1; a < n; a++) { int x = into[a]; int b = a - 1; while (b >= first && into[b] > x) { into[b + 1] = into[b]; b--; } into[b + 1] = x; }
            }
            return n;
        }
    }
}
