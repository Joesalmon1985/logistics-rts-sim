namespace God.Sim.Unified
{
    /// <summary>
    /// The network's input map (slice 1): how an <c>Obs v1</c> value becomes a network input, per vector index. Obs v1 keeps several
    /// fields as small raw integers (relative sectors 0-6, lattice distances, last-result and control codes, the small classes
    /// person 1 / fresh water 2 / salt water 3, the entity state bit field), while the network reads every input on the permille
    /// scale (<c>value / 1000</c>): such fields were below the network's resolution (a sector of 3 is an input of 0.003). Map 1
    /// rescales them to permille without changing the observation (UN2's percept, its schema and hash, and every reader of the
    /// raw vector stay as they are). Map 0 is the raw vector (artifacts written before the map existed name no map and run raw).
    /// <para>Map 1: a relative sector becomes a signed turn (0 ahead 0; 1, 2, 3 right +333, +667, +1000; 5, 4 left -333, -667;
    /// 6 same node 0); a distance x100; a small class (1-99) x300; the entity bits become kind x100 plus 1000 when in reach;
    /// control x80, last result x60, age band x300, terrain x100, carried count x100, gesture codes x60. Integer, deterministic,
    /// the same table for the game's <see cref="BrainNet"/> and the trainer's networks.</para>
    /// </summary>
    public static class BrainInput
    {
        public const int MapRaw = 0, MapV1 = 1, MapV2 = 2, Current = MapV2;
        /// <summary>Map 2's derived inputs after the 284 observation values (a network of map 2 has In = 284 + Derived).</summary>
        public const int Derived = 14;

        private const byte Raw = 0, Turn = 1, Dist = 2, Small = 3, Bits = 4, X100 = 5, X300 = 6, X80 = 7, X60 = 8;
        private static readonly ReadOnlyArray<byte> Kind = new ReadOnlyArray<byte>(Build());

        private static byte[] Build()
        {
            var k = new byte[ObsSchema.Length];
            k[ObsSchema.OffsetOf("control")] = X80;
            k[ObsSchema.OffsetOf("last_result")] = X60;
            k[ObsSchema.OffsetOf("age_band")] = X300;
            int carry = ObsSchema.OffsetOf("carry");
            for (int i = 0; i < ObsSchema.CarrySlots; i++) k[carry + i * ObsSchema.CarryFields + 1] = X100;
            int cells = ObsSchema.OffsetOf("cells");
            for (int i = 0; i < ObsSchema.Cells; i++) { int o = cells + i * ObsSchema.CellFields; k[o + 1] = X100; k[o + 2] = Small; }
            int ent = ObsSchema.OffsetOf("entities");
            for (int i = 0; i < ObsSchema.Entities; i++) { int o = ent + i * ObsSchema.EntityFields; k[o] = Small; k[o + 1] = Turn; k[o + 2] = Dist; k[o + 3] = Bits; }
            int ges = ObsSchema.OffsetOf("gestures");
            for (int i = 0; i < ObsSchema.Gestures; i++) { int o = ges + i * ObsSchema.GestureFields; k[o] = X60; k[o + 1] = X60; k[o + 2] = Small; }
            int pl = ObsSchema.OffsetOf("places");
            for (int i = 0; i < ObsSchema.Places; i++) { int o = pl + i * ObsSchema.PlaceFields; k[o] = Small; k[o + 1] = Turn; k[o + 2] = Dist; }
            return k;
        }

        /// <summary>The network input of value <paramref name="v"/> at vector index <paramref name="i"/> under <paramref name="map"/>, clamped to +-<paramref name="clamp"/>.</summary>
        public static int Apply(int map, int i, int v, int clamp)
        {
            if (map >= MapV1 && i < Kind.Count)
            {
                switch (Kind[i])
                {
                    case Turn: v = v == 1 ? 333 : v == 2 ? 667 : v == 3 ? 1000 : v == 4 ? -667 : v == 5 ? -333 : 0; break;
                    case Dist: v = v * 100; break;
                    case Small: if (v > 0 && v < 100) v *= 300; break;
                    case Bits: v = ((v & 512) != 0 ? 1000 : 0) + (v & 15) * 100; break;
                    case X100: v *= 100; break;
                    case X300: v *= 300; break;
                    case X80: v *= 80; break;
                    case X60: v *= 60; break;
                }
            }
            return v < -clamp ? -clamp : v > clamp ? clamp : v;
        }

        /// <summary>
        /// The <paramref name="n"/> network inputs of one observation into <paramref name="x"/>: the mapped observation values, then
        /// (map 2) the derived inputs: for fresh water, then for an edible forage place, the nearest one the body perceives (else
        /// the nearest it remembers): present, ahead, to the right, to the left (1000 or 0), distance x100, in reach (1000 or 0),
        /// and the perceived entity slot (slot + 1) x60 (0 when only remembered). A pure function of the observation and the
        /// catalogue (which classes are food); no world query, no allocation.
        /// </summary>
        public static void Fill(int map, short[] obs, int[] x, int n, int clamp)
        {
            int m = n < obs.Length ? n : obs.Length;
            for (int i = 0; i < m; i++) x[i] = Apply(map, i, obs[i], clamp);
            for (int i = m; i < n; i++) x[i] = 0;
            if (map != MapV2 || n < obs.Length + Derived) return;
            Nearest(obs, true, x, obs.Length);
            Nearest(obs, false, x, obs.Length + 7);
        }

        private static readonly int OEnt = ObsSchema.OffsetOf("entities"), OPl = ObsSchema.OffsetOf("places");

        private static void Nearest(short[] obs, bool water, int[] x, int at)
        {
            int best = -1, bestD = int.MaxValue, sector = 0; bool remembered = false;
            for (int e = 0; e < ObsSchema.Entities; e++)
            {
                int o = OEnt + e * ObsSchema.EntityFields, cls = obs[o];
                if (cls == 0) break;
                bool hit = water ? cls == Percept.ClassFreshWater : (obs[o + 3] & 15) == 6 && Edible(cls); // kind code 6 = ForagePlace
                if (hit && obs[o + 2] < bestD) { best = e; bestD = obs[o + 2]; sector = obs[o + 1]; }
            }
            if (best < 0)
                for (int k = 0; k < ObsSchema.Places; k++)
                {
                    int o = OPl + k * ObsSchema.PlaceFields, cls = obs[o];
                    if (cls == 0) continue;
                    bool hit = water ? cls == Percept.ClassFreshWater : Edible(cls);
                    if (hit && obs[o + 2] < bestD) { bestD = obs[o + 2]; sector = obs[o + 1]; remembered = true; best = 99; }
                }
            if (best < 0) return;
            x[at] = 1000;
            x[at + 1] = sector == 0 ? 1000 : 0;
            x[at + 2] = sector >= 1 && sector <= 3 ? 1000 : 0;
            x[at + 3] = sector == 4 || sector == 5 ? 1000 : 0;
            int d = bestD * 100; x[at + 4] = d > 1000 ? 1000 : d;
            x[at + 5] = bestD <= God.Sim.Population.Reach.Touch ? 1000 : 0;
            x[at + 6] = remembered ? 0 : (best + 1) * 60;
        }

        /// <summary>A class that is an edible forage food (a catalogue object foraged from a place, with nutrition).</summary>
        private static bool Edible(int cls)
        {
            if (cls <= 0 || cls > ushort.MaxValue) return false;
            var o = Catalogue.Object((ushort)cls);
            if (o == null || o.Nutrition <= 0) return false;
            for (int i = 0; i < o.Interactions.Count; i++) if (o.Interactions[i].TargetKind == TargetKind.ForagePlace) return true;
            return false;
        }
    }
}
