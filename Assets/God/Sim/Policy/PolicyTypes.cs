using System;
using System.Collections.Generic;

namespace God.Sim.Policy
{
    /// <summary>
    /// The verbs an <see cref="NpcIntent"/> can name. The civilian verbs are placeholders until M3 (NPC_LEARNING_DESIGN
    /// owns the full list); the military verbs are the combat subset of MILITARY_DESIGN "IMilitaryPolicy".
    /// </summary>
    public enum NpcVerb
    {
        None = 0,
        // civilian (placeholders until M3)
        Idle = 1, Move = 2, Work = 3, Eat = 4, Rest = 5, Gesture = 6,
        // combat subset (military duty)
        HoldPost = 20, Charge = 21, Attack = 22, WarningShot = 23, Retreat = 24, Flee = 25, Surrender = 26,
        AcceptSurrender = 27, Desert = 28, Defect = 29, Assist = 30,
        // civilian, M3 (the NPC_LEARNING verb table's survival, gathering and equipment verbs; Eat, Rest and Flee above)
        Sleep = 40, ForageAt = 41, Gather = 42, Build = 43, PickUp = 44, Equip = 45, Explore = 46, SeekShelter = 47,
        /// <summary>A controlled lab action (NPC_LEARNING L3): never offered on the island.</summary>
        LabAct = 48,
    }

    /// <summary>What an intent points at.</summary>
    public enum IntentTarget { None = 0, Point = 1, Unit = 2, Building = 3, Npc = 4, Faction = 5, Node = 6, Lot = 7, Hut = 8 }

    /// <summary>
    /// One decision of an individual-level policy (<see cref="INpcPolicy"/>, <see cref="IMilitaryPolicy"/>): a verb and
    /// its target. Plain data; the sim validates it like a command (<see cref="IntentRules"/>).
    /// </summary>
    public readonly struct NpcIntent
    {
        public readonly NpcVerb Verb;
        public readonly IntentTarget Target;
        public readonly int TargetId;
        /// <summary>A point in millimetres (for <see cref="IntentTarget.Point"/> and as the target's position).</summary>
        public readonly int X, Z;
        /// <summary>A sub-kind: for civilian intents the learning verb (NPC_LEARNING 3.2) and for nodes X is the node; 0 otherwise.</summary>
        public readonly int Detail;

        public NpcIntent(NpcVerb verb, IntentTarget target = IntentTarget.None, int targetId = 0, int x = 0, int z = 0, int detail = 0)
        {
            Verb = verb; Target = target; TargetId = targetId; X = x; Z = z; Detail = detail;
        }

        public static NpcIntent Nothing => new NpcIntent(NpcVerb.None);

        public override string ToString() => Verb + (Target == IntentTarget.None ? "" : " " + Target + " " + TargetId + " (" + X + "," + Z + ")");
    }

    /// <summary>Which verbs each individual level may name (M1.6: a policy cannot name an intent outside its interface).</summary>
    public static class IntentRules
    {
        public static bool IsCombat(NpcVerb v) => (int)v >= (int)NpcVerb.HoldPost && (int)v <= (int)NpcVerb.Assist || v == NpcVerb.Gesture;

        public static bool IsCivilian(NpcVerb v) =>
            (int)v >= (int)NpcVerb.Idle && (int)v <= (int)NpcVerb.Gesture || v == NpcVerb.Flee || (int)v >= (int)NpcVerb.Sleep && (int)v <= (int)NpcVerb.LabAct;

        /// <summary>True when a policy of <paramref name="level"/> may return this verb (<see cref="NpcVerb.None"/>, doing nothing, is always allowed).</summary>
        public static bool Allowed(PolicyLevel level, NpcVerb v)
        {
            if (v == NpcVerb.None) return true;
            switch (level)
            {
                case PolicyLevel.Military: return IsCombat(v);
                case PolicyLevel.Civilian: return IsCivilian(v);
                default: return false; // institution and faction levels act through commands, never intents
            }
        }

        /// <summary>Throws <see cref="PolicyException"/> for an intent outside the level's set.</summary>
        public static NpcIntent Require(PolicyLevel level, string policy, NpcIntent intent)
        {
            if (!Allowed(level, intent.Verb))
                throw new PolicyException("policy '" + policy + "' (" + level + ") returned the intent " + intent.Verb + ", which its level may not name");
            return intent;
        }
    }

    /// <summary>
    /// The command types an <see cref="IFactionPolicy"/> can return: every concrete <see cref="FactionCommand"/> of the sim
    /// (STATE_DESIGN "The state's command set"). The HUD sends exactly this set (test S-CMD-1). Developer commands
    /// (<c>DevSpawnUnit</c>, <c>DevMoveUnit</c>) are plain <see cref="Command"/>s and are refused from a policy.
    /// </summary>
    public static class FactionCommandSet
    {
        /// <summary>The set, by full name, sorted (built on each call: no static state).</summary>
        public static List<Type> Types()
        {
            var l = new List<Type>();
            foreach (var t in typeof(Command).Assembly.GetTypes())
                if (!t.IsAbstract && typeof(FactionCommand).IsAssignableFrom(t)) l.Add(t);
            l.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return l;
        }

        /// <summary>True when a faction policy of <paramref name="faction"/> may send this command: a faction command in its own name.</summary>
        public static bool Admissible(Command c, Faction faction) => c is FactionCommand fc && fc.Faction == faction;
    }

    /// <summary>A policy name or assignment that cannot be used (unknown name, a later milestone, a test-only policy).</summary>
    public sealed class PolicyException : Exception
    {
        public PolicyException(string message) : base(message) { }
    }
}
