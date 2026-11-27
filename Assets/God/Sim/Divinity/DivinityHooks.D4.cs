using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D4's sim-side hooks (contracts 11.3): the embodied gesture's <c>Start</c> (a controlled body, possessed person or avatar,
    /// says a <c>GestureAct</c> through M5's ordinary <c>Messages.Emit</c>: listeners decode it with the ordinary noise and
    /// answer with their own policy), and the action-ended slot (a held action or MoveTo input is consumed when its act ends).
    /// OWNER: D4.
    /// </summary>
    public static partial class DivinityHooks
    {
        /// <summary>Ticks an embodied hand-over may walk before it gives up (as D2's and M5's hand-over walk).</summary>
        public const int EmbodiedGiveWalkTicks = 600;

        static partial void TryStartD4(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled)
        {
            if (handled) return;
            if (c.Verb == EmbodiedVerb.Give) { handled = true; return; } // the walk and the hand-over are in TryActD4
            if (c.Verb != EmbodiedVerb.Gesture) return;
            handled = true;
            var e = Control.EntryOf(s, npc.Id);
            var act = (GestureAct)e.Target.Node;
            if ((e.Kind == ControlKind.Possessed || e.Kind == ControlKind.AvatarBody) && e.Act == DivineAct.Communicate && act != GestureAct.None && s.Social != null)
            {
                var topic = e.Target.Radius > 0 && e.Target.Radius < Wares.PersonalCount ? Messages.OfWare((Ware)e.Target.Radius) : default;
                Messages.Emit(s, ev, npc, Messages.Compose(act, topic, 0, default, e.Target.Id));
            }
            // a gesture is made at once (its duration is the view's: GestureMade.DurationTicks); the input is used up
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
            Embodiment.ClearInput(s, npc.Id);
        }

        static partial void TryActD4(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled)
        {
            if (handled || npc.Action.Verb != EmbodiedVerb.Give) return;
            handled = true;
            var a = npc.Action;
            var g = s.Territory.Grid;
            var o = s.Population.Find(a.TargetId);
            var e = Control.EntryOf(s, npc.Id);
            bool avatar = npc.Kind == BodyKind.Avatar;
            if (o == null || !Bodies.IsLivingPerson(o) || s.Tick - a.Since > EmbodiedGiveWalkTicks || e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody)
            { Embodiment.EndAct(s, npc, true); return; }
            if (g.Distance(npc.Node, o.Node) > Reach.HandOver)
            {
                Embodiment.Walk(s, npc, o.Node, Reach.HandOver);
                if (npc.Path == null && g.Distance(npc.Node, o.Node) > Reach.HandOver) { Embodiment.EndAct(s, npc, true); return; }
                if (g.Distance(npc.Node, o.Node) > Reach.HandOver) return;
            }
            var ware = (Ware)e.Target.Node;
            int lot = 0;
            foreach (var l in npc.Carry)
            {
                if (l.Empty || ware != Ware.None && l.Ware != ware) continue;
                if (!avatar && (l.Owner.Kind != OwnerKind.Npc || l.Owner.Id != npc.Id)) continue; // a person gives only its own: never conjured
                lot = l.Id; break;
            }
            if (lot == 0) { Embodiment.EndAct(s, npc, true); return; }
            var r = Goods.Move(s, ev, Holder.Carry(npc.Id), lot, 1, Holder.Carry(o.Id), MoveReason.HandOver, npc.Id, OwnerRule.ToReceiver, basis: OwnershipBasis.Gift);
            Embodiment.EndAct(s, npc, !r.Ok);
        }

        static partial void ActionEndedD4(GodState s, EventBuffer ev, Npc npc, byte verb, bool done)
        {
            var e = Control.EntryOf(s, npc.Id);
            if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody) return;
            if (e.Input == ControlInputKind.Action && Embodiment.Accepts(e.Act, verb) || e.Input == ControlInputKind.MoveTo && verb == Verb.Approach)
                Embodiment.ClearInput(s, npc.Id);
        }
    }
}
