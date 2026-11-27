using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
namespace God.Sim.Civ
{
    /// <summary>
    /// F1.6 (contracts 0.1, 13.3, 14): the Civ module's sim-side hooks, called by <c>PopulationExtensions</c> after M4 and M5
    /// (dispatch order M3, M4, M5, Civ, Orgs, Divinity). Every hook returns its default while <c>GodState.Civ</c> is null.
    /// OWNER of the bodies: C1 (C2-C5 through the slots). The dispatcher (PopulationExtensions) is frozen; the other packages of the domain add their
    /// behaviour only by implementing the partial-method slots below in their own partial file (<c>CivHooks.&lt;Pkg&gt;.cs</c>), called
    /// in package order. A slot that receives a <c>ref bool handled</c> must return at once when it is already true.
    /// </summary>
    public static partial class CivHooks
    {
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            if (s.Civ == null) return;
            CivModule.OnDeath(s, ev, npc);
            OnDeathC1(s, ev, npc);
            OnDeathC2(s, ev, npc);
            OnDeathC3(s, ev, npc);
            OnDeathC4(s, ev, npc);
            OnDeathC5(s, ev, npc);
        }
        static partial void OnDeathC1(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC2(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC3(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC4(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC5(GodState s, EventBuffer ev, Npc npc);

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            if (s.Civ == null) return false;
            bool handled = false;
            TryStartC1(s, ev, npc, in c, day, ref handled);
            TryStartC2(s, ev, npc, in c, day, ref handled);
            TryStartC3(s, ev, npc, in c, day, ref handled);
            TryStartC4(s, ev, npc, in c, day, ref handled);
            TryStartC5(s, ev, npc, in c, day, ref handled);
            return handled;
        }
        static partial void TryStartC1(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC2(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC3(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC4(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC5(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day)
        {
            if (s.Civ == null) return false;
            bool handled = false;
            TryActC1(s, ev, npc, day, ref handled);
            TryActC2(s, ev, npc, day, ref handled);
            TryActC3(s, ev, npc, day, ref handled);
            TryActC4(s, ev, npc, day, ref handled);
            TryActC5(s, ev, npc, day, ref handled);
            return handled;
        }
        static partial void TryActC1(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC2(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC3(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC4(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC5(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);

        /// <summary>The episode key of a decision of this domain (0: none). Each slot sets <c>key</c> only while it is 0.</summary>
        public static int EpisodeKey(Npc npc, in Candidate c)
        {
            int key = 0;
            EpisodeKeyC1(npc, in c, ref key);
            EpisodeKeyC2(npc, in c, ref key);
            EpisodeKeyC3(npc, in c, ref key);
            EpisodeKeyC4(npc, in c, ref key);
            EpisodeKeyC5(npc, in c, ref key);
            return key;
        }
        static partial void EpisodeKeyC1(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC2(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC3(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC4(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC5(Npc npc, in Candidate c, ref int key);

        /// <summary>The time-use category of a verb of this domain (<see cref="Activity.Other"/> when none knows it).</summary>
        public static int ActivityOf(byte verb)
        {
            int a = Activity.Other;
            ActivityOfC1(verb, ref a);
            ActivityOfC2(verb, ref a);
            ActivityOfC3(verb, ref a);
            ActivityOfC4(verb, ref a);
            ActivityOfC5(verb, ref a);
            return a;
        }
        static partial void ActivityOfC1(byte verb, ref int activity);
        static partial void ActivityOfC2(byte verb, ref int activity);
        static partial void ActivityOfC3(byte verb, ref int activity);
        static partial void ActivityOfC4(byte verb, ref int activity);
        static partial void ActivityOfC5(byte verb, ref int activity);

        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            if (s.Civ == null) return;
            FillPersonC1(s, observer, seen, ref p);
            FillPersonC2(s, observer, seen, ref p);
            FillPersonC3(s, observer, seen, ref p);
            FillPersonC4(s, observer, seen, ref p);
            FillPersonC5(s, observer, seen, ref p);
        }
        static partial void FillPersonC1(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC2(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC3(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC4(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC5(GodState s, Npc observer, Npc seen, ref SeenPerson p);

        /// <summary>The Sense seam (contracts 6): things of this domain in sight, appended to <paramref name="near"/>. Returns the new count.</summary>
        public static int SenseExtra(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n)
        {
            if (s.Civ == null) return n;
            SenseExtraC1(s, npc, sight, g, near, ref n);
            SenseExtraC2(s, npc, sight, g, near, ref n);
            SenseExtraC3(s, npc, sight, g, near, ref n);
            SenseExtraC4(s, npc, sight, g, near, ref n);
            SenseExtraC5(s, npc, sight, g, near, ref n);
            return n;
        }
        static partial void SenseExtraC1(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC2(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC3(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC4(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC5(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);

    }

    /// <summary>
    /// F1.6: the mind-side hooks of this domain, called by <c>CognitionExtensions</c> after M4 and M5. Like every mind hook
    /// they see the think scratch only (layers 2, 3, 6; reflection gate G4.1): never <see cref="GodState"/>. OWNER of the
    /// bodies: C1 (C2-C5 through the slots); the other packages implement the slots in their own partial file, called in package order.
    /// </summary>
    public static partial class CivMindHooks
    {
        /// <summary>The rows of this domain's learning verbs (contracts 14 ranges), appended to <c>VerbTable.Rows</c>.</summary>
        public static ReadOnlyArray<VerbInfo> Verbs()
        {
            var l = new List<VerbInfo>();
            VerbsC1(l);
            VerbsC2(l);
            VerbsC3(l);
            VerbsC4(l);
            VerbsC5(l);
            return new ReadOnlyArray<VerbInfo>(l);
        }
        static partial void VerbsC1(List<VerbInfo> into);
        static partial void VerbsC2(List<VerbInfo> into);
        static partial void VerbsC3(List<VerbInfo> into);
        static partial void VerbsC4(List<VerbInfo> into);
        static partial void VerbsC5(List<VerbInfo> into);

        /// <summary>The instinct rows of this domain, appended to <c>InstinctTable.Default</c>.</summary>
        public static ReadOnlyArray<Instinct> Instincts()
        {
            var l = new List<Instinct>();
            InstinctsC1(l);
            InstinctsC2(l);
            InstinctsC3(l);
            InstinctsC4(l);
            InstinctsC5(l);
            return new ReadOnlyArray<Instinct>(l);
        }
        static partial void InstinctsC1(List<Instinct> into);
        static partial void InstinctsC2(List<Instinct> into);
        static partial void InstinctsC3(List<Instinct> into);
        static partial void InstinctsC4(List<Instinct> into);
        static partial void InstinctsC5(List<Instinct> into);

        /// <summary>Candidates of this domain (the dispatcher has set <c>sc.CandLimit</c> to the quota of 4).</summary>
        public static void AddCandidates(ThinkScratch sc)
        {
            AddCandidatesC1(sc);
            AddCandidatesC2(sc);
            AddCandidatesC3(sc);
            AddCandidatesC4(sc);
            AddCandidatesC5(sc);
        }
        static partial void AddCandidatesC1(ThinkScratch sc);
        static partial void AddCandidatesC2(ThinkScratch sc);
        static partial void AddCandidatesC3(ThinkScratch sc);
        static partial void AddCandidatesC4(ThinkScratch sc);
        static partial void AddCandidatesC5(ThinkScratch sc);

        public static int Personality(byte verb, int[] traits)
        {
            int v = 0;
            PersonalityC1(verb, traits, ref v);
            PersonalityC2(verb, traits, ref v);
            PersonalityC3(verb, traits, ref v);
            PersonalityC4(verb, traits, ref v);
            PersonalityC5(verb, traits, ref v);
            return v;
        }
        static partial void PersonalityC1(byte verb, int[] traits, ref int value);
        static partial void PersonalityC2(byte verb, int[] traits, ref int value);
        static partial void PersonalityC3(byte verb, int[] traits, ref int value);
        static partial void PersonalityC4(byte verb, int[] traits, ref int value);
        static partial void PersonalityC5(byte verb, int[] traits, ref int value);

        public static bool Remembers(ThinkScratch sc, int person)
        {
            bool r = false;
            RemembersC1(sc, person, ref r);
            RemembersC2(sc, person, ref r);
            RemembersC3(sc, person, ref r);
            RemembersC4(sc, person, ref r);
            RemembersC5(sc, person, ref r);
            return r;
        }
        static partial void RemembersC1(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC2(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC3(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC4(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC5(ThinkScratch sc, int person, ref bool remembers);

        /// <summary>Words for a feature of this domain (null: none). Each slot sets <c>text</c> only while it is null.</summary>
        public static string Describe(uint f)
        {
            string t = null;
            DescribeC1(f, ref t);
            DescribeC2(f, ref t);
            DescribeC3(f, ref t);
            DescribeC4(f, ref t);
            DescribeC5(f, ref t);
            return t;
        }
        static partial void DescribeC1(uint f, ref string text);
        static partial void DescribeC2(uint f, ref string text);
        static partial void DescribeC3(uint f, ref string text);
        static partial void DescribeC4(uint f, ref string text);
        static partial void DescribeC5(uint f, ref string text);

    }
}
