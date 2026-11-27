using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D4's mind hooks (contracts 11.3): a possessed person or an avatar still thinks (perception, beliefs, candidates); this
    /// hook turns the god's held input (<c>ThinkScratch.Control</c>, F1's copy of the control entry) into one ordinary candidate
    /// built from the body's own knowledge and physical state, and marks it in <c>sc.ControlCand</c>; F1's seam skips the policy
    /// and takes it. A possessed person's candidate carries <c>Feature.Divine(Possessed)</c>, so the trace it opens is flagged
    /// as divinely controlled and the person may learn from it. Sees the think scratch only, never the true state. OWNER: D4.
    /// </summary>
    public static partial class DivinityMindHooks
    {
        /// <summary><c>Feature.Divine(Possessed 4)</c>.</summary>
        public static uint PossessedKey => Feature.Make(FeatureKind.Divine, DivineFeatureValue.Possessed);

        static partial void VerbsD4(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(EmbodiedVerb.AvatarTake, "take up (god's body)", VerbGroup.Equipment, NpcVerb.PickUp));
            into.Add(new VerbInfo(EmbodiedVerb.Give, "hand over (taken)", VerbGroup.CoopGive, NpcVerb.HandOver));
            into.Add(new VerbInfo(EmbodiedVerb.Gesture, "gesture (taken)", VerbGroup.Communication, NpcVerb.Gesture));
        }

        static partial void DescribeD4(uint f, ref string text)
        {
            if (text != null || Feature.KindOf(f) != FeatureKind.Divine) return;
            if (Feature.ValueOf(f) == DivineFeatureValue.Possessed) text = "taken by a god";
        }

        /// <summary>
        /// The controlled candidate (quota 4, after D2): a held heading's or MoveTo's next node, the held action, or, with no
        /// input, the walk the body is on (so a body never stops mid-edge between inputs). Nothing for a body nobody controls.
        /// </summary>
        static partial void AddCandidatesD4(ThinkScratch sc)
        {
            var e = sc.Control;
            if (e.Kind != ControlKind.Possessed && e.Kind != ControlKind.AvatarBody) return;
            bool avatar = e.Kind == ControlKind.AvatarBody;
            uint key = avatar ? 0u : PossessedKey;
            int at = -1;
            if ((e.Input == ControlInputKind.Heading || e.Input == ControlInputKind.MoveTo) && e.MoveNode > 0) at = MoveCand(sc, e.MoveNode, key);
            else if (e.Input == ControlInputKind.Action) at = ActionCand(sc, in e, avatar, key);
            if (at < 0) at = Continuation(sc);
            if (at >= 0) sc.ControlCand = at;
        }

        private static int MoveCand(ThinkScratch sc, int node, uint key)
        {
            var r = default(DivineReading);
            return DivineActs.Candidate(sc, DivineAct.MoveTo, new MsgTopic { Kind = TopicKind.Place, Id = node }, key, ref r);
        }

        /// <summary>The walk the body is already on (a node it is approaching), offered again so the think keeps it.</summary>
        private static int Continuation(ThinkScratch sc)
        {
            var s = sc.Self;
            if (s.RunVerb != Verb.Approach || s.RunKind != IntentTarget.Node) return -1;
            int node = s.RunTarget;
            if (node < 0 || !sc.Grid.IsLand(node)) return -1;
            int at = Thinker.AddCandidate(sc, Verb.Approach, IntentTarget.Node, 0, node, (byte)ObjClass.Spot, sc.Grid.Distance(s.Node, node));
            if (at < 0) at = Find(sc, Verb.Approach, IntentTarget.Node, 0, node);
            return at;
        }

        private static int ActionCand(ThinkScratch sc, in ControlEntry e, bool avatar, uint key)
        {
            var act = e.Act;
            var t = e.Target;
            if (act == DivineAct.Communicate) return GestureCand(sc, in t);
            if (avatar && act == DivineAct.Take) return AvatarTakeCand(sc, in t);
            if (act == DivineAct.Give) return GiveCand(sc, in t, own: !avatar, key);
            var topic = Embodiment.TopicOf(in t);
            if (act == DivineAct.Drop && t.Id == 0)
            {
                int slot = FirstCarried(sc, Ware.None, own: false);
                if (slot < 0) return -1;
                topic = new MsgTopic { Kind = TopicKind.Task, Id = sc.Carry[slot].Id, Sub = sc.Self.Node & 0xFFFFFF };
            }
            var r = default(DivineReading);
            return DivineActs.Candidate(sc, act, topic, key, ref r);
        }

        /// <summary>The avatar takes a lot (named, or the nearest its mind offered to pick up), its owner unchanged.</summary>
        private static int AvatarTakeCand(ThinkScratch sc, in DivineTarget t)
        {
            int id = t.Id, node = t.Node, cls = t.Radius;
            if (id == 0)
            {
                int best = -1;
                for (int i = 0; i < sc.CandCount; i++)
                {
                    var c = sc.Cand[i];
                    if ((c.Verb != Verb.PickUp && c.Verb != Verb.Equip) || c.Kind != IntentTarget.Lot) continue;
                    if (best < 0 || c.Dist < sc.Cand[best].Dist || c.Dist == sc.Cand[best].Dist && c.TargetId < sc.Cand[best].TargetId) best = i;
                }
                if (best < 0) return -1;
                id = sc.Cand[best].TargetId; node = sc.Cand[best].TargetNode; cls = sc.Cand[best].Class;
            }
            for (int i = 0; i < sc.CarryCount; i++) if (!sc.Carry[i].Empty && sc.Carry[i].Id == id) return -1; // already held
            int at = Thinker.AddCandidate(sc, EmbodiedVerb.AvatarTake, IntentTarget.Lot, id, node, (byte)cls, sc.Grid.Distance(sc.Self.Node, node));
            if (at < 0) at = Find(sc, EmbodiedVerb.AvatarTake, IntentTarget.Lot, id, node);
            return at;
        }

        /// <summary>
        /// A hand-over of a carried lot of the ware to a person it sees (named, or the nearest): a person gives only what it owns
        /// (never conjured), the avatar what it holds (it never owns anything).
        /// </summary>
        private static int GiveCand(ThinkScratch sc, in DivineTarget t, bool own, uint key)
        {
            int to = t.Id != 0 ? t.Id : NearestPerson(sc);
            int ware = t.Node != 0 ? t.Node : CarriedWare(sc, own);
            if (to == 0 || to == sc.Self.Id || ware == 0 || FirstCarried(sc, (Ware)ware, own) < 0) return -1;
            int seen = Seen(sc, to);
            if (seen < 0) return -1;
            var p = sc.People[seen];
            var w = (Ware)ware;
            int at = Thinker.AddCandidate(sc, EmbodiedVerb.Give, IntentTarget.Npc, to, p.Node, (byte)DivineActs.LotClass(w), p.Dist);
            if (at >= 0)
                Thinker.TargetFeatures(sc, at, Feature.Class(DivineActs.LotClass(w)), Feature.Category(Wares.IsFood(w) ? ObjCategory.Food : ObjCategory.Raw), key);
            else at = Find(sc, EmbodiedVerb.Give, IntentTarget.Npc, to, p.Node);
            return at;
        }

        /// <summary>A gesture to a listener it sees (or, listener 0, to whoever attends: a gesture at its own place).</summary>
        private static int GestureCand(ThinkScratch sc, in DivineTarget t)
        {
            if (t.Node <= 0 || t.Node > 31) return -1;
            int at;
            if (t.Id != 0)
            {
                int seen = Seen(sc, t.Id);
                if (seen < 0) return -1;
                at = Thinker.AddCandidate(sc, EmbodiedVerb.Gesture, IntentTarget.Npc, t.Id, sc.People[seen].Node, 0, sc.People[seen].Dist);
                if (at < 0) at = Find(sc, EmbodiedVerb.Gesture, IntentTarget.Npc, t.Id, sc.People[seen].Node);
            }
            else
            {
                at = Thinker.AddCandidate(sc, EmbodiedVerb.Gesture, IntentTarget.Node, 0, sc.Self.Node, (byte)ObjClass.Spot, 0);
                if (at < 0) at = Find(sc, EmbodiedVerb.Gesture, IntentTarget.Node, 0, sc.Self.Node);
            }
            return at;
        }

        private static int Find(ThinkScratch sc, byte verb, IntentTarget kind, int id, int node)
        {
            for (int i = 0; i < sc.CandCount; i++)
            {
                var c = sc.Cand[i];
                if (c.Verb == verb && c.Kind == kind && c.TargetId == id && c.TargetNode == node) return i;
            }
            return -1;
        }

        private static int Seen(ThinkScratch sc, int id)
        {
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == id) return i;
            return -1;
        }

        /// <summary>The nearest awake person in sight (the people are sorted by distance, then id), 0 for none.</summary>
        private static int NearestPerson(ThinkScratch sc)
        {
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.Body == BodyKind.Person && !p.Asleep && p.Id != sc.Self.Id) return p.Id;
            }
            return 0;
        }

        private static int FirstCarried(ThinkScratch sc, Ware w, bool own)
        {
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (l.Empty || w != Ware.None && l.Ware != w) continue;
                if (own && (l.Owner.Kind != OwnerKind.Npc || l.Owner.Id != sc.Self.Id)) continue;
                return i;
            }
            return -1;
        }

        private static int CarriedWare(ThinkScratch sc, bool own)
        {
            int i = FirstCarried(sc, Ware.None, own);
            return i < 0 ? 0 : (int)sc.Carry[i].Ware;
        }
    }
}
