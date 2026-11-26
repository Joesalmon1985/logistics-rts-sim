using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// The physical side of M4's intents (sim code, called through <c>PopulationExtensions.TryStart/TryAct</c>): walk within
    /// the named reach of the target, then act through <see cref="Goods"/> or the household modules. Consent and permission
    /// are checked here, by the actor, on arrival (A4); a failed intent ends with the M3 frustration signal. Nothing here makes
    /// anyone act: each act is the intent its own policy chose.
    /// </summary>
    public static class HouseholdActs
    {
        public static bool Start(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            // a re-chosen intent continues its action (M3's Start keeps the action when verb and target are the same)
            return HVerb.IsMine(c.Verb) && s.Households != null;
        }

        public static bool Act(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var a = npc.Action;
            if (!HVerb.IsMine(a.Verb) || s.Households == null) return false;
            switch (a.Verb)
            {
                case HVerb.Beg: Beg(s, ev, npc); break;
                case HVerb.Feed: Feed(s, ev, npc, day); break;
                case HVerb.Tend: Tend(s, ev, npc, day); break;
                case HVerb.Adopt: Adopt(s, ev, npc, day); break;
                case HVerb.TakeFromStore: Take(s, ev, npc); break;
                case HVerb.Store: Deposit(s, ev, npc); break;
                case HVerb.Propose: Display(s, ev, npc, DisplayKind.Propose, s.Tuning.Households.Partner.ProposeNodes, s.Tuning.Households.Partner.ProposeTicks); break;
                case HVerb.AskCare: Display(s, ev, npc, DisplayKind.AskCare, s.Tuning.Households.Partner.ProposeNodes, s.Tuning.Households.Partner.ProposeTicks); break;
                case HVerb.Accept: Accept(s, ev, npc, day); break;
                case HVerb.TryForChild: Try(s, ev, npc); break;
                case HVerb.Leave: LeavePartner(s, ev, npc); break;
                case HVerb.AcceptCare: AcceptCare(s, ev, npc); break;
                case HVerb.EndCare: EndCare(s, ev, npc); break;
                case HVerb.SeekChild: SeekChild(s, ev, npc); break;
                case HVerb.AcceptChild: AcceptChild(s, ev, npc); break;
                default: End(npc); break;
            }
            return true;
        }

        // ---------------- helpers ----------------

        private static void End(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        private static void Frustrate(GodState s, Npc npc)
        {
            if (npc.Mind != null) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            End(npc);
        }

        private static Npc Target(GodState s, Npc npc)
        {
            var t = s.Population.Find(npc.Action.TargetId);
            return t != null && t.Alive && t.Id != npc.Id ? t : null;
        }

        /// <summary>One step towards a node (M3's walking rule, own path cache): true when within <paramref name="reach"/>.</summary>
        public static bool WalkTo(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var hs = s.Households;
            if (hs.Pather == null) hs.Pather = new NodePather(g);
            if (npc.Path == null || npc.PathGoal != goal || npc.PathIdx >= npc.Path.Length)
            {
                npc.Path = hs.Pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost) { npc.StepTicks = 0; npc.Node = next; npc.PathIdx++; }
            return g.Distance(npc.Node, goal) <= reach;
        }

        /// <summary>Walks towards a moving person; ends the action (frustrated) when there is no way.</summary>
        private static bool Reach(GodState s, Npc npc, Npc t, int reach)
        {
            if (WalkTo(s, npc, t.Node, reach)) return true;
            if (npc.Path == null) Frustrate(s, npc);
            return false;
        }

        private static int Rhat(Npc npc, int other) => KinData.RHat(npc.Mind, other);

        private static void Reward(GodState s, Npc npc, int vp) { if (npc.Mind != null && vp != 0) Learner.AddReward(npc.Mind.Store, vp); }

        /// <summary>Care ticks of this act (since it started) into the arrangement record of the two households, if any.</summary>
        private static void CountCare(GodState s, Npc carer, Npc cared, int ticks)
        {
            if (cared.Life != null) cared.Life.CareReceived += ticks;
            foreach (var c in s.Households.Care)
            {
                if (c.Carer == carer.Id && cared.Household == c.AskerHousehold) c.Given += ticks;
                else if (c.Asker == carer.Id && cared.Household == c.CarerHousehold) c.Returned += ticks;
            }
        }

        private static int Arrangement(GodState s, Npc carer, Npc cared)
        {
            foreach (var c in s.Households.Care)
                if (c.Carer == carer.Id && cared.Household == c.AskerHousehold || c.Asker == carer.Id && cared.Household == c.CarerHousehold) return c.Id;
            return 0;
        }

        // ---------------- children ----------------

        /// <summary>
        /// Begging (HOUSEHOLDS "Feeding"; SHARED_CONTRACTS 7.2): a child approaches the adult it chose and shows a display of
        /// intensity 1-3 (from its hunger band in M4) aimed at it. A display, never a message; anyone in sight sees it.
        /// </summary>
        private static void Beg(GodState s, EventBuffer ev, Npc npc)
        {
            var ct = s.Tuning.Households.Care;
            var t = Target(s, npc);
            var life = npc.Life;
            if (t == null || life == null) { End(npc); return; }
            if (life.Display == DisplayKind.Beg && life.DisplayUntil >= s.Tick)
            {
                if (s.Tick >= life.DisplayUntil) End(npc);
                return;
            }
            if (!Reach(s, npc, t, ct.BegNodes)) return;
            int band = Thinker.Band(s.Tuning.Population, Drive.Hunger, npc.DriveOf(Drive.Hunger));
            int level = Math.Max(1, band);
            life.Display = DisplayKind.Beg; life.DisplayLevel = (byte)level; life.DisplayTarget = t.Id; life.DisplayUntil = s.Tick + ct.BegTicks;
            ev.Add(new BeggingMade { Child = npc.Id, Target = t.Id, Intensity = level, HungerBand = band });
        }

        // ---------------- care ----------------

        /// <summary>
        /// Feeding (SHARED_CONTRACTS 2.5): a real hand-over of food from the feeder's carry to the person's carry, within
        /// <see cref="Population.Reach.HandOver"/>; ownership passes unless the food is the household's (kept). The child then
        /// eats it by its own decision. The feeder learns from what the act brought: a reward scaled by the visible need and
        /// what it believes the child is to it (r̂), the Hamilton channel (NPC_LEARNING 4.4).
        /// </summary>
        private static void Feed(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var ct = s.Tuning.Households.Care;
            var t = Target(s, npc);
            if (t == null) { End(npc); return; }
            int slot = -1;
            for (int i = 0; i < npc.Carry.Length; i++)
            {
                var l = npc.Carry[i];
                if (l.Empty || !Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade) continue;
                if (l.Owner.Kind == OwnerKind.Npc && l.Owner.Id != npc.Id) continue;
                if (l.Owner.Kind == OwnerKind.Household && l.Owner.Id != npc.Household) continue;
                if (l.Owner.Kind != OwnerKind.Npc && l.Owner.Kind != OwnerKind.Household && !l.Owner.IsNone) continue;
                slot = i; break;
            }
            if (slot < 0) { Frustrate(s, npc); return; }
            if (!Reach(s, npc, t, Population.Reach.HandOver)) return;
            var lot = npc.Carry[slot];
            int n = Math.Min(ct.FeedUnits, lot.Count);
            n = Math.Min(n, PopulationModule.Room(s, t, lot.Ware));
            if (n <= 0) { Frustrate(s, npc); return; }
            int arrangement = Arrangement(s, npc, t);
            var rule = lot.Owner.Kind == OwnerKind.Household ? OwnerRule.Keep : OwnerRule.ToReceiver;
            var r = Goods.Move(s, ev, Holder.Carry(npc.Id), lot.Id, n, Holder.Carry(t.Id), MoveReason.HandOver, npc.Id, rule, default, MergeRule.Merge, arrangement);
            if (!r.Ok) { Frustrate(s, npc); return; }
            int ticks = (int)Math.Max(1, s.Tick - npc.Action.Since);
            CountCare(s, npc, t, ticks);
            int rhat = Rhat(npc, t.Id);
            var band = KinData.BandOf(npc.Mind, t.Id);
            int trueR = Genetics.Relatedness.RPermille(s.Population.Pedigree, npc.Id, t.Id, s.Tuning.Genetics.InbreedingCheckDepth);
            if (t.Stage == LifeStage.Child)
            {
                ev.Add(new ChildFed { Feeder = npc.Id, Child = t.Id, Lot = r.Lot, Ware = lot.Ware, Count = n, Source = 1, Band = band, TrueR = trueR, Arrangement = arrangement });
                LifeCycleSystem.FedBy(t, npc, day);
            }
            else ev.Add(new CareGiven { Carer = npc.Id, Patient = t.Id, Kind = CareGiven.Feed, Ticks = ticks, Band = band, TrueR = trueR });
            int need = 500 + 250 * (t.Life != null && t.Life.Display == DisplayKind.Beg && t.Life.DisplayUntil >= s.Tick ? t.Life.DisplayLevel : 0);
            Reward(s, npc, (int)((long)ct.FeedRewardVp * n * (1000 + 2 * rhat) / 1000 * need / 1000));
            End(npc);
        }

        /// <summary>Tending (HOUSEHOLDS "Care of elders, the sick"): staying beside a sick or injured person; that day its illness is milder and it recovers sooner.</summary>
        private static void Tend(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var ct = s.Tuning.Households.Care;
            var t = Target(s, npc);
            if (t?.Life == null || t.Life.Illness != 1 && t.Injury <= 300000) { End(npc); return; }
            if (!Reach(s, npc, t, Population.Reach.Touch)) return;
            npc.Action.Progress++;
            t.Life.CareReceived++;
            if (npc.Action.Progress < ct.TendTicks) return;
            t.Life.TendedDay = day;
            int ticks = (int)Math.Max(1, s.Tick - npc.Action.Since);
            CountCare(s, npc, t, 0);
            foreach (var c in s.Households.Care) { if (c.Carer == npc.Id && t.Household == c.AskerHousehold) c.Given += ticks; else if (c.Asker == npc.Id && t.Household == c.CarerHousehold) c.Returned += ticks; }
            ev.Add(new CareGiven
            {
                Carer = npc.Id, Patient = t.Id, Kind = CareGiven.Tend, Ticks = ticks, Band = KinData.BandOf(npc.Mind, t.Id),
                TrueR = Genetics.Relatedness.RPermille(s.Population.Pedigree, npc.Id, t.Id, s.Tuning.Genetics.InbreedingCheckDepth),
            });
            Reward(s, npc, (int)((long)ct.TendRewardVp * (1000 + 2 * Rhat(npc, t.Id)) / 1000));
            End(npc);
        }

        /// <summary>
        /// Adoption (HOUSEHOLDS "Fostering and orphans", Q6): any adult may take in an orphan, a couple of any sex, a single
        /// adult or a larger household; judged by room, never by the kind of partnership. The child joins (Fostered); the adult
        /// believes it a foster child; a child old enough to be told believes the adult a foster parent, a young one is not
        /// told and comes to believe its carer a parent by the raising cue.
        /// </summary>
        private static void Adopt(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var t = Target(s, npc);
            if (t?.Life == null || t.Stage != LifeStage.Child || t.Life.OrphanSince < 0 || npc.Stage == LifeStage.Child) { End(npc); return; }
            if (!Reach(s, npc, t, Population.Reach.Touch)) return;
            var hs = s.Households;
            var h = npc.Household != 0 ? hs.Find(npc.Household) : null;
            if (h == null) h = HouseholdModule.Form(s, ev, npc, JoinReason.Founded);
            if (h.Members.Count >= Household.MaxMembers || t.Household == h.Id) { Frustrate(s, npc); return; }
            int from = t.Household;
            if (from != 0) HouseholdModule.Leave(s, ev, t, LeaveReason.FosteredOut, h.Id);
            HouseholdModule.Join(s, ev, h, t, JoinReason.Fostered, from);
            var cal = SimCalendar.Of(s);
            int days = day - t.Life.OrphanSince;
            t.Life.OrphanSince = -1;
            t.Life.Fostered = true;
            t.Life.ToldFostered = cal.AgeMilli(t.BornTick, s.Tick) >= s.Tuning.Households.Life.ForageFromMilli;
            foreach (int m in h.Members)
            {
                var a = s.Population.Find(m);
                if (a == null || a.Id == t.Id || a.Stage == LifeStage.Child) continue;
                KinCues.Believe(s, ev, a, t.Id, KinRole.FosterChild, a.Id == npc.Id ? 800 : 600, a.Id == npc.Id ? KinSource.Raised : KinSource.Told);
                if (t.Life.ToldFostered) KinCues.Believe(s, ev, t, a.Id, KinRole.FosterParent, a.Id == npc.Id ? 800 : 600, KinSource.Told);
            }
            hs.Stats.Fostered++;
            ev.Add(new ChildFostered { Child = t.Id, Adopter = npc.Id, Household = h.Id, FromHousehold = from, DaysOrphaned = days });
            Reward(s, t, s.Tuning.Households.Care.AcceptedVp);
            End(npc);
        }

        // ---------------- the household store ----------------

        private static LotBin StoreAt(GodState s, Npc npc, int hutId)
        {
            if (npc.Household == 0) return null;
            var h = s.Households.Find(npc.Household);
            if (h == null || h.Bin == 0 || h.Hut != hutId) return null;
            return Goods.BinById(s.Population, h.Bin);
        }

        /// <summary>Withdraw (SHARED_CONTRACTS 2.5): food from the household store into the carry, within <see cref="Population.Reach.Store"/>, owner kept.</summary>
        private static void Take(GodState s, EventBuffer ev, Npc npc)
        {
            var hut = s.Population.HutById(npc.Action.TargetId);
            if (hut == null) { Frustrate(s, npc); return; }
            if (!WalkTo(s, npc, hut.Node, Population.Reach.Store)) { if (npc.Path == null) Frustrate(s, npc); return; }
            var bin = StoreAt(s, npc, hut.Id);
            if (bin == null || !StoreAccess.MayTake(s, npc, bin) || !HouseholdHooks.CanWork(s, npc)) { Frustrate(s, npc); return; }
            int want = s.Tuning.Households.Care.TakeUnits, got = 0;
            for (int k = 0; k < bin.Lots.Count && got < want;)
            {
                var l = bin.Lots[k];
                if (!Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade) { k++; continue; }
                int n = Math.Min(want - got, Math.Min(l.Count, PopulationModule.Room(s, npc, l.Ware)));
                if (n <= 0) break;
                bool whole = n == l.Count;
                var r = Goods.Move(s, ev, Holder.Bin(bin.Id), l.Id, n, Holder.Carry(npc.Id), MoveReason.Withdraw, npc.Id, OwnerRule.Keep);
                if (!r.Ok) break;
                got += n;
                if (!whole) k++;
            }
            if (got == 0) { Frustrate(s, npc); return; }
            End(npc);
        }

        /// <summary>Deposit (SHARED_CONTRACTS 2.5): its own and its household's food into the store, the store's owner taking it (no exchange, no gift).</summary>
        private static void Deposit(GodState s, EventBuffer ev, Npc npc)
        {
            var hut = s.Population.HutById(npc.Action.TargetId);
            if (hut == null) { Frustrate(s, npc); return; }
            if (!WalkTo(s, npc, hut.Node, Population.Reach.Store)) { if (npc.Path == null) Frustrate(s, npc); return; }
            var bin = StoreAt(s, npc, hut.Id);
            if (bin == null) { Frustrate(s, npc); return; }
            int put = 0;
            for (int i = 0; i < npc.Carry.Length; i++)
            {
                var l = npc.Carry[i];
                if (l.Empty || !Wares.IsFood(l.Ware) || l.Ware == Ware.Nightshade) continue;
                if (!(l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == npc.Id || l.Owner.Kind == OwnerKind.Household && l.Owner.Id == npc.Household)) continue;
                var r = Goods.Move(s, ev, Holder.Carry(npc.Id), l.Id, l.Count, Holder.Bin(bin.Id), MoveReason.Deposit, npc.Id, OwnerRule.ToBinOwner);
                if (r.Ok) put += l.Count;
            }
            if (put == 0) Frustrate(s, npc); else End(npc);
        }

        // ---------------- partnership ----------------

        /// <summary>A display aimed at a person (a proposal, a request for shared care): approach, show it for a while, then stop.</summary>
        private static void Display(GodState s, EventBuffer ev, Npc npc, byte kind, int nodes, int ticks)
        {
            var t = Target(s, npc);
            var life = npc.Life;
            if (t == null || life == null) { End(npc); return; }
            if (life.Display == kind && life.DisplayTarget == t.Id && life.DisplayUntil >= s.Tick)
            {
                if (s.Tick >= life.DisplayUntil) End(npc);
                return;
            }
            if (kind == DisplayKind.Propose && !MayPartner(s, npc, t)) { Frustrate(s, npc); return; }
            if (!Reach(s, npc, t, nodes)) return;
            life.Display = kind; life.DisplayLevel = 1; life.DisplayTarget = t.Id; life.DisplayUntil = s.Tick + ticks;
            if (kind == DisplayKind.Propose) ev.Add(new CourtshipAct { Giver = npc.Id, Receiver = t.Id, Kind = 1 });
        }

        /// <summary>Both are adults under the partnership cap and not already partners (nothing reads their sexes, Q6).</summary>
        private static bool MayPartner(GodState s, Npc a, Npc b)
        {
            int cap = s.Tuning.Households.Partner.MaxLivePartnerships;
            if (a.Stage == LifeStage.Child || b.Stage == LifeStage.Child || a.Life == null || b.Life == null) return false;
            if (Partnerships.Between(s, a.Id, b.Id) != null) return false;
            return Partnerships.Count(s, a.Id) < cap && Partnerships.Count(s, b.Id) < cap;
        }

        /// <summary>
        /// Accepting a proposal it perceives (HOUSEHOLDS "Accepting"; SHARED_CONTRACTS 7.2): the partnership forms because both
        /// chose it (the proposer by proposing, this NPC now). The two households join where there is a hut (else the
        /// proposer's), the one who moves bringing its own dependent children; kin beliefs follow from the partnership.
        /// </summary>
        private static void Accept(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var t = Target(s, npc);
            var ct = s.Tuning.Households.Care;
            if (t?.Life == null || t.Life.Display != DisplayKind.Propose || t.Life.DisplayTarget != npc.Id || t.Life.DisplayUntil < s.Tick || !MayPartner(s, npc, t))
            { Frustrate(s, npc); return; }
            var hs = s.Households;
            var ht = t.Household != 0 ? hs.Find(t.Household) : null;
            var hn = npc.Household != 0 ? hs.Find(npc.Household) : null;
            Household into; Npc mover;
            if (ht == null && hn == null) { into = HouseholdModule.Form(s, ev, t, JoinReason.Partnered); mover = npc; }
            else if (hn == null) { into = ht; mover = npc; }
            else if (ht == null) { into = hn; mover = t; }
            else if (hn.Hut != 0 && ht.Hut == 0) { into = hn; mover = t; }
            else { into = ht; mover = npc; }
            var p = Partnerships.Form(s, ev, t.Id, npc.Id, t.Id, into.Id);
            KinCues.Believe(s, ev, t, npc.Id, KinRole.Partner, 900, KinSource.Partnered);
            KinCues.Believe(s, ev, npc, t.Id, KinRole.Partner, 900, KinSource.Partnered);
            var other = mover == npc ? t : npc;
            if (mover.Household != into.Id)
            {
                var old = mover.Household != 0 ? hs.Find(mover.Household) : null;
                var kids = new List<Npc>();
                if (old != null)
                    foreach (int m in old.Members)
                    {
                        var k = s.Population.Find(m);
                        if (k == null || k.Stage != LifeStage.Child) continue;
                        var role = KinData.RoleOf(mover.Mind, k.Id);
                        if (role == KinRole.Daughter || role == KinRole.Son || role == KinRole.FosterChild) kids.Add(k);
                    }
                if (into.Members.Count + 1 + kids.Count <= Household.MaxMembers)
                {
                    if (mover.HomeHut != 0 && into.Hut != 0 && mover.HomeHut != into.Hut)
                    { var oldHut = s.Population.HutById(mover.HomeHut); if (oldHut != null) HutRights.Revoke(s, ev, oldHut, mover, LeaveReason.Partnered); }
                    HouseholdModule.Move(s, ev, mover, into, LeaveReason.Partnered, JoinReason.Partnered);
                    foreach (var k in kids)
                    {
                        HouseholdModule.Move(s, ev, k, into, LeaveReason.Partnered, JoinReason.Partnered);
                        KinCues.Believe(s, ev, other, k.Id, KinRole.StepChild, 600, KinSource.Partnered);
                        KinCues.Believe(s, ev, k, other.Id, KinRole.StepParent, 600, KinSource.Partnered);
                    }
                    Reward(s, mover, ct.AcceptedVp);
                }
            }
            // the other partner's household children become step-children of the one who joined
            foreach (int m in into.Members)
            {
                var k = s.Population.Find(m);
                if (k == null || k.Stage != LifeStage.Child || k.Id == npc.Id || k.Id == t.Id) continue;
                if (KinData.RoleOf(mover.Mind, k.Id) != KinRole.None) continue;
                KinCues.Believe(s, ev, mover, k.Id, KinRole.StepChild, 600, KinSource.Partnered);
                KinCues.Believe(s, ev, k, mover.Id, KinRole.StepParent, 600, KinSource.Partnered);
            }
            t.Life.DisplayUntil = s.Tick - 1; t.Life.Display = 0;
            if (t.Action.Verb == HVerb.Propose) End(t);
            Reward(s, npc, ct.PartneredVp);
            Reward(s, t, ct.PartneredVp);
            End(npc);
        }

        /// <summary>Trying for a child (HOUSEHOLDS "Reproduction decisions"): held for a while; a conception needs both partners holding it.</summary>
        private static void Try(GodState s, EventBuffer ev, Npc npc)
        {
            var t = Target(s, npc);
            var p = t != null ? Partnerships.Between(s, npc.Id, t.Id) : null;
            var u = t != null ? ReproductiveUnions.Between(s, npc.Id, t.Id) : null;
            if (p == null && u == null || npc.Stage == LifeStage.Child) { Frustrate(s, npc); return; }
            var life = npc.Life;
            bool fresh = life.TryWith != t.Id || life.TryingUntil < s.Tick;
            life.TryWith = t.Id;
            life.TryingUntil = s.Tick + s.Tuning.Households.Life.TryingTicks;
            if (life.Display == 0 || life.DisplayUntil < s.Tick) { life.Display = DisplayKind.Trying; life.DisplayLevel = 1; life.DisplayTarget = t.Id; life.DisplayUntil = life.TryingUntil; }
            if (fresh) ev.Add(new TryForChildChosen { Npc = npc.Id, Partner = t.Id, Partnership = p?.Id ?? 0 });
            if (u != null) u.LastTryTick = s.Tick;
            End(npc);
        }

        /// <summary>
        /// D4: showing that it wants a child (a display at its own spot, seen by those it could have one with). Nothing happens
        /// unless someone chooses to accept. Not shown while it cannot take a reproductive relationship (one already, pregnant,
        /// just given birth), or when the scenario allows reproduction only between romantic partners.
        /// </summary>
        private static void SeekChild(GodState s, EventBuffer ev, Npc npc)
        {
            var life = npc.Life;
            var pt = s.Tuning.Households.Partner;
            if (life == null || npc.Stage != LifeStage.Adult || pt.IndependentUnions == 0 || !ReproductiveUnions.HasRoom(s, npc) || life.PregFather != 0) { End(npc); return; }
            if (life.Display == DisplayKind.SeekChild && life.DisplayUntil >= s.Tick) { if (s.Tick >= life.DisplayUntil) End(npc); return; }
            if (life.Display == DisplayKind.SeekChild) { End(npc); return; }
            if (!Preferences.AllowsOutside(s, npc, 0)) { Frustrate(s, npc); return; }
            life.Display = DisplayKind.SeekChild; life.DisplayLevel = 1; life.DisplayTarget = 0; life.DisplayUntil = s.Tick + pt.SeekTicks;
            ev.Add(new ChildWanted { Npc = npc.Id, Desire = life.ChildDesire, Partnered = Partnerships.Count(s, npc.Id) > 0 });
        }

        /// <summary>
        /// D4: accepting someone's shown wish for a child. Both adults chose it (the other by showing it, this one now); both must
        /// have room for a reproductive relationship, and both their commitments must allow one outside their partnerships.
        /// Nobody's partnership or household changes.
        /// </summary>
        private static void AcceptChild(GodState s, EventBuffer ev, Npc npc)
        {
            var t = Target(s, npc);
            if (t?.Life == null || npc.Life == null || t.Life.Display != DisplayKind.SeekChild || t.Life.DisplayUntil < s.Tick || npc.Stage != LifeStage.Adult || t.Stage != LifeStage.Adult)
            { Frustrate(s, npc); return; }
            if (!Reach(s, npc, t, s.Tuning.Households.Partner.ProposeNodes)) return;
            if (!Reproduction.MayConceiveWith(npc, t) || !ReproductiveUnions.HasRoom(s, npc) || !ReproductiveUnions.HasRoom(s, t)
                || !Preferences.AllowsOutside(s, npc, t.Id) || !Preferences.AllowsOutside(s, t, npc.Id)) { Frustrate(s, npc); return; }
            var kind = Partnerships.Between(s, npc.Id, t.Id) != null ? ReproductiveUnion.Partners : ReproductiveUnion.Independent;
            ReproductiveUnions.Form(s, ev, t, npc, t.Id, kind);
            t.Life.DisplayUntil = s.Tick - 1; t.Life.Display = 0;
            if (t.Action.Verb == HVerb.SeekChild) End(t);
            int vp = s.Tuning.Households.Partner.UnionVp;
            Reward(s, npc, vp); Reward(s, t, vp);
            End(npc);
        }

        /// <summary>
        /// Leaving a partner (HOUSEHOLDS "Desertion"): the partnership ends by this NPC's decision; it leaves the household
        /// and its bed there (the remaining partner keeps the hut and the children) and starts a household of its own.
        /// </summary>
        private static void LeavePartner(GodState s, EventBuffer ev, Npc npc)
        {
            var t = Target(s, npc);
            var p = t != null ? Partnerships.Between(s, npc.Id, t.Id) : null;
            if (p == null) { Frustrate(s, npc); return; }
            Partnerships.End(s, ev, p, PartnershipEnded.Left, npc.Id);
            KinCues.Forget(npc, t.Id);
            KinCues.Forget(t, npc.Id);
            if (npc.Household != 0 && npc.Household == t.Household)
            {
                var h = s.Households.Find(npc.Household);
                if (h != null && h.Hut != 0) { var hut = s.Population.HutById(h.Hut); if (hut != null) HutRights.Revoke(s, ev, hut, npc, LeaveReason.Deserted); }
                HouseholdModule.Leave(s, ev, npc, LeaveReason.Deserted, 0);
                HouseholdModule.Form(s, ev, npc, JoinReason.Founded);
            }
            End(npc);
        }

        // ---------------- shared childcare ----------------

        private static void AcceptCare(GodState s, EventBuffer ev, Npc npc)
        {
            var t = Target(s, npc);
            var hs = s.Households;
            if (t?.Life == null || t.Life.Display != DisplayKind.AskCare || t.Life.DisplayTarget != npc.Id || t.Life.DisplayUntil < s.Tick
                || t.Household == 0 || npc.Household == 0 || t.Household == npc.Household) { Frustrate(s, npc); return; }
            foreach (var c in hs.Care) if (c.Asker == t.Id && c.Carer == npc.Id || c.Asker == npc.Id && c.Carer == t.Id) { End(npc); return; }
            var a = new CareArrangement { Id = hs.NextCareId++, Asker = t.Id, Carer = npc.Id, AskerHousehold = t.Household, CarerHousehold = npc.Household, AgreedTick = s.Tick };
            hs.Care.Add(a);
            ev.Add(new CareArrangementMade { Arrangement = a.Id, Asker = t.Id, Carer = npc.Id, AskerHousehold = a.AskerHousehold, CarerHousehold = a.CarerHousehold });
            t.Life.DisplayUntil = s.Tick - 1; t.Life.Display = 0;
            if (t.Action.Verb == HVerb.AskCare) End(t);
            End(npc);
        }

        private static void EndCare(GodState s, EventBuffer ev, Npc npc)
        {
            var t = Target(s, npc);
            var hs = s.Households;
            if (t == null) { End(npc); return; }
            for (int i = 0; i < hs.Care.Count; i++)
            {
                var c = hs.Care[i];
                if (!(c.Asker == t.Id && c.Carer == npc.Id || c.Asker == npc.Id && c.Carer == t.Id)) continue;
                hs.Care.RemoveAt(i);
                ev.Add(new CareArrangementEnded { Arrangement = c.Id, EndedBy = npc.Id, Given = c.Given, Returned = c.Returned, Reason = 1 });
                break;
            }
            End(npc);
        }
    }
}
