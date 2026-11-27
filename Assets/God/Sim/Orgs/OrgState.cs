using Newtonsoft.Json;

namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.2 (contracts 0.1): the organisations module's state, <c>GodState.Organisations</c>: null unless <c>Orgs.Enabled = 1</c>
    /// and the game has a population. OWNER: F1 (this container is frozen); C6 owns the core (its fields and <c>HashC6</c> in
    /// <c>OrgState.C6.cs</c>), C3 its partial (LEAD_RULINGS R21), C7-C10 their sub-objects. Hashed as the <c>organisations</c> part.
    /// </summary>
    public sealed partial class OrgState
    {
        /// <summary>The OrgId counter (C6, contracts 1).</summary>
        public int NextOrgId = 1;
        /// <summary>The RequestId counter (C6, contracts 1).</summary>
        public int NextRequestId = 1;
        /// <summary>Health and education services (C7); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public HealthEducationState Health;
        /// <summary>Banking and welfare (C8); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public FinanceState Finance;
        /// <summary>Complaints, investigations and cases (counter NextCaseId) (C9); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public JusticeState Justice;
        /// <summary>Culture, religion institutions and politics (C10); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public PolityState Polity;

        /// <summary>The fixed order: counters, the C6 part, the C3 part, then the sub-objects in declaration order, each only when non-null (with a tag).</summary>
        public void AddTo(ref StateHasher h)
        {
            h.Add(NextOrgId);
            h.Add(NextRequestId);
            HashC6(ref h);
            HashC3(ref h);
            if (Health != null) { h.Add(1); Health.AddTo(ref h); }
            if (Finance != null) { h.Add(2); Finance.AddTo(ref h); }
            if (Justice != null) { h.Add(3); Justice.AddTo(ref h); }
            if (Polity != null) { h.Add(4); Polity.AddTo(ref h); }
        }
        partial void HashC6(ref StateHasher h);
        partial void HashC3(ref StateHasher h);
    }

    /// <summary>Health and education services OWNER: C7 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class HealthEducationState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Banking and welfare OWNER: C8 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class FinanceState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Complaints, investigations and cases (counter NextCaseId) OWNER: C9 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class JusticeState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Culture, religion institutions and politics OWNER: C10 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class PolityState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

}
