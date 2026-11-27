using System;
using System.Collections.Generic;
using God.Sim.Civ;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C7a healthcare (INSTITUTIONAL_SERVICES 5.1; OWNER: C7). <c>Treat</c> is M4's <c>Tend</c> with the healer's skill and an
    /// optional remedy: a healer walks to the patient, stays within <c>Reach.Touch</c> for <c>TreatTicks</c>, sets the patient's
    /// M4 <c>Life.TendedDay</c> (so every M4 rule of a tended day holds) and leaves a record of the multipliers its skill and
    /// remedy give. The record acts on the patient's actual body through the hook below, never through a faction or
    /// organisation modifier (SVC-I2):
    /// <list type="bullet">
    /// <item>illness: on a tended day M4 multiplies the severity loss by <c>TendSeverityPermille</c>; a treatment replaces that
    /// multiplier by <see cref="SeverityPermille"/> (= M4's at skill 0 without remedy). The difference is credited to Health
    /// one tick before the patient's daily disease step (and, the part that does not fit under the maximum, just after it), so
    /// the net loss and every death check of <c>Disease.Daily</c> are exactly those of the treated multiplier;</item>
    /// <item>injury: recovery is multiplied by <see cref="InjuryPermille"/> for one day after the treatment, through the F1
    /// <c>Bodies.Heal</c> site.</item>
    /// </list>
    /// No M4 file is edited (the C7 spec 8): the hook is C7's slot in <see cref="OrganisationSystem"/>.
    /// </summary>
    public static class Healthcare
    {
        /// <summary>Treat sub-acts (in <c>Candidate.Class</c>).</summary>
        public const byte Ask = 1, Give = 2, Gather = 3, Found = 4;

        private static OrgC7Tuning T(GodState s) => s.Tuning.Orgs.C7;

        // ---------------------------------------------------------------- formulas (SERVICES 5.1; SVC-HEAL-U1/U2)

        /// <summary>The treated severity multiplier: <c>TendSeverityPermille x (1000 - Skill x SkillGain/1000 - Remedy)/1000</c>, floored.</summary>
        public static int SeverityPermille(int tendPermille, int skill, bool remedy, OrgC7Tuning tu)
        {
            int f = 1000 - skill * tu.SkillGain / 1000 - (remedy ? tu.RemedyPermille : 0);
            int m = tendPermille * f / 1000;
            if (skill == 0 && !remedy) return tendPermille; // exactly M4's Tend
            return Math.Max(tu.TreatFloorPermille, m);
        }

        /// <summary>The treated injury recovery multiplier (permille): <c>InjuryBase + Skill x InjuryGain/1000 + Remedy</c>.</summary>
        public static int InjuryPermille(int skill, bool remedy, OrgC7Tuning tu) => tu.InjuryBasePermille + skill * tu.InjuryGain / 1000 + (remedy ? tu.RemedyPermille : 0);

        /// <summary>
        /// Diagnosis (C7-T2): the healer's estimate from what it perceived (the display level it saw, 0-3) and its own skill
        /// only; noise of up to <c>DiagnoseNoise x (1000 - skill)/1000</c> levels either way (draw <c>svc-diagnose</c>).
        /// </summary>
        public static int Diagnose(int seenLevel, int skill, int noise, Rng r)
        {
            int span = noise * (1000 - Math.Max(0, Math.Min(1000, skill))) / 1000;
            int d = span > 0 ? r.NextInt(2 * span + 1) - span : 0;
            return Math.Max(0, Math.Min(3, seenLevel + d));
        }

        // ---------------------------------------------------------------- start culture

        /// <summary>The start culture (scenario): the lowest-id adults of each faction know the practice and the skill; their faction believes them healers.</summary>
        public static void StartGame(GodState s)
        {
            var tu = T(s);
            Education.StartGame(s);
            if (tu.StartHealers <= 0 || s.Population == null) return;
            var healers = new List<Npc>();
            var perFaction = new Dictionary<int, int>();
            foreach (var n in s.Population.Npcs)
            {
                if (!Bodies.IsLivingPerson(n) || n.Mind == null || n.Stage == LifeStage.Child) continue;
                perFaction.TryGetValue((int)n.Faction, out int k);
                if (k >= tu.StartHealers) continue;
                perFaction[(int)n.Faction] = k + 1;
                healers.Add(n);
            }
            foreach (var h in healers)
            {
                MakeHealer(s, h, tu.StartHealerSkill);
                foreach (var n in s.Population.Npcs)
                    if (Bodies.IsLivingPerson(n) && n.Mind != null && n.Id != h.Id && n.Faction == h.Faction) BelieveHealer(s, n, h.Id);
            }
        }

        /// <summary>A person who knows the healing practice (fixtures, the start culture): its mind and skill.</summary>
        public static void MakeHealer(GodState s, Npc n, int skill)
        {
            var m = n.Mind.Orgs ?? (n.Mind.Orgs = Orgs.NewMind(s));
            m.HealthOrCreate().Healer = true;
            m.Practices |= 1 << OrgTemplates.HealerPractice;
            if (skill > 0) Skills.Set(s, n.Id, SkillClass.Medicine, skill, T(s).SkillSlots);
        }

        public static void BelieveHealer(GodState s, Npc n, int healer)
        {
            if (n?.Mind == null) return;
            var m = n.Mind.Orgs ?? (n.Mind.Orgs = Orgs.NewMind(s));
            m.HealthOrCreate().AddHealer(healer);
        }

        // ---------------------------------------------------------------- the patient's request

        /// <summary>The patient gestures <c>NeedHelp</c> (topic: the treatment service) at a believed healer it sees.</summary>
        public static void StartAsk(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            var h = m.Health;
            var healer = s.Population.Find(c.TargetId);
            if (h == null || healer == null || !Bodies.IsLivingPerson(healer)) { OrgActs.Frustrate(s, npc); return; }
            if (h.LastAsk != 0 && s.Tick - h.LastAsk <= T(s).AskCooldownTicks) return; // it goes on waiting (no second gesture)
            h.LastAsk = (int)s.Tick;
            int id = OrgActs.Say(s, ev, npc, GestureAct.NeedHelp, new MsgTopic { Kind = TopicKind.Service, Id = 0, Sub = (int)ServiceKind.Treat }, healer.Id, MsgTime.Now);
            OrgActs.Ensure(s, healer, id);
        }

        /// <summary>
        /// After its gesture the patient waits where it is (its own act, which its next think may continue or drop) until it
        /// has been treated since it asked, or <c>AskWaitTicks</c> pass.
        /// </summary>
        public static void AskTick(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var hs = s.Organisations.Health;
            int ti = hs == null ? -1 : hs.IndexOfTreated(npc.Id);
            bool treated = ti >= 0 && hs.Treated[ti].Tick >= m.Act.StartTick;
            if (treated || s.Tick >= m.Act.StartTick + Math.Max(s.Tuning.Social.GestureTicks, T(s).AskWaitTicks)) OrgActs.End(npc);
        }

        // ---------------------------------------------------------------- the healer's treatment

        /// <summary>The practice the healer runs (it holds a Healer office), or null: an informal treatment.</summary>
        public static Organisation PracticeOf(GodState s, int healer)
        {
            var os = s.Organisations;
            foreach (var org in os.Orgs)
            {
                if (org.Status == OrgStatus.Dissolved) continue;
                foreach (var o in org.Offices) if (o.Holder == healer && o.Kind == OfficeKind.Healer) return org;
            }
            return null;
        }

        /// <summary>
        /// The healer takes the request it knows of for this patient: the request is opened (in the practice's ring, or informal),
        /// accepted, diagnosed from what the healer perceived, a remedy lot chosen from its own carry: <see cref="TreatmentStarted"/>.
        /// </summary>
        public static void StartGive(GodState s, EventBuffer ev, Npc npc, OrgMind m, in Candidate c)
        {
            var tu = T(s);
            var patient = s.Population.Find(c.TargetId);
            int ki = -1;
            for (int i = 0; i < m.KnownCount; i++) if (!m.Known[i].Done && m.Known[i].Kind == ServiceKind.Treat && patient != null && m.Known[i].Requester == patient.Id) { ki = i; break; }
            if (ki < 0 || patient == null || !Bodies.IsLivingPerson(patient) || patient.Life == null || patient.Id == npc.Id) { if (ki >= 0) m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return; }
            var hs = s.Organisations.HealthOrCreate();
            int ti = hs.IndexOfTreatment(npc.Id);
            if (ti >= 0)
            {
                var cur = hs.Active[ti];
                var old = cur.Patient == patient.Id ? patient : s.Population.Find(cur.Patient);
                if (old != null && Bodies.IsLivingPerson(old) && old.Life != null && (old.Life.Illness == 1 || old.Injury > 300000))
                {
                    // one patient at a time: it resumes the case it began (its progress kept)
                    m.Act.Request = cur.Request; m.Act.Target = old.Id;
                    npc.Action.TargetId = old.Id; npc.Action.TargetNode = old.Node;
                    return;
                }
                End(s, ev, hs, ti, TreatmentEnded.NotNeeded);
            }
            var kr = m.Known[ki];
            var org = PracticeOf(s, npc.Id);
            int request = kr.Request;
            if (org != null)
            {
                if (request == 0 || Services.IndexOf(org, request) < 0) request = Services.Open(s, ev, org, ServiceKind.Treat, kr.Requester, SubjectKind.Npc, patient.Id, patient.Node, 0, 0, kr.SeenNeed, npc.Id, npc.Id);
                if (!Services.Accept(s, ev, org, request, npc.Id))
                {
                    // the old request expired or was taken: the healer opens the case again as it sees it now
                    request = Services.Open(s, ev, org, ServiceKind.Treat, kr.Requester, SubjectKind.Npc, patient.Id, patient.Node, 0, 0, kr.SeenNeed, npc.Id, npc.Id);
                    if (!Services.Accept(s, ev, org, request, npc.Id)) { m.Known[ki].Done = true; OrgActs.Frustrate(s, npc); return; }
                }
                OrgRecords.Witness(s, org, npc.Node, RecordKind.Request, request, Ware.None, 1, npc.Id);
            }
            else
            {
                if (request == 0) request = Services.Open(s, ev, null, ServiceKind.Treat, kr.Requester, SubjectKind.Npc, patient.Id, patient.Node, 0, 0, kr.SeenNeed, npc.Id, npc.Id);
                ev?.Add(new ServiceAccepted { Request = request, Org = 0, ProviderNpc = npc.Id });
            }
            m.Known[ki].Request = request;
            int skill = Skills.Of(s, npc.Id, SkillClass.Medicine);
            int seen = Diagnose(kr.SeenNeed, skill, tu.DiagnoseNoise, s.Rng.Fork("svc-diagnose", request, s.Tick));
            int remedy = 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Ware == Ware.Herbs && l.Count > 0) { remedy = l.Id; break; }
            var t = new Treatment
            {
                Request = request, Org = org?.Id ?? 0, Healer = npc.Id, Patient = patient.Id, Skill = skill, RemedyLot = remedy, SeenNeed = seen, StartTick = s.Tick,
                SeverityBefore = patient.Life.Illness == 1 ? patient.Life.Severity : 0, InjuryBefore = patient.Injury, HealthBefore = patient.Health,
            };
            int at = 0; while (at < hs.Active.Count && hs.Active[at].Healer < npc.Id) at++;
            hs.Active.Insert(at, t);
            hs.TreatmentsStarted++;
            m.Act.Org = t.Org; m.Act.Request = request; m.Act.Target = patient.Id;
            ev?.Add(new TreatmentStarted { Request = request, Org = t.Org, Healer = npc.Id, Patient = patient.Id, Skill = skill, RemedyLot = remedy, SeenNeed = seen, Control = ControlOf(s, npc.Id) });
        }

        /// <summary>
        /// The activity: walk to the patient; within <c>Reach.Touch</c> each tick counts (the patient's M4 care counter too);
        /// after <c>TreatTicks</c> the remedy lot is consumed once (<c>ConsumeReason.Treatment</c>), <c>TendedDay</c> is set and the
        /// treated multipliers recorded: <see cref="TreatmentEnded"/>, <see cref="ServiceDelivered"/> (outcome 521), skill gain.
        /// </summary>
        public static void GiveTick(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var hs = s.Organisations.Health;
            int ti = hs == null ? -1 : hs.IndexOfTreatment(npc.Id);
            if (ti < 0) { OrgActs.End(npc); return; }
            var t = hs.Active[ti];
            var patient = s.Population.Find(t.Patient);
            if (patient == null || !Bodies.IsLivingPerson(patient) || patient.Life == null) { End(s, ev, hs, ti, TreatmentEnded.PatientGone); DoneKnown(m, t.Patient); OrgActs.End(npc); return; }
            if (patient.Life.Illness != 1 && patient.Injury <= 300000) { End(s, ev, hs, ti, TreatmentEnded.NotNeeded); DoneKnown(m, t.Patient); OrgActs.End(npc); return; }
            if (!OrgActs.WalkTo(s, npc, patient.Node, Reach.Touch)) { if (npc.Path == null) { End(s, ev, hs, ti, TreatmentEnded.Stopped); OrgActs.Frustrate(s, npc); } return; }
            t.Progress++;
            patient.Life.CareReceived++;
            hs.Active[ti] = t;
            var tu = T(s);
            if (t.Progress < tu.TreatTicks) return;
            // the outcome: the patient's own body (M4's tended day plus the treated multipliers)
            bool remedy = false;
            if (t.RemedyLot != 0 && Goods.Consume(s, ev, Holder.Carry(npc.Id), t.RemedyLot, 1, ConsumeReason.Treatment, npc.Id, t.Request)) { remedy = true; hs.RemediesUsed++; }
            var cal = SimCalendar.Of(s);
            int day = (int)cal.Day(s.Tick);
            patient.Life.TendedDay = day;
            int sev = SeverityPermille(s.Tuning.Households.Care.TendSeverityPermille, t.Skill, remedy, tu);
            int inj = InjuryPermille(t.Skill, remedy, tu);
            int pi = hs.IndexOfTreated(patient.Id);
            var rec = new TreatedPatient { Patient = patient.Id, Healer = npc.Id, Day = day, Tick = s.Tick, SeverityPermille = sev, InjuryPermille = inj, InjuryUntil = s.Tick + cal.TicksPerDay, CreditedDay = -1 };
            if (pi >= 0) { rec.Pending = hs.Treated[pi].Pending; rec.PendingDay = hs.Treated[pi].PendingDay; rec.CreditedDay = hs.Treated[pi].CreditedDay; hs.Treated[pi] = rec; }
            else hs.Treated.Insert(~pi, rec);
            hs.TreatmentsCompleted++;
            int ticks = (int)Math.Max(1, s.Tick - t.StartTick);
            hs.Active.RemoveAt(ti);
            ev?.Add(new TreatmentEnded
            {
                Request = t.Request, Healer = npc.Id, Patient = patient.Id, Ticks = ticks, SeverityBefore = t.SeverityBefore, SeverityAfter = patient.Life.Illness == 1 ? patient.Life.Severity : 0,
                InjuryBefore = t.InjuryBefore, InjuryAfter = patient.Injury, HealthBefore = t.HealthBefore, HealthAfter = patient.Health, Outcome = TreatmentEnded.Completed,
                SeverityPermille = sev, InjuryPermille = inj, Remedy = remedy,
            });
            var org = t.Org != 0 ? Orgs.Find(s, t.Org) : null;
            if (org != null)
            {
                Services.Delivered(s, ev, org, t.Request, npc.Id, ticks, remedy ? 1 : 0, 521);
                OrgRecords.Witness(s, org, npc.Node, RecordKind.Request, t.Request, remedy ? Ware.Herbs : Ware.None, remedy ? 1 : 0, npc.Id);
            }
            else ev?.Add(new ServiceDelivered { Request = t.Request, Org = 0, ProviderNpc = npc.Id, Ticks = ticks, GoodsUsed = remedy ? 1 : 0, OutcomeEvent = 521, Control = ControlOf(s, npc.Id) });
            GainSkill(s, ev, npc.Id, tu.SkillPerTreatment, HealerSkillChanged.Treated);
            var h = m.HealthOrCreate();
            h.Given++;
            if (org == null) h.GivenInformal++;
            DoneKnown(m, patient.Id);
            // the patient was treated by this person: it believes it a healer (its own experience)
            BelieveHealer(s, patient, npc.Id);
            if (npc.Mind != null) Learning.Learner.AddReward(npc.Mind.Store, tu.TreatRewardVp);
            OrgActs.End(npc);
        }

        private static void DoneKnown(OrgMind m, int patient)
        {
            for (int i = 0; i < m.KnownCount; i++) if (m.Known[i].Kind == ServiceKind.Treat && m.Known[i].Requester == patient) m.Known[i].Done = true;
        }

        private static void End(GodState s, EventBuffer ev, HealthEducationState hs, int ti, byte outcome)
        {
            var t = hs.Active[ti];
            hs.Active.RemoveAt(ti);
            var patient = s.Population.Find(t.Patient);
            ev?.Add(new TreatmentEnded
            {
                Request = t.Request, Healer = t.Healer, Patient = t.Patient, Ticks = (int)Math.Max(0, s.Tick - t.StartTick), SeverityBefore = t.SeverityBefore,
                SeverityAfter = patient?.Life != null && patient.Life.Illness == 1 ? patient.Life.Severity : 0, InjuryBefore = t.InjuryBefore, InjuryAfter = patient?.Injury ?? 0,
                HealthBefore = t.HealthBefore, HealthAfter = patient?.Health ?? 0, Outcome = outcome,
            });
            var org = t.Org != 0 ? Orgs.Find(s, t.Org) : null;
            if (org != null) Services.Refuse(s, ev, org, t.Request, t.Healer, outcome == TreatmentEnded.NotNeeded ? ServiceRefused.NotInNeed : ServiceRefused.NoProvider);
        }

        public static void GainSkill(GodState s, EventBuffer ev, int npc, int delta, byte cause)
        {
            int to = Skills.Gain(s, npc, SkillClass.Medicine, delta, Skills.Max, T(s).SkillSlots, out int from);
            if (to != from) ev?.Add(new HealerSkillChanged { Npc = npc, Skill = (int)SkillClass.Medicine, From = from, To = to, Cause = cause });
        }

        // ---------------------------------------------------------------- herbs (R22: at Roots sources)

        /// <summary>The healer walks to a root patch and, after <c>HerbTicks</c>, takes one unit of its stock as herbs (<c>Produced</c>, Foraged).</summary>
        public static void GatherTick(GodState s, EventBuffer ev, Npc npc)
        {
            var tu = T(s);
            var a = npc.Action;
            if (!OrgActs.WalkTo(s, npc, a.TargetNode, Reach.Touch)) { if (npc.Path == null) OrgActs.Frustrate(s, npc); return; }
            ResourceSource src = null;
            foreach (var x in s.Population.Sources) if (x.Node == a.TargetNode && x.Kind == SourceKind.Roots) { src = x; break; }
            if (src == null || src.Stock <= 0 || PopulationModule.Room(s, npc, Ware.Herbs) <= 0) { OrgActs.Frustrate(s, npc); return; }
            if (++npc.Action.Progress < tu.HerbTicks) return;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            src.Stock -= 1;
            if (Goods.Produce(s, ev, Holder.Carry(npc.Id), Ware.Herbs, 1, day + tu.HerbSpoilDays, 0, Owner.Npc(npc.Id), ProduceReason.Foraged, npc.Id, src.Id) == 0) { src.Stock += 1; OrgActs.Frustrate(s, npc); return; }
            s.Organisations.HealthOrCreate().HerbsGathered++;
            OrgActs.End(npc);
        }

        public static int HerbsIn(Npc npc)
        {
            int n = 0;
            foreach (var l in npc.Carry) if (!l.Empty && l.Ware == Ware.Herbs) n += l.Count;
            return n;
        }

        // ---------------------------------------------------------------- the practice (SVC-ACC-4)

        /// <summary>
        /// A healer who has treated enough people informally founds a practice of its own (template <see cref="OrgTemplates.HealerPractice"/>,
        /// private, one founder, the Healer office): an ordinary <see cref="Orgs.Found"/> at its own place, open to anyone.
        /// </summary>
        public static void FoundPractice(GodState s, EventBuffer ev, Npc npc, OrgMind m)
        {
            var tu = T(s);
            var h = m.Health;
            if (s.Tuning.Orgs.Emergence == 0 || h == null || !h.Healer || h.GivenInformal < tu.FoundAfterTreatments || PracticeOf(s, npc.Id) != null
                || Skills.Of(s, npc.Id, SkillClass.Medicine) < tu.FoundMinSkill) { OrgActs.Frustrate(s, npc); return; }
            var org = Orgs.Found(s, ev, OrgTemplates.HealerPractice, new List<int> { npc.Id }, npc.Node, OrgTemplates.HealerPractice);
            if (org == null) { OrgActs.Frustrate(s, npc); return; }
            Orgs.SetPolicy(s, ev, org, OrgPolicyKey.Eligibility, Eligibility.Anyone, 0);
            h.Practice = org.Id;
            for (int i = 0; i < m.KnownCount; i++) if (m.Known[i].Kind == ServiceKind.Treat && !m.Known[i].Done) m.Known[i].Org = org.Id;
            OrgActs.End(npc);
        }

        // ---------------------------------------------------------------- the hook into the M4 path (OrganisationSystem's C7 slot)

        /// <summary>
        /// Per tick, after the M4 and M3 systems of this tick (order 260): the treated records act on their patients' bodies.
        /// Injury: extra recovery <c>base x (InjuryPermille - 1000)/1000</c> per tick until <c>InjuryUntil</c>. Illness: the credit
        /// around the patient's daily disease step (see the class summary).
        /// </summary>
        public static void Tick(GodState s, EventBuffer ev)
        {
            OpenPerceived(s, ev);
            var hs = s.Organisations.Health;
            if (hs == null || hs.Treated.Count == 0) return;
            var p = s.Population;
            var cal = SimCalendar.Of(s);
            int tpd = cal.TicksPerDay;
            int day = (int)cal.Day(s.Tick);
            long next = s.Tick + 1;
            int nextDay = (int)cal.Day(next);
            int nextTod = cal.TickOfDay(next);
            int slotLen = Math.Max(1, tpd / 8);
            int baseHeal = (int)((long)s.Tuning.Population.InjuryHealPerDay * 1000 / tpd);
            bool lifeOn = s.Households != null && s.Tuning.Households.Enabled != 0;
            for (int i = 0; i < hs.Treated.Count; i++)
            {
                var r = hs.Treated[i];
                var n = p.Find(r.Patient);
                if (n == null || !Bodies.IsLivingPerson(n)) continue;
                bool changed = false;
                // injury
                if (s.Tick < r.InjuryUntil && n.Injury > 0 && r.InjuryPermille > 1000)
                {
                    int extra = (int)((long)baseHeal * (r.InjuryPermille - 1000) / 1000);
                    int before = n.Injury;
                    Bodies.Heal(s, ev, n, HealKind.Injury, extra, 0);
                    hs.InjuryHealed += before - n.Injury;
                }
                var life = n.Life;
                if (lifeOn && life != null)
                {
                    // the part of yesterday's credit that did not fit, after the disease step of its day
                    if (r.Pending > 0 && r.PendingDay == day && life.LastDaily == day)
                    {
                        int add = Math.Min(r.Pending, 1000000 - n.Health);
                        if (add > 0) { n.Health += add; hs.HealthCredited += add; }
                        r.Pending = 0; changed = true;
                    }
                    // the credit ahead of the patient's disease step in the next tick (LifeCycleSystem runs first in a tick)
                    int slot = 1 + (n.Id % 8) * slotLen;
                    if (nextTod == slot && life.LastDaily != nextDay && r.CreditedDay != nextDay && life.Illness == 1
                        && life.TendedDay >= nextDay - 1 && r.Day >= nextDay - 1 && r.Day <= nextDay)
                    {
                        var dt = s.Tuning.Households.Disease;
                        int baseLoss = life.Severity * dt.SeverityScalePermille / 1000;
                        int lossTend = baseLoss * s.Tuning.Households.Care.TendSeverityPermille / 1000;
                        int lossTreat = baseLoss * r.SeverityPermille / 1000;
                        int delta = (lossTend - lossTreat) * 1000;
                        if (delta > 0)
                        {
                            int credit = Math.Min(delta, Math.Max(0, 1000000 - n.Health));
                            n.Health += credit;
                            hs.HealthCredited += credit;
                            r.Pending = delta - credit; r.PendingDay = nextDay;
                        }
                        r.CreditedDay = nextDay; changed = true;
                    }
                }
                if (changed) hs.Treated[i] = r;
            }
        }

        /// <summary>
        /// A practice's healer writes the requests it perceived into the practice's ring (it is the role-holder who received
        /// them), so the practice's queue and its unmet demand (expired, refused) are measured as SERVICES 3.4 asks.
        /// </summary>
        private static void OpenPerceived(GodState s, EventBuffer ev)
        {
            var p = s.Population;
            foreach (var org in s.Organisations.Orgs)
            {
                if (org.Status == OrgStatus.Dissolved || org.Template != OrgTemplates.HealerPractice && org.Template != OrgTemplates.HealersHouse) continue;
                foreach (var o in org.Offices)
                {
                    if (o.Holder == 0 || o.Kind != OfficeKind.Healer) continue;
                    var k = p.Find(o.Holder);
                    var m = k?.Mind?.Orgs;
                    if (m == null || !Bodies.IsLivingPerson(k)) continue;
                    for (int i = 0; i < m.KnownCount; i++)
                    {
                        var kr = m.Known[i];
                        if (kr.Kind != ServiceKind.Treat || kr.Request != 0 || kr.Done) continue;
                        var subject = p.Find(kr.Requester);
                        m.Known[i].Org = org.Id;
                        m.Known[i].Request = Services.Open(s, ev, org, ServiceKind.Treat, kr.Requester, SubjectKind.Npc, kr.Requester, subject?.Node ?? org.Node, 0, 0, kr.SeenNeed, k.Id, 0);
                        OrgRecords.Witness(s, org, k.Node, RecordKind.Request, m.Known[i].Request, Ware.None, 1, k.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Day start: a healer whose patient recovered (illness over, injury below the display threshold) within
        /// <c>RecoveryDays</c> of the treatment gains extra skill; records past their effects are pruned.
        /// </summary>
        public static void DayStart(GodState s, EventBuffer ev, int day)
        {
            var hs = s.Organisations.Health;
            if (hs == null) return;
            var tu = T(s);
            var p = s.Population;
            for (int i = hs.Treated.Count - 1; i >= 0; i--)
            {
                var r = hs.Treated[i];
                var n = p.Find(r.Patient);
                bool alive = n != null && Bodies.IsLivingPerson(n);
                if (!r.RecoveryCredited && alive && (n.Life == null || n.Life.Illness != 1) && n.Injury <= 300000 && day - r.Day <= tu.RecoveryDays)
                {
                    r.RecoveryCredited = true; hs.Treated[i] = r;
                    var healer = p.Find(r.Healer);
                    if (healer != null && Bodies.IsLivingPerson(healer)) GainSkill(s, ev, r.Healer, tu.SkillPerRecovery, HealerSkillChanged.Recovered);
                }
                if (!alive || day - r.Day > Math.Max(2, tu.RecoveryDays) && s.Tick >= r.InjuryUntil && r.Pending == 0) hs.Treated.RemoveAt(i);
            }
        }

        /// <summary>A person died: its treatments (as healer or patient) stop; its skills go with it.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var hs = s.Organisations.Health;
            if (hs != null)
                for (int i = hs.Active.Count - 1; i >= 0; i--)
                    if (hs.Active[i].Healer == npc.Id || hs.Active[i].Patient == npc.Id) End(s, ev, hs, i, hs.Active[i].Healer == npc.Id ? TreatmentEnded.Stopped : TreatmentEnded.PatientGone);
            Skills.Forget(s, npc.Id);
        }

        public static void Check(GodState s, List<string> bad)
        {
            var hs = s.Organisations?.Health;
            Skills.Check(s, bad);
            if (hs == null) return;
            int prev = 0;
            foreach (var t in hs.Active) { if (t.Healer <= prev) bad.Add("C7-1: treatments not ascending by healer at " + t.Healer); prev = t.Healer; }
            prev = 0;
            foreach (var t in hs.Treated) { if (t.Patient <= prev) bad.Add("C7-2: treated records not ascending at " + t.Patient); prev = t.Patient; }
            prev = 0;
            foreach (var t in hs.Sessions) { if (t.Teacher <= prev) bad.Add("C7-3: sessions not ascending at " + t.Teacher); prev = t.Teacher; }
        }

        private static byte ControlOf(GodState s, int npc) => s.Divinity != null ? (byte)Divinity.Control.Of(s, npc) : (byte)0;
    }
}
