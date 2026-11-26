namespace God.Sim.Social
{
    /// <summary>
    /// <c>NpcMind.Social</c>: one mind's social memory (relations, obligations, inbox, log; SOCIAL_MEMORY 3). F0 slot and
    /// stub. OWNER: M5. Null until M5 gives a mind one; hashed with the mind only when non-null.
    /// </summary>
    public sealed class SocialMind
    {
        public void AddTo(ref StateHasher h) { }
    }
}
