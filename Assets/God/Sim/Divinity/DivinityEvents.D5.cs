using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D5's rows of the event catalogue (contracts 15.1, codes 850-859, domain Avatar 62; 855-859 spare). OWNER: D4 (absorbs D5).
    /// A manifestation's start and end (dismissal or destruction) pair exactly once (CT1).
    /// </summary>
    public static partial class DivinityEvents
    {
        // A property (not a field), so the concatenation never depends on the initialisation order of partial files.
        private static ReadOnlyArray<EventInfo> D5 => new ReadOnlyArray<EventInfo>(
            new EventInfo(typeof(AvatarManifested), 850, 1, TelemetryDomain.Avatar, "fact_avatar_manifestation", note: "the god's body appeared (AvatarBody control spell opened)"),
            new EventInfo(typeof(AvatarDismissed), 851, 1, TelemetryDomain.Avatar, "fact_avatar_manifestation", note: "once per manifestation: Cancelled, MythicExhausted, Superseded; carried lots to the ground (AvatarDrop)"),
            new EventInfo(typeof(AvatarDestroyed), 852, 1, TelemetryDomain.Avatar, "fact_avatar_manifestation", note: "health 0: destroyed, never killed (no NpcDied); lots to the ground; the god persists"),
            new EventInfo(typeof(AvatarDamaged), 853, 1, TelemetryDomain.Avatar, "fact_avatar_manifestation", note: "one per blow through Bodies.Damage"),
            new EventInfo(typeof(AvatarActionStarted), 854, 1, TelemetryDomain.Avatar, "fact_avatar_manifestation", true, note: "the avatar started an act at the god's input, with the people attending"));
    }

    /// <summary>850: the god manifested its body at a node.</summary>
    public sealed class AvatarManifested : DomainEvent
    {
        public int Intervention, God, Body, Node, Health;
        /// <summary>The god's possession this manifestation ended (<c>Superseded</c>), 0 none.</summary>
        public int SupersededPossession;
        /// <summary>1 on the god's first manifestation (the body record was created).</summary>
        public bool Created;
    }

    /// <summary>851: the avatar was dismissed (exactly once per manifestation unless destroyed).</summary>
    public sealed class AvatarDismissed : DomainEvent
    {
        public int Intervention, God, Body, Node;
        public ControlEndReason Reason;
        public long StartTick, Ticks;
        public int LotsDropped, Actions, Observers, DamageTaken;
    }

    /// <summary>852: the avatar was destroyed (health 0). Never a death: no <c>NpcDied</c>, no estate.</summary>
    public sealed class AvatarDestroyed : DomainEvent
    {
        public int Intervention, God, Body, Node;
        /// <summary>Who struck the last blow (0 none) and with which intervention (another god's smite).</summary>
        public int By, ByIntervention;
        public DamageCause Cause;
        public long StartTick, Ticks, CooldownUntil;
        public int LotsDropped, Actions, Observers, DamageTaken;
    }

    /// <summary>853: one blow to the avatar through <c>Bodies.Damage</c>.</summary>
    public sealed class AvatarDamaged : DomainEvent
    {
        public int Intervention, God, Body, By, ByIntervention, Amount, HealthAfter;
        public DamageCause Cause;
    }

    /// <summary>854 (HV): the avatar started an act at the god's input (<see cref="ActionTick"/>: the think that started it).</summary>
    public sealed class AvatarActionStarted : DomainEvent
    {
        public int Intervention, Body;
        public byte Verb;
        public Policy.IntentTarget TargetKind;
        public int TargetId, TargetNode;
        public long ActionTick;
        /// <summary>People attending now, and distinct observers of the manifestation so far.</summary>
        public int Attending, Observers;
    }
}
