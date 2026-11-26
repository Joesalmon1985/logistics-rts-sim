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
            PlaceSources(s);
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
            return npc;
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
            if (tu.FounderFood > 0)
            {
                var lot = NewLot(s, Ware.Roots, tu.FounderFood, day + eco.RootSpoilDays, 0);
                PutInCarry(s, npc, lot);
                s.Population.Ledger.Produce(ProduceReason.Founder, Ware.Roots, tu.FounderFood);
            }
            if (r.NextInt(1000) < tu.FounderAxePermille)
            {
                PutInCarry(s, npc, NewLot(s, Ware.Axe, 1, -1, tu.AxeUses));
                s.Population.Ledger.Produce(ProduceReason.Founder, Ware.Axe, 1);
            }
        }

        // ---------------- goods ----------------

        public static ItemLot NewLot(GodState s, Ware w, int count, int spoilDay, int uses) =>
            new ItemLot { Id = s.Population.NextLotId++, Ware = w, Count = count, SpoilDay = spoilDay, Uses = uses };

        public static int Weight(Ware w)
        {
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

        /// <summary>Puts a lot into the carry: merges with a lot of the same ware and spoil day, else takes a free slot. Returns false when it does not fit.</summary>
        public static bool PutInCarry(GodState s, Npc npc, ItemLot lot)
        {
            if (Room(s, npc, lot.Ware) < lot.Count) return false;
            // the same ware merges into one lot; a mixed lot rots with its oldest part (the earlier spoil day)
            for (int i = 0; i < npc.Carry.Length; i++)
                if (!npc.Carry[i].Empty && npc.Carry[i].Ware == lot.Ware && lot.Ware != Ware.Axe)
                {
                    npc.Carry[i].Count += lot.Count;
                    if (lot.Id < npc.Carry[i].Id) npc.Carry[i].Id = lot.Id;
                    if (lot.SpoilDay >= 0 && (npc.Carry[i].SpoilDay < 0 || lot.SpoilDay < npc.Carry[i].SpoilDay)) npc.Carry[i].SpoilDay = lot.SpoilDay;
                    return true;
                }
            for (int i = 0; i < npc.Carry.Length; i++)
                if (npc.Carry[i].Empty) { npc.Carry[i] = lot; return true; }
            return false;
        }

        public static int CountOf(Npc npc, Ware w)
        {
            int n = 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Ware == w) n += l.Count;
            return n;
        }

        /// <summary>Drops a lot on a node (merging with an equal lot there).</summary>
        public static void Drop(GodState s, int node, ItemLot lot)
        {
            if (lot.Empty) return;
            foreach (var g in s.Population.Lots)
                if (g.Node == node && g.Lot.Ware == lot.Ware && g.Lot.SpoilDay == lot.SpoilDay && lot.Ware != Ware.Axe)
                {
                    g.Lot.Count += lot.Count;
                    if (lot.Id < g.Lot.Id) g.Lot.Id = lot.Id;
                    return;
                }
            s.Population.Lots.Add(new GroundLot { Node = node, Lot = lot });
            s.Population.Lots.Sort((a, b) => a.Lot.Id.CompareTo(b.Lot.Id));
            s.Population.IndexDirty = true;
        }

        /// <summary>A controlled scenario places goods on the ground (counted as <see cref="ProduceReason.Placed"/>).</summary>
        public static int PlaceLot(GodState s, int node, Ware w, int count)
        {
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int spoil = Wares.IsFood(w) ? day + SpoilDays(s.Tuning.Ecology, w) : -1;
            var lot = NewLot(s, w, count, spoil, w == Ware.Axe ? s.Tuning.Population.AxeUses : 0);
            s.Population.Ledger.Produce(ProduceReason.Placed, w, count);
            Drop(s, node, lot);
            return lot.Id;
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
            foreach (var n in p.Npcs)
            {
                h.Add(n.Id); h.Add((int)n.Faction); h.Add(n.Alive ? 1 : 0); h.Add(n.BornTick); h.Add(n.DiedTick); h.Add((int)n.DeathCause);
                h.Add(n.Node); h.Add(n.PathIdx); h.Add(n.PathGoal); h.Add(n.StepTicks);
                if (n.Path != null) { h.Add(n.Path.Length); foreach (int x in n.Path) h.Add(x); } else h.Add(-1);
                h.Add(n.Health); h.Add(n.Nutrition); h.Add(n.Injury); h.Add(n.PainExtra);
                foreach (int d in n.Drives) h.Add(d);
                if (n.RewardSnap != null) foreach (int d in n.RewardSnap) h.Add(d);
                foreach (var l in n.Carry) { h.Add(l.Id); h.Add((int)l.Ware); h.Add(l.Count); h.Add(l.SpoilDay); h.Add(l.Uses); }
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
            }
            foreach (var g in p.Lots) { h.Add(g.Node); h.Add(g.Lot.Id); h.Add((int)g.Lot.Ware); h.Add(g.Lot.Count); h.Add(g.Lot.SpoilDay); h.Add(g.Lot.Uses); }
            if (p.OpenPileUnits != null) foreach (int u in p.OpenPileUnits) h.Add(u);
            foreach (long v in p.Ledger.Produced) h.Add(v);
            foreach (long v in p.Ledger.Consumed) h.Add(v);
            var st = p.Stats;
            h.Add(st.Decisions); h.Add(st.IntentsRefused); h.Add(st.NotPerceived); h.Add(st.Meals); h.Add(st.Sleeps); h.Add(st.HutsBuilt); h.Add(st.LogsGathered); h.Add(st.Foraged);
            foreach (int d in st.DeathsByCause) h.Add(d);
            h.Add(st.Founders); h.Add(st.Deaths);
            return h.Value;
        }

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
