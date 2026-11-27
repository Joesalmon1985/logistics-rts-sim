using Newtonsoft.Json;

namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.2 (contracts 0.1): the divinity module's state, <c>GodState.Divinity</c>: null unless <c>Divinity.Enabled = 1</c> and the
    /// game has a population. Public fields, ascending-id lists, no dictionaries, ids only. OWNER: F1 (this container is
    /// frozen); each sub-object to its package, each null until that package fills it. Hashed as the <c>divine</c> part.
    /// </summary>
    public sealed partial class DivinityState
    {
        /// <summary>The InterventionId counter (D1, contracts 1).</summary>
        public int NextInterventionId = 1;
        /// <summary>The LedgerId counter (D3, contracts 1).</summary>
        public int NextLedgerId = 1;
        /// <summary>The god registry (contracts 2) (D1); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public GodRegistry Gods;
        /// <summary>Accepted interventions, one per InterventionId (D1); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public InterventionRegistry Interventions;
        /// <summary>Temporary control authority (contracts 11; type in Control.cs) (F1/D1); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ControlTable Control;
        /// <summary>Divine instructions (suggestions, commands, compulsions) (D2); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public InstructionRegistry Messages;
        /// <summary>Miracle records, blessings, consecrated places and the miracle ledger (D3); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public MiracleState Miracles;
        /// <summary>Avatar spells (D5); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public AvatarRegistry Avatars;
        /// <summary>Commandments, religions and affiliations (counters NextCommandmentId, NextReligionId) (D6); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public DoctrineState Doctrine;
        /// <summary>Archived lives (dormant) (D7); null until it fills it.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public MindArchive Archive;

        /// <summary>The fixed order: counters, no package part, then the sub-objects in declaration order, each only when non-null (with a tag).</summary>
        public void AddTo(ref StateHasher h)
        {
            h.Add(NextInterventionId);
            h.Add(NextLedgerId);
            if (Gods != null) { h.Add(1); Gods.AddTo(ref h); }
            if (Interventions != null) { h.Add(2); Interventions.AddTo(ref h); }
            if (Control != null) { h.Add(3); Control.AddTo(ref h); }
            if (Messages != null) { h.Add(4); Messages.AddTo(ref h); }
            if (Miracles != null) { h.Add(5); Miracles.AddTo(ref h); }
            if (Avatars != null) { h.Add(6); Avatars.AddTo(ref h); }
            if (Doctrine != null) { h.Add(7); Doctrine.AddTo(ref h); }
            if (Archive != null) { h.Add(8); Archive.AddTo(ref h); }
        }
    }

    /// <summary>The god registry (contracts 2) OWNER: D1 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class GodRegistry
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Accepted interventions, one per InterventionId OWNER: D1 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class InterventionRegistry
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Divine instructions (suggestions, commands, compulsions) OWNER: D2 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class InstructionRegistry
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Miracle records, blessings, consecrated places and the miracle ledger OWNER: D3 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class MiracleState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Avatar spells OWNER: D5 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class AvatarRegistry
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Commandments, religions and affiliations (counters NextCommandmentId, NextReligionId) OWNER: D6 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class DoctrineState
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

    /// <summary>Archived lives (dormant) OWNER: D7 (fields and its <c>Hash</c> body in its partial file).</summary>
    public sealed partial class MindArchive
    {
        public void AddTo(ref StateHasher h) { Hash(ref h); }
        partial void Hash(ref StateHasher h);
    }

}
