using Newtonsoft.Json;

namespace God.Sim.Civ
{
    /// <summary>
    /// F1.2 (contracts 0.1): the civilisation module's state, <c>GodState.Civ</c>: null unless <c>Civ.Enabled = 1</c> and the game
    /// has a population (LEAD_RULINGS R42: I1a may widen the condition to any Civ or Industry switch). OWNER: F1 (this container
    /// is frozen); each sub-object to its package. Hashed as the <c>civ</c> part.
    /// </summary>
    public sealed partial class CivState
    {
        /// <summary>The ObligationId counter (C2, contracts 1).</summary>
        public int NextObligationId = 1;
        /// <summary>The employment ContractId counter (C3, contracts 1).</summary>
        public int NextContractId = 1;
        /// <summary>Productive assets, use rights and economic relationships (C1); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public OwnershipState Ownership;
        /// <summary>The double-entry registry of formal obligations (contracts 1) (C2); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ObligationRegistry Obligations;
        /// <summary>Employment contracts (C3); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public EmploymentState Employment;
        /// <summary>Citizenship spells per NpcId (contracts 3) (C5); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public CitizenshipState Citizenship;
        /// <summary>LEAD_RULINGS R42: the shared skill store, created lazily by <see cref="SkillsOrCreate"/> (C7/K1); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public SkillStore Skills;

        /// <summary>LEAD_RULINGS R42: the shared skill store (C7 and K1), created on first use; never created by F1.</summary>
        public SkillStore SkillsOrCreate() => Skills ?? (Skills = new SkillStore());

        /// <summary>The fixed order: counters, no package part, then the sub-objects in declaration order, each only when non-null (with a tag).</summary>
        public void AddTo(ref StateHasher h)
        {
            h.Add(NextObligationId);
            h.Add(NextContractId);
            if (Ownership != null) { h.Add(1); Ownership.AddTo(ref h); }
            if (Obligations != null) { h.Add(2); Obligations.AddTo(ref h); }
            if (Employment != null) { h.Add(3); Employment.AddTo(ref h); }
            if (Citizenship != null) { h.Add(4); Citizenship.AddTo(ref h); }
            if (Skills != null) { h.Add(5); Skills.AddTo(ref h); }
        }
    }

    /// <summary>Productive assets, use rights and economic relationships OWNER: C1 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class OwnershipState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>The double-entry registry of formal obligations (contracts 1) OWNER: C2 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class ObligationRegistry
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Employment contracts OWNER: C3 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class EmploymentState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Citizenship spells per NpcId (contracts 3) OWNER: C5 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class CitizenshipState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>LEAD_RULINGS R42: the shared skill store, created lazily by <see cref="SkillsOrCreate"/> OWNER: C7/K1 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class SkillStore
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

}
