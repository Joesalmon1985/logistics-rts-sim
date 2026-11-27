using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Social;

namespace God.Sim.Civ
{
    /// <summary>
    /// <see cref="FeatureKind.Posting"/> values C3 gives its candidates (what the mind makes of an employment option): 1-4
    /// the band of its own valuation (for a worker: the pay against the effort, the walk, the risk its trust leaves and the
    /// food it could forage instead; for an employer: the expected output against the pay it gives up), 16+ the situation.
    /// </summary>
    public static class PostingValue
    {
        public const int Poor = 1, Low = 2, Fair = 3, Good = 4;
        public const int Offered = 16, ActiveUnpaid = 17, Suspended = 18, SuspendedRepaid = 19, Due = 20, LossSmall = 21, LossBig = 22, Active = 23;
        public static uint F(int v) => Feature.Make(FeatureKind.Posting, v);
    }

    /// <summary>
    /// C3's mind-side slots (OWNER: C3). Like every mind hook they read only the think scratch: what is seen and heard now,
    /// the mind's own beliefs (its believed contracts and assets), its carry and its own price beliefs; never the true
    /// state. Each option is valued by the person itself (<see cref="Valuation"/>) and offered as a candidate with its
    /// features; the policy chooses among them with everything else it could do, so taking a job, paying a wage or
    /// walking out is always the person's choice.
    /// </summary>
    public static partial class CivMindHooks
    {
        static partial void VerbsC3(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(Employment.VerbHire, "offer work", VerbGroup.Employment, Employment.IntentOffer, deferred: true));
            into.Add(new VerbInfo(Employment.VerbWorkFor, "work for someone", VerbGroup.Employment, Employment.IntentAccept, deferred: true));
            into.Add(new VerbInfo(Employment.VerbPay, "pay a wage", VerbGroup.Employment, Employment.IntentPay));
        }

        /// <summary>
        /// Innate priors (they matter only for C3's own candidates, which exist only while Civ is on): good pay draws an
        /// acceptance and poor pay a refusal; unpaid work draws a walk-out; a worker who walked out goes back when it has
        /// been paid or offered better, not before; an employer offers when the work is worth more to it than the pay,
        /// pays what it owes (less readily when the pay is a big loss to it), and answers a walk-out with a better offer
        /// rather than at once with a dismissal.
        /// </summary>
        static partial void InstinctsC3(List<Instinct> into)
        {
            uint accept = Feature.MsgAct((int)GestureAct.Accept), refuse = Feature.MsgAct((int)GestureAct.Refuse), offer = Feature.MsgAct((int)GestureAct.Offer);
            byte w = Employment.VerbWorkFor, h = Employment.VerbHire, p = Employment.VerbPay;
            into.Add(new Instinct(PostingValue.F(PostingValue.Good), accept, w, 400));
            into.Add(new Instinct(PostingValue.F(PostingValue.Fair), accept, w, 250));
            into.Add(new Instinct(PostingValue.F(PostingValue.Low), accept, w, 50));
            into.Add(new Instinct(PostingValue.F(PostingValue.Poor), accept, w, -200));
            into.Add(new Instinct(PostingValue.F(PostingValue.Poor), refuse, w, 300));
            into.Add(new Instinct(PostingValue.F(PostingValue.Low), refuse, w, 150));
            into.Add(new Instinct(PostingValue.F(PostingValue.Good), refuse, w, -200));
            into.Add(new Instinct(PostingValue.F(PostingValue.ActiveUnpaid), refuse, w, 700));
            into.Add(new Instinct(PostingValue.F(PostingValue.ActiveUnpaid), accept, w, -400));
            into.Add(new Instinct(PostingValue.F(PostingValue.Suspended), accept, w, -450));
            into.Add(new Instinct(PostingValue.F(PostingValue.SuspendedRepaid), accept, w, 300));
            into.Add(new Instinct(PostingValue.F(PostingValue.Suspended), refuse, w, 100));
            into.Add(new Instinct(PostingValue.F(PostingValue.Good), offer, h, 300));
            into.Add(new Instinct(PostingValue.F(PostingValue.Fair), offer, h, 150));
            into.Add(new Instinct(PostingValue.F(PostingValue.Poor), offer, h, -200));
            into.Add(new Instinct(PostingValue.F(PostingValue.Suspended), offer, h, 250));
            into.Add(new Instinct(PostingValue.F(PostingValue.Suspended), refuse, h, 100));
            into.Add(new Instinct(PostingValue.F(PostingValue.Due), 0, p, 350));
            into.Add(new Instinct(PostingValue.F(PostingValue.LossBig), 0, p, -250));
        }

