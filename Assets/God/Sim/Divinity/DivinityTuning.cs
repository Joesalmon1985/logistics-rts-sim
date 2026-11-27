using System.ComponentModel;
using Newtonsoft.Json;

namespace God.Sim.Divinity
{
    /// <summary>
    /// F1.3 (contracts 16): <c>SimTuning.Divinity</c>. <see cref="Enabled"/> 0 (every existing scenario): no
    /// <c>GodState.Divinity</c>, no handlers, every Divinity hook returns its default. <see cref="Mode"/>: contracts 5.
    /// The root is frozen (F1.3) and deliberately <b>not</b> partial: its field order is the JSON order the core hash reads, so
    /// no second file may add root fields. Each nested block is partial and belongs to one package, which adds its fields
    /// only in its own file (<c>DivinityTuning.&lt;Pkg&gt;.cs</c>); a block written only when it differs from its defaults.
    /// </summary>
    public sealed class DivinityTuning : IOmitWhenDefault
    {
        public int Enabled;
        public DivinityMode Mode = DivinityMode.Standard;
        /// <summary>D1's numbers.</summary>
        public MythicTuning Mythic = new MythicTuning();
        /// <summary>D2's numbers.</summary>
        public InstructionTuning Instruction = new InstructionTuning();
        /// <summary>D3's numbers.</summary>
        public MiracleTuning Miracles = new MiracleTuning();
        /// <summary>D4's numbers.</summary>
        public PossessionTuning Possession = new PossessionTuning();
        /// <summary>D5's numbers.</summary>
        public AvatarTuning Avatar = new AvatarTuning();
        /// <summary>D6's numbers.</summary>
        public DoctrineTuning Doctrine = new DoctrineTuning();
        /// <summary>D7's numbers.</summary>
        public ResurrectionTuning Resurrection = new ResurrectionTuning();
        /// <summary>F1's numbers.</summary>
        public ControlTuning Control = new ControlTuning();
    }

    /// <summary><c>Mythic</c>: OWNER D1 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class MythicTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Instruction</c>: OWNER D2 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class InstructionTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Miracles</c>: OWNER D3 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class MiracleTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Possession</c>: OWNER D4 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class PossessionTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Avatar</c>: OWNER D5 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class AvatarTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Doctrine</c>: OWNER D6 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class DoctrineTuning : IOmitWhenDefault
    {
    }

    /// <summary><c>Resurrection</c>: OWNER D7 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class ResurrectionTuning : IOmitWhenDefault
    {
    }

    /// <summary>Contracts 5 (D5.26, D5.27): how Mythic is charged. <c>Disabled</c>: gods exist, every divine command is refused.</summary>
    public enum DivinityMode : byte { Standard = 0, Unlimited = 1, Disabled = 2 }

    /// <summary>
    /// <c>Divinity.Control</c> (contracts 11.3): the movement-input model of <c>DivineControlInput</c>. OWNER: F1 (frozen, D1 by
    /// amendment); not partial, so its JSON field order is fixed.
    /// </summary>
    public sealed class ControlTuning : IOmitWhenDefault
    {
        /// <summary>Heading sectors of a full turn (north = board +Z, clockwise); must be 4, 8, 16, 32 or 64.</summary>
        [DefaultValue(16), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int HeadingSectors = 16;
        /// <summary>Ticks a Heading input holds from the tick it applies.</summary>
        [DefaultValue(3), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int LeaseTicks = 3;
    }

}
