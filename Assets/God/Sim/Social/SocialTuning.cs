using System.ComponentModel;
using Newtonsoft.Json;

namespace God.Sim.Social
{
    /// <summary>
    /// <c>SimTuning.Social</c> (F0.12): M5's numbers. Written to JSON only when changed, so stage 3 and M3 hashes stay.
    /// OWNER: M5. Switches (SHARED_CONTRACTS 7.3): <see cref="Enabled"/> 0 turns social memory, recognition, gestures,
    /// barter and gifts off (every hook returns its default and <c>GodState.Social</c> stays null, so the post-F0 game is
    /// reproduced exactly); <see cref="Gestures"/> 0 turns off Say candidates and decoding (M5.15: barter and gifts fall
    /// to zero, because every hand-over of M5 follows a message and the receiver's own decision).
    /// </summary>
    /// <para>Fields added by M5 are written only when changed (so a game with only <c>Enabled = 0</c> writes exactly what the
    /// F0 stub wrote, and hashes as before, M5NeutralityTests).</para>
    public sealed class SocialTuning : IOmitWhenDefault
    {
        public int Enabled = 1;
        public int Gestures = 1;

        // ---- bounds (SOCIAL 3) ----
        /// <summary>Relation slots per mind (SOCIAL 3: default 40, range 32-48).</summary>
        [DefaultValue(40), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RelSlots = 40;

        // ---- recognition (SOCIAL 4.1) ----
        /// <summary><c>miss = FailBase x (1000 - Familiarity) / 1000</c> per mille, +Dark at night, +Far beyond half sight; clamp 0..900.</summary>
        [DefaultValue(350), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RecogniseFailBase = 350;
        [DefaultValue(100), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RecogniseDark = 100;
        [DefaultValue(150), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RecogniseFar = 150;
        /// <summary>
        /// Familiarity per completed interaction (a hand-over given or received), per addressed message decoded, and per
        /// distinct day of contact (seeing the other while attending). SOCIAL 4.1 says 100 per interaction; 180 is taken
        /// because with 100 the design's own test 5 (under 3% misses after 5 interactions) is arithmetically impossible
        /// (5 x 100 + 5 x 40 = 700 gives 10.5%).
        /// </summary>
        [DefaultValue(180), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int FamInteraction = 180;
        [DefaultValue(20), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int FamMessage = 20;
        [DefaultValue(40), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int FamDayContact = 40;

        // ---- evidence (SOCIAL 4.2) ----
        [DefaultValue(150), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int TrustGiftCap = 150;
        [DefaultValue(30), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int TrustBarter = 30;
        [DefaultValue(10), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int AffectionBarter = 10;
        [DefaultValue(-60), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RefusedTrust = -60;
        [DefaultValue(-30), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int RefusedAffection = -30;
        /// <summary>A help promised by <c>Cooperate</c> and not delivered: a small betrayal of this severity.</summary>
        [DefaultValue(20), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int UnkeptHelpSeverity = 20;

        // ---- exchange (ECONOMY 4.5) ----
        /// <summary>After my last hand-over of a deal, the partner has this many ticks to complete its side, else (in my mind) it defected.</summary>
        [DefaultValue(360), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int DealTimeoutTicks = 360;
        /// <summary>An agreed deal nobody has started is dropped after this many ticks (abandoned).</summary>
        [DefaultValue(240), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int DealAbandonTicks = 240;
        /// <summary>At most this many offers in one negotiation (offer and counter-offers), then one walks away.</summary>
        [DefaultValue(3), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int MaxRounds = 3;
        /// <summary>Below this trust a hand-over gives one unit at a time (incremental trade, ECONOMY 4.5 "who goes first").</summary>
        [DefaultValue(300), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int IncrementalTrust = 300;
        /// <summary>After a refused or failed deal with someone, it does not offer that person a new trade for this many ticks.</summary>
        [DefaultValue(600), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int OfferCooldownTicks = 600;
        /// <summary>The time a gesture takes (ticks).</summary>
        [DefaultValue(3), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int GestureTicks = 3;
        /// <summary>A walk to hand something over is given up after this many ticks.</summary>
        [DefaultValue(150), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int HandOverWalkTicks = 150;
        /// <summary>
        /// 1: the social score term (trust, grudge, affection and a deal's believed surplus per person) is added to the score.
        /// Default 0: the frozen inspector's option list (<c>OpportunitySlot</c>) has no Social column, so with the term on its
        /// terms would not add up to the total (M3.28). Proposed amendment: add <c>Social</c> to <c>OpportunitySlot</c>, then
        /// default this to 1. With 0, trust and grudge act through the relationship class feature (<c>NpcRel</c>) and what was
        /// learned about the person (<c>NpcRef</c>), and a deal's surplus through the offer-quality feature.
        /// </summary>
        [DefaultValue(0), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int Term = 0;
        /// <summary>1: each mind keeps a seen-but-unknown ring and makes a relation only at the second sighting within 5 days (SOCIAL 4.1).</summary>
        [DefaultValue(1), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] public int SecondSighting = 1;

        public SocialLearnTuning Learn = new SocialLearnTuning();
        public SocialDecayTuning Decay = new SocialDecayTuning();
    }

    /// <summary>
    /// Social reward channels into the learning store (SHARED_CONTRACTS 6; NPC_LEARNING 4.4). Goods are never paid here
    /// (only <c>Goods</c> pays goods, 2.3): what a trade or a gift brings arrives through the goods channel (logs at once,
    /// food when eaten, M3b's realised value).
    /// </summary>
    public sealed class SocialLearnTuning : IOmitWhenDefault
    {
        /// <summary>A deal partner took my goods and did not hand over its side (the agreement broken, NPC_LEARNING 4.4 "promise to me broken").</summary>
        public int Betrayed = -400;
        /// <summary>My offer or request was refused (frustration of a social act).</summary>
        public int Refused = -50;
        /// <summary>My addressed message expired with no answer.</summary>
        public int Ignored = -20;
        /// <summary>A person I gave to gave to me later (reciprocity episode, kind 9).</summary>
        public int Reciprocated = 300;
    }

    /// <summary>
    /// SOCIAL 4.7: per-day forgetting, independent of <c>LifeScale</c> (Q1) and configurable. <see cref="ScalePermille"/>
    /// multiplies every rate (rounded half up, at least 1 where the base is non-zero). Changes caused by events are never scaled.
    /// </summary>
    public sealed class SocialDecayTuning : IOmitWhenDefault
    {
        public int ScalePermille = 1000;
        public int FamiliarityPerDay = 2;
        /// <summary><c>GrudgeDecay = clamp(GrudgeBase - Vengefulness/2, GrudgeFloor, GrudgeCeil)</c> per mille per day.</summary>
        public int GrudgeBase = 60, GrudgeFloor = 10, GrudgeCeil = 110;
        /// <summary>Trust moves this per mille of the gap to the stranger prior per day (only while familiarity is below 300 and grudge not above 300).</summary>
        public int TrustDriftPermille = 10;
        /// <summary><c>Fear -= Fear &gt;&gt; FearShift</c> per day (0 off).</summary>
        public int FearShift = 4;
    }
}