        /// <summary>Industry favours working for someone and offering work; honesty favours paying what is owed.</summary>
        static partial void PersonalityC3(byte verb, int[] traits, ref int value)
        {
            if (verb == Employment.VerbWorkFor) value += traits[Trait.Industry] / 2;
            else if (verb == Employment.VerbHire) value += traits[Trait.Industry] / 4;
            else if (verb == Employment.VerbPay) value += traits[Trait.Honesty] / 2;
        }

        static partial void RemembersC3(ThinkScratch sc, int person, ref bool remembers)
        {
            if (remembers) return;
            var cm = sc.Mind?.Civ;
            if (cm == null) return;
            foreach (var j in cm.Jobs) if (j.Other == person && j.Status != ContractStatus.Ended) { remembers = true; return; }
        }

        static partial void DescribeC3(uint f, ref string text)
        {
            if (text != null || Feature.KindOf(f) != FeatureKind.Posting) return;
            switch (Feature.ValueOf(f))
            {
                case PostingValue.Poor: text = "work that pays poorly"; break;
                case PostingValue.Low: text = "work that pays little"; break;
                case PostingValue.Fair: text = "work that pays fairly"; break;
                case PostingValue.Good: text = "work that pays well"; break;
                case PostingValue.Offered: text = "offered work"; break;
                case PostingValue.ActiveUnpaid: text = "working unpaid"; break;
                case PostingValue.Suspended: text = "holding back my work"; break;
                case PostingValue.SuspendedRepaid: text = "paid what I was owed"; break;
                case PostingValue.Due: text = "owing a wage"; break;
                case PostingValue.LossSmall: text = "a wage that costs me little"; break;
                case PostingValue.LossBig: text = "a wage that costs me much"; break;
                case PostingValue.Active: text = "in work"; break;
            }
        }

        // ---------------------------------------------------------------- candidates

        /// <summary>
        /// Candidates (the Civ quota of 4 is shared with C1, which adds first; C3 may take back C1's last candidates while
        /// C1 keeps at least 2, so C3 has at least 2: the split recorded in the C3 record). In priority order: pay a wage it
        /// believes it owes; answer an offer of work addressed to it (accept or refuse, a pair); as a worker, walk out,
        /// go back or give up; work for its employer; as an employer, answer a walk-out (a better offer or a dismissal);
        /// offer work at an asset it owns to a person it sees.
        /// </summary>
        static partial void AddCandidatesC3(ThinkScratch sc)
        {
            var cm = sc.Mind?.Civ;
            if (cm == null) return;
            cm.PlanCount = 0;
            var tu = cm.C3;
            if (tu == null || tu.HireEnabled == 0 || sc.Mind.Social == null) return;
            PayCandidates(sc, cm);
            AnswerOffers(sc, cm, tu);
            WorkerCandidates(sc, cm, tu);
            EmployerCandidates(sc, cm, tu);
        }

        /// <summary>Room for <paramref name="need"/> more candidates in the Civ block, taking back C1's last ones while C1 keeps at least 2.</summary>
        private static bool Room(ThinkScratch sc, int need)
        {
            int start = sc.CandLimit - CognitionExtensions.CandidateQuota;
            while (sc.CandLimit - sc.CandCount < need && sc.CandCount - start > 2 && sc.CandCount > 0) sc.CandCount--;
            return sc.CandLimit - sc.CandCount >= need && sc.Mind.Civ.PlanCount + need <= CivMind.MaxPlans;
        }

