using System;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Cognition
{
    /// <summary>
    /// One thing an NPC perceives now (layer 2, built by the population's senses): a class and its visible state at a
    /// node, never a true object id, except for things it can pick up or that it knows as its own (lots, its hut).
    /// </summary>
    public struct Sensed
    {
        public byte Class;
        public byte Category;
        /// <summary><see cref="ObjState"/>: stock band, or Mine / Others / Unfinished for huts.</summary>
        public byte State;
        public IntentTarget Kind;
        public int Node;
        public int Dist;
        /// <summary>A lot id (things lying), a hut id (huts it may use), a unit id (an enemy); 0 for places.</summary>
        public int Ref;
    }

    /// <summary>What the NPC feels and knows about itself now (built by the population from its own body and carry).</summary>
    public struct SelfView
    {
        public int Id;
        public int Hunger, Fatigue, Cold, Fear, Pain;
        public int Health, Nutrition;
        public bool CarryFood, CarryLog, CarryTool, CarryFull, CanCarryLog, CanCarryTool, Injured;
        public bool HasBed, HasSite;
        /// <summary>The node it stands on and the node of its hut and site (-1 none), with their ids.</summary>
        public int Node, HomeNode, SiteNode, HomeHut, SiteHut;
        public int PlaceKind;
        public bool OnRoad, AtHome;
        public int TimeOfDay;
        /// <summary>It may found a hut where it stands (a free, buildable node).</summary>
        public bool CanFoundHere;
        /// <summary>Carried food lots (id and ware), at most 2 distinct wares offered.</summary>
        public int FoodLot0, FoodLot1;
        public Ware FoodWare0, FoodWare1;
        /// <summary>The running intent (learning verb, target kind and id), Verb 0 when idle.</summary>
        public byte RunVerb;
        public IntentTarget RunKind;
        public int RunTarget;
        /// <summary>Explore targets in two directions (land nodes about ExploreNodes away), -1 when none.</summary>
        public int ExploreA, ExploreB;
        public int Tick, Day;
        /// <summary>F0.7: its own life stage and household (0 none); written only by M4's <c>FillSelf</c> fields.</summary>
        public LifeStage Stage;
        public int Household;
        /// <summary>
        /// I-BS amendment (SHARED_CONTRACTS 12.33): the genome's instinct gains of its own body (BIOLOGICAL 4.4), permille (500-1500),
        /// filled by the sim from M4's phenotype; 0 for a person without one (an M3 NPC, <c>Households.Enabled = 0</c>): no gain.
        /// </summary>
        public int HungerGain, FearGain, SocialGain, NurtureGain;
    }

    /// <summary>One candidate intent (COMMUNICATION 3, 4.6): verb, target, its distance and perceived cost, and its context keys.</summary>
    public struct Candidate
    {
        public byte Verb;
        public NpcVerb Intent;
        public IntentTarget Kind;
        public int TargetId;
        public int TargetNode;
        public byte Class;
        public int Dist;
        public int Cost;
        /// <summary>Its full context (base + target + matching conjunctions, sorted) in <see cref="ThinkScratch.Keys"/>.</summary>
        public int KeyOffset, KeyCount;
        /// <summary>The target features alone, in <see cref="ThinkScratch.TargetKeys"/>.</summary>
        public int TargetOffset, TargetCount;
        public bool FromBelief;
        public bool IsRunning;
    }

    /// <summary>
    /// Reusable buffers of one think (not state): what is sensed, the base context, the candidates and their keys. The
    /// population fills <see cref="Seen"/> and <see cref="Self"/>; <see cref="Thinker"/> does the rest; the policy reads it
    /// through <see cref="NpcObservation"/> and <see cref="MindView"/>. No allocation per think.
    /// </summary>
    public sealed class ThinkScratch
    {
        /// <summary>
        /// <see cref="MaxCand"/> 24 since F0: M3 offers at most <see cref="M3Cand"/> = 16 (unchanged), M4 and M5 at most 4 each
        /// after it (<see cref="CognitionExtensions.AddCandidates"/>).
        /// </summary>
        public const int MaxSeen = 16, MaxCand = 24, M3Cand = 16, MaxBase = 12, MaxTarget = 6, MaxConj = 8, MaxPeople = 8, MaxHeard = 4;

        public SelfView Self;
        public Sensed[] Seen = new Sensed[MaxSeen];
        public int SeenCount;
        public int[] Attended = new int[8];
        public int AttendedCount;
        public int[] Salience = new int[MaxSeen];

        public uint[] Base = new uint[MaxBase];
        public int BaseCount;
        public int[] Armed = new int[64];
        public int ArmedCount;

        public Candidate[] Cand = new Candidate[MaxCand];
        public int CandCount;
        /// <summary>How many candidates may exist now (M3's 16; raised by the dispatch for each module's quota).</summary>
        public int CandLimit = M3Cand;

        /// <summary>F0.7: the people in sight (nearest first, at most <see cref="MaxPeople"/>), filled by the sim before Think.</summary>
        public SeenPerson[] People = new SeenPerson[MaxPeople];
        public int PeopleCount;
        /// <summary>F0.8: the messages this mind decoded this think (M5 fills it; empty otherwise).</summary>
        public Comms.DecodedMessage[] Heard = new Comms.DecodedMessage[MaxHeard];
        public int HeardCount;
        /// <summary>F0.7: a copy of its own carry (the lots and their owners), <see cref="CarryCount"/> slots.</summary>
        public ItemLot[] Carry = new ItemLot[8];
        public int CarryCount;
        public uint[] Keys = new uint[MaxCand * AssociationStore.MaxKeys];
        public uint[] TargetKeys = new uint[MaxCand * MaxTarget];

        /// <summary>The NPC's mind and the rules it decides by, for <see cref="MindView"/>.</summary>
        public NpcMind Mind;
        public ReadOnlyArray<Instinct> Instincts;
        public int[] Traits;
        public LearnTraits Learn;
        public LearningTuning LearningTuning;
        public PopulationTuning Tuning;
        public NodeGrid Grid;
    }

    /// <summary>
    /// The pure part of thinking (COMMUNICATION_AND_COGNITION 4.1 steps 2-5): update beliefs from what is sensed, attend
    /// the top K salient things (goal-driven), build the base context (drive bands, self, place, time, situations), and
    /// the candidates (from attended things, remembered places and the NPC's own options) with their target features and
    /// matching conjunctions. Reads only the scratch and the mind: never the true state.
    /// </summary>
    public static class Thinker
    {
        public static void Think(ThinkScratch sc)
        {
            UpdateBeliefs(sc);
            Attend(sc);
            CognitionExtensions.Recognise(sc);
            BuildBase(sc);
            sc.ArmedCount = Learner.Arm(sc.Mind.Store, sc.Base, sc.BaseCount, sc.Armed);
            CognitionExtensions.FillHeard(sc);
            BuildCandidates(sc);
        }

        // ---------------- beliefs (4.4) ----------------

        private static BeliefKind KindOfClass(int cls)
        {
            switch (cls)
            {
                case ObjClass.BerryBush: case ObjClass.RootPatch: case ObjClass.FishSpot: case ObjClass.Nightshade: return BeliefKind.Food;
                case ObjClass.WoodStand: return BeliefKind.Wood;
                case ObjClass.LogPile: return BeliefKind.Pile;
                case ObjClass.Hut: case ObjClass.HutSite: return BeliefKind.Hut;
                case ObjClass.Axe: return BeliefKind.Tool;
                default: return BeliefKind.None;
            }
        }

        /// <summary>The belief kind of a sensed thing: a bin (I-BS amendment 12.32) is a store belief whatever its class; else by class.</summary>
        private static BeliefKind KindOf(in Sensed t) => t.Kind == IntentTarget.Bin ? BeliefKind.Store : KindOfClass(t.Class);

        public static void UpdateBeliefs(ThinkScratch sc)
        {
            var b = sc.Mind.Beliefs;
            var self = sc.Self;
            int sight = self.TimeOfDay == TimeOfDay.Night ? sc.Tuning.SightNight : sc.Tuning.SightDay;
            // Places it should see from here and does not: the thing is gone (a pile taken, a lot picked up).
            for (int i = 0; i < b.Count;)
            {
                var bel = b.Slots[i];
                int d = sc.Grid.Distance(self.Node, bel.Subject);
                if (d <= sight - 1 && sc.SeenCount < ThinkScratch.MaxSeen)
                {
                    bool seen = false;
                    for (int s = 0; s < sc.SeenCount; s++)
                        if (sc.Seen[s].Node == bel.Subject && KindOf(sc.Seen[s]) == bel.Kind) { seen = true; break; }
                    if (!seen) { b.RemoveAt(i); continue; }
                }
                i++;
            }
            for (int s = 0; s < sc.SeenCount; s++)
            {
                var t = sc.Seen[s];
                var kind = KindOf(t);
                if (kind == BeliefKind.None) continue;
                int reference = kind == BeliefKind.Hut || kind == BeliefKind.Tool || kind == BeliefKind.Store ? t.Ref : 0;
                b.Upsert(kind, t.Node, t.Class, t.State, reference, self.Tick);
            }
            // its own hut and site are always known
            if (self.HomeHut != 0 && self.HomeNode >= 0) b.Upsert(BeliefKind.Hut, self.HomeNode, ObjClass.Hut, ObjState.Mine, self.HomeHut, self.Tick);
            if (self.SiteHut != 0 && self.SiteNode >= 0) b.Upsert(BeliefKind.Hut, self.SiteNode, ObjClass.HutSite, ObjState.Unfinished, self.SiteHut, self.Tick);
        }

        // ---------------- attention (4.3) ----------------

        public static void Attend(ThinkScratch sc)
        {
            var self = sc.Self;
            var st = sc.Mind.Store;
            int strongest = Math.Max(Math.Max(self.Hunger, self.Fatigue), Math.Max(self.Fear, self.Pain));
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                int s = 0;
                switch (t.Category)
                {
                    case ObjCategory.Food: s += self.Hunger; if (t.State == ObjState.StockEmpty) s -= 300; break;
                    case ObjCategory.Shelter: s += self.Fatigue + (self.HasBed ? 0 : 200); break;
                    case ObjCategory.Raw: s += self.HasBed ? 50 : 250; break;
                    case ObjCategory.Tool:
                        // goal-driven attention: a tool matters while gathering is the goal (COMMUNICATION 4.3)
                        s += self.CarryTool ? 0 : 150 + (self.RunVerb == Verb.Gather || !self.HasBed ? 200 : 0);
                        break;
                    case ObjCategory.Danger: s += 800 + self.Fear; break;
                }
                if (sc.Mind.Beliefs.IndexOf(KindOf(t), t.Node) < 0) s += 150; // novelty
                // learned salience (NPC_LEARNING 4.9): things that have mattered catch the eye
                s += LearnedSalience(st, t.Category);
                s -= t.Dist * 10;
                // the goal in progress attends its own target first
                if (self.RunVerb != 0 && (t.Node == self.RunTarget || t.Ref != 0 && t.Ref == self.RunTarget)) s += 5000;
                sc.Salience[i] = s;
            }
            // top K by salience, ties by sensed order
            int k = Math.Min(sc.Tuning.AttendK, sc.SeenCount);
            sc.AttendedCount = 0;
            for (int pick = 0; pick < k; pick++)
            {
                int best = -1;
                for (int i = 0; i < sc.SeenCount; i++)
                {
                    bool taken = false;
                    for (int j = 0; j < sc.AttendedCount; j++) if (sc.Attended[j] == i) { taken = true; break; }
                    if (taken) continue;
                    if (best < 0 || sc.Salience[i] > sc.Salience[best]) best = i;
                }
                if (best < 0) break;
                sc.Attended[sc.AttendedCount++] = best;
            }
            var m = sc.Mind;
            m.AttendedCount = Math.Min(sc.AttendedCount, m.Attended.Length);
            for (int i = 0; i < m.AttendedCount; i++)
            {
                var t = sc.Seen[sc.Attended[i]];
                m.Attended[i] = new AttendedThing { Class = t.Class, Node = t.Node, Salience = sc.Salience[sc.Attended[i]] };
            }
        }

        /// <summary><c>max over verbs |Ltw(ObjCategory(c), verb)| / 8</c> (NPC_LEARNING 4.9).</summary>
        public static int LearnedSalience(AssociationStore st, int category)
        {
            if (st.Mode == LearningMode.Off || category == 0) return 0;
            uint key = Feature.Category(category);
            int at = st.Find(key, 0);
            if (at < 0) at = ~at;
            int best = 0;
            for (int i = at; i < st.Count && st.Entries[i].Key == key; i++)
            {
                int a = Fx.Abs(st.Entries[i].Ltw);
                if (a > best) best = a;
            }
            return best / 8;
        }

        // ---------------- base context (4.5) ----------------

        public static int Band(PopulationTuning tu, int drive, int value)
        {
            int comfort = Comfort(tu, drive);
            if (value <= comfort) return 0;
            if (value < tu.BandHigh) return 1;
            if (value < tu.BandPain) return 2;
            return 3;
        }

        public static int Comfort(PopulationTuning tu, int drive)
        {
            switch (drive)
            {
                case Drive.Hunger: return tu.ComfortHunger;
                case Drive.Fatigue: return tu.ComfortFatigue;
                case Drive.Cold: return tu.ComfortCold;
                case Drive.Fear: return tu.ComfortFear;
                case Drive.Pain: return tu.ComfortPain;
                default: return 1000;
            }
        }

        public static int DriveValue(in SelfView s, int d)
        {
            switch (d)
            {
                case Drive.Hunger: return s.Hunger;
                case Drive.Fatigue: return s.Fatigue;
                case Drive.Cold: return s.Cold;
                case Drive.Fear: return s.Fear;
                default: return s.Pain;
            }
        }

        private static void AddBase(ThinkScratch sc, uint f)
        {
            if (sc.BaseCount < ThinkScratch.MaxBase) sc.Base[sc.BaseCount++] = f;
        }

        public static void BuildBase(ThinkScratch sc)
        {
            var s = sc.Self;
            sc.BaseCount = 0;
            for (int d = 0; d < Drive.Count; d++)
            {
                int band = Band(sc.Tuning, d, DriveValue(s, d));
                if (band == 0) continue;
                AddBase(sc, Feature.DriveBand(d, band));
                if (band >= 2) AddBase(sc, Feature.DriveActive(d));
            }
            if (s.CarryFood) AddBase(sc, Feature.Self(SelfFeature.CarryingFood));
            if (s.CarryLog) AddBase(sc, Feature.Self(SelfFeature.CarryingLog));
            if (s.CarryTool) AddBase(sc, Feature.Self(SelfFeature.CarryingTool));
            if (s.Injured) AddBase(sc, Feature.Self(SelfFeature.Injured));
            AddBase(sc, Feature.Self(s.HasBed ? SelfFeature.HasBed : SelfFeature.NoBed));
            if (s.HasSite) AddBase(sc, Feature.Self(SelfFeature.HasSite));
            AddBase(sc, Feature.Place(s.AtHome ? PlaceKind.Home : s.OnRoad ? PlaceKind.Road : s.PlaceKind));
            AddBase(sc, Feature.Time(s.TimeOfDay));
            // situations
            bool foodSeen = false, enemy = false;
            for (int i = 0; i < sc.SeenCount; i++)
            {
                if (sc.Seen[i].Category == ObjCategory.Food && sc.Seen[i].State != ObjState.StockEmpty) foodSeen = true;
                if (sc.Seen[i].Category == ObjCategory.Danger) enemy = true;
            }
            bool foodBelieved = false;
            var b = sc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++) if (b.Slots[i].Kind == BeliefKind.Food && b.Slots[i].Stock != ObjState.StockEmpty) { foodBelieved = true; break; }
            if (enemy) AddBase(sc, Feature.Situation(SituationKind.EnemyInSight));
            if (!foodSeen && foodBelieved) AddBase(sc, Feature.Situation(SituationKind.FoodRemembered));
            if (!foodSeen && !foodBelieved && !s.CarryFood) AddBase(sc, Feature.Situation(SituationKind.NoFoodKnown));
            CognitionExtensions.AddBase(sc);
            Array.Sort(sc.Base, 0, sc.BaseCount);
        }

        // ---------------- candidates (4.6) ----------------

        private static int AddCand(ThinkScratch sc, byte verb, IntentTarget kind, int id, int node, byte cls, int dist, bool fromBelief)
        {
            if (sc.CandCount >= sc.CandLimit) return -1;
            for (int i = 0; i < sc.CandCount; i++)
            {
                var c = sc.Cand[i];
                if (c.Verb == verb && c.Kind == kind && c.TargetId == id && c.TargetNode == node) return -1;
            }
            int at = sc.CandCount++;
            sc.Cand[at] = new Candidate
            {
                Verb = verb, Intent = VerbTable.IntentOf(verb), Kind = kind, TargetId = id, TargetNode = node, Class = cls, Dist = dist,
                FromBelief = fromBelief, TargetOffset = at * ThinkScratch.MaxTarget, KeyOffset = at * AssociationStore.MaxKeys,
                IsRunning = sc.Self.RunVerb == verb && sc.Self.RunKind == kind && sc.Self.RunTarget == (kind == IntentTarget.Node ? node : id),
            };
            return at;
        }

        /// <summary>
        /// Adds a candidate for a later module's hook (F0): its verb, target and distance. Returns its index, or -1 when the
        /// module's quota is used up or an identical candidate exists. Give it target features with <see cref="TargetFeatures"/>.
        /// </summary>
        public static int AddCandidate(ThinkScratch sc, byte verb, IntentTarget kind, int id, int node, byte cls, int dist, bool fromBelief = false) =>
            AddCand(sc, verb, kind, id, node, cls, dist, fromBelief);

        /// <summary>
        /// Sets a candidate's target features (up to <see cref="ThinkScratch.MaxTarget"/> keys, 0 skipped): the M3 kinds plus
        /// F0's person and message kinds (<see cref="Feature.NpcRel"/>, <see cref="Feature.NpcGroup"/>, <see cref="Feature.NpcRef"/>,
        /// <see cref="Feature.MsgAct"/>).
        /// </summary>
        public static void TargetFeatures(ThinkScratch sc, int at, uint a, uint b = 0, uint c = 0, uint d = 0, uint e = 0, uint f = 0)
        {
            if (at < 0) return;
            ref var cand = ref sc.Cand[at];
            int o = cand.TargetOffset, n = 0;
            if (a != 0) sc.TargetKeys[o + n++] = a;
            if (b != 0) sc.TargetKeys[o + n++] = b;
            if (c != 0) sc.TargetKeys[o + n++] = c;
            if (d != 0) sc.TargetKeys[o + n++] = d;
            if (e != 0) sc.TargetKeys[o + n++] = e;
            if (f != 0) sc.TargetKeys[o + n++] = f;
            Array.Sort(sc.TargetKeys, o, n);
            cand.TargetCount = n;
        }

        private static void Target(ThinkScratch sc, int at, int cls, int cat, int state, int placeRefNode)
        {
            if (at < 0) return;
            ref var c = ref sc.Cand[at];
            int o = c.TargetOffset, n = 0;
            if (cls != 0) sc.TargetKeys[o + n++] = Feature.Class(cls);
            if (cat != 0) sc.TargetKeys[o + n++] = Feature.Category(cat);
            if (state != 0) sc.TargetKeys[o + n++] = Feature.State(state);
            if (placeRefNode >= 0) sc.TargetKeys[o + n++] = Feature.PlaceRef(placeRefNode);
            Array.Sort(sc.TargetKeys, o, n);
            c.TargetCount = n;
        }

        public static void BuildCandidates(ThinkScratch sc)
        {
            var s = sc.Self;
            var tu = sc.Tuning;
            sc.CandCount = 0;

            // the running intent first, so its candidate keeps its place while it lasts (goal-driven attention)
            // -- it is re-offered below from what is sensed or believed; nothing is offered that is neither.

            // attended things
            for (int a = 0; a < sc.AttendedCount; a++) OfferSensed(sc, sc.Seen[sc.Attended[a]]);
            // the running intent's target, if sensed but not attended
            if (s.RunVerb != 0)
                for (int i = 0; i < sc.SeenCount; i++)
                {
                    var t = sc.Seen[i];
                    if (t.Node == s.RunTarget || t.Ref != 0 && t.Ref == s.RunTarget) OfferSensed(sc, t);
                }

            // carried food
            if (s.FoodLot0 != 0) Target(sc, AddCand(sc, Verb.Eat, IntentTarget.Lot, s.FoodLot0, s.Node, ObjClass.FoodLot, 0, false), FoodClass(s.FoodWare0), ObjCategory.Food, ObjState.Mine, -1);
            if (s.FoodLot1 != 0) Target(sc, AddCand(sc, Verb.Eat, IntentTarget.Lot, s.FoodLot1, s.Node, ObjClass.FoodLot, 0, false), FoodClass(s.FoodWare1), ObjCategory.Food, ObjState.Mine, -1);

            // remembered places not in sight
            OfferBeliefs(sc);

            // own options
            Target(sc, AddCand(sc, Verb.Rest, IntentTarget.Node, 0, s.Node, ObjClass.Spot, 0, false), ObjClass.Spot, ObjCategory.Ground, 0, -1);
            if (CanSleep(s, tu))
                Target(sc, AddCand(sc, Verb.Sleep, IntentTarget.Node, 0, s.Node, ObjClass.Spot, 0, false), ObjClass.Spot, ObjCategory.Ground, 0, -1);
            if (!s.HasBed && !s.HasSite && s.CarryLog && s.CanFoundHere)
                Target(sc, AddCand(sc, Verb.Build, IntentTarget.Node, 0, s.Node, ObjClass.HutSite, 0, false), ObjClass.HutSite, ObjCategory.Shelter, ObjState.Unfinished, -1);
            if (s.ExploreA >= 0) Target(sc, AddCand(sc, Verb.Explore, IntentTarget.Node, 0, s.ExploreA, ObjClass.Unknown, sc.Grid.Distance(s.Node, s.ExploreA), false), ObjClass.Unknown, 0, 0, -1);
            if (s.ExploreB >= 0) Target(sc, AddCand(sc, Verb.Explore, IntentTarget.Node, 0, s.ExploreB, ObjClass.Unknown, sc.Grid.Distance(s.Node, s.ExploreB), false), ObjClass.Unknown, 0, 0, -1);

            // later modules' candidates (M4, then M5), each within its quota
            CognitionExtensions.AddCandidates(sc);

            // costs and full contexts
            for (int i = 0; i < sc.CandCount; i++)
            {
                ref var c = ref sc.Cand[i];
                int walk = c.Dist * tu.WalkCostPerNode;
                walk = Fx.MulDiv(walk, 1000 + s.Fatigue, 1000);
                c.Cost = walk + (s.RunVerb != 0 && !c.IsRunning ? tu.SwitchCost : 0);
                // context = base U target (sorted merge) + conjunctions
                int o = c.KeyOffset, n = 0, bi = 0, ti = 0;
                while ((bi < sc.BaseCount || ti < c.TargetCount) && n < AssociationStore.MaxKeys - ThinkScratch.MaxConj)
                {
                    uint bk = bi < sc.BaseCount ? sc.Base[bi] : uint.MaxValue;
                    uint tk = ti < c.TargetCount ? sc.TargetKeys[c.TargetOffset + ti] : uint.MaxValue;
                    if (bk == tk) { sc.Keys[o + n++] = bk; bi++; ti++; }
                    else if (bk < tk) { sc.Keys[o + n++] = bk; bi++; }
                    else { sc.Keys[o + n++] = tk; ti++; }
                }
                n += Learner.MatchConj(sc.Mind.Store, sc.Armed, sc.ArmedCount, sc.Base, sc.BaseCount, sc.TargetKeys, c.TargetOffset, c.TargetCount,
                    sc.Keys, o + n, ThinkScratch.MaxConj);
                c.KeyCount = n;
            }
        }

        /// <summary>One can fall asleep only when tired enough; when starving, only when very tired (hunger would wake one at once otherwise).</summary>
        public static bool CanSleep(in SelfView s, PopulationTuning tu) => s.Fatigue >= tu.SleepFrom && (s.Hunger < tu.WakeHunger || s.Fatigue >= tu.BandHigh);

        private static int FoodClass(Ware w)
        {
            switch (w)
            {
                case Ware.Berries: return ObjClass.BerryBush;
                case Ware.Roots: return ObjClass.RootPatch;
                case Ware.Fish: return ObjClass.FishSpot;
                case Ware.Nightshade: return ObjClass.Nightshade;
                default: return ObjClass.FoodLot;
            }
        }

        private static void OfferSensed(ThinkScratch sc, in Sensed t)
        {
            var s = sc.Self;
            var tu = sc.Tuning;
            switch (t.Class)
            {
                case ObjClass.BerryBush: case ObjClass.RootPatch: case ObjClass.FishSpot: case ObjClass.Nightshade:
                    if (t.State == ObjState.StockEmpty) break; // seen empty: nothing to eat or pick there now
                    Target(sc, AddCand(sc, Verb.Eat, IntentTarget.Node, 0, t.Node, t.Class, t.Dist, false), t.Class, ObjCategory.Food, t.State, t.Node);
                    if (!s.CarryFull) Target(sc, AddCand(sc, Verb.ForageAt, IntentTarget.Node, 0, t.Node, t.Class, t.Dist, false), t.Class, ObjCategory.Food, t.State, t.Node);
                    break;
                case ObjClass.WoodStand: case ObjClass.LogPile:
                    if (s.CanCarryLog) Target(sc, AddCand(sc, Verb.Gather, IntentTarget.Node, 0, t.Node, t.Class, t.Dist, false), t.Class, ObjCategory.Raw, t.State, t.Node);
                    break;
                case ObjClass.Hut:
                    if (t.State == ObjState.Mine && CanSleep(s, tu))
                        Target(sc, AddCand(sc, Verb.Sleep, IntentTarget.Hut, t.Ref, t.Node, t.Class, t.Dist, false), ObjClass.Hut, ObjCategory.Shelter, ObjState.Mine, t.Node);
                    break;
                case ObjClass.HutSite:
                    // its own site, or a site of its faction with a free bed, when it has no bed (contributors get rights)
                    if (t.Ref != 0 && (t.Ref == s.SiteHut || !s.HasBed && !s.HasSite))
                        Target(sc, AddCand(sc, Verb.Build, IntentTarget.Hut, t.Ref, t.Node, t.Class, t.Dist, false), ObjClass.HutSite, ObjCategory.Shelter, t.State, t.Node);
                    break;
                case ObjClass.Axe:
                    if (!s.CarryTool && s.CanCarryTool) Target(sc, AddCand(sc, Verb.Equip, IntentTarget.Lot, t.Ref, t.Node, t.Class, t.Dist, false), ObjClass.Axe, ObjCategory.Tool, 0, -1);
                    break;
                case ObjClass.FoodLot:
                    if (!s.CarryFull) Target(sc, AddCand(sc, Verb.PickUp, IntentTarget.Lot, t.Ref, t.Node, t.Class, t.Dist, false), ObjClass.FoodLot, ObjCategory.Food, 0, -1);
                    break;
                case ObjClass.LogLot:
                    if (s.CanCarryLog) Target(sc, AddCand(sc, Verb.PickUp, IntentTarget.Lot, t.Ref, t.Node, t.Class, t.Dist, false), ObjClass.LogLot, ObjCategory.Raw, 0, -1);
                    break;
                case ObjClass.EnemySoldier:
                    Target(sc, AddCand(sc, Verb.Flee, IntentTarget.Unit, t.Ref, t.Node, t.Class, 0, false), ObjClass.EnemySoldier, ObjCategory.Danger, 0, -1);
                    break;
                case ObjClass.LabButton:
                    break;
            }
        }

        private static bool SeenAt(ThinkScratch sc, int node)
        {
            for (int i = 0; i < sc.SeenCount; i++) if (sc.Seen[i].Node == node) return true;
            return false;
        }

        /// <summary>Remembered places out of sight: the 3 best food places, the best wood place, the own hut and site.</summary>
        private static void OfferBeliefs(ThinkScratch sc)
        {
            var s = sc.Self;
            var tu = sc.Tuning;
            var b = sc.Mind.Beliefs;
            for (int pass = 0; pass < 3; pass++)
            {
                int best = -1, bestScore = int.MinValue;
                for (int i = 0; i < b.Count; i++)
                {
                    var bel = b.Slots[i];
                    if (bel.Kind != BeliefKind.Food || SeenAt(sc, bel.Subject)) continue;
                    bool already = false;
                    for (int c = 0; c < sc.CandCount; c++) if (sc.Cand[c].FromBelief && sc.Cand[c].TargetNode == bel.Subject) { already = true; break; }
                    if (already) continue;
                    int d = sc.Grid.Distance(s.Node, bel.Subject);
                    if (d > tu.RecallNodes) continue;
                    bool running = s.RunVerb == Verb.ForageAt && s.RunTarget == bel.Subject;
                    int score = bel.Confidence - d * 20 + (bel.Stock == ObjState.StockEmpty ? -400 : 0) + (running ? 5000 : 0);
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                if (best < 0) break;
                var f = b.Slots[best];
                int at = AddCand(sc, Verb.ForageAt, IntentTarget.Node, 0, f.Subject, f.Class, sc.Grid.Distance(s.Node, f.Subject), true);
                Target(sc, at, f.Class, ObjCategory.Food, f.Stock, f.Subject);
            }
            // wood
            if (s.CanCarryLog)
            {
                int best = -1, bestScore = int.MinValue;
                for (int i = 0; i < b.Count; i++)
                {
                    var bel = b.Slots[i];
                    if (bel.Kind != BeliefKind.Wood && bel.Kind != BeliefKind.Pile || SeenAt(sc, bel.Subject)) continue;
                    if (bel.Stock == ObjState.StockEmpty) continue;
                    int d = sc.Grid.Distance(s.Node, bel.Subject);
                    if (d > tu.RecallNodes) continue;
                    bool running = s.RunVerb == Verb.Gather && s.RunTarget == bel.Subject;
                    int score = bel.Confidence - d * 20 + (running ? 5000 : 0);
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                if (best >= 0)
                {
                    var w = b.Slots[best];
                    Target(sc, AddCand(sc, Verb.Gather, IntentTarget.Node, 0, w.Subject, w.Class, sc.Grid.Distance(s.Node, w.Subject), true), w.Class, ObjCategory.Raw, w.Stock, w.Subject);
                }
            }
            // own hut (sleep) and own site (build), when out of sight
            if (s.HomeHut != 0 && s.HomeNode >= 0 && !SeenAt(sc, s.HomeNode) && CanSleep(s, tu))
                Target(sc, AddCand(sc, Verb.Sleep, IntentTarget.Hut, s.HomeHut, s.HomeNode, ObjClass.Hut, sc.Grid.Distance(s.Node, s.HomeNode), true),
                    ObjClass.Hut, ObjCategory.Shelter, ObjState.Mine, s.HomeNode);
            if (s.SiteHut != 0 && s.SiteNode >= 0 && !SeenAt(sc, s.SiteNode))
                Target(sc, AddCand(sc, Verb.Build, IntentTarget.Hut, s.SiteHut, s.SiteNode, ObjClass.HutSite, sc.Grid.Distance(s.Node, s.SiteNode), true),
                    ObjClass.HutSite, ObjCategory.Shelter, ObjState.Unfinished, s.SiteNode);
        }

        // ---------------- the M3.12 check ----------------

        /// <summary>
        /// True when a target node or reference was perceived now or is believed (acceptance M3.12), independent of the candidate list.
        /// I-BS amendment (SHARED_CONTRACTS 12.31): a person target (<see cref="IntentTarget.Npc"/>, the handle in <paramref name="id"/>)
        /// is known when it is in sight now (<c>sc.People</c>) or remembered by the mind (believed kin, M4; social memory, M5);
        /// a bin target (<see cref="IntentTarget.Bin"/>, 12.32) when a sensed bin or a store belief carries its id.
        /// </summary>
        public static bool Known(ThinkScratch sc, IntentTarget kind, int id, int node)
        {
            if (kind == IntentTarget.None) return true;
            if (kind == IntentTarget.Npc)
            {
                if (id == 0) return false;
                for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == id) return true;
                return CognitionExtensions.Remembers(sc, id);
            }
            if (kind == IntentTarget.Bin)
            {
                if (id == 0) return false;
                for (int i = 0; i < sc.SeenCount; i++) if (sc.Seen[i].Kind == IntentTarget.Bin && sc.Seen[i].Ref == id) return true;
                var bb = sc.Mind.Beliefs;
                for (int i = 0; i < bb.Count; i++) if (bb.Slots[i].Kind == BeliefKind.Store && bb.Slots[i].Ref == id) return true;
                return false;
            }
            var s = sc.Self;
            if (kind == IntentTarget.Node && (node == s.Node || node == s.ExploreA || node == s.ExploreB)) return true; // its own spot and its own directions
            if (kind == IntentTarget.Lot && (id == s.FoodLot0 || id == s.FoodLot1)) return true;                        // its own carry
            if (kind == IntentTarget.Hut && (id == s.HomeHut || id == s.SiteHut)) return true;                         // its own hut and site
            for (int i = 0; i < sc.SeenCount; i++)
            {
                var t = sc.Seen[i];
                if (kind == IntentTarget.Node && t.Node == node) return true;
                if (kind != IntentTarget.Node && t.Ref == id) return true;
            }
            var b = sc.Mind.Beliefs;
            for (int i = 0; i < b.Count; i++)
            {
                if (kind == IntentTarget.Node && b.Slots[i].Subject == node) return true;
                if (kind != IntentTarget.Node && b.Slots[i].Ref == id && id != 0) return true;
            }
            return false;
        }
    }
}
