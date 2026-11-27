using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The reusable non-verbal vocabulary (NPC_ANIMATION_DESIGN section 5). Plain data: no Unity types, so the planner and the
    /// clips are testable headless. Feeding and Tending are functional poses (care transfers), never communicative cues.
    /// </summary>
    public enum AnimKind : byte
    {
        None = 0, Attention = 1, Listen = 2, Pointing = 3, Requesting = 4, Offering = 5, Displaying = 6, Giving = 7, Receiving = 8,
        Accepting = 9, Refusing = 10, Agreeing = 11, Cooperating = 12, Warning = 13, Threatening = 14, Apologising = 15,
        Gratitude = 16, Begging = 17, Praying = 18, Teaching = 19, Fear = 20, Submission = 21, Feeding = 22, Tending = 23,
        Count = 24,
    }

    /// <summary>The ware, count and sim lot a cue shows in the hands (the real lot named by the event; 0 lot: not named).</summary>
    public struct CueObject
    {
        public Ware Ware; public int Count; public int Lot;
        public bool Any => Ware != Ware.None && Count > 0;
    }

    /// <summary>
    /// One planned animation (U3 spec: Id, SourceEventId, Actor, Kind, Target, Object, StartTick, Phases, Intensity). It is a
    /// function of an event and the planner's own record of earlier events: the same log gives the same cues. The cue never
    /// reads a truth flag and never changes the sim.
    /// </summary>
    public sealed class AnimationCue
    {
        public int Id;
        /// <summary>The event's identity: (tick, order in the tick).</summary>
        public long SourceEventId;
        public int Actor;
        public AnimKind Kind;
        /// <summary>The person faced or addressed (0 none).</summary>
        public int Target;
        public CueObject Object;
        public long StartTick;
        /// <summary>The sim's duration for the act (the gesture's DurationTicks), 0 when the event has none.</summary>
        public int DurationTicks;
        /// <summary>The phases of the clip, in order (names), copied from the clip.</summary>
        public string[] Phases;
        /// <summary>0..1: amplitude and hold.</summary>
        public float Intensity;
        /// <summary>Quantity 1..9 raised as finger pips (0 none).</summary>
        public int Quantity;
        /// <summary>True for care and other functional poses.</summary>
        public bool Functional;

        public override string ToString() => Kind + " actor " + Actor + " -> " + Target + " @" + StartTick + " src " + SourceEventId;
    }

    /// <summary>An RGB triple with no engine type (the divine planner is pure).</summary>
    public struct Rgb
    {
        public float R, G, B;
        public Rgb(float r, float g, float b) { R = r; G = g; B = b; }
    }
}
