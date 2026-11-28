namespace God.Sim.Unified
{
    /// <summary>
    /// UN6's rows of the event catalogue (contracts 23.12, block 1320-1339, domain Brain). OWNER: UN6 (1320's artifact fields shared
    /// with UN6t: a field UN6t adds bumps its version). Tables "generic" until UN11a lands migration 0046 (intended tables in the UN6
    /// record and <c>UN6Mappings</c>). Knowledge and demonstrations are analytics of beliefs, never world facts (QA-02).
    /// </summary>
    public static partial class UnifiedEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> UN6 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(BrainAttached), 1320, 1, TelemetryDomain.Brain, EventCatalog.Generic, false,
                "which brain runs: kind, artifact version, weights hash, label (intended table dim_brain_artifact, 0046)"),
            new EventInfo(typeof(KnowledgeAcquired), 1321, 1, TelemetryDomain.Brain, EventCatalog.Generic, false,
                "a person came to know a design, recipe or interaction kind (intended table fact_knowledge, 0046)"),
            new EventInfo(typeof(DemonstrationObserved), 1322, 1, TelemetryDomain.Brain, EventCatalog.Generic, true,
                "a person perceived another complete something it did not know (intended table fact_knowledge, 0046)"),
            new EventInfo(typeof(TeachingGesture), 1323, 1, TelemetryDomain.Brain, EventCatalog.Generic, false,
                "a Teach or Demonstrate gesture opened a teaching window (intended table fact_knowledge, 0046)"));
    }

    /// <summary>Where knowledge came from.</summary>
    public enum KnowledgeSource : byte { Own = 0, Observed = 1, Taught = 2, Start = 3 }

    /// <summary>1320: the brain of this game (emitted once, at the first person tick of a new game; a load does not repeat it).</summary>
    public sealed class BrainAttached : DomainEvent
    {
        public int Kind;
        public int ArtifactVersion;
        public string WeightsSha256;
        public string Label;
    }

    /// <summary>1321: <see cref="Npc"/> came to know <see cref="What"/> number <see cref="Index"/>. <see cref="From"/>: the demonstrator (0 own).</summary>
    public sealed class KnowledgeAcquired : DomainEvent
    {
        public int Npc;
        public KnowledgeKind What;
        public int Index;
        public KnowledgeSource Source;
        public int From;
        /// <summary>The origin of the action the knowledge came from (the learner's own or the demonstrator's).</summary>
        public ControlOrigin Origin;
    }

    /// <summary>1322 (HV): <see cref="Observer"/> perceived <see cref="Actor"/> complete <see cref="What"/>/<see cref="Index"/> it did not know.</summary>
    public sealed class DemonstrationObserved : DomainEvent
    {
        public int Observer;
        public int Actor;
        public KnowledgeKind What;
        public int Index;
        public int Distance;
        public bool Taught;
        /// <summary>Chance (permille) the observer had of learning it, and whether it did.</summary>
        public int Chance;
        public bool Learned;
        public ControlOrigin Origin;
    }

    /// <summary>1323: <see cref="Actor"/> made a Teach or Demonstrate gesture: completions until <see cref="Until"/> are taught.</summary>
    public sealed class TeachingGesture : DomainEvent
    {
        public int Actor;
        public byte Act;
        public long Until;
        public ControlOrigin Origin;
    }
}
