namespace God.Sim.Divinity
{
    // Contracts 13.2: the structured commandment rule (never free text in the sim). The shape of CommandmentRule is frozen
    // (it is a field of the F1 commands RevealCommandment and ReviseCommandment); the values of ApplySelector,
    // ConditionKind and CommandmentSource beyond those below are D6's to append (OWNER of this file: D6).

    /// <summary>What a rule asks.</summary>
    public enum Modality : byte { None = 0, Required = 1, Prohibited = 2, Encouraged = 3 }

    /// <summary>
    /// Whom a rule applies to (COMMANDMENTS 3; D6 appended 1-7). Judged by the person as it believes itself to be (its own
    /// stage, household, community and affiliations). <see cref="Community"/> and <see cref="Role"/> take their parameter from
    /// <c>ConditionParam</c> bits 8-15 (faction index + 1, role).
    /// </summary>
    public enum ApplySelector : byte { Everyone = 0, Adults = 1, Children = 2, Elders = 3, Affiliated = 4, Community = 5, Role = 6, HouseholdHeads = 7 }

    /// <summary>
    /// When a rule applies (COMMANDMENTS 3; D6 appended 1-15): each is something the person perceives (its think scratch:
    /// base and target features, displays, its own carry), never true state. A second condition that must also hold may be
    /// given in <c>ConditionParam</c> bits 0-7 (a <see cref="ConditionKind"/>; 0 none). <see cref="NotPermitted"/>,
    /// <see cref="HouseholdSurplus"/>, <see cref="IOwe"/> outside a hand-over, <see cref="TargetApologised"/> and
    /// <see cref="NearShrine"/> are not perceivable on this branch (they wait for C1, C8, C2/M7 and the D3 shrine seam) and never hold.
    /// </summary>
    public enum ConditionKind : byte
    {
        Always = 0, TargetIsChild = 1, TargetIsElder = 2, TargetIsStranger = 3, TargetBegging = 4, TargetInjured = 5, TargetSick = 6,
        CarryingFood = 7, HouseholdSurplus = 8, StockLow = 9, NotPermitted = 10, IOwe = 11, TargetApologised = 12, EnemyInSight = 13,
        AtHome = 14, NearShrine = 15,
    }

    /// <summary>How a person learned a commandment (D6 appended 3-5).</summary>
    public enum CommandmentSource : byte { None = 0, Revealed = 1, Taught = 2, Witnessed = 3, Household = 4, Gathering = 5 }

    /// <summary>One rule of a commandment (contracts 13.2).</summary>
    public struct CommandmentRule
    {
        public Modality Modality;
        public byte LearningVerb;
        public byte ObjClass;
        public Ware Ware;
        public ApplySelector Applies;
        public ConditionKind Condition;
        public int ConditionParam;
        public DivineTarget Scope;

        public void AddTo(ref StateHasher h)
        {
            h.Add((int)Modality | (LearningVerb << 8) | (ObjClass << 16)); h.Add((int)Ware); h.Add((int)Applies | ((int)Condition << 8));
            h.Add(ConditionParam); Scope.AddTo(ref h);
        }
    }
}