        private static int AddPlanned(ThinkScratch sc, byte verb, NpcVerb intent, IntentTarget kind, int target, int node, int cls, int dist, in C3Plan plan,
            uint f0, uint f1 = 0, uint f2 = 0, uint f3 = 0, uint f4 = 0, uint f5 = 0)
        {
            var cm = sc.Mind.Civ;
            int at = Thinker.AddCandidate(sc, verb, kind, target, node, (byte)cls, dist, false);
            if (at < 0) return -1;
            sc.Cand[at].Intent = intent; // the negotiation act (the learning verb is shared, the target features tell them apart)
            Thinker.TargetFeatures(sc, at, f0, f1, f2, f3, f4, f5);
            var p = plan;
            p.Verb = verb; p.Intent = intent; p.Kind = kind; p.Target = target;
            cm.Plans[cm.PlanCount++] = p;
            return at;
        }

        private static int PersonIndex(ThinkScratch sc, int handle)
        {
            for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].Id == handle && !sc.People[k].Asleep) return k;
            return -1;
        }

        private static int Held(ThinkScratch sc, Ware w)
        {
            int n = 0;
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (l.Empty || l.Ware != w) continue;
                var b = Permits.Believed(sc, l.Owner);
                if (b.Allowed && b.Basis != PermissionBasis.Ownerless) n += l.Count;
            }
            return n;
        }

        private static short[] Price(ThinkScratch sc) => sc.Mind.Social?.Price;

        /// <summary>Its price belief of a ware, or the ware's own use to it at a moderate need when it has none.</summary>
        private static int PriceOrOwn(short[] price, Ware w) =>
            price != null && (int)w < price.Length ? Valuation.PriceOf(price, w) : Math.Max(1, Valuation.Marginal(w, 0, 300, true, true, null));

        /// <summary>The ware of a food place it believes in (none for nightshade and others).</summary>
        private static Ware FoodOf(int cls) => cls == ObjClass.BerryBush ? Ware.Berries : cls == ObjClass.RootPatch ? Ware.Roots : cls == ObjClass.FishSpot ? Ware.Fish : Ware.None;

        /// <summary>
        /// The worker's own valuation of a pay offer (worth units): the pay's marginal worth to it now, minus what it could
        /// forage in a shift from the best stocked food place it remembers within recall, the effort (more when tired), the
        /// walk, and the share of the pay its distrust says it may never see. Banded into <see cref="PostingValue"/> 1-4.
        /// </summary>
        public static int WorkerBand(ThinkScratch sc, CivC3Tuning tu, Ware w, int count, int dist, int trust)
        {
            var self = sc.Self;
            var price = Price(sc);
            bool tool = self.CarryTool;
            int pay = w == Ware.None ? 0 : Valuation.Gain(w, count, Held(sc, w), self.Hunger, self.HasBed, tool, price);
            int alt = 0;
            var b = sc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++)
            {
                var x = b.Slots[i];
                if (x.Kind != BeliefKind.Food || x.Stock == ObjState.StockEmpty) continue;
                var fw = FoodOf(x.Class);
                if (fw == Ware.None || sc.Grid.Distance(self.Node, x.Subject) > sc.Tuning.RecallNodes) continue;
                alt = Math.Max(alt, Valuation.Gain(fw, tu.ForageUnitsPerShift, Held(sc, fw), self.Hunger, self.HasBed, tool, price));
            }
            int effort = tu.EffortWorth * (1000 + self.Fatigue) / 1000;
            int walk = dist * tu.WalkWorthPerNode;
            int risk = pay * (1000 - Fx.Clamp(trust, -1000, 1000)) / 4000;
            int net = pay - alt - effort - walk - risk;
            return net < -60 ? PostingValue.Poor : net < 0 ? PostingValue.Low : net < 120 ? PostingValue.Fair : PostingValue.Good;
        }

        /// <summary>The employer's own valuation of hiring at an asset: the output of a shift at its believed price against the pay it gives up.</summary>
        public static int EmployerBand(ThinkScratch sc, CivC3Tuning tu, AssetDef def, Ware w, int count, int believedStore)
        {
            var self = sc.Self;
            var price = Price(sc);
            int gain = 0;
            if (def.Recipe != null && def.WorkTicks > 0)
                foreach (var o in def.Recipe.Outputs)
                    gain += tu.ShiftTicks * o.Count / def.WorkTicks * PriceOrOwn(price, o.Ware);
            int loss = Valuation.Loss(w, count, Held(sc, w) + believedStore, self.Hunger, self.HasBed, self.CarryTool, price);
            int net = gain - loss;
            return net > 100 ? PostingValue.Good : net > 0 ? PostingValue.Fair : net > -100 ? PostingValue.Low : PostingValue.Poor;
        }

        /// <summary>The part of a ware's believed worth a wage costs it: small or big (its own valuation).</summary>
        private static int LossBand(ThinkScratch sc, Ware w, int count)
        {
            int loss = Valuation.Loss(w, count, Math.Max(count, Held(sc, w)), sc.Self.Hunger, sc.Self.HasBed, sc.Self.CarryTool, Price(sc));
            return loss > 250 ? PostingValue.LossBig : PostingValue.LossSmall;
        }

        private static void PayCandidates(ThinkScratch sc, CivMind cm)
        {
            for (int i = 0; i < cm.Jobs.Count; i++)
            {
                var j = cm.Jobs[i];
                if (j.Side != JobBelief.AsEmployer || j.Status == ContractStatus.Ended || j.Status == ContractStatus.Offered) continue;
                int owed = j.UnitsWorked - j.UnitsPaid;
                if (owed <= 0 || j.PayCount == 0) continue;
                // it pays from what it carries, or from the output of its own asset (it knows what the work put there)
                int believedStore = AssetTable.OfClass(j.AssetClass)?.Recipe?.Outputs.Count > 0 && FirstOutput(j.AssetClass) == j.PayWare ? j.UnitsWorked * OutputPerShift(j) : 0;
                if (Held(sc, j.PayWare) + believedStore < j.PayCount) continue;
                int pi = PersonIndex(sc, j.Other);
                int node = pi >= 0 ? sc.People[pi].Node : j.AssetNode;
                int dist = pi >= 0 ? sc.People[pi].Dist : sc.Grid.Distance(sc.Self.Node, j.AssetNode);
                if (pi < 0 && dist > sc.Tuning.RecallNodes) continue;
                if (!Room(sc, 1)) return;
                var plan = new C3Plan { Act = EmploymentAct.Pay, Contract = j.Contract, Asset = j.Asset, Ware = j.PayWare, Count = j.PayCount, Units = (byte)Math.Min(owed, 9) };
                AddPlanned(sc, Employment.VerbPay, Employment.IntentPay, IntentTarget.Npc, j.Other, node, 0, dist, plan,
                    PostingValue.F(PostingValue.Due), PostingValue.F(LossBand(sc, j.PayWare, j.PayCount * Math.Min(owed, 9))), Feature.NpcRef(j.Other), Feature.Make(FeatureKind.Role, RoleValue.Employer));
                return; // one wage at a time
            }
        }

        private static Ware FirstOutput(int cls)
        {
            var d = AssetTable.OfClass(cls);
            return d?.Recipe != null && d.Recipe.Outputs.Count > 0 ? d.Recipe.Outputs[0].Ware : Ware.None;
        }

        private static int OutputPerShift(in JobBelief j)
        {
            var d = AssetTable.OfClass(j.AssetClass);
            if (d?.Recipe == null || d.WorkTicks <= 0 || d.Recipe.Outputs.Count == 0) return 0;
            return Math.Max(1, j.ShiftTicks / d.WorkTicks) * d.Recipe.Outputs[0].Count;
        }

        /// <summary>An offer of work addressed to it, heard now from a person it sees: accept or refuse, valued by itself.</summary>
        private static void AnswerOffers(ThinkScratch sc, CivMind cm, CivC3Tuning tu)
        {
            for (int h = 0; h < sc.HeardCount; h++)
            {
                var msg = sc.Heard[h];
                if (msg.Act != GestureAct.Offer || msg.Topic.Kind != TopicKind.Posting || msg.Topic.Sub != sc.Self.Id || msg.Counter.Kind != TopicKind.Ware) continue;
                int contract = msg.Topic.Id;
                int ji = cm.IndexOfJob(contract);
                // an offer it already took up (or ended) needs no answer; a renegotiating offer on a suspended job is answered there
                if (ji >= 0 && cm.Jobs[ji].Status != ContractStatus.Offered) continue;
                int pi = -1;
                for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].As == msg.Speaker && msg.Speaker != 0 && !sc.People[k].Asleep) { pi = k; break; }
                if (pi < 0) continue; // the speaker is out of sight: no answer can be made to it
                var who = sc.People[pi];
                if (who.SameHousehold) continue;
                int asset = msg.Counter.Sub;
                var w = (Ware)msg.Counter.Id;
                int count = Math.Max(1, (int)msg.Quantity);
                int oi = cm.IndexOfAsset(asset);
                int node = oi >= 0 ? cm.Own[oi].Node : who.Node;
                int cls = oi >= 0 ? cm.Own[oi].Class : 0;
                int dist = sc.Grid.Distance(sc.Self.Node, node);
                int band = WorkerBand(sc, tu, w, count, dist, SocialView.Trust(sc.Mind, who.As));
                if (!Room(sc, 2)) return;
                var plan = new C3Plan { Contract = contract, Asset = asset, Ware = w, Count = (byte)count };
                plan.Act = EmploymentAct.Accept;
                AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentAccept, IntentTarget.Building, asset, node, cls, dist, plan,
                    PostingValue.F(band), PostingValue.F(PostingValue.Offered), Feature.MsgAct((int)GestureAct.Accept), Feature.NpcRef(who.As), Feature.Category(ObjCategory.Workplace));
                plan.Act = EmploymentAct.Refuse;
                AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentRefuse, IntentTarget.Npc, who.Id, who.Node, 0, who.Dist, plan,
                    PostingValue.F(band), PostingValue.F(PostingValue.Offered), Feature.MsgAct((int)GestureAct.Refuse), Feature.NpcRef(who.As));
                return; // one offer answered at a time
            }
        }

        /// <summary>As a worker: walk out of unpaid work, take up or give up work it holds back, or go to work.</summary>
        private static void WorkerCandidates(ThinkScratch sc, CivMind cm, CivC3Tuning tu)
        {
            for (int i = 0; i < cm.Jobs.Count; i++)
            {
                var j = cm.Jobs[i];
                if (j.Side != JobBelief.AsWorker || j.Status == ContractStatus.Ended || j.Status == ContractStatus.Offered) continue;
                int dist = sc.Grid.Distance(sc.Self.Node, j.AssetNode);
                int pi = PersonIndex(sc, j.Other);
                int band = WorkerBand(sc, tu, j.PayWare, j.PayCount, dist, j.Trust);
                var plan = new C3Plan { Contract = j.Contract, Asset = j.Asset, Ware = j.PayWare, Count = j.PayCount };
                uint who = Feature.NpcRef(j.Other);
                if (j.Status == ContractStatus.Active)
                {
                    if (j.Unpaid >= tu.WalkOutUnpaid)
                    {
                        if (!Room(sc, 1)) return;
                        plan.Act = EmploymentAct.Refuse;
                        AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentRefuse, IntentTarget.Npc, j.Other, pi >= 0 ? sc.People[pi].Node : sc.Self.Node, 0, pi >= 0 ? sc.People[pi].Dist : 0, plan,
                            PostingValue.F(PostingValue.ActiveUnpaid), Feature.MsgAct((int)GestureAct.Refuse), who);
                        if (!Room(sc, 1)) return;
                        plan.Act = EmploymentAct.Accept;
                        AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentAccept, IntentTarget.Building, j.Asset, j.AssetNode, j.AssetClass, dist, plan,
                            PostingValue.F(band), PostingValue.F(PostingValue.ActiveUnpaid), Feature.MsgAct((int)GestureAct.Accept), who, Feature.Category(ObjCategory.Workplace));
                        return;
                    }
                    if (!Room(sc, 1)) return;
                    plan.Act = EmploymentAct.Accept;
                    AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentAccept, IntentTarget.Building, j.Asset, j.AssetNode, j.AssetClass, dist, plan,
                        PostingValue.F(band), PostingValue.F(PostingValue.Active), Feature.MsgAct((int)GestureAct.Accept), who, Feature.Category(ObjCategory.Workplace));
                    return;
                }
                if (j.Status == ContractStatus.Suspended && j.By == EmploymentContract.ByWorker)
                {
                    // back to work once it was paid what it was owed (or the terms were raised: a better band), else not; or give it up
                    bool repaid = j.Unpaid < tu.WalkOutUnpaid && band >= PostingValue.Fair;
                    if (!Room(sc, 2)) return;
                    plan.Act = EmploymentAct.Accept;
                    AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentAccept, IntentTarget.Building, j.Asset, j.AssetNode, j.AssetClass, dist, plan,
                        PostingValue.F(band), PostingValue.F(repaid ? PostingValue.SuspendedRepaid : PostingValue.Suspended), Feature.MsgAct((int)GestureAct.Accept), who, Feature.Category(ObjCategory.Workplace));
                    plan.Act = EmploymentAct.Refuse;
                    AddPlanned(sc, Employment.VerbWorkFor, Employment.IntentRefuse, IntentTarget.Npc, j.Other, pi >= 0 ? sc.People[pi].Node : sc.Self.Node, 0, pi >= 0 ? sc.People[pi].Dist : 0, plan,
                        PostingValue.F(PostingValue.Suspended), Feature.MsgAct((int)GestureAct.Refuse), who);
                    return;
                }
            }
        }

        /// <summary>As an employer: answer a walk-out of a worker it sees (a better offer, or a dismissal); offer work to a person it sees.</summary>
        private static void EmployerCandidates(ThinkScratch sc, CivMind cm, CivC3Tuning tu)
        {
            int open = 0;
            for (int i = 0; i < cm.Jobs.Count; i++)
            {
                var j = cm.Jobs[i];
                if (j.Side != JobBelief.AsEmployer || j.Status == ContractStatus.Ended) continue;
                open++;
                if (j.Status != ContractStatus.Suspended) continue;
                int pi = PersonIndex(sc, j.Other);
                if (pi < 0) continue;
                var p = sc.People[pi];
                if (!Room(sc, 2)) return;
                int raised = Math.Min(tu.MaxPay, j.PayCount + 1);
                if (Held(sc, j.PayWare) < raised && FirstOutput(j.AssetClass) != j.PayWare) raised = j.PayCount;
                var plan = new C3Plan { Act = EmploymentAct.Offer, Contract = j.Contract, Asset = j.Asset, Ware = j.PayWare, Count = (byte)raised };
                AddPlanned(sc, Employment.VerbHire, Employment.IntentOffer, IntentTarget.Npc, p.Id, p.Node, 0, p.Dist, plan,
                    PostingValue.F(PostingValue.Suspended), Feature.MsgAct((int)GestureAct.Offer), Feature.NpcRef(p.As), Feature.Make(FeatureKind.Role, RoleValue.Employer));
                plan.Act = EmploymentAct.Dismiss;
                AddPlanned(sc, Employment.VerbHire, Employment.IntentDismiss, IntentTarget.Building, j.Asset, j.AssetNode, j.AssetClass, sc.Grid.Distance(sc.Self.Node, j.AssetNode), plan,
                    PostingValue.F(PostingValue.Suspended), Feature.MsgAct((int)GestureAct.Refuse), Feature.NpcRef(p.As), Feature.Make(FeatureKind.Role, RoleValue.Employer));
                return;
            }
            if (open >= tu.MaxWorkersPerEmployer) return;
            // an asset it believes its own (or its household's) that work would turn into output
            int best = -1, bestD = int.MaxValue;
            for (int i = 0; i < cm.Own.Count; i++)
            {
                var b = cm.Own[i];
                var own = Permits.Believed(sc, b.BelievedOwner);
                if (!own.Allowed || own.Basis == PermissionBasis.Ownerless) continue;
                var def = AssetTable.OfClass(b.Class);
                if (def == null || def.WorkTicks <= 0 || def.Recipe == null || def.StaffSlots <= 0) continue;
                if (StaffOn(cm, b.Asset) >= def.StaffSlots) continue;
                if (def.Recipe.Inputs.Count > 0 && !StockedNow(sc, b.Asset)) continue; // a workshop is worth staffing only while it has inputs
                int d = sc.Grid.Distance(sc.Self.Node, b.Node);
                if (d > sc.Tuning.RecallNodes || d >= bestD) continue;
                best = i; bestD = d;
            }
            if (best < 0) return;
            var a = cm.Own[best];
            var adef = AssetTable.OfClass(a.Class);
            // a person it sees, awake, not of its household, who has no believed contract with it
            int target = -1;
            for (int k = 0; k < sc.PeopleCount; k++)
            {
                var p = sc.People[k];
                if (p.Asleep || p.SameHousehold || p.As == 0 || p.Stage == LifeStage.Child || p.Body != BodyKind.Person) continue;
                bool known = false;
                foreach (var j in cm.Jobs) if (j.Other == p.Id && j.Side == JobBelief.AsEmployer && j.Status != ContractStatus.Ended) { known = true; break; }
                if (known) continue;
                target = k; break; // nearest first
            }
            if (target < 0) return;
            var who = sc.People[target];
            // the pay: a ware it carries of its own (food first), else the asset's output, one more for each refusal by this person
            int refusals = 0;
            foreach (var j in cm.Jobs) if (j.Side == JobBelief.AsEmployer && j.Other == who.Id) refusals = Math.Max(refusals, j.Refusals);
            int count = Math.Min(tu.MaxPay, tu.BasePay + refusals);
            Ware w = Ware.None;
            for (int i = 0; i < sc.CarryCount && w == Ware.None; i++) { var l = sc.Carry[i]; if (!l.Empty && Wares.IsFood(l.Ware) && Held(sc, l.Ware) >= count) w = l.Ware; }
            for (int i = 0; i < sc.CarryCount && w == Ware.None; i++) { var l = sc.Carry[i]; if (!l.Empty && Held(sc, l.Ware) >= count && l.Ware != Ware.Axe) w = l.Ware; }
            if (w == Ware.None) w = FirstOutput(a.Class);
            if (w == Ware.None) return;
            int band = EmployerBand(sc, tu, adef, w, count, w == FirstOutput(a.Class) ? count : 0);
            if (!Room(sc, 1)) return;
            var plan2 = new C3Plan { Act = EmploymentAct.Offer, Contract = 0, Asset = a.Asset, Ware = w, Count = (byte)count };
            AddPlanned(sc, Employment.VerbHire, Employment.IntentOffer, IntentTarget.Npc, who.Id, who.Node, 0, who.Dist, plan2,
                PostingValue.F(band), Feature.MsgAct((int)GestureAct.Offer), Feature.NpcRef(who.As), Feature.NpcRel((int)Relations.ClassOf(sc, who)), Feature.Make(FeatureKind.Role, RoleValue.Employer));
        }

        private static int StaffOn(CivMind cm, int asset)
        {
            int n = 0;
            foreach (var j in cm.Jobs) if (j.Side == JobBelief.AsEmployer && j.Asset == asset && j.Status != ContractStatus.Ended) n++;
            return n;
        }

        private static bool StockedNow(ThinkScratch sc, int asset)
        {
            for (int i = 0; i < sc.SeenCount; i++)
                if (sc.Seen[i].Kind == IntentTarget.Building && sc.Seen[i].Ref == asset) return sc.Seen[i].State == ObjState.StockHigh || sc.Seen[i].State == ObjState.StockLow;
            return false;
        }
    }
}
