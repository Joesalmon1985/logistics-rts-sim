using System;
using System.Collections.Generic;
using God.Sim.Learning;
using God.Sim.Population.Genetics;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The start of M4 (from <c>HouseholdHooks.StartGame</c>): the household state, and the founders' genomes, ages, sexes and
    /// households from the scenario's founder mix (POPULATION "Owner population requirements": data, not code). Founder
    /// genomes come from <c>Rng.Fork("genome-founder", npcId)</c> (SHARED_CONTRACTS 12.7); the mix from <c>"founder-mix"</c>.
    /// Founder children are founders genetically (no known parents: unrelated in the pedigree) and the founders' children
    /// socially (kin beliefs from being raised), like any fostered child.
    /// </summary>
    public static class Founders
    {
        public static void StartGame(GodState s)
        {
            var p = s.Population;
            if (p == null || s.Tuning.Households.Enabled == 0) return;
            s.Households = new HouseholdState { Fouling = new int[s.Territory.Topology.HexCount] };
            p.Pedigree = new List<PedigreeRecord>();
            var ft = s.Tuning.Households.Founders;
            var cal = SimCalendar.Of(s);
            var gt = s.Tuning.Genetics;
            foreach (var npc in p.Npcs)
            {
                if (!npc.Founder) continue;
                var r = s.Rng.Fork("genome-founder", npc.Id);
                var genome = GenomeOps.Founder(r, gt.FounderSpread);
                byte sex = (byte)r.NextInt(2);
                var mix = s.Rng.Fork("founder-mix", npc.Id);
                int u = mix.NextInt(1000);
                int age;
                if (u < ft.ChildPermille) age = mix.NextRange(ft.ChildMinMilli, ft.ChildMaxMilli + 1);
                else if (u < ft.ChildPermille + ft.ElderPermille) age = mix.NextRange(3000, 3301);
                else age = mix.NextRange(ft.AgeMinMilli, ft.AgeMaxMilli + 1);
                npc.Mind.Kin = new KinMind();
                MakeLife(s, npc, sex, genome, 0, 0, 0);
                SetAge(s, npc, age);
            }
            foreach (var f in s.Setup.Factions) Families(s, f);
        }

        /// <summary>Gives a person an age (milli-years): its birth tick goes back, its stage, growth and adult phenotype follow.</summary>
        public static void SetAge(GodState s, Npc npc, int ageMilli)
        {
            var cal = SimCalendar.Of(s);
            npc.BornTick = s.Tick - cal.TicksForAgeMilli(ageMilli);
            npc.Stage = SimCalendar.StageOf(ageMilli);
            var life = npc.Life;
            int i = Relatedness.IndexOf(s.Population.Pedigree, npc.Id);
            if (i >= 0) { var pr = s.Population.Pedigree[i]; pr.BornTick = npc.BornTick; s.Population.Pedigree[i] = pr; }
            if (npc.Stage == LifeStage.Child) { life.Growth = Math.Min(1000, 300 + 700 * ageMilli / 1000); life.Matured = false; }
            else
            {
                life.Growth = 1000;
                life.Matured = true;
                PhenotypeBuilder.Adult(life.Genome, LifeCycleSystem.MapOf(s), life.Noise, 800, ref life.Pheno);
            }
        }

        /// <summary>
        /// M4's part of a person: sex, parents, generation, genome; the developmental noise and the birth phenotype; the
        /// personality, instinct gains and learning rates the decider uses (<c>Npc.Traits</c> 0-11, then the four gains at 12-15,
        /// read by the instinct-gain hook); lifespan; the pedigree record.
        /// </summary>
        public static void MakeLife(GodState s, Npc npc, byte sex, byte[] genome, int mother, int father, int generation)
        {
            var lt = s.Tuning.Households.Life;
            var life = new NpcLife { Sex = sex, Mother = mother, BioFather = father, Generation = generation, Genome = genome };
            var dev = s.Rng.Fork("develop", npc.Id);
            life.Noise = PhenotypeBuilder.Noise(dev, s.Tuning.Genetics.NoisePermille);
            var traits = new int[Trait.Count + 4];
            PhenotypeBuilder.Birth(genome, LifeCycleSystem.MapOf(s), life.Noise, ref life.Pheno, traits);
            traits[Trait.Count + 0] = life.Pheno.HungerGain; traits[Trait.Count + 1] = life.Pheno.FearGain;
            traits[Trait.Count + 2] = life.Pheno.SocialGain; traits[Trait.Count + 3] = life.Pheno.NurtureGain;
            npc.Traits = traits;
            npc.LearnRate = life.Pheno.LearnRate; npc.ForgetRate = life.Pheno.ForgetRate; npc.Curiosity = life.Pheno.Curiosity;
            life.MaxAgeMilli = lt.MaxAgeBaseMilli + life.Pheno.Lifespan + dev.NextRange(-lt.MaxAgeJitterMilli, lt.MaxAgeJitterMilli + 1);
            npc.Life = life;
            if (npc.Mind != null && npc.Mind.Kin == null) npc.Mind.Kin = new KinMind();
            var ped = s.Population.Pedigree;
            var rec = new PedigreeRecord { Id = npc.Id, Mother = mother, Father = father, BornTick = npc.BornTick };
            int at = ped.Count;
            while (at > 0 && ped[at - 1].Id > npc.Id) at--;
            ped.Insert(at, rec);
        }

        /// <summary>One faction's founder households: couples (some same-sex, by the scenario), single adults, children with a couple.</summary>
        private static void Families(GodState s, Faction f)
        {
            var p = s.Population;
            var ft = s.Tuning.Households.Founders;
            var women = new List<Npc>(); var men = new List<Npc>(); var children = new List<Npc>();
            foreach (var n in p.Npcs)
            {
                if (!n.Founder || n.Faction != f || n.Life == null) continue;
                if (n.Stage == LifeStage.Child) children.Add(n);
                else if (n.Life.Sex == 0) women.Add(n); else men.Add(n);
            }
            int adults = women.Count + men.Count;
            int couples = adults * ft.CouplePermille / 2000;
            var homes = new List<Household>();
            var couplesHomes = new List<Household>();
            for (int k = 0; k < couples; k++)
            {
                bool same = (k + 1) * 1000 <= couples * ft.SameSexPermille;
                Npc a, b;
                if (same) { var pool = women.Count >= men.Count ? women : men; if (pool.Count < 2) break; a = pool[0]; b = pool[1]; pool.RemoveRange(0, 2); }
                else { if (women.Count == 0 || men.Count == 0) break; a = women[0]; b = men[0]; women.RemoveAt(0); men.RemoveAt(0); }
                var h = HouseholdModule.Form(s, null, a, JoinReason.Founded);
                HouseholdModule.Join(s, null, h, b, JoinReason.Founded, 0);
                Partnerships.Form(s, null, a.Id, b.Id, Math.Min(a.Id, b.Id), h.Id);
                KinCues.Believe(s, null, a, b.Id, KinRole.Partner, 900, KinSource.Partnered);
                KinCues.Believe(s, null, b, a.Id, KinRole.Partner, 900, KinSource.Partnered);
                homes.Add(h); couplesHomes.Add(h);
            }
            var singles = new List<Npc>(women); singles.AddRange(men);
            singles.Sort((x, y) => x.Id.CompareTo(y.Id));
            foreach (var n in singles) homes.Add(HouseholdModule.Form(s, null, n, JoinReason.Founded));
            var raise = couplesHomes.Count > 0 ? couplesHomes : homes;
            for (int c = 0; c < children.Count; c++)
            {
                var child = children[c];
                if (raise.Count == 0) { homes.Add(HouseholdModule.Form(s, null, child, JoinReason.Founded)); continue; }
                var h = raise[c % raise.Count];
                if (!HouseholdModule.Join(s, null, h, child, JoinReason.Founded, 0)) { homes.Add(HouseholdModule.Form(s, null, child, JoinReason.Founded)); continue; }
                foreach (int m in h.Members)
                {
                    var adult = p.Find(m);
                    if (adult == null || adult.Id == child.Id || adult.Stage == LifeStage.Child) continue;
                    KinCues.Believe(s, null, child, adult.Id, KinCues.ParentRole(adult.Life.Sex), 900, KinSource.Raised);
                    KinCues.Believe(s, null, adult, child.Id, KinCues.ChildRole(child.Life.Sex), 900, KinSource.Raised);
                }
                foreach (int m in h.Members)
                {
                    var sib = p.Find(m);
                    if (sib == null || sib.Id == child.Id || sib.Stage != LifeStage.Child) continue;
                    KinCues.Believe(s, null, child, sib.Id, KinRole.FullSibling, 800, KinSource.Raised);
                    KinCues.Believe(s, null, sib, child.Id, KinRole.FullSibling, 800, KinSource.Raised);
                }
            }
            if (ft.StartHuts == 0) return;
            foreach (var h in homes)
            {
                Npc first = null;
                foreach (int m in h.Members) { var n = p.Find(m); if (n != null && n.Stage != LifeStage.Child) { first = n; break; } }
                if (first == null) continue;
                int node = FreeNode(s, first.Node);
                if (node < 0) continue;
                var adultsIn = new List<int>();
                foreach (int m in h.Members) { var n = p.Find(m); if (n != null && n.Stage != LifeStage.Child && adultsIn.Count < s.Tuning.Population.HutBeds) adultsIn.Add(m); }
                var hut = PopulationModule.AddHut(s, f, node, true, adultsIn.ToArray());
                hut.FoundedBy = first.Id;
                HouseholdModule.AdoptHut(s, null, h, hut);
                if (ft.StoreFood > 0)
                {
                    int day = (int)SimCalendar.Of(s).Day(s.Tick);
                    Goods.Produce(s, null, Holder.Bin(h.Bin), Ware.Roots, ft.StoreFood, day + s.Tuning.Ecology.RootSpoilDays, 0, Owner.Household(h.Id), ProduceReason.Founder, 0, 0);
                }
            }
        }

        /// <summary>A free node for a founder hut near a node (the hut rules of M3: land, nothing built, no source, away from huts and HQ circles).</summary>
        public static int FreeNode(GodState s, int near)
        {
            var g = s.Territory.Grid;
            var t = s.Territory;
            var tu = s.Tuning.Population;
            for (int radius = 0; radius <= 6; radius++)
                for (int dir = 0; dir < 6; dir++)
                {
                    int n = near;
                    for (int k = 0; k < radius; k++) { int v = g.Neighbour(n, (dir + k) % 6); if (v >= 0) n = v; }
                    if (!g.IsLand(n) || t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                    bool ok = true;
                    foreach (var hq in t.Hqs) if (g.InCircle(hq.Node, n)) { ok = false; break; }
                    if (!ok) continue;
                    foreach (var h in s.Population.Huts) if (g.Distance(h.Node, n) < tu.HutSpacing) { ok = false; break; }
                    if (!ok) continue;
                    foreach (var src in s.Population.Sources) if (src.Node == n) { ok = false; break; }
                    if (ok) return n;
                }
            return -1;
        }
    }

    /// <summary>
    /// The off-island arrival rule (POPULATION "Immigration", Q16): a temporary automatic rule, off by default, never
    /// replenishment (its inputs are food per head and free beds, never deaths or vacancies). Per faction once a day:
    /// <c>clamp(10 x (FoodDaysPerHead - 5) + 20 x FreeBeds, 0, 150)</c> permille, at most 15% of the faction a year.
    /// </summary>
    public static class Immigration
    {
        public static int ChancePermille(GodState s, Faction f, out int alive)
        {
            var p = s.Population;
            long food = 0; alive = 0;
            var seen = new HashSet<int>();
            foreach (var n in p.Npcs)
            {
                if (!n.Alive || n.Faction != f) continue;
                alive++;
                foreach (var l in n.Carry) if (!l.Empty && Wares.IsFood(l.Ware)) food += l.Count;
                if (n.Household != 0 && seen.Add(n.Household))
                {
                    var h = s.Households.Find(n.Household);
                    var bin = h != null && h.Bin != 0 ? Goods.BinById(p, h.Bin) : null;
                    if (bin != null) foreach (var l in bin.Lots) if (Wares.IsFood(l.Ware)) food += l.Count;
                }
            }
            int beds = 0;
            foreach (var hut in p.Huts) if (hut.Built && hut.Faction == f) beds += hut.FreeBeds;
            int foodDays = alive == 0 ? 0 : (int)(food / alive);
            return Math.Max(0, Math.Min(150, 10 * (foodDays - 5) + 20 * beds));
        }

        public static void Daily(GodState s, EventBuffer ev, int day)
        {
            var hs = s.Households;
            var p = s.Population;
            var cal = SimCalendar.Of(s);
            long year = cal.Year(s.Tick);
            if (hs.ImmigrationYear != year) { hs.ImmigrationYear = year; Array.Clear(hs.ImmigrantsThisYear, 0, hs.ImmigrantsThisYear.Length); }
            foreach (var f in s.Setup.Factions)
            {
                int chance = ChancePermille(s, f, out int alive);
                if (alive == 0) continue; // a faction that died out draws no newcomers (no replenishment)
                if (hs.ImmigrantsThisYear[(int)f] >= Math.Max(1, alive * 15 / 100)) continue;
                var r = s.Rng.Fork("immigration", (int)f, day);
                if (r.NextInt(1000) >= chance) continue;
                var hqs = s.Territory.HqsOf(f);
                if (hqs.Count == 0) continue;
                int node = Shore(s, hqs[0].Node);
                hs.Creating = true;
                Npc npc;
                try { npc = PopulationModule.AddNpc(s, f, node, InstinctSet.Default, s.Tuning.Learning.DefaultMode, false); }
                finally { hs.Creating = false; }
                var g = s.Rng.Fork("genome-founder", npc.Id);
                var genome = GenomeOps.Founder(g, s.Tuning.Genetics.FounderSpread);
                byte sex = (byte)g.NextInt(2);
                Founders.MakeLife(s, npc, sex, genome, 0, 0, 0);
                int age = r.NextRange(1000, 2201);
                Founders.SetAge(s, npc, age);
                Goods.Produce(s, ev, Holder.Carry(npc.Id), Ware.Roots, 2, day + s.Tuning.Ecology.RootSpoilDays, 0, Owner.Npc(npc.Id), ProduceReason.Placed, npc.Id, 0);
                HouseholdModule.Form(s, ev, npc, JoinReason.Founded);
                hs.ImmigrantsThisYear[(int)f]++;
                hs.Stats.Immigrants++;
                ev.Add(new NpcImmigrated { Npc = npc.Id, Faction = f, Node = node, AgeMilli = age, Sex = sex, GenomeHash = GenomeOps.Hash(genome) });
                ev.Add(NpcGenome.Of(npc));
            }
        }

        /// <summary>The coast node nearest a faction's HQ (the island's edge).</summary>
        private static int Shore(GodState s, int hq)
        {
            var g = s.Territory.Grid;
            int best = hq, bestD = int.MaxValue;
            for (int n = 0; n < g.Count; n++)
            {
                if (!PopulationModule.IsCoast(g, n)) continue;
                int d = g.Distance(n, hq);
                if (d < bestD) { bestD = d; best = n; }
            }
            return best;
        }
    }
}
