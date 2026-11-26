using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>One carried lot as the inspector shows it.</summary>
    public readonly struct CarriedItem
    {
        public readonly Ware Ware; public readonly int Count; public readonly int SpoilDay; public readonly int Uses;
        public CarriedItem(Ware w, int count, int spoilDay, int uses) { Ware = w; Count = count; SpoilDay = spoilDay; Uses = uses; }
        public override string ToString() => Count + " " + Wares.Name(Ware) + (Uses > 0 ? " (" + Uses + " uses)" : "");
    }

    /// <summary>One learned habit in words ("hungry: eat (strong, own experience)") with its weights.</summary>
    public readonly struct Habit
    {
        public readonly string Text; public readonly byte Verb; public readonly uint Key; public readonly int Stw, Ltw;
        public Habit(string text, byte verb, uint key, int stw, int ltw) { Text = text; Verb = verb; Key = key; Stw = stw; Ltw = ltw; }
        public override string ToString() => Text;
    }

    /// <summary>
    /// What a faction's records hold about one of its own people, from that person's own point of view (POPULATION 6, NPC_LEARNING
    /// 6, COMMUNICATION 6): body and drives, carry, home, what it is doing and why (the term breakdown of its last decision and
    /// the associations that carried it), what it attends to, its strongest beliefs, its learned habits. A plain copy: it
    /// holds no reference into the state and changes nothing.
    /// </summary>
    public sealed class NpcInspection
    {
        public int Id;
        public Faction Faction;
        public bool Alive;
        public DeathCause DeathCause;
        public int Node;
        /// <summary>Sim age in milli-years and the HUD age (sim year x 16).</summary>
        public long AgeMilli;
        public int HudAge;
        public bool Founder;
        public int Health, Nutrition, Injury;
        /// <summary>Hunger, fatigue, cold, fear, pain (permille).</summary>
        public int[] Drives = new int[Drive.Count];
        public List<CarriedItem> Carry = new List<CarriedItem>();
        public int HomeHut, SiteHut;
        public bool Asleep, InHut;
        /// <summary>The running intent in words ("forage at berry bush"), empty when idle.</summary>
        public string Doing = "";
        public string Policy;
        public LearningMode Mode;
        // the last decision and why
        public string LastDecision = "";
        public int Instinct, Learned, Social, Culture, Personality, Cost, Explore, Total, Candidates;
        /// <summary>The part of <see cref="Instinct"/> a need drives (the rest is situational instinct).</summary>
        public int Need;
        public bool LearnedChangedWinner, ExploreWon;
        /// <summary>The options it weighed at its last decision (perceived opportunities) with their score terms; the chosen one is marked.</summary>
        public List<Opportunity> Opportunities = new List<Opportunity>();
        /// <summary>Its last experiences with a reward, newest first.</summary>
        public List<Experience> Experiences = new List<Experience>();
        public string RunnerUp = "";
        /// <summary>The associations that contributed most to the last decision's learned value.</summary>
        public List<Habit> Why = new List<Habit>();
        public List<string> Attending = new List<string>();
        public List<string> Beliefs = new List<string>();
        /// <summary>Its strongest learned habits: top positive and top negative by long-term weight.</summary>
        public List<Habit> HabitsPositive = new List<Habit>(), HabitsNegative = new List<Habit>();
        public int Entries, Conjunctions, Recruits, Evictions, Updates;
        public int LearnedSharePermille, ExploreSharePermille;
        public int[] Traits = new int[Trait.Count];
    }

    /// <summary>One weighed option of the last decision: words, and the score terms (Need is the need-driven part of Instinct).</summary>
    public sealed class Opportunity
    {
        public string Text;
        public byte Verb, Class;
        public int Dist, Instinct, Need, Learned, Personality, Cost, Explore, Total;
        public bool Chosen, FromBelief;

        /// <summary>"eat berry bush (2 away): need +600, instinct +0, learned +120, personality +0, cost -40, explore +31 = 711".</summary>
        public string Breakdown => Text + " (" + Dist + " away" + (FromBelief ? ", remembered" : "") + "): need " + Sg(Need) + ", instinct " + Sg(Instinct - Need)
            + ", learned " + Sg(Learned) + ", personality " + Sg(Personality) + ", cost " + Sg(-Cost) + ", explore " + Sg(Explore) + " = " + Total;

        private static string Sg(int v) => v >= 0 ? "+" + v : v.ToString();
        public override string ToString() => Breakdown;
    }

    /// <summary>One remembered experience: when, what it did, and the reward it got for it (value points).</summary>
    public sealed class Experience
    {
        public int Tick, Reward;
        public byte Verb, Class;
        public bool Episode;
        public string Text;
        public override string ToString() => Text;
    }

    /// <summary>A person seen on the island: where, which faction, and what it visibly does (no mind, no body numbers).</summary>
    public readonly struct NpcSighting
    {
        public readonly int Id; public readonly Faction Faction; public readonly int Node; public readonly bool Asleep; public readonly byte Verb;
        public NpcSighting(int id, Faction f, int node, bool asleep, byte verb) { Id = id; Faction = f; Node = node; Asleep = asleep; Verb = verb; }
    }

    /// <summary>
    /// The population as one faction knows it (layer 7, M3): its own citizens in full (FullInformation: the faction's own
    /// records are near-complete in stage 4), and anyone else only as a sighting. The HUD's NPC inspector reads this, never
    /// the state (G4.5).
    /// </summary>
    public sealed class PopulationKnowledge
    {
        private readonly GodState _s;
        private readonly Faction _me;
        internal PopulationKnowledge(GodState s, Faction me) { _s = s; _me = me; }

        public bool Exists => _s.Population != null;

        /// <summary>The faction's living citizens, ascending id.</summary>
        public List<int> Citizens()
        {
            var l = new List<int>();
            if (_s.Population == null) return l;
            foreach (var n in _s.Population.Npcs) if (n.Alive && n.Faction == _me) l.Add(n.Id);
            return l;
        }

        public int CitizensAlive => _s.Population?.AliveOf(_me) ?? 0;

        /// <summary>Deaths of the faction's citizens so far, by cause (its own records).</summary>
        public int[] DeathsByCause()
        {
            var a = new int[Enum.GetValues(typeof(DeathCause)).Length];
            if (_s.Population == null) return a;
            foreach (var n in _s.Population.Npcs) if (!n.Alive && n.Faction == _me) a[(int)n.DeathCause]++;
            return a;
        }

        /// <summary>Everyone alive on the island as they can be seen (no fog in stage 4), ascending id, written into <paramref name="into"/>.</summary>
        public void Sightings(List<NpcSighting> into)
        {
            into.Clear();
            if (_s.Population == null) return;
            foreach (var n in _s.Population.Npcs) if (n.Alive) into.Add(new NpcSighting(n.Id, n.Faction, n.Node, n.Action.Asleep, n.Action.Verb));
        }

        /// <summary>A person on the island as anyone can see them (null when unknown).</summary>
        public NpcSighting? See(int npcId)
        {
            var n = _s.Population?.Find(npcId);
            if (n == null || !n.Alive) return null;
            return new NpcSighting(n.Id, n.Faction, n.Node, n.Action.Asleep, n.Action.Verb);
        }

        /// <summary>
        /// The inspector's card for one of the faction's own people (null for anyone else: another faction's people have no
        /// card, only a sighting, Q21).
        /// </summary>
        public NpcInspection Inspect(int npcId)
        {
            var n = _s.Population?.Find(npcId);
            if (n == null || n.Faction != _me) return null;
            var cal = SimCalendar.Of(_s);
            var r = new NpcInspection
            {
                Id = n.Id, Faction = n.Faction, Alive = n.Alive, DeathCause = n.DeathCause, Node = n.Node, Founder = n.Founder,
                AgeMilli = cal.AgeMilli(n.BornTick, _s.Tick), Health = n.HealthPermille, Nutrition = n.NutritionPermille, Injury = n.Injury / 1000,
                HomeHut = n.HomeHut, SiteHut = n.SiteHut, Asleep = n.Action.Asleep, InHut = n.Action.Asleep && n.Action.SleepHut != 0, Policy = n.Policy,
            };
            // founders arrive as adults (ages 1.0-2.4 from M4); until then the record counts from the scenario's start
            r.HudAge = SimCalendar.HudAge(r.AgeMilli + (n.Founder ? 1500 : 0));
            for (int d = 0; d < Drive.Count; d++) r.Drives[d] = n.DriveOf(d);
            Array.Copy(n.Traits, r.Traits, Trait.Count);
            foreach (var l in n.Carry) if (!l.Empty) r.Carry.Add(new CarriedItem(l.Ware, l.Count, l.SpoilDay, l.Uses));
            if (n.Action.Verb != 0) r.Doing = n.Action.Asleep ? (n.Action.SleepHut != 0 ? "asleep in a hut" : "asleep in the open") : VerbTable.Name(n.Action.Verb) + TargetWords(n.Action.TargetKind, n.Mind?.Last.TargetClass ?? 0, n.Action.Verb);
            var m = n.Mind;
            if (m == null) return r;
            var st = m.Store;
            r.Mode = st.Mode;
            var last = m.Last;
            if (last.Verb != 0) r.LastDecision = VerbTable.Name(last.Verb) + TargetWords(last.TargetKind, last.TargetClass, last.Verb);
            r.Instinct = last.Instinct; r.Learned = last.Learned; r.Social = last.Social; r.Culture = last.Culture; r.Personality = last.Personality;
            r.Cost = last.Cost; r.Explore = last.Explore; r.Total = last.Total; r.Candidates = last.Candidates;
            r.LearnedChangedWinner = last.LearnedChangedWinner; r.ExploreWon = last.ExploreWon; r.Need = last.Need;
            for (int i = 0; i < m.OpportunityCount; i++)
            {
                var o = m.Opportunities[i];
                r.Opportunities.Add(new Opportunity
                {
                    Text = VerbTable.Name(o.Verb) + TargetWords(Policy.IntentTarget.Node, o.Class, o.Verb), Verb = o.Verb, Class = o.Class, Dist = o.Dist,
                    Instinct = o.Instinct, Need = o.Need, Learned = o.Learned, Personality = o.Personality, Cost = o.Cost, Explore = o.Explore, Total = o.Total,
                    Chosen = o.Chosen, FromBelief = o.FromBelief,
                });
            }
            for (int k = 0; k < m.ExperienceCount; k++)
            {
                var e = m.Recent(k);
                string what = VerbTable.Name(e.Verb) + TargetWords(Policy.IntentTarget.Node, e.Class, e.Verb);
                r.Experiences.Add(new Experience
                {
                    Tick = e.Tick, Reward = e.Reward, Verb = e.Verb, Class = e.Class, Episode = e.Episode,
                    Text = "tick " + e.Tick + ": " + what + (e.Episode ? " (paid off later)" : "") + " -> " + (e.Reward >= 0 ? "+" : "") + e.Reward,
                });
            }
            if (last.SecondVerb != 0) r.RunnerUp = VerbTable.Name(last.SecondVerb) + " (" + last.SecondTotal + ")";
            AddWhy(r, st, last.Key0, last.Verb, last.Stw0);
            AddWhy(r, st, last.Key1, last.Verb, last.Stw1);
            AddWhy(r, st, last.Key2, last.Verb, last.Stw2);
            for (int i = 0; i < m.AttendedCount; i++) r.Attending.Add(Feature.ClassName(m.Attended[i].Class) + " (" + m.Attended[i].Salience + ")");
            // strongest beliefs: highest confidence x importance
            var b = m.Beliefs;
            var order = new List<int>();
            for (int i = 0; i < b.Count; i++) order.Add(i);
            order.Sort((x, y) =>
            {
                int a = b.Slots[x].Confidence * BeliefStore.Importance(b.Slots[x].Kind), c = b.Slots[y].Confidence * BeliefStore.Importance(b.Slots[y].Kind);
                return a != c ? c.CompareTo(a) : x.CompareTo(y);
            });
            for (int k = 0; k < order.Count && k < 5; k++)
            {
                var x = b.Slots[order[k]];
                r.Beliefs.Add(Feature.ClassName(x.Class) + " at #" + x.Subject + (x.Stock == ObjState.StockEmpty ? ", empty" : x.Stock == ObjState.StockLow ? ", little left" : "") + " (" + x.Confidence / 10 + "%)");
            }
            // habits: top 5 positive and top 5 negative by long-term weight (ties: lowest key, verb)
            var idx = new List<int>();
            for (int i = 0; i < st.Count; i++) idx.Add(i);
            idx.Sort((x, y) => st.Entries[y].Ltw != st.Entries[x].Ltw ? st.Entries[y].Ltw.CompareTo(st.Entries[x].Ltw) : x.CompareTo(y));
            for (int k = 0; k < idx.Count && r.HabitsPositive.Count < 5; k++) if (st.Entries[idx[k]].Ltw > 0) r.HabitsPositive.Add(HabitOf(st, st.Entries[idx[k]]));
            for (int k = idx.Count - 1; k >= 0 && r.HabitsNegative.Count < 5; k--) if (st.Entries[idx[k]].Ltw < 0) r.HabitsNegative.Add(HabitOf(st, st.Entries[idx[k]]));
            r.Entries = st.Count; r.Conjunctions = st.ConjUsed; r.Recruits = st.Recruits; r.Evictions = st.Evictions; r.Updates = st.Updates;
            r.LearnedSharePermille = m.DayDecisions > 0 ? m.DayLearnedChanged * 1000 / m.DayDecisions : m.LearnedSharePrev;
            r.ExploreSharePermille = m.DayDecisions > 0 ? m.DayExploreWon * 1000 / m.DayDecisions : m.ExploreSharePrev;
            return r;
        }

        private static string TargetWords(Policy.IntentTarget kind, byte cls, byte verb)
        {
            if (cls == ObjClass.Spot || cls == 0) return verb == Verb.Sleep ? " here" : "";
            if (cls == ObjClass.Unknown) return " (looking around)";
            return " " + Feature.ClassName(cls);
        }

        private static void AddWhy(NpcInspection r, AssociationStore st, uint key, byte verb, short stw)
        {
            if (key == 0) return;
            int at = st.Find(key, verb);
            int ltw = at >= 0 ? st.Entries[at].Ltw : 0;
            r.Why.Add(new Habit(Describe(st, key) + " (" + (stw >= 0 ? "+" : "") + stw + ")", verb, key, stw, ltw));
        }

        /// <summary>A key in words; a conjunction names its parts ("hungry & near food").</summary>
        public static string Describe(AssociationStore st, uint key)
        {
            if (Feature.KindOf(key) != FeatureKind.Conj) return Feature.Describe(key);
            int slot = Feature.ValueOf(key) >> 8;
            if (slot < 0 || slot >= st.Conj.Length || st.Conj[slot].Strength == 0) return "a forgotten combination";
            var c = st.Conj[slot];
            return Feature.Describe(c.A) + " & " + Feature.Describe(c.B);
        }

        private static Habit HabitOf(AssociationStore st, in AssocEntry e)
        {
            int a = Math.Abs((int)e.Ltw);
            string strength = a >= 1000 ? "strong" : a >= 300 ? "moderate" : "weak";
            string origin = e.Origin == Origin.Own ? "own experience" : e.Origin == Origin.Vicarious ? "seen others" : "copied";
            string text = Describe(st, e.Key) + ": " + (e.Ltw < 0 ? "avoid " : "") + VerbTable.Name(e.Verb) + " (" + strength + ", " + origin + ")";
            return new Habit(text, e.Verb, e.Key, e.Stw, e.Ltw);
        }

        internal void AddTo(ref StateHasher h)
        {
            if (_s.Population == null) return;
            foreach (var n in _s.Population.Npcs) if (n.Faction == _me) { h.Add(n.Id); h.Add(n.Alive ? 1 : 0); h.Add(n.Node); }
        }
    }
}
