using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>The candidate shape of a divine act (contracts 6.1): learning verb, intent verb and target kind.</summary>
    public readonly struct ActShape
    {
        public readonly byte Verb;
        public readonly NpcVerb Intent;
        public readonly IntentTarget Kind;
        public ActShape(byte verb, NpcVerb intent, IntentTarget kind) { Verb = verb; Intent = intent; Kind = kind; }
        public bool IsNone => Verb == 0;
    }

    /// <summary>D2's learning verbs (contracts 14: Divinity rows 116-123; D6 holds 116-118, D2 takes 119).</summary>
    public static class DivineVerb
    {
        /// <summary>Hand a named ware to a named person (the instructed hand-over: one <c>Goods.Move</c> HandOver, basis Gift).</summary>
        public const byte Give = 119;
    }

    /// <summary>
    /// Contracts 6.1 (DIVINE_PLAYER 4.4): the divine action vocabulary mapped onto the NPC's own candidates. There is one
    /// implementation of each action, the NPC's: a mapped act becomes an ordinary candidate that goes through
    /// <c>Match</c> -> <c>Start</c> -> <c>Act</c>, with every physical check. OWNER: D2 (C packages add rows for their verbs).
    /// <para><b>Target convention</b> (the frozen <c>IssueSuggestion/IssueCommand/IssueCompulsion</c> carry one
    /// <see cref="DivineTarget"/>): the target names the <b>recipients</b> (with the scope) and, in its free fields, the act's
    /// <b>object</b>. For a person, household or organisation target <c>Node</c> is the act's place (0 = none) and
    /// <c>Radius</c> its object id (a lot for Take, Drop, Eat, UseTool; a hut for Sleep; a person for Approach and Give;
    /// 0 = none); for Give <c>Node</c> is the ware to hand over. For a location target the centre node is also the act's
    /// place. An act with no object matches any candidate of its verb the recipient builds itself ("gather!"), and a
    /// compulsion with no object takes the nearest such candidate.</para>
    /// <para>The Tier 1 subset on this branch: MoveTo, Approach, Inspect (look: go until it is in reach of the eye, the
    /// same walk as MoveTo), Gather, Take, Drop, Give, Eat, UseTool, Sleep, Retreat. Trade, Communicate and Treat (Tend) are
    /// gated by M5's and M4's own pending candidates (their Start refuses a candidate they did not build), so mapping them
    /// needs a seam in those modules: refused <c>act_unavailable</c> until then, like every later package's act.</para>
    /// </summary>
    public static class DivineActs
    {
        /// <summary>True when the act has a row on this branch (else <c>act_unavailable</c> at Validate).</summary>
        public static bool Available(DivineAct a)
        {
            switch (a)
            {
                case DivineAct.MoveTo: case DivineAct.Approach: case DivineAct.Inspect: case DivineAct.Gather: case DivineAct.Take:
                case DivineAct.Drop: case DivineAct.Give: case DivineAct.Eat: case DivineAct.UseTool: case DivineAct.Sleep: case DivineAct.Retreat:
                    return true;
                default: return false;
            }
        }

        /// <summary>
        /// <c>DivineActs.Map(act, target)</c>: the candidate shape for an act whose object is of <paramref name="obj"/> kind
        /// (a node, a lot, a hut, a person, or nothing). <see cref="ActShape.IsNone"/> when the pair has no row.
        /// </summary>
        public static ActShape Map(DivineAct act, IntentTarget obj)
        {
            switch (act)
            {
                case DivineAct.MoveTo: case DivineAct.Inspect: return obj == IntentTarget.Node ? new ActShape(Learning.Verb.Approach, NpcVerb.Move, IntentTarget.Node) : default;
                case DivineAct.Approach: return obj == IntentTarget.Npc ? new ActShape(Learning.Verb.Approach, NpcVerb.Move, IntentTarget.Npc) : default;
                case DivineAct.Gather: return obj == IntentTarget.Node || obj == IntentTarget.None ? new ActShape(Learning.Verb.Gather, NpcVerb.Gather, IntentTarget.Node) : default;
                case DivineAct.Take: return obj == IntentTarget.Lot || obj == IntentTarget.None ? new ActShape(Learning.Verb.PickUp, NpcVerb.PickUp, IntentTarget.Lot) : default;
                case DivineAct.Drop: return obj == IntentTarget.Lot ? new ActShape(Learning.Verb.Drop, NpcVerb.Interact, IntentTarget.Lot) : default;
                case DivineAct.Give: return obj == IntentTarget.Npc ? new ActShape(DivineVerb.Give, NpcVerb.HandOver, IntentTarget.Npc) : default;
                case DivineAct.Eat:
                    if (obj == IntentTarget.Lot) return new ActShape(Learning.Verb.Eat, NpcVerb.Eat, IntentTarget.Lot);
                    return obj == IntentTarget.Node || obj == IntentTarget.None ? new ActShape(Learning.Verb.Eat, NpcVerb.Eat, IntentTarget.Node) : default;
                case DivineAct.UseTool: return obj == IntentTarget.Lot || obj == IntentTarget.None ? new ActShape(Learning.Verb.Equip, NpcVerb.Equip, IntentTarget.Lot) : default;
                case DivineAct.Sleep:
                    if (obj == IntentTarget.Hut) return new ActShape(Learning.Verb.Sleep, NpcVerb.Sleep, IntentTarget.Hut);
                    return obj == IntentTarget.Node || obj == IntentTarget.None ? new ActShape(Learning.Verb.Sleep, NpcVerb.Sleep, IntentTarget.Node) : default;
                case DivineAct.Retreat: return new ActShape(Learning.Verb.Flee, NpcVerb.Flee, IntentTarget.Node);
                default: return default;
            }
        }

        /// <summary>D2's verb rows (contracts 6.1: <c>Approach</c> 7 and <c>Drop</c> 16 have no M3 row; D2's Give 119).</summary>
        public static readonly ReadOnlyArray<VerbInfo> Rows = new ReadOnlyArray<VerbInfo>(
            new VerbInfo(Learning.Verb.Approach, "go to", VerbGroup.Search, NpcVerb.Move),
            new VerbInfo(Learning.Verb.Drop, "set down", VerbGroup.Equipment, NpcVerb.Interact),
            new VerbInfo(DivineVerb.Give, "hand over (told)", VerbGroup.CoopGive, NpcVerb.HandOver));

        // ---------------- the object of an act (truth, at Apply) ----------------

        /// <summary>True for a recipient kind whose free fields carry the act's object (person, household, organisation).</summary>
        public static bool IsRecipientKind(DivineTargetKind k) => k == DivineTargetKind.Npc || k == DivineTargetKind.Household || k == DivineTargetKind.Org;

        /// <summary>What kind of object the target's free fields name for this act (<see cref="IntentTarget.None"/>: no object).</summary>
        public static IntentTarget ObjectKind(DivineAct act, in DivineTarget t)
        {
            if (t.Kind == DivineTargetKind.Location) return IntentTarget.Node;
            if (!IsRecipientKind(t.Kind)) return IntentTarget.None;
            switch (act)
            {
                case DivineAct.Approach: return t.Radius != 0 ? IntentTarget.Npc : IntentTarget.None;
                case DivineAct.Give: return t.Radius != 0 ? IntentTarget.Npc : IntentTarget.None;
                case DivineAct.Take: case DivineAct.Drop: case DivineAct.UseTool:
                    return t.Radius != 0 ? IntentTarget.Lot : IntentTarget.None;
                case DivineAct.Eat: return t.Radius != 0 ? IntentTarget.Lot : t.Node != 0 ? IntentTarget.Node : IntentTarget.None;
                case DivineAct.Sleep: return t.Radius != 0 ? IntentTarget.Hut : t.Node != 0 ? IntentTarget.Node : IntentTarget.None;
                case DivineAct.Retreat: return IntentTarget.Node;
                default: return t.Node != 0 ? IntentTarget.Node : IntentTarget.None;
            }
        }

        /// <summary>
        /// The act's object as a check of the true world at Validate/Apply (no mutation): <c>null</c> when valid, else a
        /// <see cref="DivineReasons"/> code. A lot or person named must exist (a lot may be anywhere; reach is the NPC's walk).
        /// </summary>
        public static string CheckObject(GodState s, DivineAct act, in DivineTarget t)
        {
            var obj = ObjectKind(act, t);
            if (Map(act, obj).IsNone) return obj == IntentTarget.None ? DivineReasons.BadTarget : DivineReasons.ActUnavailable;
            var p = s.Population;
            var g = s.Territory.Grid;
            switch (obj)
            {
                case IntentTarget.Node:
                    if (act == DivineAct.Retreat) return null;
                    return t.Node >= 0 && t.Node < g.Count && g.IsLand(t.Node) ? null : DivineReasons.BadTarget;
                case IntentTarget.Lot:
                    return LotNode(s, t.Radius, out _) != int.MinValue ? null : DivineReasons.BadTarget;
                case IntentTarget.Hut:
                    return p.HutById(t.Radius) != null ? null : DivineReasons.BadTarget;
                case IntentTarget.Npc:
                {
                    var o = p.Find(t.Radius);
                    if (o == null || !Bodies.IsPerson(o)) return DivineReasons.NotAPerson;
                    if (!o.Alive) return DivineReasons.TargetDead;
                    if (act == DivineAct.Give && (t.Node <= 0 || t.Node >= Wares.PersonalCount)) return DivineReasons.BadTarget;
                    return null;
                }
                default: return null;
            }
        }

        /// <summary>Where a lot is now (ground node, or the carrier's node) and its ware; <c>int.MinValue</c> when it exists nowhere.</summary>
        public static int LotNode(GodState s, int lotId, out Ware ware)
        {
            ware = Ware.None;
            if (lotId == 0) return int.MinValue;
            var p = s.Population;
            foreach (var gl in p.Lots) if (gl.Lot.Id == lotId) { ware = gl.Lot.Ware; return gl.Node; }
            foreach (var n in p.Npcs)
            {
                if (n.Carry == null) continue;
                foreach (var l in n.Carry) if (!l.Empty && l.Id == lotId) { ware = l.Ware; return n.Node; }
            }
            return int.MinValue;
        }

        /// <summary>The class a perceiver gives a ware's lot (as the population's senses do).</summary>
        public static int LotClass(Ware w) => w == Ware.Axe ? ObjClass.Axe : Wares.IsFood(w) ? ObjClass.FoodLot : w == Ware.Log ? ObjClass.LogLot : 0;

        /// <summary>
        /// The topic the god "shows" the recipient (truth at Apply, before decoding): a place (<c>Place</c>, node, Sub = the
        /// class of what is there), a thing (<c>Task</c>, Id = lot or hut id, Sub = node | class &lt;&lt; 24), a person (<c>Npc</c>,
        /// Sub = the ware of a Give), or nothing.
        /// </summary>
        public static MsgTopic TopicOf(GodState s, DivineAct act, in DivineTarget t)
        {
            var obj = ObjectKind(act, t);
            var p = s.Population;
            switch (obj)
            {
                case IntentTarget.Node:
                {
                    if (act == DivineAct.Retreat) return default;
                    int cls = 0;
                    foreach (var src in p.Sources) if (src.Node == t.Node) { cls = PopulationModule.ClassOf(src.Kind); break; }
                    return new MsgTopic { Kind = TopicKind.Place, Id = t.Node, Sub = cls };
                }
                case IntentTarget.Lot:
                {
                    int node = LotNode(s, t.Radius, out var w);
                    return new MsgTopic { Kind = TopicKind.Task, Id = t.Radius, Sub = (node < 0 ? 0 : node & 0xFFFFFF) | (LotClass(w) << 24) };
                }
                case IntentTarget.Hut:
                {
                    var h = p.HutById(t.Radius);
                    return new MsgTopic { Kind = TopicKind.Task, Id = t.Radius, Sub = ((h?.Node ?? 0) & 0xFFFFFF) | (ObjClass.Hut << 24) };
                }
                case IntentTarget.Npc:
                    return new MsgTopic { Kind = TopicKind.Npc, Id = t.Radius, Sub = act == DivineAct.Give ? t.Node : 0 };
                default: return default;
            }
        }

        // ---------------- decode noise (M5's model: one field read wrong) ----------------

        /// <summary>A similar act a misread produces (same object kind), or the act itself when it has none.</summary>
        public static DivineAct Similar(DivineAct a)
        {
            switch (a)
            {
                case DivineAct.MoveTo: return DivineAct.Inspect;
                case DivineAct.Inspect: return DivineAct.MoveTo;
                case DivineAct.Gather: return DivineAct.Eat;
                case DivineAct.Eat: return DivineAct.Gather;
                case DivineAct.Take: return DivineAct.UseTool;
                case DivineAct.UseTool: return DivineAct.Take;
                case DivineAct.Approach: return DivineAct.Give;
                case DivineAct.Give: return DivineAct.Approach;
                case DivineAct.Sleep: return DivineAct.Retreat;
                case DivineAct.Retreat: return DivineAct.Sleep;
                default: return a;
            }
        }

        /// <summary>
        /// One field read wrong (as M5's <c>Messages.Misread</c>): field 0 the act as a similar act, field 1 a place as a
        /// neighbouring node. False when that field has nothing to confuse (then the other field is tried).
        /// </summary>
        public static bool Misread(NodeGrid g, ref DecodedDivine d, int field, int dir)
        {
            for (int k = 0; k < 2; k++, field ^= 1)
            {
                if (field == 0)
                {
                    var a = Similar(d.Act);
                    if (a != d.Act) { d.Act = a; return true; }
                }
                else if (d.Topic.Kind == TopicKind.Place)
                {
                    int v = g.Neighbour(d.Topic.Id, dir % 6);
                    if (v >= 0 && g.IsLand(v)) { d.Topic.Id = v; d.Topic.Sub = 0; return true; }
                }
            }
            return false;
        }

        // ---------------- the candidate (mind side: the think scratch only) ----------------

        /// <summary>The object kind a decoded topic names.</summary>
        public static IntentTarget TopicObject(in MsgTopic t)
        {
            switch (t.Kind)
            {
                case TopicKind.Place: return IntentTarget.Node;
                case TopicKind.Npc: return IntentTarget.Npc;
                case TopicKind.Task: return (t.Sub >> 24) == ObjClass.Hut ? IntentTarget.Hut : IntentTarget.Lot;
                default: return IntentTarget.None;
            }
        }

        /// <summary>
        /// Builds (or finds) the candidate a decoded instruction names, through the NPC's own knowledge and physical state in
        /// the scratch: a blocked affordance (not held, cannot carry, not seen, nothing there) gives none. Fills
        /// <paramref name="r"/> with the shape that counts as following it. Returns the candidate index, -1 for none (a wild
        /// instruction returns the first matching candidate the mind built itself, or -1).
        /// </summary>
        public static int Candidate(ThinkScratch sc, DivineAct act, in MsgTopic topic, uint divineKey, ref DivineReading r)
        {
            var s = sc.Self;
            var obj = TopicObject(topic);
            if (act == DivineAct.Retreat) obj = IntentTarget.Node;
            var shape = Map(act, obj);
            r.Offered = false;
            if (shape.IsNone) return -1;
            r.Verb = shape.Verb; r.Verb2 = 0; r.TargetKind = shape.Kind; r.Wild = false;
            if (obj == IntentTarget.None)
            {
                // "gather!" with no object: any candidate of the verb the mind builds itself (Gather also covers ForageAt)
                r.Wild = true;
                if (act == DivineAct.Gather) r.Verb2 = Learning.Verb.ForageAt;
                int first = -1;
                for (int i = 0; i < sc.CandCount; i++)
                {
                    var c = sc.Cand[i];
                    if (c.Verb != r.Verb && c.Verb != r.Verb2) continue;
                    if (act == DivineAct.Eat && c.Kind != IntentTarget.Node && c.Kind != IntentTarget.Lot) continue;
                    AddKey(sc, i, divineKey);
                    if (first < 0 || c.Dist < sc.Cand[first].Dist) first = i;
                }
                r.Offered = first >= 0;
                return first;
            }
            byte verb = shape.Verb;
            int id = 0, node = 0, cls = 0, cat = 0, dist = 0;
            switch (obj)
            {
                case IntentTarget.Node:
                {
                    node = act == DivineAct.Retreat ? s.Node : topic.Id;
                    cls = topic.Sub;
                    if (node < 0 || !sc.Grid.IsLand(node)) return -1;
                    dist = sc.Grid.Distance(s.Node, node);
                    if (act == DivineAct.Gather)
                    {
                        bool food = cls == ObjClass.BerryBush || cls == ObjClass.RootPatch || cls == ObjClass.FishSpot || cls == ObjClass.Nightshade;
                        if (food) { verb = Learning.Verb.ForageAt; if (s.CarryFull) return -1; cat = ObjCategory.Food; }
                        else { if (!s.CanCarryLog) return -1; cat = ObjCategory.Raw; if (cls == 0) cls = ObjClass.WoodStand; }
                    }
                    else if (act == DivineAct.Eat)
                    {
                        bool food = cls == ObjClass.BerryBush || cls == ObjClass.RootPatch || cls == ObjClass.FishSpot || cls == ObjClass.Nightshade;
                        if (!food) return -1;
                        cat = ObjCategory.Food;
                    }
                    else if (act == DivineAct.Sleep)
                    {
                        if (node != s.Node || !Thinker.CanSleep(s, sc.Tuning)) return -1;
                        cls = ObjClass.Spot; cat = ObjCategory.Ground;
                    }
                    else { cls = ObjClass.Spot; cat = ObjCategory.Ground; }
                    break;
                }
                case IntentTarget.Lot:
                {
                    id = topic.Id;
                    node = topic.Sub & 0xFFFFFF;
                    cls = topic.Sub >> 24;
                    int carried = CarrySlot(sc, id);
                    if (act == DivineAct.Drop || act == DivineAct.Eat)
                    {
                        if (carried < 0) return -1;            // not held: nothing to set down or eat
                        node = s.Node; dist = 0;
                        if (act == DivineAct.Eat && !Wares.IsFood(sc.Carry[carried].Ware)) return -1;
                        cls = LotClass(sc.Carry[carried].Ware);
                    }
                    else
                    {
                        if (carried >= 0) return -1;            // already held
                        if (act == DivineAct.UseTool || cls == ObjClass.Axe)
                        {
                            if (s.CarryTool || !s.CanCarryTool) return -1;
                            verb = Learning.Verb.Equip; cls = ObjClass.Axe;
                        }
                        else if (cls == ObjClass.LogLot) { if (!s.CanCarryLog) return -1; }
                        else if (s.CarryFull) return -1;
                        dist = sc.Grid.Distance(s.Node, node);
                    }
                    cat = cls == ObjClass.Axe ? ObjCategory.Tool : cls == ObjClass.FoodLot ? ObjCategory.Food : ObjCategory.Raw;
                    break;
                }
                case IntentTarget.Hut:
                    id = topic.Id; node = topic.Sub & 0xFFFFFF; cls = ObjClass.Hut; cat = ObjCategory.Shelter;
                    if (id != s.HomeHut || !Thinker.CanSleep(s, sc.Tuning)) return -1;
                    dist = sc.Grid.Distance(s.Node, node);
                    break;
                case IntentTarget.Npc:
                {
                    id = topic.Id;
                    if (id == 0 || id == s.Id) return -1;
                    int seen = -1;
                    for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == id) { seen = i; break; }
                    if (seen < 0) return -1;                    // a person it cannot see: it does not know where to go
                    node = sc.People[seen].Node; dist = sc.People[seen].Dist;
                    cls = ObjClass.Unknown;
                    if (act == DivineAct.Give)
                    {
                        var w = (Ware)topic.Sub;
                        if (OwnLotOf(sc, w) < 0) return -1;     // holds none of it: never conjured
                        cls = LotClass(w); cat = Wares.IsFood(w) ? ObjCategory.Food : ObjCategory.Raw;
                    }
                    break;
                }
            }
            r.Verb = verb; r.TargetId = obj == IntentTarget.Node ? 0 : id; r.TargetNode = node;
            int at = Thinker.AddCandidate(sc, verb, shape.Kind, r.TargetId, node, (byte)cls, dist, false);
            if (at >= 0)
            {
                Thinker.TargetFeatures(sc, at, cls != 0 ? Feature.Class(cls) : 0, cat != 0 ? Feature.Category(cat) : 0,
                    obj == IntentTarget.Node && act != DivineAct.Retreat ? Feature.PlaceRef(node) : 0, divineKey);
            }
            else
            {
                for (int i = 0; i < sc.CandCount; i++)
                    if (r.Matches(sc.Cand[i].Verb, sc.Cand[i].Kind, sc.Cand[i].TargetId, sc.Cand[i].TargetNode) || Same(sc.Cand[i], verb, shape.Kind, r.TargetId, node)) { at = i; break; }
                if (at >= 0) AddKey(sc, at, divineKey);
            }
            r.Offered = at >= 0;
            return at;
        }

        private static bool Same(in Cognition.Candidate c, byte verb, IntentTarget kind, int id, int node) =>
            c.Verb == verb && c.Kind == kind && c.TargetId == id && c.TargetNode == node;

        /// <summary>Appends a key to a candidate's target features (sorted, while there is room).</summary>
        public static void AddKey(ThinkScratch sc, int at, uint key)
        {
            if (at < 0 || key == 0) return;
            ref var c = ref sc.Cand[at];
            int o = c.TargetOffset;
            for (int i = 0; i < c.TargetCount; i++) if (sc.TargetKeys[o + i] == key) return;
            if (c.TargetCount >= ThinkScratch.MaxTarget) return;
            sc.TargetKeys[o + c.TargetCount++] = key;
            System.Array.Sort(sc.TargetKeys, o, c.TargetCount);
        }

        private static int CarrySlot(ThinkScratch sc, int lotId)
        {
            for (int i = 0; i < sc.CarryCount; i++) if (!sc.Carry[i].Empty && sc.Carry[i].Id == lotId) return i;
            return -1;
        }

        /// <summary>A carried lot of the ware that the person owns itself (-1 none).</summary>
        public static int OwnLotOf(ThinkScratch sc, Ware w)
        {
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (!l.Empty && l.Ware == w && l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == sc.Self.Id) return i;
            }
            return -1;
        }
    }
}
