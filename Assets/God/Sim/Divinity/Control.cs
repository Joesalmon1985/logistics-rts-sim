using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>Contracts 11: who drives a body. Only non-<see cref="Self"/> entries are stored.</summary>
    public enum ControlKind : byte { Self = 0, Compelled = 1, Possessed = 2, AvatarBody = 3 }

    /// <summary>Contracts 11.2: why a control spell ended. Each end is reported once (invariant CT1).</summary>
    public enum ControlEndReason : byte { None = 0, Completed = 1, Timeout = 2, Cancelled = 3, Death = 4, MythicExhausted = 5, Superseded = 6, Destroyed = 7 }

    /// <summary>The state of a controlled intent (contracts 11: Pending, Running, Blocked, Done).</summary>
    public enum ControlStatus : byte { Pending = 0, Running = 1, Blocked = 2, Done = 3 }

    /// <summary>
    /// Contracts 11: one control spell over one body. <see cref="ExpiresTick"/> 0 = no expiry. <see cref="Act"/> and
    /// <see cref="Target"/>: a compulsion's action, or the last discrete action input (possession, avatar). <see cref="Heading"/>
    /// and <see cref="Gait"/>: the held movement intent (<see cref="HeadingStop"/> = stop). F1 adds the input bookkeeping of
    /// 11.3: <see cref="Input"/>, <see cref="InputTick"/> (the tick the last input or the spell applied: the next tick thinks),
    /// <see cref="LeaseUntil"/> (the last tick a Heading input holds) and <see cref="MoveNode"/> (the node the sim chose or the
    /// MoveTo node).
    /// </summary>
    public struct ControlEntry
    {
        public const byte HeadingStop = 64;

        public int Body;
        public ControlKind Kind;
        public int God;
        public int Intervention;
        public long SinceTick, ExpiresTick;
        public DivineAct Act;
        public DivineTarget Target;
        public byte Heading, Gait;
        public ControlStatus Status;
        public bool Started;
        public ControlInputKind Input;
        public long InputTick, LeaseUntil;
        public int MoveNode;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Body); h.Add((int)Kind | ((int)Status << 8) | (Started ? 1 << 16 : 0) | ((int)Input << 17)); h.Add(God); h.Add(Intervention);
            h.Add(SinceTick); h.Add(ExpiresTick); h.Add((int)Act); Target.AddTo(ref h); h.Add(Heading | (Gait << 8));
            h.Add(InputTick); h.Add(LeaseUntil); h.Add(MoveNode);
        }
    }

    /// <summary>Contracts 11: <c>DivinityState.Control</c>, the entries sorted by Body (one per body at most).</summary>
    public sealed class ControlTable
    {
        public List<ControlEntry> Entries = new List<ControlEntry>();

        /// <summary>The index of the body's entry, or <c>~insertAt</c> when it has none.</summary>
        public int IndexOf(int body)
        {
            int lo = 0, hi = Entries.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Entries[mid].Body;
                if (v == body) return mid;
                if (v < body) lo = mid + 1; else hi = mid - 1;
            }
            return ~lo;
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Entries.Count);
            foreach (var e in Entries) e.AddTo(ref h);
        }
    }

    /// <summary>
    /// Contracts 11 (F1.9): the single owner of temporary control authority and of the decision-override seam. D2, D4 and D5
    /// open and close entries only through <see cref="TryAcquire"/> and <see cref="Release"/>; they never edit
    /// <c>PopulationSystem.Think</c>, <c>Start</c>, <c>Match</c> or <see cref="DecisionRecord"/>. No caller opens an entry in F1,
    /// so every query below answers "self" and every site is a no-op. OWNER: F1 (frozen; changed only by amendment).
    /// <para>Precedence (11.1): Self &lt; Compelled &lt; Possessed; AvatarBody only on its god's avatar. A second god's
    /// compulsion or possession of a controlled body is refused <c>already_controlled</c>; the same god's compulsion replaces
    /// its own (<c>Superseded</c>); a possession ends a compulsion (<c>Superseded</c>); a god has at most
    /// <see cref="MaxPossessionsPerGod"/> possessions (<c>control_limit</c>) and one embodied focus (a possession dismisses its
    /// avatar spell and a manifestation ends its possession, both <c>Superseded</c>).</para>
    /// </summary>
    public static partial class Control
    {
        /// <summary>Contracts 11.1 <c>Divinity.Possession.MaxPerGod = 1</c> (D4 may move it into its tuning by amendment).</summary>
        public const int MaxPossessionsPerGod = 1;

        private static ControlTable TableOf(GodState s) => s.Divinity?.Control;

        /// <summary>Who drives the body now (<see cref="ControlKind.Self"/> when nobody else does).</summary>
        public static ControlKind Of(GodState s, int body)
        {
            var t = TableOf(s);
            if (t == null) return ControlKind.Self;
            int i = t.IndexOf(body);
            return i >= 0 ? t.Entries[i].Kind : ControlKind.Self;
        }

        /// <summary>The body's entry (default, Kind Self, when it has none).</summary>
        public static ControlEntry EntryOf(GodState s, int body)
        {
            var t = TableOf(s);
            if (t == null) return default;
            int i = t.IndexOf(body);
            return i >= 0 ? t.Entries[i] : default;
        }

        /// <summary>
        /// Opens a spell after the precedence table (11.1). Validates everything first and changes nothing on a refusal; on
        /// success ends the spells it supersedes (once each), then inserts the entry (sorted by Body). <paramref name="reason"/>
        /// is a <see cref="DivineReasons"/> code on refusal.
        /// </summary>
        public static bool TryAcquire(GodState s, EventBuffer ev, in ControlEntry e, out string reason)
        {
            reason = null;
            if (s.Divinity == null) { reason = DivineReasons.DivinityOff; return false; }
            if (e.Kind == ControlKind.Self || e.God <= 0) { reason = DivineReasons.BadTarget; return false; }
            var body = s.Population?.Find(e.Body);
            if (body == null) { reason = DivineReasons.BadTarget; return false; }
            var t = TableOf(s);
            int at = t != null ? t.IndexOf(e.Body) : -1;
            var cur = t != null && at >= 0 ? t.Entries[at] : default;
            bool has = t != null && at >= 0;
            int supersedeBody = 0; // a second spell of the same god to end (embodiment rule)
            switch (e.Kind)
            {
                case ControlKind.Compelled:
                    if (!Bodies.IsPerson(body)) { reason = DivineReasons.NotAPerson; return false; }
                    if (!body.Alive) { reason = DivineReasons.TargetDead; return false; }
                    if (has && (cur.Kind != ControlKind.Compelled || cur.God != e.God)) { reason = DivineReasons.AlreadyControlled; return false; }
                    break;
                case ControlKind.Possessed:
                    if (!Bodies.IsPerson(body)) { reason = DivineReasons.NotAPerson; return false; }
                    if (!body.Alive) { reason = DivineReasons.TargetDead; return false; }
                    if (has && cur.Kind != ControlKind.Compelled) { reason = DivineReasons.AlreadyControlled; return false; }
                    if (CountOf(t, e.God, ControlKind.Possessed) >= MaxPossessionsPerGod) { reason = DivineReasons.ControlLimit; return false; }
                    supersedeBody = BodyOf(t, e.God, ControlKind.AvatarBody);
                    break;
                case ControlKind.AvatarBody:
                    if (body.Kind != BodyKind.Avatar || body.God != e.God || !body.Alive) { reason = DivineReasons.BadTarget; return false; }
                    if (has) { reason = DivineReasons.AlreadyControlled; return false; }
                    supersedeBody = BodyOf(t, e.God, ControlKind.Possessed);
                    break;
                default:
                    reason = DivineReasons.BadTarget; return false;
            }
            if (t == null) t = s.Divinity.Control = new ControlTable();
            if (has) End(s, ev, e.Body, ControlEndReason.Superseded);       // the same god's compulsion, or a compulsion under a new possession
            if (supersedeBody != 0) End(s, ev, supersedeBody, ControlEndReason.Superseded);
            var n = e;
            if (n.SinceTick == 0) n.SinceTick = s.Tick;
            n.InputTick = s.Tick;
            n.Status = ControlStatus.Pending;
            n.Started = false;
            int ins = t.IndexOf(n.Body);
            t.Entries.Insert(~ins, n);
            return true;
        }

        /// <summary>Ends the body's spell (if any) with the reason, reporting the end once. True when a spell ended.</summary>
        public static bool Release(GodState s, EventBuffer ev, int body, ControlEndReason why) => End(s, ev, body, why);

        /// <summary>
        /// Per tick (called by <c>DivinitySystem</c>, D1): ends spells on expiry (<c>Timeout</c>), on the death of a person body
        /// (<c>Death</c>), and avatar spells whose body is no longer manifested (<c>Destroyed</c>), in ascending body order.
        /// </summary>
        public static void Sweep(GodState s, EventBuffer ev)
        {
            var t = TableOf(s);
            if (t == null) return;
            for (int i = 0; i < t.Entries.Count;)
            {
                var e = t.Entries[i];
                var body = s.Population?.Find(e.Body);
                ControlEndReason why = ControlEndReason.None;
                if (body == null || !body.Alive) why = e.Kind == ControlKind.AvatarBody ? ControlEndReason.Destroyed : ControlEndReason.Death;
                else if (e.ExpiresTick != 0 && s.Tick >= e.ExpiresTick) why = ControlEndReason.Timeout;
                if (why == ControlEndReason.None) { i++; continue; }
                End(s, ev, e.Body, why); // removes entry i; the next one moves to i
            }
        }

        /// <summary>Kill order (contracts 11.4): <c>DivinityHooks.OnDeath</c> releases the dead body's control first.</summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc) => End(s, ev, npc.Id, ControlEndReason.Death);

        private static bool End(GodState s, EventBuffer ev, int body, ControlEndReason why)
        {
            var t = TableOf(s);
            if (t == null) return false;
            int i = t.IndexOf(body);
            if (i < 0) return false;
            var e = t.Entries[i];
            t.Entries.RemoveAt(i);
            Ended(s, ev, in e, why);
            return true;
        }

        /// <summary>
        /// The end report of a spell (CT1: once per spell): D2 emits <c>CompulsionEnded</c>, D4 <c>PossessionEnded</c>, D5
        /// <c>AvatarDismissed</c>/<c>AvatarDestroyed</c>, each for its own kind. Called after the entry left the table.
        /// </summary>
        private static void Ended(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why)
        {
            EndedD2(s, ev, in e, why);
            EndedD4(s, ev, in e, why);
            EndedD5(s, ev, in e, why);
        }
        static partial void EndedD2(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why);
        static partial void EndedD4(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why);
        static partial void EndedD5(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why);

        private static int CountOf(ControlTable t, int god, ControlKind kind)
        {
            if (t == null) return 0;
            int n = 0;
            foreach (var x in t.Entries) if (x.God == god && x.Kind == kind) n++;
            return n;
        }

        private static int BodyOf(ControlTable t, int god, ControlKind kind)
        {
            if (t == null) return 0;
            foreach (var x in t.Entries) if (x.God == god && x.Kind == kind) return x.Body;
            return 0;
        }

        private static void Update(GodState s, int body, ControlEntry e)
        {
            var t = TableOf(s);
            int i = t.IndexOf(body);
            if (i >= 0) t.Entries[i] = e;
        }

        // ---------------- the F1 sites in PopulationSystem (contracts 11.3) ----------------

        /// <summary>True on the tick after a new control input or a new spell applied (default false).</summary>
        public static bool ForceThink(GodState s, Npc npc)
        {
            var t = TableOf(s);
            if (t == null || t.Entries.Count == 0) return false;
            int i = t.IndexOf(npc.Id);
            return i >= 0 && s.Tick == t.Entries[i].InputTick + 1;
        }

        /// <summary>Possessed and avatar bodies skip the policy entirely (no exploration draw: <c>RngPolicy</c> not advanced).</summary>
        public static bool SkipsPolicy(GodState s, Npc npc)
        {
            var k = Of(s, npc.Id);
            return k == ControlKind.Possessed || k == ControlKind.AvatarBody;
        }

        /// <summary>
        /// The intent of a body whose policy is skipped: the input candidate (the one the divinity mind hooks marked in
        /// <see cref="ThinkScratch.ControlCand"/>), else the running action's candidate, else idle (<see cref="NpcIntent.Nothing"/>).
        /// </summary>
        public static NpcIntent ControlIntent(GodState s, Npc npc, ThinkScratch sc, ref DecisionRecord why)
        {
            var e = EntryOf(s, npc.Id);
            why.Control = e.Kind;
            if (sc.ControlCand >= 0 && sc.ControlCand < sc.CandCount)
            {
                MarkStarted(s, e);
                return AssociativeNpcPolicy.ToIntent(sc.Cand[sc.ControlCand]);
            }
            for (int i = 0; i < sc.CandCount; i++) if (sc.Cand[i].IsRunning) return AssociativeNpcPolicy.ToIntent(sc.Cand[i]);
            return NpcIntent.Nothing;
        }

        /// <summary>
        /// After the policy step: a compelled body's choice is replaced by the compelled candidate while it exists (11.3);
        /// <c>why.Control</c> is stamped with the kind. Nothing for a body with no entry.
        /// </summary>
        public static void OverrideIntent(GodState s, Npc npc, ThinkScratch sc, ref NpcIntent intent, ref DecisionRecord why)
        {
            var e = EntryOf(s, npc.Id);
            if (e.Kind != ControlKind.Compelled) return;
            why.Control = ControlKind.Compelled;
            if (sc.ControlCand < 0 || sc.ControlCand >= sc.CandCount) return;
            intent = AssociativeNpcPolicy.ToIntent(sc.Cand[sc.ControlCand]);
            MarkStarted(s, e);
        }

        private static void MarkStarted(GodState s, ControlEntry e)
        {
            if (e.Kind == ControlKind.Self) return;
            e.Started = true;
            if (e.Status == ControlStatus.Pending || e.Status == ControlStatus.Blocked) e.Status = ControlStatus.Running;
            Update(s, e.Body, e);
        }

        /// <summary>
        /// A running action of the body ended (<paramref name="done"/>: it finished rather than being replaced). A compulsion whose
        /// intent started and whose action ended done is <c>Completed</c> (11.2).
        /// </summary>
        public static void ActionEnded(GodState s, EventBuffer ev, Npc npc, byte verb, bool done)
        {
            var t = TableOf(s);
            if (t == null || t.Entries.Count == 0) return;
            var e = EntryOf(s, npc.Id);
            if (e.Kind == ControlKind.Compelled && e.Started && done) End(s, ev, npc.Id, ControlEndReason.Completed);
        }
    }


    /// <summary>
    /// F1b (contracts 11.3): the movement-input model of <see cref="DivineControlInput"/>, part of the frozen control seam
    /// (OWNER: F1). One model: a <c>Heading</c> input is an integer sector of the world frame (north = board +Z, clockwise)
    /// valid for <c>Divinity.Control.LeaseTicks</c> ticks from the tick it applies; <c>MoveTo</c> and <c>Action</c> hold until
    /// done or replaced; <c>Stop</c> or a lapsed lease halts the body at the end of its current edge. Each tick while the
    /// lease holds, <see cref="ResolveMovement"/> (run by <c>DivinitySystem</c>, D1, before the population tick) picks among
    /// the passable lattice neighbours of the body's node the one whose direction is nearest the sector's centre angle
    /// (integer angle table, ties by the lower node id) as the body's <see cref="ControlEntry.MoveNode"/>. The camera, frame
    /// rate and time scale never reach the sim.
    /// </summary>
    public static partial class Control
    {
        /// <summary>The bearing of each lattice direction (<c>NodeGrid.DI/DJ</c> order) in tenths of a degree, clockwise from north (+Z).</summary>
        public static readonly ReadOnlyArray<int> DirectionAngle = new ReadOnlyArray<int>(0, 600, 1200, 1800, 2400, 3000);

        public const int FullTurn = 3600;

        /// <summary>True for a sector count the model accepts (4, 8, 16, 32 or 64).</summary>
        public static bool ValidSectors(int sectors) => sectors == 4 || sectors == 8 || sectors == 16 || sectors == 32 || sectors == 64;

        /// <summary>The centre bearing of a sector, in tenths of a degree.</summary>
        public static int SectorAngle(int heading, int sectors) => heading * FullTurn / sectors;

        /// <summary>
        /// The passable lattice neighbour of <paramref name="node"/> nearest the sector's centre (ties: the lower node id), or -1
        /// when no neighbour is land. Pure: the same input always gives the same node.
        /// </summary>
        public static int NextNode(NodeGrid g, int node, int heading, int sectors)
        {
            int want = SectorAngle(heading, sectors);
            int best = -1, bestDiff = int.MaxValue;
            for (int dir = 0; dir < 6; dir++)
            {
                int v = g.Neighbour(node, dir);
                if (v < 0 || !g.IsLand(v)) continue;
                int d = System.Math.Abs(DirectionAngle[dir] - want) % FullTurn;
                if (d > FullTurn / 2) d = FullTurn - d;
                if (d < bestDiff || d == bestDiff && v < best) { best = v; bestDiff = d; }
            }
            return best;
        }

        /// <summary>True while a Heading input still holds at <paramref name="tick"/>.</summary>
        public static bool LeaseHolds(in ControlEntry e, long tick) => e.Input == ControlInputKind.Heading && e.Heading != ControlEntry.HeadingStop && tick <= e.LeaseUntil;

        /// <summary>
        /// Per tick, before the population tick (called by <c>DivinitySystem</c>, D1): for every possessed or avatar body with a
        /// held Heading, the next node; a lapsed lease becomes a stop (MoveNode 0: the body finishes its edge and halts).
        /// </summary>
        public static void ResolveMovement(GodState s)
        {
            var t = TableOf(s);
            if (t == null || t.Entries.Count == 0) return;
            var g = s.Territory.Grid;
            int sectors = s.Tuning.Divinity.Control.HeadingSectors;
            for (int i = 0; i < t.Entries.Count; i++)
            {
                var e = t.Entries[i];
                if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody || e.Input != ControlInputKind.Heading) continue;
                var body = s.Population?.Find(e.Body);
                if (body == null) continue;
                if (LeaseHolds(in e, s.Tick)) e.MoveNode = System.Math.Max(0, NextNode(g, body.Node, e.Heading, sectors));
                else { e.Input = ControlInputKind.Stop; e.Heading = ControlEntry.HeadingStop; e.MoveNode = 0; }
                t.Entries[i] = e;
            }
        }
    }

    /// <summary>
    /// F1b (contracts 11.3): validates and applies a <see cref="DivineControlInput"/> to the god's own spell over the body.
    /// <b>Not registered by F1</b>: D4 (possession) and D5 (avatar) register it when their spells exist, so until then the
    /// command is refused <c>disabled</c> like every F1 command. OWNER: F1 (frozen).
    /// </summary>
    public sealed class DivineControlInputHandler : CommandHandler<DivineControlInput>
    {
        protected override CommandResult Validate(GodState s, DivineControlInput c)
        {
            if (s.Divinity == null) return Reject(DivineReasons.DivinityOff);
            var e = Control.EntryOf(s, c.Body);
            if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody || e.God != c.God) return Reject(DivineReasons.BadTarget);
            switch (c.Kind)
            {
                case ControlInputKind.Heading:
                    int sectors = s.Tuning.Divinity.Control.HeadingSectors;
                    if (!Control.ValidSectors(sectors) || c.Heading >= sectors) return Reject(DivineReasons.BadTarget);
                    if (c.Gait < 1 || c.Gait > 2) return Reject(DivineReasons.BadTarget);
                    return CommandResult.Accept();
                case ControlInputKind.MoveTo:
                    return s.Territory.Grid.IsLand(c.Node) ? CommandResult.Accept() : Reject(DivineReasons.BadTarget);
                case ControlInputKind.Stop:
                    return CommandResult.Accept();
                case ControlInputKind.Action:
                    return c.Act != DivineAct.None ? CommandResult.Accept() : Reject(DivineReasons.ActUnavailable);
                default:
                    return Reject(DivineReasons.BadTarget);
            }
        }

        protected override CommandResult Apply(GodState s, DivineControlInput c, EventBuffer ev)
        {
            var r = Validate(s, c);
            if (!r.Ok) return r;
            var t = s.Divinity.Control;
            int i = t.IndexOf(c.Body);
            var e = t.Entries[i];
            e.Input = c.Kind;
            e.InputTick = s.Tick;
            switch (c.Kind)
            {
                case ControlInputKind.Heading:
                    e.Heading = c.Heading; e.Gait = c.Gait; e.LeaseUntil = s.Tick + s.Tuning.Divinity.Control.LeaseTicks - 1; e.MoveNode = 0;
                    break;
                case ControlInputKind.MoveTo:
                    e.MoveNode = c.Node; e.LeaseUntil = 0; e.Heading = ControlEntry.HeadingStop;
                    break;
                case ControlInputKind.Stop:
                    e.Heading = ControlEntry.HeadingStop; e.MoveNode = 0; e.LeaseUntil = 0;
                    break;
                case ControlInputKind.Action:
                    e.Act = c.Act; e.Target = c.Target;
                    break;
            }
            t.Entries[i] = e;
            return CommandResult.Accept();
        }

        private static CommandResult Reject(string code) => CommandResult.Reject(code, DivineReasons.Sentence(code));
    }
}
