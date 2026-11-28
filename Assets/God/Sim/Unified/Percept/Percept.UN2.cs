using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.6, D9.2 a, QA-02): the observation encoder <c>Obs v1</c>. <see cref="Encode"/> fills the vector from what the
    /// body can perceive: its own body and carry, the six cells around it, the 16 nearest entities within sight through
    /// <see cref="ISpatialIndex"/> (sorted by distance, then kind, then id), the gestures in its inbox, its own memory and knowledge
    /// bits, and divine inputs (zero when divinity is off). No truth query, no owner field, no path, no store contents beyond reach:
    /// a bin's lots are not entities, a building in reach shows only whether its store holds anything. Ids are never in the vector;
    /// they stay beside it (<see cref="EntityId"/>) so a decision's target slot can be turned back into a <see cref="TargetRef"/>.
    /// Allocation-free once the buffers exist (the first <see cref="Encode"/> of a value makes them).
    /// <para>Entity fields: class (a catalogue id; a ware without one is 3000 + ware; a person 1; fresh water 2, salt water 3), the
    /// sector relative to the facing (0-5, 6 = the same node), the lattice distance, state bits (<see cref="Bits"/>), then kin,
    /// familiarity, trust band and aggression seen (0 until UN6/UN7/UN8a give them meaning; trust from the body's own memory).</para>
    /// </summary>
    public partial struct Percept
    {
        /// <summary>The state bits of an entity: the kind (bits 0-3, <see cref="KindCode"/>), then flags.</summary>
        public static class Bits
        {
            public const int Asleep = 16, Busy = 32, Carrying = 64, ResistedMe = 128, HeldOutToMe = 256, InReach = 512, StoreHolds = 1024;
        }

        public const int ClassPerson = 1, ClassFreshWater = 2, ClassSaltWater = 3, ClassWareBase = 3000;

        /// <summary>The encoded vector (<see cref="ObsSchema.Length"/> values).</summary>
        public short[] V;
        /// <summary>The perceived entities in vector order (<see cref="EntityCount"/> of <see cref="ObsSchema.Entities"/>).</summary>
        public int EntityCount;
        public SpatialKind[] EntityKind;
        public int[] EntityId, EntityNode;
        /// <summary>The carried lot in each carry slot of the vector (0 = empty).</summary>
        public int[] CarryLot;
        /// <summary>The body's node and facing when encoded; the sight radius used.</summary>
        public int Node, Sight;
        public byte Facing;
        private SpatialHit[] _hits;

        private static readonly int OFacing = ObsSchema.OffsetOf("facing"), OControl = ObsSchema.OffsetOf("control"), OProgress = ObsSchema.OffsetOf("progress"),
            OLast = ObsSchema.OffsetOf("last_result"), OAsleep = ObsSchema.OffsetOf("asleep"), OOccupying = ObsSchema.OffsetOf("occupying"),
            OEquipped = ObsSchema.OffsetOf("equipped"), ONeeds = ObsSchema.OffsetOf("needs"), OHealth = ObsSchema.OffsetOf("health"),
            OInjury = ObsSchema.OffsetOf("injury"), OAge = ObsSchema.OffsetOf("age_band"), OTraits = ObsSchema.OffsetOf("traits"),
            OCarry = ObsSchema.OffsetOf("carry"), OCells = ObsSchema.OffsetOf("cells"), OEntities = ObsSchema.OffsetOf("entities"),
            OGestures = ObsSchema.OffsetOf("gestures"), OPlaces = ObsSchema.OffsetOf("places"), OPeople = ObsSchema.OffsetOf("people"),
            OClaims = ObsSchema.OffsetOf("claims"), ODesigns = ObsSchema.OffsetOf("know_designs"), ORecipes = ObsSchema.OffsetOf("know_recipes"),
            OInteractions = ObsSchema.OffsetOf("know_interactions"), ODivine = ObsSchema.OffsetOf("divine");

        private void EnsureBuffers()
        {
            if (V == null || V.Length != ObsSchema.Length) V = new short[ObsSchema.Length];
            if (EntityKind == null) { EntityKind = new SpatialKind[ObsSchema.Entities]; EntityId = new int[ObsSchema.Entities]; EntityNode = new int[ObsSchema.Entities]; }
            if (CarryLot == null) CarryLot = new int[ObsSchema.CarrySlots];
            if (_hits == null) _hits = new SpatialHit[ObsSchema.Entities + 1];
        }

        /// <summary>The 1-15 code of a spatial kind (its bit position + 1).</summary>
        public static int KindCode(SpatialKind k)
        {
            int c = 1; int v = (int)k;
            while (v > 1) { v >>= 1; c++; }
            return k == SpatialKind.None ? 0 : c;
        }

        /// <summary>The target kind of a perceived entity kind.</summary>
        public static TargetKind TargetKindOf(SpatialKind k)
        {
            switch (k)
            {
                case SpatialKind.Npc: return TargetKind.Npc;
                case SpatialKind.Lot: return TargetKind.Lot;
                case SpatialKind.Building: return TargetKind.Building;
                case SpatialKind.Site: return TargetKind.Site;
                case SpatialKind.RawNode: return TargetKind.RawNode;
                case SpatialKind.ForagePlace: return TargetKind.ForagePlace;
                case SpatialKind.WaterPoint: return TargetKind.WaterPoint;
                case SpatialKind.Fixture: return TargetKind.Fixture;
                default: return TargetKind.None;
            }
        }

        /// <summary>The target of option <paramref name="option"/> of the <c>target</c> head (16 entity slots, self, the facing cell, none).</summary>
        public static TargetRef TargetOf(in Percept p, int option)
        {
            if (option >= 0 && option < p.EntityCount) return new TargetRef(TargetKindOf(p.EntityKind[option]), p.EntityId[option]);
            if (option == ObsSchema.Entities) return TargetRef.Self;
            if (option == ObsSchema.Entities + 1) return new TargetRef(TargetKind.Facing, 0);
            return TargetRef.None;
        }

        /// <summary>The source lot of option <paramref name="option"/> of the <c>source</c> head (4 carry slots, then the empty hand = 0).</summary>
        public static int SourceOf(in Percept p, int option) => option >= 0 && option < ObsSchema.CarrySlots && p.CarryLot != null ? p.CarryLot[option] : 0;

        /// <summary>The quantity of each of the 16 buckets of the <c>quantity</c> head (a default: UN6 may retune by a new Act version).</summary>
        public static readonly ReadOnlyArray<short> QuantityBuckets = new ReadOnlyArray<short>(0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 20, 30, 50, 100, 999);

        /// <summary>The referent of option <paramref name="option"/> of a <c>referent</c> head (16 entity slots, 4 carry slots, self, none).</summary>
        public static Referent ReferentOf(GodState s, in Percept p, int option)
        {
            var r = default(Referent);
            if (option >= 0 && option < p.EntityCount)
            {
                switch (p.EntityKind[option])
                {
                    case SpatialKind.Npc: r.Kind = ReferentKind.Person; r.Id = p.EntityId[option]; break;
                    case SpatialKind.Lot: r.Kind = ReferentKind.Lot; r.Id = p.EntityId[option]; r.Ware = (short)GroundWare(s.Population, p.EntityId[option]); break;
                    default: r.Kind = ReferentKind.Place; r.Id = p.EntityNode[option]; break;
                }
            }
            else if (option >= ObsSchema.Entities && option < ObsSchema.Entities + ObsSchema.CarrySlots)
            {
                int lot = SourceOf(in p, option - ObsSchema.Entities);
                if (lot != 0)
                {
                    r.Kind = ReferentKind.Lot; r.Id = lot;
                    var n = s.Population.Find(p.Actor);
                    r.Ware = (short)(n != null ? Motor.WareOfLot(s, n, lot) : 0);
                }
            }
            else if (option == ObsSchema.Entities + ObsSchema.CarrySlots) { r.Kind = ReferentKind.Person; r.Id = p.Actor; }
            return r;
        }

        /// <summary>The design of option <paramref name="option"/> of the <c>design</c> head: the i-th design the body knows (by row), 0 for none.</summary>
        public static ushort DesignOf(BrainState b, int option)
        {
            if (b == null || option < 0 || option >= 32) return 0;
            int i = 0;
            for (int row = 0; row < BrainState.DesignBits; row++)
                if (Knowledge.Knows(b, KnowledgeKind.Design, row)) { if (i == option) return CatId.Building(row); i++; }
            return 0;
        }

        /// <summary>
        /// Decodes one factorised Act v1 choice (one option per head) into the <see cref="PrimitiveControl"/> the motor takes: the
        /// percept's entity slots become targets and referents, carry slots become the source lot, the design option the known design.
        /// Fields a control does not use are left at their defaults.
        /// </summary>
        public static PrimitiveControl ControlOf(GodState s, in Percept p, int control, int target, int source, int slot, int design, int gesture,
            int referent1, int referent2, int quantity)
        {
            var c = new PrimitiveControl { Control = (Prim)(control < 0 || control > (int)Prim.Wait ? 0 : control), Actor = p.Actor };
            c.Target = TargetOf(in p, target);
            c.SourceLot = SourceOf(in p, source);
            c.Slot = (byte)(slot < 0 || slot > 3 ? 0 : slot);
            c.Design = DesignOf(s.Unification?.PersonOf(p.Actor)?.Brain, design);
            if (c.Design != 0 && c.Control == Prim.Use && c.Target.Kind == TargetKind.Facing)
            {
                int node = s.Territory.Grid.Neighbour(p.Node, p.Facing);
                c.Target = new TargetRef(TargetKind.Placement, node);
            }
            if (c.Control == Prim.Gesture)
            {
                c.Gesture = (byte)(gesture < 0 || gesture > 31 ? 0 : gesture);
                c.R1 = ReferentOf(s, in p, referent1); c.R2 = ReferentOf(s, in p, referent2);
                c.Quantity = quantity >= 0 && quantity < QuantityBuckets.Count ? QuantityBuckets[quantity] : (short)0;
            }
            else if (c.Control == Prim.Take || c.Control == Prim.Drop)
                c.Quantity = quantity > 0 && quantity < QuantityBuckets.Count ? QuantityBuckets[quantity] : (short)0;
            return c;
        }

        /// <summary>The class of a ware (its catalogue id, or 3000 + ware).</summary>
        public static int ClassOfWare(InteractionBook book, Ware w)
        {
            if (w == Ware.None) return 0;
            var o = book.OfWare(w);
            return o != null ? o.CatId : ClassWareBase + (int)w;
        }

        /// <summary>The sector (0-5, world) of the lattice vector from <paramref name="a"/> to <paramref name="b"/>, 6 when equal: the nearest of the six directions (integer cube dot product, ties by the lower direction).</summary>
        public static int SectorOf(NodeGrid g, int a, int b)
        {
            int di = g.I(b) - g.I(a), dj = g.J(b) - g.J(a);
            if (di == 0 && dj == 0) return 6;
            // cube (x, y, z) = (di, -di-dj, dj); the cube metric is an isometric embedding of the lattice
            int x = di, y = -di - dj, z = dj;
            int best = 0; long bestDot = long.MinValue;
            for (int d = 0; d < 6; d++)
            {
                int ex = NodeGrid.DI[d], ez = NodeGrid.DJ[d], ey = -ex - ez;
                long dot = (long)x * ex + (long)y * ey + (long)z * ez;
                if (dot > bestDot) { bestDot = dot; best = d; }
            }
            return best;
        }

        private static short S(int v) => (short)(v < short.MinValue ? short.MinValue : v > short.MaxValue ? short.MaxValue : v);

        /// <summary>Encodes <paramref name="actor"/>'s perception now into <paramref name="p"/> (local only; no allocation once warm).</summary>
        public static void Encode(GodState s, int actor, ref Percept p)
        {
            p.EnsureBuffers();
            var v = p.V;
            for (int i = 0; i < v.Length; i++) v[i] = 0;
            for (int i = 0; i < p.CarryLot.Length; i++) p.CarryLot[i] = 0;
            p.EntityCount = 0;
            p.Actor = actor; p.Tick = s.Tick;
            var npc = s.Population?.Find(actor);
            var u = s.Unification?.PersonOf(actor);
            if (npc == null || u == null) return;
            var g = s.Territory.Grid;
            var w = s.Unification;
            var book = w.Book;
            var ix = w.Index;
            p.Node = npc.Node; p.Facing = u.Facing;
            // ---- proprioception
            if (u.Facing < 6) v[OFacing + u.Facing] = ObsSchema.Scale;
            v[OControl] = (short)u.ActionKind;
            v[OProgress] = S(u.ActionTotal > 0 ? (int)((long)(u.ActionTotal - u.ActionTicksLeft) * ObsSchema.Scale / u.ActionTotal) : 0);
            v[OLast] = (short)u.LastResult;
            v[OAsleep] = (short)(npc.Action.Asleep ? ObsSchema.Scale : 0);
            v[OOccupying] = (short)(u.Occupying != 0 ? ObsSchema.Scale : 0);
            v[OEquipped] = S(u.Equipped != 0 ? ClassOfWare(book, (Ware)Motor.WareOfLot(s, npc, u.Equipped)) : 0);
            // ---- body
            // the twelve pressures in NeedChannel order (drive c = channel c, R86 mapping); an inactive channel reads 0 (as UN1's body reading)
            for (int d = 0; d < Drive.Count && d < NeedChannels.FirstExtension; d++)
                if (Unified.NeedActive(s, (NeedChannel)d)) v[ONeeds + d] = S(System.Math.Min(1000, npc.DriveOf(d)));
            for (int i = 0; i < NeedChannels.Extensions; i++)
                if (Unified.NeedActive(s, (NeedChannel)(NeedChannels.FirstExtension + i))) v[ONeeds + NeedChannels.FirstExtension + i] = u.Needs[i];
            v[OHealth] = S(npc.HealthPermille);
            v[OInjury] = S(npc.Injury / 1000);
            v[OAge] = (short)npc.Stage;
            for (int i = 0; i < Trait.Count && i < 12; i++) v[OTraits + i] = S(npc.Traits[i]);
            // ---- carry
            if (npc.Carry != null)
                for (int i = 0, k = 0; i < npc.Carry.Length && k < ObsSchema.CarrySlots; i++)
                {
                    var l = npc.Carry[i];
                    if (l.Empty) continue;
                    p.CarryLot[k] = l.Id;
                    int o = OCarry + k * ObsSchema.CarryFields;
                    v[o] = S(ClassOfWare(book, l.Ware)); v[o + 1] = S(l.Count); v[o + 2] = S(l.Uses);
                    k++;
                }
            // ---- the six cells, egocentric (cell k = direction facing + k)
            for (int k = 0; k < ObsSchema.Cells; k++)
            {
                int n = g.Neighbour(npc.Node, (u.Facing + k) % 6);
                int o = OCells + k * ObsSchema.CellFields;
                if (n < 0) continue;
                v[o] = (short)(Motor.Passable(s, n) ? ObsSchema.Scale : 0);
                int hex = g.HexOf(n);
                v[o + 1] = (short)(hex >= 0 ? (int)s.Board.TerrainAt(hex) + 1 : 0);
                int m = ix.At(s, n, SpatialKind.WaterPoint | SpatialKind.ForagePlace, p._hits);
                for (int i = 0; i < m; i++)
                {
                    if (p._hits[i].Kind == SpatialKind.WaterPoint && v[o + 2] == 0) v[o + 2] = (short)(WaterOf(w, p._hits[i].Id) ? ClassFreshWater : ClassSaltWater);
                    if (p._hits[i].Kind == SpatialKind.ForagePlace && v[o + 3] == 0) v[o + 3] = S(ClassOfWare(book, ForageWare(w, p._hits[i].Id)));
                }
            }
            // ---- the nearest perceived entities
            int sight = s.Tuning.Unification.UN2.SightNodes;
            if (sight <= 0) sight = CommsSight(s);
            p.Sight = sight;
            int h = ix.Nearest(s, npc.Node, sight, SpatialKind.All, p._hits);
            for (int i = 0; i < h && p.EntityCount < ObsSchema.Entities; i++)
            {
                var hit = p._hits[i];
                if (hit.Kind == SpatialKind.Npc && hit.Id == actor) continue;
                int e = p.EntityCount++;
                p.EntityKind[e] = hit.Kind; p.EntityId[e] = hit.Id; p.EntityNode[e] = hit.Node;
                int o = OEntities + e * ObsSchema.EntityFields;
                int rel = SectorOf(g, npc.Node, hit.Node);
                v[o + 1] = (short)(rel == 6 ? 6 : (rel - u.Facing + 6) % 6);
                v[o + 2] = S(hit.Distance);
                int bits = KindCode(hit.Kind) | (hit.Distance <= Reach.Touch ? Bits.InReach : 0);
                switch (hit.Kind)
                {
                    case SpatialKind.Npc:
                    {
                        v[o] = ClassPerson;
                        var other = s.Population.Find(hit.Id);
                        var ou = w.PersonOf(hit.Id);
                        if (other != null)
                        {
                            if (other.Action.Asleep) bits |= Bits.Asleep;
                            if (ou != null && ou.Busy) bits |= Bits.Busy;
                            if (other.Carry != null) foreach (var l in other.Carry) if (!l.Empty) { bits |= Bits.Carrying; break; }
                            if (u.ResistedBy == hit.Id && s.Tick - u.ResistedTick <= s.Tuning.Unification.UN2.ResistedMemoryTicks) bits |= Bits.ResistedMe;
                            foreach (var x in w.HoldOuts) if (x.Npc == hit.Id && (x.To == 0 || x.To == actor) && x.Expires >= s.Tick) { bits |= Bits.HeldOutToMe; break; }
                        }
                        for (int m = 0; m < BrainState.PeopleSlots; m++) if (u.Brain.People[m].Npc == hit.Id) { v[o + 6] = u.Brain.People[m].Trust; break; }
                        break;
                    }
                    case SpatialKind.Lot: v[o] = S(ClassOfWare(book, GroundWare(s.Population, hit.Id))); break;
                    case SpatialKind.Building:
                    {
                        var b = w.BuildingById(hit.Id);
                        if (b != null)
                        {
                            v[o] = (short)b.Cat;
                            if (hit.Distance <= Reach.Store && (BinHolds(s, b.Storage) || BinHolds(s, b.WorkOut))) bits |= Bits.StoreHolds;
                        }
                        break;
                    }
                    case SpatialKind.Site: { var x = w.SiteById(hit.Id); if (x != null) v[o] = (short)x.Design; break; }
                    case SpatialKind.RawNode: foreach (var x in w.RawNodes) if (x.Id == hit.Id) { v[o] = S(ClassOfWare(book, (Ware)x.Ware)); break; } break;
                    case SpatialKind.ForagePlace: v[o] = S(ClassOfWare(book, ForageWare(w, hit.Id))); break;
                    case SpatialKind.WaterPoint: v[o] = (short)(WaterOf(w, hit.Id) ? ClassFreshWater : ClassSaltWater); break;
                    case SpatialKind.Fixture: v[o] = S(ClassOfWare(book, FixtureWare(s, hit.Id))); break;
                }
                v[o + 3] = S(bits);
            }
            // ---- gestures heard or seen: the 4 most recent inbox entries (tick, then message id, newest first)
            var sm = npc.Mind?.Social;
            if (sm != null && sm.Inbox != null)
            {
                int last = int.MaxValue, lastTick = int.MaxValue;
                for (int k = 0; k < ObsSchema.Gestures; k++)
                {
                    int bi = -1;
                    for (int i = 0; i < sm.InboxCount && i < sm.Inbox.Length; i++)
                    {
                        var m = sm.Inbox[i].Msg;
                        bool older = m.Tick < lastTick || m.Tick == lastTick && m.MessageId < last;
                        if (!older) continue;
                        if (bi < 0 || m.Tick > sm.Inbox[bi].Msg.Tick || m.Tick == sm.Inbox[bi].Msg.Tick && m.MessageId > sm.Inbox[bi].Msg.MessageId) bi = i;
                    }
                    if (bi < 0) break;
                    var msg = sm.Inbox[bi].Msg;
                    last = msg.MessageId; lastTick = msg.Tick;
                    int o = OGestures + k * ObsSchema.GestureFields;
                    int slot = 0;
                    for (int e = 0; e < p.EntityCount; e++) if (p.EntityKind[e] == SpatialKind.Npc && p.EntityId[e] == msg.Speaker) { slot = e + 1; break; }
                    v[o] = (short)slot; v[o + 1] = (short)msg.Act;
                    if (Motor.ExtOf(w, msg.MessageId, out var ext)) { v[o + 2] = S(ClassOfReferent(book, in ext.R1)); v[o + 3] = ext.Quantity; }
                    else v[o + 3] = msg.Quantity;
                }
            }
            // ---- memory (the body's own)
            var br = u.Brain;
            for (int k = 0; k < BrainState.PlaceSlots && k < ObsSchema.Places; k++)
            {
                var mp = br.Places[k];
                if (mp.Tick == 0 && mp.Node == 0 && mp.Class == 0) continue;
                int o = OPlaces + k * ObsSchema.PlaceFields;
                v[o] = (short)mp.Class;
                int rel = g.IsValid(mp.Node) ? SectorOf(g, npc.Node, mp.Node) : 0;
                v[o + 1] = (short)(rel == 6 ? 6 : (rel - u.Facing + 6) % 6);
                v[o + 2] = S(g.IsValid(mp.Node) ? g.Distance(npc.Node, mp.Node) : 0);
            }
            for (int k = 0; k < BrainState.PeopleSlots && k < ObsSchema.People; k++)
            {
                var mpp = br.People[k];
                if (mpp.Npc == 0) continue;
                int o = OPeople + k * ObsSchema.PeopleFields;
                for (int e = 0; e < p.EntityCount; e++)
                    if (p.EntityKind[e] == SpatialKind.Npc && p.EntityId[e] == mpp.Npc)
                    {
                        // where a remembered person is is known only while it is in view
                        v[o] = (short)(e + 1); v[o + 1] = v[OEntities + e * ObsSchema.EntityFields + 1]; v[o + 2] = v[OEntities + e * ObsSchema.EntityFields + 2];
                        break;
                    }
                v[o + 3] = mpp.Trust;
            }
            int own = 0, seen = 0, told = 0, all = 0;
            foreach (var c in br.Claims)
            {
                if (c.Claimant == 0 && c.Tick == 0) continue;
                all++;
                if (c.Source == ClaimSource.Own) own++; else if (c.Source == ClaimSource.Observed) seen++; else told++;
            }
            v[OClaims] = (short)all; v[OClaims + 1] = (short)own; v[OClaims + 2] = (short)seen; v[OClaims + 3] = (short)told;
            // ---- knowledge words (16 bits per value)
            Words(br.Designs, v, ODesigns, 5);
            Words(br.Recipes, v, ORecipes, 4);
            Words(br.Interactions, v, OInteractions, 8);
            // ---- divine inputs (zero without divinity)
            if (s.Divinity != null) v[ODivine] = (short)God.Sim.Divinity.Control.Of(s, actor);
        }

        private static void Words(ulong[] bits, short[] v, int at, int n)
        {
            for (int k = 0; k < n; k++)
            {
                int word = k >> 2, part = k & 3;
                ulong x = word < bits.Length ? bits[word] : 0UL;
                v[at + k] = unchecked((short)(ushort)((x >> (part * 16)) & 0xFFFF));
            }
        }

        /// <summary>The vector of an encoded percept (copied into <paramref name="obs"/>, length <see cref="ObsSchema.Length"/>).</summary>
        public static void ToVector(in Percept p, short[] obs)
        {
            if (obs == null || obs.Length != ObsSchema.Length) throw new System.ArgumentException("obs must have ObsSchema.Length values", nameof(obs));
            if (p.V == null) { for (int i = 0; i < obs.Length; i++) obs[i] = 0; return; }
            System.Array.Copy(p.V, obs, obs.Length);
        }

        private static int CommsSight(GodState s)
        {
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            return God.Sim.Comms.CommsRange.Gesture(s.Tuning.Population, night);
        }

        private static int ClassOfReferent(InteractionBook book, in Referent r)
        {
            switch (r.Kind)
            {
                case ReferentKind.Ware: case ReferentKind.Lot: return ClassOfWare(book, (Ware)r.Ware);
                case ReferentKind.Person: return ClassPerson;
                case ReferentKind.Place: return 4;
                default: return 0;
            }
        }

        private static bool WaterOf(UnifiedState w, int id) { foreach (var x in w.WaterPoints) if (x.Id == id) return x.Fresh; return false; }
        private static Ware ForageWare(UnifiedState w, int id) { foreach (var x in w.ForagePlaces) if (x.Id == id) return (Ware)x.Ware; return Ware.None; }

        private static Ware GroundWare(PopulationState p, int lot)
        {
            var a = p.Lots;
            int lo = 0, hi = a.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; int x = a[mid].Lot.Id; if (x == lot) return a[mid].Lot.Ware; if (x < lot) lo = mid + 1; else hi = mid - 1; }
            return Ware.None;
        }

        private static Ware FixtureWare(GodState s, int lot) => Resolver.LocateLot(s, lot, out _, out var w) ? w : Ware.None;

        private static bool BinHolds(GodState s, int bin)
        {
            if (bin == 0) return false;
            var b = Goods.BinById(s.Population, bin);
            if (b == null) return false;
            foreach (var l in b.Lots) if (!l.Empty) return true;
            return false;
        }
    }
}
