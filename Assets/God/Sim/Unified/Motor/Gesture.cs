using God.Sim.Comms;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.9, amendment section 4): GESTURE. The act goes out through the existing <c>Messages.Emit</c> (M5 layout
    /// unchanged: act, a topic from the first referent, quantity capped 0-9 there, the addressee when the target is a person), so
    /// listeners within <c>CommsRange</c> perceive it as any message; and the unified side record <see cref="MsgExt"/> keeps both
    /// referents, the full quantity (0-999) and the <see cref="PrimitiveControl.Corr"/> the control carries. UN2 never allocates a
    /// correlation id (UN7's). No gesture has a built-in meaning and none moves goods, changes needs or writes any claim.
    /// </summary>
    public static partial class Motor
    {
        /// <summary>The highest gesture act a unified body may make (<see cref="GestureAct.Claim"/>; 29-31 reserved).</summary>
        public const byte MaxGesture = (byte)GestureAct.Claim;

        private static MotorStatus Gesture(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            if (c.Gesture == 0 || c.Gesture > MaxGesture) return MotorStatus.Invalid;
            if (s.Social == null) return MotorStatus.Disabled;
            if (!apply) return MotorStatus.Accepted;
            int addressee = c.Target.Kind == TargetKind.Npc ? c.Target.Id : 0;
            var m = Messages.Compose((GestureAct)c.Gesture, TopicOf(in c.R1), c.Quantity, TopicOf(in c.R2), addressee);
            int id = Messages.Emit(s, ev, npc, in m);
            var ext = new MsgExt { Message = id, R1 = c.R1, R2 = c.R2, Quantity = c.Quantity, Corr = c.Corr };
            var list = s.Unification.MessageExt;
            int at = list.Count;
            while (at > 0 && list[at - 1].Message > id) at--;
            list.Insert(at, ext);
            return Start(s, u, Prim.Gesture, s.Tuning.Unification.UN2.GestureTicks, c.Target, origin, true);
        }

        /// <summary>The M5 topic of a referent: a ware (also a lot's ware), a person or a place; none otherwise.</summary>
        public static MsgTopic TopicOf(in Referent r)
        {
            switch (r.Kind)
            {
                case ReferentKind.Ware: return Messages.OfWare((Ware)r.Ware);
                case ReferentKind.Lot: return r.Ware != 0 ? Messages.OfWare((Ware)r.Ware) : default;
                case ReferentKind.Person: return Messages.AboutPerson(r.Id);
                case ReferentKind.Place: return Messages.AtPlace(r.Id);
                default: return default;
            }
        }

        /// <summary>The side record of a message, or false (binary search over the sorted <see cref="UnifiedState.MessageExt"/>).</summary>
        public static bool ExtOf(UnifiedState u, int message, out MsgExt x)
        {
            var a = u.MessageExt;
            int lo = 0, hi = a.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = a[mid].Message;
                if (v == message) { x = a[mid]; return true; }
                if (v < message) lo = mid + 1; else hi = mid - 1;
            }
            x = default; return false;
        }
    }
}
