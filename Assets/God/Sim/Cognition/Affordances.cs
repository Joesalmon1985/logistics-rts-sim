using System;
using God.Sim.Population;

namespace God.Sim.Cognition
{
    /// <summary>
    /// Why an action is not open to a mind on an object (WORLD_ONTOLOGY 5, SHARED_CONTRACTS A8). <see cref="Far"/> is not a
    /// block: it means "walk there first". Frozen by F0.
    /// </summary>
    [Flags]
    public enum Blocked : ushort
    {
        None = 0, Far = 1, Unreachable = 2, NotHeld = 4, NoRoom = 8, NoPermission = 16,
        NoTool = 32, NoSkill = 64, Empty = 128, Busy = 256, Stage = 512, NotPerceived = 1024,
    }

    /// <summary>One action a mind sees on an object: the learning verb, why it is blocked (or not), and the walk to it.</summary>
    public struct Affordance
    {
        public byte Verb;
        public Blocked Why;
        public int Dist;
    }

    /// <summary>What an affordance is about, as the mind knows it: a sensed thing, a belief, a seen person, an own lot or an own bin (by index into the scratch, or an id).</summary>
    public enum ObjectRefKind : byte { None = 0, Sensed = 1, Belief = 2, Person = 3, OwnLot = 4, OwnBin = 5 }

    public struct ObjectRef
    {
        public ObjectRefKind Kind;
        /// <summary>The index in <c>sc.Seen</c>, the mind's beliefs or <c>sc.People</c>; or the lot or bin id for own goods.</summary>
        public int Index;
    }

    /// <summary>
    /// The affordances of an object for one mind, built only from its own view (self, carry, seen things and people,
    /// beliefs, heard messages): never <see cref="GodState"/> (gate G4.1). Frozen seam (A8): M3's offers keep their inline
    /// conditions; M4's and M5's candidate hooks fill their object kinds here. F0 stub: no affordances yet.
    /// </summary>
    public static class Affordances
    {
        /// <summary>Writes the affordances of <paramref name="o"/> into <paramref name="into"/>; returns how many.</summary>
        public static int Of(ThinkScratch sc, in ObjectRef o, Span<Affordance> into) => 0;
    }

    /// <summary>On what basis a mind believes it may use goods: its own, its household's, nobody's, or none of these.</summary>
    public enum PermissionBasis : byte { None = 0, Own = 1, Household = 2, Ownerless = 3 }

    /// <summary>The answer of <see cref="Permits.Believed"/>: allowed or not, and the basis.</summary>
    public readonly struct PermissionResult
    {
        public readonly bool Allowed;
        public readonly PermissionBasis Basis;
        public PermissionResult(bool allowed, PermissionBasis basis) { Allowed = allowed; Basis = basis; }
        public override string ToString() => (Allowed ? "allowed: " : "not allowed: ") + Basis;
    }

    /// <summary>
    /// Permission to use (owner D3; WORLD_ONTOLOGY 1, A8): one pure query seam, distinct from location, reach, possession and
    /// ownership. As the mind believes it: goods it owns, goods its own household owns and ownerless goods are allowed;
    /// nothing else. Not enforced by <see cref="Goods"/> or by any M3, M4 or M5 behaviour (their candidates never offer
    /// another party's goods), so it changes nothing; M8 theft is "take without permission" (the sim classifies it with its
    /// own truth-side check). Household norms that narrow store access are M4's <c>StoreAccess.MayTake</c>, a query too (A4).
    /// </summary>
    public static class Permits
    {
        /// <summary>May this mind (<c>sc.Self</c>) use goods owned by <paramref name="owner"/>, as it believes?</summary>
        public static PermissionResult Believed(ThinkScratch sc, Owner owner)
        {
            if (owner.IsNone) return new PermissionResult(true, PermissionBasis.Ownerless);
            if (owner.Kind == OwnerKind.Npc && owner.Id == sc.Self.Id) return new PermissionResult(true, PermissionBasis.Own);
            if (owner.Kind == OwnerKind.Household && sc.Self.Household != 0 && owner.Id == sc.Self.Household) return new PermissionResult(true, PermissionBasis.Household);
            return new PermissionResult(false, PermissionBasis.None);
        }
    }
}
