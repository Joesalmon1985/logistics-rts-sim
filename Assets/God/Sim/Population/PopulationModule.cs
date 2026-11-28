using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Population
{
    /// <summary>
    /// The population module (M3): created only when <see cref="GameSetup.Population"/> is set, so a stage 3 game has no
    /// population state, no system and exactly its old hash. Founders come from the setup (<see cref="GameSetup.StartNpcsPerFaction"/>
    /// per faction, around their HQ); wild sources are placed by terrain from the ecology tuning.
    /// </summary>
    public static class PopulationModule
    {
        public const int BeliefSlots = 64;

        public static void StartGame(GodState s)
        {
            if (s.Setup == null || !s.Setup.Population) return;
            var p = new PopulationState { OpenPileUnits = new int[s.Territory.Topology.HexCount] };
            s.Population = p;
            // UN0 (R85, UN0 spec 5.4): a unified world package may replace the legacy wild sources and founders (default: kept)
            bool unified = Unified.Unified.On(s);
            if (!unified || !Unified.UnifiedHooks.ReplacesSources(s)) PlaceSources(s);
            if (!unified || !Unified.UnifiedHooks.ReplacesFounders(s))
            foreach (var f in s.Setup.Factions)
            {
                var hqs = s.Territory.HqsOf(f);
                if (hqs.Count == 0) continue;
                for (int k = 0; k < s.Setup.StartNpcsPerFaction; k++)
                {
                    int id = p.NextNpcId;
                    var r = s.Rng.Fork("founder", id);
                    int node = FounderNode(s, hqs[0].Node, r);
                    var npc = AddNpc(s, f, node, InstinctSet.Default, s.Tuning.Learning.DefaultMode, true);
                    GiveFounderPack(s, npc, r);
                }
            }
            p.Stats.Founders = p.Npcs.Count;
        }

        public static void Install(GodSim sim)
        {
            if (sim.State.Population == null) return;
            sim.AddSystem(SystemOrder.Population, new PopulationSystem());
        }

        // ---------------- NPCs ----------------

        /// <summary>
        /// Makes an NPC at a node: keyed streams (<c>Rng.Fork("npc", id)</c> for its policy, then "work" and "learn" forks),
        /// random traits and learning parameters (until M4's genome), an empty learning store in the given mode.
        /// </summary>
        public static Npc AddNpc(GodState s, Faction f, int node, InstinctSet instincts, LearningMode mode, bool founder)
        {
            var p = s.Population;
            var tu = s.Tuning.Population;
            int id = p.NextNpcId++;
            var root = s.Rng.Fork("npc", id);
            var npc = new Npc
            {
                Id = id, Faction = f, Node = node, BornTick = s.Tick, Founder = founder, Instincts = instincts,
                Carry = new ItemLot[tu.CarrySlots],
                RngPolicy = root, RngWork = root.Fork("work"), RngLearn = root.Fork("learn"),
                Nutrition = tu.FounderNutrition * 1000,
                Mind = new NpcMind(s.Tuning.Learning.Caps, BeliefSlots, mode),
            };
            npc.Drives[Drive.Hunger] = tu.FounderHunger * 1000;
            npc.Drives[Drive.Fatigue] = tu.FounderFatigue * 1000;
            var tr = root.Fork("traits");
            for (int t = 0; t < Trait.Count; t++) npc.Traits[t] = tr.NextRange(-100, 101);
            npc.LearnRate = tr.NextRange(800, 1201);
            npc.ForgetRate = tr.NextRange(800, 1201);
            npc.Curiosity = tr.NextRange(800, 1201);
            npc.RewardSnap = (int[])npc.Drives.Clone();
            int at = p.Npcs.Count;
            while (at > 0 && p.Npcs[at - 1].Id > id) at--;
            p.Npcs.Insert(at, npc);
            PopulationExtensions.NewMind(s, npc); // F0.13: M4 and M5 give the new mind their parts (no-ops until they land)
            return npc;
        }

        /// <summary>
        /// Kills an NPC with one cause (F0.13, extracted from the system's death): mark it dead, emit <see cref="NpcDied"/>,
        /// let M4 (estate, household) and then M5 react, then the M3 cleanup: what it still carries spills to the ground
        /// (<see cref="MoveReason.DeathSpill"/>), its hut rights and contributions end, its mind is released.
        /// </summary>
        public static void Kill(GodState s, EventBuffer ev, Npc npc, DeathCause cause)
        {
            if (!Bodies.IsPerson(npc)) throw new InvalidOperationException("Kill: only a person dies (an avatar is destroyed by D5, contracts 4.2)");
            var p = s.Population;
            npc.Alive = false; npc.DiedTick = s.Tick; npc.DeathCause = cause; npc.Health = 0;
            p.Stats.Deaths++;
            p.Stats.DeathsByCause[(int)cause]++;
            ev.Add(new NpcDied { Npc = npc.Id, Faction = npc.Faction, Cause = cause, Node = npc.Node, AgeTicks = s.Tick - npc.BornTick });
            PopulationExtensions.OnDeath(s, ev, npc);
            for (int i = 0; i < npc.Carry.Length; i++)
            {
                var l = npc.Carry[i];
                if (l.Empty) continue;
                var r = Goods.Move(s, ev, Holder.Carry(npc.Id), l.Id, l.Count, Holder.Ground(npc.Node), MoveReason.DeathSpill, npc.Id);
                if (!r.Ok) throw new InvalidOperationException("death spill failed: " + r.Status);
            }
            foreach (var hut in p.Huts)
            {
                for (int i = 0; i < hut.Rights.Length; i++) if (hut.Rights[i] == npc.Id) hut.Rights[i] = 0;
                hut.Contributors.Remove(npc.Id);
            }
            npc.HomeHut = 0; npc.SiteHut = 0; npc.Path = null; npc.Action = default;
            PopulationExtensions.ArchiveMind(s, npc); // F1 (contracts 4.4): D7's archive, just before the mind is released
            npc.Mind = null; // the dead keep their record, not their mind
        }

        /// <summary>Sets an NPC's nature exactly (controlled tests: identical genomes).</summary>
        public static void SetNature(Npc npc, int[] traits, int learnRate, int forgetRate, int curiosity)
        {
            Array.Copy(traits, npc.Traits, Trait.Count);
            npc.LearnRate = learnRate; npc.ForgetRate = forgetRate; npc.Curiosity = curiosity;
        }

        private static int FounderNode(GodState s, int hqNode, Rng r)
        {
            var g = s.Territory.Grid;
            var tu = s.Tuning.Population;
            int ci = g.I(hqNode), cj = g.J(hqNode);
            int fallback = -1;
            for (int tries = 0; tries < 200; tries++)
            {
                int di = r.NextRange(-tu.FounderSpreadNodes, tu.FounderSpreadNodes + 1);
                int dj = r.NextRange(-tu.FounderSpreadNodes, tu.FounderSpreadNodes + 1);
                int n = g.Id(ci + di, cj + dj);
                if (n < 0 || !g.IsLand(n)) continue;
                int d = g.Distance(n, hqNode);
                if (d <= g.CircleRadiusSteps || d > tu.FounderSpreadNodes) continue;
                if (s.Territory.NodeBuilding[n] != 0 || s.Territory.NodeFlag[n] != 0) continue;
                // a founder arrives where it can see something to eat (M3b: about 1 in 6 founders used to start with none in sight)
                if (FoodInSight(s, n, tu.SightDay)) return n;
                if (fallback < 0) fallback = n;
            }
            return fallback >= 0 ? fallback : hqNode;
        }

        private static bool FoodInSight(GodState s, int node, int sight)
        {
            var g = s.Territory.Grid;
            foreach (var src in s.Population.Sources)
                if (src.Kind != SourceKind.Wood && src.Kind != SourceKind.Nightshade && src.Stock > 0 && g.Distance(node, src.Node) <= sight) return true;
            return false;
        }

        private static void GiveFounderPack(GodState s, Npc npc, Rng r)
        {
            var tu = s.Tuning.Population;
            var eco = s.Tuning.Ecology;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            // before the first tick: no event buffer yet, so the founder packs emit no Produced (their ledger rows are booked)
            if (tu.FounderFood > 0)
                Goods.Produce(s, null, Holder.Carry(npc.Id), Ware.Roots, tu.FounderFood, day + eco.RootSpoilDays, 0, Owner.Npc(npc.Id), ProduceReason.Founder, npc.Id, 0);
            if (r.NextInt(1000) < tu.FounderAxePermille)
                Goods.Produce(s, null, Holder.Carry(npc.Id), Ware.Axe, 1, -1, tu.AxeUses, Owner.Npc(npc.Id), ProduceReason.Founder, npc.Id, 0);
        }

        // ---------------- goods ----------------

        /// <summary>Allocates a lot id: called only by <see cref="Goods.Produce"/> (a split in <see cref="Goods.Move"/> takes the next id the same way).</summary>
        public static ItemLot NewLot(GodState s, Ware w, int count, int spoilDay, int uses) =>
            new ItemLot { Id = s.Population.NextLotId++, Ware = w, Count = count, SpoilDay = spoilDay, Uses = uses };

        public static int Weight(Ware w)
        {
            if (Unified.UnifiedWares.InBlock(w)) return Unified.UnifiedWares.Weight(w); // UN0: the unification block (catalogue numbers)
            if (Wares.IsFood(w)) return 1;
            if (w == Ware.Axe || Wares.IsGear(w)) return 2;
            if (w == Ware.Log || w == Ware.Clay || w == Ware.Stone || w == Ware.Ore || w == Ware.Plank || w == Ware.Brick || w == Ware.Mortar) return 4;
            return 2;
        }

        public static int CarryWeight(Npc npc)
        {
            int w = 0;
            foreach (var l in npc.Carry) if (!l.Empty) w += Weight(l.Ware) * l.Count;
            return w;
        }

        /// <summary>How many units of a ware still fit (weight and a free or matching slot).</summary>
        public static int Room(GodState s, Npc npc, Ware w)
        {
            int free = s.Tuning.Population.CarryWeight - CarryWeight(npc);
            int n = free / Weight(w);
            if (n <= 0) return 0;
            bool slot = false;
            foreach (var l in npc.Carry) if (l.Empty || l.Ware == w && w != Ware.Axe) { slot = true; break; }
            return slot ? n : 0;
        }

        public static int CountOf(Npc npc, Ware w)
        {
            int n = 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Ware == w) n += l.Count;
            return n;
        }

        /// <summary>
        /// A controlled scenario places goods on the ground (counted as <see cref="ProduceReason.Placed"/>, ownerless). Returns the
        /// id they lie under (the lower id of an equal lot they merged into). No event: placed outside a tick.
        /// </summary>
        public static int PlaceLot(GodState s, int node, Ware w, int count)
        {
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int spoil = Wares.IsFood(w) ? day + SpoilDays(s.Tuning.Ecology, w) : -1;
            return Goods.Produce(s, null, Holder.Ground(node), w, count, spoil, w == Ware.Axe ? s.Tuning.Population.AxeUses : 0, Owner.None, ProduceReason.Placed, 0, 0);
        }

        public static int SpoilDays(EcologyTuning e, Ware w)
        {
            switch (w)
            {
                case Ware.Berries: return e.BerrySpoilDays;
                case Ware.Roots: return e.RootSpoilDays;
                case Ware.Fish: return e.FishSpoilDays;
                case Ware.Nightshade: return e.NightshadeSpoilDays;
                default: return 1000;
            }
        }

        public static int Nutrition(EcologyTuning e, Ware w)
        {
            switch (w)
            {
                case Ware.Berries: return e.BerryNutrition;
                case Ware.Roots: return e.RootNutrition;
                case Ware.Fish: return e.FishNutrition;
                case Ware.Nightshade: return e.NightshadeNutrition;
                default: return 0;
            }
        }

        public static Ware WareOf(SourceKind k)
        {
            switch (k)
            {
                case SourceKind.Berries: return Ware.Berries;
                case SourceKind.Roots: return Ware.Roots;
                case SourceKind.Fish: return Ware.Fish;
                case SourceKind.Nightshade: return Ware.Nightshade;
                default: return Ware.Log;
            }
        }

        public static int ClassOf(SourceKind k)
        {
            switch (k)
            {
                case SourceKind.Berries: return ObjClass.BerryBush;
                case SourceKind.Roots: return ObjClass.RootPatch;
                case SourceKind.Fish: return ObjClass.FishSpot;
                case SourceKind.Nightshade: return ObjClass.Nightshade;
                default: return ObjClass.WoodStand;
            }
        }

        // ---------------- sources ----------------

        /// <summary>Adds a wild source (controlled scenarios and the start of the game). One source per node.</summary>
        public static ResourceSource AddSource(GodState s, SourceKind kind, int node, int stock, int capacity, int regrowPermille)
        {
            var p = s.Population;
            var src = new ResourceSource
            {
                Id = p.NextSourceId++, Kind = kind, Node = node, Hex = s.Territory.Grid.HexOf(node), Stock = stock, Capacity = capacity, RegrowPermille = regrowPermille,
            };
            p.Sources.Add(src);
            p.IndexDirty = true;
            return src;
        }

        public static void RemoveSource(GodState s, ResourceSource src)
        {
            s.Population.Sources.Remove(src);
            s.Population.IndexDirty = true;
        }

        private static void PlaceSources(GodState s)
        {
            var g = s.Territory.Grid;
            var eco = s.Tuning.Ecology;
            int hexes = s.Territory.Topology.HexCount;
            var nodesOf = new List<int>[hexes];
            for (int h = 0; h < hexes; h++) nodesOf[h] = new List<int>();
            for (int n = 0; n < g.Count; n++) { int h = g.HexOf(n); if (h >= 0) nodesOf[h].Add(n); }
            var used = new HashSet<int>();
            for (int h = 0; h < hexes; h++)
            {
                var terrain = s.Board.HexTerrain[h];
                var r = s.Rng.Fork("ecology", h);
                int berries = 0, roots = 0, wood = 0, shade = 0;
                switch (terrain)
                {
                    case Terrain.Forest: berries = eco.BerriesPerForest; wood = eco.WoodPerForest; shade = eco.NightshadePerForest; break;
                    case Terrain.Meadow: case Terrain.Pasture: berries = eco.BerriesPerMeadow; break;
                    case Terrain.Hills: roots = eco.RootsPerHills; break;
                    case Terrain.Fields: roots = eco.RootsPerFields; break;
                }
                for (int k = 0; k < berries; k++) Place(s, r, nodesOf[h], used, SourceKind.Berries, eco.BerryCapacity, eco.BerryRegrowPermille, false);
                for (int k = 0; k < roots; k++) Place(s, r, nodesOf[h], used, SourceKind.Roots, eco.RootCapacity, eco.RootRegrowPermille, false);
                for (int k = 0; k < wood; k++) Place(s, r, nodesOf[h], used, SourceKind.Wood, eco.WoodCapacity, eco.WoodRegrowPermille, false);
                for (int k = 0; k < shade; k++) Place(s, r, nodesOf[h], used, SourceKind.Nightshade, eco.NightshadeCapacity, eco.NightshadeRegrowPermille, false);
                for (int k = 0; k < eco.FishPerCoast; k++) Place(s, r, nodesOf[h], used, SourceKind.Fish, eco.FishCapacity, eco.FishRegrowPermille, true);
            }
        }

        public static bool IsCoast(NodeGrid g, int n)
        {
            if (!g.IsLand(n)) return false;
            for (int d = 0; d < 6; d++) { int v = g.Neighbour(n, d); if (v < 0 || !g.IsLand(v)) return true; }
            return false;
        }

        private static void Place(GodState s, Rng r, List<int> nodes, HashSet<int> used, SourceKind kind, int cap, int regrow, bool coast)
        {
            var g = s.Territory.Grid;
            var t = s.Territory;
            var options = new List<int>();
            foreach (int n in nodes)
            {
                if (used.Contains(n) || t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                if (coast != IsCoast(g, n)) continue;
                bool inCircle = false;
                foreach (var hq in t.Hqs) if (g.InCircle(hq.Node, n)) { inCircle = true; break; }
                if (inCircle) continue;
                options.Add(n);
            }
            if (options.Count == 0) return;
            int node = options[r.NextInt(options.Count)];
            used.Add(node);
            AddSource(s, kind, node, cap, cap, regrow);
        }

        // ---------------- huts ----------------

        /// <summary>A hut in a controlled scenario (built, with the given right holders).</summary>
        public static Hut AddHut(GodState s, Faction f, int node, bool built, params int[] rights)
        {
            var p = s.Population;
            var tu = s.Tuning.Population;
            var hut = new Hut
            {
                Id = p.NextHutId++, Faction = f, Node = node, Hex = s.Territory.Grid.HexOf(node), Built = built, LogsNeeded = tu.HutLogs,
                LogsIn = built ? tu.HutLogs : 0, WorkNeeded = tu.HutWorkTicks, Work = built ? tu.HutWorkTicks : 0, Beds = tu.HutBeds,
                Rights = new int[tu.HutBeds], FoundedTick = s.Tick, BuiltTick = built ? s.Tick : 0,
            };
            for (int i = 0; i < rights.Length && i < hut.Rights.Length; i++)
            {
                hut.Rights[i] = rights[i];
                var npc = p.Find(rights[i]);
                if (npc != null) { if (built) npc.HomeHut = hut.Id; else npc.SiteHut = hut.Id; }
            }
            p.Huts.Add(hut);
            p.IndexDirty = true;
            return hut;
        }

        // ---------------- hashing (three parts: population, cognition, learning) ----------------

        public static ulong HashPopulation(GodState s)
        {
            var p = s.Population;
            var h = new StateHasher();
            h.Add(p.Npcs.Count); h.Add(p.NextNpcId); h.Add(p.NextLotId); h.Add(p.NextHutId); h.Add(p.NextSourceId); h.Add(p.DayDone); h.Add(p.FoundersAnnounced ? 1 : 0);
            h.Add(p.NextBinId);
            foreach (var n in p.Npcs)
            {
                h.Add(n.Id); h.Add((int)n.Faction); h.Add(n.Alive ? 1 : 0); h.Add(n.BornTick); h.Add(n.DiedTick); h.Add((int)n.DeathCause);
                if (n.Kind != BodyKind.Person || n.God != 0 || n.BioAgeOffset != 0) { h.Add(0x424F4459); h.Add((int)n.Kind); h.Add(n.God); h.Add(n.BioAgeOffset); } // F1.8: only when non-default
                h.Add(n.Node); h.Add(n.PathIdx); h.Add(n.PathGoal); h.Add(n.StepTicks);
                if (n.Path != null) { h.Add(n.Path.Length); foreach (int x in n.Path) h.Add(x); } else h.Add(-1);
                h.Add(n.Health); h.Add(n.Nutrition); h.Add(n.Injury); h.Add(n.PainExtra);
                foreach (int d in n.Drives) h.Add(d);
                if (n.RewardSnap != null) foreach (int d in n.RewardSnap) h.Add(d);
                foreach (var l in n.Carry) { h.Add(l.Id); h.Add((int)l.Ware); h.Add(l.Count); h.Add(l.SpoilDay); h.Add(l.Uses); h.Add((int)l.Owner.Kind); h.Add(l.Owner.Id); }
                h.Add((int)n.Stage); h.Add(n.Household);
                if (n.Life != null) { h.Add(0x4C494645); n.Life.AddTo(ref h); } // M4's part, only when present (M3 NPCs hash as before)
                h.Add(n.HomeHut); h.Add(n.SiteHut); h.Add(n.EatenDay); foreach (int e in n.EatenToday) h.Add(e);
                foreach (int t in n.DayTicks) h.Add(t);
                h.Add(n.DayWalkTicks); h.Add(n.DayMeals); foreach (int d in n.DayDriveSum) h.Add(d);
                foreach (int t in n.Traits) h.Add(t);
                h.Add(n.LearnRate); h.Add(n.ForgetRate); h.Add(n.Curiosity); h.Add((int)n.Instincts); h.Add(n.Policy);
                var a = n.Action;
                h.Add(a.Verb); h.Add((int)a.TargetKind); h.Add(a.TargetId); h.Add(a.TargetNode); h.Add(a.BusyUntil); h.Add(a.Progress);
                h.Add(a.Asleep ? 1 : 0); h.Add(a.SleepHut); h.Add(a.Since); h.Add(n.LastThinkTick);
                h.Add((long)(n.RngPolicy?.State ?? 0)); h.Add((long)(n.RngWork?.State ?? 0)); h.Add((long)(n.RngLearn?.State ?? 0));
            }
            foreach (var src in p.Sources) { h.Add(src.Id); h.Add((int)src.Kind); h.Add(src.Node); h.Add(src.Stock); h.Add(src.Capacity); h.Add(src.RegrowPermille); h.Add(src.EmptyDays); }
            foreach (var hut in p.Huts)
            {
                h.Add(hut.Id); h.Add((int)hut.Faction); h.Add(hut.Node); h.Add(hut.Built ? 1 : 0); h.Add(hut.LogsIn); h.Add(hut.Work); h.Add(hut.Beds);
                foreach (int r in hut.Rights) h.Add(r);
                h.Add(hut.Contributors.Count); foreach (int c in hut.Contributors) h.Add(c);
                h.Add(hut.FoundedBy); h.Add(hut.BuiltTick);
                if (hut.RightSource != null) foreach (byte r in hut.RightSource) h.Add(r);
                if (hut.Lodgers != null) { h.Add(hut.Lodgers.Count); foreach (int c in hut.Lodgers) h.Add(c); }
            }
            foreach (var g in p.Lots) { h.Add(g.Node); h.Add(g.Lot.Id); h.Add((int)g.Lot.Ware); h.Add(g.Lot.Count); h.Add(g.Lot.SpoilDay); h.Add(g.Lot.Uses); h.Add((int)g.Lot.Owner.Kind); h.Add(g.Lot.Owner.Id); }
            h.Add(p.Bins.Count);
            foreach (var b in p.Bins)
            {
                h.Add(b.Id); h.Add((int)b.Kind); h.Add((int)b.Owner.Kind); h.Add(b.Owner.Id); h.Add(b.Node); h.Add(b.WeightCap); h.Add(b.Lots.Count);
                foreach (var l in b.Lots) { h.Add(l.Id); h.Add((int)l.Ware); h.Add(l.Count); h.Add(l.SpoilDay); h.Add(l.Uses); h.Add((int)l.Owner.Kind); h.Add(l.Owner.Id); }
            }
            if (p.OpenPileUnits != null) foreach (int u in p.OpenPileUnits) h.Add(u);
            HashLedger(ref h, p.Ledger); // F1.17: the old 32-ware layout, plus wares 32-39 only when any is non-zero
            var st = p.Stats;
            h.Add(st.Decisions); h.Add(st.IntentsRefused); h.Add(st.NotPerceived); h.Add(st.Meals); h.Add(st.Sleeps); h.Add(st.HutsBuilt); h.Add(st.LogsGathered); h.Add(st.Foraged);
            foreach (int d in st.DeathsByCause) h.Add(d);
            h.Add(st.Founders); h.Add(st.Deaths);
            if (p.Pedigree != null) { h.Add(p.Pedigree.Count); foreach (var r in p.Pedigree) { h.Add(r.Id); h.Add(r.Mother); h.Add(r.Father); h.Add(r.BornTick); } }
            return h.Value;
        }

        /// <summary>
        /// F1.17: the ledger in the pre-F1 order (reason-major, wares 0-31: exactly the old array), then, only when any ware
        /// 32-39 has a non-zero count under any reason, a marker and those slots, so the population part is unchanged until a
        /// ware beyond 31 (Herbs) is produced or consumed.
        /// <para>UN0 (contracts 23.14): the ledger is 16 reasons x 160 wares; the two blocks above cover reasons 0-7 and wares
        /// 0-39 exactly as before. Then, only when any slot outside that window (wares 40-159 of reasons 0-7, or any ware of
        /// reasons 8-15) is non-zero, a second marker and every such non-zero slot as (index, value) in index order.</para>
        /// </summary>
        internal static void HashLedger(ref StateHasher h, GoodsLedger l)
        {
            const int Old = Wares.LegacyPersonalCount, F1 = Wares.F1PersonalCount, W = Wares.PersonalCount, R = GoodsLedger.LegacyReasons;
            bool extra = false, wide = false;
            for (int r = 0; r < R; r++) for (int w = 0; w < F1; w++) { long v = l.Produced[r * W + w]; if (w < Old) h.Add(v); else if (v != 0) extra = true; }
            for (int r = 0; r < R; r++) for (int w = 0; w < F1; w++) { long v = l.Consumed[r * W + w]; if (w < Old) h.Add(v); else if (v != 0) extra = true; }
            if (extra)
            {
                h.Add(0x48455242); // "HERB"
                for (int r = 0; r < R; r++) for (int w = Old; w < F1; w++) h.Add(l.Produced[r * W + w]);
                for (int r = 0; r < R; r++) for (int w = Old; w < F1; w++) h.Add(l.Consumed[r * W + w]);
            }
            for (int i = 0; i < l.Produced.Length && !wide; i++) if (l.Produced[i] != 0 && Wide(i)) wide = true;
            for (int i = 0; i < l.Consumed.Length && !wide; i++) if (l.Consumed[i] != 0 && Wide(i)) wide = true;
            if (!wide) return;
            h.Add(0x554E4946); // "UNIF"
            for (int i = 0; i < l.Produced.Length; i++) if (l.Produced[i] != 0 && Wide(i)) { h.Add(i); h.Add(l.Produced[i]); }
            h.Add(-1);
            for (int i = 0; i < l.Consumed.Length; i++) if (l.Consumed[i] != 0 && Wide(i)) { h.Add(i); h.Add(l.Consumed[i]); }
        }

        /// <summary>A ledger slot outside the F1 window (reason 8+ or ware 40+).</summary>
        private static bool Wide(int i) => i / Wares.PersonalCount >= GoodsLedger.LegacyReasons || i % Wares.PersonalCount >= Wares.F1PersonalCount;

        public static ulong HashCognition(GodState s)
        {
            var h = new StateHasher();
            foreach (var n in s.Population.Npcs) { h.Add(n.Id); if (n.Mind != null) n.Mind.AddTo(ref h); else h.Add(-1); }
            return h.Value;
        }

        public static ulong HashLearning(GodState s)
        {
            var h = new StateHasher();
            foreach (var n in s.Population.Npcs) { h.Add(n.Id); if (n.Mind?.Store != null) n.Mind.Store.AddTo(ref h); else h.Add(-1); }
            return h.Value;
        }
    }
}
