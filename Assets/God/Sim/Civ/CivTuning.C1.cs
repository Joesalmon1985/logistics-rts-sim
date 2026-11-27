namespace God.Sim.Civ
{
    /// <summary>
    /// C1's numbers (<c>Civ.C1</c>, OWNER C1): the physical economy of productive assets. Written only when changed
    /// (<see cref="IOmitWhenDefault"/>), so a game that never sets them hashes as before. The per-kind numbers (build cost,
    /// work ticks, recipe, store capacity) are rows of <see cref="AssetTable"/> (data), not tuning.
    /// </summary>
    public sealed partial class CivC1Tuning
    {
        /// <summary>1: people may found and build assets. 0: no asset is ever founded (C1-T1 "idle": nothing to sense, nothing built).</summary>
        public int BuildEnabled = 1;
        /// <summary>WORLD_ONTOLOGY O7: days a dropped owned lot stays its owner's (truth overlay) before it counts as abandoned.</summary>
        public int LostPropertyDays = 3;
        /// <summary>How many assets (finished or not) one owner (a person or a household) may hold before the free-plot sense stops offering more.</summary>
        public int MaxAssetsPerOwner = 2;
        /// <summary>The base work speed (permille of one work tick per tick) of <see cref="Work.Speed"/> (ECONOMY 4.2: 1000 + 3 x skill, tools, fatigue).</summary>
        public int BaseWorkSpeed = 1000;
        /// <summary>Confidence (0-255) of an ownership belief formed by watching someone build an asset, and by watching someone work it.</summary>
        public int SeenBuiltConfidence = 230, SeenUsedConfidence = 120;
        /// <summary>At most this many assets in sight are perceived per think (nearest first; the Sense seam bound, spec 16).</summary>
        public int SenseAssets = 3;
    }
}
