namespace God.Sim.Orgs
{
    /// <summary>
    /// C8's Tier 2 numbers (<c>Orgs.C8</c>, taken by C6 under the package index section 0: communal store and welfare). Written
    /// only when changed. Banking numbers stay out (C8 banking is deferred).
    /// </summary>
    public sealed partial class OrgC8Tuning
    {
        /// <summary>The ration a new shared store's charter starts with (units of food per request), before any decision changes it.</summary>
        public int StartRation = 2;
        /// <summary>A member keeps at least this many food units in hand when it deposits.</summary>
        public int DepositKeep = 1;
        /// <summary>The weight capacity of a communal store bin (0: none).</summary>
        public int StoreWeightCap = 200;
    }
}
