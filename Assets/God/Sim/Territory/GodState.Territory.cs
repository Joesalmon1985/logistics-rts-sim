namespace God.Sim
{
    public sealed partial class GodState
    {
        /// <summary>Land, dice and territory (P1). Null until <see cref="TerritoryModule.StartGame"/>.</summary>
        public TerritoryState Territory;
    }
}
