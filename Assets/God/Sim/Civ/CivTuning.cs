using System.ComponentModel;
using Newtonsoft.Json;

namespace God.Sim.Civ
{
    /// <summary>
    /// F1.3 (contracts 16): <c>SimTuning.Civ</c>. <see cref="Enabled"/> 0: no <c>GodState.Civ</c>: no assets, obligations,
    /// contracts or citizenship records; the C1-C5 hooks return their defaults.
    /// The root is frozen (F1.3) and deliberately <b>not</b> partial: its field order is the JSON order the core hash reads, so
    /// no second file may add root fields. Each nested block is partial and belongs to one package, which adds its fields
    /// only in its own file (<c>CivTuning.&lt;Pkg&gt;.cs</c>); a block written only when it differs from its defaults.
    /// </summary>
    public sealed class CivTuning : IOmitWhenDefault
    {
        public int Enabled;
        /// <summary>C1's numbers.</summary>
        public CivC1Tuning C1 = new CivC1Tuning();
        /// <summary>C2's numbers.</summary>
        public CivC2Tuning C2 = new CivC2Tuning();
        /// <summary>C3's numbers.</summary>
        public CivC3Tuning C3 = new CivC3Tuning();
        /// <summary>C5's numbers.</summary>
        public CivC5Tuning C5 = new CivC5Tuning();
    }

    /// <summary><c>C1</c>: OWNER C1 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class CivC1Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C2</c>: OWNER C2 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class CivC2Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C3</c>: OWNER C3 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class CivC3Tuning : IOmitWhenDefault
    {
    }

    /// <summary><c>C5</c>: OWNER C5 (fields in its own partial file). Written only when changed.</summary>
    public sealed partial class CivC5Tuning : IOmitWhenDefault
    {
    }

}
