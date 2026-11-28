using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// M4's sim-side hooks of the extension dispatch (SHARED_CONTRACTS 0.2). OWNER: M4. The call sites and their order are
    /// frozen (M3, then M4, then M5); only the bodies change. These are sim code and may read and write state; the scratch-only
    /// hooks Cognition and Policy call are on <see cref="HouseholdMindHooks"/> (gate G4.1). With <c>Households.Enabled = 0</c>
    /// M4 never starts (<c>GodState.Households</c> stays null) and every hook returns its default.
    /// </summary>
    public static class HouseholdHooks
    {
        // ---------------- module entry points ----------------

        /// <summary>From <c>GodSim.Create</c> after the population started: the household state, founder genomes, ages and households.</summary>
        public static void StartGame(GodState s) => Founders.StartGame(s);

        /// <summary>From <c>GodSim</c>'s module installation (new game and loaded save): the life cycle (240) and estates (255).</summary>
        public static void Install(GodSim sim)
        {
            if (sim.State.Households == null) return;
            sim.AddSystem(240, new LifeCycleSystem());
            if (!Unified.Unified.On(sim.State)) sim.AddSystem(255, new Estate.EstateSystem()); // UN0 (contracts 23.3): no estates in a unified game
        }

        /// <summary>From <c>PopulationModule.AddNpc</c>: an NPC M4 itself makes (a birth, an immigrant) gets a believed-kin mind.</summary>
        public static void NewMind(GodState s, Npc npc)
        {
            if (s.Households == null || !s.Households.Creating || npc.Mind == null) return;
            npc.Mind.Kin = new KinMind();
        }

        /// <summary>
        /// From <c>PopulationModule.Kill</c> after <c>NpcDied</c> (SHARED_CONTRACTS 9.2): the carry goes into an estate, the
        /// partnerships end, the household loses the member, the arrangements end. M3's cleanup follows (rights, mind).
        /// </summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var hs = s.Households;
            if (hs == null || npc.Life == null) return;
            if (npc.Life.PregFather != 0) { npc.Life.PregFather = 0; ev.Add(new PregnancyEnded { Mother = npc.Id, Outcome = PregnancyEnded.MotherDied }); }
            if (!Unified.Unified.On(s)) Estate.Estates.Open(s, ev, npc); // UN0 (contracts 23.3, D9.13): death leaves goods where they are
            foreach (var p in hs.Partnerships.ToArray())
            {
                if (p.A != npc.Id && p.B != npc.Id) continue;
                Partnerships.End(s, ev, p, PartnershipEnded.Death, npc.Id);
                var other = s.Population.Find(p.A == npc.Id ? p.B : p.A);
                if (other != null) KinCues.Forget(other, npc.Id);
            }
            for (int i = hs.Care.Count - 1; i >= 0; i--)
            {
                var c = hs.Care[i];
                if (c.Asker != npc.Id && c.Carer != npc.Id) continue;
                hs.Care.RemoveAt(i);
                ev.Add(new CareArrangementEnded { Arrangement = c.Id, EndedBy = npc.Id, Given = c.Given, Returned = c.Returned, Reason = 2 });
            }
            foreach (var hut in s.Population.Huts)
            {
                if (hut.Lodgers != null) hut.Lodgers.Remove(npc.Id);
                if (hut.RightSource != null) for (int i = 0; i < hut.Rights.Length; i++) if (hut.Rights[i] == npc.Id) hut.RightSource[i] = 0;
            }
            ReproductiveUnions.OnDeath(s, ev, npc);
            HouseholdModule.Leave(s, ev, npc, LeaveReason.Died, 0);
            npc.Life.Display = 0;
        }

        /// <summary>From <c>SimInvariants.Check</c>: M4's invariants (M4.14 membership; C4 and C5 estate clauses; M4.25 adults only).</summary>
        public static void Check(GodState s, List<string> bad) => HouseholdInvariants.Check(s, bad);

        /// <summary>
        /// M4's fields of a seen person: whether it shares the observer's household, and the one display it shows (begging,
        /// a proposal, a request for care, trying for a child; else being sick, an orphan needing a home, the care partner's
        /// owed band as the observer recorded it, or being partnered). Displays are visible state, never decoded.
        /// </summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            var hs = s.Households;
            if (hs == null) return;
            p.SameHousehold = observer.Household != 0 && seen.Household == observer.Household;
            p.PartnerMismatch = !Preferences.PartnerCompatible(observer, seen); // F2b: false unless partner-sex preferences are drawn
            var l = seen.Life;
            if (l == null || s.Tuning.Households.Care.Off != 0 && observer.Stage != LifeStage.Child && seen.Stage == LifeStage.Child) return;
            // (D4) "wants a child" is seen only by those it could have one with (a visible fact, asked of Reproduction)
            if (l.Display != 0 && l.DisplayUntil >= s.Tick && l.Display != DisplayKind.Trying
                && (l.Display != DisplayKind.SeekChild || observer.Stage != LifeStage.Child && Reproduction.MayConceiveWith(observer, seen))
                && (l.Display != DisplayKind.Beg || CareStructure.MayAnswer(s, observer, seen)))
            { p.Display = l.Display; p.DisplayLevel = l.DisplayLevel; p.DisplayTarget = l.DisplayTarget; return; }
            if (s.Tuning.Households.Care.Off == 0 && (l.Illness == 1 || seen.Injury > 300000))
            {
                // the level shows how ill (1-3) and, as bit 4, that it was tended today (a visible fact)
                int day = (int)SimCalendar.Of(s).Day(s.Tick);
                p.Display = DisplayKind.Sick;
                p.DisplayLevel = (byte)((l.Illness == 1 ? Math.Min(3, 1 + l.Severity / 100) : 1) | (l.TendedDay == day ? 4 : 0));
                return;
            }
            if (seen.Stage == LifeStage.Child && l.OrphanSince >= 0) { p.Display = DisplayKind.NeedsHome; p.DisplayLevel = 1; return; }
            var ct = s.Tuning.Households.Care;
            foreach (var c in hs.Care)
            {
                int owed;
                if (c.Carer == observer.Id && c.Asker == seen.Id) owed = c.Given - c.Returned;
                else if (c.Asker == observer.Id && c.Carer == seen.Id) owed = c.Returned - c.Given;
                else continue;
                p.Display = DisplayKind.CarePartner;
                p.DisplayLevel = (byte)(owed >= ct.OwedBand3 ? 3 : owed >= ct.OwedBand2 ? 2 : owed >= ct.OwedBand1 ? 1 : 0);
                p.DisplayTarget = observer.Id;
                return;
            }
            // (D4) the person it has a reproductive relationship with: trying with it shows as trying; otherwise the mark of the relationship
            bool union = hs.Unions.Count > 0 && ReproductiveUnions.Between(s, observer.Id, seen.Id) != null;
            if (l.Display == DisplayKind.Trying && l.DisplayUntil >= s.Tick && (!union || l.DisplayTarget == observer.Id)) { p.Display = DisplayKind.Trying; p.DisplayLevel = 1; p.DisplayTarget = l.DisplayTarget; return; }
            if (union) { p.Display = DisplayKind.UnionPartner; p.DisplayLevel = 1; p.DisplayTarget = observer.Id; return; }
            if (Partnerships.Count(s, seen.Id) >= s.Tuning.Households.Partner.MaxLivePartnerships) { p.Display = DisplayKind.Partnered; p.DisplayLevel = 1; }
        }

        /// <summary>
        /// The Sense seam for bins (I-BS amendment, SHARED_CONTRACTS 12.32): within sight, a person perceives its own household's
        /// store (class <see cref="HClass.Store"/>) and any estate goods lying at a death place (<see cref="HClass.Estate"/>), with
        /// the band of what it sees there that is its own or its household's to take (A8: Own, Household): food for a store,
        /// any goods for an estate (its inheritance once the executor has split it). Other households' stores are not offered
        /// to the mind (no permission; theft is M8). The bin id is a reference it may name (a thing it knows, like a lot).
        /// </summary>
        public static int SenseBins(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n)
        {
            if (s.Households == null || npc.Life == null) return n;
            foreach (var b in s.Population.Bins)
            {
                if (n >= near.Length) break;
                bool store = b.Kind == BinKind.HouseholdStore;
                if (store && (npc.Household == 0 || b.Owner.Kind != OwnerKind.Household || b.Owner.Id != npc.Household)) continue;
                if (!store && b.Kind != BinKind.Estate) continue;
                int d = g.Distance(npc.Node, b.Node);
                if (d > sight) continue;
                int units = 0;
                foreach (var l in b.Lots)
                {
                    if (store && (!Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade)) continue;
                    bool mine = l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == npc.Id
                        || l.Owner.Kind == OwnerKind.Household && l.Owner.Id == npc.Household && npc.Household != 0 && (store || npc.Stage != LifeStage.Child);
                    if (mine) units += l.Count;
                }
                if (!store && units == 0) continue; // another's estate: nothing of its own lies there
                near[n++] = new Sensed
                {
                    Class = (byte)(store ? HClass.Store : HClass.Estate), Category = ObjCategory.Store,
                    State = (byte)(units == 0 ? ObjState.StockEmpty : units < 4 ? ObjState.StockLow : ObjState.StockHigh),
                    Kind = IntentTarget.Bin, Node = b.Node, Dist = d, Ref = b.Id,
                };
            }
            return n;
        }

        // ---------------- sim side ----------------

        /// <summary>The episode of trying for a child: kind 1 (partnership) keyed by the partner (the hook sees the candidate only), closed at a birth.</summary>
        public static int EpisodeKey(Npc npc, in Candidate c) =>
            c.Verb == HVerb.TryForChild && c.TargetId > 0 && c.TargetId <= Learning.EpisodeKey.MaxId ? Learning.EpisodeKey.Make(Learning.EpisodeKey.Partnership, c.TargetId) : 0;

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day) => HouseholdActs.Start(s, ev, npc, in c, day);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day) => HouseholdActs.Act(s, ev, npc, day);

        /// <summary>Time use (Q12): feeding, tending, taking in a child and keeping the household store are care work; courtship and displays are other.</summary>
        public static int ActivityOf(byte verb)
        {
            switch (verb)
            {
                case HVerb.Feed: case HVerb.Tend: case HVerb.Adopt: case HVerb.Store: case HVerb.TakeFromStore: case HVerb.Provision: return Activity.Care;
                case HVerb.Beg: case HVerb.Propose: case HVerb.Accept: case HVerb.TryForChild: case HVerb.Leave:
                case HVerb.AskCare: case HVerb.AcceptCare: case HVerb.EndCare: case HVerb.SeekChild: case HVerb.AcceptChild: return Activity.Other;
                case HVerb.RangeFar: return Activity.Explore;
                default: return Activity.Other;
            }
        }

        // ---------------- the body and the learner (PopulationSystem's M4 branches, SHARED_CONTRACTS 10.2) ----------------

        /// <summary>The learning-rate shift by life stage (SHARED_CONTRACTS 6): children learn fastest, elders slowest.</summary>
        public static int AlphaShift(Npc npc, int adult)
        {
            if (npc.Life == null) return adult;
            return npc.Stage == LifeStage.Child ? Math.Max(0, adult - 1) : npc.Stage == LifeStage.Elder ? adult + 1 : adult;
        }

        /// <summary>The hunger rate (permille of the adult's): a child at 60% rising to 100% at adulthood; pregnancy +30%.</summary>
        public static int HungerPermille(GodState s, Npc npc)
        {
            var lt = s.Tuning.Households.Life;
            int f = 1000;
            if (npc.Stage == LifeStage.Child)
            {
                long age = Bodies.AgeMilli(SimCalendar.Of(s), npc, s.Tick);
                f = lt.ChildHungerPermille + (int)((1000 - lt.ChildHungerPermille) * Math.Min(1000, age) / 1000);
            }
            if (npc.Life.PregFather != 0) f = f * (1000 + lt.PregnantHungerPermille) / 1000;
            return f;
        }

        /// <summary>Starvation damage and health recovery of the frail (POPULATION "Mortality", "Ageing").</summary>
        public static int StarvePermille(GodState s, Npc npc) => npc.Stage == LifeStage.Adult ? 1000 : s.Tuning.Households.Life.FrailStarvePermille;
        public static int RecoverPermille(GodState s, Npc npc) => npc.Stage == LifeStage.Elder ? s.Tuning.Households.Life.ElderRecoverPermille : 1000;

        /// <summary>Work competence (D3 §10): 0 below the age a child can pick or forage, then rising to 1000 at adulthood.</summary>
        public static int Competence(GodState s, Npc npc)
        {
            if (npc.Life == null || npc.Stage != LifeStage.Child) return 1000;
            var lt = s.Tuning.Households.Life;
            long age = Bodies.AgeMilli(SimCalendar.Of(s), npc, s.Tick);
            if (age < lt.ForageFromMilli) return 0;
            int span = Math.Max(1, 1000 - lt.ForageFromMilli);
            return lt.CompetenceStartPermille + (int)((1000 - lt.CompetenceStartPermille) * Math.Min(span, age - lt.ForageFromMilli) / span);
        }

        public static bool CanWork(GodState s, Npc npc) => Competence(s, npc) > 0;

        /// <summary>
        /// F2c (R67): the children living in an adult's own household (it lives with them, so it knows them), for its
        /// <c>SelfView.Dependants</c>; 0 while <c>Care.Provision</c> is off, care is off, or it is a child or has no household.
        /// </summary>
        public static int ProvisionDependants(GodState s, Npc npc)
        {
            if (s.Households == null || npc.Life == null || npc.Household == 0 || npc.Stage == LifeStage.Child) return 0;
            var ct = s.Tuning.Households.Care;
            if (ct.Provision == 0 || ct.Off != 0) return 0;
            var h = s.Households.Find(npc.Household);
            if (h == null) return 0;
            int k = 0;
            foreach (int m in h.Members) { var n = s.Population.Find(m); if (n != null && n.Alive && n.Stage == LifeStage.Child) k++; }
            return k;
        }

        /// <summary>Whether this work tick counts: a child's competence share of ticks do (deterministic accumulation).</summary>
        public static bool WorkTick(GodState s, Npc npc)
        {
            int c = Competence(s, npc);
            if (c >= 1000) return true;
            var l = npc.Life;
            l.WorkAcc += c;
            if (l.WorkAcc < 1000) return false;
            l.WorkAcc -= 1000;
            return true;
        }
    }

    /// <summary>
    /// M4's mind-side hooks (OWNER: M4): the verbs and instincts the learning tables concatenate, and the hooks Cognition and
    /// Policy call. They read the think scratch only, never <see cref="GodState"/> (gate G4.1): the person in sight, its stage
    /// and displays, the mind's own believed kin (r̂, never the pedigree), its carry and its home. A mind without a believed-kin
    /// part (M3 fixtures, <c>Households.Enabled = 0</c>) gets nothing from here.
    /// <para>Person-directed candidates are <see cref="IntentTarget.Npc"/> targets: the seen person's handle (13.12) and its node.
    /// Since the I-BS amendment (SHARED_CONTRACTS 12.31) <c>Thinker.Known</c> accepts them (in sight, or remembered).</para>
    /// </summary>
    public static class HouseholdMindHooks
    {
        // ---------------- data rows (concatenated after M3's in F0.11) ----------------

        public static readonly ReadOnlyArray<VerbInfo> Verbs = new ReadOnlyArray<VerbInfo>(
            new VerbInfo(HVerb.Store, "store", VerbGroup.CoopGive, NpcVerb.UseStore),
            new VerbInfo(HVerb.TakeFromStore, "take from store", VerbGroup.CoopTake, NpcVerb.UseStore),
            new VerbInfo(HVerb.Feed, "feed", VerbGroup.CoopGive, NpcVerb.HandOver),
            new VerbInfo(HVerb.Beg, "beg", VerbGroup.CoopTake, NpcVerb.Interact),
            new VerbInfo(HVerb.Propose, "propose", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(HVerb.Accept, "accept partner", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(HVerb.TryForChild, "try for a child", VerbGroup.Relationships, NpcVerb.Reproduce, deferred: true),
            new VerbInfo(HVerb.Leave, "leave partner", VerbGroup.Relationships, NpcVerb.Leave),
            new VerbInfo(HVerb.Tend, "tend", VerbGroup.CoopGive, NpcVerb.Interact),
            new VerbInfo(HVerb.Adopt, "take in a child", VerbGroup.CoopGive, NpcVerb.Interact),
            new VerbInfo(HVerb.AskCare, "ask for shared care", VerbGroup.CoopTake, NpcVerb.Interact),
            new VerbInfo(HVerb.AcceptCare, "agree shared care", VerbGroup.CoopGive, NpcVerb.Interact),
            new VerbInfo(HVerb.EndCare, "end shared care", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(HVerb.SeekChild, "show it wants a child", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(HVerb.AcceptChild, "agree to a child", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(HVerb.RangeFar, "range far for food", VerbGroup.Search, NpcVerb.Explore),
            new VerbInfo(HVerb.Provision, "provision the household", VerbGroup.Gathering, NpcVerb.ForageAt));

        private static uint RoleF(int v) => Feature.Make(FeatureKind.Role, v);

        /// <summary>
        /// The innate priors of M4 (NPC_LEARNING 3.4: the strong defaults come from instincts and the genome's gains, and can
        /// fail): a hungry child begs; a begging child draws feeding, harder the louder it begs; the sick draw tending, an
        /// orphan a home; the hungry go to the household store; a sated carrier puts food in it; a perceived proposal or care
        /// request is answered; a partner draws "try for a child"; an unpartnered adult proposes now and then; a care partner
        /// who owes a lot draws an end. Leaving a partner has no prior.
        /// </summary>
        public static readonly ReadOnlyArray<Instinct> Instincts = new ReadOnlyArray<Instinct>(
            new Instinct(RoleF(HRole.Child), Feature.DriveBand(Drive.Hunger, 1), HVerb.Beg, 400),
            new Instinct(RoleF(HRole.Child), Feature.DriveActive(Drive.Hunger), HVerb.Beg, 550),
            new Instinct(Feature.State(HState.Begging1), 0, HVerb.Feed, 300),
            new Instinct(Feature.State(HState.Begging2), 0, HVerb.Feed, 450),
            new Instinct(Feature.State(HState.Begging3), 0, HVerb.Feed, 600),
            new Instinct(Feature.State(HState.Sick), 0, HVerb.Feed, 200),
            new Instinct(Feature.State(HState.Sick), 0, HVerb.Tend, 250),
            new Instinct(Feature.State(HState.NeedsHome), 0, HVerb.Adopt, 350),
            new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Class(HClass.Store), HVerb.TakeFromStore, 450),
            new Instinct(Feature.DriveBand(Drive.Hunger, 1), Feature.Class(HClass.Store), HVerb.TakeFromStore, 200),
            new Instinct(Feature.Situation(HSituation.DependantInNeed), Feature.Class(HClass.Store), HVerb.TakeFromStore, 350),
            new Instinct(Feature.Self(SelfFeature.CarryingFood), Feature.Class(HClass.Store), HVerb.Store, 200),
            // I-BS (12.32): goods of its own lying in an estate (its inheritance, seen) draw it to take them up
            new Instinct(Feature.Class(HClass.Estate), 0, HVerb.TakeFromStore, 450),
            new Instinct(Feature.State(HState.Proposing), 0, HVerb.Accept, 450),
            new Instinct(Feature.State(HState.AskingCare), 0, HVerb.AcceptCare, 250),
            new Instinct(Feature.NpcRel((int)NpcRelClass.Partner), 0, HVerb.TryForChild, 250),
            new Instinct(Feature.State(HState.Trying), 0, HVerb.TryForChild, 200),
            new Instinct(Feature.Self(HSelf.Unpartnered), 0, HVerb.Propose, 150),
            new Instinct(Feature.Self(HSelf.HasDependants), 0, HVerb.AskCare, 60),
            new Instinct(Feature.Situation(HSituation.CareOwed2), 0, HVerb.EndCare, 300),
            new Instinct(Feature.Situation(HSituation.CareOwed3), 0, HVerb.EndCare, 600),
            // D4: the person it agreed a child with draws trying; wanting a child draws showing it; seeing that draws agreeing
            new Instinct(Feature.Situation(HSituation.UnionPartner), 0, HVerb.TryForChild, 250),
            new Instinct(Feature.Self(HSelf.WantsChild), 0, HVerb.SeekChild, 120),
            new Instinct(Feature.Situation(HSituation.SeekingChild), 0, HVerb.AcceptChild, 300),
            // D4 (M4.43): hungry with no plentiful food known nearby draws a long walk out
            new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Situation(HSituation.NoPlentyKnown), HVerb.RangeFar, RangeFarPrior),
            new Instinct(Feature.DriveBand(Drive.Hunger, 1), Feature.Situation(HSituation.NoPlentyKnown), HVerb.RangeFar, RangeFarPrior / 2),
            // F2c (R67): its household short of food draws provisioning, more so with a dependant in need in sight
            new Instinct(Feature.Situation(HSituation.HouseholdShort), 0, HVerb.Provision, ProvisionPrior),
            new Instinct(Feature.Situation(HSituation.HouseholdShort), Feature.Situation(HSituation.DependantInNeed), HVerb.Provision, ProvisionPrior / 2));

        /// <summary>F2c: the innate prior of provisioning (scaled by the nurture gain).</summary>
        public const int ProvisionPrior = 350;

        /// <summary>The innate prior of ranging far (D4): enough to outweigh the walk's perceived cost at <see cref="RangeFarNodes"/>.</summary>
        public const int RangeFarPrior = 600;
        /// <summary>How far one ranging walk goes (nodes): beyond sight (10) and the explore step (9), within recall (40).</summary>
        public const int RangeFarNodes = 24;

        /// <summary>The kin term's scale (permille of r̂ x (100 + KinWeight) / 100): the Hamilton weighting (HOUSEHOLDS "Feeding").</summary>
        public const int KinTermPermille = 600;

        // ---------------- perception and cognition (scratch only) ----------------

        private static bool M4(ThinkScratch sc) => sc.Mind?.Kin != null;

        /// <summary>One base key: a child knows it is a child, an elder that it is old; an adult sees a dependant in need (a begging child of its household, a sick kin).</summary>
        public static void AddBase(ThinkScratch sc)
        {
            if (!M4(sc)) return;
            var self = sc.Self;
            if (self.Stage == LifeStage.Child) { Add(sc, RoleF(HRole.Child)); return; }
            if (self.Stage == LifeStage.Elder) { Add(sc, RoleF(HRole.Elder)); return; }
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                bool mine = p.SameHousehold || KinData.RHat(sc.Mind, p.As) >= 250;
                if (mine && (p.Stage == LifeStage.Child && p.Display == DisplayKind.Beg || p.Display == DisplayKind.Sick)) { Add(sc, Feature.Situation(HSituation.DependantInNeed)); return; }
            }
        }

        private static void Add(ThinkScratch sc, uint f) { if (sc.BaseCount < ThinkScratch.MaxBase) sc.Base[sc.BaseCount++] = f; }

        private static bool BaseHas(ThinkScratch sc, uint f) { for (int i = 0; i < sc.BaseCount; i++) if (sc.Base[i] == f) return true; return false; }

        /// <summary>Food in its own carry that it believes it may give (its own or its household's, A8).</summary>
        private static bool UsableFood(ThinkScratch sc)
        {
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (l.Empty || !Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade) continue;
                if (Permits.Believed(sc, l.Owner).Allowed) return true;
            }
            return false;
        }

        private static bool HasPartnerBelief(ThinkScratch sc)
        {
            foreach (var b in sc.Mind.Kin.Beliefs) if (b.Role == KinRole.Partner) return true;
            return false;
        }

        private static bool CloseKin(ThinkScratch sc, int other)
        {
            var k = sc.Mind.Kin;
            int i = k.IndexOf(other);
            if (i < 0) return false;
            var b = k.Beliefs[i];
            if (b.Confidence <= 400) return false;
            switch (b.Role)
            {
                case KinRole.Mother: case KinRole.Father: case KinRole.Daughter: case KinRole.Son: case KinRole.FullSibling: case KinRole.HalfSibling:
                case KinRole.FosterChild: case KinRole.FosterParent: case KinRole.StepChild: case KinRole.StepParent: return true;
                default: return false;
            }
        }

        /// <summary>
        /// M4's candidates (quota 4, after M3's 16), in a fixed priority order: answers to what it perceives first (a proposal,
        /// a care request), then care (feed, tend, take in), the partnership (try for a child, propose), the store (take,
        /// put), shared care (end, ask) and leaving a partner when hard-pressed.
        /// </summary>
        public static void AddCandidates(ThinkScratch sc)
        {
            if (!M4(sc)) return;
            var self = sc.Self;
            int hungerBand = Thinker.Band(sc.Tuning, Drive.Hunger, self.Hunger);
            bool food = UsableFood(sc);
            bool home = self.Household != 0;
            if (self.Stage == LifeStage.Child)
            {
                if (hungerBand >= 1 && !food)
                {
                    int best = -1, bestScore = int.MinValue;
                    for (int i = 0; i < sc.PeopleCount; i++)
                    {
                        var p = sc.People[i];
                        if (p.Stage == LifeStage.Child || p.Asleep) continue;
                        int score = (p.SameHousehold ? 1000 : 0) + KinData.RHat(sc.Mind, p.As) - p.Dist * 20;
                        if (score > bestScore) { bestScore = score; best = i; }
                    }
                    if (best >= 0) Person(sc, HVerb.Beg, best, 0);
                    if (home) Bins(sc, true, false, false);
                }
                return;
            }
            int proposer = -1, asker = -1, beggar = -1, sick = -1, orphan = -1, partner = -1, mate = -1, other = -1, owed = -1, unionMate = -1, seeker = -1;
            int dSeeker = int.MaxValue;
            bool audience = false;
            int begScore = int.MinValue, dMate = int.MaxValue, dOther = int.MaxValue, sickScore = int.MinValue, dOrphan = int.MaxValue;
            bool dependants = false, needing = false;
            bool partnered = HasPartnerBelief(sc);
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                int rhat = KinData.RHat(sc.Mind, p.As);
                var role = KinData.RoleOf(sc.Mind, p.As);
                if (p.Display == DisplayKind.Propose && p.DisplayTarget == self.Id && proposer < 0 && !p.PartnerMismatch) proposer = i;
                if (p.Display == DisplayKind.AskCare && p.DisplayTarget == self.Id && asker < 0) asker = i;
                if (p.Stage == LifeStage.Child && p.SameHousehold) dependants = true;
                if (p.Stage == LifeStage.Child && p.Display == DisplayKind.Beg)
                {
                    int score = p.DisplayLevel * 1000 + rhat - p.Dist * 20;
                    if (score > begScore) { begScore = score; beggar = i; }
                    if (p.SameHousehold) needing = true;
                }
                if (p.Display == DisplayKind.Sick && (p.SameHousehold || rhat > 0 || role == KinRole.Partner))
                {
                    // the sick one that matters most to it, as it believes (kin, partner, household), then the nearest
                    // (one already tended today, as anyone can see, draws less: care passes to the next)
                    int score = rhat + (role == KinRole.Partner ? 500 : 0) + (p.SameHousehold ? 100 : 0) - p.Dist * 20 - ((p.DisplayLevel & 4) != 0 ? 300 : 0);
                    if (score > sickScore) { sickScore = score; sick = i; }
                }
                if (p.Display == DisplayKind.NeedsHome && p.Stage == LifeStage.Child && p.Dist < dOrphan) { dOrphan = p.Dist; orphan = i; }
                if (role == KinRole.Partner && !p.Asleep && partner < 0) partner = i;
                if (!partnered && p.Stage != LifeStage.Child && p.SameFaction && !p.Asleep && p.Display != DisplayKind.Partnered && p.Display != DisplayKind.Trying
                    && role != KinRole.Partner && !CloseKin(sc, p.As) && !p.PartnerMismatch && p.Dist < dMate) { dMate = p.Dist; mate = i; }
                if (self.Household != 0 && p.Stage != LifeStage.Child && p.SameFaction && !p.SameHousehold && !p.Asleep && p.Display != DisplayKind.CarePartner && p.Dist < dOther)
                { dOther = p.Dist; other = i; }
                if (p.Display == DisplayKind.CarePartner && p.DisplayLevel >= 2 && owed < 0) owed = i;
                // D4: the one it agreed a child with (marked to it, or trying with it), and someone showing that it wants a child
                if (role != KinRole.Partner && !p.Asleep && unionMate < 0
                    && (p.Display == DisplayKind.UnionPartner || p.Display == DisplayKind.Trying && p.DisplayTarget == self.Id)) unionMate = i;
                if (p.Stage == LifeStage.Adult && p.SameFaction && !p.Asleep && role != KinRole.Partner && !CloseKin(sc, p.As)) audience = true;
                if (p.Display == DisplayKind.SeekChild && p.SameFaction && role != KinRole.Partner && !CloseKin(sc, p.As) && p.Dist < dSeeker) { dSeeker = p.Dist; seeker = i; }
            }
            // D4: may it look for a child outside a partnership? an adult (not old) with no reproductive relationship, whose
            // own commitment allows it (its partners' commitments are checked by the act); the wish itself is the instinct's gain
            bool mayUnion = self.Stage == LifeStage.Adult && sc.Mind.Kin.UnionWith == 0
                && (!partnered || sc.Traits == null || sc.Traits.Length < Preferences.TraitLength || sc.Traits[Preferences.TraitCommitment] < Preferences.ExclusiveFrom);
            if (proposer >= 0) Person(sc, HVerb.Accept, proposer, Feature.State(HState.Proposing));
            if (seeker >= 0 && mayUnion) Person(sc, HVerb.AcceptChild, seeker, Feature.Situation(HSituation.SeekingChild));
            if (asker >= 0) Person(sc, HVerb.AcceptCare, asker, Feature.State(HState.AskingCare));
            // D4 (M4.43): hungry, carrying nothing, and no plentiful food seen or remembered: range far
            if (hungerBand >= 1 && !food && self.Stage != LifeStage.Child && !PlentyKnown(sc)) RangeFar(sc);
            if (food && beggar >= 0) Person(sc, HVerb.Feed, beggar, Feature.State(HState.Begging1 + Math.Min(2, Math.Max(0, sc.People[beggar].DisplayLevel - 1))));
            else if (food && sick >= 0) Person(sc, HVerb.Feed, sick, Feature.State(HState.Sick));
            if (sick >= 0) Person(sc, HVerb.Tend, sick, Feature.State(HState.Sick));
            if (self.Dependants > 0 && home && HouseholdShort(sc)) Provision(sc);
            if (orphan >= 0) Person(sc, HVerb.Adopt, orphan, Feature.State(HState.NeedsHome));
            if (partner >= 0)
            {
                var pp = sc.People[partner];
                bool trying = pp.Display == DisplayKind.Trying && pp.DisplayTarget == self.Id;
                Person(sc, HVerb.TryForChild, partner, trying ? Feature.State(HState.Trying) : 0);
            }
            if (unionMate >= 0)
            {
                var up = sc.People[unionMate];
                Person(sc, HVerb.TryForChild, unionMate, up.Display == DisplayKind.Trying ? Feature.State(HState.Trying) : Feature.Situation(HSituation.UnionPartner));
            }
            if (mate >= 0) Person(sc, HVerb.Propose, mate, Feature.Self(HSelf.Unpartnered));
            if (mayUnion && audience && seeker < 0 && unionMate < 0) SelfAct(sc, HVerb.SeekChild, Feature.Self(HSelf.WantsChild));
            Bins(sc, home && !food && (hungerBand >= 1 || needing), home && food && hungerBand == 0, true);
            if (owed >= 0) Person(sc, HVerb.EndCare, owed, Feature.Situation(sc.People[owed].DisplayLevel >= 3 ? HSituation.CareOwed3 : HSituation.CareOwed2));
            if (dependants && other >= 0) Person(sc, HVerb.AskCare, other, Feature.Self(HSelf.HasDependants));
            if (partner >= 0 && hungerBand >= 2) Person(sc, HVerb.Leave, partner, 0);
        }

        /// <summary>
        /// F2c (R67): whether it sees or remembers its own household's store empty or low (it has children at home: the
        /// caller checks <c>SelfView.Dependants</c>). A store it knows nothing of is not judged short.
        /// </summary>
        private static bool HouseholdShort(ThinkScratch sc)
        {
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (t.Kind == IntentTarget.Bin && t.Class == HClass.Store) return t.State == ObjState.StockEmpty || t.State == ObjState.StockLow;
            }
            var b = sc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++)
            {
                var bel = b.Slots[i];
                if (bel.Kind == BeliefKind.Store && bel.Ref != 0 && bel.Class == HClass.Store) return bel.Stock == ObjState.StockEmpty || bel.Stock == ObjState.StockLow;
            }
            return false;
        }

        /// <summary>
        /// F2c (R67): "provision the household": a food place to forage at for the household (the one it is already provisioning
        /// from, else the nearest it sees with food, else the best it remembers within recall); the act forages there, then
        /// carries the food home to the store. A full carry with no running provisioning offers nothing.
        /// </summary>
        private static void Provision(ThinkScratch sc)
        {
            var s = sc.Self;
            int node = -1, cls = 0, stock = 0, dist = 0;
            bool fromBelief = false;
            if (s.RunVerb == HVerb.Provision && s.RunKind == IntentTarget.Node && s.RunTarget >= 0)
            {
                node = s.RunTarget; dist = sc.Grid.Distance(s.Node, node);
                for (int i = 0; i < sc.SeenCount; i++) if (sc.Seen[i].Node == node && sc.Seen[i].Category == ObjCategory.Food) { cls = sc.Seen[i].Class; stock = sc.Seen[i].State; }
                if (cls == 0)
                {
                    var bb = sc.Mind.Beliefs;
                    for (int i = 0; i < bb.Count; i++) if (bb.Slots[i].Kind == BeliefKind.Food && bb.Slots[i].Subject == node) { cls = bb.Slots[i].Class; stock = bb.Slots[i].Stock; fromBelief = true; break; }
                }
                if (cls == 0) node = -1;
            }
            if (node < 0 && s.CarryFull) return;
            if (node < 0)
            {
                int best = int.MaxValue;
                for (int i = 0; i < sc.SeenCount; i++)
                {
                    var t = sc.Seen[i];
                    if (t.Category != ObjCategory.Food || t.State == ObjState.StockEmpty) continue;
                    if (t.Class != ObjClass.BerryBush && t.Class != ObjClass.RootPatch && t.Class != ObjClass.FishSpot) continue;
                    if (t.Dist < best) { best = t.Dist; node = t.Node; cls = t.Class; stock = t.State; dist = t.Dist; }
                }
            }
            if (node < 0)
            {
                var b = sc.Mind.Beliefs;
                int recall = sc.Tuning.RecallNodes, bestScore = int.MinValue;
                for (int i = 0; i < b.Count; i++)
                {
                    var bel = b.Slots[i];
                    if (bel.Kind != BeliefKind.Food || bel.Stock == ObjState.StockEmpty) continue;
                    if (bel.Class != ObjClass.BerryBush && bel.Class != ObjClass.RootPatch && bel.Class != ObjClass.FishSpot) continue;
                    int d = sc.Grid.Distance(s.Node, bel.Subject);
                    if (d > recall) continue;
                    int score = bel.Confidence - d * 20;
                    if (score > bestScore) { bestScore = score; node = bel.Subject; cls = bel.Class; stock = bel.Stock; dist = d; fromBelief = true; }
                }
            }
            if (node < 0) return;
            int at = Thinker.AddCandidate(sc, HVerb.Provision, IntentTarget.Node, 0, node, (byte)cls, dist, fromBelief);
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, Feature.Situation(HSituation.HouseholdShort), Feature.Class(cls), Feature.Category(ObjCategory.Food),
                stock != 0 ? Feature.State(stock) : 0, Feature.PlaceRef(node));
        }

        /// <summary>A candidate aimed at a seen person (a person target: its handle and the node it stands on), with its person features.</summary>
        private static void Person(ThinkScratch sc, byte verb, int i, uint extra)
        {
            var p = sc.People[i];
            int at = Thinker.AddCandidate(sc, verb, IntentTarget.Npc, p.Id, p.Node, 0, p.Dist);
            if (at < 0) return;
            var rel = Relations.ClassOf(sc, in p);
            uint group = p.Stage == LifeStage.Child ? Feature.NpcGroup(NpcGroupValue.Child) : p.Stage == LifeStage.Elder ? Feature.NpcGroup(NpcGroupValue.Elder) : 0;
            Thinker.TargetFeatures(sc, at, Feature.NpcRel((int)rel), group, extra);
        }

        /// <summary>D4: whether it sees or remembers (within recall) a food source with plenty, or food lying.</summary>
        private static bool PlentyKnown(ThinkScratch sc)
        {
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (t.Category != ObjCategory.Food) continue;
                if (t.State == ObjState.StockHigh || t.Kind == IntentTarget.Lot) return true;
            }
            var b = sc.Mind.Beliefs;
            int recall = sc.Tuning.RecallNodes;
            for (int i = 0; i < b.Count; i++)
            {
                var bel = b.Slots[i];
                if (bel.Kind == BeliefKind.Food && bel.Stock == ObjState.StockHigh && sc.Grid.Distance(sc.Self.Node, bel.Subject) <= recall) return true;
            }
            return false;
        }

        /// <summary>D4: a ranging walk of up to <see cref="RangeFarNodes"/> in one of six directions (it turns with the day and its own id), over land.</summary>
        private static void RangeFar(ThinkScratch sc)
        {
            var g = sc.Grid;
            int cur = RangeFarGoal(sc);
            if (cur < 0) return;
            int at = Thinker.AddCandidate(sc, HVerb.RangeFar, IntentTarget.Node, 0, cur, 0, g.Distance(sc.Self.Node, cur));
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, Feature.Situation(HSituation.NoPlentyKnown), Feature.Class(ObjClass.Unknown));
        }

        /// <summary>
        /// D4: the end of today's ranging walk (one of six directions, turning with the day and its own id; up to
        /// <see cref="RangeFarNodes"/> over land), or -1 when the walk would be shorter than half of that.
        /// </summary>
        private static int RangeFarGoal(ThinkScratch sc)
        {
            var g = sc.Grid;
            int dir = (sc.Self.Day + sc.Self.Id) % 6;
            int cur = sc.Self.Node, steps = 0;
            for (int k = 0; k < RangeFarNodes; k++)
            {
                int v = g.Neighbour(cur, dir);
                if (v < 0 || !g.IsLand(v)) break;
                cur = v; steps++;
            }
            return steps < RangeFarNodes / 2 ? -1 : cur;
        }

        /// <summary>
        /// F0 (SHARED_CONTRACTS 12.37): a node target that is its own ranging heading (the end of today's ranging walk) is
        /// its own direction, known as M3's own explore directions are (<c>SelfView.ExploreA/B</c>). Only with M4.
        /// </summary>
        public static bool OwnHeading(ThinkScratch sc, int node) => M4(sc) && node >= 0 && RangeFarGoal(sc) == node;

        /// <summary>A candidate done where it stands (D4: showing that it wants a child), with one self feature.</summary>
        private static void SelfAct(ThinkScratch sc, byte verb, uint extra)
        {
            int at = Thinker.AddCandidate(sc, verb, IntentTarget.Node, 0, sc.Self.Node, 0, 0);
            if (at < 0) return;
            Thinker.TargetFeatures(sc, at, extra);
        }

        /// <summary>
        /// Store and estate candidates from perception (I-BS amendment 12.32): a bin it sees now (the Sense seam), else one it
        /// remembers seeing (a store belief: walk there, then act within <c>Reach.Store</c>). Taking needs something of its own
        /// or its household's seen there (the band is not empty); depositing needs its household's store. Never blind.
        /// </summary>
        private static void Bins(ThinkScratch sc, bool take, bool deposit, bool estates)
        {
            if (!take && !deposit && !estates) return;
            bool storeSeen = false;
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (t.Kind != IntentTarget.Bin) continue;
                if (t.Class == HClass.Store)
                {
                    storeSeen = true;
                    if (take && t.State != ObjState.StockEmpty) BinCand(sc, HVerb.TakeFromStore, t.Ref, t.Node, t.Class, t.State, t.Dist, false);
                    if (deposit) BinCand(sc, HVerb.Store, t.Ref, t.Node, t.Class, t.State, t.Dist, false);
                }
                else if (t.Class == HClass.Estate && estates && t.State != ObjState.StockEmpty) BinCand(sc, HVerb.TakeFromStore, t.Ref, t.Node, t.Class, t.State, t.Dist, false);
            }
            var b = sc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++)
            {
                var bel = b.Slots[i];
                if (bel.Kind != BeliefKind.Store || bel.Ref == 0) continue;
                bool seenNow = false;
                for (int k = 0; k < sc.SeenCount; k++) if (sc.Seen[k].Kind == IntentTarget.Bin && sc.Seen[k].Ref == bel.Ref) { seenNow = true; break; }
                if (seenNow) continue;
                int d = sc.Grid.Distance(sc.Self.Node, bel.Subject);
                if (d > sc.Tuning.RecallNodes) continue;
                if (bel.Class == HClass.Store && !storeSeen)
                {
                    if (take && bel.Stock != ObjState.StockEmpty) BinCand(sc, HVerb.TakeFromStore, bel.Ref, bel.Subject, bel.Class, bel.Stock, d, true);
                    if (deposit) BinCand(sc, HVerb.Store, bel.Ref, bel.Subject, bel.Class, bel.Stock, d, true);
                }
                else if (bel.Class == HClass.Estate && estates && bel.Stock != ObjState.StockEmpty) BinCand(sc, HVerb.TakeFromStore, bel.Ref, bel.Subject, bel.Class, bel.Stock, d, true);
            }
        }

        /// <summary>A candidate at a bin it perceives or remembers (a bin target: its id and node), with the bin's features.</summary>
        private static void BinCand(ThinkScratch sc, byte verb, int bin, int node, int cls, int stock, int dist, bool fromBelief)
        {
            int at = Thinker.AddCandidate(sc, verb, IntentTarget.Bin, bin, node, (byte)cls, dist, fromBelief);
            if (at < 0) return;
            uint need = BaseHas(sc, Feature.Situation(HSituation.DependantInNeed)) ? Feature.Situation(HSituation.DependantInNeed) : 0;
            Thinker.TargetFeatures(sc, at, Feature.Class(cls), Feature.Category(ObjCategory.Store), stock != 0 ? Feature.State(stock) : 0, Feature.PlaceRef(node), need);
        }

        /// <summary>I-BS amendment (12.31): a person it believes kin of any kind is remembered (a person target out of sight is then known).</summary>
        public static bool Remembers(ThinkScratch sc, int person) => M4(sc) && person != 0 && sc.Mind.Kin.IndexOf(person) >= 0;

        /// <summary>The kin term (Hamilton): <c>r̂ x (100 + KinWeight) / 100 x 0.6</c> for care aimed at a person, from its own beliefs only.</summary>
        public static int KinTerm(ThinkScratch sc, int cand)
        {
            if (!M4(sc) || cand < 0 || cand >= sc.CandCount) return 0;
            var c = sc.Cand[cand];
            if (c.Verb != HVerb.Feed && c.Verb != HVerb.Tend && c.Verb != HVerb.Adopt) return 0;
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                if (sc.People[i].Id != c.TargetId) continue;
                int rhat = KinData.RHat(sc.Mind, sc.People[i].As);
                int kw = sc.Traits != null && sc.Traits.Length > Trait.KinWeight ? sc.Traits[Trait.KinWeight] : 0;
                return rhat * (100 + kw) / 100 * KinTermPermille / 1000;
            }
            return 0;
        }

        /// <summary>
        /// The genome's instinct gains (BIOLOGICAL 4.4) on M4's situational instincts: hunger gain on begging, social gain on
        /// partnership and shared care, nurture gain on care. Read from its own <see cref="SelfView"/> (I-BS amendment 12.33;
        /// formerly copied into <c>Npc.Traits[12..15]</c>); 0 = no gain (an M3 NPC). Not on M3's drive instincts: MindView reports
        /// their need part without a gain, so a gain there would let the need exceed the instinct it is part of (kept as built,
        /// DEV_LOG I-BS record); nor on the store's (need-driven).
        /// </summary>
        public static int InstinctGain(ThinkScratch sc, byte verb, int prior)
        {
            if (prior == 0) return prior;
            var self = sc.Self;
            int g;
            switch (verb)
            {
                case HVerb.Beg: g = self.HungerGain; break;
                case HVerb.Propose: case HVerb.Accept: case HVerb.TryForChild: case HVerb.AskCare: case HVerb.AcceptCare: g = self.SocialGain; break;
                case HVerb.Feed: case HVerb.Tend: case HVerb.Adopt: case HVerb.Provision: g = self.NurtureGain; break;
                case HVerb.SeekChild: case HVerb.AcceptChild: g = self.SocialGain; break;
                default: return prior;
            }
            long v = g <= 0 ? prior : (long)prior * g / 1000;
            // D4 preferences (2 x pref / 1000, 500 neutral): companionship on partnering, child desire on everything about a child
            if (sc.Traits != null && sc.Traits.Length >= Preferences.TraitLength)
                switch (verb)
                {
                    case HVerb.Propose: case HVerb.Accept: v = v * 2 * sc.Traits[Preferences.TraitCompanionship] / 1000; break;
                    case HVerb.TryForChild: case HVerb.SeekChild: case HVerb.AcceptChild: v = v * 2 * sc.Traits[Preferences.TraitChildDesire] / 1000; break;
                }
            return (int)v;
        }

        /// <summary>Personality biases of M4's verbs (traits -100..100): generosity on care, boldness on proposing, patience against accepting at once, loyalty against leaving.</summary>
        public static int Personality(byte verb, int[] traits)
        {
            if (traits == null || traits.Length < Trait.Count) return 0;
            switch (verb)
            {
                case HVerb.Feed: case HVerb.Tend: case HVerb.Adopt: case HVerb.Provision: return traits[Trait.Generosity] / 4;
                case HVerb.Propose: return traits[Trait.Boldness] / 3;
                case HVerb.Accept: return -traits[Trait.Patience] / 3;
                case HVerb.TryForChild: case HVerb.SeekChild: case HVerb.AcceptChild: return traits[Trait.KinWeight] / 4;
                case HVerb.Leave: return -150 - traits[Trait.Loyalty] / 2;
                case HVerb.AskCare: return traits[Trait.Trustfulness] / 4;
                case HVerb.EndCare: return traits[Trait.Vengefulness] / 4;
                default: return 0;
            }
        }

        public static string Describe(uint f)
        {
            int v = Feature.ValueOf(f);
            switch (Feature.KindOf(f))
            {
                case FeatureKind.Role: return v == HRole.Child ? "is a child" : v == HRole.Elder ? "is old" : null;
                case FeatureKind.Self: return v == HSelf.Unpartnered ? "has no partner" : v == HSelf.HasDependants ? "has children at home" : v == HSelf.WantsChild ? "may look for a child" : null;
                case FeatureKind.ObjClass: return v == HClass.Store ? "household store" : v == HClass.Estate ? "estate goods" : null;
                case FeatureKind.ObjState:
                    switch (v)
                    {
                        case HState.Begging1: return "begging a little"; case HState.Begging2: return "begging"; case HState.Begging3: return "begging hard";
                        case HState.Proposing: return "proposing to it"; case HState.Trying: return "wants a child"; case HState.Sick: return "sick";
                        case HState.NeedsHome: return "needs a home"; case HState.AskingCare: return "asks for shared care";
                    }
                    return null;
                case FeatureKind.Situation:
                    switch (v)
                    {
                        case HSituation.DependantInNeed: return "a dependant in need"; case HSituation.CareOwed1: return "is owed a little care";
                        case HSituation.CareOwed2: return "is owed care"; case HSituation.CareOwed3: return "is owed much care";
                        case HSituation.UnionPartner: return "agreed a child with it"; case HSituation.SeekingChild: return "wants a child"; case HSituation.NoPlentyKnown: return "knows no plentiful food near";
                        case HSituation.HouseholdShort: return "its household is short of food";
                    }
                    return null;
                default: return null;
            }
        }
    }
}
