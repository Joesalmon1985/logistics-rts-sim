namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.4): the physical numbers of the unification ware block (72-159) that the legacy goods code needs (carry
    /// weight). Default 2 per unit until UN3a's generated catalogue fills its slot from <c>ObjectDef.Weight</c>. Legacy wares never
    /// reach this.
    /// </summary>
    public static partial class UnifiedWares
    {
        public const int DefaultWeight = 2;

        public static bool InBlock(Ware w) => (int)w >= Wares.UnifiedFirst && (int)w <= Wares.UnifiedLast;

        public static int Weight(Ware w)
        {
            int x = DefaultWeight;
            WeightUN3a(w, ref x);
            return x;
        }

        static partial void WeightUN3a(Ware w, ref int weight);
    }
}
