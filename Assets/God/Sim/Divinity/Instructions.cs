using System;
using System.Collections.Generic;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 10.2-10.3 (DIVINE_PLAYER 4.2-4.5): divine suggestions and commands. A suggestion or command is a
    /// <b>perceived message</b>: each recipient perceives it (or not) by its own draw, decodes it with M5's noise model into
    /// its own inbox, reads it at its next think and decides with its own policy, where the instruction is one candidate
    /// with a divine term. Nothing here chooses for an NPC. OWNER: D2.
    /// </summary>
    public static class Instructions
    {
        /// <summary>Not-reached reasons stored in <see cref="InstructionRecipient.Why"/> (index into this list).</summary>
        public static readonly ReadOnlyArray<string> NotReachedReason = new ReadOnlyArray<string>(
            "", DivineReasons.TargetDead, DivineReasons.NotAPerson, DivineReasons.AlreadyControlled, "asleep");

        internal static CommandResult Reject(string code) => CommandResult.Reject(code, DivineReasons.Sentence(code));

        /// <summary>The registry, created on the first instruction (absent before: pin P2).</summary>
        internal static InstructionRegistry RegistryOf(GodState s) => s.Divinity.Messages ?? (s.Divinity.Messages = new InstructionRegistry());

        /// <summary>The person's divine mind, created on first delivery (with the tuning bound).</summary>
        internal static DivineMind MindOf(GodState s, Npc npc)
        {
            if (npc.Mind == null) return null;
            var m = npc.Mind.Divine ?? (npc.Mind.Divine = new DivineMind());
            m.Tuning = s.Tuning.Divinity.Instruction;
            return m;
        }

        /// <summary>Binds every divine mind to the game's tuning (after a load).</summary>
        internal static void BindTuning(GodState s)
        {
            if (s.Population == null) return;
            foreach (var n in s.Population.Npcs) if (Bodies.IsPerson(n) && n.Mind?.Divine != null) n.Mind.Divine.Tuning = s.Tuning.Divinity.Instruction;
        }

        // ---------------- recipients (resolved at Apply, ascending NpcId) ----------------

        /// <summary>One resolved recipient: the person and, when it cannot be reached, why (index of <see cref="NotReachedReason"/>).</summary>
        public struct Resolved { public int Npc; public byte Why; }

        /// <summary>
        /// Resolves a scope into recipients in ascending NpcId (no mutation). Returns null when valid (with
        /// <paramref name="into"/> filled), else the refusal code. Possessed persons in a group scope are not reached.
        /// </summary>
        public static string Resolve(GodState s, in DivineTarget t, DivineScope scope, List<Resolved> into)
        {
            into.Clear();
            var p = s.Population;
            switch (scope)
            {
                case DivineScope.Individual:
                {
                    if (t.Kind != DivineTargetKind.Npc) return DivineReasons.BadTarget;
                    var n = p.Find(t.Id);
                    if (n == null) return DivineReasons.BadTarget;
                    if (!Bodies.IsPerson(n)) return DivineReasons.NotAPerson;
                    if (!n.Alive) return DivineReasons.TargetDead;
                    if (Control.Of(s, n.Id) == ControlKind.Possessed || Control.Of(s, n.Id) == ControlKind.AvatarBody) return DivineReasons.AlreadyControlled;
                    into.Add(new Resolved { Npc = n.Id });
                    return null;
                }
                case DivineScope.Household:
                    if (t.Kind != DivineTargetKind.Household || t.Id == 0) return DivineReasons.BadTarget;
                    foreach (var n in p.Npcs) if (n.Household == t.Id && Bodies.IsPerson(n)) into.Add(Of(s, n));
                    break;
                case DivineScope.Location:
                {
                    var g = s.Territory.Grid;
                    if (t.Kind != DivineTargetKind.Location || t.Node < 0 || t.Node >= g.Count || !g.IsLand(t.Node) || t.Radius < 0) return DivineReasons.BadTarget;
                    foreach (var n in p.Npcs) if (Bodies.IsLivingPerson(n) && g.Distance(n.Node, t.Node) <= t.Radius) into.Add(Of(s, n));
                    break;
                }
                case DivineScope.Org:
                {
                    // faction stand-in until C6's Orgs.RelayRecipients: the faction's living members, ascending, capped
                    if (t.Kind != DivineTargetKind.Org || t.Id < 0 || t.Id >= s.FactionCount) return DivineReasons.BadTarget;
                    int cap = Math.Max(1, s.Tuning.Divinity.Instruction.MaxRelay);
                    foreach (var n in p.Npcs)
                    {
                        if (!Bodies.IsLivingPerson(n) || (int)n.Faction != t.Id) continue;
                        if (into.Count >= cap) break;
                        into.Add(Of(s, n));
                    }
                    break;
                }
                default:
                    // Group needs an explicit id list the frozen command types do not carry (amendment requested)
                    return DivineReasons.BadTarget;
            }
            into.Sort((a, b) => a.Npc.CompareTo(b.Npc));
            int reached = 0;
            foreach (var r in into) if (r.Why == 0) reached++;
            if (reached == 0) return DivineReasons.BadTarget;
            if (into.Count > s.Tuning.Divinity.Instruction.MaxRecipients) return DivineReasons.BadTarget;
            return null;
        }

        private static Resolved Of(GodState s, Npc n)
        {
            byte why = 0;
            if (!n.Alive) why = 1;
            else if (!Bodies.IsPerson(n)) why = 2;
            else if (Control.Of(s, n.Id) == ControlKind.Possessed || Control.Of(s, n.Id) == ControlKind.AvatarBody) why = 3;
            return new Resolved { Npc = n.Id, Why = why };
        }

        /// <summary>MYTHIC 4.5: base cost plus the per-extra cost for every reached recipient after the first.</summary>
        public static long CostOf(GodState s, InterventionKind kind, List<Resolved> rec)
        {
            var c = s.Tuning.Divinity.Mythic.Costs;
            int reached = 0;
            foreach (var r in rec) if (r.Why == 0) reached++;
            int extra = Math.Max(0, reached - 1);
            return kind == InterventionKind.Command ? c.IssueCommand + c.CommandPerExtra * extra : c.IssueSuggestion + c.SuggestionPerExtra * extra;
        }

        // ---------------- validation and application (shared by both handlers) ----------------

        internal static CommandResult Validate(GodState s, InterventionKind kind, int god, DivineAct act, in DivineTarget t, DivineScope scope, int expire, List<Resolved> rec)
        {
            if (s.Divinity == null) return Reject(DivineReasons.DivinityOff);
            if (!DivineActs.Available(act)) return Reject(DivineReasons.ActUnavailable);
            var tu = s.Tuning.Divinity.Instruction;
            if (expire < 0 || expire > tu.MaxExpireTicks) return Reject(DivineReasons.BadTarget);
            string why = Resolve(s, t, scope, rec);
            if (why != null) return Reject(why);
            why = DivineActs.CheckObject(s, act, t);
            if (why != null) return Reject(why);
            return Mythic.CanSpend(s, god, kind, CostOf(s, kind, rec));
        }

        internal static CommandResult Apply(GodState s, EventBuffer ev, InterventionKind kind, int god, DivineAct act, DivineTarget t, DivineScope scope, int expire, List<Resolved> rec)
        {
            var r = Validate(s, kind, god, act, t, scope, expire, rec);
            if (!r.Ok) return r;
            long cost = CostOf(s, kind, rec);
            r = Mythic.TrySpend(s, ev, god, kind, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            int id = Interventions.Open(s, ev, god, kind, act, t, scope, cost);
            var tu = s.Tuning.Divinity.Instruction;
            var reg = RegistryOf(s);
            var ins = new Instruction
            {
                Id = id, God = god, Kind = kind, Act = act, Target = t, Scope = scope, Tick = s.Tick,
                Expires = s.Tick + (expire == 0 ? tu.DefaultExpireTicks : expire),
            };
            reg.Items.Add(ins);
            reg.Issued++;
            var p = s.Population;
            int notReached = 0;
            foreach (var x in rec)
            {
                var rr = new InstructionRecipient { Npc = x.Npc, Why = x.Why };
                var n = p.Find(x.Npc);
                if (x.Why != 0) rr.State = RecipientState.NotReached;
                else if (n.Action.Asleep) { rr.State = tu.WakeDelivery != 0 ? RecipientState.AwaitWake : RecipientState.NotReached; if (tu.WakeDelivery == 0) rr.Why = 4; }
                else rr.State = RecipientState.Perceived; // decided by the draw below
                if (rr.State == RecipientState.NotReached) notReached++;
                ins.Recipients.Add(rr);
            }
            if (kind == InterventionKind.Command)
                ev.Add(new DivineCommandIssued { Intervention = id, God = god, Act = act, TargetKind = t.Kind, TargetId = t.Id, TargetNode = t.Node, TargetRadius = t.Radius, Scope = scope, Recipients = rec.Count - notReached, NotReached = notReached, Expires = ins.Expires });
            else
                ev.Add(new DivineSuggestionIssued { Intervention = id, God = god, Act = act, TargetKind = t.Kind, TargetId = t.Id, TargetNode = t.Node, TargetRadius = t.Radius, Scope = scope, Recipients = rec.Count - notReached, NotReached = notReached, Expires = ins.Expires });
            var topic = DivineActs.TopicOf(s, act, t);
            for (int i = 0; i < ins.Recipients.Count; i++)
            {
                var rr = ins.Recipients[i];
                if (scope == DivineScope.Org && rr.State != RecipientState.NotReached) ev.Add(new InstructionRelayed { Intervention = id, God = god, Org = t.Id, Npc = rr.Npc });
                if (rr.State != RecipientState.Perceived) continue;
                Perceive(s, ev, ins, ref rr, p.Find(rr.Npc), topic, false);
                ins.Recipients[i] = rr;
            }
            return CommandResult.Accept();
        }

        /// <summary>
        /// The perception draw <c>divine-perceive(npc, intervention)</c> and, when perceived, the decode
        /// <c>divine-decode(npc, intervention)</c> into the inbox (M5's model: one field read wrong with the noise chance,
        /// M5's darkness term added at night), <see cref="CommandPerceived"/>, and a command's witnesses.
        /// </summary>
        internal static void Perceive(GodState s, EventBuffer ev, Instruction ins, ref InstructionRecipient rr, Npc npc, MsgTopic topic, bool onWaking)
        {
            var tu = s.Tuning.Divinity.Instruction;
            int chance = ins.Kind == InterventionKind.Command ? tu.PerceiveCommand : tu.PerceiveSuggestion;
            var draw = s.Rng.Fork("divine-perceive", npc.Id, ins.Id);
            if (draw.NextInt(1000) >= chance) { rr.State = RecipientState.Unperceived; return; }
            var dec = s.Rng.Fork("divine-decode", npc.Id, ins.Id);
            int noise = ins.Kind == InterventionKind.Command ? tu.CommandNoisePermille : tu.SuggestionNoisePermille;
            var ct = s.Tuning.Comms;
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            if (ct.Misunderstanding != 0 && night) noise += ct.MisreadDark;
            noise = Fx.Clamp(noise, 0, 1000);
            var d = new DecodedDivine
            {
                Intervention = ins.Id, God = ins.God, Kind = ins.Kind, Act = ins.Act, Topic = topic, Time = MsgTime.Now, Expires = ins.Expires,
                Clarity = (byte)(255 - noise * 255 / 1000),
            };
            bool mismatch = false;
            if (dec.NextInt(1000) < noise) mismatch = DivineActs.Misread(s.Territory.Grid, ref d, dec.NextInt(3) == 0 ? 1 : 0, dec.NextInt(6));
            var m = MindOf(s, npc);
            int evicted = m.Deliver(in d);
            rr.State = RecipientState.Perceived;
            rr.DecodedAct = d.Act;
            rr.Mismatch = mismatch;
            rr.PerceivedTick = s.Tick;
            RegistryOf(s).Perceived++;
            ev.Add(new CommandPerceived
            {
                Intervention = ins.Id, Npc = npc.Id, God = ins.God, Kind = ins.Kind, DecodedAct = d.Act, TopicKind = d.Topic.Kind, TopicId = d.Topic.Id,
                Clarity = d.Clarity, OnWaking = onWaking, Evicted = evicted, Mismatch = mismatch,
            });
            if (evicted != 0) Evicted(s, ev, npc.Id, evicted);
            if (ins.Kind == InterventionKind.Command && tu.Witnesses != 0 && !onWaking) Witnesses(s, ev, ins, npc);
        }

        /// <summary>An older entry left the inbox unread or undecided: its response is <c>Lapsed</c> now (one per perceiver).</summary>
        private static void Evicted(GodState s, EventBuffer ev, int npc, int intervention)
        {
            var ins = s.Divinity.Messages?.Find(intervention);
            if (ins == null) return;
            int i = ins.IndexOf(npc);
            if (i < 0) return;
            var rr = ins.Recipients[i];
            if (rr.State != RecipientState.Perceived) return;
            Respond(s, ev, ins, ref rr, CommandResponse.Lapsed, 0, 0);
            ins.Recipients[i] = rr;
        }

        /// <summary>
        /// DIVINE_PLAYER 4.2 step 5: a command is visible. People within gesture range of the recipient who attend it (it is
        /// among their nearest <c>Comms.AttendPeople</c> people in sight) see the sign (<see cref="DivineSignPerceived"/>). The
        /// base-context feature <c>SituationKind.DivineSign</c> needs a Divinity sense/base seam (amendment requested); the
        /// exposure is recorded as the event only.
        /// </summary>
        private static void Witnesses(GodState s, EventBuffer ev, Instruction ins, Npc recipient)
        {
            var p = s.Population;
            var g = s.Territory.Grid;
            bool night = PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;
            int sight = CommsRange.Gesture(s.Tuning.Population, night);
            foreach (var w in p.Npcs)
            {
                if (!Bodies.IsLivingPerson(w) || w.Id == recipient.Id || w.Action.Asleep) continue;
                int d = g.Distance(w.Node, recipient.Node);
                if (d > sight) continue;
                if (!Messages.Attends(p, g, w, recipient, d, sight, s.Tuning.Comms.AttendPeople)) continue;
                ev.Add(new DivineSignPerceived { Intervention = ins.Id, Witness = w.Id, Recipient = recipient.Id, Node = recipient.Node });
            }
        }

        internal static void Respond(GodState s, EventBuffer ev, Instruction ins, ref InstructionRecipient rr, CommandResponse resp, byte verb, int divine)
        {
            rr.State = RecipientState.Responded;
            rr.Response = resp;
            var reg = s.Divinity.Messages;
            reg.Responses[(int)resp]++;
            ev.Add(new CommandResponded { Intervention = ins.Id, Npc = rr.Npc, God = ins.God, Kind = ins.Kind, Response = resp, Verb = verb, Divine = divine, DecidedTick = s.Tick });
        }

        // ---------------- per tick (DivinitySystem's D2 slot) ----------------

        /// <summary>
        /// Per tick, after <c>Control.Sweep</c>: wake deliveries, responses of the minds that read an instruction at their
        /// last think (the first think after reading decides: Followed, Misread, Refused or Ignored), expiry (Lapsed), in
        /// ascending InterventionId and NpcId. O(open recipients).
        /// </summary>
        internal static void Tick(GodState s, EventBuffer ev)
        {
            var reg = s.Divinity.Messages;
            if (reg == null || reg.Items.Count == 0) return;
            var p = s.Population;
            for (int k = 0; k < reg.Items.Count; k++)
            {
                var ins = reg.Items[k];
                if (ins.Kind == InterventionKind.Compulsion) continue;
                MsgTopic topic = default;
                bool topicDone = false;
                for (int i = 0; i < ins.Recipients.Count; i++)
                {
                    var rr = ins.Recipients[i];
                    if (rr.State == RecipientState.AwaitWake)
                    {
                        var n = p.Find(rr.Npc);
                        if (n == null || !n.Alive) { rr.State = RecipientState.NotReached; rr.Why = 1; ins.Recipients[i] = rr; continue; }
                        if (n.Action.Asleep || s.Tick >= ins.Expires) continue;
                        if (Control.Of(s, n.Id) == ControlKind.Possessed) { rr.State = RecipientState.NotReached; rr.Why = 3; ins.Recipients[i] = rr; continue; }
                        if (!topicDone) { topic = DivineActs.TopicOf(s, ins.Act, ins.Target); topicDone = true; }
                        Perceive(s, ev, ins, ref rr, n, topic, true);
                        ins.Recipients[i] = rr;
                        continue;
                    }
                    if (rr.State != RecipientState.Perceived) continue;
                    var npc = p.Find(rr.Npc);
                    var m = npc?.Mind?.Divine;
                    if (m == null || m.ReadTick < 0 || m.ReadTick != npc.LastThinkTick || m.ReadTick != s.Tick - 1 || m.Read == null) continue;
                    int at = -1;
                    for (int j = 0; j < m.ReadCount; j++) if (m.Read[j].Intervention == ins.Id) { at = j; break; }
                    if (at < 0) continue; // not read at that think (the inbox had newer entries)
                    var rd = m.Read[at];
                    var last = npc.Mind.Last;
                    bool decided = last.Tick == (int)m.ReadTick;
                    CommandResponse resp;
                    if (!rd.Offered) resp = CommandResponse.Ignored;
                    else if (decided && rd.Matches(last.Verb, last.TargetKind, last.TargetId, last.TargetNode)) resp = rr.Mismatch ? CommandResponse.Misread : CommandResponse.Followed;
                    else resp = CommandResponse.Refused;
                    Respond(s, ev, ins, ref rr, resp, decided ? last.Verb : (byte)0, decided ? last.Divine : 0);
                    ins.Recipients[i] = rr;
                }
            }
            // expiry: every perceiver still undecided lapses; the inbox entries go
            for (int k = 0; k < reg.Items.Count;)
            {
                var ins = reg.Items[k];
                if (ins.Kind == InterventionKind.Compulsion || s.Tick < ins.Expires) { k++; continue; }
                Close(s, ev, ins);
                reg.Items.RemoveAt(k);
            }
        }

        /// <summary>Ends an instruction: undecided perceivers lapse; every recipient's inbox entry is removed.</summary>
        internal static void Close(GodState s, EventBuffer ev, Instruction ins)
        {
            var p = s.Population;
            for (int i = 0; i < ins.Recipients.Count; i++)
            {
                var rr = ins.Recipients[i];
                if (rr.State == RecipientState.Perceived && ins.Kind != InterventionKind.Compulsion) { Respond(s, ev, ins, ref rr, CommandResponse.Lapsed, 0, 0); ins.Recipients[i] = rr; }
                p.Find(rr.Npc)?.Mind?.Divine?.Remove(ins.Id);
            }
        }

        /// <summary>A recipient died (contracts 11.4 Kill order): its undecided instructions lapse, never-woken ones are not reached.</summary>
        internal static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            var reg = s.Divinity.Messages;
            if (reg == null) return;
            foreach (var ins in reg.Items)
            {
                if (ins.Kind == InterventionKind.Compulsion) continue;
                int i = ins.IndexOf(npc.Id);
                if (i < 0) continue;
                var rr = ins.Recipients[i];
                if (rr.State == RecipientState.Perceived) Respond(s, ev, ins, ref rr, CommandResponse.Lapsed, 0, 0);
                else if (rr.State == RecipientState.AwaitWake) { rr.State = RecipientState.NotReached; rr.Why = 1; }
                ins.Recipients[i] = rr;
            }
        }

        /// <summary>The followed instruction's action finished: its inbox entry leaves (it was done).</summary>
        internal static void ActionEnded(GodState s, Npc npc, byte verb, bool done)
        {
            var reg = s.Divinity.Messages;
            var m = npc.Mind?.Divine;
            if (!done || reg == null || m == null || m.InboxCount == 0) return;
            foreach (var ins in reg.Items)
            {
                if (ins.Kind == InterventionKind.Compulsion) continue;
                int i = ins.IndexOf(npc.Id);
                if (i < 0) continue;
                var rr = ins.Recipients[i];
                if (rr.Response != CommandResponse.Followed && rr.Response != CommandResponse.Misread) continue;
                int e = m.IndexOf(ins.Id);
                if (e < 0) continue;
                var sh = DivineActs.Map(m.Inbox[e].Act, DivineActs.TopicObject(m.Inbox[e].Topic));
                if (sh.Verb == verb || verb == Learning.Verb.ForageAt && sh.Verb == Learning.Verb.Gather || verb == Learning.Verb.Equip && sh.Verb == Learning.Verb.PickUp)
                    m.Remove(ins.Id);
            }
        }
    }

    /// <summary><c>IssueSuggestion { God, Act, Target, Scope, ExpireTicks }</c> (contracts 10.2). OWNER: D2.</summary>
    public sealed class IssueSuggestionHandler : CommandHandler<IssueSuggestion>
    {
        private readonly List<Instructions.Resolved> _rec = new List<Instructions.Resolved>();
        protected override CommandResult Validate(GodState s, IssueSuggestion c) => Instructions.Validate(s, InterventionKind.Suggestion, c.God, c.Act, c.Target, c.Scope, c.ExpireTicks, _rec);
        protected override CommandResult Apply(GodState s, IssueSuggestion c, EventBuffer ev) => Instructions.Apply(s, ev, InterventionKind.Suggestion, c.God, c.Act, c.Target, c.Scope, c.ExpireTicks, _rec);
    }

    /// <summary><c>IssueCommand { God, Act, Target, Scope, ExpireTicks }</c> (contracts 10.2): more weight and cost; still refusable. OWNER: D2.</summary>
    public sealed class IssueCommandHandler : CommandHandler<IssueCommand>
    {
        private readonly List<Instructions.Resolved> _rec = new List<Instructions.Resolved>();
        protected override CommandResult Validate(GodState s, IssueCommand c) => Instructions.Validate(s, InterventionKind.Command, c.God, c.Act, c.Target, c.Scope, c.ExpireTicks, _rec);
        protected override CommandResult Apply(GodState s, IssueCommand c, EventBuffer ev) => Instructions.Apply(s, ev, InterventionKind.Command, c.God, c.Act, c.Target, c.Scope, c.ExpireTicks, _rec);
    }
}
