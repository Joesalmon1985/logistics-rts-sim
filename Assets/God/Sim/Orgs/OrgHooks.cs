using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.6 (contracts 0.1, 13.3, 14): the Orgs module's sim-side hooks, called by <c>PopulationExtensions</c> after M4 and M5
    /// (dispatch order M3, M4, M5, Civ, Orgs, Divinity). Every hook returns its default while <c>GodState.Organisations</c> is null.
    /// OWNER of the bodies: C6 (C7-C10 through the slots). The dispatcher (PopulationExtensions) is frozen; the other packages of the domain add their
    /// behaviour only by implementing the partial-method slots below in their own partial file (<c>OrgHooks.&lt;Pkg&gt;.cs</c>), called
    /// in package order. A slot that receives a <c>ref bool handled</c> must return at once when it is already true.
    /// </summary>
    public static partial class OrgHooks
    {
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            if (s.Organisations == null) return;
            OrgModule.OnDeath(s, ev, npc);
            OnDeathC6(s, ev, npc);
            OnDeathC7(s, ev, npc);
            OnDeathC8(s, ev, npc);
            OnDeathC9(s, ev, npc);
            OnDeathC10(s, ev, npc);
        }
        static partial void OnDeathC6(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC7(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC8(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC9(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathC10(GodState s, EventBuffer ev, Npc npc);

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            if (s.Organisations == null) return false;
            bool handled = false;
            TryStartC6(s, ev, npc, in c, day, ref handled);
            TryStartC7(s, ev, npc, in c, day, ref handled);
            TryStartC8(s, ev, npc, in c, day, ref handled);
            TryStartC9(s, ev, npc, in c, day, ref handled);
            TryStartC10(s, ev, npc, in c, day, ref handled);
            return handled;
        }
        static partial void TryStartC6(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC7(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC8(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC9(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartC10(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day)
        {
            if (s.Organisations == null) return false;
            bool handled = false;
            TryActC6(s, ev, npc, day, ref handled);
            TryActC7(s, ev, npc, day, ref handled);
            TryActC8(s, ev, npc, day, ref handled);
            TryActC9(s, ev, npc, day, ref handled);
            TryActC10(s, ev, npc, day, ref handled);
            return handled;
        }
        static partial void TryActC6(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC7(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC8(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC9(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActC10(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);

        /// <summary>The episode key of a decision of this domain (0: none). Each slot sets <c>key</c> only while it is 0.</summary>
        public static int EpisodeKey(Npc npc, in Candidate c)
        {
            int key = 0;
            EpisodeKeyC6(npc, in c, ref key);
            EpisodeKeyC7(npc, in c, ref key);
            EpisodeKeyC8(npc, in c, ref key);
            EpisodeKeyC9(npc, in c, ref key);
            EpisodeKeyC10(npc, in c, ref key);
            return key;
        }
        static partial void EpisodeKeyC6(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC7(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC8(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC9(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyC10(Npc npc, in Candidate c, ref int key);

        /// <summary>The time-use category of a verb of this domain (<see cref="Activity.Other"/> when none knows it).</summary>
        public static int ActivityOf(byte verb)
        {
            int a = Activity.Other;
            ActivityOfC6(verb, ref a);
            ActivityOfC7(verb, ref a);
            ActivityOfC8(verb, ref a);
            ActivityOfC9(verb, ref a);
            ActivityOfC10(verb, ref a);
            return a;
        }
        static partial void ActivityOfC6(byte verb, ref int activity);
        static partial void ActivityOfC7(byte verb, ref int activity);
        static partial void ActivityOfC8(byte verb, ref int activity);
        static partial void ActivityOfC9(byte verb, ref int activity);
        static partial void ActivityOfC10(byte verb, ref int activity);

        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            if (s.Organisations == null) return;
            FillPersonC6(s, observer, seen, ref p);
            FillPersonC7(s, observer, seen, ref p);
            FillPersonC8(s, observer, seen, ref p);
            FillPersonC9(s, observer, seen, ref p);
            FillPersonC10(s, observer, seen, ref p);
        }
        static partial void FillPersonC6(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC7(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC8(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC9(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonC10(GodState s, Npc observer, Npc seen, ref SeenPerson p);

        /// <summary>The Sense seam (contracts 6): things of this domain in sight, appended to <paramref name="near"/>. Returns the new count.</summary>
        public static int SenseExtra(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n)
        {
            if (s.Organisations == null) return n;
            SenseExtraC6(s, npc, sight, g, near, ref n);
            SenseExtraC7(s, npc, sight, g, near, ref n);
            SenseExtraC8(s, npc, sight, g, near, ref n);
            SenseExtraC9(s, npc, sight, g, near, ref n);
            SenseExtraC10(s, npc, sight, g, near, ref n);
            return n;
        }
        static partial void SenseExtraC6(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC7(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC8(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC9(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);
        static partial void SenseExtraC10(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);

    }

    /// <summary>
    /// F1.6: the mind-side hooks of this domain, called by <c>CognitionExtensions</c> after M4 and M5. Like every mind hook
    /// they see the think scratch only (layers 2, 3, 6; reflection gate G4.1): never <see cref="GodState"/>. OWNER of the
    /// bodies: C6 (C7-C10 through the slots); the other packages implement the slots in their own partial file, called in package order.
    /// </summary>
    public static partial class OrgMindHooks
    {
        /// <summary>The rows of this domain's learning verbs (contracts 14 ranges), appended to <c>VerbTable.Rows</c>.</summary>
        public static ReadOnlyArray<VerbInfo> Verbs()
        {
            var l = new List<VerbInfo>();
            VerbsC6(l);
            VerbsC7(l);
            VerbsC8(l);
            VerbsC9(l);
            VerbsC10(l);
            return new ReadOnlyArray<VerbInfo>(l);
        }
        static partial void VerbsC6(List<VerbInfo> into);
        static partial void VerbsC7(List<VerbInfo> into);
        static partial void VerbsC8(List<VerbInfo> into);
        static partial void VerbsC9(List<VerbInfo> into);
        static partial void VerbsC10(List<VerbInfo> into);

        /// <summary>The instinct rows of this domain, appended to <c>InstinctTable.Default</c>.</summary>
        public static ReadOnlyArray<Instinct> Instincts()
        {
            var l = new List<Instinct>();
            InstinctsC6(l);
            InstinctsC7(l);
            InstinctsC8(l);
            InstinctsC9(l);
            InstinctsC10(l);
            return new ReadOnlyArray<Instinct>(l);
        }
        static partial void InstinctsC6(List<Instinct> into);
        static partial void InstinctsC7(List<Instinct> into);
        static partial void InstinctsC8(List<Instinct> into);
        static partial void InstinctsC9(List<Instinct> into);
        static partial void InstinctsC10(List<Instinct> into);

        /// <summary>Candidates of this domain (the dispatcher has set <c>sc.CandLimit</c> to the quota of 4).</summary>
        public static void AddCandidates(ThinkScratch sc)
        {
            AddCandidatesC6(sc);
            AddCandidatesC7(sc);
            AddCandidatesC8(sc);
            AddCandidatesC9(sc);
            AddCandidatesC10(sc);
        }
        static partial void AddCandidatesC6(ThinkScratch sc);
        static partial void AddCandidatesC7(ThinkScratch sc);
        static partial void AddCandidatesC8(ThinkScratch sc);
        static partial void AddCandidatesC9(ThinkScratch sc);
        static partial void AddCandidatesC10(ThinkScratch sc);

        public static int Personality(byte verb, int[] traits)
        {
            int v = 0;
            PersonalityC6(verb, traits, ref v);
            PersonalityC7(verb, traits, ref v);
            PersonalityC8(verb, traits, ref v);
            PersonalityC9(verb, traits, ref v);
            PersonalityC10(verb, traits, ref v);
            return v;
        }
        static partial void PersonalityC6(byte verb, int[] traits, ref int value);
        static partial void PersonalityC7(byte verb, int[] traits, ref int value);
        static partial void PersonalityC8(byte verb, int[] traits, ref int value);
        static partial void PersonalityC9(byte verb, int[] traits, ref int value);
        static partial void PersonalityC10(byte verb, int[] traits, ref int value);

        public static bool Remembers(ThinkScratch sc, int person)
        {
            bool r = false;
            RemembersC6(sc, person, ref r);
            RemembersC7(sc, person, ref r);
            RemembersC8(sc, person, ref r);
            RemembersC9(sc, person, ref r);
            RemembersC10(sc, person, ref r);
            return r;
        }
        static partial void RemembersC6(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC7(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC8(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC9(ThinkScratch sc, int person, ref bool remembers);
        static partial void RemembersC10(ThinkScratch sc, int person, ref bool remembers);

        /// <summary>Words for a feature of this domain (null: none). Each slot sets <c>text</c> only while it is null.</summary>
        public static string Describe(uint f)
        {
            string t = null;
            DescribeC6(f, ref t);
            DescribeC7(f, ref t);
            DescribeC8(f, ref t);
            DescribeC9(f, ref t);
            DescribeC10(f, ref t);
            return t;
        }
        static partial void DescribeC6(uint f, ref string text);
        static partial void DescribeC7(uint f, ref string text);
        static partial void DescribeC8(uint f, ref string text);
        static partial void DescribeC9(uint f, ref string text);
        static partial void DescribeC10(uint f, ref string text);

    }
}
