namespace God.Sim.Information
{
    /// <summary>The domains a <see cref="KnowledgeProfile"/> sets a mode for (INFORMATION_AND_MEDIA_DESIGN §4.8).</summary>
    public enum KnowledgeDomain { Treasury = 0, Population = 1, Health = 2, Crime = 3, Justice = 4, Military = 5, Economy = 6, Media = 7, Environment = 8, Politics = 9 }

    /// <summary>
    /// How a domain of the view is filled. <see cref="FullInformation"/> is the stage 4 default: the faction's own affairs
    /// exactly, foreign affairs only through the existing visibility rules. <see cref="Reported"/> (only what institutions
    /// measured) arrives with the records of later ages and is refused by <see cref="ViewFiller"/> until then.
    /// </summary>
    public enum KnowledgeMode { FullInformation = 0, Reported = 1 }

    /// <summary>
    /// Where a foreign item in the view came from (owner decision Q21): never the other faction's books, only a channel
    /// through which it could have been learned.
    /// </summary>
    public enum KnowledgeChannel
    {
        /// <summary>The faction's own books and people (not foreign).</summary>
        Own = 0,
        /// <summary>Seen on the island by the faction's people and soldiers (stage 3: the whole island is in view).</summary>
        Observation = 1,
        Trade = 2,
        /// <summary>What the other side declared (war, peace offers).</summary>
        Diplomacy = 3,
        Migration = 4,
        Communication = 5,
        Media = 6,
        Intelligence = 7,
        /// <summary>What the game publishes for everyone (stage 3: the victory point standings).</summary>
        OfficialReporting = 8,
    }

    /// <summary>A mode per domain. Built fresh (no shared instance), read-only once built.</summary>
    public sealed class KnowledgeProfile
    {
        public const int DomainCount = 10;
        private readonly KnowledgeMode[] _modes = new KnowledgeMode[DomainCount];

        public KnowledgeProfile(KnowledgeMode all = KnowledgeMode.FullInformation)
        {
            for (int i = 0; i < DomainCount; i++) _modes[i] = all;
        }

        /// <summary>The stage 4 default.</summary>
        public static KnowledgeProfile FullInformation() => new KnowledgeProfile(KnowledgeMode.FullInformation);

        public KnowledgeMode ModeOf(KnowledgeDomain d) => _modes[(int)d];

        /// <summary>A copy with one domain changed.</summary>
        public KnowledgeProfile With(KnowledgeDomain d, KnowledgeMode m)
        {
            var p = new KnowledgeProfile();
            for (int i = 0; i < DomainCount; i++) p._modes[i] = _modes[i];
            p._modes[(int)d] = m;
            return p;
        }

        public bool IsFullInformation
        {
            get { foreach (var m in _modes) if (m != KnowledgeMode.FullInformation) return false; return true; }
        }
    }

    /// <summary>What a foreign item says.</summary>
    public enum ForeignFact
    {
        VictoryPoints = 0, Hqs = 1, Citadels = 2, Eliminated = 3,
        /// <summary>Value: 1 while that faction is at war with the viewer.</summary>
        AtWarWithViewer = 4,
        /// <summary>Value: 1 while that faction's peace offer to the viewer stands.</summary>
        OffersPeaceToViewer = 5,
        /// <summary>Its soldiers on the island.</summary>
        VisibleSoldiers = 6,
        /// <summary>Its buildings and sites on the island.</summary>
        VisibleBuildings = 7,
        /// <summary>Its envoys walking the island.</summary>
        VisibleEnvoys = 8,
    }

    /// <summary>
    /// One thing the viewing faction knows about another (Q21, test I19): the fact, its value, the channel it came through,
    /// when it was true and how sure the faction is (permille). Under <c>FullInformation</c> in stage 4 the channels are
    /// observation (no fog yet), diplomacy and the official standings, so every item is exact and current; later ages
    /// make them late, partial or wrong.
    /// </summary>
    public readonly struct ForeignKnowledge
    {
        public readonly Faction About;
        public readonly ForeignFact Fact;
        public readonly long Value;
        public readonly KnowledgeChannel Channel;
        public readonly long AsOfTick;
        public readonly int ConfidencePermille;

        public ForeignKnowledge(Faction about, ForeignFact fact, long value, KnowledgeChannel channel, long asOfTick, int confidencePermille)
        {
            About = about; Fact = fact; Value = value; Channel = channel; AsOfTick = asOfTick; ConfidencePermille = confidencePermille;
        }

        public override string ToString() => FactionInfo.Name(About) + " " + Fact + "=" + Value + " via " + Channel + " @" + AsOfTick + " (" + ConfidencePermille + "‰)";
    }
}
