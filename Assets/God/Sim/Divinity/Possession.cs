using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>D4's refusal codes (contracts 10.1: packages add the codes of their own commands in their own files).</summary>
    public static class EmbodimentReasons
    {
        public const string TooYoung = "too_young";
        public const string Foreign = "foreign_forbidden";
        public const string NoPossession = "no_possession";
        public const string NoAvatar = "no_avatar";
        public const string NotLand = "not_land";
        public const string Manifested = "already_manifested";

        public static string Sentence(string code)
        {
            switch (code)
            {
                case TooYoung: return "That person is too young to be taken.";
                case Foreign: return "This god may not take people of another people.";
                case NoPossession: return "The god holds no one.";
                case NoAvatar: return "The god has no body in the world.";
                case NotLand: return "A body can only stand on land.";
                case Manifested: return "The god's body is already in the world.";
                default: return DivineReasons.Sentence(code);
            }
        }

        public static CommandResult Reject(string code) => CommandResult.Reject(code, Sentence(code));
    }

    /// <summary>
    /// D4's learning verbs (contracts 14: Divinity rows 116-123; D6 holds 116-118, D2 119): the avatar's take (an avatar never
    /// becomes an owner, so it cannot use the person's pick-up, which makes the first taker the owner), the embodied give (D2's
    /// instructed hand-over reads its ware from the divine inbox, so a controlled body gives through its own row: one
    /// <c>Goods.Move</c> HandOver, <c>ToReceiver</c>, basis <c>Gift</c>) and the embodied gesture (a controlled body says a
    /// <c>GestureAct</c> through M5's ordinary emission and delivery).
    /// </summary>
    public static class EmbodiedVerb
    {
        /// <summary>The avatar takes a lot into its carry, its owner unchanged (<c>OwnerRule.Keep</c>).</summary>
        public const byte AvatarTake = 120;
        /// <summary>A controlled body hands a carried lot (a person: its own; the avatar: any) to a person (<c>ToReceiver</c>, basis <c>Gift</c>).</summary>
        public const byte Give = 121;
        /// <summary>A controlled body (possessed person or avatar) makes a gesture to a listener (M5 <c>Messages.Emit</c>).</summary>
        public const byte Gesture = 122;

        public static bool IsMine(byte v) => v == AvatarTake || v == Give || v == Gesture;
    }

    /// <summary>
    /// D4 (contracts 11.3; POSSESSION_AND_AVATAR 4.2, 5.2): what possession and the avatar share. Both are driven only by
    /// <see cref="DivineControlInput"/>s through F1's lease and heading resolution; this class adds the D4 side of that seam:
    /// the action vocabulary each body kind may name, the object normalised at the input's apply (the god points at anything it
    /// sees; the body walks there), and the per-tick resolution in <c>DivinitySystem</c>'s D4 slot (an act started or refused,
    /// a stop that halts at the end of the current edge, continuous heading steps). OWNER: D4.
    /// <para><b>Input target convention</b> (<c>DivineControlInput.Target</c> names only the act's object; the body is
    /// <c>Body</c>): a lot <c>{Lot, Id}</c> for Take, Drop, UseTool and Eat (from the carry); a person <c>{Npc, Id, Node = ware}</c>
    /// for Give (Id 0: the nearest person in sight; ware 0: whatever it holds) and <c>{Npc, Id}</c> for Approach; a place
    /// <c>{Location, Node}</c> for MoveTo, Inspect, Gather, Eat and Sleep; a hut <c>{Hut, Id}</c> for Sleep; a gesture
    /// <c>{Npc, Id = listener (0: whoever attends), Node = GestureAct, Radius = ware topic}</c> for Communicate; <c>None</c>: any
    /// candidate of the act's verb the body's own mind builds (the nearest).</para>
    /// </summary>
    public static class Embodiment
    {
        public static AvatarRegistry RegistryOf(GodState s) => s.Divinity.Avatars ?? (s.Divinity.Avatars = new AvatarRegistry());

        /// <summary>The acts a possessed person may be given (its own repertoire: D2's table plus the gesture).</summary>
        public static bool PossessionAct(DivineAct a) => DivineActs.Available(a) || a == DivineAct.Communicate;

        /// <summary>The acts an avatar may be given: movement, goods handling without ownership, gestures (no biology, no work).</summary>
        public static bool AvatarAct(DivineAct a)
        {
            switch (a)
            {
                case DivineAct.MoveTo: case DivineAct.Inspect: case DivineAct.Approach: case DivineAct.Take: case DivineAct.Drop:
                case DivineAct.Give: case DivineAct.Communicate:
                    return true;
                default: return false;
            }
        }

        /// <summary>
        /// Checks the object of an action input against the true world (no mutation): <c>null</c> when valid, else a refusal
        /// code. A named lot or person must exist; reach is the body's own walk.
        /// </summary>
        public static string CheckAction(GodState s, ControlKind kind, DivineAct act, in DivineTarget t)
        {
            if (kind == ControlKind.Possessed ? !PossessionAct(act) : !AvatarAct(act)) return DivineReasons.ActUnavailable;
            var p = s.Population;
            var g = s.Territory.Grid;
            switch (t.Kind)
            {
                case DivineTargetKind.None: return null;
                case DivineTargetKind.Lot:
                    if (t.Id == 0) return null;
                    return DivineActs.LotNode(s, t.Id, out _) != int.MinValue ? null : DivineReasons.BadTarget;
                case DivineTargetKind.Npc:
                {
                    if (t.Id == 0) return null;
                    var o = p.Find(t.Id);
                    if (o == null || !Bodies.IsPerson(o)) return DivineReasons.NotAPerson;
                    return o.Alive ? null : DivineReasons.TargetDead;
                }
                case DivineTargetKind.Location:
                    return t.Node >= 0 && t.Node < g.Count && g.IsLand(t.Node) ? null : DivineReasons.BadTarget;
                case DivineTargetKind.Hut:
                    return p.HutById(t.Id) != null ? null : DivineReasons.BadTarget;
                default: return DivineReasons.BadTarget;
            }
        }

        /// <summary>The object as the body's mind will read it: a lot gets its node and class, a place the class of what grows there.</summary>
        public static DivineTarget Normalise(GodState s, DivineAct act, DivineTarget t)
        {
            var p = s.Population;
            switch (t.Kind)
            {
                case DivineTargetKind.Lot:
                    if (t.Id != 0)
                    {
                        int node = DivineActs.LotNode(s, t.Id, out var w);
                        t.Node = node < 0 ? 0 : node;
                        t.Radius = DivineActs.LotClass(w);
                    }
                    break;
                case DivineTargetKind.Location:
                {
                    int cls = 0;
                    foreach (var src in p.Sources) if (src.Node == t.Node) { cls = PopulationModule.ClassOf(src.Kind); break; }
                    t.Radius = cls;
                    break;
                }
                case DivineTargetKind.Hut:
                    t.Node = p.HutById(t.Id)?.Node ?? 0;
                    break;
            }
            return t;
        }

        /// <summary>The topic of a normalised object (D2's topic encoding, so <c>DivineActs.Candidate</c> builds the candidate).</summary>
        public static Comms.MsgTopic TopicOf(in DivineTarget t)
        {
            switch (t.Kind)
            {
                case DivineTargetKind.Lot:
                    return t.Id == 0 ? default : new Comms.MsgTopic { Kind = Comms.TopicKind.Task, Id = t.Id, Sub = (t.Node & 0xFFFFFF) | (t.Radius << 24) };
                case DivineTargetKind.Hut:
                    return new Comms.MsgTopic { Kind = Comms.TopicKind.Task, Id = t.Id, Sub = (t.Node & 0xFFFFFF) | (ObjClass.Hut << 24) };
                case DivineTargetKind.Location:
                    return new Comms.MsgTopic { Kind = Comms.TopicKind.Place, Id = t.Node, Sub = t.Radius };
                case DivineTargetKind.Npc:
                    return new Comms.MsgTopic { Kind = Comms.TopicKind.Npc, Id = t.Id, Sub = t.Node };
                default: return default;
            }
        }

        /// <summary>True when a started learning verb is what the act asked for (Gather may become ForageAt, Take may become Equip).</summary>
        public static bool Accepts(DivineAct act, byte verb)
        {
            switch (act)
            {
                case DivineAct.MoveTo: case DivineAct.Inspect: case DivineAct.Approach: return verb == Verb.Approach;
                case DivineAct.Gather: return verb == Verb.Gather || verb == Verb.ForageAt;
                case DivineAct.Take: return verb == Verb.PickUp || verb == Verb.Equip || verb == EmbodiedVerb.AvatarTake;
                case DivineAct.UseTool: return verb == Verb.Equip || verb == EmbodiedVerb.AvatarTake;
                case DivineAct.Drop: return verb == Verb.Drop;
                case DivineAct.Give: return verb == DivineVerb.Give || verb == EmbodiedVerb.Give;
                case DivineAct.Eat: return verb == Verb.Eat;
                case DivineAct.Sleep: return verb == Verb.Sleep;
                case DivineAct.Retreat: return verb == Verb.Flee;
                case DivineAct.Communicate: return verb == EmbodiedVerb.Gesture;
                default: return false;
            }
        }

        /// <summary>Ends the body's act (with the frustration signal to a person's learner when it failed: as for its own acts).</summary>
        internal static void EndAct(GodState s, Npc npc, bool failed)
        {
            if (failed && npc.Mind != null && npc.Kind == BodyKind.Person) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
        }

        /// <summary>One tick along the path (the M3 step rule and costs, an embodiment path cache); true when within reach.</summary>
        internal static bool Walk(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var reg = RegistryOf(s);
            if (reg.Pather == null) reg.Pather = new NodePather(g);
            if (npc.Path == null || npc.PathGoal != goal)
            {
                npc.Path = reg.Pather.Find(npc.Node, goal, s.Territory.NodeRoad, tu.RoadStepTicks, tu.StepTicks, tu.PathMaxExpand);
                npc.PathIdx = 0; npc.PathGoal = goal; npc.StepTicks = 0;
                if (npc.Path == null || npc.Path.Length == 0) { npc.Path = null; return false; }
            }
            if (npc.PathIdx >= npc.Path.Length) { npc.Path = null; npc.PathGoal = -1; return g.Distance(npc.Node, goal) <= reach; }
            int next = npc.Path[npc.PathIdx];
            int cost = s.Territory.NodeRoad[next] != 0 ? tu.RoadStepTicks : tu.StepTicks;
            if (++npc.StepTicks >= cost) { npc.StepTicks = 0; npc.Node = next; npc.PathIdx++; }
            return g.Distance(npc.Node, goal) <= reach;
        }

        internal static void Write(GodState s, in ControlEntry e)
        {
            var t = s.Divinity.Control;
            int i = t.IndexOf(e.Body);
            if (i >= 0) t.Entries[i] = e;
        }

        /// <summary>Clears the held action or move input of a body (the act ended, was refused, or completed at once).</summary>
        internal static void ClearInput(GodState s, int body)
        {
            var e = Control.EntryOf(s, body);
            if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody) return;
            if (e.Input != ControlInputKind.Action && e.Input != ControlInputKind.MoveTo) return;
            e.Input = ControlInputKind.None; e.MoveNode = 0;
            Write(s, e);
        }

        /// <summary>
        /// Stop (or a lapsed lease): the body finishes the edge it is on and halts (bodies never stop mid-edge): a walk with more
        /// than the current edge left becomes a walk to that edge's end; a body exactly at a node stops walking.
        /// </summary>
        internal static void HaltAtEdge(Npc body)
        {
            if (body.Action.Asleep || body.Action.BusyUntil > 0) return;
            if (body.Path == null || body.PathIdx >= body.Path.Length) return;
            if (body.StepTicks > 0)
            {
                int next = body.Path[body.PathIdx];
                body.Path = new[] { next }; body.PathIdx = 0; body.PathGoal = next;
                body.Action = new NpcAction { Verb = Verb.Approach, TargetKind = IntentTarget.Node, TargetNode = next, Since = body.Action.Since };
            }
            else
            {
                body.Action = new NpcAction { Since = body.Action.Since };
                body.Path = null; body.PathGoal = -1;
                if (body.Mind != null) Learner.EndOngoing(body.Mind.Store);
            }
        }

        /// <summary>
        /// DivinitySystem's D4 slot, every possessed and avatar body in ascending body order: the act its last think started (or
        /// the action input it could not start), a stop, continuous heading steps, then (possession) the learning scale.
        /// </summary>
        internal static void Tick(GodState s, EventBuffer ev)
        {
            var t = s.Divinity.Control;
            var reg = s.Divinity.Avatars;
            if (t == null || t.Entries.Count == 0 || reg == null) return;
            var p = s.Population;
            int every = s.Tuning.Population.ThinkEvery;
            for (int i = 0; i < t.Entries.Count; i++)
            {
                var e = t.Entries[i];
                if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody) continue;
                var body = p.Find(e.Body);
                if (body == null || !body.Alive) continue;
                var ps = e.Kind == ControlKind.Possessed ? reg.PossessionOf(e.Intervention) : null;
                var av = e.Kind == ControlKind.AvatarBody ? reg.ManifestationOf(e.Intervention) : null;
                if (ps == null && av == null) continue;

                // 1. what the body's last think started (one tick late: the think ran after this system last tick)
                if (body.LastThinkTick == s.Tick - 1 && body.Mind != null)
                {
                    var last = body.Mind.Last;
                    bool started = last.Tick == s.Tick - 1 && last.Control == e.Kind && last.Verb != 0 && body.Action.Since == s.Tick - 1;
                    if (started)
                    {
                        if (ps != null) Possession.ActionStarted(s, ev, ps, body, in last);
                        else Avatars.ActionStarted(s, ev, av, body, in last);
                    }
                    long pending = ps?.PendingAction ?? av.PendingAction;
                    if (pending >= 0 && body.LastThinkTick >= pending && e.Input == ControlInputKind.Action)
                    {
                        bool ok = started && Accepts(e.Act, last.Verb);
                        if (!ok) { if (ps != null) ps.InputsRefused++; else av.InputsRefused++; }
                        if (ps != null) ps.PendingAction = -1; else av.PendingAction = -1;
                        // refused, or done at once (picked up within reach, gestured): nothing more to hold
                        if (!ok || body.Action.Verb == 0) { e.Input = ControlInputKind.None; }
                    }
                }
                else if (e.Input == ControlInputKind.Action && (ps?.PendingAction ?? av.PendingAction) < 0 && body.Action.Verb == 0 && body.LastThinkTick > e.InputTick)
                {
                    e.Input = ControlInputKind.None; // the act ran and ended between thinks
                }

                // 2. stop, or a lapsed lease (F1's ResolveMovement turned it into Stop): finish the current edge and halt
                if (e.Input == ControlInputKind.Stop)
                {
                    HaltAtEdge(body);
                    e.Input = ControlInputKind.None; e.MoveNode = 0;
                }

                // 3. a held heading: never turn mid-edge; think at once when the body is not walking toward its next node
                if (Control.LeaseHolds(in e, s.Tick))
                {
                    if (body.Path != null && body.PathIdx < body.Path.Length && body.StepTicks > 0) e.MoveNode = body.Path[body.PathIdx];
                    bool stepping = body.Action.Verb == Verb.Approach && body.Action.TargetKind == IntentTarget.Node && body.Action.TargetNode == e.MoveNode;
                    if (e.MoveNode > 0 && !stepping && !body.Action.Asleep && body.Action.BusyUntil == 0 && s.Tick != e.InputTick + 1)
                        e.InputTick = s.Tick - 1; // F1's ForceThink: the body thinks this tick
                }
                t.Entries[i] = e;

                // 4. possession: rewards earned under control reach the learner scaled (contracts 11.3), before this tick's think
                if (ps != null && ((s.Tick + body.Id) % every == 0 || Control.ForceThink(s, body)))
                    Possession.ScaleReward(s, body, s.Tuning.Divinity.Possession.LearnPermille);
            }
        }
    }

    /// <summary>
    /// D4a (contracts 10.2, 11; POSSESSION_AND_AVATAR 4): possession. <c>BeginPossession</c> opens a <c>Possessed</c> control
    /// entry through <see cref="Control.TryAcquire"/> (which ends a compulsion on the body and the god's own avatar,
    /// <c>Superseded</c>) and a <see cref="PossessionSpell"/>; the body's mind keeps running, its policy is skipped (no
    /// <c>RngPolicy</c> draw) and the god's inputs become ordinary candidates of the person's own knowledge and physical state
    /// (D4's mind hook); the body keeps every physical process (hunger, fatigue, injury, illness, ageing) and can die, which ends
    /// the spell (<c>Death</c>). Upkeep is D1's (<c>MythicExhausted</c> the same tick it cannot be paid). OWNER: D4.
    /// </summary>
    public static class Possession
    {
        public static InterventionKind Kind => InterventionKind.Possession;

        internal static CommandResult Validate(GodState s, BeginPossession c)
        {
            if (s.Divinity == null) return EmbodimentReasons.Reject(DivineReasons.DivinityOff);
            var body = s.Population?.Find(c.Npc);
            if (body == null) return EmbodimentReasons.Reject(DivineReasons.BadTarget);
            if (!Bodies.IsPerson(body)) return EmbodimentReasons.Reject(DivineReasons.NotAPerson);
            if (!body.Alive) return EmbodimentReasons.Reject(DivineReasons.TargetDead);
            var god = s.Divinity.Gods?.Of(c.God);
            if (god == null) return EmbodimentReasons.Reject(DivineReasons.NoSuchGod);
            var tu = s.Tuning.Divinity.Possession;
            if (tu.AllowForeign == 0 && body.Faction != god.OriginFaction) return EmbodimentReasons.Reject(EmbodimentReasons.Foreign);
            if (Bodies.AgeMilli(SimCalendar.Of(s), body, s.Tick) < tu.MinAgeMilli) return EmbodimentReasons.Reject(EmbodimentReasons.TooYoung);
            // the precedence of contracts 11.1, checked without mutation (TryAcquire checks it again)
            var cur = Control.EntryOf(s, body.Id);
            if (cur.Kind != ControlKind.Self && cur.Kind != ControlKind.Compelled) return EmbodimentReasons.Reject(DivineReasons.AlreadyControlled);
            int held = 0;
            foreach (var x in s.Divinity.Control?.Entries ?? new System.Collections.Generic.List<ControlEntry>())
                if (x.God == c.God && x.Kind == ControlKind.Possessed) held++;
            if (held >= Control.MaxPossessionsPerGod) return EmbodimentReasons.Reject(DivineReasons.ControlLimit);
            return Mythic.CanSpend(s, c.God, InterventionKind.Possession, s.Tuning.Divinity.Mythic.Costs.BeginPossession);
        }

        internal static CommandResult Apply(GodState s, EventBuffer ev, BeginPossession c)
        {
            var r = Validate(s, c);
            if (!r.Ok) return r;
            long cost = s.Tuning.Divinity.Mythic.Costs.BeginPossession;
            r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Possession, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            var body = s.Population.Find(c.Npc);
            int id = Interventions.Open(s, ev, c.God, InterventionKind.Possession, DivineAct.None, new DivineTarget(DivineTargetKind.Npc, body.Id, body.Node), DivineScope.Individual, cost);
            var reg = Embodiment.RegistryOf(s);
            var cur = Control.EntryOf(s, body.Id);
            int compulsion = cur.Kind == ControlKind.Compelled ? cur.Intervention : 0;
            int avatar = reg.ManifestationOfGod(c.God)?.Intervention ?? 0;
            var entry = new ControlEntry
            {
                Body = body.Id, Kind = ControlKind.Possessed, God = c.God, Intervention = id, SinceTick = s.Tick, Heading = ControlEntry.HeadingStop, Gait = 1,
            };
            if (!Control.TryAcquire(s, ev, in entry, out string reason)) return EmbodimentReasons.Reject(reason); // validated above: not expected
            reg.Possessions.Add(new PossessionSpell { Intervention = id, God = c.God, Npc = body.Id, StartTick = s.Tick, SupersededCompulsion = compulsion });
            reg.PossessionsBegun++;
            ev.Add(new PossessionStarted
            {
                Intervention = id, God = c.God, Npc = body.Id, Faction = (int)body.Faction, Node = body.Node, SupersededCompulsion = compulsion, SupersededAvatar = avatar,
            });
            return CommandResult.Accept();
        }

        internal static CommandResult ValidateEnd(GodState s, EndPossession c)
        {
            if (s.Divinity == null) return EmbodimentReasons.Reject(DivineReasons.DivinityOff);
            var ps = s.Divinity.Avatars?.PossessionOfGod(c.God);
            if (ps == null || Control.EntryOf(s, ps.Npc).Kind != ControlKind.Possessed) return EmbodimentReasons.Reject(EmbodimentReasons.NoPossession);
            return CommandResult.Accept();
        }

        internal static CommandResult ApplyEnd(GodState s, EventBuffer ev, EndPossession c)
        {
            var r = ValidateEnd(s, c);
            if (!r.Ok) return r;
            Control.Release(s, ev, s.Divinity.Avatars.PossessionOfGod(c.God).Npc, ControlEndReason.Cancelled);
            return CommandResult.Accept();
        }

        /// <summary>
        /// The end report (from <c>Control.EndedD4</c>, once per spell, CT1): <see cref="PossessionEnded"/>; the rewards earned
        /// under control settled at the scale; the god-directed act stops, so the person's own policy decides afresh at its next
        /// think (a living, awake, not busy body).
        /// </summary>
        internal static void Ended(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why)
        {
            if (e.Kind != ControlKind.Possessed) return;
            var reg = s.Divinity.Avatars;
            var ps = reg?.PossessionOf(e.Intervention);
            var npc = s.Population?.Find(e.Body);
            ev.Add(new PossessionEnded
            {
                Intervention = e.Intervention, God = e.God, Npc = e.Body, Node = npc?.Node ?? 0, Reason = why, StartTick = e.SinceTick, Ticks = s.Tick - e.SinceTick,
                InputsApplied = ps?.InputsApplied ?? 0, InputsRefused = ps?.InputsRefused ?? 0, Actions = ps?.ActionsStarted ?? 0,
            });
            if (npc != null && npc.Alive)
            {
                ScaleReward(s, npc, s.Tuning.Divinity.Possession.LearnPermille);
                if (!npc.Action.Asleep && npc.Action.BusyUntil == 0 && npc.Action.Verb != 0)
                {
                    npc.Action = new NpcAction { Since = npc.Action.Since };
                    npc.Path = null; npc.PathGoal = -1;
                    if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
                }
            }
            if (ps != null) { reg.Possessions.Remove(ps); reg.PossessionsEnded++; }
        }

        /// <summary>
        /// Contracts 11.3 learning: rewards earned under possession scale by <c>Possession.LearnPermille</c>. The body's drive
        /// changes are settled into the store's pending reward (the population's own formula), then the pending reward is scaled;
        /// at 1000 (default) nothing is touched. (D2's rule for compulsion, with D4's number.)
        /// </summary>
        internal static void ScaleReward(GodState s, Npc npc, int permille)
        {
            if (permille >= 1000 || npc.Mind == null) return;
            var tu = s.Tuning.Population;
            var lt = s.Tuning.Learning;
            int r = 0;
            for (int d = 0; d < Drive.Count; d++)
            {
                int prev = npc.RewardSnap[d], now = npc.Drives[d];
                if (prev == now) continue;
                r += Learner.DriveReward(lt.W(d), Thinker.Comfort(tu, d) * 1000, prev, now) / 1000;
                npc.RewardSnap[d] = now;
            }
            Learner.AddReward(npc.Mind.Store, r);
            var st = npc.Mind.Store;
            st.RewardAcc = Fx.MulDiv(st.RewardAcc, permille < 0 ? 0 : permille, 1000);
        }

        internal static void ActionStarted(GodState s, EventBuffer ev, PossessionSpell ps, Npc body, in DecisionRecord last)
        {
            ps.ActionsStarted++;
            ev.Add(new PossessedActionStarted
            {
                Intervention = ps.Intervention, Npc = body.Id, Verb = last.Verb, TargetKind = last.TargetKind, TargetId = last.TargetId, TargetNode = last.TargetNode,
                ActionTick = last.Tick,
            });
        }
    }

    /// <summary><c>BeginPossession { God, Npc }</c> (contracts 10.2, 11.1). OWNER: D4.</summary>
    public sealed class BeginPossessionHandler : CommandHandler<BeginPossession>
    {
        protected override CommandResult Validate(GodState s, BeginPossession c) => Possession.Validate(s, c);
        protected override CommandResult Apply(GodState s, BeginPossession c, EventBuffer ev) => Possession.Apply(s, ev, c);
    }

    /// <summary><c>EndPossession { God }</c>: the god releases the person (<c>Cancelled</c>). OWNER: D4.</summary>
    public sealed class EndPossessionHandler : CommandHandler<EndPossession>
    {
        protected override CommandResult Validate(GodState s, EndPossession c) => Possession.ValidateEnd(s, c);
        protected override CommandResult Apply(GodState s, EndPossession c, EventBuffer ev) => Possession.ApplyEnd(s, ev, c);
    }

    /// <summary>
    /// The registration of F1's <see cref="DivineControlInputHandler"/> (contracts 11.3: "registered by D4/D5 when their spells
    /// exist"): F1's handler validates and applies the lease and heading model unchanged; D4 adds the act vocabulary of the body
    /// kind and the object check, normalises the object for the body's mind, and counts the input on its spell. OWNER: D4.
    /// </summary>
    public sealed class EmbodiedInputHandler : CommandHandler<DivineControlInput>
    {
        private readonly ICommandHandler _f1 = new DivineControlInputHandler();

        protected override CommandResult Validate(GodState s, DivineControlInput c)
        {
            var r = _f1.Validate(s, c);
            if (!r.Ok || c.Kind != ControlInputKind.Action) return r;
            var e = Control.EntryOf(s, c.Body);
            string why = Embodiment.CheckAction(s, e.Kind, c.Act, c.Target);
            return why == null ? CommandResult.Accept() : EmbodimentReasons.Reject(why);
        }

        protected override CommandResult Apply(GodState s, DivineControlInput c, EventBuffer ev)
        {
            var r = Validate(s, c);
            if (!r.Ok) return r;
            r = _f1.Apply(s, c, ev);
            if (!r.Ok) return r;
            var e = Control.EntryOf(s, c.Body);
            var reg = Embodiment.RegistryOf(s);
            var ps = e.Kind == ControlKind.Possessed ? reg.PossessionOf(e.Intervention) : null;
            var av = e.Kind == ControlKind.AvatarBody ? reg.ManifestationOf(e.Intervention) : null;
            if (ps != null) ps.InputsApplied++;
            if (av != null) av.InputsApplied++;
            if (c.Kind == ControlInputKind.Action)
            {
                e.Target = Embodiment.Normalise(s, c.Act, c.Target);
                Embodiment.Write(s, e);
                if (ps != null) ps.PendingAction = s.Tick;
                if (av != null) av.PendingAction = s.Tick;
            }
            return r;
        }
    }

    /// <summary>D4's slot of the frozen control seam: the possession's end report.</summary>
    public static partial class Control
    {
        static partial void EndedD4(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why) => Possession.Ended(s, ev, in e, why);
    }

    /// <summary>D4's slot of <see cref="DivinitySystem"/> (after <c>Control.Sweep</c>, movement and D2): possessed and avatar bodies.</summary>
    public sealed partial class DivinitySystem
    {
        partial void D4Tick(GodState s, EventBuffer ev) => Embodiment.Tick(s, ev);
    }

    /// <summary>D4's install slot: the possession handlers and the control input (shared by possession and the avatar).</summary>
    public static partial class DivinityModule
    {
        static partial void InstallD4(GodSim sim)
        {
            sim.RegisterHandler(new BeginPossessionHandler());
            sim.RegisterHandler(new EndPossessionHandler());
            sim.RegisterHandler(new EmbodiedInputHandler());
        }
    }
}
