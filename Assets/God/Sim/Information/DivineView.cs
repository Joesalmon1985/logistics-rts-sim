namespace God.Sim.Information
{
    /// <summary>Contracts 9: how a god perceives one domain.</summary>
    public enum DivinePerception : byte { Omniscient = 0, Perceived = 1, Reported = 2, Blind = 3 }

    /// <summary>Contracts 9: the channels through which a god may perceive (bit flags).</summary>
    [System.Flags]
    public enum DivineChannel : ushort
    {
        None = 0, Avatar = 1, Possessed = 2, Prayer = 4, ConsecratedSite = 8, Followers = 16, Records = 32, Sight = 64,
    }

    /// <summary>
    /// Contracts 9 (INFORMATION 12.3): a god's divine-observation profile, per scenario and per god (21.4). F1 lands the type
    /// with the default (omniscient, every channel); D1 adds the per-domain fields additively. Plain data (saved in
    /// <c>GodRecord.Profile</c> and in <c>SetDivineProfile</c>).
    /// </summary>
    public sealed class DivineObservationProfile
    {
        public DivinePerception Default = DivinePerception.Omniscient;
        public DivineChannel Channels = DivineChannel.Avatar | DivineChannel.Possessed | DivineChannel.Prayer | DivineChannel.ConsecratedSite
            | DivineChannel.Followers | DivineChannel.Records | DivineChannel.Sight;
    }

    /// <summary>What a god's view says of one person (layer 8; D1 adds fields additively).</summary>
    public struct DivinePersonView
    {
        public int Npc;
        public bool Alive;
        public int Node;
        public Faction Faction;
    }

    /// <summary>
    /// Contracts 9 (layer 8): what an <c>IGodPolicy</c> and the human divine UI may read. Filled by <c>DivineViewFiller</c> (D1)
    /// from the god's <see cref="DivineObservationProfile"/>; never <see cref="GodState"/> directly. F1 lands the minimal subset
    /// (<see cref="God"/>, <see cref="Profile"/>, <see cref="AsOfTick"/>, <see cref="TryPerson"/>); D1 and U2 add members
    /// additively in this file without an amendment. OWNER: D1.
    /// </summary>
    public interface IDivineView
    {
        int God { get; }
        DivineObservationProfile Profile { get; }
        long AsOfTick { get; }
        bool TryPerson(int npc, out DivinePersonView person);
    }
}
