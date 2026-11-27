namespace God.Sim.Civ
{
    /// <summary>
    /// C3's numbers (<c>Civ.C3</c>, OWNER C3): employment between individuals. Written only when changed
    /// (<see cref="IOmitWhenDefault"/>), so a game that never sets them hashes as before. Shift timing, grace and abandonment
    /// live here, not in constants, so continuous plant operation can set its own pattern (spec 22).
    /// </summary>
    public sealed partial class CivC3Tuning
    {
        /// <summary>1: people may offer and take work (C3's candidates exist). 0: no offer is ever made (C3 idle).</summary>
        public int HireEnabled = 1;
        /// <summary>Work ticks at the asset that make one paid shift (the default terms of an offer).</summary>
        public int ShiftTicks = 120;
        /// <summary>Ticks after a pay unit falls due within which the employer may still pay it before it counts as unpaid (an arrears unit).</summary>
        public int PayGraceTicks = 240;
        /// <summary>An Active contract without any work or pay progress for this many ticks ends as Abandoned (a suspension never does).</summary>
        public int AbandonTicks = 300;
        /// <summary>An offer not answered within this many ticks lapses.</summary>
        public int OfferTicks = 120;
        /// <summary>Unpaid units (as the worker believes) from which a walk-out is among its options.</summary>
        public int WalkOutUnpaid = 2;
        /// <summary>The pay an employer first offers per unit (it adds one after each refusal by the same person, up to <see cref="MaxPay"/>).</summary>
        public int BasePay = 1, MaxPay = 4;
        /// <summary>
        /// The worker's own valuation of a shift (worth units of <c>Social.Valuation</c>): the effort of a shift (scaled up by
        /// fatigue), the walk per node to the workplace, and the food it believes it could forage in that time instead
        /// (units per shift, from a stocked food place it remembers).
        /// </summary>
        public int EffortWorth = 60, WalkWorthPerNode = 4, ForageUnitsPerShift = 2;
        /// <summary>Trust lost (own belief, -1000..1000) per unit not paid, gained per unit paid.</summary>
        public int TrustLossUnpaid = 150, TrustGainPaid = 40;
        /// <summary>At most this many open contracts per employer agent (each needs its own slot on an asset).</summary>
        public int MaxWorkersPerEmployer = 2;
        /// <summary>Days an ended contract stays on the record (the card's wage history) before it is dropped.</summary>
        public int KeepEndedDays = 5;
    }
}
