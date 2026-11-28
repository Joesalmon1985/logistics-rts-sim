namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 6.1 (brief §9): the divine action vocabulary. The mapping to learning verbs and intents (<c>DivineActs.Map</c>)
    /// is D2's table; an act whose verb is not implemented on the branch is refused at Validate with <c>act_unavailable</c>.
    /// 22-31 reserved (LEAD_RULINGS R15: industry numbers are provisional until the contracts' addendum). OWNER: F1 (frozen).
    /// </summary>
    public enum DivineAct : byte
    {
        None = 0,
        MoveTo = 1, Approach = 2, Inspect = 3, Gather = 4, Take = 5, Drop = 6, Give = 7, Eat = 8, UseTool = 9, Sleep = 10,
        Work = 11, Construct = 12, Trade = 13, Cooperate = 14, Communicate = 15, Protect = 16, Retreat = 17, Attack = 18,
        Teach = 19, Treat = 20, InstitutionalAction = 21,
    }

    /// <summary>Contracts 6: what a divine target names. Group kinds expand at Apply to recipients in ascending NpcId.</summary>
    public enum DivineTargetKind : byte
    {
        None = 0, Npc = 1, Household = 2, Org = 3, Location = 4, Lot = 5, Bin = 6, Source = 7, Hut = 8, Building = 9, Commandment = 10,
        /// <summary>The god's own avatar.</summary>
        Self = 11,
    }

    /// <summary>Contracts 6: a divine target (plain data, saved in commands and control entries).</summary>
    public struct DivineTarget
    {
        public DivineTargetKind Kind;
        public int Id;
        public int Node;
        public int Radius;

        public DivineTarget(DivineTargetKind kind, int id, int node = 0, int radius = 0) { Kind = kind; Id = id; Node = node; Radius = radius; }

        public void AddTo(ref StateHasher h) { h.Add((int)Kind); h.Add(Id); h.Add(Node); h.Add(Radius); }

        public override string ToString() => Kind == DivineTargetKind.None ? "none" : Kind + " " + Id + (Node != 0 ? " @" + Node : "") + (Radius != 0 ? " r" + Radius : "");
    }

    /// <summary>Contracts 10.2: the scope of an instruction. An institution-level instruction is relayed to office-holders (D2, C6).</summary>
    public enum DivineScope : byte { None = 0, Individual = 1, Household = 2, Group = 3, Org = 4, Location = 5 }

    /// <summary>Contracts 10.2: the families of intervention (six of the brief plus the avatar's own family).</summary>
    public enum InterventionKind : byte { None = 0, Suggestion = 1, Command = 2, Compulsion = 3, Possession = 4, Commandment = 5, Miracle = 6, Avatar = 7 }

    /// <summary>
    /// Contracts 8: the kinds of miracle. <see cref="Area"/> + kind applies the single-target effect over every eligible thing
    /// within <c>Target.Radius</c>. <see cref="RevealTechnology"/> is reserved (refused <c>act_unavailable</c>); weather and
    /// terrain miracles are reserved without numbers.
    /// </summary>
    public enum MiracleKind : byte
    {
        None = 0,
        HealInjury = 1, HealDisease = 2, Bless = 3, Protect = 4, SmitePerson = 5, SmiteBuilding = 6, SmiteSource = 7,
        CreateResource = 8, DestroyResource = 9, RevealKnowledge = 10, RevealTechnology = 11, Consecrate = 12, Rejuvenate = 13,
        Area = 64,
    }

    /// <summary>Contracts 4.4: how much of the body a resurrection restores (Health, Nutrition, Injury).</summary>
    public enum ResurrectionVariant : byte { None = 0, Lesser = 1, Full = 2, Restoration = 3 }

    /// <summary>Contracts 11.3: what a <see cref="DivineControlInput"/> carries.
    /// <para>UN0 (contracts 23.4, LEAD_RULINGS R84): 5 <c>Primitive</c>, one of the twelve unified controls
    /// (<c>DivineControlInput.Primitive</c>), accepted only in a unified game and submitted to the same motor as every other origin.</para></summary>
    public enum ControlInputKind : byte { None = 0, Heading = 1, MoveTo = 2, Stop = 3, Action = 4, Primitive = 5 }
}
