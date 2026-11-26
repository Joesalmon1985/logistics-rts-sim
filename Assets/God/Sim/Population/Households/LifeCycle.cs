using System;
using System.Collections.Generic;
using God.Sim.Learning;
using God.Sim.Population.Genetics;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The life cycle (POPULATION 4, order 240 in the tick; SHARED_CONTRACTS 9.2). Per tick: births that are due (mothers
    /// ascending), and every few ticks whether each child has an adult near. At the first tick of a day: yesterday's
    /// person-day columns, store spoilage. Once a day per person, at its slot (<c>1 + (id % 8) x TicksPerDay / 8</c>, so no
    /// person changes at a day boundary): ageing and stage changes, natural death, growth, hazards, illness, conception and
    /// miscarriage. Once a day the households' work: homes, orphans, the household snapshot, fouling; and immigration when on.
    /// Nothing here sets a population: every birth is a conception by two partners who both chose it, every death has a
    /// physical cause, and no rule creates an NPC to replace one (M4.25).
    /// </summary>
    public sealed class LifeCycleSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            var hs = s.Households;
            var p = s.Population;
            if (hs == null || p == null || s.Tuning.Households.Enabled == 0) return;
            var cal = SimCalendar.Of(s);
            int tpd = cal.TicksPerDay;
            int tod = cal.TickOfDay(s.Tick);
            int day = (int)cal.Day(s.Tick);
            if (!hs.Announced) Announce(s, ev);
            if (tod == 0) DayEnd(s, ev, day);
            // the arrival rule first in its tick, from the state as the tick found it
            if (tod == 3 && hs.ImmigrationDay != day) { hs.ImmigrationDay = day; if (s.Tuning.Households.Immigration != 0) Immigration.Daily(s, ev, day); }
            var lt = s.Tuning.Households.Life;
            bool check = s.Tick % Math.Max(1, lt.AttendCheckTicks) == 0;
            int slotLen = Math.Max(1, tpd / 8);
            // iterate over a snapshot of the count: births append (higher ids), and a newborn starts tomorrow
            int n = p.Npcs.Count;
            for (int i = 0; i < n; i++)
            {
                var npc = p.Npcs[i];
                if (!npc.Alive || npc.Life == null) continue;
                var life = npc.Life;
                if (tod != 0 && life.PregFather != 0 && s.Tick >= life.PregDue) { Reproduction.Birth(s, ev, npc, day); if (!npc.Alive) continue; }
                if (check && npc.Stage == LifeStage.Child) Attend(s, npc, lt.AttendNodes);
                if (tod == 1 + (npc.Id % 8) * slotLen && life.LastDaily != day) { life.LastDaily = day; Daily(s, ev, npc, day); }
            }
            if (tod == 2 && hs.DayDone != day) { hs.DayDone = day; HouseholdsDaily(s, ev, day); }
        }

        /// <summary>The start as events (made before the first tick, with no event buffer): founders' genomes, households, members, partnerships.</summary>
        private static void Announce(GodState s, EventBuffer ev)
        {
            var hs = s.Households;
            hs.Announced = true;
            foreach (var n in s.Population.Npcs) if (n.Alive && n.Life != null) ev.Add(NpcGenome.Of(n));
            foreach (var h in hs.Households)
            {
                ev.Add(new HouseholdFormed { Household = h.Id, Faction = h.Faction, Hut = h.Hut, Founder = h.Members.Count > 0 ? h.Members[0] : 0, Reason = JoinReason.Founded });
                foreach (int m in h.Members) ev.Add(new HouseholdJoined { Npc = m, Household = h.Id, Reason = JoinReason.Founded });
            }
            foreach (var p in hs.Partnerships) ev.Add(new PartnershipFormed { Partnership = p.Id, A = p.A, B = p.B, Initiator = p.Initiator });
        }

        // ---------------- the day boundary ----------------

        /// <summary>Yesterday's M4 person-day columns (one per person who gets an NpcDay row), the day's counters reset, store spoilage.</summary>
        private static void DayEnd(GodState s, EventBuffer ev, int day)
        {
            var p = s.Population;
            var cal = SimCalendar.Of(s);
            if (p.DayDone >= 0)
                foreach (var npc in p.Npcs)
                {
                    if (!npc.Alive || npc.Life == null) continue;
                    var l = npc.Life;
                    ev.Add(new PersonDayLife
                    {
                        Npc = npc.Id, Day = day - 1, AgeMilli = (int)cal.AgeMilli(npc.BornTick, s.Tick), Household = npc.Household, Partner = FirstPartner(s, npc.Id),
                        CareGivenTicks = npc.DayTicks[Activity.Care], CareReceivedTicks = l.CareReceived, Growth = l.Growth, Stage = npc.Stage, Sex = l.Sex,
                        Pregnant = l.PregFather != 0, Ill = l.Illness == 1,
                    });
                    l.WorkTicks = npc.DayTicks[Activity.Forage] + npc.DayTicks[Activity.Gather];
                    l.CareGiven = npc.DayTicks[Activity.Care];
                    l.CareReceived = 0;
                    l.AloneShare = l.AttendChecks > 0 ? l.AloneChecks * 1000 / l.AttendChecks : 0;
                    l.AttendChecks = 0; l.AloneChecks = 0;
                }
            // food in bins spoils on its day like carried and ground food (ECONOMY A3)
            foreach (var b in p.Bins.ToArray())
                for (int k = b.Lots.Count - 1; k >= 0; k--)
                {
                    if (k >= b.Lots.Count) continue;
                    var lot = b.Lots[k];
                    if (lot.SpoilDay < 0 || lot.SpoilDay >= day) continue;
                    Goods.Consume(s, ev, Holder.Bin(b.Id), lot.Id, lot.Count, ConsumeReason.Spoiled, 0);
                }
        }

        public static int FirstPartner(GodState s, int npc)
        {
            foreach (var pr in s.Households.Partnerships) if (pr.A == npc) return pr.B; else if (pr.B == npc) return pr.A;
            return 0;
        }

        // ---------------- protection ----------------

        /// <summary>
        /// A child's protection check (every few ticks): is an adult near? When the adults near are not of its own household
        /// but a carer of a shared-care arrangement with it, that carer is minding it: those ticks count as care given in the
        /// arrangement's record (Q15: minding children is care work), and returned when the asker minds the carer's children.
        /// </summary>
        private static void Attend(GodState s, Npc child, int nodes)
        {
            var g = s.Territory.Grid;
            var life = child.Life;
            life.AttendChecks++;
            bool own = false; Npc minder = null;
            foreach (var o in s.Population.Npcs)
            {
                if (!o.Alive || o.Id == child.Id || o.Stage == LifeStage.Child) continue;
                if (g.Distance(o.Node, child.Node) > nodes) continue;
                if (child.Household != 0 && o.Household == child.Household) { own = true; break; }
                if (minder == null) minder = o;
            }
            if (!own && minder == null) { life.AloneChecks++; return; }
            if (own || child.Household == 0) return;
            int ticks = s.Tuning.Households.Life.AttendCheckTicks;
            foreach (var c in s.Households.Care)
            {
                if (c.AskerHousehold == child.Household && IsNear(s, c.Carer, child, nodes)) { c.Given += ticks; life.CareReceived += ticks; break; }
                if (c.CarerHousehold == child.Household && IsNear(s, c.Asker, child, nodes)) { c.Returned += ticks; life.CareReceived += ticks; break; }
            }
        }

        private static bool IsNear(GodState s, int adult, Npc child, int nodes)
        {
            var a = s.Population.Find(adult);
            return a != null && a.Alive && s.Territory.Grid.Distance(a.Node, child.Node) <= nodes;
        }

        // ---------------- one person's day ----------------

        private static void Daily(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var cal = SimCalendar.Of(s);
            var life = npc.Life;
            var lt = s.Tuning.Households.Life;
            long age = cal.AgeMilli(npc.BornTick, s.Tick);
            // ageing and stage
            var stage = SimCalendar.StageOf(age);
            if (stage != npc.Stage)
            {
                var from = npc.Stage;
                npc.Stage = stage;
                if (from == LifeStage.Child) Mature(s, ev, npc);
                ev.Add(new NpcMatured { Npc = npc.Id, From = from, To = stage, AgeMilli = (int)age });
            }
            if (age >= life.MaxAgeMilli) { PopulationModule.Kill(s, ev, npc, DeathCause.OldAge); return; }
            if (npc.Stage == LifeStage.Child)
            {
                int nut = npc.NutritionPermille;
                life.Growth = Math.Min(1000, life.Growth + Math.Max(0, (nut - lt.GrowthNutritionFrom) / Math.Max(1, lt.GrowthDivisor)));
                life.ChildNutSum += nut; life.ChildNutDays++;
                // the child's protection: a share of yesterday spent with no adult near
                int perMillion = age < 250 ? lt.HazardInfantPerMillion : age < 500 ? lt.HazardYoungPerMillion : lt.HazardChildPerMillion;
                long chance = (long)perMillion * life.AloneShare / 1000;
                if (chance > 0 && s.Rng.Fork("hazard", npc.Id, day).NextInt(1000000) < chance)
                {
                    var cause = age < 500 ? DeathCause.Exposure : DeathCause.Accident;
                    ev.Add(new ChildUnattendedHazard { Child = npc.Id, AlonePermille = life.AloneShare, Cause = cause, Died = true });
                    PopulationModule.Kill(s, ev, npc, cause);
                    return;
                }
            }
            WorkHazard(s, ev, npc, day);
            if (!npc.Alive) return;
            Disease.Daily(s, ev, npc, day);
            if (!npc.Alive) return;
            if (life.TryWith != 0 && life.TryingUntil < s.Tick) { ev.Add(new TryForChildStopped { Npc = npc.Id, Partner = life.TryWith }); life.TryWith = 0; }
            Reproduction.Daily(s, ev, npc, day, age);
            RaisingCue(s, ev, npc, day);
        }

        /// <summary>Adulthood (POPULATION "Adulthood"): the physical phenotype is fixed from the childhood nutrition; a lodging child takes a bed when one is free.</summary>
        private static void Mature(GodState s, EventBuffer ev, Npc npc)
        {
            var life = npc.Life;
            int nutMean = life.ChildNutDays > 0 ? (int)(life.ChildNutSum / life.ChildNutDays) : 1000;
            if (life.Genome != null && life.Noise != null) PhenotypeBuilder.Adult(life.Genome, MapOf(s), life.Noise, nutMean, ref life.Pheno);
            life.Matured = true;
            life.Growth = 1000;
            if (npc.HomeHut != 0) { var hut = s.Population.HutById(npc.HomeHut); if (hut != null) HutRights.Mature(s, ev, hut, npc); }
        }

        public static ReadOnlyArray<LocusDef> MapOf(GodState s) => GeneMap.Default;

        private static void WorkHazard(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var lt = s.Tuning.Households.Life;
            var life = npc.Life;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            if (life.WorkTicks > 0)
            {
                // a full work day is a quarter of the day's ticks; the chance scales with the work done
                long chance = (long)lt.ForageHazardPerMillion * Math.Min(1000, life.WorkTicks * 4000 / Math.Max(1, tpd)) / 1000;
                var r = s.Rng.Fork("hazard", npc.Id, -1 - day);
                if (r.NextInt(1000000) < chance)
                {
                    int amount = 100 + r.NextInt(201);
                    npc.Injury = Math.Min(1000000, npc.Injury + amount * 1000);
                    ev.Add(new InjuryTaken { Npc = npc.Id, Amount = amount, Cause = 1 });
                }
                life.WorkTicks = 0;
            }
            if (npc.Injury > lt.InjuryDeadlyFrom * 1000 && s.Rng.Fork("mortality", npc.Id, day).NextInt(1000) < lt.InjuryDeathPermille)
                PopulationModule.Kill(s, ev, npc, DeathCause.Wound);
        }

        /// <summary>
        /// The raising cue (BIOLOGICAL 4.6 caregiving): a child fed by the same adult on several days, not told otherwise,
        /// comes to believe that adult a parent, whoever it truly is (the cuckoo case; M4.18 "Kin_Belief_CanBeWrong").
        /// </summary>
        private static void RaisingCue(GodState s, EventBuffer ev, Npc child, int day)
        {
            var life = child.Life;
            if (child.Stage != LifeStage.Child || life.CueCarer == 0 || life.ToldFostered) return;
            if (life.CueDays < s.Tuning.Households.Care.FosterCueDays) return;
            var role = KinData.RoleOf(child.Mind, life.CueCarer);
            if (role == KinRole.Mother || role == KinRole.Father) return;
            var carer = s.Population.Find(life.CueCarer);
            if (carer?.Life == null || !carer.Alive) return;
            // a parent belief already held about someone else of that role stays; the cue adds one for the carer
            KinCues.Believe(s, ev, child, carer.Id, KinCues.ParentRole(carer.Life.Sex), 500, KinSource.Raised);
        }

        /// <summary>Counts a feeding for the raising cue (at most once a day; a new carer restarts the count).</summary>
        public static void FedBy(Npc child, Npc feeder, int day)
        {
            var l = child.Life;
            if (l == null || l.CueDay == day) return;
            if (l.CueCarer != feeder.Id) { l.CueCarer = feeder.Id; l.CueDays = 0; }
            l.CueDays++;
            l.CueDay = day;
        }

        // ---------------- households, once a day ----------------

        private static void HouseholdsDaily(GodState s, EventBuffer ev, int day)
        {
            var hs = s.Households;
            var p = s.Population;
            var dt = s.Tuning.Households.Disease;
            ReproductiveUnions.Daily(s, ev);
            foreach (var h in hs.Households.ToArray())
            {
                if (h.Members.Count == 0) continue;
                // a home: a member's built hut becomes the household's (and its store is made there)
                if (h.Hut == 0)
                    foreach (int m in h.Members)
                    {
                        var n = p.Find(m);
                        if (n == null || n.HomeHut == 0) continue;
                        var hut = p.HutById(n.HomeHut);
                        if (hut != null && hut.Built) { HouseholdModule.AdoptHut(s, ev, h, hut); break; }
                    }
                else
                {
                    var hut = p.HutById(h.Hut);
                    if (hut == null || !hut.Built) h.Hut = 0;
                    else foreach (int m in h.Members) { var n = p.Find(m); if (n != null && n.Alive && n.HomeHut == 0) HouseholdModule.GrantHome(s, ev, h, n); }
                }
                int children = 0, elders = 0, adults = 0;
                foreach (int m in h.Members)
                {
                    var n = p.Find(m);
                    if (n == null) continue;
                    if (n.Stage == LifeStage.Child) children++; else adults++;
                    if (n.Stage == LifeStage.Elder) elders++;
                }
                // orphans: children with no living adult in their household (HOUSEHOLDS "Fostering and orphans")
                if (adults == 0)
                    foreach (int m in h.Members)
                    {
                        var n = p.Find(m);
                        if (n?.Life == null || n.Stage != LifeStage.Child || n.Life.OrphanSince >= 0) continue;
                        n.Life.OrphanSince = day;
                        hs.Stats.Orphaned++;
                        ev.Add(new ChildOrphaned { Child = n.Id, Household = h.Id });
                    }
                int food = 0, units = 0;
                var bin = h.Bin != 0 ? Goods.BinById(p, h.Bin) : null;
                if (bin != null) foreach (var l in bin.Lots) { units += l.Count; if (Wares.IsFood(l.Ware)) food += l.Count; }
                ev.Add(new HouseholdDay { Household = h.Id, Day = day, Faction = h.Faction, Hut = h.Hut, Members = h.Members.Count, Children = children, Elders = elders, StoreFood = food, StoreUnits = units });
            }
            // fouling (POPULATION "Sanitation"): each hex loses a share and gains from the people on it
            var f = hs.Fouling;
            if (f != null)
            {
                for (int i = 0; i < f.Length; i++) f[i] -= f[i] * dt.FoulRegenPermille / 1000;
                var g = s.Territory.Grid;
                foreach (var n in p.Npcs)
                {
                    if (!n.Alive || n.Life == null) continue;
                    int hex = g.HexOf(n.Node);
                    if (hex >= 0 && hex < f.Length) f[hex] = Math.Min(dt.FoulMax, f[hex] + dt.FoulPerPerson);
                }
            }
        }
    }

    /// <summary>
    /// Reproduction (POPULATION "Fertility", "Birth"; BIOLOGICAL 4.5): conception, pregnancy and birth. One of the two places
    /// that may read a person's sex (with the phenotype builder): a conception needs a woman and a man; nothing else differs.
    /// </summary>
    public static class Reproduction
    {
        /// <summary>The mother's daily check: miscarriage while pregnant; else conception when both partners hold "try for a child".</summary>
        public static void Daily(GodState s, EventBuffer ev, Npc mother, int day, long age)
        {
            var life = mother.Life;
            var lt = s.Tuning.Households.Life;
            if (life.Sex != 0) return;
            if (life.PregFather != 0)
            {
                if (mother.NutritionPermille < lt.MiscarryBelowNutrition && s.Rng.Fork("fertility", mother.Id, day).NextInt(1000) < lt.MiscarryPermille)
                {
                    life.PregFather = 0;
                    s.Households.Stats.Miscarriages++;
                    ev.Add(new PregnancyEnded { Mother = mother.Id, Outcome = PregnancyEnded.Miscarriage });
                }
                return;
            }
            if (s.Tuning.Households.Partner.NoConception != 0) return;
            if (mother.Stage == LifeStage.Child || age < lt.FertileFromMilli) return;
            int end = FertileEnd(lt, life.Pheno.Fertility);
            if (age > end) return;
            var cal = SimCalendar.Of(s);
            if (life.LastBirthTick >= 0 && s.Tick - life.LastBirthTick < cal.TicksForAgeMilli(lt.AfterBirthMilli)) return;
            if (life.TryingUntil < s.Tick || life.TryWith == 0) return;
            var father = s.Population.Find(life.TryWith);
            if (father?.Life == null || !father.Alive || father.Life.Sex != 1 || father.Stage == LifeStage.Child) return;
            if (father.Life.TryWith != mother.Id || father.Life.TryingUntil < s.Tick) return;
            // D4: a conception needs a reproductive relationship between the two. Romantic partners who both hold "try for a
            // child" with each other have agreed one (formed here, when both have room); others agreed one by offer and acceptance.
            var union = ReproductiveUnions.Between(s, mother.Id, father.Id);
            if (union == null && Partnerships.Between(s, mother.Id, father.Id) != null && ReproductiveUnions.HasRoom(s, mother) && ReproductiveUnions.HasRoom(s, father))
                union = ReproductiveUnions.Form(s, ev, father, mother, father.Id, ReproductiveUnion.Partners);
            if (union == null) return;
            union.LastTryTick = s.Tick;
            int chance = DailyPermille(lt, life.Pheno.Fertility, mother.NutritionPermille, (int)age, end);
            if (s.Rng.Fork("fertility", mother.Id, day).NextInt(1000) >= chance) return;
            life.PregFather = father.Id;
            life.PregStart = s.Tick;
            life.PregDue = s.Tick + cal.TicksForAgeMilli(lt.PregnancyMilli);
            s.Households.Stats.Conceptions++;
            ev.Add(new PregnancyStarted
            {
                Mother = mother.Id, BioFather = father.Id, MotherAgeMilli = (int)age, FatherAgeMilli = (int)cal.AgeMilli(father.BornTick, s.Tick), DueTick = life.PregDue,
            });
        }

        public static int FertileEnd(LifeTuning lt, int fertility) => lt.FertileEndBaseMilli + lt.FertileEndSpanMilli * fertility / 1000;

        /// <summary>
        /// D4: whether two people could conceive together, as anyone could see: two adults of different sexes (no knowledge of
        /// fertility). With the conception rule above, the only place outside the phenotype builder that reads sex (M4.28).
        /// </summary>
        public static bool MayConceiveWith(Npc a, Npc b) =>
            a?.Life != null && b?.Life != null && a.Id != b.Id && a.Stage != LifeStage.Child && b.Stage != LifeStage.Child && a.Life.Sex != b.Life.Sex;

        /// <summary><c>(Base + Span x Fertility/1000) x NutritionFactor/1000 x AgeFactor/1000</c> permille (POPULATION "Fertility").</summary>
        public static int DailyPermille(LifeTuning lt, int fertility, int nutrition, int age, int end)
        {
            long best = lt.ConceiveBasePermille + (long)lt.ConceiveSpanPermille * fertility / 1000;
            int nf = nutrition <= lt.NutritionNone ? 0 : nutrition >= lt.NutritionFull ? 1000 : (nutrition - lt.NutritionNone) * 1000 / Math.Max(1, lt.NutritionFull - lt.NutritionNone);
            int af = 1000;
            int from = end - lt.AgeFactorSpanMilli;
            if (age > from) af = 1000 - (1000 - lt.AgeFactorEndPermille) * (age - from) / Math.Max(1, lt.AgeFactorSpanMilli);
            return (int)(best * nf / 1000 * af / 1000);
        }

        /// <summary>
        /// A birth (POPULATION "Birth"): the child's genome from one gamete of each parent (<c>Rng.Fork("conceive", childId)</c>),
        /// its sex, phenotype and lifespan; it joins its mother's household with kin beliefs from the birth cues; an empty
        /// learning store (nothing learned is ever copied, M4.11). Childbirth hazards follow from the mother's nutrition and health.
        /// </summary>
        public static Npc Birth(GodState s, EventBuffer ev, Npc mother, int day)
        {
            var hs = s.Households;
            var p = s.Population;
            var lt = s.Tuning.Households.Life;
            var gt = s.Tuning.Genetics;
            var ml = mother.Life;
            var father = p.Find(ml.PregFather);
            int childId = p.NextNpcId;
            var rng = s.Rng.Fork("conceive", childId);
            var map = LifeCycleSystem.MapOf(s);
            byte[] genome;
            var muts = new List<Mutation>();
            int matMuts;
            if (gt.Fixed != 0 || ml.Genome == null || father?.Life?.Genome == null) { genome = GenomeOps.Founder(s.Rng.Fork("genome-founder", 0), gt.FounderSpread); matMuts = 0; }
            else
            {
                var mat = new byte[GeneMap.Loci]; var pat = new byte[GeneMap.Loci];
                GenomeOps.Gamete(ml.Genome, rng, gt, map, mat, muts);
                matMuts = muts.Count;
                GenomeOps.Gamete(father.Life.Genome, rng, gt, map, pat, muts);
                genome = GenomeOps.Combine(mat, pat);
            }
            byte sex = (byte)rng.NextInt(2);
            hs.Creating = true;
            Npc child;
            try { child = PopulationModule.AddNpc(s, mother.Faction, mother.Node, InstinctSet.Default, s.Tuning.Learning.DefaultMode, false); }
            finally { hs.Creating = false; }
            int gen = 1 + Math.Max(ml.Generation, father?.Life?.Generation ?? 0);
            Founders.MakeLife(s, child, sex, genome, mother.Id, father?.Id ?? 0, gen);
            child.Stage = LifeStage.Child;
            child.Life.Growth = lt.NewbornGrowth;
            child.Nutrition = lt.NewbornNutrition * 1000;
            child.Health = Math.Min(1000, lt.NewbornHealthBase + child.Life.Pheno.Health / 5) * 1000;
            child.Drives[Learning.Drive.Hunger] = 300 * 1000; child.Drives[Learning.Drive.Fatigue] = 200 * 1000;
            Array.Copy(child.Drives, child.RewardSnap, Learning.Drive.Count);
            for (int i = 0; i < muts.Count; i++)
                ev.Add(new GeneMutated { Child = child.Id, Locus = muts[i].Locus, Old = muts[i].Old, New = muts[i].New, Parent = (byte)(i < matMuts ? 0 : 1) });
            // the household and the believed father (the mother's partner now, who may not be the biological father)
            int partner = LifeCycleSystem.FirstPartner(s, mother.Id);
            var partnerNpc = partner != 0 ? p.Find(partner) : null;
            var h = mother.Household != 0 ? hs.Find(mother.Household) : null;
            if (h == null || !HouseholdModule.Join(s, ev, h, child, JoinReason.Born, 0)) HouseholdModule.Form(s, ev, child, JoinReason.Born);
            KinCues.Birth(s, ev, child, mother, partnerNpc);
            // D4: the father by a reproductive relationship outside the mother's partnership knows the child (he agreed to it), and it him
            var union = father != null ? ReproductiveUnions.Between(s, mother.Id, father.Id) : null;
            if (union != null) union.Children++;
            bool outside = father != null && father.Alive && father.Id != partner && union != null;
            if (outside)
            {
                KinCues.Believe(s, ev, father, child.Id, KinCues.ChildRole(sex), 800, KinSource.Born);
                KinCues.Believe(s, ev, child, father.Id, KinRole.Father, 700, KinSource.Told);
            }
            ml.PregFather = 0;
            ml.LastBirthTick = s.Tick;
            hs.Stats.Births++;
            ev.Add(NpcGenome.Of(child));
            ev.Add(new NpcBorn
            {
                Npc = child.Id, Mother = mother.Id, BelievedFather = partner, BioFather = father?.Id ?? 0, Household = child.Household, Faction = child.Faction,
                Node = child.Node, Sex = sex, GenomeHash = GenomeOps.Hash(genome), Generation = gen,
            });
            ev.Add(new PregnancyEnded { Mother = mother.Id, Child = child.Id, Outcome = PregnancyEnded.Birth });
            // the decisions to try for this child learn its arrival (an episode of the partnership, SHARED_CONTRACTS 6)
            // (key: kind 1 with the other partner's id: the episode hook sees the candidate, whose target is the partner)
            if (partnerNpc != null)
            {
                CloseEpisode(s, mother, EpisodeKey.Make(EpisodeKey.Partnership, partner), s.Tuning.Households.Care.ChildBornVp, day, ev);
                if (partnerNpc.Alive) CloseEpisode(s, partnerNpc, EpisodeKey.Make(EpisodeKey.Partnership, mother.Id), s.Tuning.Households.Care.ChildBornVp, day, ev);
            }
            if (outside)
            {
                CloseEpisode(s, mother, EpisodeKey.Make(EpisodeKey.Partnership, father.Id), s.Tuning.Households.Care.ChildBornVp, day, ev);
                CloseEpisode(s, father, EpisodeKey.Make(EpisodeKey.Partnership, mother.Id), s.Tuning.Households.Care.ChildBornVp, day, ev);
            }
            // childbirth hazard (POPULATION "Birth risk"): physical, from nutrition and health, never a quota
            var br = s.Rng.Fork("birth", mother.Id, s.Tick);
            int risk = lt.BirthRiskPer1000;
            if (mother.NutritionPermille < 300) risk += (lt.BirthRiskMaxPer1000 - lt.BirthRiskPer1000) * (300 - mother.NutritionPermille) / 300;
            int momRisk = risk * (1500 - ml.Pheno.Health) / 1000;
            int babyRisk = risk * (1500 - child.Life.Pheno.Health) / 1000;
            bool momDies = br.NextInt(1000) < momRisk, babyDies = br.NextInt(1000) < babyRisk;
            if (babyDies) PopulationModule.Kill(s, ev, child, DeathCause.Childbirth);
            if (momDies) PopulationModule.Kill(s, ev, mother, DeathCause.Childbirth);
            return child;
        }

        private static void CloseEpisode(GodState s, Npc npc, int key, int reward, int day, EventBuffer ev)
        {
            if (npc.Mind == null) return;
            var sink = s.Population.GoodsSink;
            sink.Bind(ev, npc.Id, EventLearningSink.IsSampledDay(npc.Id, day, s.Tuning.Learning.SampleEvery));
            Learner.Outcome(npc.Mind.Store, s.Tuning.Learning, PopulationSystem.TraitsOf(s, npc), key, reward, day, sink);
        }
    }
}
