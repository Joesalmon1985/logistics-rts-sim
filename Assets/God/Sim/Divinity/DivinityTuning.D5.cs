namespace God.Sim.Divinity
{
    /// <summary>
    /// D4b's numbers for the avatar (the brief's D5), <c>Divinity.Avatar</c> (POSSESSION_AND_AVATAR 5). Fields only in this file.
    /// The cost, upkeep and the cooldown after a destruction are D1's (<c>Divinity.Mythic.Costs.ManifestAvatar</c>,
    /// <c>.Upkeep.AvatarPerTick</c>, <c>.Cooldown.AvatarAfterDestructionTicks</c>). OWNER: D4 (D5 slots).
    /// </summary>
    public sealed partial class AvatarTuning
    {
        /// <summary>Health (micro) the body has at each manifestation (full = 1 000 000).</summary>
        public int Health = 1_000_000;
        /// <summary>Distinct observers remembered per manifestation (the demonstration reach count is capped here).</summary>
        public int MaxObservers = 64;
    }
}
