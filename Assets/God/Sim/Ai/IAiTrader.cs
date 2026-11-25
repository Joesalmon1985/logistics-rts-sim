namespace God.Sim
{
    /// <summary>
    /// The trade seam of the faction AI. P11's <see cref="AiTrader"/> is the implementation the game uses, plugged in
    /// through <see cref="AiDirector.Attach"/> (or <see cref="AiBrain.Trader"/>); <see cref="NoTrade"/> is the default.
    /// Every hook runs inside a brain's think, on sim ticks, and must stay deterministic: read <see cref="GodState"/>,
    /// send commands through <see cref="AiBrain.Sim"/> only, and use <see cref="AiBrain.ThinkRng"/> for any randomness.
    /// Ware arrays are indexed by <c>(int)Ware</c>, <see cref="Wares.Count"/> long.
    /// </summary>
    public interface IAiTrader
    {
        /// <summary>Once per think, before building: answer the offers waiting for this faction (envoys that have arrived).</summary>
        void ConsiderOffers(AiBrain brain);

        /// <summary>Once per think, after building: send offers (for example for <see cref="AiBrain.WareNeeds"/>, paying with <see cref="AiBrain.WareSurplus"/>), preferring the worst-placed faction that is not winning.</summary>
        void ProposeTrades(AiBrain brain);

        /// <summary>
        /// Would this faction accept <paramref name="from"/>'s offer of <paramref name="give"/> wares for <paramref name="want"/>
        /// wares now? Used by <see cref="ConsiderOffers"/> and by an <c>ITradeJudge</c> adapter.
        /// </summary>
        bool JudgeIncoming(AiBrain brain, Faction from, int[] give, int[] want);
    }

    /// <summary>The default trader: never offers, never accepts.</summary>
    public sealed class NoTrade : IAiTrader
    {
        public static readonly NoTrade Instance = new NoTrade();
        public void ConsiderOffers(AiBrain brain) { }
        public void ProposeTrades(AiBrain brain) { }
        public bool JudgeIncoming(AiBrain brain, Faction from, int[] give, int[] want) => false;
    }
}
