using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D2's sim-side hooks (contracts 6.1, 11.4): the <c>Act</c> cases of the verbs M3 has none for (<c>Approach</c> 7 for
    /// MoveTo/Inspect/Approach, <c>Drop</c> 16, D2's instructed hand-over 119), each through the ordinary physical
    /// primitives (walk with reach, <see cref="Goods.Move"/>); the death slot; the action-ended slot. OWNER: D2.
    /// </summary>
    public static partial class DivinityHooks
    {
        /// <summary>Ticks an instructed hand-over may walk before it gives up (as M5's hand-over walk).</summary>
        public const int GiveWalkTicks = 600;

        private static bool IsMine(byte verb) => verb == Verb.Approach || verb == Verb.Drop || verb == DivineVerb.Give;

        static partial void TryStartD2(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled)
        {
            if (handled || !IsMine(c.Verb)) return;
            handled = true; // M3's Start set the action; the walk and the act are in TryActD2
        }

        static partial void TryActD2(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled) return;
            var a = npc.Action;
            if (!IsMine(a.Verb)) return;
            handled = true;
            var p = s.Population;
            var g = s.Territory.Grid;
            switch (a.Verb)
            {
                case Verb.Approach:
                {
                    int goal = a.TargetNode;
                    if (a.TargetKind == Policy.IntentTarget.Npc)
                    {
                        var o = p.Find(a.TargetId);
                        if (o == null || !o.Alive) { Frustrate(s, npc); return; }
                        goal = o.Node;
                        if (Walk(s, npc, goal, Reach.Touch) || npc.Path == null && g.Distance(npc.Node, goal) > Reach.Touch) End(npc);
                        return;
                    }
                    if (Walk(s, npc, goal, Reach.Enter) || npc.Path == null) End(npc);
                    return;
                }
                case Verb.Drop:
                {
                    int slot = -1;
                    for (int i = 0; i < npc.Carry.Length; i++) if (!npc.Carry[i].Empty && npc.Carry[i].Id == a.TargetId) { slot = i; break; }
                    if (slot < 0) { Frustrate(s, npc); return; }
                    var l = npc.Carry[slot];
                    var r = Goods.Move(s, ev, Holder.Carry(npc.Id), l.Id, l.Count, Holder.Ground(npc.Node), MoveReason.Drop, npc.Id);
                    if (!r.Ok) { Frustrate(s, npc); return; }
                    End(npc);
                    return;
                }
                case DivineVerb.Give:
                {
                    var o = p.Find(a.TargetId);
                    var ware = GiveWare(npc, a.TargetId);
                    if (o == null || !o.Alive || ware == Ware.None || s.Tick - a.Since > GiveWalkTicks) { Frustrate(s, npc); return; }
                    if (g.Distance(npc.Node, o.Node) > Reach.HandOver)
                    {
                        Walk(s, npc, o.Node, Reach.HandOver);
                        if (npc.Path == null && g.Distance(npc.Node, o.Node) > Reach.HandOver) { Frustrate(s, npc); return; }
                        if (g.Distance(npc.Node, o.Node) > Reach.HandOver) return;
                    }
                    int lot = 0;
                    foreach (var l in npc.Carry)
                        if (!l.Empty && l.Ware == ware && l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == npc.Id) { lot = l.Id; break; }
                    if (lot == 0) { Frustrate(s, npc); return; } // holds none of it: never conjured
                    var r = Goods.Move(s, ev, Holder.Carry(npc.Id), lot, 1, Holder.Carry(o.Id), MoveReason.HandOver, npc.Id, OwnerRule.ToReceiver, basis: OwnershipBasis.Gift);
                    if (!r.Ok) { Frustrate(s, npc); return; }
                    End(npc);
                    return;
                }
            }
        }

        /// <summary>The ware the instruction (or compulsion) to give to <paramref name="to"/> names, from the giver's own inbox.</summary>
        private static Ware GiveWare(Npc npc, int to)
        {
            var m = npc.Mind?.Divine;
            if (m == null) return Ware.None;
            for (int i = m.InboxCount - 1; i >= 0; i--)
            {
                var d = m.Inbox[i];
                if (d.Act == DivineAct.Give && d.Topic.Kind == Comms.TopicKind.Npc && d.Topic.Id == to) return (Ware)d.Topic.Sub;
            }
            return Ware.None;
        }

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

        /// <summary>One tick along the path (the M3 step rule, a D2 path cache); true when within reach.</summary>
        private static bool Walk(GodState s, Npc npc, int goal, int reach)
        {
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, goal) <= reach) { npc.Path = null; npc.PathGoal = -1; npc.StepTicks = 0; return true; }
            var tu = s.Tuning.Population;
            var reg = Instructions.RegistryOf(s);
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

        static partial void OnDeathD2(GodState s, EventBuffer ev, Npc npc) => Instructions.OnDeath(s, ev, npc);

        static partial void ActionEndedD2(GodState s, EventBuffer ev, Npc npc, byte verb, bool done) => Instructions.ActionEnded(s, npc, verb, done);
    }

    /// <summary>D2's install slot: the four handlers, and the divine minds rebound to the tuning (after a load).</summary>
    public static partial class DivinityModule
    {
        static partial void InstallD2(GodSim sim)
        {
            sim.RegisterHandler(new IssueSuggestionHandler());
            sim.RegisterHandler(new IssueCommandHandler());
            sim.RegisterHandler(new IssueCompulsionHandler());
            sim.RegisterHandler(new CancelCompulsionHandler());
            Instructions.BindTuning(sim.State);
        }
    }
}
