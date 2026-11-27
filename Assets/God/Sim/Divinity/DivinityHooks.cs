using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.6 (contracts 0.1, 13.3, 14): the Divinity module's sim-side hooks, called by <c>PopulationExtensions</c> after M4 and M5
    /// (dispatch order M3, M4, M5, Civ, Orgs, Divinity). Every hook returns its default while <c>GodState.Divinity</c> is null.
    /// OWNER of the bodies: D1 (D2-D7 through the slots, contracts 13.3/14). The dispatcher (PopulationExtensions) is frozen; the other packages of the domain add their
    /// behaviour only by implementing the partial-method slots below in their own partial file (<c>DivinityHooks.&lt;Pkg&gt;.cs</c>), called
    /// in package order. A slot that receives a <c>ref bool handled</c> must return at once when it is already true.
    /// </summary>
    public static partial class DivinityHooks
    {
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            if (s.Divinity == null) return;
            Control.OnDeath(s, ev, npc); // contracts 11.4: release control first
            DivinityModule.OnDeath(s, ev, npc);
            OnDeathD2(s, ev, npc);
            OnDeathD4(s, ev, npc);
            OnDeathD5(s, ev, npc);
            OnDeathD7(s, ev, npc);
        }
        static partial void OnDeathD2(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathD4(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathD5(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathD7(GodState s, EventBuffer ev, Npc npc);

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            if (s.Divinity == null) return false;
            bool handled = false;
            TryStartD2(s, ev, npc, in c, day, ref handled);
            TryStartD4(s, ev, npc, in c, day, ref handled);
            TryStartD5(s, ev, npc, in c, day, ref handled);
            return handled;
        }
        static partial void TryStartD2(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartD4(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);
        static partial void TryStartD5(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day, ref bool handled);

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day)
        {
            if (s.Divinity == null) return false;
            bool handled = false;
            TryActD2(s, ev, npc, day, ref handled);
            TryActD4(s, ev, npc, day, ref handled);
            TryActD5(s, ev, npc, day, ref handled);
            return handled;
        }
        static partial void TryActD2(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActD4(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);
        static partial void TryActD5(GodState s, EventBuffer ev, Npc npc, int day, ref bool handled);

        /// <summary>The episode key of a decision of this domain (0: none). Each slot sets <c>key</c> only while it is 0.</summary>
        public static int EpisodeKey(Npc npc, in Candidate c)
        {
            int key = 0;
            EpisodeKeyD2(npc, in c, ref key);
            EpisodeKeyD4(npc, in c, ref key);
            EpisodeKeyD6(npc, in c, ref key);
            return key;
        }
        static partial void EpisodeKeyD2(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyD4(Npc npc, in Candidate c, ref int key);
        static partial void EpisodeKeyD6(Npc npc, in Candidate c, ref int key);

        /// <summary>The time-use category of a verb of this domain (<see cref="Activity.Other"/> when none knows it).</summary>
        public static int ActivityOf(byte verb)
        {
            int a = Activity.Other;
            ActivityOfD6(verb, ref a);
            return a;
        }
        static partial void ActivityOfD6(byte verb, ref int activity);

        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            if (s.Divinity == null) return;
            FillPersonD5(s, observer, seen, ref p);
            FillPersonD6(s, observer, seen, ref p);
        }
        static partial void FillPersonD5(GodState s, Npc observer, Npc seen, ref SeenPerson p);
        static partial void FillPersonD6(GodState s, Npc observer, Npc seen, ref SeenPerson p);

        /// <summary>The Sense seam (contracts 6): things of this domain in sight, appended to <paramref name="near"/>. Returns the new count.</summary>
        public static int SenseExtra(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, int n)
        {
            if (s.Divinity == null) return n;
            SenseExtraD3(s, npc, sight, g, near, ref n);
            return n;
        }
        static partial void SenseExtraD3(GodState s, Npc npc, int sight, NodeGrid g, Sensed[] near, ref int n);

        // ---- F1 control sites (contracts 11.3): bodies in Control.cs (F1, frozen) ----
        public static bool ForceThink(GodState s, Npc npc) => s.Divinity != null && Control.ForceThink(s, npc);
        public static bool SkipsPolicy(GodState s, Npc npc) => s.Divinity != null && Control.SkipsPolicy(s, npc);
        public static NpcIntent ControlIntent(GodState s, Npc npc, ThinkScratch sc, ref DecisionRecord why) => Control.ControlIntent(s, npc, sc, ref why);
        public static void OverrideIntent(GodState s, Npc npc, ThinkScratch sc, ref NpcIntent intent, ref DecisionRecord why)
        {
            if (s.Divinity == null) return;
            Control.OverrideIntent(s, npc, sc, ref intent, ref why);
        }
        /// <summary>The control entry over the body for the think scratch (default when none).</summary>
        public static ControlEntry ControlOf(GodState s, Npc npc) => s.Divinity == null ? default : Control.EntryOf(s, npc.Id);

        /// <summary>A running action ended: <see cref="Control.ActionEnded"/> (compulsion completion), then the packages.</summary>
        public static void ActionEnded(GodState s, EventBuffer ev, Npc npc, byte verb, bool done)
        {
            if (s.Divinity == null) return;
            Control.ActionEnded(s, ev, npc, verb, done);
            ActionEndedD2(s, ev, npc, verb, done);
            ActionEndedD4(s, ev, npc, verb, done);
            ActionEndedD5(s, ev, npc, verb, done);
        }
        static partial void ActionEndedD2(GodState s, EventBuffer ev, Npc npc, byte verb, bool done);
        static partial void ActionEndedD4(GodState s, EventBuffer ev, Npc npc, byte verb, bool done);
        static partial void ActionEndedD5(GodState s, EventBuffer ev, Npc npc, byte verb, bool done);

        /// <summary>Contracts 4.4: just before a dead person's mind is released (D7 archives it when configured).</summary>
        public static void ArchiveMind(GodState s, Npc npc)
        {
            if (s.Divinity == null) return;
            ArchiveMindD7(s, npc);
        }
        static partial void ArchiveMindD7(GodState s, Npc npc);

        /// <summary><see cref="Bodies.Damage"/> dispatch: protection (D3) and avatar bodies (D5). Sets <c>handled</c> when the damage was taken over.</summary>
        public static void Damage(GodState s, EventBuffer ev, Npc body, ref int injuryMicro, DamageCause cause, int actor, int intervention, ref bool handled)
        {
            if (s.Divinity == null) return;
            DamageD3(s, ev, body, ref injuryMicro, cause, actor, intervention, ref handled);
            DamageD5(s, ev, body, ref injuryMicro, cause, actor, intervention, ref handled);
        }
        static partial void DamageD3(GodState s, EventBuffer ev, Npc body, ref int injuryMicro, DamageCause cause, int actor, int intervention, ref bool handled);
        static partial void DamageD5(GodState s, EventBuffer ev, Npc body, ref int injuryMicro, DamageCause cause, int actor, int intervention, ref bool handled);

        /// <summary><see cref="Bodies.Heal"/> dispatch for what the person path does not heal itself (illness: D3 through M4; avatar: D5).</summary>
        public static void Heal(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention, ref bool handled)
        {
            if (s.Divinity == null) return;
            HealD3(s, ev, body, what, amount, intervention, ref handled);
            HealD5(s, ev, body, what, amount, intervention, ref handled);
        }
        static partial void HealD3(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention, ref bool handled);
        static partial void HealD5(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention, ref bool handled);

    }

    /// <summary>
    /// F1.6: the mind-side hooks of this domain, called by <c>CognitionExtensions</c> after M4 and M5. Like every mind hook
    /// they see the think scratch only (layers 2, 3, 6; reflection gate G4.1): never <see cref="GodState"/>. OWNER of the
    /// bodies: D1 (D2-D7 through the slots, contracts 13.3/14); the other packages implement the slots in their own partial file, called in package order.
    /// </summary>
    public static partial class DivinityMindHooks
    {
        /// <summary>The rows of this domain's learning verbs (contracts 14 ranges), appended to <c>VerbTable.Rows</c>.</summary>
        public static ReadOnlyArray<VerbInfo> Verbs()
        {
            var l = new List<VerbInfo>();
            VerbsD6(l);
            return new ReadOnlyArray<VerbInfo>(l);
        }
        static partial void VerbsD6(List<VerbInfo> into);

        /// <summary>The instinct rows of this domain, appended to <c>InstinctTable.Default</c>.</summary>
        public static ReadOnlyArray<Instinct> Instincts()
        {
            var l = new List<Instinct>();
            InstinctsD2(l);
            InstinctsD6(l);
            return new ReadOnlyArray<Instinct>(l);
        }
        static partial void InstinctsD2(List<Instinct> into);
        static partial void InstinctsD6(List<Instinct> into);

        /// <summary>Candidates of this domain (the dispatcher has set <c>sc.CandLimit</c> to the quota of 4).</summary>
        public static void AddCandidates(ThinkScratch sc)
        {
            AddCandidatesD2(sc);
            AddCandidatesD4(sc);
            AddCandidatesD5(sc);
            AddCandidatesD6(sc);
        }
        static partial void AddCandidatesD2(ThinkScratch sc);
        static partial void AddCandidatesD4(ThinkScratch sc);
        static partial void AddCandidatesD5(ThinkScratch sc);
        static partial void AddCandidatesD6(ThinkScratch sc);

        public static int Personality(byte verb, int[] traits)
        {
            int v = 0;
            PersonalityD6(verb, traits, ref v);
            return v;
        }
        static partial void PersonalityD6(byte verb, int[] traits, ref int value);

        public static bool Remembers(ThinkScratch sc, int person)
        {
            bool r = false;
            RemembersD2(sc, person, ref r);
            return r;
        }
        static partial void RemembersD2(ThinkScratch sc, int person, ref bool remembers);

        /// <summary>Words for a feature of this domain (null: none). Each slot sets <c>text</c> only while it is null.</summary>
        public static string Describe(uint f)
        {
            string t = null;
            DescribeD2(f, ref t);
            DescribeD6(f, ref t);
            return t;
        }
        static partial void DescribeD2(uint f, ref string text);
        static partial void DescribeD6(uint f, ref string text);

        /// <summary>Contracts 10.3 step 2: the decoded instructions read at this think into <c>sc.Divine</c> (D2).</summary>
        public static void FillDivine(ThinkScratch sc)
        {
            FillDivineD2(sc);
        }
        static partial void FillDivineD2(ThinkScratch sc);

        /// <summary>Contracts 10.3 step 4: <c>clamp(D2 instruction term, +-2000) + clamp(D6 commandment term, +-2000)</c>; 0 by default.</summary>
        public static int DivineTerm(ThinkScratch sc, int cand)
        {
            int d2 = 0, d6 = 0;
            DivineTermD2(sc, cand, ref d2);
            DivineTermD6(sc, cand, ref d6);
            return Fx.Clamp(d2, -2000, 2000) + Fx.Clamp(d6, -2000, 2000);
        }
        static partial void DivineTermD2(ThinkScratch sc, int cand, ref int term);
        static partial void DivineTermD6(ThinkScratch sc, int cand, ref int term);

    }
}
