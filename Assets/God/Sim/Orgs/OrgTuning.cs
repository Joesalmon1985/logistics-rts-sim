using System.ComponentModel;
using Newtonsoft.Json;

namespace God.Sim.Orgs
{
    /// <summary>
    /// F1.3 (contracts 16): <c>SimTuning.Orgs</c>. <see cref="Enabled"/> 0: no <c>GodState.Organisations</c>, no founding, no
    /// services, no institution seats. <see cref="Emergence"/> 0: organisations only when a scenario seeds them.
    /// The root is frozen (F1.3) and deliberately <b>not</b> partial: its field order is the JSON order the core hash reads, so
    /// no second file may add root fields. Each nested block is partial and belongs to one package, which adds its fields
    /// only in its own file (<c>OrgTuning.&lt;Pkg&gt;.cs</c>); a block written only when it differs from its defaults.
    /// </summary>
    public sealed class OrgTuning : IOmitWhenDefault
    {
        public int Enabled;
        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int Emergence = 1;
        /// <summary>C6's numbers.</summary>
        public OrgC6Tuning C6 = new OrgC6Tuning();
        /// <summary>C7's numbers.</summary>
        public OrgC7Tuning C7 = new OrgC7Tuning();
        /// <summary>C8's numbers.</summary>
        public OrgC8Tuning C8 = new OrgC8Tuning();
        /// <summary>C9's numbers.</summary>
        public OrgC9Tuning C9 = new OrgC9Tuning();
        /// <summary>C10's numbers.</summary>
        public OrgC10Tuning C10 = new OrgC10Tuning();
    }

    /// <summary><c>C6</c>: OWNER C6 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class OrgC6Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C7</c>: OWNER C7 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class OrgC7Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C8</c>: OWNER C8 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class OrgC8Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C9</c>: OWNER C9 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class OrgC9Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C10</c>: OWNER C10 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class OrgC10Tuning : IOmitWhenDefault
    {
    }

}
