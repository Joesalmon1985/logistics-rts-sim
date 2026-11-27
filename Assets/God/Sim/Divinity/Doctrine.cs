using System;
using System.Collections.Generic;
using God.Sim.Comms;
using God.Sim.Culture;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 13 (COMMANDMENTS 4.3-4.5): commandments as the gods' records and as people's knowledge. A commandment is
    /// revealed to recipients (perceived, copied with misunderstanding), told person to person with the speaker's own
    /// interpretation, decays and is forgotten, adopted or rejected. <b>Nothing here refuses, blocks or forces an action</b>:
    /// a held interpretation only adds a term and a feature key to the person's own candidates (the mind hooks). OWNER: D6.
    /// </summary>
    public static class Doctrine
    {
        /// <summary>The telling verb (Divinity learning row 120): see the D6 record for why it is not M5's Say(Teach) 82.</summary>
        public const byte TellVerb = 120;

        internal static CommandResult Reject(string code) => CommandResult.Reject(code, DivineReasons.Sentence(code));

        /// <summary>The doctrine state, created on first use (absent before: pin P2).</summary>
        public static DoctrineState StateOf(GodState s) => s.Divinity.Doctrine ?? (s.Divinity.Doctrine = new DoctrineState());

        /// <summary>The person's divine mind, created on first use, with the tunings bound and its community set.</summary>
        public static DivineMind MindOf(GodState s, Npc npc)
        {
            if (npc.Mind == null) return null;
            var m = npc.Mind.Divine ?? (npc.Mind.Divine = new DivineMind());
            m.Tuning = s.Tuning.Divinity.Instruction;
            m.DTuning = s.Tuning.Divinity.Doctrine;
            if (m.Community == 0) m.Community = (int)npc.Faction + 1;
            return m;
        }

        /// <summary>Binds every divine mind to the doctrine tuning (after a load).</summary>
        internal static void BindTuning(GodState s)
        {
            if (s.Population == null) return;
            foreach (var n in s.Population.Npcs) if (Bodies.IsPerson(n) && n.Mind?.Divine != null) n.Mind.Divine.DTuning = s.Tuning.Divinity.Doctrine;
        }

        // ---------------- recipients ----------------

        /// <summary>
        /// The people a revelation reaches, ascending NpcId: a person (with <c>Radius</c> &gt; 0 also every living person within
        /// that many nodes of it: "the believers gathered around"), a household's members, the people within a location's radius,
        /// or a community's members (the faction stand-in until C6). Possessed bodies and avatars are not reached. Null when
        /// valid, else the refusal code.
        /// </summary>
        public static string Resolve(GodState s, in DivineTarget t, List<int> into)
        {
            into.Clear();
            var p = s.Population;
            var g = s.Territory.Grid;
            switch (t.Kind)
            {
                case DivineTargetKind.Npc:
                {
                    var n = p.Find(t.Id);
                    if (n == null) return DivineReasons.BadTarget;
                    if (!Bodies.IsPerson(n)) return DivineReasons.NotAPerson;
                    if (!n.Alive) return DivineReasons.TargetDead;
                    if (t.Radius < 0) return DivineReasons.BadTarget;
                    if (t.Radius == 0) { if (Reachable(s, n)) into.Add(n.Id); break; }
                    foreach (var o in p.Npcs) if (Bodies.IsLivingPerson(o) && g.Distance(o.Node, n.Node) <= t.Radius && Reachable(s, o)) into.Add(o.Id);
                    break;
                }
                case DivineTargetKind.Household:
                    if (t.Id == 0) return DivineReasons.BadTarget;
                    foreach (var n in p.Npcs) if (n.Household == t.Id && Bodies.IsLivingPerson(n) && Reachable(s, n)) into.Add(n.Id);
                    break;
                case DivineTargetKind.Location:
                    if (t.Node < 0 || t.Node >= g.Count || !g.IsLand(t.Node) || t.Radius < 0) return DivineReasons.BadTarget;
                    foreach (var n in p.Npcs) if (Bodies.IsLivingPerson(n) && g.Distance(n.Node, t.Node) <= t.Radius && Reachable(s, n)) into.Add(n.Id);
                    break;
                case DivineTargetKind.Org:
                    if (t.Id < 0 || t.Id >= s.FactionCount) return DivineReasons.BadTarget;
                    foreach (var n in p.Npcs) if (Bodies.IsLivingPerson(n) && (int)n.Faction == t.Id && Reachable(s, n)) into.Add(n.Id);
                    break;
                default:
                    return DivineReasons.BadTarget;
            }
            into.Sort();
            if (into.Count == 0) return DivineReasons.BadTarget;
            if (into.Count > s.Tuning.Divinity.Doctrine.MaxRecipients) return DivineReasons.BadTarget;
            return null;
        }

        private static bool Reachable(GodState s, Npc n)
        {
            var k = Control.Of(s, n.Id);
            return n.Mind != null && k != ControlKind.Possessed && k != ControlKind.AvatarBody;
        }

        // ---------------- reveal, revise, revoke (4.3) ----------------

        internal static CommandResult ValidateReveal(GodState s, RevealCommandment c, List<int> rec)
        {
            if (s.Divinity == null) return Reject(DivineReasons.DivinityOff);
            string why = CommandmentRules.Validate(c.Rules, c.Importance, c.Template);
            if (why != null) return Reject(why);
            why = Resolve(s, c.Recipients, rec);
            if (why != null) return Reject(why);
            return Mythic.CanSpend(s, c.God, InterventionKind.Commandment, s.Tuning.Divinity.Mythic.Costs.RevealCommandment);
        }

        internal static CommandResult ApplyReveal(GodState s, EventBuffer ev, RevealCommandment c, List<int> rec)
        {
            var r = ValidateReveal(s, c, rec);
            if (!r.Ok) return r;
            long cost = s.Tuning.Divinity.Mythic.Costs.RevealCommandment;
            r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Commandment, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            var st = StateOf(s);
            int cid = DivinityIds.Take(ref st.NextCommandmentId);
            int iv = Interventions.Open(s, ev, c.God, InterventionKind.Commandment, DivineAct.None, new DivineTarget(DivineTargetKind.Commandment, cid), Scope(c.Recipients), cost);
            var cm = new Commandment
            {
                Id = cid, God = c.God, Revision = 1, Template = c.Template, Rules = (CommandmentRule[])c.Rules.Clone(), Importance = c.Importance,
                IntroducedTick = s.Tick, Recipients = c.Recipients, Intervention = iv, LastIntervention = iv,
            };
            st.Commandments.Add(cm);
            var n0 = c.Recipients.Kind == DivineTargetKind.Npc ? s.Population.Find(c.Recipients.Id) : null;
            ev.Add(new CommandmentIssued
            {
                Intervention = iv, Commandment = cid, God = c.God, Template = c.Template, Importance = c.Importance, Rules = cm.Rules.Length,
                TargetKind = c.Recipients.Kind, TargetId = c.Recipients.Id, TargetNode = c.Recipients.Node, TargetRadius = c.Recipients.Radius,
                Recipients = rec.Count, Npc = n0?.Id ?? 0, Node = n0?.Node ?? c.Recipients.Node,
            });
            Reveal(s, ev, cm, rec, iv, false);
            return CommandResult.Accept();
        }

        private static DivineScope Scope(in DivineTarget t)
        {
            switch (t.Kind)
            {
                case DivineTargetKind.Household: return DivineScope.Household;
                case DivineTargetKind.Location: return DivineScope.Location;
                case DivineTargetKind.Org: return DivineScope.Org;
                default: return t.Radius > 0 ? DivineScope.Location : DivineScope.Individual;
            }
        }

        internal static CommandResult ValidateRevise(GodState s, ReviseCommandment c, List<int> rec)
        {
            if (s.Divinity == null) return Reject(DivineReasons.DivinityOff);
            var cm = s.Divinity.Doctrine?.Find(c.Commandment);
            if (cm == null || cm.God != c.God || cm.Revoked) return Reject(DivineReasons.BadTarget);
            string why = CommandmentRules.Validate(c.Rules, c.Importance, 0);
            if (why != null) return Reject(why);
            ResolveAgain(s, cm, rec);
            return Mythic.CanSpend(s, c.God, InterventionKind.Commandment, s.Tuning.Divinity.Mythic.Costs.ReviseCommandment);
        }

        /// <summary>The recipients of a revision or revocation: the revelation's target, re-resolved now (possibly nobody: still accepted).</summary>
        private static void ResolveAgain(GodState s, Commandment cm, List<int> rec)
        {
            if (Resolve(s, cm.Recipients, rec) != null) rec.Clear();
        }

        internal static CommandResult ApplyRevise(GodState s, EventBuffer ev, ReviseCommandment c, List<int> rec)
        {
            var r = ValidateRevise(s, c, rec);
            if (!r.Ok) return r;
            long cost = s.Tuning.Divinity.Mythic.Costs.ReviseCommandment;
            r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Commandment, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            var cm = s.Divinity.Doctrine.Find(c.Commandment);
            int iv = Interventions.Open(s, ev, c.God, InterventionKind.Commandment, DivineAct.None, new DivineTarget(DivineTargetKind.Commandment, cm.Id), Scope(cm.Recipients), cost);
            cm.History.Add(new CommandmentRevision { Revision = cm.Revision, Rules = cm.Rules, Importance = cm.Importance, EndedTick = s.Tick, Intervention = cm.LastIntervention });
            cm.Revision++;
            cm.Rules = (CommandmentRule[])c.Rules.Clone();
            cm.Importance = c.Importance;
            cm.LastIntervention = iv;
            ev.Add(new CommandmentRevised { Intervention = iv, Commandment = cm.Id, God = cm.God, Revision = cm.Revision, Importance = cm.Importance, Rules = cm.Rules.Length, Recipients = rec.Count });
            Reveal(s, ev, cm, rec, iv, false);
            return CommandResult.Accept();
        }

        internal static CommandResult ValidateRevoke(GodState s, RevokeCommandment c, List<int> rec)
        {
            if (s.Divinity == null) return Reject(DivineReasons.DivinityOff);
            var cm = s.Divinity.Doctrine?.Find(c.Commandment);
            if (cm == null || cm.God != c.God || cm.Revoked) return Reject(DivineReasons.BadTarget);
            ResolveAgain(s, cm, rec);
            return Mythic.CanSpend(s, c.God, InterventionKind.Commandment, s.Tuning.Divinity.Mythic.Costs.RevokeCommandment);
        }

        internal static CommandResult ApplyRevoke(GodState s, EventBuffer ev, RevokeCommandment c, List<int> rec)
        {
            var r = ValidateRevoke(s, c, rec);
            if (!r.Ok) return r;
            long cost = s.Tuning.Divinity.Mythic.Costs.RevokeCommandment;
            r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Commandment, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            var cm = s.Divinity.Doctrine.Find(c.Commandment);
            int iv = Interventions.Open(s, ev, c.God, InterventionKind.Commandment, DivineAct.None, new DivineTarget(DivineTargetKind.Commandment, cm.Id), Scope(cm.Recipients), cost);
            cm.Revoked = true;
            cm.History.Add(new CommandmentRevision { Revision = cm.Revision, Rules = cm.Rules, Importance = cm.Importance, EndedTick = s.Tick, Revoked = true, Intervention = iv });
            cm.LastIntervention = iv;
            ev.Add(new CommandmentRevoked { Intervention = iv, Commandment = cm.Id, God = cm.God, Revision = cm.Revision, Recipients = rec.Count });
            Reveal(s, ev, cm, rec, iv, true);
            return CommandResult.Accept();
        }

        /// <summary>
        /// Delivery through the divine-message path (DIVINE_PLAYER 4.2, D2's model): each recipient perceives it by its own draw
        /// <c>commandment-perceive(npc, intervention)</c> (an asleep one as a dream), copies each rule with the revelation's blur
        /// (scaled by its disbelief), and gains or updates its entry; people within gesture range who attend a recipient hear it
        /// too at half salience.
        /// </summary>
        private static void Reveal(GodState s, EventBuffer ev, Commandment cm, List<int> rec, int iv, bool revoke)
        {
            var tu = s.Tuning.Divinity.Doctrine;
            var p = s.Population;
            var g = s.Territory.Grid;
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            int sight = CommsRange.Gesture(s.Tuning.Population, night);
            List<int> witnesses = null;
            foreach (int id in rec)
            {
                var n = p.Find(id);
                var draw = s.Rng.Fork("commandment-perceive", n.Id, iv);
                if (draw.NextInt(1000) >= tu.RevealPerceive) continue;
                var m = MindOf(s, n);
                Beliefs.Read(m, cm.God, out int belief, out _, out _, out _, out int loyalty);
                int blur = tu.RevealBlurPermille * (1000 - Fx.Clamp(belief, 0, 1000)) / 1000;
                var inc = Copy(s, cm, n.Id, iv, blur, "commandment-interpret");
                inc.Source = CommandmentSource.Revealed;
                inc.ToldRevoked = revoke;
                int sal = tu.RevealSalienceBase + Math.Max(0, belief) / 2;
                int adopt = belief * loyalty / 1000 - tu.RevealAdoptionOffset;
                Expose(s, ev, n, m, inc, sal, adopt, true, iv);
                if (tu.Witnesses == 0 || n.Action.Asleep) continue;
                foreach (var w in p.Npcs)
                {
                    if (!Bodies.IsLivingPerson(w) || w.Id == n.Id || w.Action.Asleep || w.Mind == null || rec.BinarySearch(w.Id) >= 0) continue;
                    int d = g.Distance(w.Node, n.Node);
                    if (d > sight || !Messages.Attends(p, g, w, n, d, sight, s.Tuning.Comms.AttendPeople)) continue;
                    if (witnesses == null) witnesses = new List<int>();
                    if (!witnesses.Contains(w.Id)) witnesses.Add(w.Id);
                }
            }
            if (witnesses == null) return;
            witnesses.Sort();
            foreach (int id in witnesses)
            {
                var w = p.Find(id);
                var m = MindOf(s, w);
                Beliefs.Read(m, cm.God, out int belief, out _, out _, out _, out int loyalty);
                int blur = tu.RevealBlurPermille * (1000 - Fx.Clamp(belief, 0, 1000)) / 1000;
                var inc = Copy(s, cm, w.Id, iv, blur, "commandment-witness");
                inc.Source = CommandmentSource.Witnessed;
                inc.FromNpc = 0;
                inc.ToldRevoked = revoke;
                int sal = (tu.RevealSalienceBase + Math.Max(0, belief) / 2) / 2;
                int adopt = (belief * loyalty / 1000 - tu.RevealAdoptionOffset) / 2;
                Expose(s, ev, w, m, inc, sal, adopt, true, iv);
            }
        }

        /// <summary>A known entry carrying the commandment's current revision, each rule copied with the chance <paramref name="chance"/> of one field altered.</summary>
        private static KnownCommandment Copy(GodState s, Commandment cm, int npc, int key, int chance, string label)
        {
            var k = new KnownCommandment
            {
                Commandment = cm.Id, Revision = cm.Revision, God = cm.God, Importance = (byte)cm.Importance, RuleCount = (byte)Math.Min(CommandmentRules.MaxRules, cm.Rules.Length),
                Interpretation = cm.Rules[0], Interpretation2 = cm.Rules.Length > 1 ? cm.Rules[1] : default,
            };
            var rng = s.Rng.Fork(label, npc, ((long)key << 16) | (uint)cm.Revision);
            Garble(ref k, ref rng, chance);
            return k;
        }

        /// <summary>COMMANDMENTS 4.5: each rule is altered (one field) with the chance; returns the count altered.</summary>
        public static int Garble(ref KnownCommandment k, ref Rng rng, int chancePermille)
        {
            int n = 0;
            for (int i = 0; i < k.RuleCount; i++)
            {
                if (rng.NextInt(1000) >= chancePermille) continue;
                var r = k.Rule(i);
                if (!Alter(ref r, rng.NextInt(5))) continue;
                if (i == 0) k.Interpretation = r; else k.Interpretation2 = r;
                n++;
            }
            return n;
        }

        /// <summary>
        /// One misunderstanding operator, starting at <paramref name="op"/> and moving to the next when it does not apply: drop
        /// the condition; replace the verb by another of the same group; widen the object to any; Required to Encouraged or
        /// the reverse; widen a radius scope by one node. False only when none applies.
        /// </summary>
        public static bool Alter(ref CommandmentRule r, int op)
        {
            for (int k = 0; k < 5; k++)
            {
                switch ((op + k) % 5)
                {
                    case 0:
                        if (r.Condition == ConditionKind.Always && (r.ConditionParam & 0xFF) == 0) break;
                        r.Condition = ConditionKind.Always; r.ConditionParam &= ~0xFF; return true;
                    case 1:
                    {
                        byte v = SameGroup(r.LearningVerb);
                        if (v == 0) break;
                        r.LearningVerb = v; return true;
                    }
                    case 2:
                        if (r.ObjClass != 0) { r.ObjClass = 0; return true; }
                        if (r.Ware != Ware.None) { r.Ware = Ware.None; return true; }
                        break;
                    case 3:
                        if (r.Modality == Modality.Required) { r.Modality = Modality.Encouraged; return true; }
                        if (r.Modality == Modality.Encouraged) { r.Modality = Modality.Required; return true; }
                        break;
                    case 4:
                        if (r.Scope.Kind == DivineTargetKind.Location) { r.Scope.Radius++; return true; }
                        break;
                }
            }
            return false;
        }

        /// <summary>The next verb of the verb table (cyclic, table order) in the same group as <paramref name="v"/> (0 none).</summary>
        public static byte SameGroup(byte v)
        {
            if (!VerbTable.TryGet(v, out var info)) return 0;
            var rows = VerbTable.Rows;
            int at = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Id == v) { at = i; break; }
            for (int k = 1; k < rows.Count; k++)
            {
                var row = rows[(at + k) % rows.Count];
                if (row.Group == info.Group && row.Id != v && row.Id != TellVerb) return row.Id;
            }
            return 0;
        }

        /// <summary>Analytics truth: which fields of a held entry differ from the revision it carries as issued.</summary>
        public static int VariantMask(GodState s, in KnownCommandment k)
        {
            var cm = s.Divinity?.Doctrine?.Find(k.Commandment);
            if (cm == null) return 0;
            var rules = cm.Revision == k.Revision ? cm.Rules : null;
            if (rules == null) foreach (var h in cm.History) if (h.Revision == k.Revision) { rules = h.Rules; break; }
            if (rules == null) return 0;
            int mask = 0;
            for (int i = 0; i < k.RuleCount && i < rules.Length; i++)
            {
                var a = k.Rule(i); var b = rules[i];
                if (a.Modality != b.Modality) mask |= 1;
                if (a.LearningVerb != b.LearningVerb) mask |= 2;
                if (a.ObjClass != b.ObjClass || a.Ware != b.Ware) mask |= 4;
                if (a.Applies != b.Applies) mask |= 8;
                if (a.Condition != b.Condition || a.ConditionParam != b.ConditionParam) mask |= 16;
                if (a.Scope.Kind != b.Scope.Kind || a.Scope.Id != b.Scope.Id || a.Scope.Node != b.Scope.Node || a.Scope.Radius != b.Scope.Radius) mask |= 32;
            }
            return mask;
        }

        // ---------------- knowledge (4.5) ----------------

        /// <summary>
        /// One exposure of a person to an interpretation: a new entry (salience <paramref name="salience"/>, adoption
        /// <paramref name="adoption"/>), or, for one it holds, more salience, the adoption moved by the exposure, a newer
        /// revision replacing an older one, and a told revocation moving it towards rejection. Emits 863/864/865.
        /// </summary>
        public static void Expose(GodState s, EventBuffer ev, Npc n, DivineMind m, KnownCommandment inc, int salience, int adoption, bool setAdoption, int intervention)
        {
            var tu = s.Tuning.Divinity.Doctrine;
            var st = StateOf(s);
            int i = m.IndexOfKnown(inc.Commandment);
            if (i < 0)
            {
                inc.Salience = (short)Fx.Clamp(salience, 1, 1000);
                inc.Adoption = (short)Fx.Clamp(adoption + (inc.ToldRevoked ? tu.RevokeShift : 0), -1000, 1000);
                inc.LearnedTick = s.Tick;
                inc.VariantMask = (byte)VariantMask(s, inc);
                inc.Stance = CommandmentStance.None;
                int at = m.AddKnown(inc, out var evicted);
                if (evicted.Commandment != 0) { st.Forgotten++; ev.Add(new CommandmentForgotten { Npc = n.Id, Commandment = evicted.Commandment, Revision = evicted.Revision, Evicted = true }); }
                st.Learned++;
                ev.Add(new CommandmentLearned
                {
                    Npc = n.Id, Commandment = inc.Commandment, Revision = inc.Revision, God = inc.God, Source = inc.Source, FromNpc = inc.FromNpc,
                    Salience = inc.Salience, Adoption = inc.Adoption, Intervention = intervention, VariantMask = inc.VariantMask,
                });
                Restance(s, ev, n, m, at, 5);
                return;
            }
            ref var k = ref m.Known[i];
            bool update = false;
            if (inc.Revision > k.Revision)
            {
                k.Revision = inc.Revision; k.Interpretation = inc.Interpretation; k.Interpretation2 = inc.Interpretation2; k.RuleCount = inc.RuleCount;
                k.Importance = inc.Importance; k.Source = inc.Source; k.FromNpc = inc.FromNpc;
                k.VariantMask = (byte)VariantMask(s, k);
                update = true;
            }
            k.Salience = (short)Fx.Clamp(k.Salience + salience, 0, 1000);
            int a = setAdoption ? Math.Max(k.Adoption, adoption) : k.Adoption + adoption;
            if (inc.ToldRevoked && !k.ToldRevoked) { k.ToldRevoked = true; a = k.Adoption + tu.RevokeShift; }
            k.Adoption = (short)Fx.Clamp(a, -1000, 1000);
            if (update)
            {
                st.Learned++;
                ev.Add(new CommandmentLearned
                {
                    Npc = n.Id, Commandment = k.Commandment, Revision = k.Revision, God = k.God, Source = k.Source, FromNpc = k.FromNpc,
                    Salience = k.Salience, Adoption = k.Adoption, Intervention = intervention, Update = true, VariantMask = k.VariantMask,
                });
            }
            Restance(s, ev, n, m, i, inc.ToldRevoked ? 4 : 2);
        }

        /// <summary>The state band of an entry (COMMANDMENTS 4.5).</summary>
        public static CommandmentStance StanceOf(DoctrineTuning tu, in KnownCommandment k)
        {
            if (k.Adoption <= tu.RejectBelow) return CommandmentStance.Rejected;
            if (k.Salience < tu.HeardBelow) return CommandmentStance.Heard;
            if (k.Adoption >= tu.AdoptFrom) return CommandmentStance.Adopted;
            return CommandmentStance.Learned;
        }

        private static void Restance(GodState s, EventBuffer ev, Npc n, DivineMind m, int i, int cause)
        {
            ref var k = ref m.Known[i];
            var now = StanceOf(s.Tuning.Divinity.Doctrine, k);
            if (now == k.Stance) return;
            ev.Add(new CommandmentStanceChanged { Npc = n.Id, Commandment = k.Commandment, From = k.Stance, To = now, Adoption = k.Adoption, Salience = k.Salience, Cause = cause });
            k.Stance = now;
            StateOf(s).StanceChanges++;
        }

        // ---------------- telling (4.5 "Telling") ----------------

        /// <summary>The entry a person tells (highest adoption x salience among those it may tell; ties by id), -1 none.</summary>
        public static int Tellable(DoctrineTuning tu, DivineMind m)
        {
            int best = -1; long bv = 0;
            for (int i = 0; i < m.KnownCount; i++)
            {
                var k = m.Known[i];
                if (k.Salience < tu.TellMinSalience || k.Adoption <= 0 || k.Adoption <= tu.RejectBelow) continue;
                long v = (long)k.Adoption * k.Salience;
                if (best < 0 || v > bv || v == bv && k.Commandment < m.Known[best].Commandment) { best = i; bv = v; }
            }
            return best;
        }

        /// <summary>
        /// A person starts telling (the TellCommandment intent): one <see cref="GestureAct.Teach"/> gesture about the commandment
        /// (M5's <c>Messages.Emit</c>, so it is seen and decoded like any gesture), and every listener who perceives it (the
        /// addressee and those who attend the speaker within gesture range, as M5's rule) gets the speaker's interpretation
        /// copied with M5's field-error chance (distance, darkness) through <see cref="TellTo"/>.
        /// </summary>
        public static void StartTell(GodState s, EventBuffer ev, Npc speaker, int target)
        {
            var m = speaker.Mind?.Divine;
            var tu = s.Tuning.Divinity.Doctrine;
            m.LastTellTick = s.Tick;
            m.LastTold = target;
            int i = Tellable(tu, m);
            if (i < 0) return;
            var k = m.Known[i];
            StateOf(s).Told++;
            if (s.Social != null)
                Messages.Emit(s, ev, speaker, Messages.Compose(GestureAct.Teach, new MsgTopic { Kind = TopicKind.Commandment, Id = k.Commandment, Sub = k.Revision }, target: target));
            var p = s.Population;
            var g = s.Territory.Grid;
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            int sight = CommsRange.Gesture(s.Tuning.Population, night);
            foreach (var l in p.Npcs)
            {
                if (!Bodies.IsLivingPerson(l) || l.Id == speaker.Id || l.Mind == null || l.Action.Asleep) continue;
                int d = g.Distance(l.Node, speaker.Node);
                if (d > sight) continue;
                if (l.Id != target && !Messages.Attends(p, g, l, speaker, d, sight, s.Tuning.Comms.AttendPeople)) continue;
                TellTo(s, ev, speaker, l, k, d, sight, night);
            }
        }

        /// <summary>M5's field-error chance of a gesture at distance <paramref name="d"/> (per mille, 0-500), plus D6's telling noise.</summary>
        public static int TellChance(GodState s, int d, int sight, bool night)
        {
            var ct = s.Tuning.Comms;
            int chance = s.Tuning.Divinity.Doctrine.TellNoisePermille;
            if (ct.Misunderstanding != 0) chance += ct.MisreadEdgePermille * d / Math.Max(1, sight) + (night ? ct.MisreadDark : 0);
            return Fx.Clamp(chance, 0, 500);
        }

        /// <summary>One listener decodes a telling: the speaker's interpretation, each rule altered with <see cref="TellChance"/> (draw <c>commandment-interpret</c>).</summary>
        public static void TellTo(GodState s, EventBuffer ev, Npc speaker, Npc l, in KnownCommandment spoken, int d, int sight, bool night)
        {
            var tu = s.Tuning.Divinity.Doctrine;
            var inc = spoken;
            inc.Source = CommandmentSource.Taught;
            inc.FromNpc = speaker.Id;
            var rng = s.Rng.Fork("commandment-interpret", l.Id, (s.Tick << 20) | (uint)(speaker.Id & 0xFFFFF));
            Garble(ref inc, ref rng, TellChance(s, d, sight, night));
            int esteem = tu.StrangerEsteem;
            var sm = l.Mind.Social;
            if (sm != null) { int ri = sm.Find(speaker.Id); if (ri >= 0) esteem = Math.Max(0, (int)sm.Rel[ri].Trust); }
            var m = MindOf(s, l);
            int exposure = tu.ExposureSalience * tu.TellRate / 100;
            bool fresh = m.IndexOfKnown(inc.Commandment) < 0;
            Expose(s, ev, l, m, inc, fresh ? exposure + tu.NewEntrySalience : exposure, spoken.Adoption * esteem / 4000, false, 0);
        }

        /// <summary>The telling gesture lasts <c>TellTicks</c>; then the action ends.</summary>
        internal static bool ActTell(GodState s, Npc npc)
        {
            if (npc.Action.Verb != TellVerb) return false;
            if (s.Tick - npc.Action.Since >= s.Tuning.Divinity.Doctrine.TellTicks)
            {
                npc.Action = new NpcAction { Since = npc.Action.Since };
                npc.Path = null; npc.PathGoal = -1;
                if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
            }
            return true;
        }

        // ---------------- the daily pass (4.5; slot (tick + id x 13) % TicksPerDay) ----------------

        /// <summary>
        /// One person's day: salience decays (forgotten at 0); adoption is pulled towards what its beliefs in the god support
        /// (5%), moved by its own learned values on its rule keys (experience), held at rejection once told of a revocation;
        /// its household members are exposed to its adopted commandments (the vertical channel).
        /// </summary>
        public static void Day(GodState s, EventBuffer ev, Npc n, DivineMind m)
        {
            var tu = s.Tuning.Divinity.Doctrine;
            var st = StateOf(s);
            for (int i = m.KnownCount - 1; i >= 0; i--)
            {
                ref var k = ref m.Known[i];
                k.Salience = (short)Math.Max(0, k.Salience - tu.SalienceDecay);
                if (k.Salience <= 0)
                {
                    st.Forgotten++;
                    ev.Add(new CommandmentForgotten { Npc = n.Id, Commandment = k.Commandment, Revision = k.Revision });
                    m.RemoveKnownAt(i);
                    continue;
                }
                Beliefs.Read(m, k.God, out int belief, out int faith, out _, out int fear, out int loyalty);
                int target = Beliefs.AdoptionTarget(belief, faith, fear, loyalty, tu.RevealAdoptionOffset);
                if (k.ToldRevoked) target = Math.Min(target, tu.RevokeShift);
                int a = k.Adoption + (target - k.Adoption) * tu.AdoptPullPermille / 1000;
                int exp = Experience(n, k);
                if (exp < tu.ExperienceRejectBelow) a -= tu.ExperiencePull;
                else if (exp > tu.ExperienceAdoptAbove) a += tu.ExperiencePull / 2;
                k.Adoption = (short)Fx.Clamp(a, -1000, 1000);
                Restance(s, ev, n, m, i, exp < tu.ExperienceRejectBelow ? 3 : 1);
            }
            if (tu.HouseholdPass != 0 && n.Household != 0 && n.Stage != LifeStage.Child) Household(s, ev, n, m);
        }

        /// <summary>
        /// Experience (4.5): the person's learned value (Stw + Ltw, vp) of its own rule keys <c>Feature.Divine(5|6|7)</c> for each
        /// rule's verb, signed so that positive means keeping the rule paid (a Prohibited rule's key sits on the forbidden act).
        /// </summary>
        public static int Experience(Npc n, in KnownCommandment k)
        {
            var store = n.Mind?.Store;
            if (store == null) return 0;
            int sum = 0;
            for (int i = 0; i < k.RuleCount; i++)
            {
                var r = k.Rule(i);
                int at = store.Find(DivinityMindHooks.RuleKey(r.Modality), r.LearningVerb);
                if (at < 0) continue;
                int v = store.Entries[at].Stw + store.Entries[at].Ltw;
                sum += r.Modality == Modality.Prohibited ? -v : v;
            }
            return sum;
        }

        /// <summary>The household channel: each other living member is exposed to this adult's adopted commandments (rate 120) when the shared adoption test passes.</summary>
        private static void Household(GodState s, EventBuffer ev, Npc n, DivineMind m)
        {
            var tu = s.Tuning.Divinity.Doctrine;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            for (int i = 0; i < m.KnownCount; i++)
            {
                var k = m.Known[i];
                if (StanceOf(tu, k) != CommandmentStance.Adopted) continue;
                foreach (var o in s.Population.Npcs)
                {
                    if (o.Id == n.Id || o.Household != n.Household || !Bodies.IsLivingPerson(o) || o.Mind == null) continue;
                    var rng = s.Rng.Fork("commandment-household", o.Id, ((long)day << 24) | (uint)(k.Commandment & 0xFFFFFF));
                    int odds = AdoptionTest.Odds(tu.HouseholdRate * 5, 1000, k.Adoption / 4, tu.StrangerEsteem / 4, 0, 1000);
                    if (rng.NextInt(1000) >= odds) continue;
                    var inc = k;
                    inc.Source = CommandmentSource.Household;
                    inc.FromNpc = n.Id;
                    Garble(ref inc, ref rng, tu.HouseholdNoisePermille);
                    var om = MindOf(s, o);
                    int exposure = tu.ExposureSalience * tu.HouseholdRate / 100;
                    bool fresh = om.IndexOfKnown(k.Commandment) < 0;
                    Expose(s, ev, o, om, inc, fresh ? exposure + tu.NewEntrySalience : exposure, k.Adoption * tu.StrangerEsteem / 4000, false, 0);
                }
            }
        }
    }

    /// <summary><c>RevealCommandment { God, Template, Rules[], Importance, Recipients }</c> (contracts 10.2). OWNER: D6.</summary>
    public sealed class RevealCommandmentHandler : CommandHandler<RevealCommandment>
    {
        private readonly List<int> _rec = new List<int>();
        protected override CommandResult Validate(GodState s, RevealCommandment c) => Doctrine.ValidateReveal(s, c, _rec);
        protected override CommandResult Apply(GodState s, RevealCommandment c, EventBuffer ev) => Doctrine.ApplyReveal(s, ev, c, _rec);
    }

    /// <summary><c>ReviseCommandment { God, Commandment, Rules[], Importance }</c>: keeps the id; only those the god speaks to (the revelation's target) learn it directly. OWNER: D6.</summary>
    public sealed class ReviseCommandmentHandler : CommandHandler<ReviseCommandment>
    {
        private readonly List<int> _rec = new List<int>();
        protected override CommandResult Validate(GodState s, ReviseCommandment c) => Doctrine.ValidateRevise(s, c, _rec);
        protected override CommandResult Apply(GodState s, ReviseCommandment c, EventBuffer ev) => Doctrine.ApplyRevise(s, ev, c, _rec);
    }

    /// <summary><c>RevokeCommandment { God, Commandment }</c>: told people move towards rejection; nobody's knowledge is erased. OWNER: D6.</summary>
    public sealed class RevokeCommandmentHandler : CommandHandler<RevokeCommandment>
    {
        private readonly List<int> _rec = new List<int>();
        protected override CommandResult Validate(GodState s, RevokeCommandment c) => Doctrine.ValidateRevoke(s, c, _rec);
        protected override CommandResult Apply(GodState s, RevokeCommandment c, EventBuffer ev) => Doctrine.ApplyRevoke(s, ev, c, _rec);
    }
}
