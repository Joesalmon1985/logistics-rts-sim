using System.Collections.Generic;
using God.Sim.Divinity;

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
        /// <summary>D1: the layer the item comes from (<c>DivineObservation</c> for an omniscient god).</summary>
        public KnowledgeLayer Layer;
        /// <summary>D1: asleep, and the verb of the visible action (as a sighting shows it).</summary>
        public bool Asleep;
        public byte Verb;
    }

    /// <summary>D1: one god as a divine view shows it (what U2's Mythic meter and the god list read). Plain data, a copy.</summary>
    public struct DivineGodView
    {
        public int God;
        public string Name;
        public Faction OriginFaction;
        public GodSeat Seat;
        public string PolicyName;
        /// <summary>Milli-Mythic.</summary>
        public long Mythic, MythicMax, RegenPerDay;
        public DivinityMode Mode;
        /// <summary>Per <see cref="InterventionKind"/>: the tick from which a spend of the kind is allowed (a copy).</summary>
        public long[] CooldownUntil;
        public int AvatarBody;
        public DivinePerception Profile;
        public KnowledgeLayer Layer;

        /// <summary>Ticks until a kind's cooldown ends (0: ready).</summary>
        public long CooldownLeft(InterventionKind kind, long now)
        {
            int k = (int)kind;
            if (CooldownUntil == null || k < 0 || k >= CooldownUntil.Length) return 0;
            long left = CooldownUntil[k] - now;
            return left > 0 ? left : 0;
        }
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

        // ---- D1 additive members (contracts 9 last paragraph) ----
        /// <summary>The game's current Mythic mode.</summary>
        DivinityMode Mode { get; }
        /// <summary>Number of gods (1..GodCount are their ids).</summary>
        int GodCount { get; }
        /// <summary>One god (false for an unknown id).</summary>
        bool TryGod(int god, out DivineGodView view);
        /// <summary>Every god, ascending id, written into <paramref name="into"/>.</summary>
        void Gods(List<DivineGodView> into);
        /// <summary>Every living person as the god perceives them, ascending id, written into <paramref name="into"/>.</summary>
        void People(List<DivinePersonView> into);
    }
}
