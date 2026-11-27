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
            if (ft.Generator == 1) { foreach (var f in s.Setup.Factions) Established(s, f); return; }
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
        /// personality and learning rates the decider uses (<c>Npc.Traits</c> 0-11; the instinct gains reach the mind through
        /// <c>SelfView</c>, I-BS amendment 12.33; slots 12-15 stay reserved, zero, so later trait slots keep their indices);
        /// lifespan; the pedigree record.
        /// </summary>
        public static void MakeLife(GodState s, Npc npc, byte sex, byte[] genome, int mother, int father, int generation)
        {
            var lt = s.Tuning.Households.Life;
            var life = new NpcLife { Sex = sex, Mother = mother, BioFather = father, Generation = generation, Genome = genome };
            var dev = s.Rng.Fork("develop", npc.Id);
            life.Noise = PhenotypeBuilder.Noise(dev, s.Tuning.Genetics.NoisePermille);
            var traits = new int[Preferences.TraitLength];
            PhenotypeBuilder.Birth(genome, LifeCycleSystem.MapOf(s), life.Noise, ref life.Pheno, traits);
            // D4: individual preferences (the decider reads them as traits 16-18)
            Preferences.Draw(s, npc, life);
            traits[Preferences.TraitCompanionship] = life.Companionship; traits[Preferences.TraitChildDesire] = life.ChildDesire; traits[Preferences.TraitCommitment] = life.Commitment;
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
            Homes(s, f, homes);
        }

        /// <summary>A hut (and a store of food) for each founder household with an adult, when the scenario says so.</summary>
        private static void Homes(GodState s, Faction f, List<Household> homes)
        {
            var p = s.Population;
            var ft = s.Tuning.Households.Founders;
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

        /// <summary>
        /// The established band (owner D4 §4, M4.42) of one faction. The founders (ascending id) are shuffled with
        /// <c>Rng.Fork("founder-band", faction)</c> and given classes in that order: elders, mature adults, dependent children,
        /// young adults; ages are drawn uniformly within each class, so birth dates spread and founder deaths do not fall
        /// together. Mature adults form couples (some same-sex) and singles; each child joins a mature household as its
        /// dependant: a mixed couple's own biological child when the woman is at least 1.0 older (its genome from one gamete of
        /// each, <c>Rng.Fork("conceive", childId)</c>, as a birth), otherwise raised by them as an adopted child (a founder
        /// genetically). Elders live with a couple as grandparents (believed kin, not genetic). Young adults pair or live alone.
        /// Every belief has its source (born, raised, partnered); nothing here reads a pedigree to decide a belief.
        /// </summary>
        public static void Established(GodState s, Faction f)
        {
            var p = s.Population;
            var ft = s.Tuning.Households.Founders;
            var gt = s.Tuning.Genetics;
            var lt = s.Tuning.Households.Life;
            var map = LifeCycleSystem.MapOf(s);
            var members = new List<Npc>();
            foreach (var n in p.Npcs) if (n.Founder && n.Faction == f) members.Add(n);
            if (members.Count == 0) return;
            var r = s.Rng.Fork("founder-band", (int)f);
            var order = new List<Npc>(members);
            for (int i = order.Count - 1; i > 0; i--) { int j = r.NextInt(i + 1); var t = order[i]; order[i] = order[j]; order[j] = t; }
            int total = order.Count;
            int nElder = total * ft.ElderPermille / 1000, nMature = total * ft.MaturePermille / 1000, nChild = total * ft.ChildPermille / 1000;
            if (nMature == 0 && nChild > 0) nMature = 1;
            var genomes = new Dictionary<int, byte[]>(); var sexes = new Dictionary<int, byte>(); var ages = new Dictionary<int, int>();
            foreach (var n in members)
            {
                var gr = s.Rng.Fork("genome-founder", n.Id);
                genomes[n.Id] = GenomeOps.Founder(gr, gt.FounderSpread);
                sexes[n.Id] = (byte)gr.NextInt(2);
            }
            var elders = new List<Npc>(); var mature = new List<Npc>(); var kids = new List<Npc>(); var young = new List<Npc>();
            for (int i = 0; i < order.Count; i++)
            {
                var n = order[i];
                if (i < nElder) { elders.Add(n); ages[n.Id] = r.NextRange(ft.ElderMinMilli, ft.ElderMaxMilli + 1); }
                else if (i < nElder + nMature) { mature.Add(n); ages[n.Id] = r.NextRange(ft.MatureMinMilli, ft.MatureMaxMilli + 1); }
                else if (i < nElder + nMature + nChild) { kids.Add(n); ages[n.Id] = r.NextRange(ft.ChildMinMilli, ft.ChildMaxMilli + 1); }
                else { young.Add(n); ages[n.Id] = r.NextRange(ft.AgeMinMilli, ft.AgeMaxMilli + 1); }
            }
            // couples: mixed first, then same-sex by the scenario's share
            List<(Npc a, Npc b)> Pair(List<Npc> pool, int permille)
            {
                var women = new List<Npc>(); var men = new List<Npc>();
                foreach (var n in pool) if (sexes[n.Id] == 0) women.Add(n); else men.Add(n);
                int couples = pool.Count * permille / 2000;
                int same = (couples * ft.SameSexPermille + 500) / 1000;
                var res = new List<(Npc, Npc)>();
                for (int k = 0; k < couples; k++)
                {
                    bool wantSame = k >= couples - same;
                    if (!wantSame && women.Count > 0 && men.Count > 0) { res.Add((women[0], men[0])); women.RemoveAt(0); men.RemoveAt(0); continue; }
                    var big = women.Count >= men.Count ? women : men;
                    if (big.Count >= 2) { res.Add((big[0], big[1])); big.RemoveRange(0, 2); continue; }
                    if (women.Count > 0 && men.Count > 0) { res.Add((women[0], men[0])); women.RemoveAt(0); men.RemoveAt(0); }
                }
                foreach (var c in res) { pool.Remove(c.Item1); pool.Remove(c.Item2); }
                return res;
            }
            var matureSingles = new List<Npc>(mature);
            var matureCouples = Pair(matureSingles, ft.CouplePermille);
            var youngSingles = new List<Npc>(young);
            var youngCouples = Pair(youngSingles, ft.YoungCouplePermille);
            // the households that raise children: mature couples (mixed first), then mature singles
            var raisers = new List<List<Npc>>();
            foreach (var c in matureCouples) if (sexes[c.a.Id] != sexes[c.b.Id]) raisers.Add(new List<Npc> { c.a, c.b });
            foreach (var c in matureCouples) if (sexes[c.a.Id] == sexes[c.b.Id]) raisers.Add(new List<Npc> { c.a, c.b });
            foreach (var n in matureSingles) raisers.Add(new List<Npc> { n });
            var parentOf = new Dictionary<int, (int mother, int father)>();
            var raisedBy = new Dictionary<int, List<Npc>>();
            for (int c = 0; c < kids.Count; c++)
            {
                var kid = kids[c];
                if (raisers.Count == 0) break;
                var home = raisers[c % raisers.Count];
                raisedBy[kid.Id] = home;
                if (home.Count == 2 && sexes[home[0].Id] != sexes[home[1].Id])
                {
                    var mother = sexes[home[0].Id] == 0 ? home[0] : home[1];
                    var father = mother == home[0] ? home[1] : home[0];
                    if (ages[mother.Id] - ages[kid.Id] >= lt.FertileFromMilli && ages[father.Id] - ages[kid.Id] >= lt.FertileFromMilli) parentOf[kid.Id] = (mother.Id, father.Id);
                }
            }
            // lives: everyone but the biological children first (their parents' genomes are fixed), then those children
            foreach (var n in members)
            {
                if (parentOf.ContainsKey(n.Id)) continue;
                if (n.Mind != null) n.Mind.Kin = new KinMind();
                MakeLife(s, n, sexes[n.Id], genomes[n.Id], 0, 0, 0);
                SetAge(s, n, ages[n.Id]);
            }
            foreach (var n in members)
            {
                if (!parentOf.TryGetValue(n.Id, out var par)) continue;
                var mother = p.Find(par.mother); var father = p.Find(par.father);
                var cr = s.Rng.Fork("conceive", n.Id);
                var mat = new byte[GeneMap.Loci]; var pat = new byte[GeneMap.Loci];
                var muts = new List<Mutation>();
                GenomeOps.Gamete(mother.Life.Genome, cr, gt, map, mat, muts);
                GenomeOps.Gamete(father.Life.Genome, cr, gt, map, pat, muts);
                if (n.Mind != null) n.Mind.Kin = new KinMind();
                MakeLife(s, n, sexes[n.Id], GenomeOps.Combine(mat, pat), mother.Id, father.Id, 1);
                SetAge(s, n, ages[n.Id]);
            }
            // households
            var homes = new List<Household>();
            Household Couple(Npc a, Npc b)
            {
                var h = HouseholdModule.Form(s, null, a, JoinReason.Founded);
                HouseholdModule.Join(s, null, h, b, JoinReason.Founded, 0);
                Partnerships.Form(s, null, a.Id, b.Id, Math.Min(a.Id, b.Id), h.Id);
                KinCues.Believe(s, null, a, b.Id, KinRole.Partner, 900, KinSource.Partnered);
                KinCues.Believe(s, null, b, a.Id, KinRole.Partner, 900, KinSource.Partnered);
                homes.Add(h);
                return h;
            }
            var homeOf = new Dictionary<List<Npc>, Household>();
            foreach (var home in raisers)
            {
                var h = home.Count == 2 ? Couple(home[0], home[1]) : HouseholdModule.Form(s, null, home[0], JoinReason.Founded);
                if (home.Count == 1) homes.Add(h);
                homeOf[home] = h;
            }
            foreach (var kid in kids)
            {
                if (!raisedBy.TryGetValue(kid.Id, out var home) || !HouseholdModule.Join(s, null, homeOf[home], kid, JoinReason.Founded, 0))
                { homes.Add(HouseholdModule.Form(s, null, kid, JoinReason.Founded)); continue; }
                bool bio = parentOf.ContainsKey(kid.Id);
                if (!bio) { kid.Life.Fostered = true; kid.Life.ToldFostered = ages[kid.Id] >= lt.ForageFromMilli; }
                foreach (var adult in home)
                {
                    if (bio) { KinCues.Believe(s, null, adult, kid.Id, KinCues.ChildRole(kid.Life.Sex), 950, KinSource.Born); KinCues.Believe(s, null, kid, adult.Id, KinCues.ParentRole(adult.Life.Sex), 900, KinSource.Born); }
                    else if (kid.Life.ToldFostered) { KinCues.Believe(s, null, adult, kid.Id, KinRole.FosterChild, 800, KinSource.Raised); KinCues.Believe(s, null, kid, adult.Id, KinRole.FosterParent, 800, KinSource.Told); }
                    else { KinCues.Believe(s, null, adult, kid.Id, KinCues.ChildRole(kid.Life.Sex), 900, KinSource.Raised); KinCues.Believe(s, null, kid, adult.Id, KinCues.ParentRole(adult.Life.Sex), 900, KinSource.Raised); }
                }
            }
            // siblings: children raised in one household believe each other siblings (full when both are the couple's own)
            foreach (var home in raisers)
            {
                var h = homeOf[home];
                foreach (int a in h.Members)
                    foreach (int b in h.Members)
                    {
                        if (a >= b) continue;
                        var x = p.Find(a); var y = p.Find(b);
                        if (x.Stage != LifeStage.Child || y.Stage != LifeStage.Child) continue;
                        KinCues.Believe(s, null, x, y.Id, KinRole.FullSibling, 800, KinSource.Raised);
                        KinCues.Believe(s, null, y, x.Id, KinRole.FullSibling, 800, KinSource.Raised);
                    }
            }
            // elders: each lives with a couple (those with children first) as a grandparent of its children
            var withKids = new List<Household>();
            foreach (var home in raisers) if (home.Count == 2) withKids.Add(homeOf[home]);
            for (int e = 0; e < elders.Count; e++)
            {
                var elder = elders[e];
                Household h = null;
                for (int k = 0; k < withKids.Count && h == null; k++) { var c = withKids[(e + k) % withKids.Count]; if (c.Members.Count < Household.MaxMembers) h = c; }
                if (h == null || !HouseholdModule.Join(s, null, h, elder, JoinReason.Founded, 0)) { homes.Add(HouseholdModule.Form(s, null, elder, JoinReason.Founded)); continue; }
                foreach (int m in h.Members)
                {
                    var o = p.Find(m);
                    if (o == null || o.Id == elder.Id) continue;
                    if (o.Stage == LifeStage.Child) { KinCues.Believe(s, null, elder, o.Id, KinRole.Grandchild, 800, KinSource.Told); KinCues.Believe(s, null, o, elder.Id, KinRole.Grandparent, 800, KinSource.Told); }
                    else { KinCues.Believe(s, null, elder, o.Id, KinRole.InLaw, 700, KinSource.Told); KinCues.Believe(s, null, o, elder.Id, KinRole.InLaw, 700, KinSource.Told); }
                }
            }
            foreach (var c in youngCouples) Couple(c.a, c.b);
            foreach (var n in youngSingles) homes.Add(HouseholdModule.Form(s, null, n, JoinReason.Founded));
            homes.Sort((x, y) => x.Id.CompareTo(y.Id));
            Homes(s, f, homes);
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
