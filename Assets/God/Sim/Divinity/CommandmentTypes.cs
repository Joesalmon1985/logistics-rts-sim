namespace God.Sim.Divinity
{
    // Contracts 13.2: the structured commandment rule (never free text in the sim). The shape of CommandmentRule is frozen
    // (it is a field of the F1 commands RevealCommandment and ReviseCommandment); the values of ApplySelector,
    // ConditionKind and CommandmentSource beyond those below are D6's to append (OWNER of this file: D6).

    /// <summary>What a rule asks.</summary>
    public enum Modality : byte { None = 0, Required = 1, Prohibited = 2, Encouraged = 3 }

    /// <summary>Whom a rule applies to (D6 appends values).</summary>
    public enum ApplySelector : byte { Everyone = 0 }

    /// <summary>When a rule applies (D6 appends values).</summary>
    public enum ConditionKind : byte { Always = 0 }

    /// <summary>How a person learned a commandment (D6 appends values).</summary>
    public enum CommandmentSource : byte { None = 0, Revealed = 1, Taught = 2 }

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
